using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VR147.AAA.Editor
{
    [InitializeOnLoad]
    internal static class M21LayerCollisionPreflight
    {
        private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_M21_StunCalibration.unity";
        static M21LayerCollisionPreflight() => EditorSceneManager.sceneOpened += OnOpened;

        private static void OnOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var table = GameObject.Find("PREFAB SNOOKER table");
            var bed = table != null ? table.GetComponentInChildren<BoxCollider>(true) : null;
            var cue = GameObject.Find("Sphere.009")?.GetComponent<Rigidbody>();
            if (bed == null || cue == null) return;
            UnityEngine.Physics.IgnoreLayerCollision(cue.gameObject.layer, bed.gameObject.layer, false);
            UnityEngine.Physics.SyncTransforms();
            Debug.Log($"[147VR M2.1] Layer collision forced ON | cueLayer={cue.gameObject.layer} bedLayer={bed.gameObject.layer} ignored={UnityEngine.Physics.GetIgnoreLayerCollision(cue.gameObject.layer, bed.gameObject.layer)}");
        }
    }
}
