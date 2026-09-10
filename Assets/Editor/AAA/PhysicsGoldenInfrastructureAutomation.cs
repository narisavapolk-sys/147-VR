using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VR147.AAA.Physics.Golden;

namespace VR147.AAA.Editor
{
    public static class PhysicsGoldenInfrastructureAutomation
    {
        private const string CatalogPath = "Assets/AAA/PhysicsCalibration/Golden/147VR_GoldenCatalog.asset";
        private const string DefaultInput = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/straight_regression_v2_measurements.json";
        private const string DefaultReport = "Assets/AAA/PhysicsCalibration/Golden/147VR_PhysicsGoldenReport.json";

        [MenuItem("147VR/AAA/Physics/Validate Golden Infrastructure")]
        public static void ValidateGoldenInfrastructure()
        {
            PhysicsGoldenCatalog catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Golden catalog not found: " + CatalogPath);
            if (!catalog.ValidateCatalog(out string error)) throw new InvalidOperationException(error);
            Debug.Log($"[147VR M1.5] Catalog VALID cases={catalog.Cases.Count}");
            for (int i = 0; i < catalog.Cases.Count; i++)
            {
                PhysicsGoldenCase item = catalog.Cases[i];
                Debug.Log($"[147VR M1.5] Case {i + 1}/{catalog.Cases.Count}: {item.CaseId} r{item.Revision} type={item.Category} reps={item.SourceRepetitionCount} tolerance={item.RegressionTolerancePercent:F3}%");
            }
            WriteCatalogReport(catalog, DefaultReport);
            Debug.Log("[147VR M1.5] INFRASTRUCTURE VALIDATION PASS");
            EditorApplication.Exit(0);
        }

        public static void RunHeadlessRegression()
        {
            string inputPath = Environment.GetEnvironmentVariable("VR147_GOLDEN_INPUT");
            if (string.IsNullOrWhiteSpace(inputPath)) inputPath = DefaultInput;
            string reportPath = Environment.GetEnvironmentVariable("VR147_GOLDEN_REPORT");
            if (string.IsNullOrWhiteSpace(reportPath)) reportPath = DefaultReport;
            RunRegression(inputPath, reportPath);
            EditorApplication.Exit(0);
        }

        private static void RunRegression(string inputPath, string reportPath)
        {
            PhysicsGoldenCatalog catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Golden catalog not found: " + CatalogPath);
            if (!catalog.ValidateCatalog(out string catalogError)) throw new InvalidOperationException(catalogError);
            if (!File.Exists(inputPath)) throw new FileNotFoundException("Golden regression input not found.", inputPath);
            RegressionInput input = JsonUtility.FromJson<RegressionInput>(File.ReadAllText(inputPath));
            if (input == null || input.samples == null || input.samples.Count == 0)
                throw new InvalidOperationException("Golden regression input contains no samples.");

            var output = new RegressionOutput { timestampUtc = DateTime.UtcNow.ToString("O"), inputPath = inputPath };
            int pass = 0;
            foreach (RegressionSample sample in input.samples)
            {
                if (!catalog.TryGetCase(sample.caseId, out PhysicsGoldenCase goldenCase))
                    throw new InvalidOperationException("Unknown Golden case: " + sample.caseId);
                PhysicsGoldenResult result = PhysicsGoldenEvaluator.Evaluate(goldenCase, sample.measuredDistance, sample.measuredPeakSpeed);
                output.results.Add(new RegressionResultRecord(result));
                if (result.passed) pass++;
                Debug.Log($"[147VR Golden CI] {sample.caseId} {(result.passed ? "PASS" : "FAIL")} distance={sample.measuredDistance:F9} peakSpeed={sample.measuredPeakSpeed:F9}");
            }
            output.total = output.results.Count;
            output.passed = pass;
            output.failed = output.total - pass;
            output.allPassed = output.total > 0 && output.failed == 0;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)) ?? ".");
            File.WriteAllText(reportPath, JsonUtility.ToJson(output, true));
            Debug.Log($"[147VR Golden CI] {(output.allPassed ? "PASS" : "FAIL")} total={output.total} passed={output.passed} failed={output.failed}");
            if (!output.allPassed) throw new InvalidOperationException("Headless Golden Regression FAILED.");
        }

        private static void WriteCatalogReport(PhysicsGoldenCatalog catalog, string path)
        {
            var report = new InfrastructureReport { generatedUtc = DateTime.UtcNow.ToString("O") };
            foreach (PhysicsGoldenCase item in catalog.Cases) report.cases.Add(new CaseRecord(item));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }

        [Serializable] private sealed class RegressionInput { public List<RegressionSample> samples = new(); }
        [Serializable] private sealed class RegressionSample { public string caseId; public float measuredDistance; public float measuredPeakSpeed; }
        [Serializable] private sealed class RegressionOutput { public string timestampUtc; public string inputPath; public int total; public int passed; public int failed; public bool allPassed; public List<RegressionResultRecord> results = new(); }
        [Serializable] private sealed class RegressionResultRecord
        {
            public string caseId; public int revision; public bool passed; public float distanceError; public float peakSpeedError;
            public RegressionResultRecord(PhysicsGoldenResult result) { caseId = result.caseId; revision = result.revision; passed = result.passed; distanceError = result.distanceError; peakSpeedError = result.peakSpeedError; }
        }
        [Serializable] private sealed class InfrastructureReport { public string generatedUtc; public List<CaseRecord> cases = new(); }
        [Serializable] private sealed class CaseRecord
        {
            public string caseId; public int revision; public string category; public string sourceJsonPath; public string sourceTimestampUtc; public string sourceScene; public string sourceUnityVersion; public string sourceShotType; public int repetitions; public float distanceMean; public float distanceStdDev; public float peakSpeedMean; public float peakSpeedStdDev; public float tolerancePercent;
            public CaseRecord(PhysicsGoldenCase item) { caseId=item.CaseId; revision=item.Revision; category=item.Category.ToString(); sourceJsonPath=item.SourceJsonPath; sourceTimestampUtc=item.SourceTimestampUtc; sourceScene=item.SourceScene; sourceUnityVersion=item.SourceUnityVersion; sourceShotType=item.SourceShotType; repetitions=item.SourceRepetitionCount; distanceMean=item.MeasuredDistanceMean; distanceStdDev=item.MeasuredDistanceStdDev; peakSpeedMean=item.MeasuredPeakSpeedMean; peakSpeedStdDev=item.MeasuredPeakSpeedStdDev; tolerancePercent=item.RegressionTolerancePercent; }
        }
    }
}