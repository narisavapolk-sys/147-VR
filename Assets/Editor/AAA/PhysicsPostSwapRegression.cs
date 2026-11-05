#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Physics.Golden;

// Post-swap Golden regression: opens the calibration scene, ENTERS PLAY MODE, runs the
// live shot executor for each catalog case (N reps), records real measurements, evaluates
// against the Golden catalog, writes a timestamped JSON report, then exits play mode.
// This is a REAL re-measure (not a stored-JSON proxy). Defensive: reports if scene not wired.
namespace VR147.AAA.Editor
{
    public static class PhysicsPostSwapRegression
    {
        const string CALIBRATION_SCENE = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
        const string CATALOG = "Assets/AAA/PhysicsCalibration/Golden/147VR_GoldenCatalog.asset";

        [MenuItem("147VR/AAA/Physics/Post-Swap Golden Regression (live re-measure)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("147VR/PostSwap: already in play mode. Stop first.");
                return;
            }
            if (!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, CALIBRATION_SCENE.Replace('/', Path.DirectorySeparatorChar))))
            {
                Debug.LogError("147VR/PostSwap: calibration scene not found: " + CALIBRATION_SCENE);
                return;
            }
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(CALIBRATION_SCENE, OpenSceneMode.Single);

            var executor = UnityEngine.Object.FindObjectOfType<PhysicsGoldenShotExecutor>();
            var runner = UnityEngine.Object.FindObjectOfType<PhysicsGoldenRegressionRunner>();
            var catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(CATALOG);
            if (executor == null || runner == null || catalog == null || catalog.Cases == null || catalog.Cases.Count == 0)
            {
                Debug.LogError("147VR/PostSwap: scene not wired for live regression. Need PhysicsGoldenShotExecutor + PhysicsGoldenRegressionRunner in the calibration scene + a non-empty catalog. executor=" + (executor != null) + " runner=" + (runner != null) + " catalogCases=" + (catalog?.Cases?.Count ?? -1));
                return;
            }
            var driver = new GameObject("147VR_PostSwapRegressionDriver").AddComponent<PostSwapRegressionDriver>();
            driver.catalogPath = CATALOG;
            driver.reps = 5;
            EditorApplication.playModeStateChanged += OnMode;
            EditorApplication.isPlaying = true;
        }

        static void OnMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.ExitingPlayMode) EditorApplication.playModeStateChanged -= OnMode;
        }
    }

    public sealed class PostSwapRegressionDriver : MonoBehaviour
    {
        public string catalogPath;
        public int reps = 5;
        int caseIdx, rep;
        float waitTimer;
        bool started;

        void Update()
        {
            if (!Application.isPlaying) return;
            if (!started) { if (Time.frameCount < 5) return; started = true; }
            if (waitTimer > 0f) { waitTimer -= Time.deltaTime; return; }

            var executor = FindFirstObjectByType<PhysicsGoldenShotExecutor>();
            var runner = FindFirstObjectByType<PhysicsGoldenRegressionRunner>();
            var catalog = AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(catalogPath);
            if (executor == null || runner == null || catalog == null) { Finish("scene components lost", null); return; }

            var cases = catalog.Cases;
            if (caseIdx >= cases.Count) { EvaluateAndFinish(runner, cases.Count); return; }

            var golden = cases[caseIdx];
            if (executor.IsExecuting) { waitTimer = 0.1f; return; } // let current shot settle

            if (!executor.BeginShot(golden))
            {
                Debug.LogWarning("147VR/PostSwap: BeginShot returned false for " + golden.name + " (executor not configured?). Skipping.");
                caseIdx++; rep = 0; waitTimer = 0.2f; return;
            }
            rep++;
            if (rep >= reps) { caseIdx++; rep = 0; }
            waitTimer = 0.2f; // poll again after shot captures in FixedUpdate
        }

        void EvaluateAndFinish(PhysicsGoldenRegressionRunner runner, int caseCount)
        {
            var report = runner.Run();
            var sb = new StringBuilder();
            sb.Append("{\"timestampUtc\":\"").Append(DateTime.UtcNow.ToString("o")).Append("\",\"type\":\"post-swap-live-regression\"");
            sb.Append(",\"catalogCases\":").Append(caseCount);
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            sb.Append(",\"catalogCount\":").Append(report.catalogCount);
            sb.Append(",\"total\":").Append(report.total);
            sb.Append(",\"passed\":").Append(report.passed);
            sb.Append(",\"failed\":").Append(report.failed);
            sb.Append(",\"invalid\":").Append(report.invalid);
            sb.Append(",\"missing\":").Append(report.missing);
            sb.Append(",\"allCasesCovered\":").Append(report.allCasesCovered ? "true" : "false");
            sb.Append(",\"passRate\":").Append(report.passRate.ToString("F4", ci));
            sb.Append(",\"measurements\":[");
            var ms = runner.Measurements;
            for (int i = 0; i < ms.Count; i++)
            {
                var m = ms[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"case\":\"").Append(m.goldenCase != null ? m.goldenCase.name : "?").Append("\",\"dist\":").Append(m.measuredDistance.ToString("F6", ci)).Append(",\"peak\":").Append(m.measuredPeakSpeed.ToString("F6", ci)).Append("}");
            }
            sb.Append("]}");
            string outPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "ART_STAGE", "post_swap_regression_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json");
            try { File.WriteAllText(outPath, sb.ToString()); } catch (Exception e) { Debug.LogError("147VR/PostSwap write fail: " + e.Message); }
            bool allPass = report.total > 0 && report.failed == 0 && report.invalid == 0 && report.allCasesCovered;
            Finish((allPass ? "PASS" : "FAIL") + " | total=" + report.total + " passed=" + report.passed + " failed=" + report.failed + " invalid=" + report.invalid + " missing=" + report.missing + " -> " + outPath, null);
        }

        void Finish(string msg, Exception _)
        {
            Debug.Log("147VR/PostSwap regression: " + msg);
            StartCoroutine(StopNextFrame());
        }
        System.Collections.IEnumerator StopNextFrame()
        {
            yield return null;
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
