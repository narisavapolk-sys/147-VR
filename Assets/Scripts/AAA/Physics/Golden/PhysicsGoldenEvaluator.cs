using UnityEngine;

namespace VR147.AAA.Physics.Golden
{
    public static class PhysicsGoldenEvaluator
    {
        public static PhysicsGoldenResult Evaluate(
            PhysicsGoldenCase goldenCase,
            float measuredDistance,
            float measuredPeakSpeed = 0f)
        {
            if (goldenCase == null || !goldenCase.IsValid())
                return new PhysicsGoldenResult(null,
                    new ShotCalibrationResult(false, measuredDistance,
                        float.PositiveInfinity, measuredPeakSpeed, float.PositiveInfinity));

            float distanceTolerance = goldenCase.MeasuredDistanceMean *
                (goldenCase.RegressionTolerancePercent / 100f);
            float peakSpeedTolerance = goldenCase.MeasuredPeakSpeedMean *
                (goldenCase.RegressionTolerancePercent / 100f);
            if (peakSpeedTolerance <= 0f) peakSpeedTolerance = 0.001f;

            float distanceError = Mathf.Abs(measuredDistance - goldenCase.MeasuredDistanceMean);
            float peakSpeedError = Mathf.Abs(measuredPeakSpeed - goldenCase.MeasuredPeakSpeedMean);
            bool distancePassed = distanceError <= distanceTolerance;
            bool peakSpeedPassed = peakSpeedError <= peakSpeedTolerance;

            var calibration = new ShotCalibrationResult(
                distancePassed && peakSpeedPassed,
                measuredDistance,
                distanceError,
                measuredPeakSpeed,
                peakSpeedError,
                true);
            return new PhysicsGoldenResult(goldenCase, calibration);
        }
    }
}