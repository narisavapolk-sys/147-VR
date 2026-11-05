#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using PoolPhysics.Unity;
using Object = UnityEngine.Object;

namespace _147VR.QA
{
    /// <summary>
    /// Script Verification &amp; Android/Quest Build Preflight — read-only audit the player can run
    /// from the menu. It never builds, never saves the scene and never edits wiring; it reports.
    ///
    /// Menu: 147VR/QA/Script Verification &amp; Quest Build Preflight
    /// Report: Logs/preflight_report.txt
    ///
    /// Sections:
    ///   A. Compile health   — error CS lines from the current Editor log
    ///   B. Authority wiring — the four bay backends Pass C created (spec/pockets/balls/cue/scale)
    ///   C. Handoff readiness— Phase 1/2/3 code present and mutually consistent
    ///   D. Quest build      — target, backend, arch, SDK, graphics API, scene list, packages
    /// </summary>
    public static class PhysicsAuthorityPreflight
    {
        const string MainScene = "Assets/Scenes/147VR_MainScene.unity";
        const string ReportPath = "Logs/preflight_report.txt";
        const string CueControllerPath = "Assets/Scripts/Quest/SnookerCueController.cs";
        const string DirectorPath = "Assets/Scripts/Authority/PoolPhysicsAuthorityDirector.cs";
        const string BridgePath = "Assets/Scripts/Authority/PoolPhysicsRulesBridge.cs";

        static readonly StringBuilder Rep = new StringBuilder();
        static int _fail;
        static int _warn;

