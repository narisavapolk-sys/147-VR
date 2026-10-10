#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Non-destructive table visual swap. MenuItem-only (no auto-run).
// Saves result to a NEW scene file; the original 147VR_MainScene.unity is NOT modified.
public static class _147VR_ArtSwap_Tools
{
    const string FBX        = "Assets/AAA/ImportedSnooker/V006_DARC_V04/147VR_Table_WPBSA_V006_DARC_V04_GAME.fbx";
    const string OLD_TABLE  = "Prefab_WPBSA_12Foot_Snooker";
    const string TEST_OBJ   = "TEST_NewTable_V006_DARC";
    const string NEW_SCENE  = "Assets/Scenes/147VR_MainScene_ART_V006.unity";

    [MenuItem("147VR/Art/Swap To New Table (save to NEW scene)")]
    public static void SwapToNewTable()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) { Debug.LogError("147VR/Swap: no scene open"); return; }

        // clean up the earlier test object
        foreach (var root in scene.GetRootGameObjects())
        {
            var t = FindDeep(root.transform, TEST_OBJ);
            if (t != null) { Object.DestroyImmediate(t); Debug.Log("147VR/Swap: removed test object."); break; }
        }

        // find old visual table root
        GameObject old = null;
        foreach (var root in scene.GetRootGameObjects()) { old = FindDeep(root.transform, OLD_TABLE); if (old != null) break; }
        if (old == null) { Debug.LogError("147VR/Swap: old table '" + OLD_TABLE + "' not found"); return; }

        var oldR = old.GetComponentsInChildren<Renderer>(true);
        if (oldR.Length == 0) { Debug.LogError("147VR/Swap: old table has no renderers"); return; }
        Bounds ob = Union(oldR);

        // instantiate new table
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
        if (prefab == null) { Debug.LogError("147VR/Swap: FBX not found at " + FBX); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "147VR_Table_V006_DARC_VISUAL";

        // match old transform (orientation + placement follow the playfield/physics)
        go.transform.position = old.transform.position;
        go.transform.rotation = old.transform.rotation;
        go.transform.localScale = old.transform.localScale;

        // align by bounds: XZ center to old, and bottom (floor contact) to old
        var nr = go.GetComponentsInChildren<Renderer>(true);
        Bounds nb = Union(nr);
        Vector3 p = go.transform.position;
        p.x += ob.center.x - nb.center.x;
        p.z += ob.center.z - nb.center.z;
        p.y += ob.min.y - nb.min.y;   // sit on same floor line as old table
        go.transform.position = p;

        nb = Union(go.GetComponentsInChildren<Renderer>(true)); // re-measure after move

        // disable old (keep for rollback)
        old.SetActive(false);

        Undo.RegisterCreatedObjectUndo(go, "Swap New Table");
        Selection.activeGameObject = go;

        bool saved = EditorSceneManager.SaveScene(scene, NEW_SCENE);
        Debug.Log(string.Format(
            "147VR/Swap {0} -> '{1}' (original untouched). old: top={2:F3} size={3} | new: top={4:F3} size={5}. Old table DISABLED (re-enable to rollback).",
            saved ? "SAVED" : "SAVE FAILED", NEW_SCENE,
            ob.max.y, Fmt(ob.size), nb.max.y, Fmt(nb.size)));
    }

    static GameObject FindDeep(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; }
        return null;
    }
    static Bounds Union(Renderer[] rs) { Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds); return b; }
    static string Fmt(Vector3 v) { return string.Format("({0:F2},{1:F2},{2:F2})", v.x, v.y, v.z); }
}
#endif
