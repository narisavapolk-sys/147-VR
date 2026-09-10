using System.Collections.Generic;
using UnityEngine;

namespace VR147.AAA.Physics
{
    public sealed class CalibrationScorecardReporter : MonoBehaviour
    {
        [SerializeField] private float tolerance = 0.02f;
        [SerializeField] private float peakSpeedTolerance = 0.25f;

        public CalibrationScorecard Build(IReadOnlyList<ShotCalibrationResult> results)
            => new(results, tolerance, peakSpeedTolerance);

        public void Log(IReadOnlyList<ShotCalibrationResult> results, string shotType)
        {
            CalibrationScorecard score = Build(results);
            Debug.Log($"[147VR AAA] {shotType}: " +
                      $"{(score.passed ? "PASS" : "FAIL")} " +
                      $"samples={score.samples}, mean={score.meanError:F4}, " +
                      $"range={score.minError:F4}-{score.maxError:F4}, " +
                      $"std={score.standardDeviation:F4}, " +
                      $"consistency={score.consistencyScore:P0}, " +
                      $"peakMeanError={score.meanPeakSpeedError:F4}, " +
                      $"peakConsistency={score.peakSpeedConsistencyScore:P0}, " +
                      $"peakValidated={score.peakSpeedValidatedSamples}, " +
                      $"peakInvalid={score.peakSpeedInvalidSamples}, " +
                      $"invalid={score.invalidSamples}", this);
        }
    }
}
