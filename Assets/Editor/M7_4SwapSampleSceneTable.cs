using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class M7_4SwapSampleSceneTable
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string PrefabPath = "Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v003.prefab";
    const string NewRootName = "147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v003";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var concert = GameObject.Find("ConcertRoom");
        if (concert == null) throw new System.Exception("ConcertRoom not found");
        var oldSurface = FindDesc(concert.transform, "TABLE SURFACE");
        if (oldSurface == null) throw new System.Exception("TABLE SURFACE not found");
        var oldRenderer = oldSurface.GetComponent<Renderer>();
        if (oldRenderer == null) throw new System.Exception("TABLE SURFACE renderer not found");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new System.Exception("V003 prefab missing");
        foreach (var t in GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (IsOldTableVisual(t)) DisableRenderers(t.gameObject);
        var existing = GameObject.Find(NewRootName);
        if (existing != null) Object.DestroyImmediate(existing);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        visual.name = NewRootName;
        visual.transform.SetParent(concert.transform, true);
        var newSurface = FindDesc(visual.transform, "TABLE SURFACE");
        if (newSurface == null) throw new System.Exception("V003 TABLE SURFACE not found");
        var newRenderer = newSurface.GetComponent<Renderer>();
        if (newRenderer == null) throw new System.Exception("V003 TABLE SURFACE renderer not found");
        visual.transform.rotation = oldSurface.rotation;
        float sx = oldRenderer.bounds.size.x / Mathf.Max(0.000001f, newRenderer.bounds.size.x);
        float sz = oldRenderer.bounds.size.z / Mathf.Max(0.000001f, newRenderer.bounds.size.z);
        float scale = (sx + sz) * 0.5f;
        visual.transform.localScale *= scale;
        newRenderer = newSurface.GetComponent<Renderer>();
        visual.transform.position += oldRenderer.bounds.center - newRenderer.bounds.center;
        EnsureVisualOnly(visual);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("SampleScene save failed");
        AssetDatabase.SaveAssets();
        Debug.Log($"M7.4 SAMPLESCENE TABLE SWAP PASS scale={scale:F6}");
        Debug.Log($"M7.4 V003 VISUAL={visual.name} parent={visual.transform.parent.name}");
    }

    static bool IsOldTableVisual(Transform t)
    {
        if (t.name == "TABLE SURFACE" || t.name == "TABLE FRAME" || t.name == "Base Table Support") return true;
        var n=t.name.ToLowerInvariant();
        if (n.Contains("cue")) return false;
        return n == "pocket pad" || n.Contains("pocket") || n.Contains("cushion");
    }

    static void DisableRenderers(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled=false;
    }

    static void EnsureVisualOnly(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled=false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic=true;
    }

    static Transform FindDesc(Transform root, string exact)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == exact) return t;
        return null;
    }
}
