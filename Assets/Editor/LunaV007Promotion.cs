using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public static class LunaV007Promotion
{
    const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
    const string PrefabPath = "Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab";
    const string VisualName = "147VR_Table_WPBSA_Visual_Clean_v007_MARKING";

    [MenuItem("147VR/LUNA/Promote V007 MARKING Visual")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var physicsRoot = GameObject.Find("Prefab_WPBSA_12Foot_Snooker");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new Exception("V007 MARKING prefab not found.");
        if (physicsRoot == null) throw new Exception("Physics table root not found; refusing promotion.");
        if (GameObject.Find(VisualName) != null) throw new Exception("V007 visual already exists.");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        visual.name = VisualName;
        visual.transform.position = physicsRoot.transform.position;
        visual.transform.rotation = physicsRoot.transform.rotation;
        visual.transform.localScale = physicsRoot.transform.localScale;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("LUNA V007 PROMOTION SAVED: " + ScenePath);
    }
}
