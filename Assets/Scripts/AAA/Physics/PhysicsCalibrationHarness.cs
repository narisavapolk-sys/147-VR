using System.Collections.Generic;
using UnityEngine;

namespace VR147.AAA.Physics
{
    public sealed class PhysicsCalibrationHarness : MonoBehaviour
    {
        [SerializeField] private SnookerCueController cue;
        [SerializeField] private Rigidbody cueBall;
        [SerializeField] private float settleSpeed = 0.025f;
        [SerializeField] private float settleSeconds = 0.15f;
        [SerializeField] private float shotPower = 0.5f;
        [SerializeField] private float targetDistance = 0.75f;
        [SerializeField] private bool autoRun;
        private readonly List<CalibrationSample> samples = new();
        private float settledFor;
        private float peakSpeed;
        private Vector3 startPosition;
        private bool measuring;

        public IReadOnlyList<CalibrationSample> Samples => samples;

        private void Start()
        {
            if (cue == null) cue = FindFirstObjectByType<SnookerCueController>();
            if (cueBall == null)
            {
                var tracker = FindFirstObjectByType<SnookerBallTracker>();
                var info = tracker?.FindBall("White_CueBall");
                cueBall = info?.transform?.GetComponent<Rigidbody>();
            }
            if (autoRun && cue != null && cueBall != null) BeginShot();
        }

        public void BeginShot()
        {
            if (cue == null || cueBall == null) return;
            startPosition = cueBall.position;
            settledFor = 0f;
            peakSpeed = 0f;
            measuring = true;
            Vector3 aim = startPosition + transform.forward * targetDistance;
            aim.y = startPosition.y;
            cue.ShootAt(aim, shotPower);
        }

        private void FixedUpdate()
        {
            if (!measuring || cueBall == null) return;
            float speed = cueBall.linearVelocity.magnitude;
            if (float.IsNaN(speed) || float.IsInfinity(speed)) { measuring = false; return; }
            peakSpeed = Mathf.Max(peakSpeed, speed);
            settledFor = speed <= settleSpeed ? settledFor + Time.fixedDeltaTime : 0f;
            if (settledFor < settleSeconds) return;
            Vector3 d = cueBall.position - startPosition;
            float distance = new Vector2(d.x, d.z).magnitude;
            if (!float.IsNaN(distance) && !float.IsInfinity(distance))
                samples.Add(new CalibrationSample(shotPower, distance, peakSpeed));
            measuring = false;
        }

        [System.Serializable]
        public readonly struct CalibrationSample
        {
            public readonly float power;
            public readonly float distance;
            public readonly float peakSpeed;
            public CalibrationSample(float power, float distance, float peakSpeed)
            { this.power = power; this.distance = distance; this.peakSpeed = peakSpeed; }
        }
    }
}
