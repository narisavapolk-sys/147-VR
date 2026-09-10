using System;
using System.Collections.Generic;
using UnityEngine;

namespace VR147.AAA.Physics
{
    [Serializable]
    public struct CalibrationSample
    {
        public ShotCalibrationType type;
        public float targetDistance;
        public float measuredDistance;
        public float error;
        public bool passed;
    }

    public sealed class PhysicsCalibrationSession : MonoBehaviour
    {
        [SerializeField] private ShotCalibrationCase[] cases;
        [SerializeField] private ShotCalibrationRunner runner;
        [SerializeField] private bool logEachSample = true;

        [SerializeField] private List<CalibrationSample> samples = new();

        public IReadOnlyList<CalibrationSample> Samples => samples;

        [ContextMenu("Capture All Current Samples")]
        public void CaptureAllCurrentSamples()
        {
            samples.Clear();
            if (runner == null || cases == null) return;

            foreach (ShotCalibrationCase calibrationCase in cases)
            {
                if (calibrationCase == null) continue;

                ShotCalibrationResult result = runner.EvaluateCurrent(
                    Array.IndexOf(cases, calibrationCase));

                samples.Add(new CalibrationSample
                {
                    type = calibrationCase.type,
                    targetDistance = calibrationCase.expectedDistance,
                    measuredDistance = result.measuredDistance,
                    error = result.error,
                    passed = result.passed
                });

                if (logEachSample)
                    Debug.Log($"[147VR Calibration] {calibrationCase.type}: " +
                              $"{(result.passed ? "PASS" : "FAIL")} | " +
                              $"target={calibrationCase.expectedDistance:F4} | " +
                              $"measured={result.measuredDistance:F4} | " +
                              $"error={result.error:F4}", this);
            }
        }
    }
}
