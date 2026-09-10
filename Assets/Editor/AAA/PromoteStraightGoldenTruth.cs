using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using VR147.AAA.Physics;
using VR147.AAA.Physics.Golden;

namespace VR147.AAA.Editor
{
    public static class PromoteStraightGoldenTruth
    {
        private const string Input = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/straight_runtime_measurements.json";
        private const string Dir = "Assets/AAA/PhysicsCalibration/Golden";
        private const string CasePath = Dir + "/147VR-PHY-001_Straight_r3_Calibration.asset";
        private const string GoldenPath = Dir + "/147VR-PHY-001_Straight_r3.asset";
        private const string CatalogPath = Dir + "/147VR_GoldenCatalog.asset";

        public static void Run()
        {
            var data = JsonUtility.FromJson<MeasurementFile>(File.ReadAllText(Input));
            if (data == null || data.repetitions <= 0 || data.measuredDistance == null ||
                data.measuredPeakSpeed == null || data.measuredDistance.Length != data.repetitions ||
                data.measuredPeakSpeed.Length != data.repetitions)
                throw new InvalidOperationException("Measured Straight runtime data is invalid/incomplete.");
            for (int i = 0; i < data.repetitions; i++)
                if (!IsFinitePositive(data.measuredDistance[i]) || !IsFinite(data.measuredPeakSpeed[i]))
                    throw new InvalidOperationException("Measured Truth contains invalid numeric data at sample " + i + ".");

            float distanceMean = Mean(data.measuredDistance);
            float distanceSd = StdDev(data.measuredDistance, distanceMean);
            float speedMean = Mean(data.measuredPeakSpeed);
            float speedSd = StdDev(data.measuredPeakSpeed, speedMean);
            EnsureFolder(Dir);

            var calibration = AssetDatabase.LoadAssetAtPath<ShotCalibrationCase>(CasePath);
            if (calibration == null)
            {
                calibration = ScriptableObject.CreateInstance<ShotCalibrationCase>();
                AssetDatabase.CreateAsset(calibration, CasePath);
            }
            calibration.type = ShotCalibrationType.Straight;
            calibration.power = 0.5f;
            calibration.expectedDistance = distanceMean;
            calibration.tolerance = Mathf.Max(distanceSd * 3f, distanceMean * 0.001f);
            calibration.expectedPeakSpeed = speedMean;
            calibration.peakSpeedTolerance = Mathf.Max(speedSd * 3f, speedMean * 0.001f);
            calibration.validatePeakSpeed = true;

            var golden = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>(GoldenPath);
            if (golden == null)
            {
                golden = ScriptableObject.CreateInstance<PhysicsGoldenCase>();
                AssetDatabase.CreateAsset(golden, GoldenPath);
            }
            SetGolden(golden, calibration, data, distanceMean, distanceSd, speedMean, speedSd);
            UpdateCatalog(golden);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[147VR Golden] Promoted REAL Straight Truth r3 | reps={data.repetitions} | distance={distanceMean:F9}±{distanceSd:F9} | peak={speedMean:F9}±{speedSd:F9}");
        }

        private static void SetGolden(PhysicsGoldenCase golden, ShotCalibrationCase calibration, MeasurementFile data,
            float distanceMean, float distanceSd, float speedMean, float speedSd)
        {
            var so = new SerializedObject(golden);
            so.FindProperty("caseId").stringValue = "147VR-PHY-001";
            so.FindProperty("revision").intValue = 3;
            so.FindProperty("category").enumValueIndex = (int)PhysicsGoldenCategory.Straight;
            so.FindProperty("intent").stringValue = "Measured REAL Straight Shot baseline from 2026-08-29 runtime PhysX simulation. No synthetic Truth.";
            so.FindProperty("calibrationCase").objectReferenceValue = calibration;
            so.FindProperty("requirePass").boolValue = true;
            so.FindProperty("sourceJsonPath").stringValue = Input;
            so.FindProperty("sourceTimestampUtc").stringValue = data.timestampUtc;
            so.FindProperty("sourceScene").stringValue = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
            so.FindProperty("sourceUnityVersion").stringValue = "6000.4.4f1";
            so.FindProperty("sourceShotType").stringValue = data.caseType;
            so.FindProperty("sourceRepetitionCount").intValue = data.repetitions;
            so.FindProperty("measuredDistanceMean").floatValue = distanceMean;
            so.FindProperty("measuredDistanceStdDev").floatValue = distanceSd;
            so.FindProperty("measuredPeakSpeedMean").floatValue = speedMean;
            so.FindProperty("measuredPeakSpeedStdDev").floatValue = speedSd;
            so.FindProperty("regressionTolerancePercent").floatValue = 0.5f;
            so.FindProperty("measuredDistanceSamples").arraySize = data.repetitions;
            so.FindProperty("measuredPeakSpeedSamples").arraySize = data.repetitions;
            for (int i = 0; i < data.repetitions; i++)
            {
                so.FindProperty("measuredDistanceSamples").GetArrayElementAtIndex(i).floatValue = data.measuredDistance[i];
                so.FindProperty("measuredPeakSpeedSamples").GetArrayElementAtIndex(i).floatValue = data.measuredPeakSpeed[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void UpdateCatalog(PhysicsGoldenCase golden)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Golden catalog missing: " + CatalogPath);
            var so = new SerializedObject(catalog);
            var cases = so.FindProperty("cases");
            cases.ClearArray();
            cases.InsertArrayElementAtIndex(0);
            cases.GetArrayElementAtIndex(0).objectReferenceValue = golden;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float Mean(float[] values)
        {
            double sum = 0d;
            for (int i = 0; i < values.Length; i++) sum += values[i];
            return (float)(sum / values.Length);
        }

        private static float StdDev(float[] values, float mean)
        {
            double sum = 0d;
            for (int i = 0; i < values.Length; i++)
            {
                double d = values[i] - mean;
                sum += d * d;
            }
            return (float)Math.Sqrt(sum / values.Length);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsFinitePositive(float value) => IsFinite(value) && value > 0f;

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        [Serializable]
        private sealed class MeasurementFile
        {
            public string timestampUtc;
            public string caseType;
            public int repetitions;
            public float[] measuredDistance;
            public float[] measuredPeakSpeed;
            public bool[] passed;
        }
    }
}
