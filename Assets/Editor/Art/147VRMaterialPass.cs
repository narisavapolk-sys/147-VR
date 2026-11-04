#if UNITY_EDITOR
// -----------------------------------------------------------------------------
// 147VR Room Material Pass (URP Lit rebind)
// -----------------------------------------------------------------------------
// Why: the ripped room (Assets/AAA/Imports/naris_vintage/naris_vintage.fbx) imports with
// materialImportMode = 2, and its materials end up with no BaseMap wired, so walls, floor and
// ceiling render as flat grey while the render from Blender shows walnut panelling, damask and
// parquet. This is a MATERIAL problem, not a lighting one - no amount of lights fixes it.
//
// Same loop as the light rig: look before you touch, drive everything from CSV.
//
//   147 VR > Art > 1 Dump Material Inventory (CSV)
//        Logs/material_inventory.csv : every renderer slot in the scene with its material name,
//        shader, which texture slots are wired, and whether it came from a prefab instance.
//        Logs/material_textures.csv  : the texture catalogue next to the FBX (.fbm) with
//        sRGB/normal flags, so names can be matched reliably.
//
//   147 VR > Art > 2 Write Material Map (auto-match)
//        Logs/material_map.csv       : materialName -> baseMap/normalMap/emissiveMap, with
//        use=1 only where the name match is confident. Edit this file (Excel/VSCode) to
//        connect anything the matcher missed, then re-Apply. No recompile needed.
//
//   147 VR > Art > 3 Apply Material Rebind (+ Save Scene)
//        Creates URP Lit materials under Assets/AAA/Imports/naris_vintage/Rebound/ and assigns
//        them as SCENE OVERRIDES on the renderers. The FBX and its extracted materials are never
//        modified, so this is fully reversible.
//
//   147 VR > Art > 4 Revert Material Overrides
//        Puts every touched renderer back on the material its prefab source uses.
//
// Report: Logs/materialPass_report.txt
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VR147.EditorTools
{
    public static class MaterialPass
    {
        const string InventoryCsv = "Logs/material_inventory.csv";
        const string TextureCsv   = "Logs/material_textures.csv";
        const string MapCsv       = "Logs/material_map.csv";
        const string ReportFile   = "Logs/materialPass_report.txt";
        const string ReboundDir   = "Assets/AAA/Imports/naris_vintage/Rebound";
        const string TextureDir   = "Assets/AAA/Imports/naris_vintage/naris_vintage.fbm";
        const string LitShader    = "Universal Render Pipeline/Lit";
        const string RigRootName  = "147VR_MaterialPass";

        sealed class Slot
        {
            public string rendererPath;
            public string rendererName;
            public int index;
            public string materialName;
            public string shader;
            public string baseMap;
            public string normalMap;
            public string materialAsset;
            public bool fromPrefab;
            public int renderers;
            /// <summary>The material's own _BaseColor ("r,g,b,a"). This is how a texture-less
            /// material (Table Mahogany, 147vr_b1_darkframe, FELT …) keeps its intended colour.</summary>
            public string baseColor;
            public string smoothness;
            /// <summary>Renderer enabled state + world centre, so "I cannot see it" becomes a
            /// question the CSV answers instead of a guess.</summary>
            public bool active;
            public string position;
        }

        // ---- menu ----

        [MenuItem("147 VR/Art/1 Dump Material Inventory (CSV)", false, 320)]
        static void MenuDump()
        {
            var log = new StringBuilder();
            var slots = ScanScene();
            WriteInventory(slots, log);
            WriteTextures(log);
            Finish(log);
        }

        [MenuItem("147 VR/Art/2 Write Material Map (auto-match)", false, 321)]
        static void MenuMap()
        {
            var log = new StringBuilder();
            WriteMap(ScanScene(), log, true);
            Finish(log);
        }

        [MenuItem("147 VR/Art/3 Apply Material Rebind (+ Save Scene)", false, 322)]
        static void MenuApply() => Apply(saveScene: true);

        [MenuItem("147 VR/Art/4 Revert Material Overrides", false, 323)]
        static void MenuRevert() => Revert();

        // ---- public entry points for ToolRunner (Logs/editor_cmd.txt) ----

        public static void Dump()
        {
            var log = new StringBuilder();
            var slots = ScanScene();
            WriteInventory(slots, log);
            WriteTextures(log);
            Finish(log);
        }

        public static void AutoMap()
        {
            var log = new StringBuilder();
            WriteMap(ScanScene(), log, true);
            Finish(log);
        }

        public static void ApplyRebind(bool saveScene) => Apply(saveScene);

        public static void RevertOverrides() => Revert();

        // ---- scan ----

        static List<Slot> ScanScene()
        {
            var list = new List<Slot>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r == null || r is LineRenderer || r is TrailRenderer)
                    continue;
                Material[] mats;
                try { mats = r.sharedMaterials; }
                catch { continue; }
                if (mats == null)
                    continue;

                var src = PrefabUtility.GetCorrespondingObjectFromSource(r);
                bool fromPrefab = src != null;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null)
                        continue;
                    list.Add(new Slot
                    {
                        rendererPath  = HierarchyPath(r.transform),
                        rendererName  = r.name,
                        index         = i,
                        materialName  = m.name,
                        shader        = m.shader != null ? m.shader.name : "<null>",
                        baseMap       = Tex(m, "_BaseMap") ?? Tex(m, "_MainTex") ?? Tex(m, "_ColorMap"),
                        normalMap     = Tex(m, "_BumpMap") ?? Tex(m, "_Bump"),
                        materialAsset = AssetDatabase.GetAssetPath(m),
                        fromPrefab    = fromPrefab,
                        renderers     = 1,
                        baseColor     = ColorText(m),
                        smoothness    = ValueText(m, "_Smoothness"),
                        active        = r.gameObject.activeInHierarchy,
                        position      = string.Format(CultureInfo.InvariantCulture, "{0:F2},{1:F2},{2:F2}",
                                                      r.bounds.center.x, r.bounds.center.y, r.bounds.center.z),
                    });
                }
            }
            return list;
        }

        static string Tex(Material m, string prop)
        {
            if (m == null || !m.HasProperty(prop))
                return null;
            var t = m.GetTexture(prop);
            if (t == null)
                return null;
            string p = AssetDatabase.GetAssetPath(t);
            return string.IsNullOrEmpty(p) ? t.name : p;
        }

        static string ColorText(Material m)
        {
            if (m == null) return "";
            Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                      : (m.HasProperty("_DiffuseColor") ? m.GetColor("_DiffuseColor")
                      : (m.HasProperty("Color") ? m.GetColor("Color") : Color.white));
            return string.Format(CultureInfo.InvariantCulture, "{0:F4},{1:F4},{2:F4},{3:F4}", c.r, c.g, c.b, c.a);
        }

        static string ValueText(Material m, string prop)
        {
            if (m == null || !m.HasProperty(prop)) return "";
            return m.GetFloat(prop).ToString("F4", CultureInfo.InvariantCulture);
        }

        static string HierarchyPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null) { stack.Push(t.name); t = t.parent; }
            return string.Join("/", stack);
        }

        // ---- dumps ----

        static void WriteInventory(List<Slot> slots, StringBuilder log)
        {
            Directory.CreateDirectory("Logs");
            var sb = new StringBuilder();
            sb.AppendLine("rendererPath,rendererName,slot,materialName,shader,baseMap,normalMap,materialAsset,fromPrefab,baseColor,smoothness,active,worldPos");
            foreach (var s in slots.OrderBy(s => s.rendererPath).ThenBy(s => s.index))
                sb.AppendLine(string.Join(",", C(s.rendererPath), C(s.rendererName), s.index.ToString(), C(s.materialName),
                                            C(s.shader), C(s.baseMap), C(s.normalMap), C(s.materialAsset), s.fromPrefab ? "1" : "0",
                                            C(s.baseColor), C(s.smoothness), s.active ? "1" : "0", C(s.position)));
            File.WriteAllText(InventoryCsv, sb.ToString());
            int noTex = slots.Count(s => string.IsNullOrEmpty(s.baseMap));
            log.AppendLine($"[OK] {slots.Count} material slots -> {Path2(InventoryCsv)}");
            log.AppendLine($"     {noTex} slot(s) have NO base map wired ({(slots.Count > 0 ? 100.0 * noTex / slots.Count : 0):F0}%)");
            foreach (var g in slots.GroupBy(s => s.shader).OrderByDescending(g => g.Count()).Take(8))
                log.AppendLine($"     shader {g.Key,-46} {g.Count(),4} slots, no-baseMap {g.Count(x => string.IsNullOrEmpty(x.baseMap))}");
        }

        static void WriteTextures(StringBuilder log)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureDir });
            var sb = new StringBuilder();
            sb.AppendLine("name,path,sRGB,textureType,width,height");
            int n = 0;
            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                if (tex == null)
                    continue;
                sb.AppendLine(string.Join(",", C(tex.name), C(p),
                    (imp != null && imp.sRGBTexture) ? "1" : "0",
                    C(imp != null ? imp.textureType.ToString() : "?"),
                    tex.width.ToString(), tex.height.ToString()));
                n++;
            }
            File.WriteAllText(TextureCsv, sb.ToString());
            log.AppendLine($"[OK] {n} textures catalogued -> {Path2(TextureCsv)} (folder {TextureDir})");
            if (n == 0)
                log.AppendLine("     [WARN] no textures found in that folder - the rip may keep them elsewhere");
        }

        // ---- auto map ----

        static void WriteMap(List<Slot> slots, StringBuilder log, bool force)
        {
            Directory.CreateDirectory("Logs");
            if (File.Exists(MapCsv) && !force)
            {
                log.AppendLine("[INFO] existing map kept: " + MapCsv);
                return;
            }

            var texByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var texByStrip = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TextureDir }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (tex == null) continue;
                string key = Normalize(tex.name);
                if (!texByName.ContainsKey(key))
                    texByName[key] = p;
                if (!IsUtilityMap(key))
                {
                    string strip = StripBaseSuffix(key);
                    if (strip.Length > 2 && !texByStrip.ContainsKey(strip))
                        texByStrip[strip] = p;
                }
            }

            var rows = new Dictionary<string, MapRow>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in slots)
            {
                if (rows.ContainsKey(s.materialName))
                    continue;
                string key = Normalize(s.materialName);
                string baseMap;
                if (texByName.TryGetValue(key, out baseMap) == false)
                    texByStrip.TryGetValue(StripBaseSuffix(key), out baseMap);
                string normal = null;
                if (baseMap != null)
                {
                    string bn = Normalize(Path.GetFileNameWithoutExtension(baseMap));
                    foreach (var suf in new[] { " n", "_n", " normal", "_normal", "_nm", " nm", "_no" })
                    {
                        if (texByName.TryGetValue(bn + suf, out normal)) break;
                    }
                    if (normal == null)
                    {
                        string bs = StripBaseSuffix(bn);
                        foreach (var suf in new[] { " n", "_n", " normal", "_normal" })
                        {
                            if (texByName.TryGetValue(bs + suf, out normal)) break;
                        }
                    }
                }
                rows[s.materialName] = new MapRow
                {
                    material = s.materialName,
                    baseMap = baseMap ?? "",
                    normal = normal ?? "",
                    use = baseMap != null ? 1 : 0,
                    note = baseMap != null ? "auto-matched" : "no texture with this name in .fbm - wire by hand or import the missing maps",
                };
            }

            var sb = new StringBuilder();
            sb.AppendLine("material,use,baseMap,normalMap,smoothness,emissive,note");
            foreach (var kv in rows.OrderBy(k => k.Key))
            {
                var r = kv.Value;
                sb.AppendLine(string.Join(",", C(r.material), r.use.ToString(), C(r.baseMap), C(r.normal),
                                            "", "0", C(r.note)));
            }
            File.WriteAllText(MapCsv, sb.ToString());
            int on = rows.Values.Count(r => r.use == 1);
            log.AppendLine($"[OK] {rows.Count} unique materials -> {Path2(MapCsv)}  (auto-enabled {on}, needs hand work {rows.Count - on})");
            foreach (var r in rows.Values.Where(v => v.use == 1).OrderBy(v => v.material).Take(20))
                log.AppendLine($"     + {r.material}  <=  {Path.GetFileName(r.baseMap)}{(string.IsNullOrEmpty(r.normal) ? "" : " + " + Path.GetFileName(r.normal))}");
        }

        static readonly string[] UtilityTokens = { " n", "_n", " normal", "_normal", "_nm", " nm", "_no",
            " rough", "_rough", "_r", " metal", "_metal", "_m", " ao", "_ao", "occlusion", "_occ", "_orm", " mask", "_mask" };

        static bool IsUtilityMap(string normalized)
        {
            foreach (string t in UtilityTokens)
                if (normalized.EndsWith(t, StringComparison.Ordinal))
                    return true;
            return false;
        }

        static readonly string[] BaseSuffixes = { "_base_color", " base_color", "_basecolor", " basecolor",
            " base color", "_albedo", " albedo", "_alb", " alb", "_diffuse", " diffuse", "_color", " color", "_d", " d" };

        static string StripBaseSuffix(string normalized)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (string s in BaseSuffixes)
                {
                    if (normalized.Length > s.Length && normalized.EndsWith(s, StringComparison.Ordinal))
                    {
                        normalized = normalized.Substring(0, normalized.Length - s.Length).TrimEnd('_', ' ');
                        changed = true;
                        break;
                    }
                }
            }
            return normalized;
        }

        sealed class MapRow { public string material, baseMap, normal, note; public int use; public float smoothness = -1f; }

        static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("(Instance)", "", StringComparison.OrdinalIgnoreCase);
            var sb = new StringBuilder(s.Length);
            foreach (char ch in s)
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == ' ') sb.Append(char.ToLowerInvariant(ch));
            return sb.ToString().Trim();
        }

        // ---- apply ----

        static void Apply(bool saveScene)
        {
            var log = new StringBuilder();
            log.AppendLine($"[MaterialPass] Apply  {DateTime.Now:HH:mm:ss}");
            var scene = SceneManager.GetActiveScene();
            log.AppendLine($"scene: {scene.name} ({scene.path})");

            if (!File.Exists(MapCsv))
            {
                log.AppendLine("[INFO] no map yet, scanning + auto-matching first");
                WriteMap(ScanScene(), log, false);
            }
            if (!File.Exists(MapCsv))
            {
                log.AppendLine("[ABORT] could not produce " + MapCsv);
                Finish(log);
                return;
            }

            var shader = Shader.Find(LitShader);
            if (shader == null)
            {
                log.AppendLine($"[ABORT] shader '{LitShader}' not found (URP missing?)");
                Finish(log);
                return;
            }

            Directory.CreateDirectory(ReboundDir);
            var byMaterial = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            int applied = 0, skipped = 0, created = 0, slots = 0;

            foreach (var line in File.ReadAllLines(MapCsv).Skip(1))
            {
                var c = SplitCsv(line);
                if (c.Count < 4) continue;
                string matName = c[0];
                if (c[1] != "1") { skipped++; continue; }

                Texture2D baseTex = string.IsNullOrWhiteSpace(c[2])
                    ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(c[2]);
                Texture2D normalTex = c.Count > 3 && !string.IsNullOrWhiteSpace(c[3])
                    ? AssetDatabase.LoadAssetAtPath<Texture2D>(c[3]) : null;
                Color emis = Color.white;
                bool hasEmis = TryColor(c.Count > 5 ? c[5] : "", out emis);
                Color colOnly = Color.white;
                bool hasCol = c.Count > 7 && TryColor(c[7], out colOnly);
                if (baseTex == null && !hasEmis && !hasCol)
                {
                    log.AppendLine($"   [skip] {matName}: no baseMap, no emissive and no base colour");
                    skipped++;
                    continue;
                }

                Material rebound = GetOrCreate(matName, shader, byMaterial, log, ref created);
                rebound.shader = shader;
                if (baseTex != null)
                {
                    rebound.SetTexture("_BaseMap", baseTex);
                    rebound.SetColor("_BaseColor", hasCol ? colOnly : Color.white);
                }
                else if (hasCol)
                {
                    // colour-only material: keep the value the FBX import carried (mahogany, felt…)
                    rebound.SetColor("_BaseColor", colOnly);
                }
                else
                {
                    // emissive-only (a bulb): keep the surface black so only the glow shows
                    rebound.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.02f, 1f));
                }
                float smooth;
                if (c.Count > 4 && float.TryParse(c[4], NumberStyles.Float, CultureInfo.InvariantCulture, out smooth) && smooth >= 0f)
                    rebound.SetFloat("_Smoothness", smooth);
                if (normalTex != null)
                {
                    EnsureLinear(normalTex, log);
                    rebound.SetTexture("_BumpMap", normalTex);
                    rebound.SetFloat("_BumpScale", 1f);
                    rebound.EnableKeyword("_NORMALMAP");
                }
                if (hasEmis)
                {
                    rebound.EnableKeyword("_EMISSION");
                    rebound.SetColor("_EmissionColor", emis);
                    log.AppendLine($"   [glow] {matName} emissive=({emis.r:F2}, {emis.g:F2}, {emis.b:F2})");
                }
                EditorUtility.SetDirty(rebound);

                foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                {
                    if (r == null || r is LineRenderer || r is TrailRenderer) continue;
                    Material[] mats;
                    try { mats = r.sharedMaterials; } catch { continue; }
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null || !string.Equals(mats[i].name, matName, StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (mats[i] == rebound) continue;
                        mats[i] = rebound;
                        changed = true;
                        slots++;
                    }
                    if (changed)
                    {
                        r.sharedMaterials = mats;
                        EditorUtility.SetDirty(r);
                        applied++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            log.AppendLine($"[OK] rebound {slots} renderer slot(s) across {applied} renderer(s), " +
                           $"{created} URP Lit material(s) created in {ReboundDir}, {skipped} map row(s) skipped");
            if (applied == 0)
                log.AppendLine("[WARN] nothing was rebound - check that material names in the map still exist in the scene");

            if (saveScene)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveOpenScenes();
                log.AppendLine(saved ? $"[OK] scene saved -> {scene.path}" : "[WARN] scene save refused; overrides live only in memory");
            }
            Finish(log);
        }

        static Material GetOrCreate(string matName, Shader shader, Dictionary<string, Material> cache, StringBuilder log, ref int created)
        {
            Material m;
            if (cache.TryGetValue(matName, out m)) return m;
            string safe = string.Join("_", matName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            string assetPath = $"{ReboundDir}/147VR_{safe}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (m == null)
            {
                m = new Material(shader) { name = $"147VR_{safe}" };
                AssetDatabase.CreateAsset(m, assetPath);
                created++;
                log.AppendLine($"   [new] {assetPath}");
            }
            cache[matName] = m;
            return m;
        }

        /// <summary>Parse the emissive column, "r,g,b" in 0..1 (values above 1 are allowed so a
        /// bulb can glow into bloom). Empty, "0" or an unparsable triple means "no emission".</summary>
        static bool TryColor(string s, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(s) || s.Trim() == "0")
                return false;
            string[] p = s.Split(',');
            if (p.Length != 3 && p.Length != 4)
                return false;
            float r, g, b, a = 1f;
            if (!float.TryParse(p[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out r) ||
                !float.TryParse(p[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out g) ||
                !float.TryParse(p[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out b))
                return false;
            if (p.Length == 4 && !float.TryParse(p[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out a))
                a = 1f;
            color = new Color(r, g, b, a);
            return true;
        }

        static void EnsureLinear(Texture2D tex, StringBuilder log)
        {
            string p = AssetDatabase.GetAssetPath(tex);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null || !imp.sRGBTexture)
                return;                     // already linear
            imp.sRGBTexture = false;
            imp.SaveAndReimport();
            log.AppendLine($"   [fix] {Path.GetFileName(p)} was imported sRGB; set to Linear for use as a normal map");
        }

        // ---- revert ----

        static void Revert()
        {
            var log = new StringBuilder();
            log.AppendLine($"[MaterialPass] Revert  {DateTime.Now:HH:mm:ss}");
            int restored = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r == null || r is LineRenderer || r is TrailRenderer) continue;
                var src = PrefabUtility.GetCorrespondingObjectFromSource(r) as Renderer;
                if (src == null) continue;
                Material[] original;
                try { original = src.sharedMaterials; } catch { continue; }
                Material[] current;
                try { current = r.sharedMaterials; } catch { continue; }
                if (original.Length != current.Length) continue;
                bool diff = false;
                for (int i = 0; i < current.Length; i++)
                    if (current[i] != original[i]) { diff = true; break; }
                if (!diff) continue;
                r.sharedMaterials = original;
                EditorUtility.SetDirty(r);
                restored++;
            }
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveOpenScenes();
            log.AppendLine($"[OK] {restored} renderer(s) back on their prefab materials; scene {(saved ? "saved" : "NOT saved")}");
            log.AppendLine("[INFO] the created materials stay in " + ReboundDir + " (delete by hand if unwanted)");
            Finish(log);
        }

        // ---- helpers ----

        static string C(string s) => string.IsNullOrEmpty(s) ? "" : (s.Contains(",") ? "\"" + s.Replace("\"", "\"\"") + "\"" : s);
        static string Path2(string p) => Path.GetFullPath(p);

        static List<string> SplitCsv(string line)
        {
            var outp = new List<string>();
            if (string.IsNullOrEmpty(line)) return outp;
            int i = 0;
            while (i < line.Length)
            {
                var sb = new StringBuilder();
                bool quoted = false;
                while (i < line.Length)
                {
                    char ch = line[i];
                    if (quoted)
                    {
                        if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i += 2; continue; }
                        if (ch == '"') { quoted = false; i++; continue; }
                        sb.Append(ch); i++; continue;
                    }
                    if (ch == '"') { quoted = true; i++; continue; }
                    if (ch == ',') { i++; break; }
                    sb.Append(ch); i++;
                }
                outp.Add(sb.ToString());
            }
            return outp;
        }

        /// <summary>Set by ToolRunner so automated runs do not pop a modal dialog.</summary>
        public static bool NoDialog;

        static void Finish(StringBuilder log, string reportName = ReportFile)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(reportName, log.ToString());
            foreach (var line in log.ToString().Split('\n'))
                Debug.Log(line.TrimEnd());
            Debug.Log($"[MaterialPass] report -> {Path2(reportName)}");
            if (!Application.isBatchMode && !NoDialog)
                EditorUtility.DisplayDialog("MaterialPass", log.ToString().Trim(), "OK");
        }
    }
}
#endif

// round 8: TableKey per bulb, baseColor dump + colour-only rebind
