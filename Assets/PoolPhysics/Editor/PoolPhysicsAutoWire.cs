using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PoolPhysics.EditorTools;
using Object = UnityEngine.Object;

namespace PoolPhysics.Unity.EditorTools
{
    /// <summary>
    /// One-click wiring of the event-based simulator into a live game scene (STAGING build —
    /// created by QwenWork QA session 10-27; review before use).
    ///
    /// Menus:
    ///   147VR/Physics/Auto-Wire Event Physics   — attach + link a PoolPhysicsEventRunner
    ///   147VR/Physics/Validate Event Wiring     — red/green checklist of every invariant the
    ///                                             runner's Build() silently depends on
    ///
    /// Hard invariants enforced here (from PoolPhysicsEventRunner.Build):
    ///   - ballTransforms[0] MUST be the cue ball (Build sets CueBallIndex = 0 unconditionally)
    ///   - tableTransform.lossyScale MUST be 1 (sim runs in metres of table-local space)
    ///   - TableSpec + CueVRPhysicsProfile assets must exist and be non-null
    /// This tool never saves the scene, never enables the runner, and never deletes the legacy
    /// PhysX path — the runner disables registered legacyDrivers only while it owns physics.
    /// </summary>
    public static class PoolPhysicsAutoWire
    {
        const string DataDir    = "Assets/PoolPhysics/Data";
        const string ProfilePth = DataDir + "/CueVRPhysicsProfile.asset";
        const string SpecPrefix = DataDir + "/TableSpec";

        // Ball matching: name-based first (game sets are named), SphereCollider as fallback.
        static readonly Regex BallNameRx = new Regex(
            @"(cue|ball|\bred\b|\bcolou?r\b|yellow|green|brown|blue|pink|black|white)", RegexOptions.IgnoreCase);
        // Type names of known legacy physics owners in Assets/Scripts/AAA (+ classic VR pool naming).
        static readonly Regex LegacyRx = new Regex(
            @"CuePhysicsAdapter|CueImpactSolver|CueStrokeModel|BallCollisionResponder|BallMotionController|BallPhysics|M5RealStraightBatchRunner|ShotDriver|PocketDetector",
            RegexOptions.IgnoreCase);

        [MenuItem("147VR/Physics/Auto-Wire Event Physics", false, 1)]
        public static void AutoWire()
        {
            var report = DoAutoWire();
            Debug.Log(report);
            if (report != null) EditorUtility.DisplayDialog("PoolPhysics Auto-Wire", report.Replace("[AutoWire] ", ""), "OK");
        }

        /// <summary>Batch-safe entry — Unity -batchmode -executeMethod PoolPhysics.Unity.EditorTools.PoolPhysicsAutoWire.AutoWireBatch</summary>
        public static void AutoWireBatch()
        {
            var report = DoAutoWire();
            Debug.Log(report ?? "[AutoWire] FAILED (see errors above)");
            System.IO.Directory.CreateDirectory("WireReports");
            System.IO.File.AppendAllText("WireReports/autowire.log", (report ?? "FAILED") + "\n");
        }

        static string DoAutoWire()
        {
            var table = ResolveTableRoot(out string tableWhy);
            if (table == null) { Debug.LogError("[AutoWire] No table found. Select the table GameObject and re-run, or name it containing 'Table'."); return null; }

            var balls = ResolveBalls(table, out string cueWhy);
            if (balls.Count == 0) { Debug.LogError("[AutoWire] No ball transforms found under/near the table. Check naming (contains 'ball'/'cue'/'red') or assign manually."); return null; }

            EnsureFolder();
            var profile = EnsureProfile();
            var spec    = EnsureTableSpec(table);
            var legacy  = FindLegacyDrivers(table, balls);

            var runner = table.GetComponent<PoolPhysicsEventRunner>();
            if (runner == null) runner = Undo.AddComponent<PoolPhysicsEventRunner>(table);

            var so = new SerializedObject(runner);
            so.FindProperty("tableTransform").objectReferenceValue       = table.transform;
            so.FindProperty("tableSpec").objectReferenceValue            = spec;
            so.FindProperty("profileSO").objectReferenceValue            = profile;
            FillList(so.FindProperty("ballTransforms"), balls.Select(b => (Object)b));
            FillList(so.FindProperty("legacyDrivers"),  legacy.Select(l => (Object)l));
            so.FindProperty("defaultBallRadius").floatValue = profile.poolBallRadius;
            so.FindProperty("recordEvents").boolValue       = true;
            so.FindProperty("logEvents").boolValue          = false;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(table.scene);

            var sb = new StringBuilder();
            sb.AppendLine($"[AutoWire] {tableWhy}");
            sb.AppendLine($"  table          : '{table.name}' (lossyScale {table.transform.lossyScale})");
            sb.AppendLine($"  balls ({balls.Count}) : {cueWhy}");
            sb.AppendLine($"  profile        : {AssetPathOf(profile)}   spec: {AssetPathOf(spec)}");
            sb.AppendLine($"  legacyDrivers ({legacy.Count}): {string.Join(", ", legacy.Select(c => c.GetType().Name + "@" + c.name))}");
            sb.AppendLine("  runner.Enabled : LEFT FALSE — flip in Inspector only after the in-editor golden pass.");
            sb.AppendLine("  Scene NOT saved. Review the Inspector, then Ctrl+S.");
            if (balls.Count != 16) sb.AppendLine($"  NOTE: found {balls.Count} balls, not 16 — snooker table expects 22, 8-ball 16. Verify the set.");
            return sb.ToString();
        }

