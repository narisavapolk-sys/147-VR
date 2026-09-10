using UnityEngine;

namespace VR147.AAA.Cue
{
    public static class CueStrikeSolver
    {
        public static CueStrikeResult Solve(
            CueImpactProfile profile,
            Vector3 aimDirection,
            Vector2 english,
            float power,
            float ballRadius)
        {
            Vector3 forward = aimDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.000001f)
                return default;
            forward.Normalize();

            float p = Mathf.Clamp01(power);
            float radius = Mathf.Max(0.001f, ballRadius);
            Vector3 impulse = CueImpactSolver.SolveImpulse(profile, english, p, forward);
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 contactOffset = right * Mathf.Clamp(english.x, -1f, 1f) * radius * 0.85f;
            contactOffset += Vector3.up * Mathf.Clamp(english.y, -1f, 1f) * radius * 0.85f;
            Vector3 angularImpulse = Vector3.Cross(contactOffset, impulse);

            return new CueStrikeResult(impulse, angularImpulse, contactOffset);
        }
    }
}
