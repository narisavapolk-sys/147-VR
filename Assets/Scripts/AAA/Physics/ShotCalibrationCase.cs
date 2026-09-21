using UnityEngine;

namespace VR147.AAA.Physics
{
    public enum ShotCalibrationType
    {
        Straight,
        Stun,
        Follow,
        Draw,
        LeftEnglish,
        RightEnglish,
        Cushion,
        Pocket
    }

    [CreateAssetMenu(menuName = "147VR/Physics/Shot Calibration Case")]
    public sealed class ShotCalibrationCase : ScriptableObject
    {
        public ShotCalibrationType type;
        [Min(0f)] public float power = 0.5f;
        [Range(-1f, 1f)] public float side;
        [Range(-1f, 1f)] public float vertical;
        [Min(0f)] public float expectedDistance = 1f;
        [Min(0f)] public float tolerance = 0.05f;
        [Min(0f)] public float expectedPeakSpeed;
        [Min(0f)] public float peakSpeedTolerance = 0.25f;
        public bool validatePeakSpeed;
    }
}
