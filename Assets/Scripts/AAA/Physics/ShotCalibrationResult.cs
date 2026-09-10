using UnityEngine;

namespace VR147.AAA.Physics
{
    public readonly struct ShotCalibrationResult
    {
        public readonly bool passed;
        public readonly float measuredDistance;
        public readonly float error;
        public readonly float measuredPeakSpeed;
        public readonly float peakSpeedError;
        public readonly bool peakSpeedValidated;

        public ShotCalibrationResult(bool passed, float measuredDistance, float error,
            float measuredPeakSpeed = 0f, float peakSpeedError = 0f, bool peakSpeedValidated = false)
        {
            this.passed = passed;
            this.measuredDistance = measuredDistance;
            this.error = error;
            this.measuredPeakSpeed = measuredPeakSpeed;
            this.peakSpeedError = peakSpeedError;
            this.peakSpeedValidated = peakSpeedValidated;
        }
    }
}
