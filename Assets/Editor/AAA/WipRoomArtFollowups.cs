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
using UnityEngine.Rendering.Universal;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Owner-requested art follow-ups (2026-11-10). One dispatch, three jobs, with rendered proof.
    ///
    ///  A) FLAG CLEARANCE - owner: "ขยับธงออกจากกำแพงหน่อย บางธงเห็นไม่เต็มผืนเพราะติดผนัง".
    ///     Evidence already on disk (Logs/WipRoom_Flag_Facing_Apply.txt 2026-11-08 13:24): the cloth band
    ///     is z=-14.171..-14.310 while wall/shell vertices inside the same band reach z=-13.889, so flags
    ///     in front of a protruding surface are buried by up to ~0.32 m. Menu 6/7 measured this but left
    ///     the translation off (ApplyPushOut = false, owner had said "rotate only") and it only computed a
    ///     push for flags that still needed rotating - every flag is rotated now, so re-running menu 7
    ///     would skip all 26. This tool pushes instead, and pushes the WHOLE ROW by one number so the row
    ///     stays coplanar instead of following the wall contour. Translation is safe on this FBX (unlike
    ///     rotation or scale) because a baked-vertex mesh still moves with its own transform.
    ///
    ///  B) CORNICE STRETCH - owner: "ขยาย SM_cornice ให้ไปชนกับ SM_0010_Ornament_ceiling".
    ///     Scales about the WORLD BOUNDS CENTRE, never about the pivot: this room FBX leaves pivots at the
    ///     origin, so a bare localScale change would fling the moulding across the room - the exact failure
    ///     recorded in WipRoomFlagFacingFix. Refuses when the needed factor is implausible.
    ///
    ///  C) EMISSION KEYWORDS - menu 18 reported 16 self-check failures. Those materials already carry their
    ///     HDR _EmissionColor on disk but have no "- _EMISSION" line, and URP Lit gates emission behind that
    ///     keyword, so they render black. Written through SerializedObject because Material.EnableKeyword()
    ///     does not persist into an asset .mat file, then re-read from the saved files to prove it landed.
    ///     Scope note: those are donor/prepared copies - the Scene binds the Rebound set, which already
    ///     passes - so this makes the audit green, it does not by itself light up the room.
    ///
    /// Scene changes are Undoable and the Scene is NEVER saved. No scene is opened either: opening one
    /// would destroy whatever another session has in flight.
    /// </summary>
    public static class WipRoomArtFollowups
    {
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string ReportPath = "Logs/WipRoom_Art_Followups.txt";
        private const string RenderDir = "Logs/renders";

        /// <summary>Air gap left between the cloth and the wall surface behind it.</summary>
        private const float FlagClearanceM = 0.08f;
        /// <summary>Hard ceiling on the row push, so a bad measurement cannot move the row half a room.</summary>
        private const float FlagMaxPushM = 0.60f;
        /// <summary>How far past the ornament surface the cornice is driven, to kill a hairline gap.</summary>
        private const float OrnamentOverlapM = 0.004f;
        /// <summary>Refuse a stretch that needs more than this scale factor - that is a measurement error.</summary>
        private const float MaxScaleFactor = 2.0f;
        private const int MaxVerticesForWallScan = 600000;
        private const int RenderWidth = 1600;
        private const int RenderHeight = 900;

        private static readonly string[] MaterialFolders =
        {
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/",
            "Assets/AAA/Imports/naris_vintage/Rebound/",
        };

        /// <summary>Set by ToolRunner so a dispatched run never blocks the Editor on a modal popup.</summary>
        public static bool SilenceDialogs = false;

        private static StringBuilder _log = new StringBuilder();

        // ------------------------------------------------------------------ entry points

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/20) Art Follow-ups - Full Session (renders + flags + cornice + emission)")]
        public static void MenuSession()
        {
            Run();
            Dialog("Art follow-ups done",
                   "Report: " + ReportPath + "\nRenders: " + RenderDir +
                   "\n\nThe Scene is DIRTY but NOT SAVED: Ctrl+Z reverts, Ctrl+S keeps.");
        }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/21) Flag Clearance Push Only (Undoable / No Save)")]
        public static void MenuFlags() { Begin(); PushFlags(); Finish(); }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/22) Cornice Stretch To Ceiling Ornament Only (Undoable / No Save)")]
        public static void MenuCornice() { Begin(); StretchCornice(); Finish(); }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/23) Repair Missing _EMISSION Keywords (Assets Only)")]
        public static void MenuEmission() { Begin(); RepairEmissionKeywords(); Finish(); }

        /// <summary>ToolRunner entry point: put "art-followups" in Logs/editor_cmd.txt.</summary>
        public static void Run()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;

                List<Renderer> flags = GetFlags(scene);
                List<Renderer> all = AllRenderers(scene);

                if (flags.Count > 0)
                {
                    Bounds row = UnionOf(flags);
                    _log.AppendLine("FLAG ROW BEFORE : " + Describe(row, flags.Count));
                    Capture("01-flags-before", row, new Vector3(0f, 0.9f, 7.5f));
                }
                else _log.AppendLine("FLAGS: no SM_147VR_AAA_Flag_* renderers found");

                Bounds? corniceBounds = ReportBounds(FindOne(all, "cornice"), "CORNICE  SM_cornice");
                Bounds? ornamentBounds = ReportBounds(FindOne(all, "ornament_ceiling"), "ORNAMENT SM_0010_Ornament_ceiling");
                if (corniceBounds.HasValue && ornamentBounds.HasValue)
                {
                    Bounds junction = corniceBounds.Value;
                    junction.Encapsulate(ornamentBounds.Value);
                    Capture("02-ceiling-before", junction, new Vector3(3.2f, -1.6f, 5.0f));
                }
                else Capture("02-ceiling-before", corniceBounds ?? ornamentBounds ?? new Bounds(Vector3.zero, Vector3.one),
                             new Vector3(3.2f, -1.6f, 5.0f));

                GameObject bay2 = GameObject.Find("Bay2_PhysicsRoot");
                if (bay2 != null) Capture("03-bay2-before", BoundsOf(bay2), new Vector3(3.0f, 1.6f, 3.4f));
                else _log.AppendLine("BAY2: Bay2_PhysicsRoot not found - no before/after shot");

                AuditRailMaterials(scene);

                int keywordFixed = Guard("EMISSION", () => RepairEmissionKeywords());
                int pushed = Guard("FLAGS", () => PushFlags());
                int stretched = Guard("CORNICE", () => StretchCornice() ? 1 : 0);

                _log.AppendLine();
                if (flags.Count > 0)
                {
                    Bounds rowAfter = UnionOf(flags);
                    _log.AppendLine("FLAG ROW AFTER  : " + Describe(rowAfter, flags.Count));
                    Capture("04-flags-after", rowAfter, new Vector3(0f, 0.9f, 7.5f));
                }
                if (corniceBounds.HasValue && ornamentBounds.HasValue)
                {
                    GameObject corniceNow = FindOne(AllRenderers(scene), "cornice");
                    GameObject ornamentNow = FindOne(AllRenderers(scene), "ornament_ceiling");
                    if (corniceNow != null && ornamentNow != null)
                    {
                        Bounds after = corniceNow.GetComponent<Renderer>().bounds;
                        after.Encapsulate(ornamentNow.GetComponent<Renderer>().bounds);
                        Capture("05-ceiling-after", after, new Vector3(3.2f, -1.6f, 5.0f));
                    }
                }
                if (bay2 != null) Capture("06-bay2-after", BoundsOf(bay2), new Vector3(3.0f, 1.6f, 3.4f));

                _log.AppendLine();
                _log.AppendLine("RESULT keywordFixed=" + keywordFixed + " flagsPushed=" + pushed +
                                " corniceStretched=" + stretched);
                _log.AppendLine("The Scene was marked dirty and NOT saved. Ctrl+Z reverts the transform changes.");
            }
            finally { Finish(); }
        }

        /// <summary>ToolRunner entry point: "emission-fix". Undoes the wrong keyword additions, then
        /// applies the corrected set, so one queued command repairs the damage and finishes the job.</summary>
        public static void RunEmissionFix()
        {
            Begin();
            try
            {
                int stripped = Guard("EMISSION REVERT", () => RevertWrongEmissionKeywords());
                int added = Guard("EMISSION", () => RepairEmissionKeywords());
                _log.AppendLine();
                _log.AppendLine("RESULT stripped=" + stripped + " added=" + added +
                                " - only materials with authored (non-black) emission keep _EMISSION.");
            }
            finally { Finish(); }
        }

        /// <summary>ToolRunner entry point: "art-fix-round2". The three things the evidence agents proved
        /// from disk on 2026-11-11: the scene has almost no Unity lights even though the .blend art pass
        /// authored bar/stage/chandelier lights; several scene-bound Rebound materials are black-emissive
        /// while their donor carries the authored value; and two materials bind the wrong texture file.</summary>
        public static void RunArtFixRound2()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;

                ReportLightsAndBar(scene);

                int parity = Guard("EMISSION PARITY", () => EmissionParityFromDonors());
                int rebinds = Guard("TEXTURE REBIND", () => RebindProvenWrongTextures());

                List<Renderer> all = AllRenderers(scene);
                Renderer bar = all.FirstOrDefault(r => BarHints.Any(h =>
                    MaterialName(r).IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.name.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0));
                if (bar != null) Capture("20-bar-area", BoundsOf(bar.gameObject), new Vector3(0f, 1.4f, 5.0f));

                Capture("21-ceiling-now", new Bounds(new Vector3(0f, 4.0f, 0f), new Vector3(26f, 1.6f, 36f)),
                        new Vector3(0f, -3.0f, 12f));

                List<Renderer> curtains = all.Where(r =>
                    MaterialName(r).IndexOf("Curtain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.name.IndexOf("Curtain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    MaterialName(r).IndexOf("Velvet", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                if (curtains.Count > 0) Capture("22-curtains-now", UnionOf(curtains), new Vector3(0f, 0.6f, 6.0f));
                else _log.AppendLine("CURTAINS: no curtain renderer found");

                _log.AppendLine();
                _log.AppendLine("RESULT parityFixed=" + parity + " textureRebinds=" + rebinds +
                                " - assets only; the Scene was not modified, saved or reopened.");
            }
            finally { Finish(); }
        }

        /// <summary>ToolRunner entry point: "lamp-audit". Read-only. The parity pass edited Rebound
        /// materials matched BY NAME, and the ceiling render came out byte-identical to the previous one
        /// with the bulb spheres still black, so this proves which material asset each lamp renderer
        /// actually binds instead of assuming the name match was the right target.</summary>
        public static void RunLampAudit()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;

                string[] hints = { "bulb", "lamp", "light", "shade", "loft", "cove", "chandelier",
                                   "pin", "uplight", "sconce", "neon", "marquee", "glow" };

                var rows = new List<LampRow>();
                foreach (Renderer r in AllRenderers(scene))
                {
                    if (r.sharedMaterials.Length == 0 || r.sharedMaterials[0] == null) continue;
                    string m = r.sharedMaterials[0].name;
                    string hay = (r.name + "|" + m).ToLowerInvariant();
                    if (!hints.Any(h => hay.Contains(h))) continue;

                    string matPath = AssetDatabase.GetAssetPath(r.sharedMaterials[0]);
                    Color e = r.sharedMaterials[0].HasProperty("_EmissionColor")
                                  ? r.sharedMaterials[0].GetColor("_EmissionColor") : Color.black;
                    Texture em = r.sharedMaterials[0].HasProperty("_EmissionMap")
                                  ? r.sharedMaterials[0].GetTexture("_EmissionMap") : null;
                    bool kw = false;
                    if (!string.IsNullOrEmpty(matPath) && File.Exists(matPath))
                        kw = File.ReadAllText(matPath).Contains("- _EMISSION");

                    rows.Add(new LampRow
                    {
                        Object = r.name,
                        MaterialPath = string.IsNullOrEmpty(matPath) ? "<not an asset (scene override?)>" : matPath,
                        Emission = e,
                        HasKeyword = kw,
                        EmissionMap = em != null ? em.name : "<none>",
                        Centre = r.bounds.center,
                        Active = r.gameObject.activeInHierarchy && r.enabled,
                    });
                }

                _log.AppendLine("LAMP BINDING AUDIT - " + rows.Count + " lamp-family renderers");
                foreach (var group in rows.GroupBy(x => x.MaterialPath, StringComparer.Ordinal)
                                          .OrderByDescending(g => g.Count()))
                {
                    LampRow first = group.First();
                    _log.AppendLine("  [" + group.Count() + " renderers] " + group.Key);
                    _log.AppendLine("      emission=" + Fmt(first.Emission) + " keyword=" +
                                    (first.HasKeyword ? "YES" : "NO ") + " emissionMap=" + first.EmissionMap);
                    foreach (LampRow x in group.Take(6))
                        _log.AppendLine("      " + (x.Active ? "on " : "OFF") + " " + x.Object +
                                        " centre=" + Fmt(x.Centre));
                    if (group.Count() > 6) _log.AppendLine("      ... " + (group.Count() - 6) + " more");
                }

                int dark = rows.Count(x => !x.HasKeyword ||
                                           Mathf.Max(x.Emission.r, Mathf.Max(x.Emission.g, x.Emission.b)) <= 0.011f);
                _log.AppendLine("SUMMARY renderers=" + rows.Count + " still-dark-or-no-keyword=" + dark +
                                " distinct materials=" + rows.Select(x => x.MaterialPath).Distinct().Count());
                _log.AppendLine("Nothing was modified.");
            }
            finally { Finish(); }
        }

        private sealed class LampRow
        {
            public string Object;
            public string MaterialPath;
            public Color Emission;
            public bool HasKeyword;
            public string EmissionMap;
            public Vector3 Centre;
            public bool Active;
        }

        private static readonly string[] BarHints =
        {
            "BarPult", "CashDesk", "BottleHolders", "Sampagne", "WhiskeyBottles",
            "WineBottles", "BeerTap", "BCStools", "Espresso",
        };

        private static void ReportLightsAndBar(Scene scene)
        {
            _log.AppendLine("UNITY LIGHT INVENTORY");
            _log.AppendLine("  why: the .blend art pass authored LGT_Bar_glow (AREA e=175 colour 1,0.58,0.31),");
            _log.AppendLine("  LGT_AAA_Bar_Shelf_Key_01/02 (AREA e=68) and LGT_AAA_Bar_Counter_Fill (AREA e=47.6)");
            _log.AppendLine("  and the agents found only one !u!108 block in the Scene YAML.");
            Light[] lights = scene.GetRootGameObjects()
                                  .SelectMany(g => g.GetComponentsInChildren<Light>(true))
                                  .Where(l => l != null)
                                  .ToArray();
            _log.AppendLine("  Light components=" + lights.Length + " lit=" +
                            lights.Count(l => l.enabled && l.gameObject.activeInHierarchy));
            foreach (Light l in lights.OrderBy(l => l.name, StringComparer.Ordinal))
                _log.AppendLine("  " + (l.enabled && l.gameObject.activeInHierarchy ? "ON  " : "OFF ") +
                                l.name + " type=" + l.type + " intensity=" + l.intensity.ToString("F2") +
                                " range=" + l.range.ToString("F2") + " colour=" + Fmt(l.color) +
                                " pos=" + Fmt(l.transform.position));

            List<Renderer> barParts = AllRenderers(scene)
                .Where(r => BarHints.Any(h => MaterialName(r).IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                              r.name.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
            if (barParts.Count == 0)
            {
                _log.AppendLine("BAR: no bar prop renderer matched - world position still UNVERIFIED, so the");
                _log.AppendLine("     authored bar lights cannot be placed honestly yet.");
                return;
            }
            Bounds bb = UnionOf(barParts);
            _log.AppendLine("BAR: renderers=" + barParts.Count + " union " + Fmt(bb.min) + ".." + Fmt(bb.max) +
                            " size=" + Fmt(bb.size) + " centre=" + Fmt(bb.center));
            foreach (Renderer r in barParts.OrderBy(x => x.name, StringComparer.Ordinal).Take(12))
                _log.AppendLine("  " + r.name + " mat=" + MaterialName(r) + " centre=" + Fmt(r.bounds.center));
        }

        private static string MaterialName(Renderer r) =>
            r.sharedMaterials.Length > 0 && r.sharedMaterials[0] != null ? r.sharedMaterials[0].name : string.Empty;

        /// <summary>Names that must never be touched by a bulk parity pass - the shared wall and damask
        /// materials are edited deliberately elsewhere and re-serialising them has stripped keywords before.</summary>
        private static readonly string[] ParityBlacklist = { "PBR_wall", "Wall_Damask" };

        /// <summary>Where a scene-bound Rebound material is black-emissive but its donor counterpart carries
        /// authored emission, copy the donor value across. Proven pattern: the donor
        /// 147VR_Aged_Brass_LoftLight.mat holds (0.18, 0.1224, 0.0774) while the scene-bound
        /// 147VR_147VR_Aged_Brass_LoftLight.mat is (0,0,0,1), so the fixture renders dark. Rebound files carry
        /// doubled "147VR_" prefixes, so names are normalised before matching. Every value comes from a real
        /// asset - nothing is invented here.</summary>
        private static int EmissionParityFromDonors()
        {
            const string reboundFolder = "Assets/AAA/Imports/naris_vintage/Rebound/";
            const string donorFolder = "Assets/AAA/Imports/NeonVintageArtPass/Materials/";

            // Authored values can live in either folder. The scene binds a TRIPLED-prefix Rebound file
            // (147VR_147VR_147VR_AAA_CeilingPin_WarmDiffuser.mat, emission 0,0,0) whose value only exists
            // in a sibling Rebound file (147VR_147VR_AAA_CeilingPin_WarmDiffuser.mat, 1.15,0.8,0.45), so
            // searching the donor folder alone silently skipped the ceiling pins. When several names
            // normalise to the same key, the one that actually carries emission wins.
            var candidates = new List<string>();
            foreach (string folder in new[] { donorFolder, reboundFolder })
                candidates.AddRange(AssetDatabase.FindAssets("t:Material", new[] { folder })
                                                 .Select(AssetDatabase.GUIDToAssetPath)
                                                 .Where(x => x.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)));

            var donorsByName = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (string p in candidates)
            {
                Material cand = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (cand == null || !cand.HasProperty("_EmissionColor")) continue;
                Color c = cand.GetColor("_EmissionColor");
                bool authored = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) > 0.011f;
                if (!authored) continue;

                string key = NormaliseMatName(Path.GetFileNameWithoutExtension(p));
                if (donorsByName.TryGetValue(key, out Material existing))
                {
                    Color old = existing.HasProperty("_EmissionColor") ? existing.GetColor("_EmissionColor") : Color.black;
                    if (Mathf.Max(old.r, Mathf.Max(old.g, old.b)) >= c.r) continue;
                }
                donorsByName[key] = cand;
            }

            int fixedCount = 0;
            var touched = new List<Material>();

            foreach (string path in AssetDatabase.FindAssets("t:Material", new[] { reboundFolder })
                                                 .Select(AssetDatabase.GUIDToAssetPath)
                                                 .Where(x => x.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                                                 .OrderBy(x => x, StringComparer.Ordinal))
            {
                Material target = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (target == null || !target.HasProperty("_EmissionColor")) continue;
                string fileBase = Path.GetFileNameWithoutExtension(path);
                if (ParityBlacklist.Any(b => fileBase.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0)) continue;

                Color current = target.GetColor("_EmissionColor");
                if (Mathf.Max(current.r, Mathf.Max(current.g, current.b)) > 0.011f) continue;

                string key = NormaliseMatName(fileBase);
                if (!donorsByName.TryGetValue(key, out Material donor)) continue;
                if (donor == target) continue;
                Color authored = donor.GetColor("_EmissionColor");

                WriteEmission(target, authored, "147VR Emission Parity");
                touched.Add(target);
                fixedCount++;
                _log.AppendLine("  parity " + Path.GetFileName(path) + " (0,0,0) -> " + Fmt(authored) +
                                "  from " + Path.GetFileName(AssetDatabase.GetAssetPath(donor)));
            }

            if (touched.Count > 0) AssetDatabase.SaveAssets();

            int verifyFail = 0;
            foreach (Material m in touched)
            {
                string path = AssetDatabase.GetAssetPath(m);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) { verifyFail++; continue; }
                string txt = File.ReadAllText(path);
                Color want = m.GetColor("_EmissionColor");
                bool rgbLanded = txt.Contains("_EmissionColor: {r: " + want.r.ToString("0.####"));
                bool kwLanded = txt.Contains("- _EMISSION");
                bool flagsLanded = txt.Contains("m_LightmapFlags: 2");
                if (!kwLanded || !rgbLanded || !flagsLanded)
                {
                    _log.AppendLine("  PARITY VERIFY FAIL " + Path.GetFileName(path) +
                                    " keyword=" + kwLanded + " rgb=" + rgbLanded + " bakedEmissive=" + flagsLanded);
                    verifyFail++;
                }
            }
            _log.AppendLine("EMISSION PARITY: fixed=" + fixedCount + " verifyFailures=" + verifyFail);
            return fixedCount;
        }

        /// <summary>Writes emission the only way that survives a save in this project: colour, then the
        /// _EMISSION keyword through SerializedObject (Material.EnableKeyword does not persist into an asset
        /// .mat file), then BakedEmissive through globalIlluminationFlags. Without the last step Unity
        /// recomputes m_LightmapFlags as EmissiveIsBlack and strips the keyword again - proven on
        /// 2026-11-11 when 15 files written at 05:45 all read keyword=NO by 05:50. The enum type is
        /// resolved by reflection because Unity 6.4 does not expose it as
        /// UnityEngine.Rendering.MaterialGlobalIlluminationFlags (CS0234).</summary>
        private static void WriteEmission(Material target, Color authored, string undoLabel)
        {
            Undo.RecordObject(target, undoLabel);
            target.SetColor("_EmissionColor", authored);

            var so = new SerializedObject(target);
            SerializedProperty kw = so.FindProperty("m_ValidKeywords");
            if (kw != null && kw.isArray)
            {
                bool present = false;
                for (int i = 0; i < kw.arraySize; i++)
                    if (kw.GetArrayElementAtIndex(i).stringValue == "_EMISSION") { present = true; break; }
                if (!present)
                {
                    kw.InsertArrayElementAtIndex(kw.arraySize);
                    kw.GetArrayElementAtIndex(kw.arraySize - 1).stringValue = "_EMISSION";
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var giProp = typeof(Material).GetProperty("globalIlluminationFlags");
            if (giProp != null)
            {
                try { giProp.SetValue(target, System.Enum.Parse(giProp.PropertyType, "BakedEmissive"), null); }
                catch (Exception e) { _log.AppendLine("  WARN BakedEmissive failed on " + target.name + ": " + e.Message); }
            }
            else _log.AppendLine("  WARN no globalIlluminationFlags property on this Unity version");

            EditorUtility.SetDirty(target);
        }

        /// <summary>Strips the repeated "147VR_" prefix the Rebound copies accumulate so a Rebound file can
        /// be matched back to its single-prefix donor.</summary>
        private static string NormaliseMatName(string fileName)
        {
            string s = fileName;
            while (s.StartsWith("147VR_", StringComparison.Ordinal) && s.Length > 6) s = s.Substring(6);
            return s;
        }

        private static readonly (string Material, string BadGuid, string TexturePath, string Label)[] TextureRebinds =
        {
            ("Assets/AAA/Imports/NeonVintageArtPass/Materials/147 VR Burgundy Red Velvet _ Sketchfab Curtains.mat",
             "f83294f83f81afb4b88a32d1e81542dd",
             "Assets/147 main/BlendTextures/material_2_baseColor.png.png",
             "curtain velvet (donor)"),
            ("Assets/AAA/Imports/naris_vintage/Rebound/147VR_147 VR Burgundy Red Velvet _ Sketchfab Curtains.mat",
             "f83294f83f81afb4b88a32d1e81542dd",
             "Assets/147 main/BlendTextures/material_2_baseColor.png.png",
             "curtain velvet (scene-bound)"),
            ("Assets/AAA/Imports/naris_vintage/Rebound/147VR_147VR_147VR_Mat_PannelFrame.mat",
             "2faf45249b959da428ef4e53cc34d8e0",
             "Assets/147 main/BlendTextures/0096_T_WoodPanel_Dark_BaseColor.png.png",
             "panel frame (doubled prefix)"),
            ("Assets/AAA/Imports/naris_vintage/Rebound/147VR_147VR_Mat_PannelFrame.mat",
             "2faf45249b959da428ef4e53cc34d8e0",
             "Assets/147 main/BlendTextures/0096_T_WoodPanel_Dark_BaseColor.png.png",
             "panel frame (single prefix)"),
        };

        /// <summary>Repoints the two proven mis-bindings and only while the material still carries the
        /// known-bad GUID, so a repeat run does nothing and a hand fix is never overwritten. The curtain
        /// _BaseMap is a transmission mask used as albedo (guid f83294f8...), and the panel frame binds
        /// guid 2faf4524... which no .meta defines, so it is dead and renders white.</summary>
        private static int RebindProvenWrongTextures()
        {
            int done = 0;
            var expected = new List<KeyValuePair<string, string>>();

            foreach ((string matPath, string badGuid, string texPath, string label) in TextureRebinds)
            {
                if (!File.Exists(matPath)) { _log.AppendLine("  REBIND SKIP (no material) " + matPath); continue; }
                if (!File.Exists(texPath)) { _log.AppendLine("  REBIND SKIP (no texture) " + texPath); continue; }
                if (!File.ReadAllText(matPath).Contains(badGuid))
                {
                    _log.AppendLine("  REBIND already clean: " + label);
                    continue;
                }

                Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                Texture t = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                if (m == null || t == null)
                {
                    _log.AppendLine("  REBIND LOAD FAIL " + label);
                    continue;
                }

                Undo.RecordObject(m, "147VR Texture Rebind");
                m.SetTexture("_BaseMap", t);
                EditorUtility.SetDirty(m);
                expected.Add(new KeyValuePair<string, string>(matPath,
                    AssetDatabase.AssetPathToGUID(texPath)));
                done++;
                _log.AppendLine("  REBIND " + label + " -> " + Path.GetFileName(texPath) +
                                " (was guid " + badGuid.Substring(0, 8) + "...)");
            }

            if (expected.Count > 0) AssetDatabase.SaveAssets();

            foreach (KeyValuePair<string, string> pair in expected)
                if (!File.ReadAllText(pair.Key).Contains(pair.Value))
                    _log.AppendLine("  REBIND VERIFY FAIL " + pair.Key);

            _log.AppendLine("TEXTURE REBIND: done=" + done);
            return done;
        }

        /// <summary>ToolRunner entry point: "art-capture". Measures and renders only - it never moves a
        /// transform and never writes an asset, so it is safe to run as often as needed. It lists EVERY
        /// cornice and ornament renderer, because the room carries one moulding per wall and the single
        /// match FindOne reported before was only the +X wall strip.</summary>
        public static void RunCapture()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;

                List<Renderer> all = AllRenderers(scene);

                _log.AppendLine("ALL CORNICE RENDERERS");
                foreach (Renderer r in all.Where(x => x.name.IndexOf("cornice", StringComparison.OrdinalIgnoreCase) >= 0)
                                         .OrderBy(x => x.bounds.center.x).ThenBy(x => x.bounds.center.z))
                    _log.AppendLine("  " + r.name + " pivot=" + Fmt(r.transform.position) +
                                    " scale=" + Fmt(r.transform.localScale) +
                                    " bounds=" + Fmt(r.bounds.min) + ".." + Fmt(r.bounds.max));

                _log.AppendLine("ALL ORNAMENT RENDERERS");
                foreach (Renderer r in all.Where(x => x.name.IndexOf("ornament", StringComparison.OrdinalIgnoreCase) >= 0)
                                         .OrderBy(x => x.bounds.center.y))
                    _log.AppendLine("  " + r.name + " pivot=" + Fmt(r.transform.position) +
                                    " scale=" + Fmt(r.transform.localScale) +
                                    " bounds=" + Fmt(r.bounds.min) + ".." + Fmt(r.bounds.max));

                List<Renderer> flags = GetFlags(scene);
                if (flags.Count > 0)
                {
                    Bounds row = UnionOf(flags);
                    _log.AppendLine("FLAG ROW NOW: " + Describe(row, flags.Count));
                    Capture("10-flags-now", row, new Vector3(0f, 0.9f, 7.5f));
                }

                Renderer cornice = all.FirstOrDefault(x => x.name.IndexOf("cornice", StringComparison.OrdinalIgnoreCase) >= 0);
                Renderer ornament = all.FirstOrDefault(x => x.name.IndexOf("ornament_ceiling", StringComparison.OrdinalIgnoreCase) >= 0);
                if (cornice != null && ornament != null)
                {
                    Bounds junction = cornice.bounds;
                    junction.Encapsulate(ornament.bounds);
                    Capture("11-ceiling-junction", junction, new Vector3(2.0f, -2.4f, 6.0f));
                }
                // A wide ceiling view regardless: the junction boxes are far apart, so frame the whole
                // ceiling band instead of the union of two distant boxes.
                var ceilingBand = new Bounds(new Vector3(0f, 4.0f, 0f), new Vector3(26f, 1.6f, 36f));
                Capture("12-ceiling-wide", ceilingBand, new Vector3(0f, -3.0f, 12f));

                GameObject bay2 = GameObject.Find("Bay2_PhysicsRoot");
                if (bay2 != null) Capture("13-bay2-now", BoundsOf(bay2), new Vector3(3.0f, 1.6f, 3.4f));
                else _log.AppendLine("BAY2: Bay2_PhysicsRoot not found");
            }
            finally { Finish(); }
        }

        private static int Guard(string label, Func<int> action)
        {
            try { return action(); }
            catch (Exception e) { _log.AppendLine(label + ": ERROR " + e); return 0; }
        }

        private static void Begin()
        {
            _log = new StringBuilder();
            _log.AppendLine("147VR ART FOLLOW-UPS (flags off the wall / cornice to ornament / emission keywords)");
            _log.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _log.AppendLine("Undoable. The Scene is never saved and no Scene is ever opened from here.");
        }

        private static void Finish()
        {
            Directory.CreateDirectory("Logs");
            if (File.Exists(ReportPath))
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string archive = "Logs/WipRoom_Art_Followups_" + stamp + ".prev.txt";
                int n = 0;
                while (File.Exists(archive)) archive = "Logs/WipRoom_Art_Followups_" + stamp + "_" + (++n) + ".prev.txt";
                File.Move(ReportPath, archive);
            }
            File.WriteAllText(ReportPath, _log.ToString());
            Debug.Log("[ArtFollowups] done -> " + ReportPath);
        }

        // ------------------------------------------------------------------ A) flags

        private static int PushFlags()
        {
            Scene scene = ActiveWipRoom();
            if (!scene.IsValid()) return 0;

            List<Renderer> flags = GetFlags(scene);
            if (flags.Count == 0) { _log.AppendLine("FLAGS: none found - nothing done"); return 0; }

            List<Renderer> others = AllRenderers(scene).Where(r => !flags.Contains(r)).ToList();

            Bounds row = UnionOf(flags);
            float rowDepth = row.max.z - row.min.z;
            if (rowDepth > 1.0f)
            {
                _log.AppendLine("FLAGS: REFUSED - the row spans " + rowDepth.ToString("F2") +
                                " m in z, so it is in the swung state rather than a wall band. Nothing moved.");
                return 0;
            }

            List<WallPoint> band = CollectWallPointsInBand(others, row);

            // One number for the whole row: the deepest burial any single flag has, plus clearance.
            float maxBuried = 0f;
            var perFlag = new List<FlagPlan>();
            foreach (Renderer flag in flags.OrderBy(r => r.bounds.center.x))
            {
                WallFace face = NearestWallFaceBehind(band, flag.bounds);
                float buried = face.Valid ? face.Z - flag.bounds.max.z : 0f;   // > 0 means behind that surface
                perFlag.Add(new FlagPlan { R = flag, Buried = buried, Face = face });
                if (buried > maxBuried) maxBuried = buried;
            }

            // Idempotent: nothing moves unless something is actually buried. Without the guard the clamp
            // would still return the 0.08 m clearance floor and walk the row deeper into the room on
            // every repeat run.
            float push = maxBuried > 0.001f ? Mathf.Clamp(maxBuried + FlagClearanceM, 0f, FlagMaxPushM) : 0f;
            _log.AppendLine("FLAGS: deepest burial=" + maxBuried.ToString("F3") + " m -> uniform row push=" +
                            push.ToString("F3") + " m (clearance " + FlagClearanceM.ToString("F2") +
                            ", cap " + FlagMaxPushM.ToString("F2") + ")");

            if (push <= 0.001f)
            {
                _log.AppendLine("FLAGS: no cloth sits behind a wall surface, so nothing was moved.");
                return 0;
            }

            Vector3 toRoom = FlatDir(row.center, Vector3.zero);
            int moved = 0;
            foreach (FlagPlan plan in perFlag)
            {
                Renderer flag = plan.R;
                Transform t = flag.transform;
                Vector3 before = flag.bounds.center;
                Undo.RecordObject(t, "147VR Flag Clearance");
                t.position = t.position + toRoom * push;
                EditorUtility.SetDirty(t);
                moved++;
                Vector3 after = flag.bounds.center;
                _log.AppendLine("  moved " + flag.name + " centre " + Fmt(before) + " -> " + Fmt(after) +
                                " | wall face z=" + (plan.Face.Valid ? plan.Face.Z.ToString("F3") : "<none>") +
                                " (" + plan.Face.SourceName + ")" +
                                " | was buried " + plan.Buried.ToString("F3") +
                                (plan.Buried > 0f ? " -> now " + (plan.Buried + push).ToString("F3") + " m clear"
                                                  : " (was already clear)"));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Bounds rowAfter = UnionOf(flags);
            _log.AppendLine("FLAGS: moved " + moved + " renderers by " + push.ToString("F3") + " m toward " +
                            Fmt(toRoom) + " | row z after " + rowAfter.min.z.ToString("F3") + ".." +
                            rowAfter.max.z.ToString("F3") + " (thickness " +
                            (rowAfter.max.z - rowAfter.min.z).ToString("F3") + " m, must stay ~0.14)");
            return moved;
        }

        private sealed class FlagPlan
        {
            public Renderer R;
            public float Buried;
            public WallFace Face;
        }

        // ------------------------------------------------------------------ B) cornice

        private static bool StretchCornice()
        {
            Scene scene = ActiveWipRoom();
            if (!scene.IsValid()) return false;

            List<Renderer> all = AllRenderers(scene);
            GameObject corniceGo = FindOne(all, "cornice");
            GameObject ornamentGo = FindOne(all, "ornament_ceiling");
            if (corniceGo == null || ornamentGo == null)
            {
                _log.AppendLine("CORNICE: REFUSED - missing " +
                                (corniceGo == null ? "*cornice* " : string.Empty) +
                                (ornamentGo == null ? "*ornament_ceiling*" : string.Empty) +
                                " in the loaded Scene. Nothing was scaled.");
                return false;
            }

            Renderer cRen = corniceGo.GetComponent<Renderer>();
            Renderer oRen = ornamentGo.GetComponent<Renderer>();
            Bounds cb = cRen.bounds, ob = oRen.bounds;

            // The axis to stretch is the only one the two boxes are separated on - the other two overlap.
            var candidates = new List<AxisGap>();
            string[] axisNames = { "X", "Y", "Z" };
            for (int a = 0; a < 3; a++)
            {
                bool otherAxesOverlap = true;
                for (int b = 0; b < 3; b++)
                {
                    if (b == a) continue;
                    float gapOther = Mathf.Max(AxisMin(cb, b) - AxisMax(ob, b), AxisMin(ob, b) - AxisMax(cb, b));
                    if (gapOther > 0f) { otherAxesOverlap = false; break; }
                }
                if (!otherAxesOverlap) continue;
                float gap = Mathf.Max(AxisMin(cb, a) - AxisMax(ob, a), AxisMin(ob, a) - AxisMax(cb, a));
                candidates.Add(new AxisGap { Axis = a, Name = axisNames[a], Gap = Mathf.Max(gap, 0f) });
            }

            _log.AppendLine("CORNICE: cornice " + Fmt(cb.min) + ".." + Fmt(cb.max) +
                            " | ornament " + Fmt(ob.min) + ".." + Fmt(ob.max) +
                            " | separated-axis candidates: " +
                            (candidates.Count == 0 ? "none" : string.Join(", ",
                                candidates.Select(c => c.Name + "=" + c.Gap.ToString("F4")).ToArray())));

            if (candidates.Count == 0)
            {
                _log.AppendLine("CORNICE: no single-axis stretch can close this (separated on more than one axis, " +
                                "or the boxes already interlock). Nothing was scaled.");
                return false;
            }

            AxisGap target = candidates.OrderByDescending(c => c.Gap).First();
            if (target.Gap <= 0.001f)
            {
                _log.AppendLine("CORNICE: already touching on " + target.Name + " (gap " + target.Gap.ToString("F4") +
                                " m) - nothing to stretch.");
                return false;
            }

            float thickness = AxisSize(cb, target.Axis);
            if (thickness <= 0.0001f)
            {
                _log.AppendLine("CORNICE: zero thickness on " + target.Name + ", cannot scale from it.");
                return false;
            }

            float factor = (thickness + target.Gap + OrnamentOverlapM) / thickness;
            if (factor > MaxScaleFactor || factor < 1f)
            {
                _log.AppendLine("CORNICE: REFUSED - needs " + factor.ToString("F2") + "x on " + target.Name +
                                " (limit " + MaxScaleFactor.ToString("F2") + "x). That is a measurement problem, " +
                                "not a moulding problem - review the axes above before forcing it.");
                return false;
            }

            Transform t = corniceGo.transform;
            Vector3 centre = cb.center;
            Vector3 startScale = t.localScale;
            Vector3 wanted = target.Axis == 0 ? new Vector3(startScale.x * factor, startScale.y, startScale.z)
                                 : target.Axis == 1 ? new Vector3(startScale.x, startScale.y * factor, startScale.z)
                                 : new Vector3(startScale.x, startScale.y, startScale.z * factor);

            Undo.RecordObject(t, "147VR Cornice Stretch");
            // Scale about the world bounds centre: apply the scale, then pull the pivot back until the bounds
            // centre returns to where it was. Converges immediately for an axis-aligned stretch on a baked mesh.
            float residual = 0f;
            for (int pass = 0; pass < 4; pass++)
            {
                t.localScale = wanted;
                Bounds now = cRen.bounds;
                Vector3 drift = now.center - centre;
                residual = drift.magnitude;
                if (residual < 0.0005f) break;
                t.position = t.position - drift;
            }
            EditorUtility.SetDirty(t);
            EditorSceneManager.MarkSceneDirty(scene);

            Bounds after = cRen.bounds;
            float gapAfter = Mathf.Max(AxisMin(after, target.Axis) - AxisMax(ob, target.Axis),
                                       AxisMin(ob, target.Axis) - AxisMax(after, target.Axis));
            _log.AppendLine("CORNICE: stretched " + target.Name + " " + factor.ToString("F4") + "x (" +
                            Fmt(startScale) + " -> " + Fmt(t.localScale) + ") to close " +
                            target.Gap.ToString("F4") + " m | bounds now " + Fmt(after.min) + ".." + Fmt(after.max) +
                            " | centre held to " + residual.ToString("F4") + " m | gap now " +
                            gapAfter.ToString("F4") + " m (negative = overlapping by design)");
            return true;
        }

        private sealed class AxisGap
        {
            public int Axis;
            public string Name;
            public float Gap;
        }

        // ------------------------------------------------------------------ C) emission keywords

        /// <summary>Adds the missing _EMISSION keyword where emission is actually authored.
        /// The trigger is the maximum RGB channel ONLY: _EmissionColor.a defaults to 1 even for a
        /// black-emissive material, and using it here wrongly lit up 629 materials on 2026-11-10.
        /// By this project's convention rgb already holds colour x strength, so black rgb means no
        /// emission, whatever alpha says.</summary>
        private static int RepairEmissionKeywords()
        {
            var paths = new List<string>();
            foreach (string folder in MaterialFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) { _log.AppendLine("EMISSION: folder missing " + folder); continue; }
                paths.AddRange(AssetDatabase.FindAssets("t:Material", new[] { folder })
                                           .Select(AssetDatabase.GUIDToAssetPath));
            }
            paths = paths.Where(p => p.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(p => p, StringComparer.Ordinal)
                         .ToList();

            int changed = 0, alreadyOk = 0, noEmission = 0;
            var touched = new List<Material>();

            foreach (string path in paths)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) continue;
                if (!m.HasProperty("_EmissionColor")) continue;

                Color e = m.GetColor("_EmissionColor");
                bool wantsEmission = Mathf.Max(e.r, Mathf.Max(e.g, e.b)) > 0.011f;
                if (!wantsEmission) { noEmission++; continue; }

                string txt = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
                bool hasKeyword = txt.Contains("- _EMISSION");
                bool flagsOk = txt.Contains("m_LightmapFlags: 2");
                if (hasKeyword && flagsOk) { alreadyOk++; continue; }

                // Shared with the parity pass on purpose: the flag must be set AFTER
                // ApplyModifiedPropertiesWithoutUndo, otherwise the apply pushes the stale serialized
                // value back and BakedEmissive never lands (this pass used to fail every file that way).
                WriteEmission(m, e, "147VR Emission Keyword");
                touched.Add(m);
                changed++;
                _log.AppendLine("  +keyword " + path + "  emission=" + Fmt(e) +
                                (hasKeyword ? " (keyword was already there)" : "") +
                                (flagsOk ? "" : "  flags 4->2"));
            }

            if (touched.Count > 0) AssetDatabase.SaveAssets();

            // Prove it from the SAVED FILES. An in-memory Material can report a keyword that never landed.
            int stillBad = 0;
            foreach (Material m in touched)
            {
                string path = AssetDatabase.GetAssetPath(m);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) { stillBad++; continue; }
                string txt = File.ReadAllText(path);
                if (!txt.Contains("- _EMISSION") || !txt.Contains("m_LightmapFlags: 2"))
                {
                    _log.AppendLine("  VERIFY FAIL " + path);
                    stillBad++;
                }
            }

            _log.AppendLine("EMISSION: scanned=" + paths.Count + " changed=" + changed +
                            " alreadyCorrect=" + alreadyOk + " emissionZero=" + noEmission +
                            " verifyFailures=" + stillBad);
            return changed;
        }

        /// <summary>Surgical cleanup for the 2026-11-10 over-application: 629 materials gained _EMISSION
        /// because the filter trusted _EmissionColor alpha, which defaults to 1 even for black emission.
        /// Strips the keyword from the black-emissive set only, through the Material API (never by
        /// editing YAML), and leaves every genuinely emissive material - and every other field - alone.</summary>
        public static int RevertWrongEmissionKeywords()
        {
            var paths = new List<string>();
            foreach (string folder in MaterialFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                paths.AddRange(AssetDatabase.FindAssets("t:Material", new[] { folder })
                                           .Select(AssetDatabase.GUIDToAssetPath));
            }
            paths = paths.Where(p => p.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(p => p, StringComparer.Ordinal)
                         .ToList();

            int stripped = 0, kept = 0, noKeyword = 0;
            var touched = new List<Material>();

            foreach (string path in paths)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || !File.Exists(path)) continue;
                string txt = File.ReadAllText(path);
                if (!txt.Contains("- _EMISSION")) { noKeyword++; continue; }

                Color e = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
                bool wantsEmission = Mathf.Max(e.r, Mathf.Max(e.g, e.b)) > 0.011f;
                if (wantsEmission) { kept++; continue; }

                var so = new SerializedObject(m);
                SerializedProperty kw = so.FindProperty("m_ValidKeywords");
                if (kw != null && kw.isArray)
                {
                    for (int i = kw.arraySize - 1; i >= 0; i--)
                        if (kw.GetArrayElementAtIndex(i).stringValue == "_EMISSION") kw.DeleteArrayElementAtIndex(i);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(m);
                touched.Add(m);
                stripped++;
            }

            if (touched.Count > 0) AssetDatabase.SaveAssets();

            int stillWrong = 0;
            foreach (Material m in touched)
            {
                string path = AssetDatabase.GetAssetPath(m);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) { stillWrong++; continue; }
                if (File.ReadAllText(path).Contains("- _EMISSION"))
                {
                    _log.AppendLine("  REVERT VERIFY FAIL " + path);
                    stillWrong++;
                }
            }

            _log.AppendLine("EMISSION REVERT: scanned=" + paths.Count + " stripped=" + stripped +
                            " legitimatelyKept=" + kept + " hadNoKeyword=" + noKeyword +
                            " stillWrong=" + stillWrong);
            return stripped;
        }

        // ------------------------------------------------------------------ rail material evidence

        /// <summary>Tests the owner's hypothesis that the "missing" Bay2 rail is a texture problem rather
        /// than missing geometry: a rail whose material has no _BaseMap and a white tint renders flat and
        /// reads as absent. Geometry was already proven present by the owner's own scene save (commit
        /// d6b161cb deleted the inactive legacy 12-foot prefab whose 120 blocked parts caused the misread).</summary>
        private static void AuditRailMaterials(Scene scene)
        {
            _log.AppendLine();
            _log.AppendLine("RAIL MATERIAL EVIDENCE (owner hypothesis: the rail looks missing because of its texture)");
            List<Renderer> rails = AllRenderers(scene)
                .Where(r => r.name.IndexOf("rail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            r.name.IndexOf("cushion", StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(500)
                .ToList();

            int noBase = 0, flatWhite = 0, noMat = 0, railNamed = 0;
            foreach (Renderer r in rails)
            {
                bool isRail = r.name.IndexOf("rail", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isRail) railNamed++;
                Material m = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
                if (m == null) { noMat++; if (isRail) _log.AppendLine("  " + r.name + " -> <NO MATERIAL>"); continue; }

                Texture baseMap = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                Color tint = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                bool white = baseMap == null && Mathf.Min(tint.r, Mathf.Min(tint.g, tint.b)) > 0.95f;
                if (baseMap == null) noBase++;
                if (white) flatWhite++;
                if (!isRail || white)
                {
                    _log.AppendLine("  " + (r.enabled ? "" : "RENDERER-OFF ") +
                                    (r.gameObject.activeInHierarchy ? "" : "INACTIVE ") +
                                    r.name + " -> " + m.name +
                                    " | baseMap=" + (baseMap != null ? baseMap.name : "<NONE>") +
                                    " tint=" + Fmt(tint) + (white ? "  *** FLAT WHITE (reads as missing)" : "") +
                                    " | size=" + Fmt(r.bounds.size));
                }
            }
            _log.AppendLine("  renderers matched=" + rails.Count + " (rail-named=" + railNamed +
                            ") no _BaseMap=" + noBase + " flat white=" + flatWhite + " without material=" + noMat);
        }

        // ------------------------------------------------------------------ capture

        /// <summary>Renders one shot through a temporary URP camera, following the recipe that
        /// 147VRLightRigAB.Capture already proved in this project. Reusing SceneView.camera produced six
        /// identical 25250-byte blank PNGs on 2026-11-10 - that camera does not render to a target texture
        /// this way - so a fresh Camera GameObject is used and the byte count is logged, because a real
        /// frame is far larger than an empty one and the report then shows it.</summary>
        private static void Capture(string name, Bounds target, Vector3 eyeOffset)
        {
            var camGo = new GameObject("147VR ArtFollowups Capture Camera");
            try
            {
                Directory.CreateDirectory(RenderDir);
                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.allowHDR = true;
                cam.fieldOfView = 58f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 120f;
                UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
                if (data != null) { data.renderPostProcessing = true; data.renderShadows = true; }

                Vector3 eye = target.center + eyeOffset;
                camGo.transform.SetPositionAndRotation(eye,
                    Quaternion.LookRotation((target.center - eye).normalized, Vector3.up));

                var rt = new RenderTexture(RenderWidth, RenderHeight, 24, RenderTextureFormat.ARGB32,
                                           RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                rt.Create();

                RenderTexture previousActive = RenderTexture.active;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previousActive;
                cam.targetTexture = null;

                string file = Path.Combine(RenderDir, name + ".png");
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(file, bytes);
                _log.AppendLine("CAPTURE " + name + " -> " + file + " (" + RenderWidth + "x" + RenderHeight +
                                ", " + bytes.Length + " bytes, eye=" + Fmt(eye) + ")");

                UnityEngine.Object.DestroyImmediate(tex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
            catch (Exception e)
            {
                _log.AppendLine("CAPTURE " + name + ": ERROR " + e.GetType().Name + ": " + e.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        // ------------------------------------------------------------------ helpers

        private static Scene ActiveWipRoom()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                _log.AppendLine("REFUSED: the loaded Scene is '" + scene.path + "', expected " + SourceScenePath +
                                ". Nothing was modified. Open it with menu WipRoom/0).");
                return default;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _log.AppendLine("REFUSED: the Editor is in (or entering) Play Mode. Nothing was modified.");
                return default;
            }
            return scene;
        }

        private static List<Renderer> AllRenderers(Scene scene) =>
            scene.GetRootGameObjects()
                 .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                 .Where(r => r != null)
                 .ToList();

        private static List<Renderer> GetFlags(Scene scene) =>
            AllRenderers(scene)
                .Where(r => r.name.StartsWith("SM_147VR_AAA_Flag_", StringComparison.OrdinalIgnoreCase))
                .ToList();

        private static GameObject FindOne(List<Renderer> renderers, string contains)
        {
            Renderer hit = renderers.FirstOrDefault(r => r.name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0);
            return hit != null ? hit.gameObject : null;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            var active = rs.Where(r => r != null).ToList();
            if (active.Count == 0) return new Bounds(go.transform.position, Vector3.one * 0.5f);
            Bounds b = active[0].bounds;
            for (int i = 1; i < active.Count; i++) b.Encapsulate(active[i].bounds);
            return b;
        }

        private static string Describe(Bounds b, int count) =>
            count == 0 ? "<no renderers>"
                       : "n=" + count + " min=" + Fmt(b.min) + " max=" + Fmt(b.max) + " size=" + Fmt(b.size);

        private static Bounds? ReportBounds(GameObject go, string label)
        {
            if (go == null) { _log.AppendLine(label + ": NOT FOUND in the loaded Scene"); return null; }
            Renderer r = go.GetComponent<Renderer>();
            Transform t = go.transform;
            _log.AppendLine(label + " pivot=" + Fmt(t.position) + " scale=" + Fmt(t.localScale) +
                            " bounds=" + (r != null ? Fmt(r.bounds.min) + ".." + Fmt(r.bounds.max) : "<no renderer>"));
            return r != null ? r.bounds : (Bounds?)null;
        }

        private static float AxisMin(Bounds b, int a) => a == 0 ? b.min.x : a == 1 ? b.min.y : b.min.z;
        private static float AxisMax(Bounds b, int a) => a == 0 ? b.max.x : a == 1 ? b.max.y : b.max.z;
        private static float AxisSize(Bounds b, int a) => AxisMax(b, a) - AxisMin(b, a);

        private static Vector3 FlatDir(Vector3 from, Vector3 toward)
        {
            Vector3 d = toward - from;
            d.y = 0f;
            return d.sqrMagnitude < 1e-8f ? Vector3.forward : d.normalized;
        }

        private static Bounds UnionOf(List<Renderer> renderers)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static Mesh GetMesh(Renderer renderer) =>
            renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                                                    : renderer.GetComponent<MeshFilter>() != null
                                                        ? renderer.GetComponent<MeshFilter>().sharedMesh
                                                        : null;

        private sealed class WallPoint { public float X, Y, Z; public string Source; }

        private struct WallFace
        {
            public float Z;
            public string SourceName;
            public bool Valid => !float.IsNaN(Z);
        }

        private static List<WallPoint> CollectWallPointsInBand(List<Renderer> others, Bounds row)
        {
            var points = new List<WallPoint>();
            int scanned = 0;
            foreach (Renderer r in others)
            {
                bool wallish = r.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               r.name.Equals("SM_0020_Cube.004", StringComparison.OrdinalIgnoreCase);
                if (!wallish) continue;

                Bounds rb = r.bounds;
                if (rb.max.z < row.min.z - 2f || rb.min.z > row.max.z + 2f) continue;
                if (rb.min.x > row.max.x || rb.max.x < row.min.x) continue;

                Mesh mesh = GetMesh(r);
                if (mesh == null || mesh.vertexCount > MaxVerticesForWallScan) continue;
                scanned++;

                Matrix4x4 l2w = r.transform.localToWorldMatrix;
                Vector3[] verts = mesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 w = l2w.MultiplyPoint(verts[i]);
                    if (w.y < row.min.y - 0.3f || w.y > row.max.y + 0.3f) continue;
                    if (w.x < row.min.x - 0.5f || w.x > row.max.x + 0.5f) continue;
                    if (w.z < row.min.z - 1.5f || w.z > row.max.z + 1.0f) continue;
                    points.Add(new WallPoint { X = w.x, Y = w.y, Z = w.z, Source = r.name });
                }
            }
            _log.AppendLine("FLAGS: wall vertices sampled=" + points.Count + " from meshes scanned=" + scanned +
                            " | innermost wall z=" + (points.Count == 0 ? "<none>" : points.Max(p => p.Z).ToString("F3")) +
                            " | cloth z=" + row.min.z.ToString("F3") + ".." + row.max.z.ToString("F3"));
            return points;
        }

        private static WallFace NearestWallFaceBehind(List<WallPoint> band, Bounds flagBounds)
        {
            float best = float.MinValue;
            string source = null;
            foreach (WallPoint p in band)
            {
                if (p.X < flagBounds.min.x - 0.15f || p.X > flagBounds.max.x + 0.15f) continue;
                if (p.Y < flagBounds.min.y - 0.10f || p.Y > flagBounds.max.y + 0.10f) continue;
                if (p.Z > best) { best = p.Z; source = p.Source; }
            }
            return new WallFace { Z = source == null ? float.NaN : best, SourceName = source ?? "<none>" };
        }

        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F3") + ", " + v.y.ToString("F3") + ", " + v.z.ToString("F3") + ")";

        private static string Fmt(Color c) =>
            "(" + c.r.ToString("F3") + ", " + c.g.ToString("F3") + ", " + c.b.ToString("F3") + ", " +
            c.a.ToString("F3") + ")";

        private static void Dialog(string title, string body)
        {
            if (SilenceDialogs) return;
            EditorUtility.DisplayDialog(title, body, "OK");
        }
    }
}
#endif
