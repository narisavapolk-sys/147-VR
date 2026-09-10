using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class _147VR_M5YoloHarness
{
    private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
    private const string ReadyMarker = "Assets/AAA/PhysicsCalibration/.m5_straight_ready";
    private static bool booted;
    private static bool playRequested;
    private static bool certRequested;

    static _147VR_M5YoloHarness()
    {
        EditorApplication.delayCall += Boot;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void Boot()
    {
        if (booted || SceneManager.GetActiveScene().path != ScenePath) return;
        booted = true;
        var setup = Object.FindFirstObjectByType<SnookerPhysicsSetup>();
        if (setup == null) { Debug.LogError("[M5 YOLO] SnookerPhysicsSetup missing."); return; }
        var host = GameObject.Find("M5 Authority") ?? new GameObject("M5 Authority");
        var tracker = host.GetComponent<SnookerBallTracker>() ?? host.AddComponent<SnookerBallTracker>();
        var lifecycle = host.GetComponent<M5ShotLifecycle>() ?? host.AddComponent<M5ShotLifecycle>();
        if (host.GetComponent<M5ShotEventContract>() == null) host.AddComponent<M5ShotEventContract>();
        tracker.tableRoot = setup.tableRoot;
        lifecycle.ballTracker = tracker;
        tracker.Refresh();
        Debug.Log($"[M5 YOLO] Authority wired. balls={tracker.BallsCount()} pockets={tracker.PocketsCount()} state={lifecycle.CurrentState}");
    }

    private static void Update()
    {
        if (!booted) Boot();

        // Manual PlayMode only. The M5 harness must not force the Editor into PlayMode on scene load; execution is explicitly launched by the M5 runner.
if (!certRequested && File.Exists(ToFullPath(ReadyMarker)) && !EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            certRequested = true;
            EditorApplication.delayCall += Certify;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            playRequested = true;
        if (state == PlayModeStateChange.EnteredEditMode)
            playRequested = false;
    }

    private static void Certify()
    {
        try
        {
            VR147.AAA.Editor.PhysicsGoldenCertificationAutomation.BuildMeasuredGoldenFromRuntimeJson();
            VR147.AAA.Editor.PhysicsGoldenCertificationAutomation.RunGoldenRegressionFromRuntimeJson();
            File.WriteAllText(ToFullPath("Assets/AAA/PhysicsCalibration/.m5_certified"), "M5 CERTIFIED");
            Debug.Log("[M5 CERTIFIED] REAL baseline + independent regression PASS.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[M5 CERTIFY] BLOCKED: " + ex);
            certRequested = false;
        }
        finally
        {
            string marker = ToFullPath(ReadyMarker);
            if (File.Exists(marker)) File.Delete(marker);
        }
    }
    private static string ToFullPath(string assetPath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace("/", "\\"));
    }
}
