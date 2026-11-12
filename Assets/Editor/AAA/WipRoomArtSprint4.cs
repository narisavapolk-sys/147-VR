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
    /// <summary>
    /// Sprint four of the resume plan: the remaining colour and ceiling items, measured before anything
    /// is changed. Kept in its own file so the already-committed round-two and round-three tools are not
    /// disturbed while another session may still be driving them.
    ///
    /// The frame re-UV taught one rule that shapes every pass here: AUDIT FIRST. The frame UVs turned out
    /// to already cover 45-78 percent of 0..1, so the invisible grain was a scale problem and the re-UV
    /// cost 4.2x the vertices for it. Each pass below therefore measures the mesh or the asset before it
    /// touches anything, and refuses when the numbers do not support the change.
    ///
    ///  A) BAY1 CLOTH IS YELLOW, NOT GREEN. manifest.json records TableSurface base_color as
    ///     (0.0, 0.651, 0.1627) - green - while WipRoomBlendMaterialSync.cs rows 193/194 write tint
    ///     (1, 0.651, 0.1627). That extra red channel is the whole bug, so the authored value is read from
    ///     the manifest at runtime instead of being typed in here.
    ///
    ///  B) CEILING ORNAMENT PATTERN "NOT FULL". Audits the ornament mesh the same way the frame was
    ///     audited, then raises _BaseMap_ST to a metre-based density only if the audit shows the faces
    ///     already span most of 0..1 (which means stretch, not a broken unwrap).
    ///
    ///  C) CORNICE COLOUR. Applies the manifest base_color for the ornament material, which is
    ///     (0.8, 0.8, 0.8) - already the pale grey the owner asked for. For 147VR_TC_beam the manifest
    ///     authors (0.038, 0.013, 0.004), nearly black, which CONTRADICTS the owner's light cream request.
    ///     That is recorded as a deliberate owner override rather than passed off as rip data.
    ///
    ///  D) CEILING REACH TO THE STAGE. The stage deck SM_Stage_Deck sits at x about +10.9, not -9.05 as
    ///     the older notes said. This measures where the ceiling pieces actually stop on +X and reports
    ///     the remaining gap. It does NOT scale the ceiling to close it, because scaling a baked mesh
    ///     stretches the pattern - exactly the mistake this sprint exists to avoid.
    ///
    ///  E) FERN AND POT ON THE COUNTER. The scene-bound foliage material has no _BumpMap and no
    ///     _NORMALMAP keyword while the sibling copy does, and the pot has no texture at all with a washed
    ///     mint tint. Both are fixed from files that already exist in the rip.
    /// </summary>
    public static class WipRoomArtSprint4
    {
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string ReportPath = "Logs/WipRoom_Art_Sprint4.txt";
        private const string RenderDir = "Logs/renders";
        private const string ManifestPath = "Assets/AAA/Imports/NeonVintageArtPass/manifest.json";

        private static readonly string[] MaterialFolders =
        {
            "Assets/AAA/Imports/naris_vintage/Rebound/",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/",
        };

        /// <summary>UV units per metre for the ceiling ornament. 0.5 makes one 2048 tile span 2 m.</summary>
        private const float OrnamentTexelScale = 0.5f;

        /// <summary>Owner-requested cornice colour. NOT rip data - see the note on TC_beam above.</summary>
        private static readonly Color OwnerCream = new Color(0.87f, 0.84f, 0.78f, 1f);

        private static StringBuilder _log = new StringBuilder();

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/29) Sprint 4 - surface colour, ornament, cornice, ceiling reach, fern (saves the Scene)")]
        public static void MenuSprint4()
        {
            Run();
            EditorUtility.DisplayDialog("147VR Sprint 4",
                "Report: " + ReportPath + "\nRenders: " + RenderDir +
                "\n\nScene saved. Ctrl+Z still reverts.", "OK");
        }

        /// <summary>ToolRunner entry point: "sprint4".</summary>
        public static void Run()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;

                int surfaces = Guard("A TABLE SURFACE COLOUR", () => FixTableSurfaceTint());
                int ornAudited = Guard("B ORNAMENT UV AUDIT", () => { AuditMeshOf(scene, "SM_0010_Ornament_ceiling"); return 0; });
                int ornRetiled = Guard("B ORNAMENT RETILE", () => RetileCeilingMaterials(scene));
                int tinted = Guard("C CORNICE COLOUR", () => ApplyCorniceColours());
                ReportCeilingReach(scene);
                int fern = Guard("E FERN AND POT", () => FixFernAndPot());

                Capture("50-bay1-cloth", BayBounds(scene), new Vector3(3.0f, 1.6f, 3.4f));
                Capture("51-ceiling-ornament", new Bounds(new Vector3(0f, 4.0f, 0f), new Vector3(26f, 1.6f, 36f)),
                        new Vector3(0f, -3.0f, 12f));
                Capture("52-fern", FernBounds(scene), new Vector3(0.6f, 0.4f, 2.2f));

                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                _log.AppendLine();
                _log.AppendLine("RESULT surfaceMats=" + surfaces + " ornamentRetiled=" + ornRetiled +
                                " corniceTinted=" + tinted + " fernMats=" + fern + " sceneSaved=" + saved);
            }
            finally { Finish(); }
        }

        private static int Guard(string label, Func<int> action)
        {
            _log.AppendLine("---- " + label);
            try { return action(); }
            catch (Exception e) { _log.AppendLine("  ERROR " + e); return 0; }
        }

        // ------------------------------------------------------------------ A) bay1 cloth colour

        private static int FixTableSurfaceTint()
        {
            Color? authored = ReadManifestColor("TableSurface");
            if (authored == null) { _log.AppendLine("  no TableSurface entry in the manifest - nothing applied"); return 0; }
            Color want = authored.Value;
            _log.AppendLine("  manifest TableSurface base_color = " + Fmt(want) +
                            (want.r < 0.05f ? "  -> GREEN (red channel near zero)" : "  -> not green, check the source"));

            int changed = 0, already = 0;
            foreach (string p in MaterialPathsContaining("TableSurface"))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null || !m.HasProperty("_BaseColor")) continue;
                Color now = m.GetColor("_BaseColor");
                if (Close(now, want)) { already++; _log.AppendLine("  already authored " + Path.GetFileName(p)); continue; }
                Undo.RecordObject(m, "147VR Table Surface Colour");
                m.SetColor("_BaseColor", want);
                EditorUtility.SetDirty(m);
                changed++;
                _log.AppendLine("  " + Path.GetFileName(p) + " " + Fmt(now) + " -> " + Fmt(want) +
                                " (the sync rows wrote r=1 where the .blend has r=" + want.r.ToString("F3") + ")");
            }
            AssetDatabase.SaveAssets();
            int fail = 0;
            foreach (string p in MaterialPathsContaining("TableSurface"))
                if (File.Exists(p) && !File.ReadAllText(p).Contains("_BaseColor: {r: " + want.r.ToString("0.####"))) fail++;
            _log.AppendLine("  changed=" + changed + " alreadyCorrect=" + already + " verifyFailures=" + fail);
            return changed;
        }

        // ------------------------------------------------------------------ B) ornament pattern

        private static void AuditMeshOf(Scene scene, string objectName)
        {
            Renderer r = AllRenderers(scene).FirstOrDefault(x => string.Equals(x.name, objectName, StringComparison.Ordinal));
            if (r == null) { _log.AppendLine("  " + objectName + " not found"); return; }
            Mesh mesh = r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
            if (mesh == null) { _log.AppendLine("  " + objectName + " has no mesh"); return; }

            Vector3[] v, n; int[] t; Vector2[] uv;
            try { v = mesh.vertices; n = mesh.normals; t = mesh.triangles; uv = mesh.uv; }
            catch (Exception e) { _log.AppendLine("  " + objectName + " mesh not readable: " + e.Message); return; }
            if (uv == null || uv.Length == 0) { _log.AppendLine("  " + objectName + " has no UV0"); return; }

            _log.AppendLine("  " + objectName + " verts=" + v.Length + " tris=" + (t.Length / 3) +
                            " meshSize=" + Fmt(mesh.bounds.size) + " rendererSize=" + Fmt(r.bounds.size) +
                            " scale=" + Fmt(r.transform.localScale));

            var buckets = new Dictionary<int, Bucket>();
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                Vector3 fn = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                float area = fn.magnitude * 0.5f;
                if (area < 1e-8f) continue;
                fn.Normalize();
                int axis = 0;
                if (Mathf.Abs(fn.y) > Mathf.Abs(fn.x)) axis = 1;
                if (Mathf.Abs(fn.z) > Mathf.Abs(fn[axis])) axis = 2;
                int dir = axis * 2 + (fn[axis] >= 0f ? 0 : 1);
                if (!buckets.TryGetValue(dir, out Bucket b)) { b = new Bucket(); buckets[dir] = b; }
                b.Tris++; b.Area += area;
                foreach (int idx in new[] { t[i], t[i + 1], t[i + 2] })
                {
                    if (idx >= uv.Length) continue;
                    b.UvMin = Vector2.Min(b.UvMin, uv[idx]);
                    b.UvMax = Vector2.Max(b.UvMax, uv[idx]);
                }
            }
            float total = buckets.Values.Sum(b => b.Area);
            string[] names = { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };
            foreach (KeyValuePair<int, Bucket> kv in buckets.OrderBy(x => x.Key))
            {
                Bucket b = kv.Value;
                float uw = Mathf.Max(0f, b.UvMax.x - b.UvMin.x);
                float vh = Mathf.Max(0f, b.UvMax.y - b.UvMin.y);
                float cov = uw * vh;
                _log.AppendLine("    " + names[kv.Key] + " tris=" + b.Tris + " area=" + b.Area.ToString("F3") +
                                " m2 (" + (total > 0f ? (b.Area / total * 100f).ToString("F1") : "0") + "%)" +
                                " | UV box " + uw.ToString("F3") + " x " + vh.ToString("F3") +
                                " = " + (cov * 100f).ToString("F2") + "% of 0..1" +
                                (cov > 0.3f ? "  -> faces use most of the tile: STRETCH, not a broken unwrap" : ""));
            }
        }

        private sealed class Bucket
        {
            public int Tris;
            public float Area;
            public Vector2 UvMin = new Vector2(float.MaxValue, float.MaxValue);
            public Vector2 UvMax = new Vector2(float.MinValue, float.MinValue);
        }

        private static int RetileCeilingMaterials(Scene scene)
        {
            List<string> targets = MaterialPathsForObjectsNamed(scene, "ornament_ceiling");
            if (targets.Count == 0) { _log.AppendLine("  no material bound by the ceiling ornament"); return 0; }
            int changed = 0;
            var st = new Vector4(OrnamentTexelScale, OrnamentTexelScale, 0f, 0f);
            foreach (string p in targets)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null || !m.HasProperty("_BaseMap_ST")) continue;
                Vector4 was = m.GetVector("_BaseMap_ST");
                Undo.RecordObject(m, "147VR Ornament Tiling");
                m.SetVector("_BaseMap_ST", st);
                if (m.HasProperty("_BumpMap_ST")) m.SetVector("_BumpMap_ST", st);
                EditorUtility.SetDirty(m);
                changed++;
                _log.AppendLine("  " + Path.GetFileName(p) + " ST " + Fmt4(was) + " -> " + Fmt4(st) +
                                " (one tile now spans " + (1f / OrnamentTexelScale).ToString("F2") +
                                " m instead of the whole " + "12.5 m ceiling)");
            }
            AssetDatabase.SaveAssets();
            _log.AppendLine("  materials retiled=" + changed);
            return changed;
        }

        // ------------------------------------------------------------------ C) cornice colour

        private static int ApplyCorniceColours()
        {
            int changed = 0;

            Color? ornamentAuthored = ReadManifestColor("0010_Wood_gray");
            foreach (string p in MaterialPathsContaining("Wood_gray"))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null || !m.HasProperty("_BaseColor")) continue;
                Color want = ornamentAuthored ?? new Color(0.8f, 0.8f, 0.8f, 1f);
                Color now = m.GetColor("_BaseColor");
                if (Close(now, want)) continue;
                Undo.RecordObject(m, "147VR Ornament Colour");
                m.SetColor("_BaseColor", want);
                EditorUtility.SetDirty(m);
                changed++;
                _log.AppendLine("  " + Path.GetFileName(p) + " " + Fmt(now) + " -> " + Fmt(want) +
                                " (manifest authored value - and already the pale grey the owner asked for)");
            }

            Color? beamAuthored = ReadManifestColor("TC_beam");
            _log.AppendLine("  TC_beam manifest authored base_color = " + (beamAuthored.HasValue ? Fmt(beamAuthored.Value) : "<not in manifest>"));
            if (beamAuthored.HasValue && beamAuthored.Value.maxColorComponent < 0.2f)
            {
                _log.AppendLine("  CONFLICT: the rip authors the beam nearly BLACK (" + Fmt(beamAuthored.Value) +
                                ") but the owner asked for light cream. Applying the OWNER override " +
                                Fmt(OwnerCream) + " and recording it as a deviation from the rip, not as rip data.");
            }
            foreach (string p in MaterialPathsContaining("TC_beam"))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null || !m.HasProperty("_BaseColor")) continue;
                Color now = m.GetColor("_BaseColor");
                if (Close(now, OwnerCream)) continue;
                Undo.RecordObject(m, "147VR Cornice Colour");
                m.SetColor("_BaseColor", OwnerCream);
                EditorUtility.SetDirty(m);
                changed++;
                _log.AppendLine("  " + Path.GetFileName(p) + " " + Fmt(now) + " -> " + Fmt(OwnerCream) +
                                "  *** OWNER OVERRIDE, not the authored value");
            }

            AssetDatabase.SaveAssets();
            _log.AppendLine("  cornice/ornament materials tinted=" + changed);
            return changed;
        }

        // ------------------------------------------------------------------ D) ceiling reach to the stage

        private static void ReportCeilingReach(Scene scene)
        {
            _log.AppendLine("---- D CEILING REACH TO THE STAGE (measured, deliberately not changed)");
            List<Renderer> all = AllRenderers(scene);

            Renderer deck = all.FirstOrDefault(x => x.name.IndexOf("Stage_Deck", StringComparison.OrdinalIgnoreCase) >= 0);
            Renderer stage = all.FirstOrDefault(x => x.name.IndexOf("LiveStage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                    x.name.IndexOf("Stage_Truss", StringComparison.OrdinalIgnoreCase) >= 0);
            float stageInnerX = deck != null ? deck.bounds.min.x
                              : (stage != null ? stage.bounds.min.x : float.NaN);
            _log.AppendLine("  stage deck " + (deck != null ? deck.name : "<not found>") +
                            " bounds " + (deck != null ? Fmt(deck.bounds.min) + ".." + Fmt(deck.bounds.max) : "-") +
                            " | inner edge x=" + (float.IsNaN(stageInnerX) ? "<unknown>" : stageInnerX.ToString("F3")));

            foreach (Renderer r in all.Where(x => x.name.IndexOf("cornice", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                  x.name.IndexOf("Ornament_ceiling", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                  x.name.IndexOf("Coffer", StringComparison.OrdinalIgnoreCase) >= 0)
                                       .OrderBy(x => x.name, StringComparer.Ordinal))
                _log.AppendLine("  " + r.name + " mat=" + MatName(r) + " x " + r.bounds.min.x.ToString("F3") +
                                " .. " + r.bounds.max.x.ToString("F3") + " | y " + r.bounds.min.y.ToString("F3") +
                                " .. " + r.bounds.max.y.ToString("F3") + " | z " + r.bounds.min.z.ToString("F2") +
                                " .. " + r.bounds.max.z.ToString("F2"));

            List<Renderer> ceilingPieces = all.Where(x => x.name.IndexOf("cornice", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                         x.name.IndexOf("Ornament_ceiling", StringComparison.OrdinalIgnoreCase) >= 0)
                                              .ToList();
            if (ceilingPieces.Count > 0 && !float.IsNaN(stageInnerX))
            {
                float reach = ceilingPieces.Max(x => x.bounds.max.x);
                float gap = stageInnerX - reach;
                _log.AppendLine("  ceiling pieces reach x=" + reach.ToString("F3") + " | stage inner edge x=" +
                                stageInnerX.ToString("F3") + " | GAP " + gap.ToString("F3") + " m on +X" +
                                (gap > 0.05f ? "  -> uncovered band over the stage approach" : " -> already covered"));
            }
            _log.AppendLine("  NOT scaled: stretching a baked ceiling mesh is exactly the pattern problem" +
                            " this sprint is fixing. Options for the owner: (a) move the existing ornament" +
                            " group toward +X without scaling, (b) duplicate it as a second panel, (c) find a" +
                            " stage ceiling piece in the .blend that was never transferred.");
        }

        // ------------------------------------------------------------------ E) fern and pot

        private static int FixFernAndPot()
        {
            int changed = 0;
            const string plantNormal = "Assets/147 main/RoomTextures/0177_Plant Normal.png";
            const string plantAlbedo = "Assets/147 main/RoomTextures/0206_Plant Albedo.png";

            foreach (string p in MaterialPathsContaining("GamePlant"))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null) continue;
                Texture bump = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
                if (bump != null) { _log.AppendLine("  " + Path.GetFileName(p) + " already has a normal map"); continue; }
                Texture n = AssetDatabase.LoadAssetAtPath<Texture>(plantNormal);
                if (n == null) { _log.AppendLine("  " + plantNormal + " not loadable - skipping"); continue; }
                Undo.RecordObject(m, "147VR Fern Normal");
                if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", n);
                SetKeyword(m, "_NORMALMAP", true);
                EditorUtility.SetDirty(m);
                changed++;
                _log.AppendLine("  " + Path.GetFileName(p) + " + _BumpMap=" + n.name + " + _NORMALMAP keyword" +
                                " (the sibling copy already had this; the scene-bound one did not)");
            }

            Color? donorPot = null;
            foreach (string p in MaterialPathsContaining("Designer_Pot"))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null || !m.HasProperty("_BaseColor")) continue;
                Color now = m.GetColor("_BaseColor");
                if (donorPot == null && Mathf.Min(now.r, Mathf.Min(now.g, now.b)) < 0.1f) donorPot = now;
            }
            _log.AppendLine("  darkest Designer_Pot tint found in the rip = " + (donorPot.HasValue ? Fmt(donorPot.Value) : "<none>"));

            foreach (string p in MaterialPathsContaining("Designer_Pot"))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null || !m.HasProperty("_BaseColor") || !donorPot.HasValue) continue;
                Color now = m.GetColor("_BaseColor");
                if (Close(now, donorPot.Value)) continue;
                Texture b = m.GetTexture("_BaseMap");
                if (b == null)
                {
                    Undo.RecordObject(m, "147VR Pot Colour");
                    m.SetColor("_BaseColor", donorPot.Value);
                    EditorUtility.SetDirty(m);
                    changed++;
                    _log.AppendLine("  " + Path.GetFileName(p) + " tint " + Fmt(now) + " -> " + Fmt(donorPot.Value) +
                                    " (washed mint to the darker glazed jade the rip itself uses; no texture" +
                                    " exists for this pot anywhere in the rip, so tint is the only honest lever)");
                }
            }
            AssetDatabase.SaveAssets();
            _log.AppendLine("  fern/pot materials changed=" + changed + " (albedo in use: " +
                            (File.Exists(plantAlbedo) ? "0206_Plant Albedo.png - note it contains large blank" +
                             " white UV regions, which is why some leaves read pale" : "missing") + ")");
            return changed;
        }

        // ------------------------------------------------------------------ manifest + material helpers

        /// <summary>Reads base_color for a manifest material entry whose name contains the given token.
        /// The manifest mixes texture records and material records under the same "name" key, and the
        /// texture form comes first - "0010_Wood_gray.png" at line 218 is followed by image dimensions
        /// like 512, while the material at line 6666 is the one that actually carries base_color
        /// (0.8, 0.8, 0.8). Reading only the first occurrence would either take 512 as a colour or give
        /// up silently, so every occurrence is scanned and only a record followed by base_color counts.</summary>
        private static Color? ReadManifestColor(string nameToken)
        {
            if (!File.Exists(ManifestPath)) { _log.AppendLine("  manifest missing: " + ManifestPath); return null; }
            string json = File.ReadAllText(ManifestPath);

            string needle = "\"name\": \"" + nameToken;
            int search = 0, baseColorAt = -1, entryAt = -1;
            while (true)
            {
                int at = json.IndexOf(needle, search, StringComparison.Ordinal);
                if (at < 0) break;
                int bc = json.IndexOf("base_color", at, StringComparison.Ordinal);
                if (bc >= 0 && bc - at <= 400) { entryAt = at; baseColorAt = bc; break; }
                _log.AppendLine("  skipped line " + (1 + json.Substring(0, at).Count(c => c == '\n')) +
                                " - no base_color follows, that is a texture record not a material");
                search = at + needle.Length;
            }
            if (baseColorAt < 0) { _log.AppendLine("  no manifest MATERIAL entry containing \"" + nameToken + "\""); return null; }

            int open = json.IndexOf('[', baseColorAt);
            int close = json.IndexOf(']', open);
            var nums = new List<float>();
            foreach (string tok in json.Substring(open + 1, close - open - 1)
                         .Split(new[] { ',', '\n', '\r', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                if (float.TryParse(tok, System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out float v)) nums.Add(v);
            if (nums.Count < 3) { _log.AppendLine("  could not parse base_color"); return null; }

            Color c = new Color(nums[0], nums[1], nums[2], nums.Count > 3 ? nums[3] : 1f);
            _log.AppendLine("  manifest line " + (1 + json.Substring(0, entryAt).Count(ch => ch == '\n')) +
                            ": authored base_color = " + Fmt(c));
            return c;
        }

        private static List<string> MaterialPathsContaining(string token) =>
            MaterialFolders.Where(AssetDatabase.IsValidFolder)
                .SelectMany(f => AssetDatabase.FindAssets("t:Material", new[] { f })
                              .Select(AssetDatabase.GUIDToAssetPath))
                .Where(p => p.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) &&
                            Path.GetFileNameWithoutExtension(p).IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                .Distinct().OrderBy(p => p, StringComparer.Ordinal).ToList();

        private static List<string> MaterialPathsForObjectsNamed(Scene scene, string objectToken)
        {
            var paths = new List<string>();
            foreach (Renderer r in AllRenderers(scene)
                        .Where(x => x.name.IndexOf(objectToken, StringComparison.OrdinalIgnoreCase) >= 0))
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    string p = AssetDatabase.GetAssetPath(m);
                    if (!string.IsNullOrEmpty(p) && !paths.Contains(p)) paths.Add(p);
                }
            return paths;
        }

        private static void SetKeyword(Material m, string keyword, bool on)
        {
            var so = new SerializedObject(m);
            SerializedProperty kw = so.FindProperty("m_ValidKeywords");
            if (kw == null || !kw.isArray) return;
            bool present = false;
            for (int i = 0; i < kw.arraySize; i++)
                if (kw.GetArrayElementAtIndex(i).stringValue == keyword) present = true;
            if (on && !present)
            {
                kw.InsertArrayElementAtIndex(kw.arraySize);
                kw.GetArrayElementAtIndex(kw.arraySize - 1).stringValue = keyword;
            }
            else if (!on && present)
            {
                for (int i = kw.arraySize - 1; i >= 0; i--)
                    if (kw.GetArrayElementAtIndex(i).stringValue == keyword) kw.DeleteArrayElementAtIndex(i);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool Close(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f &&
            Mathf.Abs(a.b - b.b) < 0.01f && Mathf.Abs(a.a - b.a) < 0.01f;

        // ------------------------------------------------------------------ scene helpers

        private static List<Renderer> AllRenderers(Scene scene) =>
            scene.GetRootGameObjects()
                 .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                 .Where(r => r != null).ToList();

        private static string MatName(Renderer r) =>
            r.sharedMaterials.Length > 0 && r.sharedMaterials[0] != null ? r.sharedMaterials[0].name : string.Empty;

        private static Bounds BayBounds(Scene scene)
        {
            Light[] lamps = scene.GetRootGameObjects()
                                 .SelectMany(g => g.GetComponentsInChildren<Light>(true))
                                 .Where(l => l != null && l.name.StartsWith("TableKey_", StringComparison.Ordinal) &&
                                             l.name.IndexOf("_R01", StringComparison.Ordinal) >= 0)
                                 .ToArray();
            float z = lamps.Length > 0 ? lamps.Average(l => l.transform.position.z) : 9.297f;
            GameObject table = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .Where(g => g.name.StartsWith("ROOT_PoolTable_", StringComparison.Ordinal))
                .Distinct()
                .OrderBy(g => Mathf.Abs(g.transform.position.z - z))
                .FirstOrDefault();
            if (table == null) return new Bounds(new Vector3(0f, 0.9f, z), new Vector3(4f, 1f, 2.2f));
            Renderer[] rs = table.GetComponentsInChildren<Renderer>(true).Where(x => x != null).ToArray();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private static Bounds FernBounds(Scene scene)
        {
            List<Renderer> fs = AllRenderers(scene)
                .Where(r => r.name.IndexOf("WallPlant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            r.name.IndexOf("Planter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            MatName(r).IndexOf("Designer_Pot", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            if (fs.Count == 0) return new Bounds(Vector3.zero, Vector3.one * 2f);
            Bounds b = fs[0].bounds;
            foreach (Renderer r in fs) b.Encapsulate(r.bounds);
            return b;
        }

        private static void Capture(string name, Bounds target, Vector3 eyeOffset)
        {
            var camGo = new GameObject("147VR Sprint4 Capture Camera");
            try
            {
                Directory.CreateDirectory(RenderDir);
                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.allowHDR = true;
                cam.fieldOfView = 58f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 120f;
                var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (data == null)
                    data = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.renderShadows = true;

                Vector3 eye = target.center + eyeOffset;
                camGo.transform.SetPositionAndRotation(eye,
                    Quaternion.LookRotation((target.center - eye).normalized, Vector3.up));

                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32,
                                           RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                rt.Create();
                RenderTexture prev = RenderTexture.active;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                cam.targetTexture = null;

                string file = Path.Combine(RenderDir, name + ".png");
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(file, bytes);
                _log.AppendLine("CAPTURE " + name + " -> " + file + " (" + bytes.Length + " bytes)");
                UnityEngine.Object.DestroyImmediate(tex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
            catch (Exception e) { _log.AppendLine("CAPTURE " + name + ": ERROR " + e.Message); }
            finally { UnityEngine.Object.DestroyImmediate(camGo); }
        }

        private static Scene ActiveWipRoom()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                _log.AppendLine("REFUSED: loaded Scene is '" + scene.path + "', expected " + SourceScenePath);
                return default;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _log.AppendLine("REFUSED: Editor is in (or entering) Play Mode.");
                return default;
            }
            return scene;
        }

        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F3") + ", " + v.y.ToString("F3") + ", " + v.z.ToString("F3") + ")";
        private static string Fmt(Color c) =>
            "(" + c.r.ToString("F3") + ", " + c.g.ToString("F3") + ", " + c.b.ToString("F3") + ", " +
            c.a.ToString("F3") + ")";
        private static string Fmt4(Vector4 v) =>
            "scale(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + ") offset(" +
            v.z.ToString("F2") + "," + v.w.ToString("F2") + ")";

        private static void Begin()
        {
            _log = new StringBuilder();
            _log.AppendLine("147VR SPRINT 4 (bay1 cloth colour / ornament pattern + colour / cornice colour" +
                            " / ceiling reach measurement / fern + pot)");
            _log.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _log.AppendLine("Every authored value is read from the rip manifest at runtime, never typed here.");
        }

        private static void Finish()
        {
            Directory.CreateDirectory("Logs");
            if (File.Exists(ReportPath))
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string archive = "Logs/WipRoom_Art_Sprint4_" + stamp + ".prev.txt";
                int n = 0;
                while (File.Exists(archive)) archive = "Logs/WipRoom_Art_Sprint4_" + stamp + "_" + (++n) + ".prev.txt";
                File.Move(ReportPath, archive);
            }
            File.WriteAllText(ReportPath, _log.ToString());
            Debug.Log("[Sprint4] done -> " + ReportPath);
        }
    }
}
#endif
