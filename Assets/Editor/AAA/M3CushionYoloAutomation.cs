using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Physics;

namespace VR147.AAA.Editor
{
    public static class M3CushionYoloAutomation
    {
        private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_M3_CushionCalibration.unity";
        private const string OutputDir = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements";
        private const string GoldenDir = "Assets/AAA/PhysicsCalibration/Golden";

        [MenuItem("147 VR/M3/YOLO Build + Run")]
        public static void BuildAndRun() => BuildSceneThenRun();

        public static void BuildSceneThenRun()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "AAA/PhysicsCalibration/RuntimeMeasurements"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "AAA/PhysicsCalibration/Golden"));
            var scene = new M3SceneBuilder().Build(ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void RunOnly()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "AAA/PhysicsCalibration/147VR_M3_CushionCalibration.unity")))
                BuildSceneThenRun();
            else
            {
                EditorSceneManager.OpenScene(ScenePath);
                EditorApplication.isPlaying = true;
            }
        }

        public static void FinalizeAndExit()
        {
            Debug.Log("[147VR M3] FINALIZE requested. Runtime data remains source-of-truth; no synthetic values are generated.");
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }

    internal sealed class M3SceneBuilder
    {
        public UnityEngine.SceneManagement.Scene Build(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var profile = ScriptableObject.CreateInstance<CushionProfile>();
            profile.restitution = 0.82f;
            profile.tangentialFriction = 0.035f;
            profile.spinTransfer = 0.35f;
            profile.minimumImpactSpeed = 0.02f;
            AssetDatabase.CreateAsset(profile, "Assets/AAA/PhysicsCalibration/M3_CushionProfile.asset");

            var root = new GameObject("M3_CushionCalibration");
            var runner = root.AddComponent<M3CushionRuntimeRunner>();
            runner.Configure(profile);
            CreateWall("Cushion_X_Pos", new Vector3(3f, 0.5f, 0f), new Vector3(0.2f, 1f, 8f), profile);
            CreateWall("Cushion_X_Neg", new Vector3(-3f, 0.5f, 0f), new Vector3(0.2f, 1f, 8f), profile);
            CreateWall("Cushion_Z_Pos", new Vector3(0f, 0.5f, 3f), new Vector3(8f, 1f, 0.2f), profile);
            CreateWall("Cushion_Z_Neg", new Vector3(0f, 0.5f, -3f), new Vector3(8f, 1f, 0.2f), profile);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "CalibrationSurface";
            floor.transform.position = new Vector3(0f, -0.25f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.5f, 8f);
            var rb = floor.AddComponent<Rigidbody>(); rb.isKinematic = true;
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "CalibrationBall";
            ball.transform.localScale = Vector3.one * 0.1f;
            var brb = ball.AddComponent<Rigidbody>();
            brb.mass = 0.142f; brb.linearDamping = 0.01f; brb.angularDamping = 0.02f;
            brb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            runner.ConfigureBall(brb); var ballResponder = ball.AddComponent<M3CushionCollisionResponder>(); ballResponder.Configure(profile);
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[147VR M3] Controlled cushion scene built.");
            return scene;
        }

        private static void CreateWall(string name, Vector3 pos, Vector3 scale, CushionProfile profile)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name; wall.transform.position = pos; wall.transform.localScale = scale;
            var rb = wall.AddComponent<Rigidbody>(); rb.isKinematic = true;
            
            var mat = new PhysicsMaterial("M3_NoBounce_NoFriction"); mat.bounciness = 0f; mat.dynamicFriction = 0f; mat.staticFriction = 0f; mat.frictionCombine = PhysicsMaterialCombine.Minimum; mat.bounceCombine = PhysicsMaterialCombine.Minimum;
            wall.GetComponent<Collider>().material = mat;
        }
    }
}