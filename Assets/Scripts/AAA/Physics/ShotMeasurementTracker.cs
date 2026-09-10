using UnityEngine;

namespace VR147.AAA.Physics
{
    public sealed class ShotMeasurementTracker : MonoBehaviour
    {
        [SerializeField] private Rigidbody targetBall;
        [SerializeField] private Transform measurementOrigin;
        [SerializeField, Min(0.001f)] private float settleSpeed = 0.025f;
        [SerializeField, Min(0.01f)] private float settleDuration = 0.15f;
        private float settledFor;
        private float peakSpeed;
        private bool measuring;
        private Vector3 startPosition;
        private int debugFrames;

        public bool IsMeasuring => measuring;
        public bool IsSettled => measuring && settledFor >= settleDuration;
        public bool HasMeasurement { get; private set; }
        public float PeakSpeed => peakSpeed;
        public float Distance { get; private set; }

        public void ConfigureTarget(Rigidbody target)
        {
            targetBall = target;
        }

        public void Begin()
        {
            if (targetBall == null) return;
            measuring = true;
            settledFor = 0f;
            peakSpeed = 0f;
            Distance = 0f;
            HasMeasurement = false;
            debugFrames = 0;
            startPosition = measurementOrigin != null
                ? measurementOrigin.position
                : targetBall.position;
        }

        public bool TryCapture()
        {
            if (!measuring || targetBall == null) return false;
            float speed = targetBall.linearVelocity.magnitude;
            debugFrames++;
            if ((debugFrames % 600) == 0)
                Debug.Log($"[147VR Calibration] Tracker speed={speed:F5} angular={targetBall.angularVelocity.magnitude:F5} pos={targetBall.position} settledFor={settledFor:F3}", this);
            if (float.IsNaN(speed) || float.IsInfinity(speed))
            {
                measuring = false;
                HasMeasurement = false;
                Distance = 0f;
                peakSpeed = 0f;
                return false;
            }

            peakSpeed = Mathf.Max(peakSpeed, speed);
            settledFor = speed <= settleSpeed ? settledFor + Time.fixedDeltaTime : 0f;
            if (!IsSettled) return false;
            Vector3 origin = startPosition;
            Vector3 delta = targetBall.position - origin;
            Distance = new Vector2(delta.x, delta.z).magnitude;
            if (float.IsNaN(Distance) || float.IsInfinity(Distance))
            {
                measuring = false;
                HasMeasurement = false;
                Distance = 0f;
                peakSpeed = 0f;
                return false;
            }

            measuring = false;
            HasMeasurement = true;
            return true;
        }
    }
}

