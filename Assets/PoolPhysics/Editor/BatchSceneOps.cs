using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _147VR.Wiring
{
    /// <summary>
    /// Batch-safe scene pipeline for Unity -batchmode -executeMethod (STAGING tool).
    /// Order: open scene -> REMOVE OLD HDRI (user instruction 10-27: old "PromDance_Skybox"
    /// object + default skybox ref; the real HDRI arrives with the Neon Vintage Blender
    /// import) -> Auto-Wire ->
    /// Validate -> save scene + write WireReports/RUNALL.txt + set process exit code.
    ///
    /// Invoke:
    ///   Unity -batchmode -nographics -quit -projectPath &lt;wip&gt; \
    ///     -executeMethod _147VR.Wiring.BatchSceneOps.RunAll \
    ///     -scene "Assets/Scenes/147VR_MainScene.unity" \
    ///     -logFile runall.log
    ///
    /// Fail-safe: HDRI removal only deletes objects matching OldHdriRx AND clears
    /// RenderSettings.skybox; everything else is reported, never silently destroyed.
    /// </summary>
    public static class BatchSceneOps
    {
        static readonly Regex OldHdriRx = new Regex("skybox|hdri|panorama|dome", RegexOptions.IgnoreCase);

        public static void RunAll()
        {
            var args = Environment.GetCommandLineArgs();
            string scene = GetArg(args, "-scene") ?? "Assets/Scenes/147VR_MainScene.unity";
            var rep = new StringBuilder($"[RunAll] {DateTime.Now:yyyy-MM-dd HH:mm:ss} scene={scene}\n");
            bool ok = true;

            if (!File.Exists(scene)) { rep.AppendLine($"x scene not found: {scene}"); Fail(rep); return; }
            var sc = EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            if (!sc.IsValid()) { rep.AppendLine("x could not open scene"); Fail(rep); return; }

            // 1) HDRI removal (user instruction 10-27)
            int removed = 0;
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(g => OldHdriRx.IsMatch(g.name)))
            {
                rep.AppendLine($"  - removing old HDRI object: '{go.name}'");
                UnityEngine.Object.DestroyImmediate(go, allowDestroyingAssets: false); removed++;
            }
            if (RenderSettings.skybox != null)
            { rep.AppendLine($"  - clearing RenderSettings.skybox ({RenderSettings.skybox.name})"); RenderSettings.skybox = null; }
            rep.AppendLine($"v HDRI sweep done ({removed} object(s) removed) — new environment comes from Neon Vintage import");

            // 2) Auto-Wire (reuse the staged tool's core via its batch entry)
            PoolPhysics.Unity.EditorTools.PoolPhysicsAutoWire.AutoWireBatch();
            rep.AppendLine("  (autowire report in WireReports/autowire.log)");

            // 3) Validate: fail the exit code on any 'x'
            var validate = ValidateToString();
            rep.Append(validate);
            ok = !validate.Contains("\nx ") && !validate.Contains("\n  x ");

            // 4) Save
            if (EditorSceneManager.SaveScene(sc)) rep.AppendLine("v scene saved");
            else { rep.AppendLine("x scene save failed"); ok = false; }

            Directory.CreateDirectory("WireReports");
            File.WriteAllText("WireReports/RUNALL.txt", rep.ToString());
            Debug.Log(rep.ToString());
            if (!ok) Environment.ExitCode = 1;
        }

        [MenuItem("147VR/Physics/Remove Old HDRI (current scene)", false, 5)]
        public static void RemoveOldHdriMenu()
        {
            int removed = 0;
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(g => OldHdriRx.IsMatch(g.name)))
            {
                Debug.Log($"[HDRI] removing '{go.name}'");
                UnityEngine.Object.DestroyImmediate(go, allowDestroyingAssets: false); removed++;
            }
            if (RenderSettings.skybox != null) { RenderSettings.skybox = null; Debug.Log("[HDRI] RenderSettings.skybox cleared"); }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Remove Old HDRI", $"{removed} object(s) removed + skybox cleared.\nScene marked dirty — Ctrl+S to save.", "OK");
        }

        static string ValidateToString()
        {
            var sb = new StringBuilder("[Validate] (batch copy)\n");
            var runners = UnityEngine.Object.FindObjectsByType<PoolPhysics.Unity.PoolPhysicsEventRunner>(FindObjectsSortMode.None);
            if (runners.Length == 0) { sb.Append("x no runner in scene (autowire failed?)\n"); return sb.ToString(); }
            foreach (var r in runners)
            {
                var so = new SerializedObject(r);
                string F(string p) => so.FindProperty(p)?.objectReferenceValue?.name ?? "NULL";
                int n = so.FindProperty("ballTransforms")?.arraySize ?? 0;
                var t = so.FindProperty("tableTransform")?.objectReferenceValue as Transform;
                sb.Append(n >= 1 && !F("tableTransform").Contains("NULL") && !F("tableSpec").Contains("NULL") &&
                          !F("profileSO").Contains("NULL") && t != null && Mathf.Approximately(t.lossyScale.x, 1f)
                        ? $"v '{r.name}': balls={n} spec={F("tableSpec")} profile={F("profileSO")}\n"
                        : $"x '{r.name}': balls={n} table={F("tableTransform")} spec={F("tableSpec")} profile={F("profileSO")} scale={(t ? t.lossyScale.x : -1)}\n");
            }
            return sb.ToString();
        }

        static string GetArg(string[] args, string key)
        {
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i + 1];
            return null;
        }
        static void Fail(StringBuilder rep) { Directory.CreateDirectory("WireReports"); File.WriteAllText("WireReports/RUNALL.txt", rep.ToString()); Debug.LogError(rep.ToString()); Environment.ExitCode = 1; }
    }
}
