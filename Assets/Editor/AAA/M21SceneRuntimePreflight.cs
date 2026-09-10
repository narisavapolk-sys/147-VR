using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VR147.AAA.Editor
{
    [InitializeOnLoad]
    internal static class M21SceneRuntimePreflight
    {
        private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_M21_StunCalibration.unity";
        private static bool busy;

        static M21SceneRuntimePreflight() => EditorSceneManager.sceneOpened += OnSceneOpened;

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (busy || scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode) return;
            busy = true;
            try { Normalize(scene); }
            finally { busy = false; }
        }

        private static void Normalize(Scene scene)
        {
            var table = GameObject.Find("PREFAB SNOOKER table");
            var bed = table != null ? table.GetComponentInChildren<BoxCollider>(true) : null;
            var cue = GameObject.Find("Sphere.009")?.GetComponent<Rigidbody>();
            var target = GameObject.Find("Red")?.GetComponent<Rigidbody>();
            if (bed == null || cue == null || target == null) return;
            var cueCol = cue.GetComponent<SphereCollider>();
            var targetCol = target.GetComponent<SphereCollider>();
            if (cueCol == null || targetCol == null) return;

            float surfaceTop = cue.position.y - cueCol.radius;
            Vector3 worldSize = bed.bounds.size;
            Vector3 center = bed.bounds.center;
            bed.transform.SetParent(null, true);
            bed.transform.localScale = Vector3.one;
            bed.transform.SetPositionAndRotation(new Vector3(center.x, surfaceTop - 0.02f, center.z), Quaternion.identity);
            bed.center = Vector3.zero;
            bed.size = new Vector3(worldSize.x, 0.04f, worldSize.z);
            bed.enabled = true;
            bed.isTrigger = false;

            cueCol.enabled = true;
            targetCol.enabled = true;
            cue.position = new Vector3(center.x, surfaceTop + cueCol.radius, center.z - 0.325f);
            target.position = new Vector3(center.x, surfaceTop + targetCol.radius, center.z + 0.325f);
            cue.linearVelocity = Vector3.zero;
            cue.angularVelocity = Vector3.zero;
            target.linearVelocity = Vector3.zero;
            target.angularVelocity = Vector3.zero;
            UnityEngine.Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[147VR M2.1] Scene preflight normalized | surfaceTop={surfaceTop:F6} | bedSize={bed.bounds.size} | cue={cue.position} | target={target.position}");
        }
    }
}
