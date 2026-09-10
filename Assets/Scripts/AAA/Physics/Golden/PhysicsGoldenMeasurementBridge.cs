using UnityEngine;

namespace VR147.AAA.Physics.Golden
{
    public sealed class PhysicsGoldenMeasurementBridge : MonoBehaviour
    {
        [SerializeField] private PhysicsGoldenRegressionRunner runner;
        [SerializeField] private PhysicsGoldenCase goldenCase;
        [SerializeField] private ShotMeasurementTracker measurementTracker;

        public bool Capture()
        {
            if (runner == null || goldenCase == null || measurementTracker == null)
                return false;
            if (!measurementTracker.HasMeasurement)
                return false;

            return runner.RecordMeasurement(
                goldenCase,
                measurementTracker.Distance,
                measurementTracker.PeakSpeed);
        }

        [ContextMenu("Capture Current Measurement")]
        private void CaptureFromContextMenu()
        {
            if (!Capture())
                Debug.LogWarning("[147VR Golden] Measurement capture failed.", this);
        }
    }
}
