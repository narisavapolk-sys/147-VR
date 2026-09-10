using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.IO;

[InitializeOnLoad]
public static class TempCleanChainRuntimeAudit
{
    const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
    const string Trigger = "TempCleanChainPlay.trigger";
    static bool armed;
    static TempCleanChainRuntimeAudit()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Trigger)) return;
            File.Delete(Trigger); armed = true;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.playModeStateChanged += OnPlayState;
            EditorApplication.isPlaying = true;
        };
    }
    static void OnPlayState(PlayModeStateChange state)
    {
        if (!armed || state != PlayModeStateChange.EnteredPlayMode) return;
        armed = false;
        EditorApplication.delayCall += AuditInPlay;
    }
    static void AuditInPlay()
    {
        var setups = UnityEngine.Object.FindObjectsByType<SnookerPhysicsSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[CleanChainPlay] setups={setups.Length}");
        if (setups.Length != 1) throw new Exception("Expected exactly one SnookerPhysicsSetup");
        var table = setups[0].tableRoot;
        if (table == null) throw new Exception("tableRoot NULL in Play Mode");
        Debug.Log($"[CleanChainPlay] tableRoot={table.name} active={table.gameObject.activeInHierarchy}");
        if (table.name != "Prefab_WPBSA_12Foot_Snooker") throw new Exception("Wrong tableRoot");
        var bed = table.Find("Bed_Collider");
        var bc = bed ? bed.GetComponent<BoxCollider>() : null;
        if (bc == null) throw new Exception("Golden Bed_Collider missing");
        Debug.Log($"[CleanChainPlay] goldenBed size={bc.size} center={bc.center} topY={bc.bounds.max.y:F6}");
        var runtime = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int roots = 0;
        Transform rt = null;
        foreach (var tr in runtime)
            if (tr.name == "Physics Table (runtime)" && tr.parent == null && tr.gameObject.scene.IsValid()) { roots++; rt = tr; }
        Debug.Log($"[CleanChainPlay] runtimePhysicsRoots={roots}");
        if (roots != 1) throw new Exception("Expected exactly one runtime Physics Table root");
        var boxes = rt.GetComponentsInChildren<BoxCollider>(true);
        Debug.Log($"[CleanChainPlay] runtimeBoxColliders={boxes.Length}");
        foreach (var box in boxes)
            Debug.Log($"[CleanChainPlay] collider {box.name} size={box.size} center={box.center} enabled={box.enabled}");
        Debug.Log("[CleanChainPlay] PLAY_MODE_ALIGNMENT_SMOKE_PASS");
        EditorApplication.isPlaying = false;
        EditorApplication.playModeStateChanged -= OnPlayState;
    }
}
