using System.Collections.Generic;
using UnityEngine;

namespace VR147.AAA.Physics
{
    public sealed class ShotCalibrationReport : MonoBehaviour
    {
        [SerializeField] private ShotCalibrationCase[] cases;
        [SerializeField] private ShotCalibrationRunner runner;
        [SerializeField] private bool logResults = true;

        private readonly List<ShotCalibrationResult> results = new();

        public IReadOnlyList<ShotCalibrationResult> Results => results;

        [ContextMenu("Evaluate All Cases")]
        public void EvaluateAllCases()
        {
            results.Clear();
            if (runner == null || cases == null) return;

            for (int i = 0; i < cases.Length; i++)
            {
                ShotCalibrationResult result = runner.EvaluateCurrent(i);
                results.Add(result);
                if (logResults)
                    Debug.Log($"[147VR Calibration] {cases[i].type}: " +
                              $"{(result.passed ? "PASS" : "FAIL")} " +
                              $"distance={result.measuredDistance:F4}, error={result.error:F4}", this);
            }
        }
    }
}
