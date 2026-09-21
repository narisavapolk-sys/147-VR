using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VR147.AAA.Physics;

public static class M5BatchCI
{
    private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
    private const string ReadyMarker = "Assets/AAA/PhysicsCalibration/.m5_straight_ready";
    private static bool certificationDone;
    private static double startTime;

    public static void Run()
    {
        startTime = EditorApplication.timeSinceStartup;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += Tick;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) throw new InvalidOperationException("M5 scene failed to open.");
        var go = GameObject.Find("M5 REAL Straight Batch") ?? new GameObject("M5 REAL Straight Batch");
        if (go.GetComponent<M5RealStraightBatchRunner>() == null)
            go.AddComponent<M5RealStraightBatchRunner>();
        foreach (var b in UnityEngine.Object.FindObjectsByType<CalibrationBatchBootstrap>(FindObjectsInactive.Include)) b.enabled = false;
        foreach (var r in UnityEngine.Object.FindObjectsByType<CalibrationBatchRunner>(FindObjectsInactive.Include)) r.enabled = false;
        foreach (var c in UnityEngine.Object.FindObjectsByType<CalibrationShotController>(FindObjectsInactive.Include)) c.enabled = false;
        Debug.Log("[M5 CI] Prepared REAL runner. Entering PlayMode.");
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (certificationDone) return;
        if (EditorApplication.timeSinceStartup - startTime > 180.0)
        {
            Debug.LogError("[M5 CI] Timeout waiting for REAL batch.");
            EditorApplication.Exit(2);
            return;
        }
        if (!EditorApplication.isPlaying && File.Exists(Full(ReadyMarker)) && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            certificationDone = true;
            EditorApplication.delayCall += Certify;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            Debug.Log("[M5 CI] ENTERED PLAYMODE");
        else if (state == PlayModeStateChange.EnteredEditMode)
            Debug.Log("[M5 CI] RETURNED EDITMODE");
    }

    private static void Certify()
    {
        try
        {
            VR147.AAA.Editor.PhysicsGoldenCertificationAutomation.BuildMeasuredGoldenFromRuntimeJson();
            VR147.AAA.Editor.PhysicsGoldenCertificationAutomation.RunGoldenRegressionFromRuntimeJson();
            File.WriteAllText(Full("Assets/AAA/PhysicsCalibration/.m5_certified"), "M5 CERTIFIED");
            Debug.Log("[M5 CERTIFIED] REAL baseline + independent regression PASS.");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("[M5 CERTIFY] FAILED: " + ex);
            EditorApplication.Exit(3);
        }
    }

    private static string Full(string assetPath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace("/", "\\"));
    }
}
