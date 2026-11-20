#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    public static class ApplyToMainProductionScene
    {
        [MenuItem("147VR/AAA/Bay2/Apply to Production WipRoom Scene")]
        public static void ApplyToProduction()
        {
            const string mainScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
            Scene current = SceneManager.GetActiveScene();
            if (current.IsValid() && current.isDirty && current.path != mainScenePath)
            {
                EditorUtility.DisplayDialog("147VR Bay2 Production Apply",
                    "The active scene has unsaved changes. Save or discard them deliberately before switching to WipRoom. No scene was changed.",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Apply Bay2 frame to Production WipRoom?",
                "This replaces the Bay2 frame in the production scene and saves WipRoom. The original MainScene remains untouched. Continue?",
                "Apply and Save", "Cancel");
            if (!confirmed) return;

            if (current.IsValid() && current.isDirty && current.path == mainScenePath)
            {
                // The production scene is already open and dirty; preserve the current in-memory state.
            }
            else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = current.IsValid() && current.path == mainScenePath
                ? current
                : EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Could not open " + mainScenePath);
                return;
            }

            PlaceBay2FrameClean.ApplyCleanFrameToScene(true);
            Debug.Log("Successfully applied Bay 2 clean frame to Production scene: " + mainScenePath);
        }
    }
}
#endif
