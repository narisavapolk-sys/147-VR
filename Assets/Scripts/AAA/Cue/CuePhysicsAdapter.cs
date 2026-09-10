using UnityEngine;

namespace VR147.AAA.Cue
{
    /// <summary>
    /// Single runtime physics authority for cue strikes.
    /// Converts a validated shot description into a mass-correct impulse and optional spin torque.
    /// </summary>
    public sealed class CuePhysicsAdapter : MonoBehaviour
    {
        [SerializeField] private Rigidbody cueBall;
        [SerializeField] private CueImpactProfile impactProfile;
        [SerializeField] private float ballRadius = 0.02625f;
        [SerializeField] private float maxLinearImpulse = 10f;
        [SerializeField] private float maxAngularImpulse = 250f;

        public Rigidbody CueBall => cueBall;
        public bool IsConfigured => cueBall != null;

        public void Configure(Rigidbody target)
        {
            cueBall = target;
        }

        /// <summary>
        /// Applies the validated shot. This is the only method used by gameplay/calibration
        /// paths to mutate cue-ball linear/angular velocity through physics.
        /// </summary>
        public bool Apply(CueShotData shot, Vector2 english)
        {
            if (cueBall == null || !shot.IsValid || cueBall.isKinematic)
            {
                Debug.LogError($"[CuePhysicsAdapter] Apply rejected: cueBallNull={cueBall == null} cueBallName={(cueBall != null ? cueBall.name : string.Empty)} isKinematic={(cueBall != null && cueBall.isKinematic)} shotValid={shot.IsValid} speed={shot.speed:F6} power={shot.power01:F6} mass={(cueBall != null ? cueBall.mass : 0f):F6}");
                return false;
            }

            Vector3 linearImpulse = cueBall.mass * shot.speed * shot.direction;
            Vector3 right = Vector3.Cross(Vector3.up, shot.direction).normalized;
            float radius = Mathf.Max(0.001f, ballRadius);
            Vector3 contactOffset = right * Mathf.Clamp(english.x, -1f, 1f) * radius * 0.85f;
            contactOffset += Vector3.up * Mathf.Clamp(english.y, -1f, 1f) * radius * 0.85f;

            // Straight shots produce zero torque. English produces a physically derived
            // angular impulse around the contact offset; no second velocity writer exists.
            Vector3 angularImpulse = Vector3.Cross(contactOffset, linearImpulse);
            CueStrikeResult result = new CueStrikeResult(linearImpulse, angularImpulse, contactOffset);

            if (!CueStrikeDiagnostics.IsPhysicallyPlausible(
                    result, maxLinearImpulse, maxAngularImpulse))
                return false;

            cueBall.AddForce(result.linearImpulse, ForceMode.Impulse);
            if (result.angularImpulse.sqrMagnitude > 0.0000001f)
                cueBall.AddTorque(result.angularImpulse, ForceMode.Impulse);
            return true;
        }

        /// <summary>Legacy solver entry point retained for isolated AAA tests only.</summary>
        public bool Apply(Vector3 aimDirection, Vector2 english, float power)
        {
            if (cueBall == null)
                return false;

            CueStrikeResult result = CueStrikeSolver.Solve(
                impactProfile, aimDirection, english, power, ballRadius);
            if (!CueStrikeDiagnostics.IsPhysicallyPlausible(
                    result, maxLinearImpulse, maxAngularImpulse))
                return false;

            cueBall.AddForce(result.linearImpulse, ForceMode.Impulse);
            if (result.angularImpulse.sqrMagnitude > 0.0000001f)
                cueBall.AddTorque(result.angularImpulse, ForceMode.Impulse);
            return true;
        }
    }
}
