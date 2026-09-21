using UnityEngine;

namespace VR147.AAA.Physics
{
    public static class PocketSolver
    {
        public static PocketCaptureResult Evaluate(
            Vector3 ballPosition, Vector3 velocity, Vector3 pocketPosition,
            PocketProfile profile)
        {
            if (profile == null) return new PocketCaptureResult(false, pocketPosition, 0f);

            Vector3 delta = pocketPosition - ballPosition;
            float horizontalDistance = new Vector2(delta.x, delta.z).magnitude;
            float depth = Mathf.Abs(delta.y);
            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

            if (horizontalDistance > profile.captureRadius ||
                depth > profile.captureDepth ||
                horizontalSpeed > profile.captureSpeed)
                return new PocketCaptureResult(false, pocketPosition, 0f);

            float distanceFactor = 1f - Mathf.Clamp01(
                horizontalDistance / Mathf.Max(profile.captureRadius, 0.0001f));
            float speedFactor = 1f - Mathf.Clamp01(
                horizontalSpeed / Mathf.Max(profile.captureSpeed, 0.0001f));
            float strength = Mathf.Clamp01(distanceFactor * 0.7f + speedFactor * 0.3f);
            return new PocketCaptureResult(true, pocketPosition, strength);
        }
    }
}
