using UnityEngine;

namespace VR147.AAA.Cue
{
    /// <summary>Pure stroke tuning. No XR/input/physics dependency.</summary>
    // Compile gate: keep this model in the shared AAA Cue namespace.
    [System.Serializable]
    public sealed class CueStrokeModel
    {
        [SerializeField] private CueAimProfile profile;
        private float _charge;

        public float Charge01 => _charge;
        public bool IsCharging => _charge > 0f;

        public void SetProfile(CueAimProfile value)
        {
            profile = value;
            Reset();
        }

        public void Begin()
        {
            _charge = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (profile == null || profile.chargeSeconds <= 0f)
                return;
            _charge = Mathf.Clamp01(_charge + deltaTime / profile.chargeSeconds);
        }

        public float Release()
        {
            float power = _charge;
            _charge = 0f;
            return power;
        }

        public void Reset()
        {
            _charge = 0f;
        }

        public float EvaluateSpeed(float power)
        {
            return profile != null ? profile.EvaluateSpeed(power) : 0f;
        }
    }
}
