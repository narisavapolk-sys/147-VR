using UnityEngine;

namespace VR147.AAA.Physics
{
    public sealed class ShotCalibrationHarness : MonoBehaviour
    {
        [SerializeField] private ShotCalibrationRunner runner;
        [SerializeField] private int caseIndex;
        [SerializeField] private bool runOnStart;

        public ShotCalibrationResult LastResult { get; private set; }

        private void Start()
        {
            if (runOnStart)
                RunCurrentCase();
        }

        [ContextMenu("Run Current Calibration Case")]
        public void RunCurrentCase()
        {
            if (runner == null) return;
            LastResult = runner.EvaluateCurrent(caseIndex);
            Debug.Log($"[147VR Calibration] Case {caseIndex}: " +
                      $"{(LastResult.passed ? "PASS" : "FAIL")} " +
                      $"distance={LastResult.measuredDistance:F4} " +
                      $"error={LastResult.error:F4}", this);
        }
    }
}
