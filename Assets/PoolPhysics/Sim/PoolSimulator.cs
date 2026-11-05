using System;
using System.Collections.Generic;
using System.Numerics;

namespace PoolPhysics.Sim
{
    /// <summary>
    /// Deterministic, table-agnostic billiard simulator. Pure C# (no UnityEngine)
    /// so it runs identically headless (GOLDEN regression) and in-engine.
    ///
    /// Model recovered from Cue VR: fixed 0.5 ms substeps, analytic spheres,
    /// sliding→rolling via cloth friction, ball-ball impulse + throw, cushion
    /// reflection + throw, pocket capture. No iterative solver.
    /// </summary>
    public sealed class PoolSimulator
    {
        public readonly List<Ball> Balls = new List<Ball>();
        public TableSpecData Table;
        public PhysicsProfile Profile;

        private float _accumulator;
        public int CueBallIndex = 0;

        public PoolSimulator(TableSpecData table, PhysicsProfile profile)
        {
            Table = table; Profile = profile;
        }

        public bool AllAtRest
        {
            get
            {
                for (int i = 0; i < Balls.Count; i++)
                    if (Balls[i].Active && !Balls[i].Pocketed && Balls[i].IsMoving) return false;
                return true;
            }
        }

        /// <summary>Advance the sim by real elapsed time using a fixed-substep accumulator.</summary>
        public void Step(float dt)
        {
            _accumulator += dt;
            int guard = 0;
            while (_accumulator >= Profile.Substep && guard < Profile.MaxSubstepsPerFrame)
            {
                Substep(Profile.Substep);
                _accumulator -= Profile.Substep;
                guard++;
            }
        }

        /// <summary>One physics substep. Exposed for unit tests / determinism checks.</summary>
        public void Substep(float dt)
        {
            float g = Profile.Gravity;
            Vector3 napU = new Vector3(MathF.Cos(Table.NapHeading), 0f, MathF.Sin(Table.NapHeading));

            // 1) Integrate ball motion: nap-aware friction (slide→roll) + spin decay.
            for (int i = 0; i < Balls.Count; i++)
            {
                var b = Balls[i];
                if (!b.Active || b.Pocketed) continue;

                float R = b.Radius;
                Vector3 vc = b.ContactPointVelocity();
                float vcm = vc.Length();

                if (vcm > Profile.RestSpeedEpsilon)
                {
                    Vector3 vhat = vc / vcm;
                    // nap anisotropy: more friction when moving ACROSS the nap
                    float along = Vector3.Dot(vhat, napU);
                    float acrossFrac = MathF.Sqrt(MathF.Max(0f, 1f - along * along));
                    float napMul = 1f + Profile.NapAnisotropy * acrossFrac;

                    Vector3 a = -vhat * (Profile.SlidingFriction * g * napMul);

                    // nap steer/curve: lateral accel nudging longitudinal shots off straight
                    if (Profile.NapSteer != 0f)
                    {
                        Vector3 across = vhat - napU * along;
                        float am = across.Length();
                        if (am > 1e-5f)
                            a += (across / am) * (Profile.NapSteer * g * along);
                    }

                    b.Velocity += a * dt;
                    // friction torque about contact: alpha = (r x a) * 5/(2 R^2)
                    Vector3 r = new Vector3(0, -R, 0);
                    Vector3 alpha = Vector3.Cross(r, a) * (5f / (2f * R * R));
                    b.AngularVelocity += alpha * dt;
                }
                else
                {
                    // ROLLING: nap-aware rolling resistance; enforce no-slip.
                    float vm = b.Velocity.Length();
                    if (vm > Profile.RestSpeedEpsilon)
                    {
                        Vector3 vhat = b.Velocity / vm;
                        float along = Vector3.Dot(vhat, napU);
                        float acrossFrac = MathF.Sqrt(MathF.Max(0f, 1f - along * along));
                        float napMul = 1f + Profile.NapAnisotropy * acrossFrac;
                        b.Velocity -= vhat * (Profile.RollingFriction * g * napMul * dt);
                    }
                    else { b.Velocity = Vector3.Zero; }
                    // no-slip constraint: w.x = v.z/R, w.z = -v.x/R (keep w.y english)
                    float wy = b.AngularVelocity.Y;
                    b.AngularVelocity = new Vector3(b.Velocity.Z / R, wy, -b.Velocity.X / R);
                }

                // vertical (english) spin decays via spinning friction / decay const
                float wy2 = b.AngularVelocity.Y * (float)Math.Exp(-Profile.SpinDecay * dt);
                if (Math.Abs(wy2) < 1e-4f) wy2 = 0f;
                b.AngularVelocity = new Vector3(b.AngularVelocity.X, wy2, b.AngularVelocity.Z);

                // integrate position on the plane
                b.Position += b.Velocity * dt;
                b.Position.Y = R;
                Balls[i] = b;
            }

            // 2) Ball-ball collisions (equal-ish mass impulse + throw hook).
            for (int i = 0; i < Balls.Count; i++)
            {
                if (!Balls[i].Active || Balls[i].Pocketed) continue;
                for (int j = i + 1; j < Balls.Count; j++)
                {
                    if (!Balls[j].Active || Balls[j].Pocketed) continue;
                    var a = Balls[i]; var bb = Balls[j];
                    ResolveBallBall(ref a, ref bb);
                    Balls[i] = a; Balls[j] = bb;
                }
            }

            // 3) Cushion collisions.
            for (int i = 0; i < Balls.Count; i++)
            {
                if (!Balls[i].Active || Balls[i].Pocketed) continue;
                var b = Balls[i];
                for (int r = 0; r < Table.Rails.Count; r++)
                    ResolveCushion(ref b, Table.Rails[r]);
                b.Position.Y = b.Radius;
                Balls[i] = b;
            }

            // 4) Pockets.
            for (int i = 0; i < Balls.Count; i++)
            {
                var b = Balls[i];
                if (!b.Active || b.Pocketed) continue;
                for (int p = 0; p < Table.Pockets.Count; p++)
                {
                    var pk = Table.Pockets[p];
                    float dx = b.Position.X - pk.Center.X, dz = b.Position.Z - pk.Center.Z;
                    float rr = pk.Radius + Profile.PocketCaptureRadius * 0.0f; // capture radius
                    if (dx * dx + dz * dz < pk.Radius * pk.Radius)
                    { b.Pocketed = true; b.Velocity = Vector3.Zero; b.AngularVelocity = Vector3.Zero; break; }
                }
                Balls[i] = b;
            }
        }

