using UnityEngine;

namespace VR147.AAA.Physics
{
    [RequireComponent(typeof(Collider))]
    public sealed class PocketResponder : MonoBehaviour
    {
        [SerializeField] private PocketProfile profile;
        [SerializeField] private Transform capturePoint;
        [SerializeField] private float captureDuration = 0.12f;

        private Rigidbody capturedBall;
        public PocketCaptureState State { get; private set; }

        private void OnTriggerStay(Collider other)
        {
            Rigidbody ball = other.attachedRigidbody;
            if (ball == null || ball != capturedBall && State == PocketCaptureState.Captured)
                return;
            if (profile == null || capturePoint == null) return;

            PocketCaptureResult result = PocketSolver.Evaluate(
                ball.position, ball.linearVelocity, capturePoint.position, profile);
            if (!result.captured) return;

            capturedBall = ball;
            State = PocketCaptureState.Capturing;
            Vector3 toTarget = result.target - ball.position;
            if (toTarget.sqrMagnitude <= 0.000001f)
            {
                ball.linearVelocity = Vector3.zero;
                ball.angularVelocity = Vector3.zero;
                State = PocketCaptureState.Captured;
                return;
            }

            float t = result.strength * profile.rollInAssist * Time.fixedDeltaTime /
                      Mathf.Max(captureDuration, 0.001f);
            float speed = new Vector2(ball.linearVelocity.x, ball.linearVelocity.z).magnitude;
            Vector3 targetVelocity = toTarget.normalized * Mathf.Max(speed, 0.15f);
            ball.linearVelocity = Vector3.Lerp(ball.linearVelocity, targetVelocity, Mathf.Clamp01(t));
        }
    }
}
