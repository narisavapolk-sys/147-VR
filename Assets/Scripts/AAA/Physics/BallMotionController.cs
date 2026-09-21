using UnityEngine;

namespace VR147.AAA.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BallMotionController : MonoBehaviour
    {
        [SerializeField] private Rigidbody ball;
        [SerializeField] private BallMotionProfile profile;
        [SerializeField] private float radius = 0.02625f;

        public BallMotionState CurrentState { get; private set; }

        private void Reset() => ball = GetComponent<Rigidbody>();

        private void FixedUpdate()
        {
            if (ball == null || profile == null) return;
            CurrentState = BallMotionEvaluator.Evaluate(
                ball.linearVelocity, ball.angularVelocity, radius, profile);
            ApplyClothResponse(Time.fixedDeltaTime);
            ApplyRollingCorrection(Time.fixedDeltaTime);
            SettleIfNeeded();
        }

        private void ApplyClothResponse(float dt)
        {
            Vector3 planar = new Vector3(ball.linearVelocity.x, 0f, ball.linearVelocity.z);
            if (planar.sqrMagnitude < 0.000001f) return;
            float friction = CurrentState.rollingState == BallRollingState.Sliding
                ? profile.slidingFriction : profile.rollingFriction;
            ball.AddForce(-planar.normalized * friction * ball.mass, ForceMode.Force);
        }

        private void ApplyRollingCorrection(float dt)
        {
            if (CurrentState.rollingState != BallRollingState.Rolling) return;
            Vector3 planar = new Vector3(ball.linearVelocity.x, 0f, ball.linearVelocity.z);
            if (planar.sqrMagnitude < 0.000001f) return;
            Vector3 targetSpin = Vector3.Cross(Vector3.up, planar) / radius;
            Vector3 delta = targetSpin - ball.angularVelocity;
            ball.AddTorque(delta * profile.maxSpinCorrection * ball.mass * dt,
                ForceMode.Impulse);
        }

        private void SettleIfNeeded()
        {
            if (CurrentState.rollingState != BallRollingState.Resting) return;
            Vector3 v = ball.linearVelocity;
            ball.linearVelocity = new Vector3(0f, v.y, 0f);
            ball.angularVelocity = Vector3.zero;
        }
    }
}
