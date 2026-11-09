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
    /// Read-only geometry audit for the four table bays, written to answer one owner report:
    /// "ขอบโต๊ะ BAY2 หายไป" (the Bay2 rail is gone) with the hypothesis that something - maybe a
    /// cube/collider - is lifting the table off the floor.
    ///
    /// Why proximity search instead of hierarchy walk: Bay1..Bay4_PhysicsRoot are EMPTY scene
    /// roots (Bay2 sits at x=-1.925, z=+2.211 with zero scene children) and the room is one
    /// prefab instance, so the table visuals and colliders cannot be found by parenting. They
    /// are found by world position around each bay, which is also how the player experiences them.
    ///
    /// The audit compares the four bays against EACH OTHER, because a missing rail shows up as
    /// Bay2 having fewer rail/cushion/jaw/pocket renderers than Bay1/3/4, and a lifted table
    /// shows up as a cloth-top Y that does not match its siblings. A collider whose vertical
    /// span sits between the floor top and the cloth bottom is the direct test of the owner's
    /// "something is pushing it up" theory. Broken materials are reported too - an
    /// InternalErrorShader or a dead _BaseMap reference makes geometry disappear visually while
    /// the Transform still looks fine, which is exactly the symptom.
    ///
    /// Nothing is modified: no material, transform, collider, or Scene write.
    /// </summary>
    public static class WipRoomBayGeometryAudit
    {
        private const string ReportPath = "Logs/WipRoom_Bay_Geometry_Audit.txt";
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const float BayRadius = 2.8f;

        private static readonly string[] Bays =
        {
            "Bay1_PhysicsRoot", "Bay2_PhysicsRoot", "Bay3_PhysicsRoot", "Bay4_PhysicsRoot"
        };

        // Tightened 2026-11-09: the old list also matched "frame", "side" and "corner", which
        // pulled in picture frames, window frames and furniture and inflated Bay2 to 64.
        // These are table parts only.
        private static readonly Regex Railish =
            new Regex("cushion|rail|pocket|jaw|throat|lining|support|pad", RegexOptions.IgnoreCase);
        private static readonly Regex Clothish =
            new Regex("cloth|felt|bed|slate|surface|playing", RegexOptions.IgnoreCase);
        private static readonly Regex Floorish =
            new Regex("floor|parquet|carpet|rug|ground", RegexOptions.IgnoreCase);
        private static readonly Regex Legish =
            new Regex("leg|foot|base|pillar|apron", RegexOptions.IgnoreCase);

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/13) Audit Bay Table Geometry (Read-Only)")]
        public static void Audit()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("147VR Bay Audit",
                    "Safety stop. Open the actual gameplay Scene first:\n" + SourceScenePath, "OK");
                return;
            }

            Renderer[] all = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => r != null)
                .ToArray();
            Collider[] allColliders = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Collider>(true))
                .Where(c => c != null)
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("147VR BAY GEOMETRY AUDIT (READ-ONLY - nothing was modified)");
            sb.AppendLine("Scene: " + scene.path + " | Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Scene renderers=" + all.Length + " | scene colliders=" + allColliders.Length +
                          " | search radius per bay=" + BayRadius + " m");
            sb.AppendLine();

            var perBay = new List<BaySummary>();

            foreach (string bayName in Bays)
            {
                GameObject bay = GameObject.Find(bayName);
                if (bay == null)
                {
                    sb.AppendLine("### " + bayName + ": NOT FOUND IN SCENE");
                    continue;
                }
                Vector3 origin = bay.transform.position;
                Renderer[] near = all.Where(r => InRadius(r.bounds.center, origin)).ToArray();
                Collider[] nearCol = allColliders.Where(c => InRadius(c.bounds.center, origin)).ToArray();

                var rails = near.Where(r => Railish.IsMatch(r.name)).ToArray();
                var ancestorBlocks = new Dictionary<Transform, int>();
                int selfOff = 0;
                var cloths = near.Where(r => Clothish.IsMatch(r.name)).ToArray();
                var legs = near.Where(r => Legish.IsMatch(r.name)).ToArray();
                var floors = near.Where(r => Floorish.IsMatch(r.name)).ToArray();

                var summary = new BaySummary
                {
                    Name = bayName,
                    Origin = origin,
                    Near = near.Length,
                    Rails = rails.Length,
                    RailsVisible = rails.Count(r => r.enabled && r.gameObject.activeInHierarchy),
                    Cloths = cloths.Length,
                    Legs = legs.Length,
                    Colliders = nearCol.Length,
                    ClothTopY = cloths.Length > 0 ? cloths.Max(r => r.bounds.max.y) : float.NaN,
                    LegBottomY = legs.Length > 0 ? legs.Min(r => r.bounds.min.y) : float.NaN,
                    FloorTopY = floors.Length > 0 ? floors.Max(f => f.bounds.max.y) : float.NaN,
                };
                perBay.Add(summary);

                sb.AppendLine("### " + bayName + "  origin=" + Fmt(origin));
                sb.AppendLine("  renderers in radius=" + near.Length +
                              " | rail-ish=" + rails.Length + " (visible " + summary.RailsVisible + ")" +
                              " | cloth-ish=" + cloths.Length + " | leg-ish=" + legs.Length +
                              " | colliders=" + nearCol.Length);
                sb.AppendLine("  heights: clothTopY=" + F1(summary.ClothTopY) +
                              " legBottomY=" + F1(summary.LegBottomY) +
                              " floorTopY=" + F1(summary.FloorTopY) +
                              " | cloth-to-floor gap=" + Gap(summary.ClothTopY, summary.FloorTopY) +
                              " | leg-bottom minus floor=" + Gap(summary.LegBottomY, summary.FloorTopY));

                if (rails.Length > 0)
                {
                    sb.AppendLine("  RAIL / CUSHION / POCKET RENDERERS");
                    foreach (Renderer r in rails.OrderBy(x => x.bounds.center.x))
                    {
                        Material m = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
                        string shaderName = m != null ? m.shader.name : "<no material>";
                        sb.AppendLine("    " + r.name);
                        sb.AppendLine("       bounds min=" + Fmt(r.bounds.min) + " max=" + Fmt(r.bounds.max) +
                                      " size=" + Fmt(r.bounds.size));
                        sb.AppendLine("       activeInHierarchy=" + r.gameObject.activeInHierarchy +
                                      " activeSelf=" + r.gameObject.activeSelf +
                                      " enabled=" + r.enabled + " shadowCasting=" + r.shadowCastingMode +
                                      " receiveShadows=" + r.receiveShadows +
                                      " lightmapIndex=" + r.lightmapIndex);
                        if (!r.gameObject.activeInHierarchy)
                        {
                            Transform blocker = FirstInactiveAncestor(r.transform);
                            string why = blocker == null
                                ? "own activeSelf=false (switched off on the object itself, or authored off in the FBX)"
                                : "BLOCKED BY ANCESTOR '" + PathOf(blocker) + "' activeSelf=false";
                            sb.AppendLine("       *** NOT VISIBLE: " + why);
                            if (blocker != null) ancestorBlocks[blocker] = (ancestorBlocks.TryGetValue(blocker, out int n) ? n : 0) + 1;
                            else selfOff++;
                        }
                        sb.AppendLine("       material=" + (m != null ? m.name : "<none>") +
                                      " shader=" + shaderName +
                                      (shaderName.Contains("InternalError") ? "  *** BROKEN SHADER - RENDERS NOTHING" : "") +
                                      (m != null && HasDeadBaseMap(m) ? "  *** DEAD _BaseMap REFERENCE" : "") +
                                      " | bounds.size.y=" + r.bounds.size.y.ToString("F3"));
                    }
                }
                else
                {
                    sb.AppendLine("  *** NO rail/cushion/jaw/pocket renderer found within " + BayRadius + " m");
                }

                if (ancestorBlocks.Count > 0 || selfOff > 0)
                {
                    sb.AppendLine("  VISIBILITY CULPRITS (what to re-enable, biggest first)");
                    sb.AppendLine("    parts switched off on themselves: " + selfOff);
                    foreach (KeyValuePair<Transform, int> pair in ancestorBlocks.OrderByDescending(p => p.Value))
                        sb.AppendLine("    ancestor hides " + pair.Value + " parts: " + PathOf(pair.Key));
                }

                if (cloths.Length > 0)
                {
                    sb.AppendLine("  CLOTH / SURFACE RENDERERS");
                    foreach (Renderer r in cloths.OrderBy(x => -x.bounds.max.y))
                        sb.AppendLine("    " + r.name + " top=" + r.bounds.max.y.ToString("F3") +
                                      " bottom=" + r.bounds.min.y.ToString("F3") +
                                      " active=" + r.gameObject.activeInHierarchy + " enabled=" + r.enabled);
                }

                if (nearCol.Length > 0)
                {
                    sb.AppendLine("  COLLIDERS IN RADIUS (owner theory: something lifting the table)");
                    foreach (Collider c in nearCol.OrderBy(x => x.bounds.min.y))
                    {
                        bool between = float.IsNaN(summary.FloorTopY) == false &&
                                       c.bounds.max.y > summary.FloorTopY + 0.01f &&
                                       (float.IsNaN(summary.ClothTopY) || c.bounds.min.y < summary.ClothTopY - 0.01f);
                        sb.AppendLine("    " + c.gameObject.name + " [" + c.GetType().Name + "]" +
                                      " enabled=" + c.enabled + " isTrigger=" + c.isTrigger +
                                      " min=" + Fmt(c.bounds.min) + " max=" + Fmt(c.bounds.max) +
                                      (between ? "  *** SPANS FLOOR-TO-TABLE" : ""));
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("CROSS-BAY COMPARISON (this is where a missing rail becomes obvious)");
            int maxRails = perBay.Count > 0 ? perBay.Max(b => b.Rails) : 0;
            foreach (BaySummary b in perBay)
            {
                string flag = b.Rails < maxRails ? "  *** FEWER RAIL RENDERERS THAN " + maxRails : "";
                string visFlag = b.RailsVisible < b.Rails ? "  *** " + (b.Rails - b.RailsVisible) + " HIDDEN/DISABLED" : "";
                sb.AppendLine("  " + b.Name + " rails=" + b.Rails + " visible=" + b.RailsVisible +
                              " cloths=" + b.Cloths + " legs=" + b.Legs + " colliders=" + b.Colliders +
                              " clothTopY=" + F1(b.ClothTopY) + " legBottomY=" + F1(b.LegBottomY) + flag + visFlag);
            }

            float medianTop = Median(perBay.Where(b => !float.IsNaN(b.ClothTopY)).Select(b => b.ClothTopY).ToList());
            sb.AppendLine();
            sb.AppendLine("  median clothTopY across bays = " + medianTop.ToString("F3"));
            foreach (BaySummary b in perBay)
                if (!float.IsNaN(b.ClothTopY) && Mathf.Abs(b.ClothTopY - medianTop) > 0.02f)
                    sb.AppendLine("  *** " + b.Name + " cloth top differs by " + (b.ClothTopY - medianTop).ToString("F3") +
                                  " m from the median - table height is NOT consistent");

            sb.AppendLine();
            sb.AppendLine("INTERPRETATION GUIDE");
            sb.AppendLine("  rails fewer than siblings            -> geometry really is missing from the room source");
            sb.AppendLine("  rails equal but visible fewer        -> a renderer is disabled or its GameObject inactive");
            sb.AppendLine("  BROKEN SHADER / DEAD _BaseMap         -> the rail is there but renders nothing (looks missing)");
            sb.AppendLine("  clothTopY off the median              -> the table sits at a different height");
            sb.AppendLine("  SPANS FLOOR-TO-TABLE on a collider    -> the owner's cube/lift hypothesis is confirmed");

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log("[BayAudit] wrote " + ReportPath + " | bays=" + perBay.Count +
                      " | rails " + string.Join("/", perBay.Select(b => b.Rails)));
            EditorUtility.DisplayDialog("147VR Bay Audit",
                "Audit written. Nothing was modified.\n\n" + ReportPath +
                "\nRails per bay: " + string.Join(" / ", perBay.Select(b => b.Name.Substring(3, 4) + "=" + b.Rails +
                                                    "(" + b.RailsVisible + " visible)")) +
                "\nCloth top Y:   " + string.Join(" / ", perBay.Select(b => F1(b.ClothTopY))), "OK");
        }

        private sealed class BaySummary
        {
            public string Name;
            public Vector3 Origin;
            public int Near, Rails, RailsVisible, Cloths, Legs, Colliders;
            public float ClothTopY, LegBottomY, FloorTopY;
        }

        /// <summary>Nearest ancestor whose own activeSelf is false, or null when the object
        /// itself is the switched-off node (then activeSelf is already false on it).</summary>
        private static Transform FirstInactiveAncestor(Transform start)
        {
            if (start == null || !start.gameObject.activeSelf) return null;
            Transform parent = start.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf) return parent;
                parent = parent.parent;
            }
            return null;   // inactive for a reason outside the transform chain
        }

        private static string PathOf(Transform t)
        {
            var parts = new List<string>();
            Transform current = t;
            while (current != null) { parts.Insert(0, current.name); current = current.parent; }
            return string.Join("/", parts);
        }

        private static bool InRadius(Vector3 point, Vector3 origin)
        {
            float dx = point.x - origin.x, dz = point.z - origin.z;
            return dx * dx + dz * dz <= BayRadius * BayRadius;
        }

        private static bool HasDeadBaseMap(Material material)
        {
            string path = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            Match m = Regex.Match(File.ReadAllText(path),
                @"_BaseMap:\s*\n\s*m_Texture: \{fileID: 2800000, guid: ([0-9a-f]{32})");
            return m.Success && string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value));
        }

        private static float Median(List<float> values)
        {
            if (values.Count == 0) return float.NaN;
            var sorted = values.OrderBy(v => v).ToList();
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) * 0.5f;
        }

        private static string F1(float v) => float.IsNaN(v) ? "<n/a>" : v.ToString("F3");
        private static string Gap(float a, float b) =>
            float.IsNaN(a) || float.IsNaN(b) ? "<n/a>" : (a - b).ToString("F3");
        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";
    }
}
#endif
