using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VR147.AAA.Physics;
using VR147.AAA.Physics.Golden;

namespace VR147.AAA.Editor
{
    public static class M3CushionGoldenCertificationAutomation
    {
        private const string Root = "Assets/AAA/PhysicsCalibration";
        private const string RuntimeDir = Root + "/RuntimeMeasurements";
        private const string GoldenDir = Root + "/Golden";
        private const string CatalogPath = GoldenDir + "/147VR_GoldenCatalog.asset";
        private static readonly CaseSpec[] Specs =
        {
            new("m3_straight0_runtime_measurements.json", "147VR-PHY-007", "M3.1 Cushion 0°", "M3_Case_007", false),
            new("m3_angle30_runtime_measurements.json", "147VR-PHY-008", "M3.2 Cushion 30°", "M3_Case_008", false),
            new("m3_angle45_runtime_measurements.json", "147VR-PHY-009", "M3.3 Cushion 45°", "M3_Case_009", false),
            new("m3_angle60_runtime_measurements.json", "147VR-PHY-010", "M3.4 Cushion 60°", "M3_Case_010", false),
            new("m3_angle90_runtime_measurements.json", "147VR-PHY-011", "M3.5 Cushion 90°", "M3_Case_011", false),
            new("m3_english_runtime_measurements.json", "147VR-PHY-012", "M3.6 Cushion + English", "M3_Case_012", false, true),
            new("m3_multiple_runtime_measurements.json", "147VR-PHY-013", "M3.7 Double Cushion", "M3_Case_013", true)
        };

        [MenuItem("147VR/AAA/Physics/M3/Promote REAL Cushion Goldens")]
        public static void PromoteAll()
        {
            EnsureFolders();
            var goldens = new List<PhysicsGoldenCase>();
            foreach (var spec in Specs)
            {
                MeasurementFile data = Load(spec.runtimeFile);
                Validate(data, spec);
                ShotCalibrationCase calibration = CreateOrUpdateCalibration(spec, data);
                PhysicsGoldenCase golden = CreateOrUpdateGolden(spec, data, calibration);
                goldens.Add(golden);
            }
            UpdateCatalogPreservingExisting(goldens);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[147VR M3 GOLDEN] PROMOTION PASS | cases={goldens.Count} | PHY-007..013 | REAL runtime JSON only");
            EditorApplication.Exit(0);
        }

