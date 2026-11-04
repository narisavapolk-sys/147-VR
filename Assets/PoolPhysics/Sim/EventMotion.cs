using System;
using System.Numerics;

namespace PoolPhysics.Sim
{
    /// <summary>
    /// One analytic motion segment for a ball's current phase (Leckie & Greenspan).
    /// Locks the motion-segment API that the event loop consumes.
    /// </summary>
    public struct MotionSegment
    {
        public Vector3 EndPosition;
        public Vector3 EndLinearVelocity;
        public Vector3 EndAngularVelocity;
        public float   Duration;        // time until this phase ends (transition/stop)
        public BallPhase Phase;
        public bool    EntersRolling;   // sliding -> rolling at end
        public bool    ComesToRest;     // reaches rest at end
    }

    /// <summary>
    /// Canonical single-ball motion math (table plane = XZ, up = +Y, english = w.Y).
    /// Both the event loop (Advance/Segment) and the closed-form free-roll
    /// reference (Solve) live here so there is ONE source of truth for motion.
    /// </summary>
    public static class EventMotion
    {
        public const float EPS = 1e-4f;

        static Vector2 ContactU(ref Ball b)
        {
            float R = b.Radius;
            return new Vector2(b.Velocity.X + R * b.AngularVelocity.Z, b.Velocity.Z - R * b.AngularVelocity.X);
        }

        /// <summary>Trace helper: length of the contact-point slip velocity.</summary>
        public static float ContactULen(ref Ball b) => ContactU(ref b).Length();

        // NOTE: only w.Y (english) is a free-decaying spin. w.X/w.Z are the ROLLING
        // state derived from linear velocity (no-slip) during Rolling, and are set by
        // friction during Sliding — they are not independent free spin.
        public static BallPhase Classify(ref Ball b)
        {
            float vm = new Vector2(b.Velocity.X, b.Velocity.Z).Length();
            if (vm < EPS && MathF.Abs(b.AngularVelocity.Y) < EPS) return BallPhase.Rest;
            if (ContactU(ref b).Length() > EPS) return BallPhase.Sliding;
            if (vm > EPS) return BallPhase.Rolling;
            return BallPhase.Spinning;
        }

        static void CheckInputs(float R, float g, float muS, float muR)
        {
            if (R <= 0f) throw new ArgumentOutOfRangeException(nameof(R), "radius must be > 0");
            if (g <= 0f) throw new ArgumentOutOfRangeException(nameof(g), "gravity must be > 0");
            if (muS < 0f) throw new ArgumentOutOfRangeException(nameof(muS), "sliding friction must be >= 0");
            if (muR < 0f) throw new ArgumentOutOfRangeException(nameof(muR), "rolling friction must be >= 0");
        }

        /// <summary>Analytic duration until the current phase ends. Nap-aware: the bound MUST
        /// match the accel that Advance() integrates (μR·g·(1+napAniso·across)) — otherwise
        /// the loop overshoots the phase end and chunk splits diverge (determinism bug).</summary>
        public static float SegmentDuration(ref Ball b, float g, float muS, float muR, float spinDecay,
                                            Vector2 napU, float napAniso)
        {
            switch (Classify(ref b))
            {
                case BallPhase.Sliding:
                    if (muS <= 0f) return float.PositiveInfinity;
                    // u' ∥ û during slide (u direction is constant), so the classic
                    // 2u/(7μS·g) bound stays conservative (nap makes the real slide→roll
                    // transition LATER: rate 1.5−napAniso·across < 1.5). No overshoot ⇒ safe.
                    return 2f * ContactU(ref b).Length() / (7f * muS * g);
                case BallPhase.Rolling:
                {
                    if (muR <= 0f) return float.PositiveInfinity;
                    Vector2 v2 = new Vector2(b.Velocity.X, b.Velocity.Z);
                    float vm = v2.Length();
                    if (vm < 1e-9f) return float.PositiveInfinity;
                    Vector2 vh = v2 / vm;
                    float alR = Vector2.Dot(vh, napU);
                    float acrossR = MathF.Sqrt(MathF.Max(0f, 1f - alR * alR));
                    return vm / (muR * g * (1f + napAniso * acrossR));
                }
                case BallPhase.Spinning:
                    if (spinDecay <= 0f) return float.PositiveInfinity;
                    return MathF.Max(0.05f, MathF.Log(MathF.Abs(b.AngularVelocity.Y) / EPS) / spinDecay);
                default: return float.PositiveInfinity;
            }
        }