        [MenuItem("147VR/Physics/Validate Event Wiring", false, 2)]
        public static void ValidateWiring()
        {
            var runners = Object.FindObjectsByType<PoolPhysicsEventRunner>(FindObjectsSortMode.None);
            if (runners.Length == 0) { Debug.LogWarning("[Validate] No PoolPhysicsEventRunner in the open scenes."); return; }
            var sb = new StringBuilder("[Validate] PoolPhysics wiring\n");
            foreach (var r in runners)
            {
                var so = new SerializedObject(r);
                var table   = so.FindProperty("tableTransform").objectReferenceValue as Transform;
                var spec    = so.FindProperty("tableSpec").objectReferenceValue as TableSpec;
                var profile = so.FindProperty("profileSO").objectReferenceValue as CueVRPhysicsProfile;
                var ballsP  = so.FindProperty("ballTransforms");
                var legsP   = so.FindProperty("legacyDrivers");

                sb.AppendLine($"— '{r.name}'");
                Check(sb, table != null, "tableTransform assigned");
                Check(sb, spec != null, "TableSpec asset assigned");
                Check(sb, profile != null, "CueVRPhysicsProfile asset assigned");
                Check(sb, ballsP.arraySize >= 2, $"ballTransforms count = {ballsP.arraySize}");
                Check(sb, table == null || Mathf.Approximately(table.lossyScale.x, 1f),
                      table == null ? "lossyScale == 1 (unknown)" : $"table lossyScale == 1 (got {table.lossyScale}) — sim runs metres");
                bool cueFirst = false, cueWhy2 = false;
                var first = ballsP.arraySize > 0 ? ballsP.GetArrayElementAtIndex(0).objectReferenceValue as Transform : null;
                if (first != null) { cueFirst = BallNameRx.IsMatch(first.name); cueWhy2 = first.name.Contains("cue", System.StringComparison.OrdinalIgnoreCase); }
                Check(sb, cueFirst && cueWhy2, $"ballTransforms[0] is the CUE ball (Build() hardcodes CueBallIndex=0) — index0='{(first ? first.name : "<null>")}'");

                var rExpected = profile != null ? profile.poolBallRadius : 0.028575f;
                for (int i = 0; i < ballsP.arraySize; i++)
                {
                    var t = ballsP.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    if (t == null) { sb.AppendLine($"  x ballTransforms[{i}] NULL (Build skips+errors)"); continue; }
                    var sc = t.GetComponent<SphereCollider>();
                    if (sc == null) sb.AppendLine($"  ~ '{t.name}': no SphereCollider (mass/radius falls back to default)");
                    else { float wr = sc.radius * t.lossyScale.x;
                           if (!Mathf.Approximately(wr, rExpected) && Mathf.Abs(wr - rExpected) > 1e-4f)
                               sb.AppendLine($"  ~ '{t.name}': world collider r {wr:F4} != profile {rExpected:F4} (runner warns per-ball)"); }
                }
                Check(sb, legsP.arraySize > 0, $"legacyDrivers registered = {legsP.arraySize} (runner disables while owning physics)");
                var en = so.FindProperty("enabled_override").boolValue;
                sb.AppendLine($"  i Enabled = {en}{(en ? "  <-- runtime owner is the SIM; legacy drivers are OFF right now" : "")}");
            }
            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("PoolPhysics Wiring", sb.ToString(), "OK");
        }

