using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Physics;

namespace VR147.AAA.Editor
{
    public static class M3CushionBatchRunner
    {
        private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_M3_CushionCalibration.unity";

        public static void RunAll()
        {
            Debug.Log("[147VR M3 BATCH] START | dedicated PhysX runtime certification");
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError("[147VR M3 BATCH] Scene missing: " + ScenePath);
                EditorApplication.Exit(2);
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var runner = UnityEngine.Object.FindFirstObjectByType<M3CushionRuntimeRunner>();
            if (runner == null)
            {
                Debug.LogError("[147VR M3 BATCH] M3CushionRuntimeRunner not found");
                EditorApplication.Exit(2);
                return;
            }

            try
            {
                runner.RunBatchAll();
                Debug.Log("[147VR M3 BATCH] COMPLETE");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(3);
            }
        }
    }
}
