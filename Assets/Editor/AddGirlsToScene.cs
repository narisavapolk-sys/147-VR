using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds the two slim girl models into SampleScene, side by side on the floor,
/// facing the pool table (toward +Z where the camera is).
/// </summary>
public static class AddGirlsToScene
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string CuteFbx = "Assets/Models/Girls/Cute Girl SLIM.fbx";
    const string ChubbyFbx = "Assets/Models/Girls/Chubby magic girl SLIM.fbx";

    [MenuItem("Tools/147/Add Slim Girls to Scene")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name.Contains("SLIM"))
                Object.DestroyImmediate(go);
        }

        // Table occupies center (x=-1.5..1.5, z=-0.75..0.75). Place girls
        // right beside the table, on the floor, facing +Z (toward camera).
        GameObject cute = AddModel(CuteFbx, "CuteGirl_SLIM", new Vector3(-2.3f, 0f, 0.8f));
        GameObject chub = AddModel(ChubbyFbx, "ChubbyGirl_SLIM", new Vector3(2.3f, 0f, 0.8f));

        // FBX from Blender: front faces +Z in Unity. Keep default rotation so
        // the girls face the camera (which sits at z=+3.6 looking toward origin).
        foreach (var go in new[] { cute, chub })
        {
            if (go != null)
            {
                Debug.Log($"GIRL_ADDED {go.name} at {go.transform.position}");
            }
        }

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        Debug.Log("SCENE_SAVED " + ScenePath);
    }

    static GameObject AddModel(string fbxPath, string name, Vector3 pos)
    {
        Object prefab = AssetDatabase.LoadMainAssetAtPath(fbxPath);
        if (prefab == null)
        {
            Debug.LogError($"FBX_MISSING {fbxPath}");
            return null;
        }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        return go;
    }
}