        [MenuItem("147VR/QA/Script Verification & Quest Build Preflight", false, 20)]
        public static void Run()
        {
            Rep.Clear();
            _fail = 0;
            _warn = 0;
            Rep.AppendLine($"[Preflight] {DateTime.Now:yyyy-MM-dd HH:mm:ss}  Unity {Application.unityVersion}");

            SectionA_Compile();
            SectionB_AuthorityWiring();
            SectionC_HandoffReadiness();
            SectionD_QuestBuild();

            Rep.AppendLine();
            Rep.AppendLine(_fail == 0
                ? $"RESULT: PASS ({_warn} warning(s))"
                : $"RESULT: FAIL — {_fail} blocking issue(s), {_warn} warning(s)");

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, Rep.ToString());
            foreach (var line in Rep.ToString().Split('\n'))
                Debug.Log(line.TrimEnd());
            Debug.Log($"[Preflight] report written to {Path.GetFullPath(ReportPath)}");

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    _fail == 0 ? "Preflight PASS" : "Preflight FAIL",
                    _fail == 0
                        ? $"All blocking checks passed.\n{_warn} warning(s).\n\n{ReportPath}"
                        : $"{_fail} blocking issue(s) — open {ReportPath} for details.",
                    "OK");
            }
        }

        // ---- A. Compile health ----

        static void SectionA_Compile()
        {
            Header("A. Script compilation");
            try
            {
                var errors = ReadCompileErrorsFromEditorLog();
                if (errors.Count == 0)
                    Ok("no 'error CS' lines in the current Editor log");
                else
                {
                    Fail($"{errors.Count} compile error line(s) in Editor.log (newest last):");
                    foreach (var e in errors.Take(12))
                        Rep.AppendLine($"      {e}");
                }
            }
            catch (Exception e)
            {
                Warn($"Editor.log unreadable while the Editor holds it open: {e.Message}");
            }
        }

        static List<string> ReadCompileErrorsFromEditorLog()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string log = Path.Combine(local, "Unity", "Editor", "Editor.log");
            var found = new List<string>();
            if (!File.Exists(log))
                return found;

            using (var stream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int i = line.IndexOf("error CS", StringComparison.Ordinal);
                    if (i >= 0)
                    {
                        // De-duplicate: Unity re-prints the same set per assembly pass.
                        string key = line.Trim();
                        if (!found.Contains(key))
                            found.Add(key);
                    }
                }
            }
            return found;
        }

        // ---- B. Authority wiring (four bays) ----

        static void SectionB_AuthorityWiring()
        {
            Header("B. Bay physics authority wiring");
            var scene = SceneManager.GetActiveScene();
            Rep.AppendLine($"    active scene: {scene.name} ({scene.path})");
            if (scene.path != MainScene)
                Warn($"open {MainScene} before trusting bay numbers (currently '{scene.path}')");

            var runners = Object.FindObjectsByType<PoolPhysicsEventRunner>()
                .OrderBy(r => r.BayName, StringComparer.Ordinal)
                .ToList();
            if (runners.Count == 0)
            {
                Fail("no PoolPhysicsEventRunner in the scene — run 147VR/Physics/Scene Swap Pass C");
                return;
            }
            Ok($"{runners.Count} bay backend(s) discovered");

            foreach (var r in runners)
            {
                var sb = new StringBuilder($"    {r.BayName}:");
                bool bad = false;

                int balls = r.BallCount;
                if (balls == 0) { sb.Append(" NO BALLS"); bad = true; } else sb.Append($" balls={balls}");

                int pockets = r.PocketCount;
                if (pockets == 0) { sb.Append(" NO POCKETS"); bad = true; } else sb.Append($" pockets={pockets}");

                var spec = r.Spec;
                if (spec == null) { sb.Append(" spec=MISSING"); bad = true; }
                else
                {
                    var size = r.PlayfieldSize;
                    if (size.x <= 0.01f || size.y <= 0.01f) { sb.Append($" playfield={size}"); bad = true; }
                    else sb.Append($" playfield={size.x:F2}x{size.y:F2}m");
                }

                var root = r.TableRootTransform;
                if (root == null) { sb.Append(" root=MISSING"); bad = true; }
                else if (!Mathf.Approximately(root.lossyScale.x, 1f) ||
                         !Mathf.Approximately(root.lossyScale.y, 1f) ||
                         !Mathf.Approximately(root.lossyScale.z, 1f))
                {
                    sb.Append($" rootScale={root.lossyScale} (!=1 breaks metre units)"); bad = true;
                }

                if (r.BallRadius < 0.02f || r.BallRadius > 0.04f)
                { sb.Append($" ballRadius={r.BallRadius:F5} (outside 26-40mm)"); bad = true; }
                else sb.Append($" r={r.BallRadius:F5}m");

                Transform cue = r.BallTransformAt(r.CueBallIndex);
                if (cue == null) { sb.Append(" cue=NULL"); bad = true; }
                else sb.Append($" cue='{cue.name}'");

                // Cue must not be a numbered/colour ball, and no duplicate transforms allowed.
                var unique = new HashSet<Transform>();
                for (int i = 0; i < balls; i++)
                {
                    var t = r.BallTransformAt(i);
                    if (t != null && !unique.Add(t)) { sb.Append($" DUPLICATE ball[{i}]"); bad = true; break; }
                }

                if (bad) { _fail++; Rep.AppendLine("  x" + sb); } else Rep.AppendLine("  v" + sb);
                Rep.AppendLine($"        surfaceY={(cue != null ? (cue.position.y - r.BallRadius) : 0f):F3}  centre={PoolPhysicsAuthorityDirectorProbe(r)}");
            }
        }

        /// <summary>Reported through the director's own geometry helper when present.</summary>
        static string PoolPhysicsAuthorityDirectorProbe(PoolPhysicsEventRunner r)
        {
            try
            {
                Type t = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("VR147.Authority.PoolPhysicsAuthorityDirector"))
                    .FirstOrDefault(x => x != null);
                if (t == null)
                    return "centre=<director not compiled>";

                var m = t.GetMethod("TableCenter", BindingFlags.Static | BindingFlags.Public);
                if (m == null)
                    return "centre=<TableCenter missing>";
                Vector3 c = (Vector3)m.Invoke(null, new object[] { r });
                return $"({c.x:F2}, {c.z:F2})";
            }
            catch (Exception e)
            {
                return "centre=<probe failed: " + e.Message + ">";
            }
        }

        // ---- C. Handoff readiness (Phase 1/2/3 code) ----

        static void SectionC_HandoffReadiness()
        {
            Header("C. Physics authority handoff (Phase 1-3)");

            foreach (var pair in new[]
                     {
                         new { Label = "Phase 1 director", Path = DirectorPath, Need = "VR147.Authority.PoolPhysicsAuthorityDirector" },
                         new { Label = "Phase 3 rules bridge", Path = BridgePath, Need = "VR147.Authority.PoolPhysicsRulesBridge" }
                     })
            {
                if (!File.Exists(pair.Path))
                {
                    Fail($"{pair.Label} file missing: {pair.Path}");
                    continue;
                }
                string src = File.ReadAllText(pair.Path);
                bool bootstrap = src.Contains("RuntimeInitializeOnLoadMethod");
                bool compiled = Type.GetType(pair.Need + ", Assembly-CSharp") != null ||
                                AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType(pair.Need) != null);
                if (compiled && bootstrap) Ok($"{pair.Label} present, bootstraps itself at runtime (no scene edit)");
                else if (compiled) Warn($"{pair.Label} compiled but has no runtime bootstrap");
                else Fail($"{pair.Label} file exists but the type is NOT compiled ({pair.Need})");
            }

            if (!File.Exists(CueControllerPath))
            {
                Fail($"cue controller missing: {CueControllerPath}");
                return;
            }
            string cue = File.ReadAllText(CueControllerPath);
            Check(cue.Contains("PoolPhysicsAuthorityDirector.Instance"), "cue controller resolves the ACTIVE bay (not FindObjectOfType)");
            Check(cue.Contains("runner.Shoot(shot.direction, shot.speed, tip"), "cue strike is routed into PoolPhysicsEventRunner.Shoot");
            Check(cue.Contains("ActiveSurfaceTopY"), "aiming plane follows the active bay");
            Check(cue.Contains("ActiveTableBounds"), "aim clamp follows the active bay");

            string bridge = File.Exists(BridgePath) ? File.ReadAllText(BridgePath) : "";
            Check(bridge.Contains("PoolEventKind.Pocket"), "pots come from simulator Pocket events (not PhysX triggers)");
            Check(bridge.Contains("PoolEventKind.BallBall"), "first-ball-hit comes from simulator BallBall events");
            Check(bridge.Contains("RestoreBall"), "re-spots are reconciled back into the simulator");

            string setup = File.ReadAllText("Assets/Scripts/Quest/SnookerPhysicsSetup.cs");
            Check(setup.Contains("if (rb.isKinematic)"), "PhysX pocket catcher stays silent while the sim owns the balls");
        }

        static void Check(bool ok, string label)
        {
            if (ok) Ok(label);
            else Warn(label + " — NOT confirmed");
        }

        // ---- D. Android / Quest build settings ----

        static void SectionD_QuestBuild()
        {
            Header("D. Android / Quest build preflight");

            Guard("build target", () =>
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    Fail($"active build target is {EditorUserBuildSettings.activeBuildTarget}, expected Android");
                else Ok($"activeBuildTarget = Android (development={EditorUserBuildSettings.development})");
            });

            Guard("scripting backend / API level", () =>
            {
                var group = BuildPipeline.GetBuildTargetGroup(BuildTarget.Android);
                var named = NamedBuildTarget.FromBuildTargetGroup(group);
                var backend = PlayerSettings.GetScriptingBackend(named);
                if (backend != ScriptingImplementation.IL2CPP)
                    Fail($"scripting backend is {backend}, Quest requires IL2CPP");
                else Ok("scriptingBackend = IL2CPP");

                string defines = PlayerSettings.GetScriptingDefineSymbols(named) ?? "";
                Rep.AppendLine($"        defines: {(defines.Length > 0 ? defines : "<none>")}");
            });

            Guard("target architectures", () =>
            {
                var arch = PlayerSettings.Android.targetArchitectures;
                if ((arch & AndroidArchitecture.ARM64) == 0)
                    Fail($"AndroidArchitecture = {arch}; Quest needs ARM64");
                else Ok($"targetArchitectures = {arch}");
            });

            Guard("min SDK", () =>
            {
                var min = PlayerSettings.Android.minSdkVersion;
                if ((int)min < 29)
                    Warn($"minSdkVersion = {min} (Quest headsets run Android 10+; 29 or higher recommended)");
                else Ok($"minSdkVersion = {min}");
            });

            Guard("graphics APIs", () =>
            {
                var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                if (apis == null || apis.Length == 0) Fail("no graphics API set for Android");
                else Rep.AppendLine($"        graphicsAPIs = {string.Join(", ", apis.Select(a => a.ToString()))}" +
                                    (apis.Contains(GraphicsDeviceType.OpenGLES3) ? "" : "  [WARN] OpenGLES3 missing"));
            });

            Guard("player settings", () =>
            {
                Rep.AppendLine($"        productName = {PlayerSettings.productName}");
                Rep.AppendLine($"        package     = {PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(BuildTarget.Android)))}");
            });

            Guard("build scenes", () =>
            {
                var scenes = EditorBuildSettings.scenes;
                var enabled = scenes.Where(s => s.enabled).ToList();
                if (enabled.Count == 0) { Fail("Build Settings has no enabled scene"); return; }
                int idx = Array.FindIndex(scenes, s => s.path == MainScene);
                if (idx < 0) Fail($"{MainScene} is not in Build Settings scenes");
                else if (!scenes[idx].enabled) Fail($"{MainScene} present but disabled in Build Settings");
                else if (idx != 0) Warn($"{MainScene} is at index {idx}; play scene is expected first");
                else Ok($"build scene[0] = {MainScene} (enabled scenes: {enabled.Count})");
            });

            Guard("XR packages", () =>
            {
                string manifestPath = "Packages/manifest.json";
                if (!File.Exists(manifestPath)) { Warn("Packages/manifest.json not found"); return; }
                string manifest = File.ReadAllText(manifestPath);
                foreach (var pkg in new[] { "com.meta.xr.sdk.core", "com.meta.xr.sdk.interaction", "com.unity.xr.management", "com.unity.inputsystem", "com.unity.render-pipelines.universal" })
                {
                    if (manifest.Contains(pkg)) Rep.AppendLine($"        {pkg} referenced");
                    else Warn($"{pkg} NOT in Packages/manifest.json");
                }
            });

            Guard("project size / Library health (informational)", () =>
            {
                var library = new DirectoryInfo("Library");
                if (library.Exists)
                {
                    double gb = NewSizeGb(library);
                    Rep.AppendLine($"        Library/ ~{gb:F1} GB  (never delete Library/PackageCache on this machine)");
                }
            });
        }

        static double NewSizeGb(DirectoryInfo dir)
        {
            // Non-recursive: top-level file bytes only. A recursive scan here froze this machine before.
            try { return dir.GetFiles().Sum(f => (double)f.Length) / 1073741824.0; }
            catch { return -1; }
        }

        // ---- helpers ----

        static void Header(string title) => Rep.AppendLine($"\n=== {title} ===");
        static void Ok(string s) { Rep.AppendLine("  v " + s); }
        static void Warn(string s) { _warn++; Rep.AppendLine("  ! " + s); }
        static void Fail(string s) { _fail++; Rep.AppendLine("  x " + s); }

        static void Guard(string label, Action body)
        {
            try { body(); }
            catch (Exception e) { Warn($"{label}: check skipped ({e.GetType().Name}: {e.Message})"); }
        }
    }
}
#endif
