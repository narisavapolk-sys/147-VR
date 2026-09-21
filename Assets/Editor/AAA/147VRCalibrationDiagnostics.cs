using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;

namespace VR147.AAA.Editor
{
    public static class CalibrationDiagnostics
    {
        private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";

        public static void DumpCalibrationGeometry()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers.Where(x => x.name.Contains("tableBed") || x.name.Contains("TABLE SURFACE")))
                Debug.Log($"[147VR DIAG] SURFACE name={r.name} pos={r.transform.position} scale={r.transform.lossyScale} boundsCenter={r.bounds.center} boundsSize={r.bounds.size} maxY={r.bounds.max.y}");

            var bodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            foreach (var rb in bodies)
            {
                var sc = rb.GetComponent<SphereCollider>();
                if (sc == null) continue;
                Debug.Log($"[147VR DIAG] BODY name={rb.name} pos={rb.position} scale={rb.transform.lossyScale} radius={sc.radius} mass={rb.mass} gravity={rb.useGravity} kinematic={rb.isKinematic}");
            }

            var setups = Object.FindObjectsByType<SnookerPhysicsSetup>(FindObjectsSortMode.None);
            foreach (var setup in setups)
                Debug.Log($"[147VR DIAG] SETUP root={(setup.tableRoot != null ? setup.tableRoot.name : "NULL")} surfaceName={setup.tableSurfaceName} auto={setup.autoSetupOnStart}");

            Debug.Log($"[147VR DIAG] SCENE={scene.path} rootCount={scene.rootCount}");
            EditorSceneManager.CloseScene(scene, true);
            EditorApplication.Exit(0);
        }
    }
}
