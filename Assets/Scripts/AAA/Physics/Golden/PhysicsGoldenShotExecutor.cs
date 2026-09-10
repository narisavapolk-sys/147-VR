using UnityEngine;
using VR147.AAA.Cue;

namespace VR147.AAA.Physics.Golden
{
    public sealed class PhysicsGoldenShotExecutor : MonoBehaviour // AAA compile gate
    {
        [SerializeField] private Rigidbody cueBall;
        [SerializeField] private Transform shotOrigin;
        [SerializeField] private ShotMeasurementTracker measurementTracker;
        [SerializeField] private PhysicsGoldenRegressionRunner regressionRunner;
        [SerializeField] private CueStrokeModel strokeModel;
        [SerializeField] private CuePhysicsAdapter physicsAdapter;
        private PhysicsGoldenCase activeCase;
        private bool executing;

        public bool IsExecuting => executing;
        public PhysicsGoldenCase ActiveCase => activeCase;

        public bool BeginShot(PhysicsGoldenCase goldenCase)
        {
            if (executing || cueBall == null || goldenCase == null || !goldenCase.IsValid()) return false;
            if (measurementTracker == null || regressionRunner == null || strokeModel == null || physicsAdapter == null) return false;
            physicsAdapter.Configure(cueBall);
            activeCase = goldenCase;
            executing = true;
            cueBall.angularVelocity = Vector3.zero;
            cueBall.linearVelocity = Vector3.zero;
            measurementTracker.Begin();
            Vector3 direction = shotOrigin != null ? shotOrigin.forward : transform.forward;
            float power = Mathf.Clamp01(goldenCase.CalibrationCase.power);
            if (!CueShotValidator.TryCreate(
                    cueBall.position,
                    cueBall.position + direction,
                    power,
                    strokeModel,
                    out CueShotData shot))
            {
                executing = false;
                return false;
            }

            if (!physicsAdapter.Apply(shot, new Vector2(goldenCase.CalibrationCase.side, goldenCase.CalibrationCase.vertical)))
            {
                executing = false;
                return false;
            }
            return true;
        }

        private void FixedUpdate()
        {
            if (!executing || measurementTracker == null) return;
            if (!measurementTracker.TryCapture()) return;
            regressionRunner.RecordMeasurement(
                activeCase,
                measurementTracker.Distance,
                measurementTracker.PeakSpeed);
            executing = false;
            activeCase = null;
        }

        public void Cancel()
        {
            executing = false;
            activeCase = null;
        }

        public bool TryRun(PhysicsGoldenCase goldenCase)
        {
            return BeginShot(goldenCase);
        }
    }
}

