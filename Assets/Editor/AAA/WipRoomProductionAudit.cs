using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PoolPhysics.Sim;
using PoolPhysics.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VR147.AAA.Editor
{
    /// <summary>
    /// Read-only audit of the already-open WipRoom scene.
    /// It never opens, saves, or dirties a scene; the owner opens WipRoom first.
    /// </summary>
    public static class WipRoomProductionAudit
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string ReportPath = "Logs/WipRoom_ProductionAudit_20261118.txt";

        /// <summary>Headless entry point: opens WipRoom read-only, audits it, and never saves it.</summary>
        public static void RunBatch()
        {
            Scene opened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!opened.IsValid() || !opened.isLoaded)
                throw new InvalidOperationException("Could not load WipRoom for read-only audit.");
            Run();
        }

        [MenuItem("Tools/147/Production/Audit Loaded WipRoom (Read Only)")]
        public static void Run()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (!Application.isBatchMode) EditorUtility.DisplayDialog(
                    "147VR WipRoom Audit",
                    "Open Assets/Scenes/147VR_MainScene_WipRoom.unity first, then run this read-only audit again.\n\nNo scene was opened or changed.",
                    "OK");
                return;
            }

            var report = new StringBuilder(8192);
            int passed = 0, warnings = 0, failed = 0;
            Action<string> info = value => report.AppendLine(value);
            Action<string> pass = value => { passed++; info("PASS  " + value); };
            Action<string> warn = value => { warnings++; info("WARN  " + value); };
            Action<string> fail = value => { failed++; info("FAIL  " + value); };

            try
            {
                info("147VR WIPROOM PRODUCTION AUDIT");
                info("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                info("Unity: " + Application.unityVersion);
                info("Scene: " + ScenePath);
                info("Scene modified/saved: NO");
                info("");

                var enabledScenes = EditorBuildSettings.scenes.Where(item => item.enabled).ToArray();
                foreach (var buildScene in enabledScenes) info("BUILD " + buildScene.path);
                if (enabledScenes.Length == 1 && enabledScenes[0].path == ScenePath)
                    pass("WipRoom is the only enabled Build Settings scene");
                else
                    fail("Build Settings must enable WipRoom only.");

                var sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                    .Where(go => go != null && go.scene == scene).ToArray();
                var behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                    .Where(component => component != null && component.gameObject.scene == scene).ToArray();

                info("GameObjects=" + sceneObjects.Length + " MonoBehaviours=" + behaviours.Length);
                int missingScripts = sceneObjects.Sum(go => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go));
                if (missingScripts == 0) pass("No missing MonoBehaviour script references");
                else fail("Missing MonoBehaviour script references=" + missingScripts);

                CountComponent(behaviours, "SnookerPhysicsSetup", 1, pass, fail, info);
                CountComponent(behaviours, "SnookerBallTracker", 1, pass, fail, info);
                CountComponent(behaviours, "SnookerShotTracker", 1, pass, fail, info);
                CountComponent(behaviours, "SnookerTurnManager", 1, pass, fail, info);
                CountComponent(behaviours, "SnookerScoreManager", 1, pass, fail, info);
                CountComponent(behaviours, "PlayerViewManager", 1, pass, fail, info);

                info("");
                info("PHYSICS BAYS");
                var runners = Resources.FindObjectsOfTypeAll<PoolPhysicsEventRunner>()
                    .Where(runner => runner != null && runner.gameObject.scene == scene)
                    .OrderBy(runner => runner.BayName, StringComparer.Ordinal).ToArray();
                if (runners.Length == 4) pass("Exactly four Physics Runners found");
                else fail("Expected four Physics Runners; found " + runners.Length);

                string[] expectedBays =
                {
                    "Bay1_PhysicsRoot", "Bay2_PhysicsRoot",
                    "Bay3_PhysicsRoot", "Bay4_PhysicsRoot"
                };
                foreach (string bay in expectedBays)
                {
                    if (runners.Any(runner => runner.BayName == bay)) pass(bay + " exists");
                    else fail(bay + " missing");
                }

                foreach (PoolPhysicsEventRunner runner in runners)
                {
                    string prefix = runner.BayName + " | ";
                    info("");
                    info(prefix + "Variant=" + runner.Variant +
                         " Balls=" + runner.BallCount +
                         " CueIndex=" + runner.CueBallIndex +
                         " Pockets=" + runner.PocketCount +
                         " OwnsPhysics=" + runner.OwnsPhysics);

                    if (runner.Spec != null) pass(prefix + "TableSpec=" + AssetDatabase.GetAssetPath(runner.Spec));
                    else fail(prefix + "TableSpec missing");

                    if (runner.TableRootTransform != null)
                    {
                        Vector3 scale = runner.TableRootTransform.lossyScale;
                        info(prefix + "TableRoot=" + GetPath(runner.TableRootTransform) +
                             " Scale=" + scale.ToString("F3") +
                             " Playfield=" + runner.PlayfieldSize.ToString("F3"));
                        if (Mathf.Abs(scale.x - 1f) < 0.01f &&
                            Mathf.Abs(scale.y - 1f) < 0.01f &&
                            Mathf.Abs(scale.z - 1f) < 0.01f)
                            pass(prefix + "table scale is (1,1,1)");
                        else fail(prefix + "table scale is not (1,1,1)");
                    }
                    else fail(prefix + "table transform missing");

                    if (runner.PlayfieldSize != Vector2.zero) pass(prefix + "playfield dimensions valid");
                    else fail(prefix + "playfield dimensions are zero");

                    if (runner.PocketCount == 6) pass(prefix + "six pockets configured");
                    else warn(prefix + "expected six pockets; found " + runner.PocketCount);

                    var names = new List<string>(runner.BallCount);
                    var unique = new HashSet<Transform>();
                    for (int i = 0; i < runner.BallCount; i++)
                    {
                        Transform ball = runner.BallTransformAt(i);
                        if (ball == null)
                        {
                            fail(prefix + "ballTransforms[" + i + "] is null");
                            names.Add(string.Empty);
                            continue;
                        }
                        if (!unique.Add(ball)) fail(prefix + "duplicate ball Transform at index " + i);
                        names.Add(ball.name);
                        info(prefix + "ball[" + i.ToString("00") + "]=" + ball.name +
                             (SnookerBallTracker.IsCueBallName(ball.name) ? " [CUE]" : ""));
                    }

                    if (names.Count > 0 && SnookerBallTracker.IsCueBallName(names[0]))
                        pass(prefix + "serialized ball index 0 is the cue ball");
                    else
                        fail(prefix + "ball index 0 is not recognizably the cue ball");

                    int cueCount = names.Count(SnookerBallTracker.IsCueBallName);
                    int reds = names.Count(name => SnookerBallTracker.PointsForSimName(name) == 1);
                    int colours = names.Count(name =>
                    {
                        int points = SnookerBallTracker.PointsForSimName(name);
                        return points >= 2 && points <= 7;
                    });

                    switch (runner.Variant)
                    {
                        case GameVariant.Snooker:
                            if (cueCount == 1 && reds == 15 && colours == 6 && names.Count == 22)
                                pass(prefix + "standard 15-red Snooker set");
                            else if (cueCount == 1 && reds == 10 && colours == 6 && names.Count == 17)
                                warn(prefix + "10-red Snooker set; confirm short-frame mode is intentional");
                            else
                                fail(prefix + "Snooker rack invalid: cue=" + cueCount +
                                     " reds=" + reds + " colours=" + colours + " total=" + names.Count);
                            break;

                        case GameVariant.EightBall:
                            ValidatePoolRack(prefix, names, 15, pass, fail);
                            break;

                        case GameVariant.NineBall:
                            ValidatePoolRack(prefix, names, 9, pass, fail);
                            break;

                        default:
                            fail(prefix + "unknown GameVariant value " + (int)runner.Variant);
                            break;
                    }
                }

                info("");
                info("RUNTIME FOOTPRINT");
                int cameras = sceneObjects.Sum(go => go.GetComponents<Camera>().Length);
                int audioSources = sceneObjects.Sum(go => go.GetComponents<AudioSource>().Length);
                int audioListeners = sceneObjects.Sum(go => go.GetComponents<AudioListener>().Length);
                int xr = behaviours.Count(component =>
                    component.GetType().Name.IndexOf("XR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    component.GetType().Name.IndexOf("OVR", StringComparison.OrdinalIgnoreCase) >= 0);
                int mirror = behaviours.Count(component =>
                    component.GetType().FullName.IndexOf("Mirror", StringComparison.OrdinalIgnoreCase) >= 0);
                int dissonance = behaviours.Count(component =>
                    component.GetType().FullName.IndexOf("Dissonance", StringComparison.OrdinalIgnoreCase) >= 0);
                info("Cameras=" + cameras + " AudioSources=" + audioSources +
                     " AudioListeners=" + audioListeners + " XR/OVR=" + xr +
                     " Mirror=" + mirror + " Dissonance=" + dissonance);
                if (xr > 0) pass("XR/OVR components present");
                else warn("No XR/OVR component directly in scene; verify bootstrap/loader");
                if (mirror > 0) pass("Mirror components present");
                else warn("No Mirror component directly in scene");
                if (dissonance > 0) pass("Dissonance components present");
                else warn("No Dissonance component directly in scene");

                info("fixedDeltaTime=" + Time.fixedDeltaTime.ToString("F6") +
                     " gravity=" + UnityEngine.Physics.gravity.ToString("F3") +
                     " solverIterations=" + UnityEngine.Physics.defaultSolverIterations +
                     " MSAA=" + QualitySettings.antiAliasing);
                info("");
                info("SUMMARY PASS=" + passed + " WARN=" + warnings + " FAIL=" + failed);
                info("Scene modified/saved: NO");

                WriteReport(report);
                Debug.Log("[WipRoom Audit] PASS=" + passed + " WARN=" + warnings +
                          " FAIL=" + failed + " Report=" + ReportPath);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("147VR WipRoom Audit",
                    "PASS " + passed + " | WARN " + warnings + " | FAIL " + failed +
                    "\n\nReport: " + ReportPath + "\n\nScene was not modified or saved.", "OK");
            }
            catch (Exception exception)
            {
                info("EXCEPTION " + exception);
                WriteReport(report);
                Debug.LogException(exception);
            }
        }

        private static void CountComponent(MonoBehaviour[] behaviours, string typeName, int expected,
            Action<string> pass, Action<string> fail, Action<string> info)
        {
            int count = behaviours.Count(component => component.GetType().Name == typeName);
            info(typeName + " count=" + count);
            if (count == expected) pass(typeName + " count is " + expected);
            else fail(typeName + " expected " + expected + " but found " + count);
        }

        private static void ValidatePoolRack(string prefix, List<string> names, int maxNumber,
            Action<string> pass, Action<string> fail)
        {
            int cueCount = names.Count(SnookerBallTracker.IsCueBallName);
            int[] numbers = names.Where(name => !SnookerBallTracker.IsCueBallName(name))
                .Select(name => TryParseLastNumber(name, out int number) ? number : -1).ToArray();
            var expected = Enumerable.Range(1, maxNumber).ToHashSet();
            if (cueCount == 1 && names.Count == maxNumber + 1 &&
                numbers.Length == maxNumber && numbers.ToHashSet().SetEquals(expected))
                pass(prefix + "cue + unique object balls 1.." + maxNumber);
            else
                fail(prefix + "pool rack invalid: cue=" + cueCount +
                     " total=" + names.Count + " numbered=" + numbers.Length +
                     " distinct=" + numbers.Distinct().Count());
        }

        private static bool TryParseLastNumber(string value, out int number)
        {
            number = 0;
            MatchCollection matches = Regex.Matches(value ?? string.Empty,
                @"(?:^|[_\-\s])(\d+)(?=$|[_\-\s])");
            return matches.Count > 0 &&
                   int.TryParse(matches[matches.Count - 1].Groups[1].Value, out number);
        }

        private static string GetPath(Transform transform)
        {
            var names = new Stack<string>();
            while (transform != null)
            {
                names.Push(transform.name);
                transform = transform.parent;
            }
            return string.Join("/", names);
        }

        private static void WriteReport(StringBuilder report)
        {
            string fullPath = Path.GetFullPath(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, report.ToString(), new UTF8Encoding(false));
        }
    }
}
