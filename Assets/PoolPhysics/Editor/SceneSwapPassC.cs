#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PoolPhysics.EditorTools;
using Object = UnityEngine.Object;

namespace _147VR.Wiring
{
    /// <summary>
    /// Pass C — explicit 4-bay wiring of the naris FBX room (no generic heuristics):
    /// for each bay region, find the table mesh + its balls, create a PhysicsRoot at the
    /// playfield corner, generate TableSpec via TableSpecGenerator (renderer-bounds fallback),
    /// attach PoolPhysicsEventRunner with cue ball at index 0, kinematic rigidbodies.
    /// Report -> Logs/passC_report.txt. Runner stays DISABLED (enabled_override=false).
    /// </summary>
    public static class SceneSwapPassC
    {
        const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
        const string ProfilePth = "Assets/PoolPhysics/Data/CueVRPhysicsProfile.asset";

        struct BayDef { public string name; public float y; public string[] tableMeshes; }
        // Unity-space z (= -Blender y) — verified against Logs/room_dump.txt 10-29
        static readonly BayDef[] Bays = {
            new BayDef{ name="Bay1", y= 9.3f, tableMeshes=new[]{"SM_TableBase","SM_147vr_GlowPad_Bay1"} },
            new BayDef{ name="Bay2", y= 3.1f, tableMeshes=new[]{"SM_TABLE SURFACE","SM_TABLE FRAME","SM_Base Table Support"} },
            new BayDef{ name="Bay3", y=-3.1f, tableMeshes=new[]{"SM_PoolTable_03"} },
            new BayDef{ name="Bay4", y=-9.3f, tableMeshes=new[]{"SM_PoolTable_04"} },
        };

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) { Debug.LogError("[PassC] cannot open scene"); return; }
            var rep = new StringBuilder($"[PassC] {System.DateTime.Now:HH:mm:ss}\n");

            EnsureFolder();
            var profile = EnsureProfile(rep);

            foreach (var bay in Bays)
            {
                // table mesh in this bay region
                GameObject table = null;
                foreach (var tn in bay.tableMeshes)
                {
                    var cand = FindByNameInRegion(tn, bay.y);
                    if (cand != null) { table = cand; break; }
                }
                if (table == null) { rep.AppendLine($"x {bay.name}: table mesh not found — skipped"); continue; }

                var tb = table.GetComponent<Renderer>() ? table.GetComponent<Renderer>().bounds : default;
                float px = 0.14f, pz = bay.y;
                Vector2 size = new Vector2(Mathf.Max(tb.size.x, 1f), Mathf.Max(tb.size.z, 0.5f));
                px = (tb.center.x);
                var rootGo = GameObject.Find($"{bay.name}_PhysicsRoot");
                if (rootGo == null) rootGo = new GameObject($"{bay.name}_PhysicsRoot");
                rootGo.transform.SetPositionAndRotation(
                    new Vector3(px - size.x / 2f, 0f, pz - size.y / 2f), Quaternion.identity);

                // spec
                var specPath = $"Assets/PoolPhysics/Data/TableSpec_{bay.name}.asset";
                var spec = AssetDatabase.LoadAssetAtPath<PoolPhysics.Unity.TableSpec>(specPath);
                if (spec == null)
                {
                    var genRep = new List<string>();
                    spec = TableSpecGenShim.Generate(table, specPath, genRep);
                    foreach (var g in genRep) rep.AppendLine($"  spec {bay.name}: {g}");
                    if (spec == null) { rep.AppendLine($"x {bay.name}: spec generation failed"); continue; }
                    spec.playfieldSize = size;   // measured world bounds (generator fallback equals this)
                    EditorUtility.SetDirty(spec);
                }

                // balls in region with components from Pass B (Rigidbody+SphereCollider)
                var balls = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)
                    .Select(rb => rb.transform)
                    .Where(t => Mathf.Abs(t.position.x - px) < size.x / 2f + 0.6f &&
                                Mathf.Abs(t.position.z - pz) < size.y / 2f + 0.6f &&
                                IsBallName(t.name))
                    .ToList();
                if (balls.Count == 0) { rep.AppendLine($"x {bay.name}: no balls in region (table found at z={pz})"); continue; }
                var cue = balls.FirstOrDefault(t => t.name.ToLower().Contains("cue") || t.name.ToLower().Contains("white"));
                if (cue != null) { balls.Remove(cue); balls.Insert(0, cue); }

