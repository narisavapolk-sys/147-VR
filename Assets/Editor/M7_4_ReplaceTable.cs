using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class M7_4_ReplaceTable
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string PrefabPath = "Assets/BlenderTest/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004.prefab";
    const string OldName = "147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v003";
    const string BackupPath = "Assets/Scenes/SampleScene_PRE_M7_4_V004_REPLACEMENT_20260903.unity";

    public static void Run()
    {
        Debug.Log("[M7.4] START V004 replacement");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) { Debug.LogError("[M7.4] SampleScene invalid"); return; }
        if (!File.Exists(BackupPath))
            File.Copy(ScenePath, BackupPath);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (!prefab) { Debug.LogError("[M7.4] V004 prefab missing"); return; }

        GameObject old = GameObject.Find(OldName);
        Transform parent = old != null ? old.transform.parent : GameObject.Find("ConcertRoom")?.transform;
        if (parent == null) { Debug.LogError("[M7.4] ConcertRoom parent not found"); return; }

        if (old != null) Object.DestroyImmediate(old);

        GameObject table = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        table.name = "147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004";
        table.transform.localPosition = Vector3.zero;
        table.transform.localRotation = Quaternion.identity;
        table.transform.localScale = Vector3.one;

        var visual = FindLargestRenderer(table.transform);
        if (visual == null) { Debug.LogError("[M7.4] No visual renderer in V004"); return; }
        visual.gameObject.name = "TABLE SURFACE";

        var bed = table.transform.Find("Bed_Collider");
        if (bed == null)
        {
            bed = new GameObject("Bed_Collider").transform;
            bed.SetParent(table.transform, false);
        }
        var box = bed.GetComponent<BoxCollider>();
        if (box == null) box = bed.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.314f, 0f);
        box.size = new Vector3(1.78f, 0.04f, 3.56f);
        box.isTrigger = false;
        bed.localPosition = Vector3.zero;
        bed.localRotation = Quaternion.identity;
        bed.localScale = Vector3.one;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[M7.4] REPLACED old table -> V004; visual={visual.name}; Bed_Collider size={box.size} center={box.center}; scene saved");
    }

    static Renderer FindLargestRenderer(Transform root)
    {
        Renderer best = null; float bestVol = -1f;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            float v = r.bounds.size.x * r.bounds.size.y * r.bounds.size.z;
            if (v > bestVol) { bestVol = v; best = r; }
        }
        return best;
    }
}

