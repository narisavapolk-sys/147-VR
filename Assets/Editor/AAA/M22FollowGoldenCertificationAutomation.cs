using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VR147.AAA.Physics;
using VR147.AAA.Physics.Golden;

namespace VR147.AAA.Editor
{
    public static class M22FollowGoldenCertificationAutomation
    {
        private const string RuntimeJson = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/follow_runtime_measurements.json";
        private const string GoldenPath = "Assets/AAA/PhysicsCalibration/Golden/147VR-PHY-003_Follow.asset";
        private const string CasePath = "Assets/AAA/PhysicsCalibration/Follow_Runtime_Case.asset";
        private const string CatalogPath = "Assets/AAA/PhysicsCalibration/Golden/147VR_GoldenCatalog.asset";
        private const float TolerancePercent = 0.5f;

        [Serializable] private sealed class Measurement
        {
            public string timestampUtc;
            public string caseType;
            public int repetitions;
            public float shotSpeed;
            public float followEnglish;
            public float[] cuePostContactSpeed;
            public float[] firstFlightDistance;
            public float[] objectPeakSpeed;
            public bool[] passed;
        }

        public static void BuildGolden()
        {
            var data = Load();
            Validate(data);
            float distanceMean = Mean(data.firstFlightDistance);
            float distanceStd = Std(data.firstFlightDistance, distanceMean);
            float speedMean = Mean(data.objectPeakSpeed);
            float speedStd = Std(data.objectPeakSpeed, speedMean);
            var calibrationCase = AssetDatabase.LoadAssetAtPath<ShotCalibrationCase>(CasePath);
            if (calibrationCase == null)
            {
                calibrationCase = ScriptableObject.CreateInstance<ShotCalibrationCase>();
                AssetDatabase.CreateAsset(calibrationCase, CasePath);
            }
            Set(calibrationCase, "type", (int)ShotCalibrationType.Follow);
            Set(calibrationCase, "power", 1f);
            Set(calibrationCase, "vertical", data.followEnglish);
            Set(calibrationCase, "expectedDistance", distanceMean);
            Set(calibrationCase, "tolerance", Mathf.Max(distanceMean * TolerancePercent / 100f, 0.000001f));
            Set(calibrationCase, "expectedPeakSpeed", speedMean);
            Set(calibrationCase, "peakSpeedTolerance", Mathf.Max(speedMean * TolerancePercent / 100f, 0.000001f));
            Set(calibrationCase, "validatePeakSpeed", true);
            var golden = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>(GoldenPath);
            if (golden == null)
            {
                golden = ScriptableObject.CreateInstance<PhysicsGoldenCase>();
                AssetDatabase.CreateAsset(golden, GoldenPath);
            }
            Set(golden, "caseId", "147VR-PHY-003");
            Set(golden, "revision", 1);
            Set(golden, "category", (int)PhysicsGoldenCategory.Follow);
            Set(golden, "intent", "Measured REAL Follow runtime truth. Golden distance is first-flight distance captured at cue/object contact; peak speed is object-ball peak speed. Follow-specific evidence additionally requires positive post-contact cue velocity aligned with the shot direction.");
            Set(golden, "calibrationCase", calibrationCase);
            Set(golden, "requirePass", true);
            Set(golden, "sourceJsonPath", RuntimeJson);
            Set(golden, "sourceTimestampUtc", data.timestampUtc ?? string.Empty);
            Set(golden, "sourceScene", "Assets/AAA/PhysicsCalibration/147VR_M22_FollowCalibration.unity");
            Set(golden, "sourceUnityVersion", Application.unityVersion);
            Set(golden, "sourceShotType", data.caseType ?? "Follow");
            Set(golden, "sourceRepetitionCount", data.repetitions);
            Set(golden, "measuredDistanceMean", distanceMean);
            Set(golden, "measuredDistanceStdDev", distanceStd);
            Set(golden, "measuredPeakSpeedMean", speedMean);
            Set(golden, "measuredPeakSpeedStdDev", speedStd);
            Set(golden, "regressionTolerancePercent", TolerancePercent);
            Set(golden, "measuredDistanceSamples", data.firstFlightDistance);
            Set(golden, "measuredPeakSpeedSamples", data.objectPeakSpeed);
            EditorUtility.SetDirty(calibrationCase); EditorUtility.SetDirty(golden);
            AssetDatabase.SaveAssets();
            UpdateCatalog(golden);
            Debug.Log($"[147VR M2.2 Golden] BUILD PASS | firstFlight mean={distanceMean:F9} std={distanceStd:F9} | peak mean={speedMean:F9} std={speedStd:F9} | postCue mean={Mean(data.cuePostContactSpeed):F9}");
        }
        public static void RunRegression()
        {
            var data = Load(); Validate(data);
            var golden = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>(GoldenPath);
            var catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (golden == null || catalog == null) throw new InvalidOperationException("M2.2 Golden or catalog missing.");
            if (!catalog.ValidateCatalog(out string error)) throw new InvalidOperationException(error);
            bool allPass = true; int passCount = 0;
            for (int i = 0; i < 5; i++)
            {
                var result = PhysicsGoldenEvaluator.Evaluate(golden, data.firstFlightDistance[i], data.objectPeakSpeed[i]);
                bool pass = result.passed && data.cuePostContactSpeed[i] >= 0.05f && data.passed[i];
                allPass &= pass; if (pass) passCount++;
                Debug.Log($"[147VR M2.2 Golden Regression] rep={i + 1}/5 {(pass ? "PASS" : "FAIL")} | firstFlight={data.firstFlightDistance[i]:F9} | peak={data.objectPeakSpeed[i]:F9} | postCue={data.cuePostContactSpeed[i]:F9}");
            }
            Debug.Log($"[147VR M2.2 Golden Regression] {(allPass ? "PASS" : "FAIL")} | {passCount}/5 | tolerance={TolerancePercent:F2}%");
            if (!allPass || passCount != 5) throw new InvalidOperationException("M2.2 Follow Golden regression failed.");
        }

