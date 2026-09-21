using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using VR147.AAA.Physics;

public static class M22FollowCalibrationAutomation
{
    private const string SourceScene = "Assets/AAA/PhysicsCalibration/147VR_M21_StunCalibration.unity";
    private const string TargetScene = "Assets/AAA/PhysicsCalibration/147VR_M22_FollowCalibration.unity";

    public static void CreateScene()
    {
        var scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
        var old = Object.FindFirstObjectByType<M21StunBatchRunner>();
        if (old != null) Object.DestroyImmediate(old.gameObject.GetComponent<M21StunBatchRunner>());
        var runner = Object.FindFirstObjectByType<M22FollowBatchRunner>();
        if (runner == null)
        {
            var host = new GameObject("M22_FollowBatchRunner");
            runner = host.AddComponent<M22FollowBatchRunner>();
        }
        var so = new SerializedObject(runner);
        so.FindProperty("cueBall").objectReferenceValue = GameObject.Find("Red")?.GetComponent<Rigidbody>();
        so.FindProperty("objectBall").objectReferenceValue = GameObject.Find("Sphere.009")?.GetComponent<Rigidbody>();
        so.FindProperty("physicsAdapter").objectReferenceValue = Object.FindAnyObjectByType<VR147.AAA.Cue.CuePhysicsAdapter>();
        so.FindProperty("physicsSetup").objectReferenceValue = Object.FindAnyObjectByType<SnookerPhysicsSetup>();
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, TargetScene);
        AssetDatabase.SaveAssets();
        Debug.Log("[147VR M2.2] Scene created: " + TargetScene);
    }
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        EditorApplication.update -= PollCompletion;
        EditorApplication.update += PollCompletion;
        EditorApplication.isPlaying = true;
        Debug.Log("[147VR M2.2] Batch runtime START");
    }

    private static void PollCompletion()
    {
        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/follow_runtime_measurements.json");
        if (!File.Exists(path)) return;
        EditorApplication.update -= PollCompletion;
        EditorApplication.isPlaying = false;
        Debug.Log("[147VR M2.2] Batch runtime COMPLETE; JSON exists");
        EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }
}