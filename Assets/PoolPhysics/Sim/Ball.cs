using System.Numerics;

namespace PoolPhysics.Sim
{
    public enum BallPhase { Rest, Sliding, Rolling, Spinning }

    /// <summary>
    /// Pure-C# billiard ball state. NO UnityEngine dependency so the whole
    /// simulator can run headless (edit-mode tests / CI / the GOLDEN regression
    /// harness) and be rendered by a thin Unity bridge.
    /// Table plane = XZ, world up = +Y. Ball rests at Y = Radius.
    /// </summary>
    public struct Ball
    {
        public Vector3 Position;      // world position (Y kept at Radius while grounded)
        public Vector3 Velocity;      // linear velocity, m/s (planar; Y used only for jump/flight)
        public Vector3 AngularVelocity; // spin, rad/s (Y component = "english"/side spin)
        public float   Radius;        // m
        public float   Mass;          // kg
        public bool    Pocketed;
        public bool    Active;        // false = removed from sim (e.g., cue ball respot pending)
        public BallPhase Phase;       // motion phase for the event-based integrator

        public readonly bool IsMoving => (Velocity.LengthSquared() > 1e-8f)
                                      || (AngularVelocity.LengthSquared() > 1e-6f);

        /// <summary>Velocity of the point touching the cloth (drives slide vs roll).</summary>
        public readonly Vector3 ContactPointVelocity()
        {
            // r from centre to contact = (0,-R,0); v_contact = v + w x r
            var r = new Vector3(0f, -Radius, 0f);
            return Velocity + Vector3.Cross(AngularVelocity, r);
        }
    }
}
