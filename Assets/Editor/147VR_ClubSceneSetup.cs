#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Phase 3: create a CLEAN club scene (table + physics + XR rig + lights only) as a NEW scene file,
// so the original scene is untouched. MenuItem-only; the OWNER runs it.
public static class _147VR_ClubSceneSetup
{
    const string NEW_SCENE = "Assets/Scenes/147VR_ClubRoom.unity";

    [MenuItem("147VR/Art/Club/Create Clean Club Scene (table+physics only, NEW scene)")]
    public static void Create()
    {
        var current = EditorSceneManager.GetActiveScene();
        if (!current.IsValid()) { Debug.LogError("147VR/ClubScene: no valid scene open."); return; }

        if (File.Exists(NEW_SCENE))
        {
            if (!EditorUtility.DisplayDialog("Overwrite?",
                NEW_SCENE + " already exists. Overwrite it?", "Overwrite", "Cancel"))
                return;
            AssetDatabase.DeleteAsset(NEW_SCENE);
        }

        // 1) duplicate the current scene to a NEW file (original file on disk is NOT modified)
        if (!EditorSceneManager.SaveScene(current, NEW_SCENE))
        {
            Debug.LogError("147VR/ClubScene: could not save scene copy.");
            return;
        }

        // 2) open the new scene and strip venue roots (non-destructive to the original)
        var scene = EditorSceneManager.OpenScene(NEW_SCENE, OpenSceneMode.Single);
        int removed = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (ShouldRemove(root.name)) { Object.DestroyImmediate(root); removed++; }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("147VR/ClubScene: created " + NEW_SCENE + " (table+physics+rig+lights); removed " +
                  removed + " venue root(s). Original scene untouched. Now run 'Place CC0 Props Around Table' in this scene.");
    }

    static bool ShouldRemove(string n)
    {
        return n.StartsWith("147VR_VENUE") || n.StartsWith("Crowd_BB") || n.StartsWith("Ceil_BB") || n == "ClubDressing";
    }
}
#endif
