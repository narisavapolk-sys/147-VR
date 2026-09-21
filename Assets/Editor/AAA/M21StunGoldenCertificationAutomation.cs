using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VR147.AAA.Physics;
using VR147.AAA.Physics.Golden;

namespace VR147.AAA.Editor
{
    public static class M21StunGoldenCertificationAutomation
    {
        private const string RuntimeJson = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/stun_runtime_measurements.json";
        private const string GoldenPath = "Assets/AAA/PhysicsCalibration/Golden/147VR-PHY-002_Stun.asset";
        private const string CasePath = "Assets/AAA/PhysicsCalibration/Stun_Runtime_Case.asset";
        private const string CatalogPath = "Assets/AAA/PhysicsCalibration/Golden/147VR_GoldenCatalog.asset";
        private const float TolerancePercent = 0.5f;

        [Serializable]
        private sealed class Measurement
        {
            public string timestampUtc;
            public string caseType;
            public int repetitions;
            public float[] measuredObjectDistance;
            public float[] measuredObjectPeakSpeed;
            public bool[] passed;
        }

        [MenuItem("147VR/AAA/Physics/M2.1/Build Stun Golden From Runtime JSON")]
        public static void BuildGolden()
        {
            var data = Load();
            Validate(data);
            var calibrationCase = AssetDatabase.LoadAssetAtPath<ShotCalibrationCase>(CasePath);
            if (calibrationCase == null)
            {
                calibrationCase = ScriptableObject.CreateInstance<ShotCalibrationCase>();
                AssetDatabase.CreateAsset(calibrationCase, CasePath);
            }
            float distanceMean = Mean(data.measuredObjectDistance);
            float distanceStd = Std(data.measuredObjectDistance, distanceMean);
            float speedMean = Mean(data.measuredObjectPeakSpeed);
            float speedStd = Std(data.measuredObjectPeakSpeed, speedMean);
            Set(calibrationCase, "type", (int)ShotCalibrationType.Stun);
            Set(calibrationCase, "power", 1f);
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
            Set(golden, "caseId", "147VR-PHY-002");
            Set(golden, "revision", 1);
            Set(golden, "category", (int)PhysicsGoldenCategory.Stun);
            Set(golden, "intent", "Measured REAL Stun runtime truth. Values are derived only from stun_runtime_measurements.json; measuredDistance is final object-ball displacement and peakSpeed is measured object-ball peak speed.");
            Set(golden, "calibrationCase", calibrationCase);
            Set(golden, "requirePass", true);
            Set(golden, "sourceJsonPath", RuntimeJson);
            Set(golden, "sourceTimestampUtc", data.timestampUtc ?? string.Empty);
            Set(golden, "sourceScene", "Assets/AAA/PhysicsCalibration/147VR_M21_StunCalibration.unity");
            Set(golden, "sourceUnityVersion", Application.unityVersion);
            Set(golden, "sourceShotType", data.caseType ?? "Stun");
            Set(golden, "sourceRepetitionCount", data.repetitions);
            Set(golden, "measuredDistanceMean", distanceMean);
            Set(golden, "measuredDistanceStdDev", distanceStd);
            Set(golden, "measuredPeakSpeedMean", speedMean);
            Set(golden, "measuredPeakSpeedStdDev", speedStd);
            Set(golden, "regressionTolerancePercent", TolerancePercent);
            Set(golden, "measuredDistanceSamples", data.measuredObjectDistance);
            Set(golden, "measuredPeakSpeedSamples", data.measuredObjectPeakSpeed);
            EditorUtility.SetDirty(calibrationCase);
            EditorUtility.SetDirty(golden);
            AssetDatabase.SaveAssets();
            UpdateCatalog(golden);
            Verify(golden, data);
            Debug.Log($"[147VR M2.1 Golden] BUILD PASS | distance mean={distanceMean:F9} std={distanceStd:F9} | peak mean={speedMean:F9} std={speedStd:F9} | source={RuntimeJson}");
        }
        [MenuItem("147VR/AAA/Physics/M2.1/Run Stun Golden Regression")]
        public static void RunRegression()
        {
            var data = Load();
            Validate(data);
            var golden = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>(GoldenPath);
            var catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (golden == null || catalog == null) throw new InvalidOperationException("M2.1 Golden or catalog missing.");
            if (!catalog.ValidateCatalog(out string error)) throw new InvalidOperationException(error);
            bool allPass = true;
            int passCount = 0;
            for (int i = 0; i < data.repetitions; i++)
            {
                var result = PhysicsGoldenEvaluator.Evaluate(golden, data.measuredObjectDistance[i], data.measuredObjectPeakSpeed[i]);
                bool pass = result.passed;
                allPass &= pass;
                if (pass) passCount++;
                Debug.Log($"[147VR M2.1 Golden Regression] rep={i + 1}/{data.repetitions} {(pass ? "PASS" : "FAIL")} | distance={data.measuredObjectDistance[i]:F9} | peak={data.measuredObjectPeakSpeed[i]:F9}");
            }
            Debug.Log($"[147VR M2.1 Golden Regression] {(allPass ? "PASS" : "FAIL")} | {passCount}/{data.repetitions} | tolerance={TolerancePercent:F2}%");
            if (!allPass || passCount != data.repetitions) throw new InvalidOperationException("M2.1 Golden regression failed.");
        }