                var runner = rootGo.GetComponent<PoolPhysics.Unity.PoolPhysicsEventRunner>();
                if (runner == null) runner = rootGo.AddComponent<PoolPhysics.Unity.PoolPhysicsEventRunner>(); if (rootGo.GetComponent<PoolPhysics.Unity.PoolPhysicsAudioHapticsBridge>() == null) rootGo.AddComponent<PoolPhysics.Unity.PoolPhysicsAudioHapticsBridge>();
                var so = new SerializedObject(runner);
                so.FindProperty("tableTransform").objectReferenceValue = rootGo.transform;
                so.FindProperty("tableSpec").objectReferenceValue = spec;
                so.FindProperty("profileSO").objectReferenceValue = profile;
                FillList(so.FindProperty("ballTransforms"), balls.Select(b => (Object)b));
                so.FindProperty("legacyDrivers").arraySize = 0;
                so.FindProperty("recordEvents").boolValue = true;
                so.FindProperty("enabled_override").boolValue = false;
                var firstCol = balls[0].GetComponent<SphereCollider>();
                if (firstCol != null)
                    so.FindProperty("defaultBallRadius").floatValue = firstCol.radius * Mathf.Max(balls[0].lossyScale.x, 1e-4f);
                so.ApplyModifiedProperties();

                rep.AppendLine($"v {bay.name}: root '{rootGo.name}' corner=({rootGo.transform.position.x:F2},{rootGo.transform.position.z:F2}) " +
                               $"playfield={size.x:F2}x{size.y:F2} balls={balls.Count} cue0='{balls[0].name}'");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            rep.AppendLine("scene saved");
            File.WriteAllText("Logs/passC_report.txt", rep.ToString());
            Debug.Log("[PassC] DONE\n" + rep);
        }

        static bool IsBallName(string n)
        {
            n = n.ToLower();
            if (n.StartsWith("root_")) return false;              // FBX empty roots (e.g. SnookerCue stick) are not balls
            return n.StartsWith("bp_ball") || n.Contains("_ball") || n.Contains("cueball") ||
                   n.StartsWith("sm_red") || n.StartsWith("sm_black") || n.StartsWith("sm_white");
        }

        static GameObject FindByNameInRegion(string name, float bayY)
        {
            var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Where(r => r.gameObject.name == name && Mathf.Abs(r.bounds.center.z - bayY) < 1.6f)
                .OrderByDescending(r => r.bounds.size.x * r.bounds.size.z);
            var hit = all.FirstOrDefault();
            return hit ? hit.gameObject : null;
        }

        static void FillList(SerializedProperty list, IEnumerable<Object> items)
        {
            var arr = items.ToList();
            list.arraySize = arr.Count;
            for (int i = 0; i < arr.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/PoolPhysics/Data"))
                AssetDatabase.CreateFolder("Assets/PoolPhysics", "Data");
        }

        static PoolPhysics.Unity.CueVRPhysicsProfile EnsureProfile(StringBuilder rep)
        {
            var p = AssetDatabase.LoadAssetAtPath<PoolPhysics.Unity.CueVRPhysicsProfile>(ProfilePth);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<PoolPhysics.Unity.CueVRPhysicsProfile>();
                AssetDatabase.CreateAsset(p, ProfilePth);
                rep.AppendLine("created CueVRPhysicsProfile asset (RE defaults, [TUNE] pending)");
            }
            return p;
        }

        /// <summary>Thin shim over TableSpecGenerator so a failure never aborts the pass.</summary>
        static class TableSpecGenShim
        {
            public static PoolPhysics.Unity.TableSpec Generate(GameObject table, string path, List<string> report)
            {
                try
                {
                    var s = TableSpecGenerator.Generate(table, path);
                    report.Add(s != null ? "generated" : "returned null");
                    return s;
                }
                catch (System.Exception e) { report.Add("exception: " + e.Message); return null; }
            }
        }
    }
}
#endif
