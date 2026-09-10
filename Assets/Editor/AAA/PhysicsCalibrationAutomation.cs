using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Physics;

namespace VR147.AAA.Editor
{
    public static class PhysicsCalibrationAutomation
    {
        private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
        private const string RunMarkerPath = "Assets/AAA/PhysicsCalibration/.run_straight_batch";
        private const string SourceScene = "Assets/Scenes/PoolTable_8Ball.unity";
        private const string CasePath = "Assets/AAA/PhysicsCalibration/Straight_Runtime_Case.asset";

        // Explicit runtime entry point. It is intentionally NOT an InitializeOnLoad hook.
        // The external YOLO orchestrator launches a fresh Unity instance for this phase.
        public static void StartPreparedBatch()
        {
            // Runtime phase is explicitly launched by the external orchestrator.
            // The marker is advisory only; the saved scene is the authoritative handoff.
            if (System.IO.File.Exists(RunMarkerPath))
                System.IO.File.Delete(RunMarkerPath);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Calibration scene asset is missing: " + ScenePath);

            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new InvalidOperationException("Calibration scene could not be opened.");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("147VR/AAA/Physics/Build Calibration Scene")]
        public static void BuildCalibrationScene()
        {
            EnsureFolder("Assets/AAA");
            EnsureFolder("Assets/AAA/PhysicsCalibration");
            EnsureFolder("Assets/AAA/PhysicsCalibration/RuntimeMeasurements");

            var source = AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene);
            if (source == null) throw new InvalidOperationException("Calibration source scene not found.");
            var scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            PrepareSnookerCalibrationTable(scene);
            ConfigureCalibration(scene);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                AssetDatabase.DeleteAsset(ScenePath);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Calibration scene save failed: " + ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[147VR] Calibration scene ready: " + ScenePath);
        }

        [MenuItem("147VR/AAA/Physics/Run Straight Calibration Batch")]
        public static void RunStraightCalibrationBatch()
        {
            BuildCalibrationScene();
            System.IO.File.WriteAllText(RunMarkerPath, "prepared");
            AssetDatabase.SaveAssets();
            Debug.Log("[147VR] Calibration prepared. Phase 1 complete; external orchestrator must launch fresh runtime Unity.");
            EditorApplication.Exit(0);
        }

        private static void PrepareSnookerCalibrationTable(UnityEngine.SceneManagement.Scene scene)
        {
            var oldTable = GameObject.Find("PREFAB POoL table");
            if (oldTable == null) throw new InvalidOperationException("Pool calibration table root not found.");

            var sourceCueBall = FindCalibrationBall();
            if (sourceCueBall == null) throw new InvalidOperationException("Cue ball not found in source calibration scene.");

            // Clone only the cue ball before replacing the pool table. Do not detach
            // prefab children from their prefab instance; Unity correctly rejects that.
            var cueCloneObject = UnityEngine.Object.Instantiate(sourceCueBall.gameObject);
            cueCloneObject.name = "Sphere.009";
            cueCloneObject.transform.SetParent(null, true);
            var cueBall = cueCloneObject.GetComponent<Rigidbody>();
            if (cueBall == null) throw new InvalidOperationException("Cue ball clone lost its Rigidbody.");

            UnityEngine.Object.DestroyImmediate(oldTable);
            var snookerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AAA/ImportedSnooker/Prefab_WPBSA_12Foot_Snooker.prefab");
            if (snookerPrefab == null) throw new InvalidOperationException("Imported WPBSA snooker table prefab not found.");
            var table = PrefabUtility.InstantiatePrefab(snookerPrefab) as GameObject;
            table.name = "PREFAB SNOOKER table";
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(table);

            var bed = table.GetComponentInChildren<BoxCollider>(true);
            if (bed == null || bed.name != "Bed_Collider")
                throw new InvalidOperationException("WPBSA snooker table Bed_Collider not found.");

            cueBall.position = new Vector3(bed.bounds.center.x, bed.bounds.max.y + 0.02625f, bed.bounds.center.z - 1.0f);
            cueBall.linearVelocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
            cueBall.isKinematic = false;
            cueBall.detectCollisions = true;

            Debug.Log($"[147VR Calibration] Snooker table prepared: playingArea={bed.bounds.size.x:F3} x {bed.bounds.size.z:F3} cue={cueBall.name} cuePos={cueBall.position}");
        }

        private static void ConfigureCalibration(UnityEngine.SceneManagement.Scene scene)
        {
            var ball = FindCalibrationBall();
            if (ball == null) throw new InvalidOperationException("No suitable cue-ball Rigidbody found.");

            var table = GameObject.Find("PREFAB SNOOKER table");
            if (table == null) throw new InvalidOperationException("Snooker calibration table root not found.");
            var physicsSetup = table.GetComponent<SnookerPhysicsSetup>();
            if (physicsSetup == null) physicsSetup = table.AddComponent<SnookerPhysicsSetup>();
            physicsSetup.tableRoot = table.transform;
            // Calibration authority: the WPBSA prefab Bed_Collider is the physics surface.\n            // Never select a visual renderer for this certified scene.\n            physicsSetup.tableSurfaceName = "__CALIBRATION_BED_COLLIDER__";
            physicsSetup.ballRadius = 0.02625f;
            physicsSetup.ballMass = 0.14f;
            physicsSetup.autoSetupOnStart = true;

            var caseAsset = AssetDatabase.LoadAssetAtPath<ShotCalibrationCase>(CasePath);
            if (caseAsset == null)
            {
                caseAsset = ScriptableObject.CreateInstance<ShotCalibrationCase>();
                caseAsset.type = ShotCalibrationType.Straight;
                caseAsset.power = 0.5f;
                caseAsset.expectedDistance = 1f;
                caseAsset.tolerance = 0.05f;
                AssetDatabase.CreateAsset(caseAsset, CasePath);
            }

            foreach (Rigidbody candidate in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (candidate == null || candidate == ball) continue;
                var sphere = candidate.GetComponent<SphereCollider>();
                if (sphere == null || sphere.radius < 0.02f || sphere.radius > 0.04f) continue;
                sphere.enabled = false;
                candidate.linearVelocity = Vector3.zero;
                candidate.angularVelocity = Vector3.zero;
                candidate.isKinematic = true;
                candidate.detectCollisions = false;
            }

            var root = new GameObject("147VR_RuntimeCalibration");
            var origin = new GameObject("ShotOrigin").transform;
            origin.SetParent(root.transform, false);
            origin.position = ball.position - Vector3.forward * 0.25f;
            origin.forward = Vector3.forward;

            var cueController = root.AddComponent<SnookerCueController>();
            var physicsAdapter = root.AddComponent<VR147.AAA.Cue.CuePhysicsAdapter>();
            SetObject(physicsAdapter, "cueBall", ball);
            SetObject(cueController, "physicsAdapter", physicsAdapter);

            var tracker = root.AddComponent<ShotMeasurementTracker>();
            SetObject(tracker, "targetBall", ball);
            SetObject(tracker, "measurementOrigin", ball.transform);

            var controller = root.AddComponent<CalibrationShotController>();
            SetObject(controller, "targetBall", ball);
            SetObject(controller, "tracker", tracker);
            SetObject(controller, "calibrationCase", caseAsset);
            SetObject(controller, "shotOrigin", origin);
            SetObject(controller, "physicsAdapter", physicsAdapter);
            SetObject(controller, "physicsSetup", physicsSetup);
            SetValue(controller, "resetPosition", ball.position);

            var runner = root.AddComponent<CalibrationBatchRunner>();
            SetObject(runner, "shotController", controller);
            SetValue(runner, "repetitions", 5);
            SetValue(runner, "delayBetweenShots", 0.75f);
            SetValue(runner, "persistResults", true);
            SetValue(runner, "outputFileName", "straight_runtime_measurements.json");

            var bootstrap = root.AddComponent<CalibrationBatchBootstrap>();
            SetObject(bootstrap, "runner", runner);
            SetValue(bootstrap, "beginOnStart", true);

            Selection.activeGameObject = root;
        }

        private static Rigidbody FindCalibrationBall()
        {
            var cueObject = GameObject.Find("Sphere.009");
            if (cueObject != null)
            {
                var cueBody = cueObject.GetComponent<Rigidbody>();
                var cueSphere = cueObject.GetComponent<SphereCollider>();
                if (cueBody != null && !cueBody.isKinematic && cueSphere != null && cueSphere.radius > 0.02f && cueSphere.radius < 0.04f)
                    return cueBody;
            }

            foreach (var body in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (body == null || body.isKinematic) continue;
                var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(body.gameObject);
                bool isCueObject = sourceObject != null && sourceObject.name == "Sphere.009";
                var renderer = body.GetComponent<Renderer>();
                var material = renderer != null ? renderer.sharedMaterial : null;
                string materialPath = material != null ? AssetDatabase.GetAssetPath(material) : string.Empty;
                bool isCueMaterial = material != null && (material.name.Contains("ballCue", StringComparison.OrdinalIgnoreCase) || materialPath.Contains("ballCue", StringComparison.OrdinalIgnoreCase));
                if (!isCueObject && !isCueMaterial) continue;
                var sphere = body.GetComponent<SphereCollider>();
                if (sphere != null && sphere.radius > 0.02f && sphere.radius < 0.04f) return body;
            }

            foreach (var body in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (body == null || body.isKinematic) continue;
                var sphere = body.GetComponent<SphereCollider>();
                if (sphere != null && sphere.radius > 0.02f && sphere.radius < 0.04f) return body;
            }
            return null;
        }

        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException($"Serialized field not found: {target.GetType().Name}.{field}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(UnityEngine.Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException($"Serialized field not found: {target.GetType().Name}.{field}");
            switch (value)
            {
                case bool b: property.boolValue = b; break;
                case int i: property.intValue = i; break;
                case float f: property.floatValue = f; break;
                case string s: property.stringValue = s; break;
                case Vector3 v: property.vector3Value = v; break;
                default: throw new InvalidOperationException($"Unsupported serialized value: {value.GetType().Name}");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}

