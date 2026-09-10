using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VR147.AAA.Physics;

[InitializeOnLoad]
public static class M5DeterministicLaunch
{
    private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
    private const string ReadyMarker = "Assets/AAA/PhysicsCalibration/.m5_straight_ready";
    private const string ArmedKey = "147VR.M5.DeterministicLaunch.Armed";
    private static bool certScheduled;

    static M5DeterministicLaunch()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += Update;
    }

    public static void StartRealBatch()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) throw new InvalidOperationException("M5 calibration scene could not be opened.");
        var runnerObject = GameObject.Find("M5 REAL Straight Batch") ?? new GameObject("M5 REAL Straight Batch");
        if (runnerObject.GetComponent<M5RealStraightBatchRunner>() == null)
            runnerObject.AddComponent<M5RealStraightBatchRunner>();
        DisableLegacyBatchRunners();
        EditorPrefs.SetBool(ArmedKey, true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[M5 LAUNCH] Deterministic REAL batch armed. Starting PlayMode.");
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }
    private static void DisableLegacyBatchRunners()
    {
        foreach (var bootstrap in UnityEngine.Object.FindObjectsByType<CalibrationBatchBootstrap>(FindObjectsInactive.Include))
            bootstrap.enabled = false;
        foreach (var runner in UnityEngine.Object.FindObjectsByType<CalibrationBatchRunner>(FindObjectsInactive.Include))
            runner.enabled = false;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && EditorPrefs.GetBool(ArmedKey, false) && File.Exists(ToFullPath(ReadyMarker)))
        {
            EditorPrefs.SetBool(ArmedKey, false);
            certScheduled = true;
            EditorApplication.delayCall += Certify;
        }
    }

    private static void Update()
    {
        if (!certScheduled && EditorPrefs.GetBool(ArmedKey, false) && File.Exists(ToFullPath(ReadyMarker)) && !EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            EditorPrefs.SetBool(ArmedKey, false);
            certScheduled = true;
            EditorApplication.delayCall += Certify;
        }
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
        catch (Exception ex)
        {
            Debug.LogError("[M5 CERTIFY] BLOCKED: " + ex);
            certScheduled = false;
        }
        finally
        {
            string marker = ToFullPath(ReadyMarker);
            if (File.Exists(marker)) File.Delete(marker);
            AssetDatabase.Refresh();
        }
    }

    private static string ToFullPath(string assetPath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace("/", "\\"));
    }

    [MenuItem("147VR/AAA/Physics/M5 Deterministic Real Batch")]
    private static void MenuStart() => StartRealBatch();
}