        /// <summary>
        /// Planar (XZ) acceleration of a ball in its current phase — the SAME
        /// accel Advance() integrates with. Used by the trajectory-consistent TOI
        /// so collision-time prediction matches the quadratic motion (fixes the
        /// linear-TOI non-additivity / determinism gap).
        /// </summary>
        public static Vector2 Acceleration(ref Ball b, float g, float muS, float muR,
                                           Vector2 napU, float napAniso, float napSteer)
        {
            float R = b.Radius;
            Vector2 v2 = new Vector2(b.Velocity.X, b.Velocity.Z);
            switch (Classify(ref b))
            {
                case BallPhase.Sliding:
                {
                    Vector2 u = ContactU(ref b); float um = u.Length();
                    if (um < 1e-9f) return Vector2.Zero;
                    Vector2 uh = u / um;
                    float al = Vector2.Dot(uh, napU); float across = MathF.Sqrt(MathF.Max(0f, 1f - al * al));
                    Vector2 a = -uh * (muS * g * (1f + napAniso * across));
                    if (napSteer != 0f) { Vector2 perp = uh - napU * al; if (perp.Length() > 1e-5f) a += perp / perp.Length() * (napSteer * g * al); }
                    return a;
                }
                case BallPhase.Rolling:
                {
                    float vm = v2.Length(); if (vm < 1e-9f) return Vector2.Zero;
                    Vector2 vh = v2 / vm;
                    float al = Vector2.Dot(vh, napU); float across = MathF.Sqrt(MathF.Max(0f, 1f - al * al));
                    return -vh * (muR * g * (1f + napAniso * across));
                }
                default: return Vector2.Zero; // Rest / Spinning: no planar accel
            }
        }

        /// <summary>Advance one ball by dt (dt <= its phase duration). Analytic, no overshoot.</summary>
        public static void Advance(ref Ball b, float dt, float g, float muS, float muR, float spinDecay,
                                   Vector2 napU, float napAniso, float napSteer)
        {
            CheckInputs(b.Radius, g, muS, muR);
            float R = b.Radius;
            Vector2 v2 = new Vector2(b.Velocity.X, b.Velocity.Z);
            Vector2 p2 = new Vector2(b.Position.X, b.Position.Z);
            BallPhase ph = Classify(ref b);

            if (ph == BallPhase.Sliding)
            {
                Vector2 u = ContactU(ref b); float um = u.Length();
                if (um < 1e-9f) { b.Phase = BallPhase.Rolling; return; }
                Vector2 uh = u / um;
                float al = Vector2.Dot(uh, napU); float across = MathF.Sqrt(MathF.Max(0f, 1f - al * al));
                Vector2 a = -uh * (muS * g * (1f + napAniso * across));
                if (napSteer != 0f) { Vector2 perp = uh - napU * al; if (perp.Length() > 1e-5f) a += perp / perp.Length() * (napSteer * g * al); }
                Vector2 vnew = v2 + a * dt;
                Vector2 pnew = p2 + v2 * dt + 0.5f * a * dt * dt;
                float k = 5f * muS * g / (2f * R);
                b.AngularVelocity = new Vector3(b.AngularVelocity.X + k * uh.Y * dt, b.AngularVelocity.Y, b.AngularVelocity.Z - k * uh.X * dt);
                b.Velocity = new Vector3(vnew.X, 0, vnew.Y);
                b.Position = new Vector3(pnew.X, R, pnew.Y);
                b.Phase = BallPhase.Sliding;
            }
            else if (ph == BallPhase.Rolling)
            {
                float vm = v2.Length(); if (vm < 1e-9f) { b.Phase = BallPhase.Rest; b.Velocity = Vector3.Zero; return; }
                Vector2 vh = v2 / vm;
                float al = Vector2.Dot(vh, napU); float across = MathF.Sqrt(MathF.Max(0f, 1f - al * al));
                Vector2 a = -vh * (muR * g * (1f + napAniso * across));
                float aa = a.Length();
                Vector2 vnew = v2 + a * dt;
                Vector2 pnew;
                if (Vector2.Dot(vnew, vh) < 0f)
                {   // reached the stop point inside this hop: land EXACTLY on it
                    // (parabolic overshoot would wrap and depend on how dt was chunked)
                    vnew = Vector2.Zero;
                    pnew = p2 + vh * (vm * vm / (2f * MathF.Max(aa, 1e-9f)));
                }
                else pnew = p2 + v2 * dt + 0.5f * a * dt * dt;
                b.Velocity = new Vector3(vnew.X, 0, vnew.Y);
                b.Position = new Vector3(pnew.X, R, pnew.Y);
                float wy = b.AngularVelocity.Y * (float)Math.Exp(-spinDecay * dt);
                b.AngularVelocity = new Vector3(vnew.Y / R, wy, -vnew.X / R);
                b.Phase = (vnew.Length() < EPS && MathF.Abs(wy) < EPS) ? BallPhase.Rest : BallPhase.Rolling;
            }
            else if (ph == BallPhase.Spinning)
            {
                float wy = b.AngularVelocity.Y * (float)Math.Exp(-spinDecay * dt);
                if (MathF.Abs(wy) < EPS) { b.AngularVelocity = Vector3.Zero; b.Phase = BallPhase.Rest; } // snap at boundary
                else { b.AngularVelocity = new Vector3(0, wy, 0); b.Phase = BallPhase.Spinning; }
            }
            else b.Phase = BallPhase.Rest;
        }

