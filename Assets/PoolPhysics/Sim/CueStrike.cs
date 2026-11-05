using System;
using System.Numerics;

namespace PoolPhysics.Sim
{
    /// <summary>
    /// Converts a player's shot intent into cue-ball initial conditions.
    /// direction = planar aim (normalized); speed = m/s; tipOffset = contact
    /// point on the cue ball in ball-local units [-1,1]:
    ///   x = side english (right +), y = vertical (above center = follow +, below = draw/screw).
    /// Uses the recovered spin gains (0.8 tangential / 0.4 vertical) and the
    /// elevation-dependent smoothstep 3t^2-2t^3 draw/screw response.
    /// </summary>
    public static class CueStrike
    {
        public static void Apply(ref Ball cue, Vector3 direction, float speed,
                                 Vector2 tipOffset, PhysicsProfile p, float cueElevation = 0f)
        {
            Vector3 dir = direction; dir.Y = 0;
            if (dir.LengthSquared() < 1e-12f) return;         // zero/NaN aim -> no shot (guard)
            dir = Vector3.Normalize(dir);
            if (!float.IsFinite(speed) || speed <= 0f) return; // invalid speed -> no shot (guard)
            cue.Velocity = dir * speed;
            cue.Position.Y = cue.Radius;

            // english (side spin) about vertical axis
            float R = cue.Radius;
            float side = Math.Clamp(tipOffset.X, -1f, 1f);
            float vert = Math.Clamp(tipOffset.Y, -1f, 1f);

            // smoothstep on |vert| for draw/screw authority (elevation-dependent)
            float t = MathF.Abs(vert);
            float ss = t * t * (3f - 2f * t);
            float elev = Math.Clamp(cueElevation, 0f, 1f);
            float followDraw = vert * ss * (1f - 0.5f * elev);

            // horizontal spin axis perpendicular to travel (topspin/backspin)
            Vector3 horizontalAxis = Vector3.Cross(new Vector3(0, 1, 0), dir); // points left of travel
            Vector3 followSpin = horizontalAxis * (followDraw * speed * p.SpinGainTangential / R);
            // vertical spin (english): right english = +Y (tune sign to your convention)
            Vector3 english = new Vector3(0, side * speed * p.SpinGainVertical / R, 0);

            cue.AngularVelocity = followSpin + english;
        }
    }
}