        private static Measurement Load()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(root, RuntimeJson.Replace("/", "\\"));
            if (!File.Exists(path)) throw new FileNotFoundException("Follow runtime JSON missing", path);
            return JsonUtility.FromJson<Measurement>(File.ReadAllText(path));
        }

        private static void Validate(Measurement data)
        {
            if (data == null || data.repetitions != 5 || data.firstFlightDistance?.Length != 5 || data.objectPeakSpeed?.Length != 5 || data.cuePostContactSpeed?.Length != 5 || data.passed?.Length != 5)
                throw new InvalidOperationException("Follow runtime JSON must contain exactly 5 complete samples.");
            if (data.passed.Any(v => !v)) throw new InvalidOperationException("Follow runtime JSON contains a failed sample.");
            for (int i = 0; i < 5; i++) if (data.cuePostContactSpeed[i] < 0.05f) throw new InvalidOperationException("Follow runtime JSON lacks post-contact forward motion at sample " + i);
        }

        private static void UpdateCatalog(PhysicsGoldenCase golden)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<PhysicsGoldenCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            var so = new SerializedObject(catalog); var list = so.FindProperty("cases"); int index = -1;
            for (int i = 0; i < list.arraySize; i++)
            {
                var existing = list.GetArrayElementAtIndex(i).objectReferenceValue as PhysicsGoldenCase;
                if (existing != null && existing.CaseId == golden.CaseId) { index = i; break; }
            }
            if (index < 0) { index = list.arraySize; list.arraySize++; }
            list.GetArrayElementAtIndex(index).objectReferenceValue = golden;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            if (!catalog.ValidateCatalog(out string error)) throw new InvalidOperationException(error);
        }

        private static float Mean(float[] values) => values.Sum() / values.Length;
        private static float Std(float[] values, float mean)
        {
            float sum = 0f; for (int i = 0; i < values.Length; i++) { float d = values[i] - mean; sum += d * d; }
            return Mathf.Sqrt(sum / values.Length);
        }

        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var so = new SerializedObject(target); var prop = so.FindProperty(field);
            if (prop == null) throw new InvalidOperationException($"Serialized field missing: {target.GetType().Name}.{field}");
            if (value is string s) prop.stringValue = s;
            else if (value is int i) prop.intValue = i;
            else if (value is float f) prop.floatValue = f;
            else if (value is bool b) prop.boolValue = b;
            else if (value is UnityEngine.Object o) prop.objectReferenceValue = o;
            else if (value is float[] a) { prop.arraySize = a.Length; for (int j = 0; j < a.Length; j++) prop.GetArrayElementAtIndex(j).floatValue = a[j]; }
            else throw new InvalidOperationException("Unsupported serialized value: " + value?.GetType().Name);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