        // ---- Closed-form free-roll reference (no collisions) for validation ----
        // NOTE: Solve is the ISOTROPIC closed form (napAniso=0, napSteer=0). It is the
        // canonical reference ONLY for nap-free shots; the event Advance() adds nap as a
        // correction that does not change the slide->roll transition time. Golden tests
        // that compare against Solve must assert NapAnisotropy==0 && NapSteer==0.
        public struct Solution
        {
            public Vector3 FinalPos, FinalSpin, RollStartVel;
            public float SlideTime, RollTime, PathLength, PeakSpeed;
        }

        public static Solution Solve(Vector3 p0, Vector3 v0, Vector3 w0,
                                     float R, float g, float muS, float muR, float spinDecay)
        {
            CheckInputs(R, g, muS, muR);
            var sol = new Solution { PeakSpeed = v0.Length() };
            float vx=v0.X, vz=v0.Z, wx=w0.X, wy=w0.Y, wz=w0.Z;
            float px=p0.X, pz=p0.Z;

            Vector2 u = new Vector2(vx + R*wz, vz - R*wx); float um = u.Length();
            if (um > 1e-7f && muS > 0f)
            {
                Vector2 uh = u/um; float ts = 2f*um/(7f*muS*g); sol.SlideTime = ts;
                float vrx = vx - muS*g*ts*uh.X, vrz = vz - muS*g*ts*uh.Y;
                float k = 5f*muS*g/(2f*R);
                float wrx = wx + k*uh.Y*ts, wrz = wz - k*uh.X*ts;
                float prx = px + vx*ts - 0.5f*muS*g*ts*ts*uh.X, prz = pz + vz*ts - 0.5f*muS*g*ts*ts*uh.Y;
                sol.PathLength += MathF.Sqrt((prx-px)*(prx-px)+(prz-pz)*(prz-pz));
                // capture rolling-onset velocity BEFORE it is modified (was the RollStartVel bug)
                sol.RollStartVel = new Vector3(vrx, 0f, vrz);
                px=prx; pz=prz; vx=vrx; vz=vrz; wx=wrx; wz=wrz;
            }

            float vm = MathF.Sqrt(vx*vx+vz*vz);
            if (vm > 1e-7f && muR > 0f)
            {
                float tr = vm/(muR*g); sol.RollTime = tr; float seg = vm*tr*0.5f;
                px += vx/vm*seg; pz += vz/vm*seg; sol.PathLength += seg;
            }
            else if (sol.SlideTime == 0f) { sol.RollStartVel = new Vector3(vx,0f,vz); }

            float totalT = sol.SlideTime + sol.RollTime;
            sol.FinalPos = new Vector3(px, R, pz);
            sol.FinalSpin = new Vector3(0f, wy*(float)Math.Exp(-spinDecay*totalT), 0f); // expose residual english
            return sol;
        }
    }
}
