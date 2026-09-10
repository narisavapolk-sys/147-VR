using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Removes the two slim girl models (Cute/Chubby SLIM) from SampleScene.</summary>
public static class RemoveGirlsFromScene
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/147/Remove Slim Girls from Scene")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Scene scene = SceneManager.GetActiveScene();

        int removed = 0;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name.Contains("SLIM"))
            {
                Debug.Log("GIRL_REMOVED " + go.name + " at " + go.transform.position);
                Object.DestroyImmediate(go);
                removed++;
            }
        }

        Debug.Log($"REMOVED_TOTAL {removed}");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("SCENE_SAVED " + ScenePath);
    }
}
