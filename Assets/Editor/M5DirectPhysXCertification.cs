using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VR147.AAA.Cue;

public static class M5DirectPhysXCertification
{
    private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
    private const string RuntimeDir = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements";
    private const string BaselineFile = "straight_runtime_measurements.json";
    private const string RegressionFile = "straight_regression_measurements.json";
    private const int Samples = 5;
    private const float Dt = 1f / 120f;
    private const float MaxSeconds = 12f;
    private const float SettleSpeed = 0.025f;
    private const float SettleSeconds = 0.15f;

    [MenuItem("147VR/AAA/Physics/M5 Direct PhysX Certification")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) throw new InvalidOperationException("M5 scene could not be opened.");

        var setup = UnityEngine.Object.FindFirstObjectByType<SnookerPhysicsSetup>();
        if (setup == null) throw new InvalidOperationException("SnookerPhysicsSetup missing.");
        setup.EnsurePhysics();

        var tracker = UnityEngine.Object.FindFirstObjectByType<SnookerBallTracker>();
        var info = tracker != null ? tracker.FindBall("White_CueBall") : null;
        var cueBall = info?.transform != null ? info.transform.GetComponent<Rigidbody>() : null;
        if (cueBall == null) throw new InvalidOperationException("White_CueBall Rigidbody missing.");

        DisableOtherBalls(cueBall, tracker);
        var adapterObject = new GameObject("M5 Direct CuePhysicsAdapter");
        var adapter = adapterObject.AddComponent<CuePhysicsAdapter>();
        adapter.Configure(cueBall);

        var profile = ScriptableObject.CreateInstance<CueAimProfile>();
        profile.maxShotSpeed = 8f;
        profile.minShotSpeed = 0.15f;
        profile.chargeSeconds = 0.85f;
        profile.deadZone = 0.02f;
        var stroke = new CueStrokeModel();
        stroke.SetProfile(profile);

        Vector3 start = new Vector3(cueBall.position.x, setup.SurfaceTopY + 0.02725f, cueBall.position.z);
        var baseline = RunSamples(cueBall, adapter, stroke, start, Samples, null);
        var regression = RunSamples(cueBall, adapter, stroke, start, Samples, baseline);
        Persist(BaselineFile, baseline);
        Persist(RegressionFile, regression);
        File.WriteAllText(Full("Assets/AAA/PhysicsCalibration/.m5_direct_ready"), "ready");
        UnityEngine.Object.DestroyImmediate(adapterObject);
        UnityEngine.Object.DestroyImmediate(profile);
        Debug.Log("[M5 DIRECT] REAL PhysX batch persisted: 5 baseline + 5 regression.");
        Debug.Log("[M5 DIRECT] NOTE: simulation uses CuePhysicsAdapter + real Rigidbody + Physics.Simulate; no fake measurements.");
        EditorApplication.Exit(0);
    }
    private static List<Sample> RunSamples(
        Rigidbody cueBall,
        CuePhysicsAdapter adapter,
        CueStrokeModel stroke,
        Vector3 start,
        int count,
        List<Sample> reference)
    {
        var samples = new List<Sample>(count);
        for (int i = 0; i < count; i++)
        {
            cueBall.isKinematic = false;
            cueBall.detectCollisions = true;
            cueBall.position = start;
            cueBall.rotation = Quaternion.identity;
            cueBall.linearVelocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();

            var shot = BuildStraightShot(cueBall, stroke);
            if (!adapter.Apply(shot, Vector2.zero))
                throw new InvalidOperationException($"CuePhysicsAdapter rejected real shot {i + 1}/{count}.");

            Physics.SyncTransforms();
            float peak = 0f;
            Vector3 last = cueBall.position;
            float settledFor = 0f;
            int steps = Mathf.CeilToInt(MaxSeconds / Dt);
            for (int step = 0; step < steps; step++)
            {
                Physics.Simulate(Dt);
                peak = Mathf.Max(peak, cueBall.linearVelocity.magnitude);
                last = cueBall.position;
                if (cueBall.linearVelocity.magnitude <= SettleSpeed)
                    settledFor += Dt;
                else
                    settledFor = 0f;
                if (settledFor >= SettleSeconds)
                    break;
            }

            float distance = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(last.x, last.z));
            if (distance <= 0f || float.IsNaN(distance) || float.IsInfinity(distance))
                throw new InvalidOperationException($"Invalid real distance for shot {i + 1}/{count}: {distance}");
            samples.Add(new Sample(distance, peak));
            Debug.Log($"[M5 DIRECT] REAL {(reference == null ? "BASE" : "REG")} {i + 1}/{count} distance={distance:F9} peakSpeed={peak:F9}");
        }
        return samples;
    }

    private static CueShotData BuildStraightShot(Rigidbody cueBall, CueStrokeModel stroke)
    {
        Vector3 aim = cueBall.position + Vector3.forward;
        if (!CueShotValidator.TryCreate(cueBall.position, aim, 1f, stroke, out CueShotData shot))
            throw new InvalidOperationException("CueShotValidator rejected straight shot.");
        return shot;
    }

    private static void DisableOtherBalls(Rigidbody cueBall, SnookerBallTracker tracker)
    {
        if (tracker == null) return;
        foreach (var ball in tracker.Balls)
        {
            if (ball?.transform == null) continue;
            Rigidbody rb = ball.transform.GetComponent<Rigidbody>();
            if (rb == null || rb == cueBall) continue;
            var sphere = rb.GetComponent<SphereCollider>();
            if (sphere != null) sphere.enabled = false;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
    }
    private static void Persist(string fileName, List<Sample> samples)
    {
        var payload = new Measurement
        {
            timestampUtc = DateTime.UtcNow.ToString("O"),
            caseType = "Straight",
            repetitions = samples.Count,
            measuredDistance = new float[samples.Count],
            measuredPeakSpeed = new float[samples.Count]
        };
        for (int i = 0; i < samples.Count; i++)
        {
            payload.measuredDistance[i] = samples[i].distance;
            payload.measuredPeakSpeed[i] = samples[i].peakSpeed;
        }
        string path = Full(Path.Combine(RuntimeDir, fileName));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(payload, true));
    }

    [Serializable]
    private struct Sample
    {
        public float distance;
        public float peakSpeed;
        public Sample(float distance, float peakSpeed)
        {
            this.distance = distance;
            this.peakSpeed = peakSpeed;
        }
    }

    [Serializable]
    private sealed class Measurement
    {
        public string timestampUtc;
        public string caseType;
        public int repetitions;
        public float[] measuredDistance;
        public float[] measuredPeakSpeed;
    }

    private static string Full(string assetPath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace("/", "\\"));
    }
}
