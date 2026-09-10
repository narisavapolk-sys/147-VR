using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class M5_1_Install_TMP
{
    public static void Run()
    {
        const string scenePath = "Assets/Scenes/147VR_MainScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var tracker = Object.FindFirstObjectByType<SnookerShotTracker>(FindObjectsInactive.Include);
        if (tracker == null) { Debug.LogError("[M5.1 INSTALL FAIL] SnookerShotTracker missing"); return; }
        var go = tracker.gameObject;
        var lifecycle = go.GetComponent<M5ShotLifecycle>() ?? go.AddComponent<M5ShotLifecycle>();
        var contract = go.GetComponent<M5ShotEventContract>() ?? go.AddComponent<M5ShotEventContract>();
        lifecycle.ballTracker = tracker.ballTracker;
        tracker.shotLifecycle = lifecycle;
        tracker.eventContract = contract;
        contract.InitializeBindings();
        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[M5.1 INSTALL PASS] lifecycle={lifecycle.GetInstanceID()} contract={contract.GetInstanceID()} ballTracker={(lifecycle.ballTracker != null)} scene={scene.name}");
        EditorApplication.Exit(0);
    }
}