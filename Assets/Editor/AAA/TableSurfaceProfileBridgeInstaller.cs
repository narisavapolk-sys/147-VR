using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Physics;

namespace VR147.AAA.Editor
{
    public static class TableSurfaceProfileBridgeInstaller
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
        private const string ProfilePath = "Assets/AAA/PhysicsCalibration/TableSurfaceProfile_RuntimeBaseline.asset";
        private static double smokeDeadline;
        private static bool smokePassed;

        public static void Run()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TableSurfaceProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<TableSurfaceProfile>();
                profile.rollingFriction = 0.18f;
                profile.slidingFriction = 0.32f;
                profile.spinFriction = 0.08f;
                profile.settleSpeed = 0.012f;
                profile.settleSpin = 0.035f;
                profile.measuredTruthCertified = false;
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var setups = Object.FindObjectsByType<SnookerPhysicsSetup>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            SnookerPhysicsSetup setup = null;
            for (int i = 0; i < setups.Length; i++)
            {
                if (setups[i] != null && setups[i].gameObject.scene == scene)
                {
                    setup = setups[i];
                    break;
                }
            }

            if (setup == null)
                throw new System.InvalidOperationException(
                    "SnookerPhysicsSetup not found in loaded Main Scene roots.");

            if (setup.tableSurfaceProfile != profile)
                throw new System.InvalidOperationException("Main Scene does not reference the runtime surface profile.");

            Debug.Log($"[147VR Surface Bridge] REFERENCE PASS | profile={ProfilePath} | certified={profile.measuredTruthCertified} | scene reference verified.");
            EditorApplication.Exit(0);
        }

        public static void RunRuntimeSmoke()
        {
            AssetDatabase.ImportAsset(ProfilePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
                throw new System.InvalidOperationException("Main Scene could not be opened.");

            var setups = Object.FindObjectsByType<SnookerPhysicsSetup>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            SnookerPhysicsSetup setup = null;
            for (int i = 0; i < setups.Length; i++)
            {
                if (setups[i] != null && setups[i].gameObject.scene == scene)
                {
                    setup = setups[i];
                    break;
                }
            }

            var profile = AssetDatabase.LoadAssetAtPath<TableSurfaceProfile>(ProfilePath);
            if (setup == null || profile == null)
                throw new System.InvalidOperationException(
                    $"Main Scene lookup failed | setup={(setup != null)} profile={(profile != null)}.");

            if (setup.tableSurfaceProfile != profile)
                throw new System.InvalidOperationException(
                    $"Main Scene profile reference mismatch | serializedRef={(setup.tableSurfaceProfile != null)}.");

            setup.EnsurePhysics();

            var controllers = Object.FindObjectsByType<TableSurfaceController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            TableSurfaceController controller = null;
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null && controllers[i].gameObject.scene.IsValid())
                {
                    controller = controllers[i];
                    break;
                }
            }

            bool pass = controller != null &&
                        controller.Profile == profile &&
                        !profile.measuredTruthCertified;

            if (pass)
                Debug.Log("[147VR Surface Bridge] RUNTIME CONSTRUCTION PASS | Surface controller exists, profile reference is correct, certified=false keeps unmeasured friction inactive.");
            else
                Debug.LogError("[147VR Surface Bridge] RUNTIME CONSTRUCTION FAIL | Surface TableSurfaceController/profile bridge was not observed.");

            var runtimePhysicsRoot = GameObject.Find("Physics Table (runtime)");
            if (runtimePhysicsRoot != null)
                Object.DestroyImmediate(runtimePhysicsRoot);

            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);

            EditorApplication.Exit(pass ? 0 : 1);
        }
    }
}