        [MenuItem("147VR/AAA/Physics/M3/Regress REAL Cushion Goldens")]
        public static void RegressAll()
        {
            PhysicsGoldenCatalog catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("M3 regression catalog missing: " + CatalogPath);

            var goldenById = new Dictionary<string, PhysicsGoldenCase>(StringComparer.Ordinal);
            foreach (PhysicsGoldenCase item in catalog.Cases)
                if (item != null) goldenById[item.CaseId] = item;

            int totalSamples = 0;
            int passedSamples = 0;
            foreach (var spec in Specs)
            {
                if (!goldenById.TryGetValue(spec.caseId, out PhysicsGoldenCase golden))
                    throw new InvalidOperationException("Golden missing from catalog: " + spec.caseId);
                MeasurementFile data = Load(spec.runtimeFile);
                Validate(data, spec);
                var go = new GameObject("M3_Regression_" + spec.caseId);
                try
                {
                    var runner = go.AddComponent<PhysicsGoldenRegressionRunner>();
                    SetObject(runner, "catalog", catalog);
                    for (int i = 0; i < data.repetitions; i++)
                    {
                        if (!runner.RecordMeasurement(golden, data.samples[i].travel, data.samples[i].postSpeed))
                            throw new InvalidOperationException($"Cannot record {spec.caseId} repetition {i + 1}");
                        PhysicsGoldenRegressionReport report = runner.Run();
                        bool pass = report.total >= 1 && report.passed >= 1 && report.failed == 0 && report.invalid == 0 && runner.LastCatalogValid;
                        totalSamples++;
                        if (pass) passedSamples++;
                        Debug.Log($"[147VR M3 REGRESSION] {spec.caseId} rep={i + 1}/{data.repetitions} {(pass ? "PASS" : "FAIL")} | travel={data.samples[i].travel:F6} | postSpeed={data.samples[i].postSpeed:F6}");
                        if (!pass) throw new InvalidOperationException("Regression failed: " + spec.caseId);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }

            bool allPass = totalSamples == passedSamples && totalSamples == Specs.Length * 5;
            Debug.Log($"[147VR M3 REGRESSION] {(allPass ? "PASS" : "FAIL")} | samples={passedSamples}/{totalSamples}");
            if (!allPass) throw new InvalidOperationException("M3 regression did not certify all samples.");
            EditorApplication.Exit(0);
        }

        private static ShotCalibrationCase CreateOrUpdateCalibration(CaseSpec spec, MeasurementFile data)
        {
            float distance = MeanTravel(data);
            float speed = MeanPostSpeed(data);
            string path = Root + "/" + spec.calibrationAssetName + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<ShotCalibrationCase>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ShotCalibrationCase>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.type = ShotCalibrationType.Cushion;
            asset.power = 0.5f;
            asset.side = spec.hasEnglish ? 0.5f : 0f;
            asset.vertical = 0f;
            asset.expectedDistance = distance;
            asset.tolerance = Mathf.Max(0.001f, distance * 0.01f);
            asset.expectedPeakSpeed = speed;
            asset.peakSpeedTolerance = Mathf.Max(0.01f, speed * 0.01f);
            asset.validatePeakSpeed = true;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static PhysicsGoldenCase CreateOrUpdateGolden(CaseSpec spec, MeasurementFile data, ShotCalibrationCase calibration)
        {
            string path = GoldenDir + "/" + spec.caseId + ".asset";
            var golden = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>(path);
            if (golden == null)
            {
                golden = ScriptableObject.CreateInstance<PhysicsGoldenCase>();
                AssetDatabase.CreateAsset(golden, path);
            }
            SetString(golden, "caseId", spec.caseId);
            SetInt(golden, "revision", 1);
            SetEnum(golden, "category", (int)PhysicsGoldenCategory.Cushion);
            SetString(golden, "intent", spec.label + ". REAL runtime PhysX evidence; Golden stores measured distance/post-speed oracle.");
            SetObject(golden, "calibrationCase", calibration);
            SetBool(golden, "requirePass", true);
            SetString(golden, "sourceJsonPath", RuntimeDir + "/" + spec.runtimeFile);
            SetString(golden, "sourceTimestampUtc", data.timestampUtc ?? string.Empty);
            SetString(golden, "sourceScene", data.scene ?? string.Empty);
            SetString(golden, "sourceUnityVersion", data.unityVersion ?? Application.unityVersion);
            SetString(golden, "sourceShotType", data.caseType ?? spec.label);
            SetInt(golden, "sourceRepetitionCount", data.repetitions);
            SetFloat(golden, "measuredDistanceMean", MeanTravel(data));
            SetFloat(golden, "measuredDistanceStdDev", StdDevTravel(data));
            SetFloat(golden, "measuredPeakSpeedMean", MeanPostSpeed(data));
            SetFloat(golden, "measuredPeakSpeedStdDev", StdDevPostSpeed(data));
            SetFloat(golden, "regressionTolerancePercent", 1f);
            SetFloatArray(golden, "measuredDistanceSamples", ExtractTravel(data));
            SetFloatArray(golden, "measuredPeakSpeedSamples", ExtractPostSpeed(data));
            EditorUtility.SetDirty(golden);
            if (!golden.IsValid()) throw new InvalidOperationException("Generated Golden invalid: " + spec.caseId);
            return golden;
        }
        private static MeasurementFile Load(string fileName)
        {
            string path = Path.Combine(Application.dataPath, "AAA/PhysicsCalibration/RuntimeMeasurements", fileName);
            if (!File.Exists(path)) throw new FileNotFoundException("M3 runtime JSON missing", path);
            var data = JsonUtility.FromJson<MeasurementFile>(File.ReadAllText(path));
            if (data == null) throw new InvalidOperationException("Could not parse M3 JSON: " + fileName);
            return data;
        }

        private static void Validate(MeasurementFile data, CaseSpec spec)
        {
            if (data.repetitions != 5 || data.samples == null || data.samples.Length != 5)
                throw new InvalidOperationException($"{spec.caseId}: expected exactly 5 runtime samples.");
            int required = spec.doubleCushion ? 2 : 1;
            for (int i = 0; i < data.samples.Length; i++)
            {
                Sample s = data.samples[i];
                if (!s.pass || s.cushionHits < required || s.travel <= 0f || s.postSpeed < 0f)
                    throw new InvalidOperationException($"{spec.caseId}: invalid REAL sample {i + 1}.");
                if (float.IsNaN(s.travel) || float.IsInfinity(s.travel) || float.IsNaN(s.postSpeed) || float.IsInfinity(s.postSpeed))
                    throw new InvalidOperationException($"{spec.caseId}: non-finite runtime measurement.");
            }
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/AAA")) AssetDatabase.CreateFolder("Assets", "AAA");
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/AAA", "PhysicsCalibration");
            if (!AssetDatabase.IsValidFolder(GoldenDir)) AssetDatabase.CreateFolder(Root, "Golden");
        }

        private static void UpdateCatalogPreservingExisting(List<PhysicsGoldenCase> additions)
        {
            PhysicsGoldenCatalog catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PhysicsGoldenCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var all = new List<PhysicsGoldenCase>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (PhysicsGoldenCase item in catalog.Cases)
                if (item != null && seen.Add(item.CaseId)) all.Add(item);

            string[] guids = AssetDatabase.FindAssets("t:PhysicsGoldenCase", new[] { GoldenDir });
            foreach (string guid in guids)
            {
                var item = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null && seen.Add(item.CaseId)) all.Add(item);
            }
            foreach (PhysicsGoldenCase item in additions)
                if (item != null && seen.Add(item.CaseId)) all.Add(item);

            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("cases");
            list.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            if (!catalog.ValidateCatalog(out string error)) throw new InvalidOperationException("Catalog invalid after M3 promotion: " + error);
        }

        private static float[] ExtractTravel(MeasurementFile d) { var a = new float[d.samples.Length]; for (int i = 0; i < a.Length; i++) a[i] = d.samples[i].travel; return a; }
        private static float[] ExtractPostSpeed(MeasurementFile d) { var a = new float[d.samples.Length]; for (int i = 0; i < a.Length; i++) a[i] = d.samples[i].postSpeed; return a; }
        private static float MeanTravel(MeasurementFile d) => Mean(ExtractTravel(d));
        private static float MeanPostSpeed(MeasurementFile d) => Mean(ExtractPostSpeed(d));
        private static float Mean(float[] a) { double sum = 0; foreach (float x in a) sum += x; return (float)(sum / a.Length); }
        private static float StdDev(float[] a, float mean) { float sum = 0; foreach (float x in a) { float d = x - mean; sum += d * d; } return Mathf.Sqrt(sum / a.Length); }
        private static float StdDevTravel(MeasurementFile d) => StdDev(ExtractTravel(d), MeanTravel(d));
        private static float StdDevPostSpeed(MeasurementFile d) => StdDev(ExtractPostSpeed(d), MeanPostSpeed(d));

        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value) { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetString(UnityEngine.Object target, string field, string value) { var so = new SerializedObject(target); so.FindProperty(field).stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetInt(UnityEngine.Object target, string field, int value) { var so = new SerializedObject(target); so.FindProperty(field).intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetFloat(UnityEngine.Object target, string field, float value) { var so = new SerializedObject(target); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetBool(UnityEngine.Object target, string field, bool value) { var so = new SerializedObject(target); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetEnum(UnityEngine.Object target, string field, int value) { var so = new SerializedObject(target); so.FindProperty(field).enumValueIndex = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetFloatArray(UnityEngine.Object target, string field, float[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) throw new InvalidOperationException($"Serialized field not found: {target.GetType().Name}.{field}");
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).floatValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Serializable] private sealed class CaseSpec
        {
            public readonly string runtimeFile, caseId, label, calibrationAssetName; public readonly bool doubleCushion, hasEnglish;
            public CaseSpec(string file, string id, string text, string calibration, bool multi, bool english = false) { runtimeFile = file; caseId = id; label = text; calibrationAssetName = calibration; doubleCushion = multi; hasEnglish = english; }
        }
        [Serializable] private sealed class MeasurementFile
        {
            public string timestampUtc, scene, unityVersion, caseType; public int repetitions; public float restitution, friction; public Sample[] samples;
        }
        [Serializable] private sealed class Sample
        {
            public float preSpeed, postSpeed, travel, directionDot; public int cushionHits; public bool pass;
        }
    }
}