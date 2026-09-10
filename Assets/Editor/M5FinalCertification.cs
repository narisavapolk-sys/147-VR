using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VR147.AAA.Cue;
using VR147.AAA.Physics;

public static class M5FinalCertification
{
    private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
    private const string RuntimeDir = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements";
    private const int Repetitions = 5;
    private const float Dt = 1f / 120f;
    private const float MaxSeconds = 600f;
    private const float SettleSpeed = 0.025f;
    private const float SettleSeconds = 0.15f;

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) throw new InvalidOperationException("M5 scene could not be opened.");

        var setup = UnityEngine.Object.FindFirstObjectByType<SnookerPhysicsSetup>();
        if (setup == null) throw new InvalidOperationException("SnookerPhysicsSetup missing.");
        setup.EnsurePhysics();

        var tracker = UnityEngine.Object.FindFirstObjectByType<SnookerBallTracker>();
        tracker ??= new GameObject("M5 Direct Tracker").AddComponent<SnookerBallTracker>();
        tracker.tableRoot = setup.tableRoot;
        tracker.Refresh();
        var cueInfo = tracker.FindBall("White_CueBall");
        if (cueInfo == null)
        {
            foreach (var candidate in tracker.Balls)
            {
                if (candidate != null && candidate.points == 0 && candidate.transform != null &&
                    (candidate.name.StartsWith("White_CueBall", StringComparison.Ordinal) || candidate.name == "Sphere.009" || candidate.name == "Cue")) { cueInfo = candidate; break; }
            }
        }
        var cueBall = cueInfo?.transform != null ? cueInfo.transform.GetComponent<Rigidbody>() : null;
        if (cueBall == null)
        {
            var namedCue = GameObject.Find("Sphere.009");
            cueBall = namedCue != null ? namedCue.GetComponent<Rigidbody>() : null;
        }
        if (cueBall == null) throw new InvalidOperationException("Cue ball Rigidbody missing after resolver fallback.");

        if (!CalibrationEnvironmentIsolator.Isolate(cueBall, setup))
            throw new InvalidOperationException("M5 calibration environment isolation failed.");
        var probeSurface = GameObject.Find("Surface")?.GetComponent<BoxCollider>();
        var surfaceBody = probeSurface != null ? probeSurface.gameObject.AddComponent<Rigidbody>() : null;
        if (surfaceBody != null) surfaceBody.isKinematic = true;
        var probeCue = cueBall.GetComponent<SphereCollider>();
        bool probePenetration = probeSurface != null && probeCue != null &&
            Physics.ComputePenetration(probeCue, cueBall.position, cueBall.rotation,
                probeSurface, probeSurface.transform.position, probeSurface.transform.rotation, out _, out _);
        var probeHits = Physics.OverlapSphere(cueBall.position, 0.04f, ~0, QueryTriggerInteraction.Ignore);
        Debug.Log($"[M5 PROBE] cue={cueBall.name} pos={cueBall.position} radius={(probeCue != null ? probeCue.radius : -1f):F6} surface={(probeSurface != null ? probeSurface.bounds.ToString() : "NULL")} penetrationAtActual={probePenetration} ignorePair={(probeSurface != null && probeCue != null ? Physics.GetIgnoreCollision(probeCue, probeSurface) : true)} ignoreLayers={(probeSurface != null ? Physics.GetIgnoreLayerCollision(cueBall.gameObject.layer, probeSurface.gameObject.layer) : true)} overlapCount={probeHits.Length} overlapNames={string.Join(",", System.Linq.Enumerable.Select(probeHits, c => c.name))}");
        var host = GameObject.Find("M5 Authority") ?? new GameObject("M5 Authority");
        var lifecycle = host.GetComponent<M5ShotLifecycle>() ?? host.AddComponent<M5ShotLifecycle>();
        lifecycle.ballTracker = tracker;
        lifecycle.settledSpeedThreshold = SettleSpeed;
        lifecycle.settledDuration = SettleSeconds;
        int started = 0, settled = 0;
        lifecycle.ShotStarted += () => started++;
        lifecycle.ShotSettled += () => settled++;
        MethodInfo lifecycleUpdate = typeof(M5ShotLifecycle).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        if (lifecycleUpdate == null) throw new InvalidOperationException("M5ShotLifecycle.Update reflection failed.");

        var adapterObject = new GameObject("M5 Final CuePhysicsAdapter");
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
        SimulationMode oldMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        try
        {
            var baseline = RunGroup(cueBall, adapter, stroke, lifecycle, lifecycleUpdate, start, null, ref started, ref settled);
            var regression = RunGroup(cueBall, adapter, stroke, lifecycle, lifecycleUpdate, start, baseline, ref started, ref settled);
            if (started != Repetitions * 2 || settled != Repetitions * 2)
                throw new InvalidOperationException($"M5 lifecycle event counts invalid: started={started} settled={settled}");
            Persist(BaselineFile, baseline);
            Persist(RegressionFile, regression);
            string root = Directory.GetParent(Application.dataPath).FullName;
            File.WriteAllText(Path.Combine(root, "Assets/AAA/PhysicsCalibration/.m5_straight_ready"), "ready");
            Debug.Log($"[M5 FINAL] REAL PhysX complete: baseline={baseline.Count} regression={regression.Count} ShotStarted={started} ShotSettled={settled}");

            VR147.AAA.Editor.PhysicsGoldenCertificationAutomation.BuildMeasuredGoldenFromRuntimeJson();
            VR147.AAA.Editor.PhysicsGoldenCertificationAutomation.RunGoldenRegressionFromRuntimeJson();
            File.WriteAllText(Path.Combine(root, "Assets/AAA/PhysicsCalibration/.m5_certified"), "M5 CERTIFIED");
            Debug.Log("[M5 CERTIFIED] REAL Straight + lifecycle + independent regression PASS.");
        }
        finally
        {
            Physics.simulationMode = oldMode;
            UnityEngine.Object.DestroyImmediate(adapterObject);
            UnityEngine.Object.DestroyImmediate(profile);
        }
        EditorApplication.Exit(0);
    }
    private const string BaselineFile = "straight_runtime_measurements.json";
    private const string RegressionFile = "straight_regression_measurements.json";

    private static List<Sample> RunGroup(
        Rigidbody cueBall,
        CuePhysicsAdapter adapter,
        CueStrokeModel stroke,
        M5ShotLifecycle lifecycle,
        MethodInfo lifecycleUpdate,
        Vector3 start,
        List<Sample> reference,
        ref int started,
        ref int settled)
    {
        var samples = new List<Sample>(Repetitions);
        for (int i = 0; i < Repetitions; i++)
        {
            cueBall.isKinematic = false;
            cueBall.detectCollisions = true;
            cueBall.position = start;
            cueBall.rotation = Quaternion.identity;
            cueBall.linearVelocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            lifecycle.ResetToIdle();
            int beforeStarted = started;
            int beforeSettled = settled;
            if (!lifecycle.BeginShot()) throw new InvalidOperationException("M5 lifecycle rejected REAL shot start.");
            if (started != beforeStarted + 1) throw new InvalidOperationException("M5 ShotStarted event missing.");

            Vector3 aim = cueBall.position + Vector3.forward;
            if (!CueShotValidator.TryCreate(cueBall.position, aim, 1f, stroke, out CueShotData shot))
                throw new InvalidOperationException("CueShotValidator rejected REAL straight shot.");
            if (!adapter.Apply(shot, Vector2.zero))
                throw new InvalidOperationException("CuePhysicsAdapter rejected REAL straight shot.");

            float peak = cueBall.linearVelocity.magnitude;
            Vector3 last = cueBall.position;
            float quiet = 0f;
            bool lifecycleSettled = false;
            int maxSteps = Mathf.CeilToInt(MaxSeconds / Dt);
            for (int step = 0; step < maxSteps; step++)
            {
                Physics.Simulate(Dt);
                lifecycleUpdate.Invoke(lifecycle, null);
                peak = Mathf.Max(peak, cueBall.linearVelocity.magnitude);
                last = cueBall.position;
                if (cueBall.linearVelocity.magnitude <= SettleSpeed) quiet += Dt;
                else quiet = 0f;
                if (settled > beforeSettled)
                {
                    lifecycleSettled = true;
                    break;
                }
            }
            if (!lifecycleSettled)
                throw new InvalidOperationException($"M5 lifecycle did not reach Settled on shot {i + 1}/{Repetitions}: state={lifecycle.CurrentState}, finalSpeed={cueBall.linearVelocity.magnitude:F9}, finalPos={cueBall.position}, peak={peak:F9}");
            float distance = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(last.x, last.z));
            if (distance <= 0f || float.IsNaN(distance) || float.IsInfinity(distance) || peak <= 0f)
                throw new InvalidOperationException($"Invalid REAL measurement {i + 1}/{Repetitions}: distance={distance}, peak={peak}");
            samples.Add(new Sample(distance, peak));
            Debug.Log($"[M5 FINAL] REAL {(reference == null ? "BASE" : "REG")} {i + 1}/{Repetitions} distance={distance:F9} peakSpeed={peak:F9} lifecycle={lifecycle.CurrentState}");
        }
        return samples;
    }
    private static void Persist(string fileName, List<Sample> samples)
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        string dir = Path.Combine(root, RuntimeDir.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(dir);
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
        File.WriteAllText(Path.Combine(dir, fileName), JsonUtility.ToJson(payload, true));
    }

    private static void DisableOtherBalls(Rigidbody cueBall, SnookerBallTracker tracker)
    {
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
}



