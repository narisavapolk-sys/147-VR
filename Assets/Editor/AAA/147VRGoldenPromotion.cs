using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Physics;
using VR147.AAA.Physics.Golden;

namespace VR147.AAA.Editor
{
    public static class GoldenPromotion
    {
        private const string MeasurementPath = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/straight_runtime_measurements.json";
        private const string CasePath = "Assets/AAA/PhysicsCalibration/Straight_Runtime_Case.asset";
        private const string GoldenDir = "Assets/AAA/PhysicsCalibration/Golden";
        private const string GoldenCasePath = GoldenDir + "/147VR-PHY-001_Straight.asset";
        private const string CatalogPath = GoldenDir + "/147VR_GoldenCatalog.asset";

        [MenuItem("147VR/AAA/Physics/Promote Measured Straight Golden")]
        public static void PromoteMeasuredStraightGolden()
        {
            var data = JsonUtility.FromJson<MeasurementFile>(File.ReadAllText(MeasurementPath));
            if (data == null || data.repetitions < 1 || data.measuredDistance == null || data.measuredPeakSpeed == null)
                throw new InvalidOperationException("Measured Straight JSON is missing or invalid.");

            float distance = Mean(data.measuredDistance);
            float peakSpeed = Mean(data.measuredPeakSpeed);
            float maxDistanceDeviation = MaxDeviation(data.measuredDistance, distance);
            float maxSpeedDeviation = MaxDeviation(data.measuredPeakSpeed, peakSpeed);
            if (maxDistanceDeviation > 0.0001f || maxSpeedDeviation > 0.0001f)
                throw new InvalidOperationException($"Measured repetitions are not deterministic: distanceDev={maxDistanceDeviation}, speedDev={maxSpeedDeviation}");

            var calibrationCase = AssetDatabase.LoadAssetAtPath<ShotCalibrationCase>(CasePath);
            if (calibrationCase == null) throw new InvalidOperationException("Straight calibration case not found.");
            SetFloat(calibrationCase, "expectedDistance", distance);
            SetFloat(calibrationCase, "tolerance", Mathf.Max(0.001f, distance * 0.01f));
            SetFloat(calibrationCase, "expectedPeakSpeed", peakSpeed);
            SetFloat(calibrationCase, "peakSpeedTolerance", Mathf.Max(0.01f, peakSpeed * 0.01f));
            SetBool(calibrationCase, "validatePeakSpeed", true);

            EnsureFolder();
            var goldenCase = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>(GoldenCasePath);
            if (goldenCase == null)
            {
                goldenCase = ScriptableObject.CreateInstance<PhysicsGoldenCase>();
                AssetDatabase.CreateAsset(goldenCase, GoldenCasePath);
            }
            SetString(goldenCase, "caseId", "147VR-PHY-001");
            SetInt(goldenCase, "revision", 1);
            SetEnum(goldenCase, "category", (int)PhysicsGoldenCategory.Straight);
            SetString(goldenCase, "intent", "Measured REAL Straight Shot baseline from runtime PhysX calibration.");
            SetObject(goldenCase, "calibrationCase", calibrationCase);
            SetBool(goldenCase, "requirePass", true);

            var catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PhysicsGoldenCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var catSo = new SerializedObject(catalog);
            var cases = catSo.FindProperty("cases");
            cases.ClearArray();
            cases.InsertArrayElementAtIndex(0);
            cases.GetArrayElementAtIndex(0).objectReferenceValue = goldenCase;
            catSo.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var runnerObject = new GameObject("147VR_GoldenRegression_Verification");
            var runner = runnerObject.AddComponent<PhysicsGoldenRegressionRunner>();
            SetObject(runner, "catalog", catalog);
            bool recorded = runner.RecordMeasurement(goldenCase, distance, peakSpeed);
            if (!recorded) throw new InvalidOperationException("Measured Golden recording was rejected.");
            var report = runner.Run();
            Debug.Log($"[147VR Golden] Regression: total={report.total} passed={report.passed} failed={report.failed} invalid={report.invalid} coverage={report.coverageRate:F3} allCovered={report.allCasesCovered}");
            if (report.total != 1 || report.passed != 1 || report.failed != 0 || report.invalid != 0 || !report.allCasesCovered)
                throw new InvalidOperationException("Measured Golden Regression did not PASS.");
            Debug.Log("[147VR M1] MEASURED GOLDEN REGRESSION PASS");
            UnityEngine.Object.DestroyImmediate(runnerObject);
            EditorApplication.Exit(0);
        }

        private static float Mean(float[] values)
        {
            double sum = 0;
            foreach (float value in values) sum += value;
            return (float)(sum / values.Length);
        }
        private static float MaxDeviation(float[] values, float mean)
        {
            float max = 0f;
            foreach (float value in values) max = Mathf.Max(max, Mathf.Abs(value - mean));
            return max;
        }
        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(GoldenDir)) AssetDatabase.CreateFolder("Assets/AAA/PhysicsCalibration", "Golden");
        }
        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetString(UnityEngine.Object target, string field, string value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).stringValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetInt(UnityEngine.Object target, string field, int value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).intValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetFloat(UnityEngine.Object target, string field, float value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetBool(UnityEngine.Object target, string field, bool value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetEnum(UnityEngine.Object target, string field, int value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).enumValueIndex = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Serializable]
        private sealed class MeasurementFile
        {
            public int repetitions;
            public float[] measuredDistance;
            public float[] measuredPeakSpeed;
        }
    }
}
