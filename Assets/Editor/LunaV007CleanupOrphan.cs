using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LunaV007CleanupOrphan
{
    const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
    const string TempPath = "Assets/Scenes/147VR_MainScene_V007_CLEANUP_TEMP.unity";
    const string PrefabPath = "Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab";

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int removed = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource<GameObject>(root);
            if (src != null && AssetDatabase.GetAssetPath(src) == PrefabPath)
            {
                Debug.Log("[V007 CLEANUP] removing orphan root " + root.name);
                UnityEngine.Object.DestroyImmediate(root);
                removed++;
            }
        }
        Debug.Log("[V007 CLEANUP] removed=" + removed);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, TempPath, false))
            throw new Exception("Temp scene save failed: " + TempPath);
        AssetDatabase.Refresh();
        Debug.Log("[V007 CLEANUP] TEMP_SAVED=" + TempPath);
        EditorApplication.Exit(0);
    }
}
