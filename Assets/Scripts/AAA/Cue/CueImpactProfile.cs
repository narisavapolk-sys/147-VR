using UnityEngine;

namespace VR147.AAA.Cue
{
    [CreateAssetMenu(menuName = "147VR/Cue/Impact Profile")]
    public sealed class CueImpactProfile : ScriptableObject
    {
        [Header("Cue Ball")]
        [Min(0f)] public float maxSpinImpulse = 1.8f;
        [Min(0f)] public float maxSideImpulse = 1.2f;
        [Range(0f, 1f)] public float verticalSpinLimit = 0.85f;

        [Header("Contact")]
        [Min(0.001f)] public float ballRadius = 0.02625f;
        [Min(0f)] public float minContactOffset = 0.0005f;
        [Range(0f, 1f)] public float strikeEfficiency = 0.98f;

        public Vector3 EvaluateLocalImpulse(Vector2 english, float power)
        {
            float p = Mathf.Clamp01(power);
            float x = Mathf.Clamp(english.x, -1f, 1f);
            float y = Mathf.Clamp(english.y, -1f, 1f) * verticalSpinLimit;
            return new Vector3(
                x * maxSideImpulse * p,
                y * maxSpinImpulse * p,
                p * strikeEfficiency);
        }
    }
}