        // ---------- helpers ----------

        static GameObject ResolveTableRoot(out string why)
        {
            var sel = Selection.activeGameObject;
            if (sel != null && LooksLikeTable(sel)) { why = "using current Selection"; return sel; }
            var best = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Select(r => r.gameObject).Where(g => LooksLikeTable(g)).Distinct()
                .OrderByDescending(g => CountInHierarchyRigidbodies(g)).FirstOrDefault();
            why = best != null ? "heuristic: name contains 'table'/'โต๊ะ' + has colliders + most rigidbodies in subtree"
                               : "no table heuristic hit";
            return best;

            static bool LooksLikeTable(GameObject g) =>
                (g.name.Contains("table", System.StringComparison.OrdinalIgnoreCase) ||
                 g.name.Contains("โต๊ะ", StringComparison.Ordinal)) &&
                g.GetComponentInChildren<Collider>(true) != null;
        }

        static List<Transform> ResolveBalls(GameObject table, out string cueWhy)
        {
            var cands = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)
                .Select(rb => rb.transform)
                .Where(t => BallNameRx.IsMatch(t.name) || t.GetComponent<SphereCollider>() != null)
                .Distinct().ToList();

            // cue first — hard invariant of runner.Build (CueBallIndex = 0). Prefer explicit "cue" name.
            var cue = cands.FirstOrDefault(t => t.name.Contains("cue", System.StringComparison.OrdinalIgnoreCase))
                   ?? cands.FirstOrDefault(t => t.name.Contains("white", System.StringComparison.OrdinalIgnoreCase));
            if (cue != null) cands.Remove(cue);
            if (cands.Count > 0) cands.Insert(0, cue ?? cands[0]);
            cueWhy = cue != null ? $"cue ball '{cue.name}' forced to index 0"
                                 : (cands.Count > 0 ? "NO ball named *cue* found — index0 guessed; VERIFY" : "no balls found");
            return cands;
        }

        static List<MonoBehaviour> FindLegacyDrivers(GameObject table, List<Transform> balls)
        {
            var seen = new HashSet<MonoBehaviour>();
            foreach (var mb in table.GetComponentsInChildren<MonoBehaviour>(true)
                        .Concat(balls.SelectMany(b => b.GetComponentsInParent<MonoBehaviour>(true))))
            {
                if (mb == null || mb is PoolPhysicsEventRunner) continue;
                if (LegacyRx.IsMatch(mb.GetType().Name)) seen.Add(mb);
            }
            return seen.OrderBy(c => c.name).ToList();
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/PoolPhysics")) return;
            if (!AssetDatabase.IsValidFolder(DataDir))
                AssetDatabase.CreateFolder("Assets/PoolPhysics", "Data");
        }

        static CueVRPhysicsProfile EnsureProfile()
        {
            var p = AssetDatabase.LoadAssetAtPath<CueVRPhysicsProfile>(ProfilePth);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<CueVRPhysicsProfile>();   // pre-filled with RE'd Cue VR constants + [TUNE] seeds
                AssetDatabase.CreateAsset(p, ProfilePth);
                Debug.Log($"[AutoWire] created {ProfilePth} (values from Cue VR RE; [TUNE] fields still to fit vs golden)");
            }
            return p;
        }

        static TableSpec EnsureTableSpec(GameObject table)
        {
            string path = $"{SpecPrefix}_{table.name.Replace(" ", "_")}.asset";
            var s = AssetDatabase.LoadAssetAtPath<TableSpec>(path);
            if (s == null)
            {
                s = TableSpecGenerator.Generate(table, path);   // existing geometry-aware generator (fallback variant)
                if (s == null) Debug.LogWarning($"[AutoWire] TableSpecGenerator returned null — create/assign TableSpec manually. ({path})");
            }
            return s;
        }

        static void FillList(SerializedProperty list, IEnumerable<Object> items)
        {
            var arr = items.ToList();
            list.arraySize = arr.Count;
            for (int i = 0; i < arr.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
        }

        static int CountInHierarchyRigidbodies(GameObject g) => g.GetComponentsInChildren<Rigidbody>(true).Length;
        static string AssetPathOf(Object o) => AssetDatabase.GetAssetPath(o);
        static void Check(StringBuilder sb, bool ok, string msg) => sb.AppendLine($"  {(ok ? "v" : "x")} {msg}");
    }
}
