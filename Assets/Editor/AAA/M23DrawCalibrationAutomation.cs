using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Physics;
using VR147.AAA.Physics.Golden;

namespace VR147.AAA.Editor
{
    public static class M23DrawCalibrationAutomation
    {
        private const string SourceScene = "Assets/AAA/PhysicsCalibration/147VR_M22_FollowCalibration.unity";
        private const string TargetScene = "Assets/AAA/PhysicsCalibration/147VR_M23_DrawCalibration.unity";
        private const string RuntimeJson = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/draw_runtime_measurements.json";
        private const string GoldenPath = "Assets/AAA/PhysicsCalibration/Golden/147VR-PHY-004_Draw.asset";
        private const string CasePath = "Assets/AAA/PhysicsCalibration/Draw_Runtime_Case.asset";
        private const string CatalogPath = "Assets/AAA/PhysicsCalibration/Golden/147VR_GoldenCatalog.asset";
        private const float TolerancePercent = 0.5f;
        private const float ExpectedDrawInput = -0.75f;

        [Serializable] private sealed class Measurement
        {
            public string timestampUtc;
            public string caseType;
            public int repetitions;
            public float shotSpeed;
            public float drawEnglish;
            public float[] cueSpeedAtContact;
            public Vector3[] cueVelocityAtContact;
            public Vector3[] cueVelocityAfterContact;
            public Vector3[] objectVelocityAtContact;
            public float[] objectPeakSpeed;
            public float[] firstFlightDistance;
            public float[] cuePostContactDistance;
            public float[] cuePostContactSpeed;
            public float[] drawDot;
            public bool[] passed;
        }

        [MenuItem("147VR/AAA/Physics/M2.3/Create Draw Calibration Scene")]
        public static void CreateScene()
        {
            if (!File.Exists(ProjectPath(SourceScene))) throw new FileNotFoundException("M2.2 Follow scene missing", ProjectPath(SourceScene));
            var scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            var old = UnityEngine.Object.FindFirstObjectByType<M22FollowBatchRunner>();
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var runner = UnityEngine.Object.FindFirstObjectByType<M23DrawBatchRunner>();
            if (runner == null)
            {
                var host = new GameObject("M23_DrawBatchRunner");
                runner = host.AddComponent<M23DrawBatchRunner>();
            }
            var so = new SerializedObject(runner);
            so.FindProperty("cueBall").objectReferenceValue = GameObject.Find("Red")?.GetComponent<Rigidbody>();
            so.FindProperty("objectBall").objectReferenceValue = GameObject.Find("Sphere.009")?.GetComponent<Rigidbody>();
            so.FindProperty("physicsAdapter").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<VR147.AAA.Cue.CuePhysicsAdapter>();
            so.FindProperty("physicsSetup").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<SnookerPhysicsSetup>();
            so.FindProperty("drawEnglish").floatValue = ExpectedDrawInput;
            so.FindProperty("repetitions").intValue = 5;
            so.FindProperty("shotSpeed").floatValue = 4f;
            so.FindProperty("outputFileName").stringValue = "draw_runtime_measurements.json";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, TargetScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[147VR M2.3] Scene created: " + TargetScene);
        }

        [MenuItem("147VR/AAA/Physics/M2.3/Run Real Draw x5")]
        public static void RunBatch()
        {
            if (!File.Exists(ProjectPath(TargetScene))) throw new FileNotFoundException("M2.3 Draw scene missing", ProjectPath(TargetScene));
            DeleteRuntimeJson();
            EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
            EditorApplication.update -= PollCompletion;
            EditorApplication.update += PollCompletion;
            EditorApplication.isPlaying = true;
            Debug.Log("[147VR M2.3] REAL DRAW BATCH START | input=-0.75 | repetitions=5");
        }