        private static Measurement Load()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(root, RuntimeJson.Replace("/", "\\"));
            if (!File.Exists(path)) throw new FileNotFoundException("Stun runtime JSON missing", path);
            return JsonUtility.FromJson<Measurement>(File.ReadAllText(path));
        }

        private static void Validate(Measurement data)
        {
            if (data == null || data.repetitions != 5 || data.measuredObjectDistance == null || data.measuredObjectPeakSpeed == null || data.passed == null)
                throw new InvalidOperationException("Invalid Stun runtime JSON.");
            if (data.measuredObjectDistance.Length != 5 || data.measuredObjectPeakSpeed.Length != 5 || data.passed.Length != 5)
                throw new InvalidOperationException("Stun runtime JSON must contain exactly 5 samples.");
            if (data.passed.Any(v => !v)) throw new InvalidOperationException("Stun runtime JSON contains a failed sample.");
        }

        private static void Verify(PhysicsGoldenCase golden, Measurement data)
        {
            if (golden.SourceJsonPath != RuntimeJson || golden.SourceRepetitionCount != 5 || golden.Category != PhysicsGoldenCategory.Stun)
                throw new InvalidOperationException("Stun Golden provenance/category mismatch.");
            for (int i = 0; i < 5; i++)
                if (golden.MeasuredDistanceSamples[i] != data.measuredObjectDistance[i] || golden.MeasuredPeakSpeedSamples[i] != data.measuredObjectPeakSpeed[i])
                    throw new InvalidOperationException("Stun Golden sample mismatch at " + i);
            if (!golden.IsValid()) throw new InvalidOperationException("Stun Golden IsValid() failed.");
        }
        private static void UpdateCatalog(PhysicsGoldenCase golden)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PhysicsGoldenCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var so = new SerializedObject(catalog);
            var list = so.FindProperty("cases");
            int index = -1;
            for (int i = 0; i < list.arraySize; i++)
            {
                var existing = list.GetArrayElementAtIndex(i).objectReferenceValue as PhysicsGoldenCase;
                if (existing != null && existing.CaseId == golden.CaseId) { index = i; break; }
            }
            if (index < 0) { index = list.arraySize; list.arraySize++; }
            list.GetArrayElementAtIndex(index).objectReferenceValue = golden;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            if (!catalog.ValidateCatalog(out string error)) throw new InvalidOperationException(error);
        }

        private static float Mean(float[] values) => values.Sum() / values.Length;

        private static float Std(float[] values, float mean)
        {
            float sum = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                float d = values[i] - mean;
                sum += d * d;
            }
            return Mathf.Sqrt(sum / values.Length);
        }

        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) throw new InvalidOperationException($"Serialized field missing: {target.GetType().Name}.{field}");
            if (value is string s) prop.stringValue = s;
            else if (value is int i) prop.intValue = i;
            else if (value is float f) prop.floatValue = f;
            else if (value is bool b) prop.boolValue = b;
            else if (value is UnityEngine.Object o) prop.objectReferenceValue = o;
            else if (value is float[] a)
            {
                prop.arraySize = a.Length;
                for (int j = 0; j < a.Length; j++) prop.GetArrayElementAtIndex(j).floatValue = a[j];
            }
            else throw new InvalidOperationException("Unsupported serialized value: " + value.GetType().Name);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