        private void ResolveBallBall(ref Ball a, ref Ball b)
        {
            Vector3 delta = b.Position - a.Position;
            delta.Y = 0;
            float dist = delta.Length();
            float minD = a.Radius + b.Radius;
            if (dist <= 0f || dist >= minD) return;

            Vector3 n = delta / dist;
            float vn = Vector3.Dot(a.Velocity - b.Velocity, n);
            if (vn <= 0f) return; // separating

            float e = Profile.BallBallRestitution;
            float invMa = 1f / a.Mass, invMb = 1f / b.Mass;
            float jn = (1f + e) * vn / (invMa + invMb);
            Vector3 Jn = n * jn;
            a.Velocity -= Jn * invMa;
            b.Velocity += Jn * invMb;

            // positional de-overlap
            float pen = minD - dist;
            Vector3 corr = n * (pen * 0.5f);
            a.Position -= corr; b.Position += corr;

            // THROW: tangential friction impulse from relative SURFACE velocity (spin-aware).
            if (Profile.BallBallFriction > 0f)
            {
                Vector3 ra = new Vector3(n.X * a.Radius, 0f, n.Z * a.Radius);
                Vector3 rb = new Vector3(-n.X * b.Radius, 0f, -n.Z * b.Radius);
                Vector3 sa = a.Velocity + Vector3.Cross(a.AngularVelocity, ra);
                Vector3 sb = b.Velocity + Vector3.Cross(b.AngularVelocity, rb);
                Vector3 rel = sa - sb;
                Vector3 relT = rel - Vector3.Dot(rel, n) * n;
                float relTm = relT.Length();
                if (relTm > 1e-6f)
                {
                    Vector3 tt = relT / relTm;
                    float jt = MathF.Min(Profile.BallBallFriction * jn, relTm / (invMa + invMb));
                    Vector3 Jt = -tt * jt;
                    a.Velocity += Jt * invMa;
                    b.Velocity -= Jt * invMb;
                    float kA = 5f / (2f * a.Mass * a.Radius * a.Radius);
                    float kB = 5f / (2f * b.Mass * b.Radius * b.Radius);
                    a.AngularVelocity += Vector3.Cross(ra, Jt) * kA;
                    b.AngularVelocity += Vector3.Cross(rb, -Jt) * kB;
                }
            }
        }

        private void ResolveCushion(ref Ball ball, Rail rail)
        {
            Vector3 c = ClosestPointOnSegment(new Vector3(ball.Position.X, 0, ball.Position.Z),
                                              new Vector3(rail.A.X, 0, rail.A.Z),
                                              new Vector3(rail.B.X, 0, rail.B.Z));
            Vector3 d = new Vector3(ball.Position.X - c.X, 0, ball.Position.Z - c.Z);
            float dist = d.Length();
            if (dist >= ball.Radius || dist <= 1e-6f) return;

            Vector3 inward = new Vector3(rail.Inward.X, 0, rail.Inward.Z);
            if (Vector3.Dot(d, inward) > 0f) return; // already inside

            // push out + reflect
            ball.Position = new Vector3(c.X + inward.X * ball.Radius, ball.Radius, c.Z + inward.Z * ball.Radius);
            float vn = Vector3.Dot(ball.Velocity, inward);
            if (vn < 0f)
            {
                float e = Profile.CushionRestitution;
                ball.Velocity -= (1f + e) * vn * inward;

                // tangential friction on the rail
                Vector3 vt = ball.Velocity - Vector3.Dot(ball.Velocity, inward) * inward;
                ball.Velocity = Vector3.Dot(ball.Velocity, inward) * inward + vt * (1f - Profile.CushionFriction * 0.1f);

                // spin loss + gearing: english (vertical spin) converts to tangential throw
                Vector3 tangent = new Vector3(-inward.Z, 0f, inward.X);
                float gear = Profile.CushionGear * ball.AngularVelocity.Y * ball.Radius;
                ball.Velocity += tangent * gear;
                ball.AngularVelocity *= Profile.CushionSpinLoss;

                // [TUNE] cushion throw ∝ x/√y (approach-angle normal loss) — CushionThrowCoeff
                if (Profile.CushionThrowCoeff != 0f)
                {
                    float tanSpeed = vt.Length();
                    if (tanSpeed > 1e-5f)
                        ball.Velocity -= vt / tanSpeed * (Profile.CushionThrowCoeff * (-vn));
                }
            }
        }

        private static Vector3 ClosestPointOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(p - a, ab) / Math.Max(1e-9f, ab.LengthSquared());
            t = Math.Clamp(t, 0f, 1f);
            return a + ab * t;
        }
    }
}