        private static void PollCompletion()
        {
            if (!File.Exists(ProjectPath(RuntimeJson))) return;
            EditorApplication.update -= PollCompletion;
            EditorApplication.isPlaying = false;
            Debug.Log("[147VR M2.3] REAL DRAW BATCH COMPLETE; JSON exists");
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        [MenuItem("147VR/AAA/Physics/M2.3/Build Draw Golden From Runtime JSON")]
        public static void BuildGolden()
        {
            var data = Load();
            Validate(data);
            float distanceMean = Mean(data.firstFlightDistance);
            float distanceStd = Std(data.firstFlightDistance, distanceMean);
            float speedMean = Mean(data.objectPeakSpeed);
            float speedStd = Std(data.objectPeakSpeed, speedMean);
            var calibrationCase = AssetDatabase.LoadAssetAtPath<ShotCalibrationCase>(CasePath);
            if (calibrationCase == null) { calibrationCase = ScriptableObject.CreateInstance<ShotCalibrationCase>(); AssetDatabase.CreateAsset(calibrationCase, CasePath); }
            Set(calibrationCase, "type", (int)ShotCalibrationType.Draw);
            Set(calibrationCase, "power", 1f);
            Set(calibrationCase, "vertical", data.drawEnglish);
            Set(calibrationCase, "expectedDistance", distanceMean);
            Set(calibrationCase, "tolerance", Mathf.Max(distanceMean * TolerancePercent / 100f, 0.000001f));
            Set(calibrationCase, "expectedPeakSpeed", speedMean);
            Set(calibrationCase, "peakSpeedTolerance", Mathf.Max(speedMean * TolerancePercent / 100f, 0.000001f));
            Set(calibrationCase, "validatePeakSpeed", true);
            var golden = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>(GoldenPath);
            if (golden == null) { golden = ScriptableObject.CreateInstance<PhysicsGoldenCase>(); AssetDatabase.CreateAsset(golden, GoldenPath); }
            Set(golden, "caseId", "147VR-PHY-004");
            Set(golden, "revision", 1);
            Set(golden, "category", (int)PhysicsGoldenCategory.Draw);
            Set(golden, "intent", "Measured REAL Draw runtime truth. Golden distance is object-ball displacement measured after a deterministic 10-step post-contact observation window; peak speed is measured object-ball peak speed. Draw-specific evidence requires negative post-contact cue velocity aligned opposite to the shot direction.");
            Set(golden, "calibrationCase", calibrationCase);
            Set(golden, "requirePass", true);
            Set(golden, "sourceJsonPath", RuntimeJson);
            Set(golden, "sourceTimestampUtc", data.timestampUtc ?? string.Empty);
            Set(golden, "sourceScene", TargetScene);
            Set(golden, "sourceUnityVersion", Application.unityVersion);
            Set(golden, "sourceShotType", data.caseType ?? "Draw");
            Set(golden, "sourceRepetitionCount", data.repetitions);
            Set(golden, "measuredDistanceMean", distanceMean);
            Set(golden, "measuredDistanceStdDev", distanceStd);
            Set(golden, "measuredPeakSpeedMean", speedMean);
            Set(golden, "measuredPeakSpeedStdDev", speedStd);
            Set(golden, "regressionTolerancePercent", TolerancePercent);
            Set(golden, "measuredDistanceSamples", data.firstFlightDistance);
            Set(golden, "measuredPeakSpeedSamples", data.objectPeakSpeed);
            EditorUtility.SetDirty(calibrationCase); EditorUtility.SetDirty(golden); AssetDatabase.SaveAssets();
            UpdateCatalog(golden);
            Verify(golden, data);
            Debug.Log($"[147VR M2.3 Golden] BUILD PASS | drawInput={data.drawEnglish:F3} | firstFlight mean={distanceMean:F9} std={distanceStd:F9} | peak mean={speedMean:F9} std={speedStd:F9} | postCue mean={Mean(data.cuePostContactSpeed):F9} | dot mean={Mean(data.drawDot):F9}");
        }

        [MenuItem("147VR/AAA/Physics/M2.3/Run Draw Golden Regression")]
        public static void RunRegression()
        {
            var data = Load(); Validate(data);
            var golden = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>(GoldenPath);
            var catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (golden == null || catalog == null) throw new InvalidOperationException("M2.3 Golden or catalog missing.");
            if (!catalog.ValidateCatalog(out string error)) throw new InvalidOperationException(error);
            bool allPass = true; int passCount = 0;
            for (int i = 0; i < 5; i++)
            {
                var result = PhysicsGoldenEvaluator.Evaluate(golden, data.firstFlightDistance[i], data.objectPeakSpeed[i]);
                bool pass = result.passed && data.cuePostContactSpeed[i] >= 0.05f && data.drawDot[i] <= -0.5f && data.passed[i];
                allPass &= pass; if (pass) passCount++;
                Debug.Log($"[147VR M2.3 Golden Regression] rep={i + 1}/5 {(pass ? "PASS" : "FAIL")} | firstFlight={data.firstFlightDistance[i]:F9} | peak={data.objectPeakSpeed[i]:F9} | postCue={data.cuePostContactSpeed[i]:F9} | drawDot={data.drawDot[i]:F9}");
            }
            Debug.Log($"[147VR M2.3 Golden Regression] {(allPass ? "PASS" : "FAIL")} | {passCount}/5 | tolerance={TolerancePercent:F2}%");
            if (!allPass || passCount != 5) throw new InvalidOperationException("M2.3 Draw Golden regression failed.");
        }

        private static Measurement Load()
        {
            string path = ProjectPath(RuntimeJson);
            if (!File.Exists(path)) throw new FileNotFoundException("Draw runtime JSON missing", path);
            return JsonUtility.FromJson<Measurement>(File.ReadAllText(path));
        }

        private static void Validate(Measurement data)
        {
            if (data == null || data.repetitions != 5 || data.firstFlightDistance?.Length != 5 || data.objectPeakSpeed?.Length != 5 || data.cuePostContactSpeed?.Length != 5 || data.drawDot?.Length != 5 || data.passed?.Length != 5)
                throw new InvalidOperationException("Draw runtime JSON must contain exactly 5 complete samples.");
            if (Mathf.Abs(data.drawEnglish - ExpectedDrawInput) > 0.0001f) throw new InvalidOperationException($"Draw input mismatch: expected {ExpectedDrawInput}, got {data.drawEnglish}");
            if (!string.Equals(data.caseType, "Draw", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Draw runtime JSON caseType mismatch.");
            for (int i = 0; i < 5; i++)
            {
                if (!data.passed[i]) throw new InvalidOperationException("Draw runtime JSON contains a failed sample at " + i);
                if (data.cuePostContactSpeed[i] < 0.05f) throw new InvalidOperationException("Draw runtime JSON lacks post-contact cue motion at sample " + i);
                if (data.drawDot[i] >= -0.5f) throw new InvalidOperationException("Draw runtime JSON lacks reverse direction at sample " + i);
            }
        }

        private static void Verify(PhysicsGoldenCase golden, Measurement data)
        {
            if (golden.CaseId != "147VR-PHY-004" || golden.Category != PhysicsGoldenCategory.Draw || golden.SourceJsonPath != RuntimeJson || golden.SourceRepetitionCount != 5)
                throw new InvalidOperationException("Draw Golden provenance/category mismatch.");
            for (int i = 0; i < 5; i++) if (golden.MeasuredDistanceSamples[i] != data.firstFlightDistance[i] || golden.MeasuredPeakSpeedSamples[i] != data.objectPeakSpeed[i]) throw new InvalidOperationException("Draw Golden sample mismatch at " + i);
            if (!golden.IsValid()) throw new InvalidOperationException("Draw Golden IsValid() failed.");
        }

        private static void UpdateCatalog(PhysicsGoldenCase golden)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<PhysicsGoldenCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            var so = new SerializedObject(catalog); var list = so.FindProperty("cases"); int index = -1;
            for (int i = 0; i < list.arraySize; i++) { var existing = list.GetArrayElementAtIndex(i).objectReferenceValue as PhysicsGoldenCase; if (existing != null && existing.CaseId == golden.CaseId) { index = i; break; } }
            if (index < 0) { index = list.arraySize; list.arraySize++; }
            list.GetArrayElementAtIndex(index).objectReferenceValue = golden; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            if (!catalog.ValidateCatalog(out string error)) throw new InvalidOperationException(error);
        }

        private static void DeleteRuntimeJson() { string path = ProjectPath(RuntimeJson); if (File.Exists(path)) File.Delete(path); }
        private static string ProjectPath(string assetPath) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace('/', Path.DirectorySeparatorChar));
        private static float Mean(float[] values) => values.Sum() / values.Length;
        private static float Std(float[] values, float mean) { float sum = 0f; for (int i = 0; i < values.Length; i++) { float d = values[i] - mean; sum += d * d; } return Mathf.Sqrt(sum / values.Length); }
        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var so = new SerializedObject(target); var prop = so.FindProperty(field); if (prop == null) throw new InvalidOperationException($"Serialized field missing: {target.GetType().Name}.{field}");
            if (value is string s) prop.stringValue = s; else if (value is int i) prop.intValue = i; else if (value is float f) prop.floatValue = f; else if (value is bool b) prop.boolValue = b; else if (value is UnityEngine.Object o) prop.objectReferenceValue = o; else if (value is float[] a) { prop.arraySize = a.Length; for (int j = 0; j < a.Length; j++) prop.GetArrayElementAtIndex(j).floatValue = a[j]; } else throw new InvalidOperationException("Unsupported serialized value: " + value?.GetType().Name);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

