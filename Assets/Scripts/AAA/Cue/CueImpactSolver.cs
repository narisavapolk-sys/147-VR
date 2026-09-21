using UnityEngine;

namespace VR147.AAA.Cue
{
    /// <summary>Converts cue-tip offset into a controlled strike description.</summary>
    public static class CueImpactSolver
    {
        public static Vector3 SolveImpulse(
            CueImpactProfile profile,
            Vector2 english,
            float power,
            Vector3 aimDirection)
        {
            if (profile == null)
                return aimDirection.normalized * Mathf.Clamp01(power);

            Vector3 forward = aimDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.000001f)
                return Vector3.zero;
            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 local = profile.EvaluateLocalImpulse(english, power);
            return forward * local.z + right * local.x + Vector3.up * local.y;
        }
    }
}
