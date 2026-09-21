using UnityEngine;

namespace VR147.AAA.Physics
{
    public readonly struct BallMotionState
    {
        public readonly BallRollingState rollingState;
        public readonly Vector3 velocity;
        public readonly Vector3 angularVelocity;
        public readonly float slipSpeed;

        public BallMotionState(BallRollingState state, Vector3 velocity,
            Vector3 angularVelocity, float slipSpeed)
        {
            rollingState = state;
            this.velocity = velocity;
            this.angularVelocity = angularVelocity;
            this.slipSpeed = slipSpeed;
        }
    }
}
