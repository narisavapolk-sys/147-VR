using UnityEngine;

namespace VR147.AAA.Physics
{
    public static class ShotCalibrationEvaluator
    {
        public static ShotCalibrationResult Evaluate(
            ShotCalibrationCase calibrationCase,
            float measuredDistance,
            float measuredPeakSpeed = 0f)
        {
            if (calibrationCase == null)
                return new ShotCalibrationResult(false, measuredDistance,
                    float.PositiveInfinity, measuredPeakSpeed, float.PositiveInfinity, false);

            if (!IsValidCase(calibrationCase) ||
                float.IsNaN(measuredDistance) || float.IsInfinity(measuredDistance) ||
                float.IsNaN(measuredPeakSpeed) || float.IsInfinity(measuredPeakSpeed))
            {
                return new ShotCalibrationResult(false, measuredDistance,
                    float.PositiveInfinity, measuredPeakSpeed, float.PositiveInfinity,
                    calibrationCase != null && calibrationCase.validatePeakSpeed);
            }

            float distanceError = Mathf.Abs(measuredDistance - calibrationCase.expectedDistance);
            bool distancePassed = distanceError <= Mathf.Max(0f, calibrationCase.tolerance);

            float peakSpeedError = 0f;
            bool peakSpeedPassed = true;
            if (calibrationCase.validatePeakSpeed)
            {
                peakSpeedError = Mathf.Abs(measuredPeakSpeed - calibrationCase.expectedPeakSpeed);
                peakSpeedPassed = peakSpeedError <= Mathf.Max(0f, calibrationCase.peakSpeedTolerance);
            }

            return new ShotCalibrationResult(
                distancePassed && peakSpeedPassed,
                measuredDistance,
                distanceError,
                measuredPeakSpeed,
                peakSpeedError,
                calibrationCase.validatePeakSpeed);
        }

        public static bool IsValidCase(ShotCalibrationCase calibrationCase)
        {
            return calibrationCase != null &&
                   calibrationCase.expectedDistance > 0f &&
                   calibrationCase.tolerance >= 0f &&
                   calibrationCase.power >= 0f &&
                   (!calibrationCase.validatePeakSpeed ||
                    (calibrationCase.expectedPeakSpeed >= 0f && calibrationCase.peakSpeedTolerance >= 0f));
        }
    }
}
