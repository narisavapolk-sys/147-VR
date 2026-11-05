using UnityEngine;

namespace VR147.AAA.Physics
{
    public static class BallMotionEvaluator
    {
        public static BallMotionState Evaluate(
            Vector3 velocity, Vector3 angularVelocity, float radius,
            BallMotionProfile profile)
        {
            if (profile == null || radius <= 0f)
                return new BallMotionState(BallRollingState.Resting, velocity,
                    angularVelocity, 0f);

            Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 contactVelocity = Vector3.Cross(angularVelocity,
                Vector3.up * radius);
            float slip = (planarVelocity - contactVelocity).magnitude;

            BallRollingState state;
            if (planarVelocity.magnitude <= profile.settleSpeed &&
                angularVelocity.magnitude <= profile.settleSpin)
                state = BallRollingState.Resting;
            else if (slip > profile.rollSlipTolerance)
                state = BallRollingState.Sliding;
            else
                state = BallRollingState.Rolling;

            return new BallMotionState(state, velocity, angularVelocity, slip);
        }
    }
}
