using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Linq;

public static class M7_4_MakeV007Main
{
    const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
    const string FbxPath = "Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v007.fbx";
    const string BackupPath = "Assets/Scenes/147VR_MainScene.unity.PRE_V007_MAIN_20260905.bak";

    public static void Run()
    {
        if (Application.isPlaying) throw new System.Exception("ABORT: Play Mode active.");
        AssetDatabase.CopyAsset(ScenePath, BackupPath);
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var oldTable = GameObject.Find("Prefab_WPBSA_12Foot_Snooker");
        if (!oldTable) throw new System.Exception("ABORT: Existing physics table root not found.");

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (!model) throw new System.Exception("ABORT: v007 FBX not found/imported.");

        var oldRenderers = oldTable.GetComponentsInChildren<Renderer>(true);
        foreach (var r in oldRenderers) r.enabled = false;
        foreach (var l in oldTable.GetComponentsInChildren<Light>(true)) l.enabled = false;

        var existing = oldTable.transform.Find("V007_VISUAL_MAIN");
        if (existing) Object.DestroyImmediate(existing.gameObject);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.name = "V007_VISUAL_MAIN";
        visual.transform.SetParent(oldTable.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("M7_4 V007 MAIN PASS: v007 visual installed; existing physics table retained; old table renderers/lights disabled.");
    }
}
