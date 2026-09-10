using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using VR147.AAA.Physics.Golden;

namespace VR147.AAA.Editor
{
    public static class PhysicsGoldenReportGenerator
    {
        private const string CatalogPath = "Assets/AAA/PhysicsCalibration/Golden/147VR_GoldenCatalog.asset";
        private const string ReportPath = "Assets/AAA/PhysicsCalibration/Golden/147VR_PhysicsGoldenReport.md";

        [MenuItem("147VR/AAA/Physics/Generate Golden Report")]
        public static void GenerateReport()
        {
            PhysicsGoldenCatalog catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Golden catalog not found: " + CatalogPath);
            if (!catalog.ValidateCatalog(out string error)) throw new InvalidOperationException(error);
            StringBuilder md = new StringBuilder();
            md.AppendLine("# 147 VR Physics Golden Report");
            md.AppendLine();
            md.AppendLine($"Generated UTC: `{DateTime.UtcNow:O}`");
            md.AppendLine();
            md.AppendLine("## Infrastructure Status");
            md.AppendLine();
            md.AppendLine($"- Catalog: **PASS**");
            md.AppendLine($"- Golden cases: **{catalog.Cases.Count}**");
            md.AppendLine("- Provenance required: **YES**");
            md.AppendLine("- Headless regression entry point: **RunHeadlessRegression**");
            md.AppendLine();
            md.AppendLine("## Golden Cases");
            md.AppendLine();
            md.AppendLine("| Case | Revision | Type | Reps | Distance Mean | Distance SD | Peak Speed Mean | Peak Speed SD | Tolerance |");
            md.AppendLine("|---|---:|---|---:|---:|---:|---:|---:|---:|");
            for (int i = 0; i < catalog.Cases.Count; i++)
            {
                PhysicsGoldenCase item = catalog.Cases[i];
                md.AppendLine($"| {item.CaseId} | {item.Revision} | {item.Category} | {item.SourceRepetitionCount} | {item.MeasuredDistanceMean:F9} | {item.MeasuredDistanceStdDev:F9} | {item.MeasuredPeakSpeedMean:F9} | {item.MeasuredPeakSpeedStdDev:F9} | {item.RegressionTolerancePercent:F3}% |");
            }
            md.AppendLine();
            md.AppendLine("## Provenance");
            md.AppendLine();
            for (int i = 0; i < catalog.Cases.Count; i++)
            {
                PhysicsGoldenCase item = catalog.Cases[i];
                md.AppendLine($"### {item.CaseId} r{item.Revision}");
                md.AppendLine($"- Source JSON: `{item.SourceJsonPath}`");
                md.AppendLine($"- Timestamp UTC: `{item.SourceTimestampUtc}`");
                md.AppendLine($"- Scene: `{item.SourceScene}`");
                md.AppendLine($"- Unity: `{item.SourceUnityVersion}`");
                md.AppendLine($"- Shot type: `{item.SourceShotType}`");
                md.AppendLine();
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportPath)) ?? ".");
            File.WriteAllText(ReportPath, md.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[147VR M1.5] Human-readable Golden report generated: " + ReportPath);
            EditorApplication.Exit(0);
        }
    }
}