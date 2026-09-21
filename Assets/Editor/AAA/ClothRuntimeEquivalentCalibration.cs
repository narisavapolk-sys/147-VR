using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Physics;

namespace VR147.AAA.Editor
{
    public static class ClothRuntimeEquivalentCalibration
    {
        private const string SourceScene = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
        private const string TargetScene = "Assets/AAA/PhysicsCalibration/147VR_ClothEquivalentCalibration.unity";
        private const string ProfilePath = "Assets/AAA/PhysicsCalibration/TableSurfaceProfile_RuntimeBaseline.asset";
        private const string MatrixJson = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/cloth_runtime_equivalent_matrix.json";

        public static void Prepare()
        {
            var source = AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene);
            if (source == null) throw new InvalidOperationException("Physics calibration scene missing.");

            var scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            var profile = AssetDatabase.LoadAssetAtPath<TableSurfaceProfile>(ProfilePath);
            if (profile == null) throw new InvalidOperationException("Runtime baseline profile missing.");

            profile.measuredTruthCertified = false;

            var legacyRuntime = GameObject.Find("147VR_RuntimeCalibration");
            if (legacyRuntime != null)
                UnityEngine.Object.DestroyImmediate(legacyRuntime);

            DestroyLegacyComponentRoot<CalibrationBatchBootstrap>();
            DestroyLegacyComponentRoot<CalibrationBatchRunner>();
            DestroyLegacyComponentRoot<CalibrationShotController>();
            DestroyLegacyComponentRoot<SnookerCueController>();

            var setup = UnityEngine.Object.FindFirstObjectByType<SnookerPhysicsSetup>();
            if (setup == null) throw new InvalidOperationException("SnookerPhysicsSetup missing in calibration scene.");

            var old = GameObject.Find("147VR_ClothResponseMatrix");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);

            var root = new GameObject("147VR_ClothResponseMatrix");
            var runner = root.AddComponent<ClothResponseMatrixRunner>();

            var so = new SerializedObject(runner);
            so.FindProperty("physicsSetup").objectReferenceValue = setup;
            so.FindProperty("profile").objectReferenceValue = profile;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene) != null)
                AssetDatabase.DeleteAsset(TargetScene);

            if (!EditorSceneManager.SaveScene(scene, TargetScene))
                throw new InvalidOperationException("Cloth calibration scene save failed.");

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(TargetScene, ImportAssetOptions.ForceUpdate);
            Debug.Log("[147VR Cloth Matrix] Prepared: " + TargetScene);
        }

        public static void Run()
        {
            Prepare();

            string json = ProjectPath(MatrixJson);
            if (File.Exists(json)) File.Delete(json);

            EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            EditorApplication.isPlaying = true;
            Debug.Log("[147VR Cloth Matrix] REAL-EQUIVALENT MATRIX START | repetitions=5");
        }

        private static void Poll()
        {
            if (!File.Exists(ProjectPath(MatrixJson))) return;

            EditorApplication.update -= Poll;
            EditorApplication.isPlaying = false;
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var matrix = JsonUtility.FromJson<PromoteData>(
                File.ReadAllText(ProjectPath(MatrixJson)));
            if (matrix == null || !matrix.candidateAccepted)
                throw new InvalidOperationException("Cloth response matrix candidate rejected.");

            Debug.Log("[147VR Cloth Matrix] MATRIX PASS | persisted runtime-equivalent candidate accepted.");
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        public static void Promote()
        {
            string path = ProjectPath(MatrixJson);
            if (!File.Exists(path)) throw new FileNotFoundException("Cloth response matrix missing.", path);

            var data = JsonUtility.FromJson<PromoteData>(File.ReadAllText(path));
            if (data == null || !data.candidateAccepted)
                throw new InvalidOperationException("Matrix candidate is not accepted.");

            var profile = AssetDatabase.LoadAssetAtPath<TableSurfaceProfile>(ProfilePath);
            if (profile == null) throw new InvalidOperationException("Runtime baseline profile missing.");

            if (float.IsNaN(data.fittedRollingFriction) ||
                float.IsNaN(data.fittedRollingDamping) ||
                float.IsNaN(data.fittedSlidingFriction) ||
                float.IsNaN(data.fittedSpinFriction))
                throw new InvalidOperationException("Fitted coefficient payload is incomplete.");

            profile.rollingFriction = data.fittedRollingFriction;
            profile.rollingDamping = data.fittedRollingDamping;
            profile.slidingFriction = data.fittedSlidingFriction;
            profile.spinFriction = data.fittedSpinFriction;
            profile.measuredTruthCertified = true;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            Debug.Log($"[147VR Cloth Matrix] PROMOTE PASS | rolling={profile.rollingFriction:F6} rollingDamping={profile.rollingDamping:F6} sliding={profile.slidingFriction:F6} spin={profile.spinFriction:F6} | maxErr={Mathf.Max(data.maxRollingRelativeError, Mathf.Max(data.maxSlidingRelativeError, data.maxSpinRelativeError)):P2}");
            EditorApplication.Exit(0);
        }

        [Serializable]
        private sealed class PromoteData
        {
            public bool candidateAccepted;
            public float fittedRollingFriction;
            public float fittedRollingDamping;
            public float fittedSlidingFriction;
            public float fittedSpinFriction;
            public float maxRollingRelativeError;
            public float maxSlidingRelativeError;
            public float maxSpinRelativeError;
        }

        private static void DestroyLegacyComponentRoot<T>() where T : Component
        {
            var component = UnityEngine.Object.FindFirstObjectByType<T>();
            if (component != null)
                UnityEngine.Object.DestroyImmediate(component.gameObject);
        }

        private static string ProjectPath(string assetPath) =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
