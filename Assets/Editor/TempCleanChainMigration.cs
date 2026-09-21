using UnityEditor;
using UnityEngine;
using System;
using System.IO;

[InitializeOnLoad]
public static class TempCleanChainMigration
{
    const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
    const string PrefabPath = "Assets/AAA/ImportedSnooker/Prefab_WPBSA_12Foot_Snooker.prefab";
    const string Trigger = "TempCleanChainMigration.trigger";
    static TempCleanChainMigration() { EditorApplication.delayCall += AutoRun; }
    static void AutoRun() { if (File.Exists(Trigger)) { File.Delete(Trigger); Run(); } }

    public static void Run()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new Exception("Golden prefab not found");
        GameObject mainTable = null;
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Prefab_WPBSA_12Foot_Snooker") { mainTable = root; break; }
        if (mainTable == null)
        {
            mainTable = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            mainTable.name = "Prefab_WPBSA_12Foot_Snooker";
            mainTable.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            mainTable.transform.localScale = Vector3.one;
        }
        mainTable.SetActive(true);
        int oldDisabled = 0;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene != scene || !go.activeSelf) continue;
            var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (src == null) continue;
            var srcPath = AssetDatabase.GetAssetPath(src);
            if (srcPath.IndexOf("147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004", StringComparison.OrdinalIgnoreCase) >= 0)
            { go.SetActive(false); oldDisabled++; }
        }
        var t = mainTable.transform;
        foreach (var c in UnityEngine.Object.FindObjectsByType<SnookerPhysicsSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None)) c.tableRoot = t;
        foreach (var c in UnityEngine.Object.FindObjectsByType<SnookerBallTracker>(FindObjectsInactive.Include, FindObjectsSortMode.None)) c.tableRoot = t;
        foreach (var c in UnityEngine.Object.FindObjectsByType<PlayerViewManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)) c.tableRoot = t;
        var bed = mainTable.transform.Find("Bed_Collider");
        var bc = bed != null ? bed.GetComponent<BoxCollider>() : null;
        if (bc == null) throw new Exception("Golden Bed_Collider missing");
        Debug.Log($"[CleanChain] Golden Bed size={bc.size} center={bc.center} world={bc.bounds.size} topY={bc.bounds.max.y:F6}");
        var renderers = mainTable.GetComponentsInChildren<Renderer>(true);
        Bounds b = default; bool has = false;
        foreach (var r in renderers) { if (!r.enabled) continue; if (!has) { b=r.bounds; has=true; } else b.Encapsulate(r.bounds); }
        if (has) Debug.Log($"[CleanChain] Visual bounds={b.size} minY={b.min.y:F6} maxY={b.max.y:F6}");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CleanChain] SAVED oldV004Disabled={oldDisabled} mainTable={mainTable.name}");
    }
}
