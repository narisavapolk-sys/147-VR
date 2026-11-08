#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    /// <summary>Read-only inventory of the actual Build Index 0 WipRoom scene and art-pass FBX overlap.</summary>
    public static class WipRoomVisualAudit
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string ArtRoot = "Assets/AAA/Imports/NeonVintageArtPass";

        private sealed class AssetInfo
        {
            public string Path;
            public string RootName;
            public int Transforms;
            public int MeshFilters;
            public int Renderers;
            public int Colliders;
            public int Rigidbodies;
            public int Lights;
            public long Triangles;
            public Bounds Bounds;
            public bool HasBounds;
            public HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public string Summary => $"{RootName}: transforms={Transforms}, meshes={MeshFilters}, renderers={Renderers}, tris={Triangles}, colliders={Colliders}, rigidbodies={Rigidbodies}, lights={Lights}, bounds={(HasBounds ? Bounds.size.ToString("F2") : "none")}";
        }

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var sb = new StringBuilder();
            sb.AppendLine("147VR WipRoom Visual Audit (READ ONLY)");
            sb.AppendLine("Scene: " + scene.path + " | Build Index 0");
            sb.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("No Scene objects or assets were modified or saved.");
            sb.AppendLine();
            var roots = scene.GetRootGameObjects();
            sb.AppendLine("SCENE ROOTS");
            var sceneInfos = new List<AssetInfo>();
            foreach (var root in roots)
            {
                var info = Inspect(root, root.name);
                sceneInfos.Add(info);
                sb.AppendLine("- " + (root.activeSelf ? "ACTIVE " : "INACTIVE ") + info.Summary);
            }
            sb.AppendLine();

            var paths = new List<string>
            {
                "Assets/AAA/Imports/naris_vintage/naris_vintage.fbx",
                "Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab",
                "Assets/AAA/ImportedSnooker/Prefab_WPBSA_12Foot_Snooker.prefab",
                "Assets/Prefabs/Lighting/PREFAB_147VR_LightRig_A.prefab",
                ArtRoot + "/Static_Baked.fbx",
                ArtRoot + "/Dynamic.fbx"
            };
            paths.AddRange(AssetDatabase.FindAssets("t:Model", new[] { ArtRoot }).Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)).Distinct());
            var assets = new Dictionary<string, AssetInfo>(StringComparer.OrdinalIgnoreCase);
            sb.AppendLine("SOURCE ASSET INVENTORY");
            foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { sb.AppendLine("- MISSING/NOT IMPORTED: " + path); continue; }
                var info = Inspect(prefab, path);
                assets[path] = info;
                sb.AppendLine("- " + info.Summary + " | " + path);
            }
            sb.AppendLine();

            var vintage = assets.Values.FirstOrDefault(a => a.Path.EndsWith("naris_vintage.fbx", StringComparison.OrdinalIgnoreCase));
            var table = assets.Values.FirstOrDefault(a => a.Path.EndsWith("147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab", StringComparison.OrdinalIgnoreCase));
            var stat = assets.Values.FirstOrDefault(a => a.Path.EndsWith("Static_Baked.fbx", StringComparison.OrdinalIgnoreCase));
            var dyn = assets.Values.FirstOrDefault(a => a.Path.EndsWith("Dynamic.fbx", StringComparison.OrdinalIgnoreCase));
            sb.AppendLine("OVERLAP CHECK");
            Compare(sb, "Static_Baked vs naris_vintage", stat, vintage);
            Compare(sb, "Static_Baked vs table visual", stat, table);
            Compare(sb, "Dynamic vs naris_vintage", dyn, vintage);
            Compare(sb, "Dynamic vs table visual", dyn, table);
            sb.AppendLine();
            sb.AppendLine("PER-COLLECTION DUPLICATION SCREEN (against active room + table asset)");
            foreach (var group in assets.Values.Where(a => a.Path.StartsWith(ArtRoot + "/", StringComparison.OrdinalIgnoreCase) && a.Path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) && !a.Path.EndsWith("Static_Baked.fbx", StringComparison.OrdinalIgnoreCase) && !a.Path.EndsWith("Dynamic.fbx", StringComparison.OrdinalIgnoreCase)).OrderBy(a => a.Path))
            {
                int roomOverlap = vintage == null ? -1 : group.Names.Intersect(vintage.Names, StringComparer.OrdinalIgnoreCase).Count();
                int tableOverlap = table == null ? -1 : group.Names.Intersect(table.Names, StringComparer.OrdinalIgnoreCase).Count();
                sb.AppendLine(Path.GetFileName(group.Path) + $" | tris={group.Triangles} | room-name-overlap={roomOverlap}/{group.Names.Count} | table-name-overlap={tableOverlap}/{group.Names.Count} | bounds={(group.HasBounds ? group.Bounds.size.ToString("F2") : "none")}");
                if (vintage != null)
                {
                    var uniqueNames = group.Names.Except(vintage.Names, StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
                    if (uniqueNames.Count > 0) sb.AppendLine("  UNIQUE: " + string.Join(" | ", uniqueNames.Take(20)));
                }
            }
            sb.AppendLine("MATERIAL REMAP COVERAGE ON ACTIVE NARISVINTAGE_ROOM (READ ONLY)");
            if (vintage != null)
            {
                var materialMap = BuildMaterialMap(out int ambiguousCount);
                var roomRoot = roots.FirstOrDefault(g => string.Equals(g.name, "NarisVintage_Room", StringComparison.Ordinal));
                if (roomRoot != null)
                {
                    int slots = 0, matched = 0, matchedRenderers = 0;
                    var unmatched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var roomRenderers = roomRoot.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in roomRenderers)
                    {
                        bool rendererMatched = false;
                        foreach (var material in renderer.sharedMaterials)
                        {
                            slots++;
                            if (material != null && materialMap.ContainsKey(NormalizeName(material.name))) { matched++; rendererMatched = true; }
                            else if (material != null) unmatched.Add(material.name);
                        }
                        if (rendererMatched) matchedRenderers++;
                    }
                    sb.AppendLine($"Unique material keys={materialMap.Count}; ambiguous keys skipped={ambiguousCount}; renderers matched={matchedRenderers}/{roomRenderers.Length}; material slots matched={matched}/{slots}; unmatched slots={slots - matched}");
                    sb.AppendLine("UNMATCHED MATERIAL NAMES (first 80)");
                    foreach (var name in unmatched.OrderBy(n => n).Take(80)) sb.AppendLine("  - " + name);
                }
                else sb.AppendLine("NarisVintage_Room root not found.");
            }
            else sb.AppendLine("naris_vintage asset not loaded.");
            sb.AppendLine();
            sb.AppendLine("STATIC_BAKED KEY NAMES (table/room/lighting/props candidates)");
            if (stat != null)
                foreach (var name in stat.Names.Where(n => ContainsAny(n, "table", "felt", "rail", "pocket", "ball", "room", "wall", "floor", "ceiling", "stage", "curtain", "light", "sofa", "bar", "rug", "carpet")).OrderBy(n => n).Take(240))
                    sb.AppendLine("- " + name);

            Directory.CreateDirectory("Logs");
            var output = "Logs/wiproom_visual_audit.txt";
            File.WriteAllText(output, sb.ToString());
            Debug.Log("[WipRoomVisualAudit] wrote " + output + " (read-only; no scene save)");
        }

        private static AssetInfo Inspect(GameObject root, string path)
        {
            var info = new AssetInfo { Path = path, RootName = root.name };
            var transforms = root.GetComponentsInChildren<Transform>(true);
            info.Transforms = transforms.Length;
            foreach (var t in transforms.Skip(1)) if (!string.IsNullOrWhiteSpace(t.name)) info.Names.Add(t.name.Trim());
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            info.MeshFilters = filters.Length;
            foreach (var mf in filters) if (mf.sharedMesh != null) info.Triangles += mf.sharedMesh.triangles.LongLength / 3;
            var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var sr in skinned) if (sr.sharedMesh != null) info.Triangles += sr.sharedMesh.triangles.LongLength / 3;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            info.Renderers = renderers.Length;
            foreach (var renderer in renderers)
            {
                if (!info.HasBounds) { info.Bounds = renderer.bounds; info.HasBounds = true; }
                else info.Bounds.Encapsulate(renderer.bounds);
            }
            info.Colliders = root.GetComponentsInChildren<Collider>(true).Length;
            info.Rigidbodies = root.GetComponentsInChildren<Rigidbody>(true).Length;
            info.Lights = root.GetComponentsInChildren<Light>(true).Length;
            return info;
        }

        private static void Compare(StringBuilder sb, string label, AssetInfo a, AssetInfo b)
        {
            if (a == null || b == null) { sb.AppendLine(label + ": NOT ASSESSED (asset missing)"); return; }
            var overlap = a.Names.Intersect(b.Names, StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
            sb.AppendLine(label + $": {overlap.Count} shared names / {a.Names.Count} vs {b.Names.Count} unique names");
            foreach (var name in overlap.Take(80)) sb.AppendLine("  * " + name);
        }

        private static Dictionary<string, Material> BuildMaterialMap(out int ambiguousCount)
        {
            var map = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { ArtRoot + "/Materials" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) continue;
                var key = NormalizeName(material.name);
                if (string.IsNullOrEmpty(key) || ambiguous.Contains(key)) continue;
                if (map.ContainsKey(key)) { map.Remove(key); ambiguous.Add(key); }
                else map.Add(key, material);
            }
            ambiguousCount = ambiguous.Count;
            return map;
        }

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var normalized = new string(value.Replace(" (Instance)", string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
            while (normalized.StartsWith("147vr", StringComparison.OrdinalIgnoreCase)) normalized = normalized.Substring(5);
            return normalized;
        }

        private static bool ContainsAny(string value, params string[] tokens) => tokens.Any(token => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
#endif