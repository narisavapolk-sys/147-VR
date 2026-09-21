using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using VR147.AAA.Cue;
using VR147.AAA.Physics;

namespace VR147.AAA.Editor
{
    public static class M21StunCalibrationAutomation
    {
        private const string SourceScene = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
        private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_M21_StunCalibration.unity";
        private const string Output = "stun_runtime_measurements.json";

        [MenuItem("147VR/AAA/Physics/M2.1/Build Stun Calibration Scene")]
        public static void BuildScene()
        {
            Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new InvalidOperationException("M1 calibration scene could not be opened.");
            GameObject cueObject = GameObject.Find("Sphere.009");
            if (cueObject == null) throw new InvalidOperationException("Deterministic cue ball Sphere.009 missing.");
            Rigidbody cue = cueObject.GetComponent<Rigidbody>();
            SphereCollider cueCollider = cueObject.GetComponent<SphereCollider>();
            if (cue == null || cueCollider == null) throw new InvalidOperationException("Cue ball physics components missing.");

            GameObject oldTarget = GameObject.Find("M21_ObjectBall");
            if (oldTarget != null) UnityEngine.Object.DestroyImmediate(oldTarget);

            var table = GameObject.Find("PREFAB SNOOKER table");
            if (table == null) throw new InvalidOperationException("Snooker table root missing.");
            var bed = table.GetComponentInChildren<BoxCollider>(true);
            if (bed == null || bed.name != "Bed_Collider") throw new InvalidOperationException("Bed_Collider missing.");
            cue.position = new Vector3(cue.position.x, bed.bounds.max.y + cueCollider.radius, cue.position.z);
            cue.isKinematic = false;
            cue.detectCollisions = true;
            cue.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "Red";
            target.transform.position = cue.position + Vector3.forward * 0.65f;
            target.transform.position = new Vector3(target.transform.position.x, bed.bounds.max.y + cueCollider.radius, target.transform.position.z);
            target.transform.localScale = Vector3.one;
            target.layer = cueObject.layer;
            target.GetComponent<Collider>().isTrigger = false;
            UnityEngine.Object.DestroyImmediate(target.GetComponent<SphereCollider>());
            SphereCollider targetCollider = target.AddComponent<SphereCollider>();
            targetCollider.radius = cueCollider.radius;
            targetCollider.sharedMaterial = cueCollider.sharedMaterial;
            Rigidbody targetBody = target.AddComponent<Rigidbody>();
            targetBody.mass = cue.mass;
            targetBody.linearDamping = cue.linearDamping;
            targetBody.angularDamping = cue.angularDamping;
            targetBody.useGravity = true;
            targetBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // M2.1 owns its own real-shot runner. Remove the M1 batch runner/controller from the derived scene to avoid two test drivers.
            foreach (var bootstrap in UnityEngine.Object.FindObjectsByType<CalibrationBatchBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(bootstrap);
            foreach (var batchRunner in UnityEngine.Object.FindObjectsByType<CalibrationBatchRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(batchRunner);
            foreach (var shotController in UnityEngine.Object.FindObjectsByType<CalibrationShotController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(shotController);

            var runtimeRoot = GameObject.Find("147VR_RuntimeCalibration");
            if (runtimeRoot == null) throw new InvalidOperationException("Runtime calibration root missing.");
            var adapter = runtimeRoot.GetComponent<CuePhysicsAdapter>();
            if (adapter == null) throw new InvalidOperationException("CuePhysicsAdapter missing.");
            var runner = runtimeRoot.GetComponent<M21StunBatchRunner>();
            if (runner == null) runner = runtimeRoot.AddComponent<M21StunBatchRunner>();
            Set(runner, "cueBall", cue);
            Set(runner, "objectBall", targetBody);
            Set(runner, "physicsAdapter", adapter);
            Set(runner, "repetitions", 5);
            Set(runner, "shotSpeed", 4f);
            Set(runner, "settleSpeed", 0.08f);
            Set(runner, "settleSeconds", 0.5f);
            Set(runner, "resetGap", 0.65f);
            Set(runner, "outputFileName", Output);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) AssetDatabase.DeleteAsset(ScenePath);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("M2.1 scene save failed.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[147VR M2.1] Calibration scene built: " + ScenePath);
        }

        [MenuItem("147VR/AAA/Physics/M2.1/Run Real Stun Batch")]
        public static void RunRealBatch()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = Path.Combine(root, "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/stun_runtime_measurements.json");
            if (File.Exists(outputPath)) File.Delete(outputPath);
            BuildScene();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new InvalidOperationException("M2.1 scene invalid.");
            EditorApplication.isPlaying = true;
            double deadline = EditorApplication.timeSinceStartup + 120.0;
            bool finished = false;
            EditorApplication.update += Monitor;

            void Monitor()
            {
                if (finished) return;
                if (File.Exists(outputPath))
                {
                    string json = File.ReadAllText(outputPath);
                    var probe = JsonUtility.FromJson<M21MeasurementProbe>(json);
                    bool valid = probe != null && probe.repetitions == 5 &&
                                 probe.measuredCueSpeedAtContact != null && probe.measuredCueSpeedAtContact.Length == 5 &&
                                 probe.measuredCueResidualSpeed != null && probe.measuredCueResidualSpeed.Length == 5 &&
                                 probe.measuredObjectPeakSpeed != null && probe.measuredObjectPeakSpeed.Length == 5 &&
                                 probe.passed != null && probe.passed.Length == 5;
                    if (!valid) return;
                    finished = true;
                    EditorApplication.update -= Monitor;
                    bool allPass = true;
                    for (int i = 0; i < probe.passed.Length; i++) allPass &= probe.passed[i];
                    Debug.Log($"[147VR M2.1] REAL BATCH COMPLETE | reps={probe.repetitions} | allStunPass={allPass} | output={outputPath}");
                    EditorApplication.Exit(allPass ? 0 : 1);
                    return;
                }

                if (EditorApplication.timeSinceStartup >= deadline)
                {
                    finished = true;
                    EditorApplication.update -= Monitor;
                    Debug.LogError("[147VR M2.1] REAL batch timeout: stun_runtime_measurements.json was not persisted within 120s.");
                    EditorApplication.Exit(1);
                }
            }
        }

        [Serializable]
        private sealed class M21MeasurementProbe
        {
            public int repetitions;
            public float[] measuredCueSpeedAtContact;
            public float[] measuredCueResidualSpeed;
            public float[] measuredObjectPeakSpeed;
            public bool[] passed;
        }
        [MenuItem("147VR/AAA/Physics/M2.1/Validate Scene")]
        public static void ValidateScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new InvalidOperationException("M2.1 scene invalid.");
            var cue = GameObject.Find("Sphere.009")?.GetComponent<Rigidbody>();
            var target = GameObject.Find("Red")?.GetComponent<Rigidbody>();
            var table = GameObject.Find("PREFAB SNOOKER table");
            var bed = table != null ? table.GetComponentInChildren<BoxCollider>(true) : null;
            if (cue == null || target == null || bed == null || bed.name != "Bed_Collider") throw new InvalidOperationException("M2.1 deterministic scene validation failed.");
            float gap = Vector3.Distance(cue.position, target.position);
            if (gap < 0.05f) throw new InvalidOperationException("M2.1 cue/object gap is unsafe.");
            Debug.Log($"[147VR M2.1] Scene VALID | cue=Sphere.009 | object=Red | Bed_Collider | gap={gap:F4}m");
        }

        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) throw new InvalidOperationException($"Serialized field not found: {target.GetType().Name}.{field}");
            if (value is UnityEngine.Object o) prop.objectReferenceValue = o;
            else if (value is int i) prop.intValue = i;
            else if (value is float f) prop.floatValue = f;
            else if (value is string s) prop.stringValue = s;
            else throw new InvalidOperationException("Unsupported value: " + value.GetType().Name);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}













