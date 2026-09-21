using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VR147.AAA.Physics
{
    public sealed class CalibrationBatchRunner : MonoBehaviour
    {
        [SerializeField] private CalibrationShotController shotController;
        [SerializeField] private int repetitions = 5;
        [SerializeField] private float delayBetweenShots = 0.5f;
        [SerializeField] private bool persistResults = true;
        [SerializeField] private float shotTimeoutSeconds = 15f;
        [SerializeField] private string outputFileName = "straight_runtime_measurements.json";

        private readonly List<ShotCalibrationResult> results = new();
        private float nextRunTime;
        private float shotStartTime;
        private int completedRuns;
        private bool running;
        private int debugFrame;

        public IReadOnlyList<ShotCalibrationResult> Results => results;
        public bool Running => running;
        public int CompletedRuns => completedRuns;

        public void BeginBatch()
        {
            Debug.Log($"[147VR Calibration] BeginBatch runner={name} repetitions={repetitions} controller={(shotController != null ? "BOUND" : "NULL")}", this);
            results.Clear();
            completedRuns = 0;
            shotStartTime = 0f;
            running = shotController != null && repetitions > 0;
            nextRunTime = Time.time + Mathf.Max(Time.fixedDeltaTime * 2f, 0.05f);
            if (running) shotController.ResetShot();
        }

        public void StopBatch() => running = false;

        private void Update()
        {
            debugFrame++;
            if (running && (debugFrame % 60) == 0)
                Debug.Log($"[147VR Calibration] Tick frame={debugFrame} time={Time.time:F3} realtime={Time.realtimeSinceStartup:F3} executed={shotController?.HasExecutedShot} completed={shotController?.Completed}", this);
            if (!running || shotController == null) return;
            if (completedRuns >= repetitions)
            {
                running = false;
                PersistResults();
                return;
            }

            if (!shotController.HasExecutedShot)
            {
                if (Time.time >= nextRunTime)
                {
                    shotStartTime = Time.time;
                    if (!shotController.ExecuteShot())
                    {
                        running = false;
                        Debug.LogError("[147VR Calibration] ExecuteShot failed; batch aborted.", this);
                    }
                }
                return;
            }

            if (!shotController.Completed)
            {
                if (shotStartTime > 0f && Time.time - shotStartTime > Mathf.Max(1f, shotTimeoutSeconds))
                {
                    running = false;
                    Debug.LogError($"[147VR Calibration] Shot timeout after {shotTimeoutSeconds:F1}s; batch aborted.", this);
                }
                return;
            }

            results.Add(shotController.Result);
            completedRuns++;
            shotStartTime = 0f;
            nextRunTime = Time.time + delayBetweenShots;
            if (completedRuns < repetitions) shotController.ResetShot();
        }

        private void PersistResults()
        {
            if (!persistResults || results.Count == 0) return;
            var payload = new CalibrationMeasurementFile
            {
                timestampUtc = DateTime.UtcNow.ToString("O"),
                caseType = "Straight",
                repetitions = results.Count,
                measuredDistance = new float[results.Count],
                measuredPeakSpeed = new float[results.Count],
                passed = new bool[results.Count]
            };
            for (int i = 0; i < results.Count; i++)
            {
                payload.measuredDistance[i] = results[i].measuredDistance;
                payload.measuredPeakSpeed[i] = results[i].measuredPeakSpeed;
                payload.passed[i] = results[i].passed;
            }
            string directory = Path.Combine(Application.dataPath, "AAA", "PhysicsCalibration", "RuntimeMeasurements");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, outputFileName);
            File.WriteAllText(path, JsonUtility.ToJson(payload, true));
            Debug.Log($"[147VR Calibration] Persisted {results.Count} real measurements: {path}", this);
        }

        [Serializable]
        private sealed class CalibrationMeasurementFile
        {
            public string timestampUtc;
            public string caseType;
            public int repetitions;
            public float[] measuredDistance;
            public float[] measuredPeakSpeed;
            public bool[] passed;
        }
    }
}