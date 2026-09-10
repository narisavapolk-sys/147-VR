using UnityEngine;

namespace VR147.AAA.Cue
{
    [CreateAssetMenu(menuName = "147VR/Cue/Aim Profile")]
    public sealed class CueAimProfile : ScriptableObject
    {
        [Header("Stroke")]
        [Min(0.1f)] public float maxShotSpeed = 8f;
        [Min(0.01f)] public float minShotSpeed = 0.15f;
        [Min(0.01f)] public float chargeSeconds = 0.85f;
        [Min(0f)] public float deadZone = 0.02f;

        [Header("Presentation")]
        [Min(0f)] public float tipGap = 0.012f;
        [Min(0f)] public float maxPullback = 0.18f;
        [Min(0f)] public float cueVisualLength = 1.45f;

        public float EvaluateSpeed(float normalizedPower)
        {
            float p = Mathf.Clamp01(normalizedPower);
            p = Mathf.SmoothStep(0f, 1f, p);
            if (p <= deadZone)
                return 0f;
            return Mathf.Lerp(minShotSpeed, maxShotSpeed, p);
        }
    }
}
