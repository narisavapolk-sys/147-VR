#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Restores the Bay2 table parts that the audit found switched off.
    ///
    /// Evidence that created this tool (Logs/WipRoom_Bay_Geometry_Audit.txt, 2026-11-09):
    ///   Bay2 has 34 of its rail-family renderers visible and 30 hidden - Side Cushion.001,
    ///   End Cushion.002, Pocket_Net / Brass_Rim / Throat / Throat_Lining for the _SE, _E and
    ///   _NE corners, Corner Support-002, Side Support-001/004, Pocket Pad.002/006. The table is
    ///   NOT lifted (cloth top 0.881 versus Bay1 0.893, legs 66 mm into the floor), so the
    ///   owner's "something is pushing the table up" theory is ruled out: a whole side of the
    ///   table is simply inactive.
    ///   Only 6 m_IsActive overrides exist in the scene while 30 renderers are hidden, so most
    ///   of them are hidden by an ancestor node or authored off inside naris_vintage.fbx.
    ///
    /// Two menus, deliberately separated, because re-enabling table parts also re-enables their
    /// COLLIDERS, which changes the physics field (cushion bounce, pocket jaws) and therefore
    /// obliges a PlayMode rerun:
    ///   14) DRY RUN - read-only. Lists every object it would switch on, how many colliders and
    ///       rigidbodies that adds, and which ancestor nodes still block parts (ancestors are
    ///       never touched by the apply step, because switching one on can light up dozens of
    ///       unrelated objects).
    ///   15) APPLY - switches on exactly the listed table parts through Undo, marks the Scene
    ///       dirty, never saves, and prints the collider/rigidbody delta.
    /// </summary>
    public static class WipRoomBayRailRestore
    {
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string BayName = "Bay2_PhysicsRoot";
        private const float BayRadius = 2.8f;
        private const string DryRunReport = "Logs/WipRoom_Bay2_Restore_DryRun.txt";
        private const string ApplyReport = "Logs/WipRoom_Bay2_Restore_Apply.txt";

        private static readonly Regex TablePart =
            new Regex("cushion|rail|pocket|jaw|throat|lining|support|pad", RegexOptions.IgnoreCase);

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/14) Bay2 Restore - Dry Run (Read-Only)")]
        public static void DryRun() => Run(false);

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/15) Bay2 Restore - Apply (Undoable / No Save)")]
        public static void Apply() => Run(true);

        private static void Run(bool apply)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("147VR Bay2 Restore",
                    "Safety stop. Open the actual gameplay Scene first:\n" + SourceScenePath, "OK");
                return;
            }

            GameObject bay = GameObject.Find(BayName);
            if (bay == null)
            {
                EditorUtility.DisplayDialog("147VR Bay2 Restore", BayName + " is not in this Scene.", "OK");
                return;
            }
            Vector3 origin = bay.transform.position;

            Renderer[] all = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => r != null && TablePart.IsMatch(r.name) && InRadius(r.bounds.center, origin))
                .ToArray();

            // Objects we can legally switch on: their OWN activeSelf is false. Anything whose own
            // flag is true but that is still not active in the hierarchy is hidden by an ANCESTOR
            // and is reported instead - enabling an ancestor can light up dozens of unrelated
            // objects. Fixed 2026-11-09: the old guard read
            //   if (activeSelf || activeInHierarchy) continue;
            // which skipped every ancestor-blocked part (they all have activeSelf=true), so the
            // dry run claimed "0 blocked" while the audit listed 34.
            var selfOff = new List<GameObject>();
            var ancestorBlocked = new Dictionary<Transform, List<Renderer>>();
            foreach (Renderer r in all)
            {
                if (!r.gameObject.activeSelf)
                {
                    selfOff.Add(r.gameObject);
                    continue;
                }
                if (r.gameObject.activeInHierarchy) continue;

                Transform blocker = FirstInactiveAncestor(r.transform);
                if (blocker == null) continue;
                if (!ancestorBlocked.TryGetValue(blocker, out List<Renderer> list))
                    ancestorBlocked[blocker] = list = new List<Renderer>();
                list.Add(r);
            }

            selfOff = selfOff.Distinct().ToList();

            int collidersBefore = Count<Collider>(scene);
            int bodiesBefore = Count<Rigidbody>(scene);
            int lightsBefore = Count<Light>(scene);

            // Predict what the enabled objects will add.
            int colliderDelta = 0, bodyDelta = 0;
            foreach (GameObject go in selfOff)
            {
                colliderDelta += go.GetComponentsInChildren<Collider>(true).Length;
                bodyDelta += go.GetComponentsInChildren<Rigidbody>(true).Length;
            }

            var sb = new StringBuilder();
            sb.AppendLine(apply ? "147VR BAY2 RAIL RESTORE - APPLY" : "147VR BAY2 RAIL RESTORE - DRY RUN (nothing modified)");
            sb.AppendLine("Scene: " + scene.path + " | Bay: " + BayName + " origin=" + Fmt(origin) +
                          " | radius=" + BayRadius + " m");
            sb.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("table-part renderers in radius: " + all.Length +
                          " | visible now: " + all.Count(r => r.gameObject.activeInHierarchy));
            sb.AppendLine();

            sb.AppendLine("WOULD SWITCH ON (own activeSelf=false): " + selfOff.Count);
            foreach (GameObject go in selfOff.OrderBy(g => g.name))
            {
                Renderer rend = go.GetComponent<Renderer>();
                sb.AppendLine("  " + PathOf(go.transform) +
                              (rend != null ? " bounds=" + Fmt(rend.bounds.center) : "") +
                              " colliders=" + go.GetComponentsInChildren<Collider>(true).Length +
                              " bodies=" + go.GetComponentsInChildren<Rigidbody>(true).Length);
            }

            sb.AppendLine();
            sb.AppendLine("STILL BLOCKED BY AN INACTIVE ANCESTOR (NOT touched by this tool): " + ancestorBlocked.Count);
            foreach (KeyValuePair<Transform, List<Renderer>> pair in ancestorBlocked.OrderByDescending(p => p.Value.Count))
                sb.AppendLine("  " + PathOf(pair.Key) + " hides " + pair.Value.Count + " parts: " +
                              string.Join(", ", pair.Value.Select(v => v.name).Take(12)) +
                              (pair.Value.Count > 12 ? " ..." : ""));
            if (ancestorBlocked.Count > 0)
                sb.AppendLine("  -> re-enabling these nodes is a separate decision: one ancestor can light up unrelated objects.");

            sb.AppendLine();
            sb.AppendLine("PHYSICS IMPACT WARNING");
            sb.AppendLine("  switching these on adds ~" + colliderDelta + " colliders and ~" + bodyDelta +
                          " rigidbodies to the scene.");
            sb.AppendLine("  Cushion/pocket colliders change how balls bounce, so the PlayMode suite");
            sb.AppendLine("  (PoolPhysicsPhaseTests + SoloPracticeUndoAndCueGripTests) must be rerun after this.");

            if (apply)
            {
                foreach (GameObject go in selfOff)
                {
                    Undo.RecordObject(go, "147VR Bay2 Rail Restore");
                    go.SetActive(true);
                    EditorUtility.SetDirty(go);
                }
                int collidersAfter = Count<Collider>(scene);
                int bodiesAfter = Count<Rigidbody>(scene);
                int lightsAfter = Count<Light>(scene);
                sb.AppendLine();
                sb.AppendLine("APPLIED");
                sb.AppendLine("  objects enabled: " + selfOff.Count);
                sb.AppendLine("  Colliders: " + collidersBefore + " -> " + collidersAfter);
                sb.AppendLine("  Rigidbodies: " + bodiesBefore + " -> " + bodiesAfter);
                sb.AppendLine("  Lights: " + lightsBefore + " -> " + lightsAfter);

                Renderer[] stillVisible = all.Where(r => r.gameObject.activeInHierarchy).ToArray();
                sb.AppendLine("  table-part renderers visible after: " + stillVisible.Length + " / " + all.Length);
                if (stillVisible.Length == all.Length)
                    sb.AppendLine("  *** EVERYTHING IS ON - the parts were already visible, nothing to do");

                EditorSceneManager.MarkSceneDirty(scene);
            }

            string report = apply ? ApplyReport : DryRunReport;
            Directory.CreateDirectory("Logs");
            File.WriteAllText(report, sb.ToString());
            Debug.Log("[Bay2Restore] " + (apply ? "applied" : "dry-run") + " selfOff=" + selfOff.Count +
                      " ancestorBlocked=" + ancestorBlocked.Count + " colliderDelta~" + colliderDelta + " -> " + report);
            EditorUtility.DisplayDialog("147VR Bay2 Restore",
                (apply ? "Enabled " : "Would enable ") + selfOff.Count + " table parts.\n" +
                "Ancestor-blocked (untouched): " + ancestorBlocked.Count + " nodes.\n" +
                "Colliders this adds: ~" + colliderDelta + " | rigidbodies: ~" + bodyDelta + "\n\n" +
                (apply ? "Scene is DIRTY but NOT saved. Inspect, then Ctrl+S or Ctrl+Z." : "Nothing was modified.") +
                "\nReport: " + report, "OK");
        }

        private static bool InRadius(Vector3 point, Vector3 origin)
        {
            float dx = point.x - origin.x, dz = point.z - origin.z;
            return dx * dx + dz * dz <= BayRadius * BayRadius;
        }

        private static Transform FirstInactiveAncestor(Transform start)
        {
            if (start == null || !start.gameObject.activeSelf) return null;
            Transform parent = start.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf) return parent;
                parent = parent.parent;
            }
            return null;
        }

        private static string PathOf(Transform t)
        {
            var parts = new List<string>();
            Transform current = t;
            while (current != null) { parts.Insert(0, current.name); current = current.parent; }
            return string.Join("/", parts);
        }

        private static int Count<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<T>(true).Length);

        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";
    }
}
#endif
