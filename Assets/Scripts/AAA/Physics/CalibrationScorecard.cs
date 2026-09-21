using System.Collections.Generic;
using UnityEngine;

namespace VR147.AAA.Physics
{
    public readonly struct CalibrationScorecard
    {
        public readonly int samples;
        public readonly float meanError;
        public readonly float minError;
        public readonly float maxError;
        public readonly float standardDeviation;
        public readonly float consistencyScore;
        public readonly float meanPeakSpeedError;
        public readonly float peakSpeedConsistencyScore;
        public readonly int invalidSamples;
        public readonly int peakSpeedValidatedSamples;
        public readonly int peakSpeedInvalidSamples;
        public readonly bool passed;

        public CalibrationScorecard(IReadOnlyList<ShotCalibrationResult> results, float tolerance,
            float peakSpeedTolerance = -1f)
        {
            var stats = new CalibrationStatistics(results);
            samples = stats.sampleCount;
            meanError = stats.meanError;
            minError = stats.minError;
            maxError = stats.maxError;
            standardDeviation = stats.standardDeviation;
            consistencyScore = samples == 0 || float.IsInfinity(standardDeviation) ? 0f :
                Mathf.Clamp01(1f - standardDeviation / Mathf.Max(tolerance, 0.0001f));
            meanPeakSpeedError = stats.meanPeakSpeedError;
            float peakTolerance = peakSpeedTolerance < 0f ? tolerance : peakSpeedTolerance;
            peakSpeedConsistencyScore = samples == 0 || float.IsInfinity(stats.peakSpeedStandardDeviation) ? 0f :
                Mathf.Clamp01(1f - stats.peakSpeedStandardDeviation / Mathf.Max(peakTolerance, 0.0001f));
            invalidSamples = stats.invalidSamples;
            peakSpeedValidatedSamples = stats.peakSpeedValidatedSamples;
            peakSpeedInvalidSamples = stats.peakSpeedInvalidSamples;
            float effectivePeakTolerance = Mathf.Max(peakTolerance, 0.0001f);
            bool distancePassed = meanError <= Mathf.Max(tolerance, 0.0001f);
            bool peakSpeedRequired = peakSpeedValidatedSamples > 0;
            bool peakSpeedPassed = !peakSpeedRequired ||
                (peakSpeedInvalidSamples == 0 && meanPeakSpeedError <= effectivePeakTolerance);
            passed = samples > 0 && invalidSamples == 0 &&
                     distancePassed && peakSpeedPassed;
        }
    }
}
