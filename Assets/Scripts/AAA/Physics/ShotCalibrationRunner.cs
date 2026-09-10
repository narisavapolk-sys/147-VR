using UnityEngine;

namespace VR147.AAA.Physics
{
    public sealed class ShotCalibrationRunner : MonoBehaviour
    {
        [SerializeField] private ShotCalibrationCase[] cases;
        [SerializeField] private Rigidbody targetBall;
        [SerializeField] private Transform measurementOrigin;
        [SerializeField] private ShotMeasurementTracker measurementTracker;

        public ShotCalibrationResult EvaluateCurrent(int index)
        {
            if (targetBall == null || cases == null || index < 0 || index >= cases.Length ||
                !ShotCalibrationEvaluator.IsValidCase(cases[index]))
            {
                return new ShotCalibrationResult(false, float.NaN, float.PositiveInfinity);
            }

            if (measurementTracker != null)
            {
                if (!measurementTracker.HasMeasurement)
                {
                    return new ShotCalibrationResult(
                        false,
                        float.NaN,
                        float.PositiveInfinity,
                        float.NaN,
                        float.PositiveInfinity,
                        cases[index].validatePeakSpeed);
                }

                return ShotCalibrationEvaluator.Evaluate(
                    cases[index],
                    measurementTracker.Distance,
                    measurementTracker.PeakSpeed);
            }

            if (cases[index].validatePeakSpeed)
            {
                return new ShotCalibrationResult(
                    false,
                    float.NaN,
                    float.PositiveInfinity,
                    float.NaN,
                    float.PositiveInfinity,
                    true);
            }

            Vector3 origin = measurementOrigin != null
                ? measurementOrigin.position : transform.position;
            Vector3 delta = targetBall.position - origin;
            float measuredDistance = new Vector2(delta.x, delta.z).magnitude;
            return ShotCalibrationEvaluator.Evaluate(cases[index], measuredDistance);
        }
    }
}
