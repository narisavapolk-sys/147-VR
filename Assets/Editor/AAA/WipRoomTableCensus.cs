#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Read-only table census. Answers "how many tables are actually in this Scene, where are
    /// they, which bay owns each, and what is switched off".
    ///
    /// v2 (2026-11-09). The first version seeded clusters from renderers whose NAME looked like
    /// cloth, and found exactly one table - `SM_Slate` under ROOT_PoolTable_01 - while three
    /// tables were plainly visible in the viewport. Object names in this FBX are Blender
    /// leftovers and the other table tops carry material identity (`b3_poolSkin`,
    /// `Pool_VR_Material`) rather than a name match. It also searched a fixed radius around each
    /// BayN_PhysicsRoot, but the bay roots are not the table centres, so it declared Bay3/Bay4
    /// empty while their balls sit at Y 0.85 above them.
    ///
    /// Now: clusters are grown from table-part density with no anchor at all, cloth is detected
    /// by object name OR material name, hidden parts are split into the three real cases
    /// (own activeSelf false / blocked by an ancestor / renderer component disabled), and the
    /// nearest bay becomes an OUTPUT of the census instead of its seed.
    ///
    /// Nothing is modified.
    /// </summary>
    public static class WipRoomTableCensus
    {
        private const string ReportPath = "Logs/WipRoom_Table_Census.txt";
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const float ClusterRadius = 2.4f;

        private static readonly Regex TablePart =
            new Regex("cushion|rail|pocket|jaw|throat|lining|support|pad|leg|apron|slate|cloth|felt|table",
                      RegexOptions.IgnoreCase);
        private static readonly Regex ClothName =
            new Regex("cloth|felt|slate|playing|surface", RegexOptions.IgnoreCase);
        private static readonly Regex ClothMaterial =
            new Regex("poolskin|pool_vr_material|cloth|felt|baize|snooker_cloth", RegexOptions.IgnoreCase);

        private sealed class Cluster
        {
            public string Label = "(unnamed)";
            public List<Renderer> Parts = new List<Renderer>();
            public Bounds Bounds;
            public bool BoundsInit;
            public int SelfOff, AncestorBlocked, RendererDisabled, Visible;
            public readonly Dictionary<string, int> Blockers = new Dictionary<string, int>();
            public readonly Dictionary<string, int> Roles = new Dictionary<string, int>();
            public readonly List<string> ClothHits = new List<string>();
        }

        /// <summary>Part role, so "Bay2 has 2 cushions where Bay1 has 4" is readable.</summary>
        private static string RoleOf(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("table surface") || n.Contains("slate") || n.Contains("cloth") ||
                n.Contains("felt") || n.Contains("pooltable")) return "surface/body";
            if (n.Contains("cushion")) return "cushion";
            if (n.Contains("rail")) return "rail";
            if (n.Contains("pocket") || n.Contains("jaw") || n.Contains("throat") || n.Contains("lining")) return "pocket";
            if (n.Contains("leg") || n.Contains("apron") || n.Contains("support")) return "leg/support";
            return null;
        }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/16) Census Tables In Scene (Read-Only)")]
        public static void Census()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("147VR Table Census",
                    "Safety stop. Open the actual gameplay Scene first:\n" + SourceScenePath, "OK");
                return;
            }

            Renderer[] all = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => r != null && TablePart.IsMatch(r.name))
                .ToArray();
            Collider[] allColliders = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Collider>(true))
                .Where(c => c != null)
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("147VR TABLE CENSUS v3 (READ-ONLY - nothing was modified)");
            sb.AppendLine("Scene: " + scene.path + " | Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("table-part renderers considered=" + all.Length + " | scene colliders=" + allColliders.Length);
            sb.AppendLine("Grouping: by owning table ROOT (ROOT_PoolTable_NN / Prefab_WPBSA_NFoot_Snooker /" +
                          " 147VR_Table_VNN); leftovers fall back to " + ClusterRadius +
                          " m distance clusters and are labelled as such.");
            sb.AppendLine();

            // Group by the OWNING TABLE ROOT instead of by distance. Distance chaining merged
            // ROOT_PoolTable_02 + ROOT_PoolTable_03 + the legacy Prefab_WPBSA_12Foot_Snooker into
            // one 190-part blob, which made "does THIS table have all its cushions" unanswerable.
            // The room's naming is consistent (ROOT_PoolTable_0N), so the root is the honest key.
            var rootRegex = new Regex("ROOT_PoolTable_\\d+|Prefab_WPBSA_\\d+Foot_Snooker|147VR_Table_V\\d+",
                                      RegexOptions.IgnoreCase);
            var byRoot = new Dictionary<string, Cluster>();
            var ungrouped = new List<Renderer>();
            foreach (Renderer r in all)
            {
                string key = null;
                for (Transform t = r.transform; t != null; t = t.parent)
                    if (rootRegex.IsMatch(t.name)) { key = t.name; break; }
                if (key == null) { ungrouped.Add(r); continue; }
                if (!byRoot.TryGetValue(key, out Cluster c)) byRoot[key] = c = new Cluster { Label = key };
                c.Parts.Add(r);
            }

            var clusters = new List<Cluster>(byRoot.Values);
            var remaining = new List<Renderer>(ungrouped);
            while (remaining.Count > 0)
            {
                var cluster = new Cluster { Label = "(distance cluster, no table root)" };
                var queue = new List<Renderer> { remaining[0] };
                remaining.RemoveAt(0);
                for (int i = 0; i < queue.Count; i++)
                {
                    Vector3 anchor = queue[i].bounds.center;
                    for (int j = remaining.Count - 1; j >= 0; j--)
                    {
                        if (DistanceXZ(remaining[j].bounds.center, anchor) > ClusterRadius) continue;
                        queue.Add(remaining[j]);
                        remaining.RemoveAt(j);
                    }
                }
                cluster.Parts = queue;
                clusters.Add(cluster);
            }

            foreach (Cluster cluster in clusters)
            {
                cluster.BoundsInit = false;
                foreach (Renderer r in cluster.Parts)
                {
                    if (!cluster.BoundsInit) { cluster.Bounds = r.bounds; cluster.BoundsInit = true; }
                    else cluster.Bounds.Encapsulate(r.bounds);

                    GameObject go = r.gameObject;
                    if (!go.activeSelf) { cluster.SelfOff++; }
                    else if (!go.activeInHierarchy)
                    {
                        cluster.AncestorBlocked++;
                        Transform blocker = FirstInactiveAncestor(go.transform);
                        string key = blocker != null ? PathOf(blocker) : "<no inactive ancestor found>";
                        cluster.Blockers[key] = cluster.Blockers.TryGetValue(key, out int n) ? n + 1 : 1;
                    }
                    else if (!r.enabled) cluster.RendererDisabled++;
                    else cluster.Visible++;

                    string matName = r.sharedMaterials.Length > 0 && r.sharedMaterials[0] != null
                        ? r.sharedMaterials[0].name : "";
                    if (ClothName.IsMatch(r.name) || ClothMaterial.IsMatch(matName))
                        cluster.ClothHits.Add(PathOf(r.transform) + " [mat=" + matName + "] top=" +
                                              r.bounds.max.y.ToString("F3"));
                    string role = RoleOf(r.name);
                    if (role != null) cluster.Roles[role] = cluster.Roles.TryGetValue(role, out int rc) ? rc + 1 : 1;
                }
            }

            clusters = clusters.OrderByDescending(c => c.Parts.Count).ToList();
            sb.AppendLine("CLUSTERS FOUND: " + clusters.Count + " (only those with >= 8 parts are listed as table candidates)")
                ;
            foreach (Cluster c in clusters.Where(c => c.Parts.Count >= 8))
            {
                GameObject bay = NearestBay(c.Bounds.center);
                float bayDist = bay != null ? Vector3.Distance(bay.transform.position, c.Bounds.center) : float.NaN;
                Collider[] balls = allColliders
                    .Where(k => k.name.IndexOf("Ball", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                DistanceXZ(k.bounds.center, c.Bounds.center) <= ClusterRadius * 1.5f)
                    .ToArray();
                float lowestBall = balls.Length > 0 ? balls.Min(k => k.bounds.min.y) : float.NaN;

                sb.AppendLine();
                sb.AppendLine("  GROUP " + c.Label + " centre=" + Fmt(c.Bounds.center) + " size=" + Fmt(c.Bounds.size));
                sb.AppendLine("    parts=" + c.Parts.Count + " visible=" + c.Visible +
                              " selfOff=" + c.SelfOff + " ancestorBlocked=" + c.AncestorBlocked +
                              " rendererDisabled=" + c.RendererDisabled);
                sb.AppendLine("    roles over ALL parts (not only visible): " + string.Join("  ", c.Roles.OrderBy(p => p.Key)
                    .Select(p => p.Key + "=" + p.Value)));
                sb.AppendLine("    nearest bay=" + (bay != null ? bay.name + " at " + bayDist.ToString("F2") + " m" : "<none>") +
                              " | ball colliders nearby=" + balls.Length +
                              (balls.Length > 0 ? " lowestBallY=" + lowestBall.ToString("F3") : ""));
                if (c.ClothHits.Count > 0)
                {
                    sb.AppendLine("    TABLE SURFACE candidates (" + c.ClothHits.Count + "):");
                    foreach (string hit in c.ClothHits.Take(6)) sb.AppendLine("      " + hit);
                }
                else sb.AppendLine("    *** NO table surface in this cluster (no name or material match)");
                if (c.Blockers.Count > 0)
                {
                    sb.AppendLine("    HIDDEN BY ANCESTOR:");
                    foreach (KeyValuePair<string, int> pair in c.Blockers.OrderByDescending(p => p.Value))
                        sb.AppendLine("      " + pair.Value + " parts <- " + pair.Key);
                }
                if (c.SelfOff > 0) sb.AppendLine("    " + c.SelfOff + " parts switched off on themselves");
                if (c.RendererDisabled > 0) sb.AppendLine("    " + c.RendererDisabled + " parts have their Renderer component disabled");
            }

            sb.AppendLine();
            sb.AppendLine("SMALL CLUSTERS (furniture/props caught by the same words, listed for honesty)");
            foreach (Cluster c in clusters.Where(c => c.Parts.Count < 8).Take(15))
                sb.AppendLine("  " + Fmt(c.Bounds.center) + " parts=" + c.Parts.Count +
                              " visible=" + c.Visible + " e.g. " + c.Parts[0].name);

            sb.AppendLine();
            sb.AppendLine("INACTIVE ROOTS BIG ENOUGH TO BE A TABLE (bounds > 1.5 m wide, self off)");
            bool anyRoot = false;
            foreach (GameObject root in scene.GetRootGameObjects().Where(g => !g.activeSelf))
            {
                Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) continue;
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs) b.Encapsulate(r.bounds);
                if (b.size.x < 1.5f || b.size.z < 1.5f) continue;
                anyRoot = true;
                sb.AppendLine("  " + root.name + " renderers=" + rs.Length + " world centre=" + Fmt(b.center) +
                              " size=" + Fmt(b.size) + " colliders=" + root.GetComponentsInChildren<Collider>(true).Length +
                              " rigidbodies=" + root.GetComponentsInChildren<Rigidbody>(true).Length);
                sb.AppendLine("     -> a whole table exists here, switched off at the root. Enabling it is a scene decision, not a repair.");
            }
            if (!anyRoot) sb.AppendLine("  (none)");

            sb.AppendLine();
            sb.AppendLine("BAY ROOTS VS CLUSTERS (a bay is 'served' when a cluster with a table surface is within " +
                          (ClusterRadius * 1.6f).ToString("F1") + " m)");
            float serveRadius = ClusterRadius * 1.6f;
            foreach (string bayName in new[] { "Bay1_PhysicsRoot", "Bay2_PhysicsRoot", "Bay3_PhysicsRoot", "Bay4_PhysicsRoot" })
            {
                GameObject bay = GameObject.Find(bayName);
                if (bay == null) { sb.AppendLine("  " + bayName + ": NOT FOUND"); continue; }
                Vector3 p = bay.transform.position;
                Cluster served = clusters.FirstOrDefault(c => c.ClothHits.Count > 0 &&
                                                              DistanceXZ(c.Bounds.center, p) <= serveRadius);
                int balls = allColliders.Count(k => k.name.IndexOf("Ball", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                                   DistanceXZ(k.bounds.center, p) <= serveRadius);
                float lowest = allColliders
                    .Where(k => k.name.IndexOf("Ball", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                DistanceXZ(k.bounds.center, p) <= serveRadius)
                    .Select(k => k.bounds.min.y).DefaultIfEmpty(float.NaN).Min();
                sb.AppendLine("  " + bayName + " at " + Fmt(p) +
                              " | surface cluster nearby: " + (served != null ? "YES " + Fmt(served.Bounds.center) +
                                                              " (visible " + served.Visible + "/" + served.Parts.Count + ")" : "NO") +
                              " | balls nearby: " + balls + (balls > 0 ? " lowestY=" + lowest.ToString("F3") : ""));
                if (served == null && balls > 0)
                    sb.AppendLine("     *** physics wired, table visual absent for this bay");
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log("[TableCensus v2] clusters=" + clusters.Count + " big=" +
                      clusters.Count(c => c.Parts.Count >= 8) + " -> " + ReportPath);
            EditorUtility.DisplayDialog("147VR Table Census",
                "Census v2 written. Nothing was modified.\n\n" + ReportPath +
                "\nTable-part renderers: " + all.Length +
                "\nClusters with >= 8 parts: " + clusters.Count(c => c.Parts.Count >= 8) +
                "\nInactive table-sized roots: see report", "OK");
        }

        private static GameObject NearestBay(Vector3 point) =>
            new[] { "Bay1_PhysicsRoot", "Bay2_PhysicsRoot", "Bay3_PhysicsRoot", "Bay4_PhysicsRoot" }
                .Select(GameObject.Find)
                .Where(g => g != null)
                .OrderBy(g => DistanceXZ(g.transform.position, point))
                .FirstOrDefault();

        private static float DistanceXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
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

        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";
    }
}
#endif
