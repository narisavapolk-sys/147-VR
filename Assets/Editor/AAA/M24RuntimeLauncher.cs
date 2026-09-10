using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Physics;

namespace VR147.AAA.Editor
{
    public static class M24RuntimeLauncher
    {
        const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_M24_EnglishCalibrationV2.unity";

        public static void RunLeft()
        {
            Run(true);
        }

        public static void RunRight()
        {
            Run(false);
        }

        static void Run(bool left)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var runners = Object.FindObjectsByType<M24EnglishBatchRunnerV2>(FindObjectsSortMode.None);
            foreach (var runner in runners)
                runner.enabled = false;

            foreach (var runner in runners)
            {
                var goName = runner.gameObject.name;
                if (left ? goName == "M24_V2_Left" : goName == "M24_V2_Right")
                    runner.enabled = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[147VR M2.4 LAUNCHER] {(left ? "LEFT" : "RIGHT")} V2 enabled; all other V2 runners disabled.");
            EditorApplication.isPlaying = true;
        }
    }
}
