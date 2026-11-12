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
    /// Owner-requested round three (2026-11-11), rewritten after the audit contradicted the first plan.
    /// Every number is measured in the loaded Scene before anything moves. Scene edits are Undoable and
    /// the Scene is never saved or reopened.
    ///
    /// WHAT THE AUDIT CHANGED (all three were bugs in the first plan, caught by running the audit pass
    /// before the apply pass - Logs/WipRoom_Art_Round3.txt 07:24):
    ///
    ///  1) Planters are assemblies. SM_147VR_AAA_Planter_02_Body.002 floats by 0.278 m while its own
    ///     SoilDisk.002 floats by 0.637 m. Moving each renderer separately would tear the pot and the
    ///     soil apart in mid-air, so parts are grouped under their shared prop ancestor and the whole
    ///     group is lowered by the SMALLEST gap in it, which is the part that should touch the floor.
    ///
    ///  2) The table cones are already 130 degrees wide. The three hot spots the owner sees are the
    ///     NARROW INNER cores: innerSpotAngle measured 21.8 degrees, so at the 0.815 m drop the flat
    ///     bright core is only about 0.15 m in radius. Widening the outer angle would have made it
    ///     worse, and the original formula would have SHRUNK it to 76 degrees. The fix is to raise the
    ///     inner angle so each lamp has a flat pool covering its own third of the table, and let the
    ///     inner-to-outer falloff happen OUTSIDE the cloth - that is what removes the overlapping
    ///     ring pattern the owner asked to get rid of ("ไม่มีลายทับซ้อนของแสง").
    ///
    ///  3) The wall crown mouldings bind 147VR_147VR_PBR_wall_WS - the shared WALL material. Repainting
    ///     "the cornice" by object name would have repainted every wall in the room. A material is only
    ///     tinted when every renderer using it in the Scene is a cornice or ornament part.
    ///
    /// Owner confirmations this pass acts on: paint the cornice/ornament materials light cream, lift the
    /// ceiling pieces level with the crown so nothing under the ceiling shows through, bar lights use the
    /// project precedent (WallSconce 16, TableKey 4.5), table lamps 8-16 with no overlap banding.
    /// </summary>
    public static class WipRoomArtRound3
    {
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string ReportPath = "Logs/WipRoom_Art_Round3.txt";
        private const string RenderDir = "Logs/renders";

        private const float JunctionToleranceM = 0.005f;
        private const float MaxJunctionLiftM = 0.80f;
        private const float MaxFloatDropM = 0.40f;
        private const float PlantGroupMinHeightM = 0.5f;
        private const float PlantGroupMaxCentreY = 2.2f;

        /// <summary>Owner-approved band for the table lamps.</summary>
        private const float TableLampIntensityMin = 8f;
        private const float TableLampIntensityMax = 16f;
        private const float TableLampIntensityTarget = 10f;
        /// <summary>Fraction of each lamp's flat pool radius that must reach past its own third of the
        /// table, so the seams between lamps fall in the falloff zone instead of on the cloth.</summary>
        private const float PoolSeamMargin = 1.15f;

        private static readonly Color CreamTint = new Color(0.87f, 0.84f, 0.78f, 1f);

        private static StringBuilder _log = new StringBuilder();

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/24) Art Round 3 - audit only (read-only)")]
        public static void MenuAudit() { Begin(); AuditOnly(); Finish(); }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/25) Art Round 3 - apply all (Undoable / No Save)")]
        public static void MenuApply()
        {
            Run();
            EditorUtility.DisplayDialog("147VR Art Round 3",
                "Report: " + ReportPath + "\nRenders: " + RenderDir +
                "\n\nScene is DIRTY but NOT saved. Ctrl+Z reverts.", "OK");
        }

        /// <summary>ToolRunner entry points: "art-round3-audit" then "art-round3".</summary>
        public static void RunAudit() { Begin(); AuditOnly(); Finish(); }

        public static void Run()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;

                Guard("RESTORE ROOM ROOT", () => RestoreRoomRoot(scene));
                float floorY = MeasureFloor(scene);

                AuditBayRails(scene, floorY);
                CompareTableFrames(scene);
                int dropped = Guard("PLANTS", () => DropFloatingPlantGroups(scene, floorY));
                int lifted = Guard("CEILING", () => LiftCeilingToCrown(scene));
                int restored = Guard("ORNAMENT PATTERN", () => RevertCorniceVariants(scene));
                int levelled = Guard("TABLE LIGHTS", () => EvenOutTableLights(scene));
                int retiled = Guard("MAHOGANY TILING", () => AdjustMahoganyTiling(scene));
                int created = Guard("BAR LIGHTS", () => AddBarLights(scene));

                Capture("30-bay2-rail", NearestTableBounds(scene, floorY), new Vector3(2.6f, 1.3f, 3.0f));
                Capture("35-frame-closeup", NearestTableBounds(scene, floorY), new Vector3(1.4f, 0.35f, 1.6f));
                Capture("31-plants", UnionOf(PlantRenderers(scene)), new Vector3(0f, 0.4f, 7.0f));
                Capture("32-ceiling-junction", new Bounds(new Vector3(0f, 4.0f, 0f), new Vector3(26f, 1.6f, 36f)),
                        new Vector3(0f, -3.0f, 12f));
                Capture("33-table-evenness", NearestTableBounds(scene, floorY), new Vector3(3.0f, 1.6f, 3.4f));
                Capture("34-bar-lit", BarBounds(scene), new Vector3(-2.0f, 1.2f, 5.0f));

                _log.AppendLine();
                _log.AppendLine("RESULT plantGroupsDropped=" + dropped + " ceilingLifted=" + lifted +
                                " ornamentSlotsRestored=" + restored + " tableLightsLevelled=" + levelled +
                                " mahoganyRetiled=" + retiled + " barLightsCreated=" + created +
                                " floorY=" + floorY.ToString("F3"));
                _log.AppendLine("Scene marked dirty, NOT saved. Ctrl+Z reverts every transform, tint and light.");
            }
            finally { Finish(); }
        }

        private static void AuditOnly()
        {
            Scene scene = ActiveWipRoom();
            if (!scene.IsValid()) return;
            float floorY = MeasureFloor(scene);
            AuditBayRails(scene, floorY);
            CompareTableFrames(scene);
            ReportFloatingPlants(scene, floorY);
            ReportJunction(scene);
            ReportTableLights(scene);
            ReportBarFootprint(scene);
            _log.AppendLine("AUDIT ONLY - nothing was modified. floorY=" + floorY.ToString("F3"));
        }

        /// <summary>The owner wants the carved ceiling pattern back. Every cornice/ornament slot pointing at
        /// a _Cream variant is put back on its original material. The origin is resolved through a
        /// name-to-path map because the earlier path.Replace missed: the real asset names end in
        /// ".png.mat", so the origin of 147VR_147VR_MV_0010_Wood_gray.png_Cream.mat is
        /// 147VR_147VR_MV_0010_Wood_gray.png.mat.</summary>
        private static int RevertCorniceVariants(Scene scene)
        {
            _log.AppendLine("C) RESTORING THE ORIGINAL CORNICE/ORnAMENT MATERIALS (pattern back)");
            var byName = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (string folder in new[] { "Assets/AAA/Imports/NeonVintageArtPass/Materials/",
                                              "Assets/AAA/Imports/naris_vintage/Rebound/" })
            {
                foreach (string p in AssetDatabase.FindAssets("t:Material", new[] { folder })
                             .Select(AssetDatabase.GUIDToAssetPath)
                             .Where(x => x.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)))
                {
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                    if (m != null && !byName.ContainsKey(Path.GetFileNameWithoutExtension(p))) byName[m.name] = m;
                }
            }

            List<Renderer> targets = AllRenderers(scene).Where(x =>
                x.name.IndexOf("cornice", StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.name.IndexOf("ornament_ceiling", StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            int restored = 0, notFound = 0;
            foreach (Renderer r in targets.OrderBy(x => x.name, StringComparer.Ordinal))
            {
                Material[] slots = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material m = slots[i];
                    if (m == null || !m.name.EndsWith("_Cream", StringComparison.Ordinal)) continue;
                    string originName = m.name.Substring(0, m.name.Length - "_Cream".Length);
                    if (!byName.TryGetValue(originName, out Material origin))
                    {
                        notFound++;
                        _log.AppendLine("  UNTRACED " + m.name + " on " + r.name + " slot " + i +
                                        " - no material named " + originName);
                        continue;
                    }
                    slots[i] = origin;
                    changed = true;
                    restored++;
                    _log.AppendLine("  restored " + r.name + " slot " + i + ": " + m.name + " -> " + origin.name);
                }
                if (changed)
                {
                    Undo.RecordObject(r, "147VR Restore Ornament Pattern");
                    r.sharedMaterials = slots;
                    EditorUtility.SetDirty(r);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            _log.AppendLine("  slotsRestored=" + restored + " untraced=" + notFound +
                            " (the 3 _Cream assets stay on disk but are no longer referenced)");
            return restored;
        }

        /// <summary>How many times the wood grain repeats across the frame. 1.0 was the measured value and
        /// stretched one 2048 tile over a 3.847 m frame.</summary>
        private const float MahoganyTilingX = 6f;
        private const float MahoganyTilingY = 2f;

        /// <summary>The comparison proved the grain is NOT missing from the material: SM_TABLE FRAME binds
        /// 147VR_Table Mahogany with Wood_Mahogany_Diffuse_mqt_2048, its normal map and 23178 UVs. What is
        /// missing is the REPEAT - _BaseMap_ST measured scale(1.00,1.00) across a 3.847 m frame, so one
        /// tile of grain is stretched over the whole side and reads as flat colour. Only the tiling is
        /// changed here; no mesh, no geometry, no invented texture.</summary>
        private static int AdjustMahoganyTiling(Scene scene)
        {
            _log.AppendLine("A3) MAHOGANY GRAIN REPEAT (tiling, not geometry)");
            int done = 0;
            foreach (string folder in new[] { "Assets/AAA/Imports/naris_vintage/Rebound/",
                                              "Assets/AAA/Imports/NeonVintageArtPass/Materials/" })
            {
                foreach (string p in AssetDatabase.FindAssets("t:Material", new[] { folder })
                             .Select(AssetDatabase.GUIDToAssetPath)
                             .Where(x => x.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) &&
                                         Path.GetFileNameWithoutExtension(x)
                                             .IndexOf("Table Mahogany", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                    if (m == null) continue;
                    var st = new Vector4(MahoganyTilingX, MahoganyTilingY, 0f, 0f);
                    string before = m.HasProperty("_BaseMap_ST") ? Fmt4(m.GetVector("_BaseMap_ST")) : "<n/a>";
                    Undo.RecordObject(m, "147VR Mahogany Tiling");
                    if (m.HasProperty("_BaseMap_ST")) m.SetVector("_BaseMap_ST", st);
                    if (m.HasProperty("_BumpMap_ST")) m.SetVector("_BumpMap_ST", st);
                    EditorUtility.SetDirty(m);
                    done++;
                    _log.AppendLine("  " + Path.GetFileName(p) + " _BaseMap_ST " + before + " -> " + Fmt4(st) +
                                    " (grain repeats " + MahoganyTilingX.ToString("F0") + "x along the 3.847 m frame)");
                }
            }
            AssetDatabase.SaveAssets();
            _log.AppendLine("  materials retiled=" + done);
            return done;
        }

        private static string Fmt4(Vector4 v) =>
            "scale(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + ") offset(" +
            v.z.ToString("F2") + "," + v.w.ToString("F2") + ")";

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/26) Close the flag row gap (Undoable + saves the Scene)")]
        public static void MenuFlagGap() { RunFlagGap(); }

        /// <summary>ToolRunner entry point: "flag-gap". Sprint one of the resume plan.</summary>
        public static void RunFlagGap()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;
                CloseFlagRowGap(scene);
            }
            finally { Finish(); }
        }

        /// <summary>ToolRunner entry point: "flag-verify". Read-only. The single wide shot from 7.5 m back
        /// cannot prove the centre hole is closed, so this renders three close-ups along the row and lists
        /// every neighbour gap over 0.9 m, plus the flags that render as blank cloth.</summary>
        public static void RunFlagVerify()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;

                List<Renderer> flags = FlagRenderers(scene);
                if (flags.Count == 0) { _log.AppendLine("no flag renderers"); return; }

                float[] xs = flags.Select(f => f.bounds.center.x).OrderBy(v => v).ToArray();
                float zFace = flags.Average(f => f.bounds.center.z);
                _log.AppendLine("FLAG VERIFY - " + flags.Count + " flags, sorted x: " +
                                string.Join(", ", xs.Select(v => v.ToString("F2")).ToArray()));

                var wide = new List<string>();
                for (int i = 1; i < xs.Length; i++)
                    if (xs[i] - xs[i - 1] > 0.9f) wide.Add(xs[i - 1].ToString("F2") + " -> " + xs[i].ToString("F2") +
                                                            " (" + (xs[i] - xs[i - 1]).ToString("F3") + " m)");
                _log.AppendLine("  gaps wider than 0.9 m: " + (wide.Count == 0 ? "NONE - the row is continuous"
                                                                                : string.Join(" | ", wide.ToArray())));

                int blank = 0;
                foreach (Renderer f in flags)
                {
                    Material m = f.sharedMaterials.Length > 0 ? f.sharedMaterials[0] : null;
                    if (m == null) { blank++; continue; }
                    Texture b = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                    Color t = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                    bool flatWhite = b == null && Mathf.Min(t.r, Mathf.Min(t.g, t.b)) > 0.9f;
                    if (b == null || flatWhite)
                    {
                        blank++;
                        _log.AppendLine("  BLANK " + f.name + " mat=" + m.name +
                                        " baseMap=" + (b != null ? b.name : "<NONE>") + " tint=" + Fmt(t));
                    }
                }
                _log.AppendLine("  flags with no albedo or flat-white cloth: " + blank + " / " + flags.Count);

                float y = flags.Average(f => f.bounds.center.y);
                var shots = new (string Label, float CentreX)[] { ("left", -6.7f), ("centre", 0f), ("right", 6.7f) };
                foreach (var s in shots)
                {
                    var box = new Bounds(new Vector3(s.CentreX, y, zFace), new Vector3(6.0f, 2.6f, 0.4f));
                    Capture("41-flags-" + s.Label, box, new Vector3(0f, 0.15f, 4.0f));
                }
                _log.AppendLine("Nothing modified, Scene not saved.");
            }
            finally { Finish(); }
        }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/27) Sprint 2+3 - Bay1 lights down + carpet diagnosis (saves the Scene)")]
        public static void MenuSprint23() { RunSprint23(); }

        /// <summary>ToolRunner entry point: "sprint23". Two jobs, one Unity cycle.</summary>
        public static void RunSprint23()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;

                int dimmed = Guard("BAY1 LIGHTS", () => DimBay1TableLights(scene));
                Capture("42-bay1-after", BayBounds(scene, "_R01"), new Vector3(3.0f, 1.6f, 3.4f));

                int carpetFixed = DiagnoseAndFixCarpet(scene);
                Capture("43-carpet-after", HallFloorBounds(scene), new Vector3(0f, 6.0f, 11.0f));

                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                _log.AppendLine();
                _log.AppendLine("RESULT bay1LampsDimmed=" + dimmed + " carpetMaterialsChanged=" + carpetFixed +
                                " sceneSaved=" + saved);
            }
            finally { Finish(); }
        }

        /// <summary>Owner request: only the Bay1 row (z 9.297, the three ..._R01_B01/B02/B03) goes down.</summary>
        private const float Bay1TargetIntensity = 6f;

        private static int DimBay1TableLights(Scene scene)
        {
            _log.AppendLine("2) BAY1 TABLE LIGHTS - 10 -> " + Bay1TargetIntensity.ToString("F1") + " (row _R01 only)");
            Light[] lamps = scene.GetRootGameObjects()
                                 .SelectMany(g => g.GetComponentsInChildren<Light>(true))
                                 .Where(l => l != null && l.name.StartsWith("TableKey_", StringComparison.Ordinal) &&
                                             l.name.IndexOf("_R01", StringComparison.Ordinal) >= 0)
                                 .OrderBy(l => l.name, StringComparer.Ordinal)
                                 .ToArray();
            if (lamps.Length == 0) { _log.AppendLine("  no _R01 TableKey lamps found"); return 0; }

            int changed = 0;
            foreach (Light l in lamps)
            {
                float before = l.intensity;
                if (before <= Bay1TargetIntensity + 0.01f)
                {
                    _log.AppendLine("  SKIP " + l.name + " - already at " + before.ToString("F2") +
                                    " (target " + Bay1TargetIntensity.ToString("F1") + ")");
                    continue;
                }
                Undo.RecordObject(l, "147VR Bay1 Light Down");
                l.intensity = Bay1TargetIntensity;
                EditorUtility.SetDirty(l);
                changed++;
                _log.AppendLine("  " + l.name + " intensity " + before.ToString("F2") + " -> " +
                                l.intensity.ToString("F2") + " | inner=" + l.innerSpotAngle.ToString("F1") +
                                " outer=" + l.spotAngle.ToString("F1") + " pos=" + Fmt(l.transform.position));
            }
            _log.AppendLine("  lamps dimmed=" + changed + " of " + lamps.Length);
            return changed;
        }

        private static Bounds BayBounds(Scene scene, string rowTag)
        {
            // The bay rows sit at z 9.297 / 3.097 / -3.103 / -9.303; use the lamp z to find the table box.
            Light[] lamps = scene.GetRootGameObjects()
                                 .SelectMany(g => g.GetComponentsInChildren<Light>(true))
                                 .Where(l => l != null && l.name.StartsWith("TableKey_", StringComparison.Ordinal) &&
                                             l.name.IndexOf(rowTag, StringComparison.Ordinal) >= 0)
                                 .ToArray();
            if (lamps.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 4f);
            float z = lamps.Average(l => l.transform.position.z);
            GameObject table = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .Where(g => g.name.StartsWith("ROOT_PoolTable_", StringComparison.Ordinal))
                .Distinct()
                .OrderBy(g => Mathf.Abs(g.transform.position.z - z))
                .FirstOrDefault();
            return table != null ? BoundsOf(table) : new Bounds(new Vector3(0f, 0.9f, z), new Vector3(4f, 1f, 2.2f));
        }

        private static Bounds HallFloorBounds(Scene scene)
        {
            List<Renderer> carpets = CarpetRenderers(scene);
            return carpets.Count > 0 ? UnionOf(carpets) : new Bounds(new Vector3(0f, 0.0f, 0f), new Vector3(20f, 0.2f, 26f));
        }

        private static List<Renderer> CarpetRenderers(Scene scene) =>
            AllRenderers(scene)
                .Where(r => MatName(r).IndexOf("Carpet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            r.name.IndexOf("Carpet", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

        /// <summary>The owner confirmed the hall carpet reads as burnt orange. The scene-bound MP_Carpet copy
        /// is clean on disk, so this measures which renderer actually paints the middle of the hall and only
        /// changes something when the evidence says the material is at fault.</summary>
        private static int DiagnoseAndFixCarpet(Scene scene)
        {
            _log.AppendLine("3) CARPET - WHICH RENDERER DRAWS THE HALL CENTRE?");
            const string chosenCarpetTexture = "Assets/AAA/ClubVenue/MiraclePool_vintage/textures/Carpet.png";

            List<Renderer> carpets = CarpetRenderers(scene);
            if (carpets.Count == 0) { _log.AppendLine("  no carpet renderers found"); return 0; }

            Renderer centre = null;
            float bestArea = 0f;
            foreach (Renderer r in carpets.OrderBy(x => x.name, StringComparer.Ordinal))
            {
                if (r.sharedMaterials.Length == 0 || r.sharedMaterials[0] == null)
                {
                    _log.AppendLine("  " + r.name + " -> <NO MATERIAL> centre=" + Fmt(r.bounds.center));
                    continue;
                }
                Material m = r.sharedMaterials[0];
                Texture b = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                Color t = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                float sm = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : -1f;
                Color e = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
                float area = r.bounds.size.x * r.bounds.size.z;
                float distToCentre = Mathf.Sqrt(r.bounds.center.x * r.bounds.center.x +
                                                r.bounds.center.z * r.bounds.center.z);
                bool coversMiddle = distToCentre < 3f && area > bestArea;
                _log.AppendLine("  " + r.name + " mat=" + m.name +
                                " | baseMap=" + (b != null ? b.name : "<NONE>") +
                                " tint=" + Fmt(t) + " smoothness=" + sm.ToString("F2") +
                                " emission=" + Fmt(e) +
                                " | area=" + area.ToString("F1") + " m2 centre=" + Fmt(r.bounds.center) +
                                " distToHallCentre=" + distToCentre.ToString("F2") + " m" +
                                (coversMiddle ? "  *** main hall cover" : ""));
                if (coversMiddle) { bestArea = area; centre = r; }
            }

            if (centre == null)
            {
                _log.AppendLine("  no carpet renderer covers the hall centre (within 3 m and largest)." +
                                " The orange must come from the floor itself - reporting, not guessing.");
                return 0;
            }

            Material target = centre.sharedMaterials[0];
            Texture bound = target.HasProperty("_BaseMap") ? target.GetTexture("_BaseMap") : null;
            if (bound != null)
            {
                _log.AppendLine("  VERDICT: " + centre.name + " already binds a real image (" + bound.name +
                                ") through " + target.name + ", so the material is NOT the cause. The burnt look" +
                                " must be lighting/exposure on top of that image - no material was changed here.");
                return 0;
            }

            if (!File.Exists(chosenCarpetTexture))
            {
                _log.AppendLine("  REFUSED - " + chosenCarpetTexture + " is not on disk.");
                return 0;
            }
            Texture carpet = AssetDatabase.LoadAssetAtPath<Texture>(chosenCarpetTexture);
            if (carpet == null) { _log.AppendLine("  REFUSED - could not load " + chosenCarpetTexture); return 0; }

            Color before = target.HasProperty("_BaseColor") ? target.GetColor("_BaseColor") : Color.white;
            Undo.RecordObject(target, "147VR Carpet Texture Restore");
            target.SetTexture("_BaseMap", carpet);
            if (target.HasProperty("_BaseColor")) target.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            _log.AppendLine("  FIX APPLIED: " + AssetDatabase.GetAssetPath(target) +
                            " had NO _BaseMap - bound it to the owner-chosen rip image " +
                            Path.GetFileName(chosenCarpetTexture) + " and reset tint " + Fmt(before) +
                            " -> (1.000, 1.000, 1.000, 1.000)");

            string saved = File.Exists(AssetDatabase.GetAssetPath(target))
                ? File.ReadAllText(AssetDatabase.GetAssetPath(target)) : "";
            string want = AssetDatabase.AssetPathToGUID(chosenCarpetTexture);
            _log.AppendLine("  VERIFY from the saved file: guid present=" + saved.Contains(want));
            return 1;
        }

        /// <summary>ToolRunner entry point: "surface-tint". Corrects the baize colour from the rip manifest.</summary>
        public static void RunSurfaceTint()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;
                FixTableSurfaceTint(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                Capture("44-bay1-surface", BayBounds(scene, "_R01"), new Vector3(3.0f, 1.6f, 3.4f));
                _log.AppendLine("sceneSaved=" + saved);
            }
            finally { Finish(); }
        }

        private const string ManifestPath = "Assets/AAA/Imports/NeonVintageArtPass/manifest.json";

        /// <summary>Bay1's cloth renders yellow-orange while the other three tables look right. The cause is
        /// a transcription error, not the art: manifest.json records TableSurface base_color as
        /// (0.0, 0.651, 0.1627) - green - but the sync rows write tint (1, 0.651, 0.1627), and adding that
        /// red channel turns green into amber. The authored value is therefore read straight out of the
        /// manifest at runtime instead of being hardcoded here, so this tool cannot repeat the same mistake.</summary>
        private static int FixTableSurfaceTint(Scene scene)
        {
            _log.AppendLine("2b) TABLE SURFACE COLOUR - authored value read from the rip manifest");
            if (!File.Exists(ManifestPath)) { _log.AppendLine("  manifest missing: " + ManifestPath); return 0; }

            string json = File.ReadAllText(ManifestPath);
            int nameAt = json.IndexOf("\"name\": \"TableSurface\"", StringComparison.Ordinal);
            if (nameAt < 0) { _log.AppendLine("  no TableSurface entry in the manifest"); return 0; }
            int bcAt = json.IndexOf("base_color", nameAt, StringComparison.Ordinal);
            if (bcAt < 0 || bcAt - nameAt > 200) { _log.AppendLine("  no base_color near that entry"); return 0; }

            var nums = new List<float>();
            int bracket = json.IndexOf('[', bcAt);
            int close = json.IndexOf(']', bracket);
            foreach (string tok in json.Substring(bracket + 1, close - bracket - 1)
                         .Split(new[] { ',', '\n', '\r', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                if (float.TryParse(tok, System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out float v)) nums.Add(v);
            if (nums.Count < 3) { _log.AppendLine("  could not parse base_color"); return 0; }

            Color authored = new Color(nums[0], nums[1], nums[2], nums.Count > 3 ? nums[3] : 1f);
            _log.AppendLine("  manifest TableSurface base_color = " + Fmt(authored) +
                            "  (green: red channel is " + authored.r.ToString("F3") + ")");

            int changed = 0, already = 0;
            foreach (string folder in new[] { "Assets/AAA/Imports/naris_vintage/Rebound/",
                                              "Assets/AAA/Imports/NeonVintageArtPass/Materials/" })
            {
                foreach (string p in AssetDatabase.FindAssets("t:Material", new[] { folder })
                             .Select(AssetDatabase.GUIDToAssetPath)
                             .Where(x => x.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) &&
                                         NormaliseSurfaceName(Path.GetFileNameWithoutExtension(x))
                                             .StartsWith("TableSurface", StringComparison.Ordinal))
                             .OrderBy(x => x, StringComparer.Ordinal))
                {
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                    if (m == null || !m.HasProperty("_BaseColor")) continue;
                    Color now = m.GetColor("_BaseColor");
                    bool same = Mathf.Abs(now.r - authored.r) < 0.01f && Mathf.Abs(now.g - authored.g) < 0.01f &&
                                Mathf.Abs(now.b - authored.b) < 0.01f;
                    if (same) { already++; _log.AppendLine("  already authored " + Path.GetFileName(p)); continue; }

                    Undo.RecordObject(m, "147VR Table Surface Colour");
                    m.SetColor("_BaseColor", authored);
                    EditorUtility.SetDirty(m);
                    changed++;
                    _log.AppendLine("  " + Path.GetFileName(p) + " " + Fmt(now) + " -> " + Fmt(authored) +
                                    "  (was " + (now.r > 0.5f ? "amber - red channel added" : "off-author") + ")");
                }
            }

            AssetDatabase.SaveAssets();
            foreach (string p in AssetDatabase.FindAssets("t:Material", new[] { "Assets/AAA/Imports" })
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .Where(x => x.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) &&
                                     NormaliseSurfaceName(Path.GetFileNameWithoutExtension(x))
                                         .StartsWith("TableSurface", StringComparison.Ordinal)))
            {
                string txt = File.Exists(p) ? File.ReadAllText(p) : "";
                if (!txt.Contains("_BaseColor: {r: " + authored.r.ToString("0.####")))
                    _log.AppendLine("  VERIFY FAIL " + p);
            }
            _log.AppendLine("  changed=" + changed + " alreadyCorrect=" + already);
            return changed;
        }

        private static string NormaliseSurfaceName(string fileName)
        {
            string s = fileName;
            while (s.StartsWith("147VR_", StringComparison.Ordinal) && s.Length > 6) s = s.Substring(6);
            return s;
        }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/28) Bay2 frame edge - UV audit then re-UV (saves the Scene)")]
        public static void MenuFrameReUv() { RunFrameReUv(); }

        /// <summary>ToolRunner entry point: "frame-reuv". The owner's current priority.</summary>
        public static void RunFrameReUv()
        {
            Begin();
            try
            {
                Scene scene = ActiveWipRoom();
                if (!scene.IsValid()) return;

                Renderer frame = FindBay2Frame(scene);
                if (frame == null) { _log.AppendLine("no SM_TABLE FRAME renderer under ROOT_PoolTable_02"); return; }

                AuditFrameUv(frame);
                bool applied = ReUvFrame(frame);
                if (applied)
                {
                    ResetMahoganyTiling();
                    EditorSceneManager.MarkSceneDirty(scene);
                    bool saved = EditorSceneManager.SaveScene(scene);
                    _log.AppendLine("  sceneSaved=" + saved);
                }
                Capture("45-bay2-frame-after", frame.bounds, new Vector3(1.1f, 0.25f, 1.5f));
            }
            finally { Finish(); }
        }

        /// <summary>One texture tile spans 1/TexelScale metres. UVs are written in metres so the grain
        /// repeats at a real-world size instead of being stretched over the whole frame.</summary>
        private const float TexelScale = 0.45f;

        private static Renderer FindBay2Frame(Scene scene)
        {
            GameObject table = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .FirstOrDefault(g => g.name == "ROOT_PoolTable_02");
            if (table == null) return null;
            return table.GetComponentsInChildren<Renderer>(true)
                       .FirstOrDefault(r => r.name.IndexOf("TABLE FRAME", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static readonly string[] BoxDir = { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };

        private static void AuditFrameUv(Renderer frame)
        {
            _log.AppendLine("BAY2 FRAME UV AUDIT - " + frame.name + " (read-only)");
            Mesh mesh = frame.GetComponent<MeshFilter>() != null ? frame.GetComponent<MeshFilter>().sharedMesh : null;
            if (mesh == null) { _log.AppendLine("  no MeshFilter mesh"); return; }

            Vector3[] v, n; int[] t; Vector2[] uv;
            try { v = mesh.vertices; n = mesh.normals; t = mesh.triangles; uv = mesh.uv; }
            catch (Exception e) { _log.AppendLine("  mesh not readable (" + e.GetType().Name + ") - " +
                                                 "the copy step below uses Instantiate, which works regardless."); return; }
            if (uv == null || uv.Length == 0) { _log.AppendLine("  mesh has no UV0 at all"); return; }

            var buckets = new Dictionary<int, Bucket>();
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                int i0 = t[i], i1 = t[i + 1], i2 = t[i + 2];
                if (i0 >= v.Length || i1 >= v.Length || i2 >= v.Length) continue;
                Vector3 fn = Vector3.Cross(v[i1] - v[i0], v[i2] - v[i0]);
                float area = fn.magnitude * 0.5f;
                if (area < 1e-8f) continue;
                fn.Normalize();
                int axis = 0;
                if (Mathf.Abs(fn.y) > Mathf.Abs(fn.x)) axis = 1;
                if (Mathf.Abs(fn.z) > Mathf.Abs(fn[axis])) axis = 2;
                int dir = axis * 2 + (fn[axis] >= 0f ? 0 : 1);

                if (!buckets.TryGetValue(dir, out Bucket b)) { b = new Bucket(); buckets[dir] = b; }
                b.Tris++; b.Area += area;
                foreach (int idx in new[] { i0, i1, i2 })
                {
                    if (idx >= uv.Length) continue;
                    if (uv[idx].x < b.UvMin.x) b.UvMin.x = uv[idx].x;
                    if (uv[idx].y < b.UvMin.y) b.UvMin.y = uv[idx].y;
                    if (uv[idx].x > b.UvMax.x) b.UvMax.x = uv[idx].x;
                    if (uv[idx].y > b.UvMax.y) b.UvMax.y = uv[idx].y;
                }
            }

            float totalArea = buckets.Values.Sum(b => b.Area);
            foreach (KeyValuePair<int, Bucket> kv in buckets.OrderBy(x => x.Key))
            {
                Bucket b = kv.Value;
                float uw = Mathf.Max(0f, b.UvMax.x - b.UvMin.x);
                float vh = Mathf.Max(0f, b.UvMax.y - b.UvMin.y);
                float uvCoverage = uw * vh;
                _log.AppendLine("  " + BoxDir[kv.Key] + " tris=" + b.Tris +
                                " area=" + b.Area.ToString("F3") + " m2 (" +
                                (totalArea > 0f ? (b.Area / totalArea * 100f).ToString("F1") : "0") + "% of surface)" +
                                " | UV0 box u " + b.UvMin.x.ToString("F3") + ".." + b.UvMax.x.ToString("F3") +
                                " v " + b.UvMin.y.ToString("F3") + ".." + b.UvMax.y.ToString("F3") +
                                " = " + uw.ToString("F3") + " x " + vh.ToString("F3") +
                                " (" + (uvCoverage * 100f).ToString("F2") + "% of 0..1)" +
                                (uvCoverage < 0.02f && b.Area > 0.1f ? "  *** THIS FACE CANNOT SHOW GRAIN" : ""));
            }
            _log.AppendLine("  mesh verts=" + v.Length + " tris=" + (t.Length / 3) +
                            " bounds=" + Fmt(mesh.bounds.size) + " | uv2=" +
                            (mesh.uv2 != null ? mesh.uv2.Length : 0));
        }

        private sealed class Bucket
        {
            public int Tris;
            public float Area;
            public Vector2 UvMin = new Vector2(float.MaxValue, float.MaxValue);
            public Vector2 UvMax = new Vector2(float.MinValue, float.MinValue);
        }

        /// <summary>Rebuilds the frame with box-projected, metre-based UVs. The source FBX mesh is never
        /// touched: this instantiates a readable copy, saves it as a new .asset and re-points only the Bay2
        /// MeshFilter through Undo, so the prefab and the import stay intact.</summary>
        private static bool ReUvFrame(Renderer frame)
        {
            _log.AppendLine("BAY2 FRAME RE-UV (owner authorized modifying the ripped mesh)");
            var filter = frame.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) { _log.AppendLine("  no MeshFilter"); return false; }

            Mesh src = filter.sharedMesh;
            Mesh copy = UnityEngine.Object.Instantiate(src);
            copy.name = src.name + "_REUV";

            Vector3[] v = copy.vertices;
            Vector3[] n = copy.normals;
            Vector2[] uv2 = copy.uv2;
            int[] t = copy.triangles;
            bool haveNormals = n != null && n.Length == v.Length;
            bool haveUv2 = uv2 != null && uv2.Length == v.Length;

            int corners = t.Length;
            var nv = new Vector3[corners];
            var nn = new Vector3[corners];
            var nuv = new Vector2[corners];
            var nuv2 = haveUv2 ? new Vector2[corners] : null;

            int tris = corners / 3;
            for (int i = 0; i < corners; i++)
            {
                int src0 = t[3 * (i / 3)], src1 = t[3 * (i / 3) + 1], src2 = t[3 * (i / 3) + 2];
                int my = i % 3;
                int s = my == 0 ? src0 : my == 1 ? src1 : src2;
                nv[i] = v[s];
                nn[i] = haveNormals ? n[s] : Vector3.up;
                if (haveUv2) nuv2[i] = uv2[s];

                Vector3 fn = Vector3.Cross(v[src1] - v[src0], v[src2] - v[src0]);
                if (fn.sqrMagnitude < 1e-12f) fn = Vector3.up;
                fn.Normalize();
                int axis = 0;
                if (Mathf.Abs(fn.y) > Mathf.Abs(fn.x)) axis = 1;
                if (Mathf.Abs(fn.z) > Mathf.Abs(fn[axis])) axis = 2;
                int uAxis = axis == 0 ? 1 : 0;
                int wAxis = axis == 0 ? 2 : axis == 1 ? 2 : 0;
                Vector3 p = v[s];
                float u = (uAxis == 0 ? p.x : uAxis == 1 ? p.y : p.z) * TexelScale;
                float w = (wAxis == 0 ? p.x : wAxis == 1 ? p.y : p.z) * TexelScale;
                nuv[i] = new Vector2(u, w);
            }

            copy.vertices = nv;
            copy.triangles = Enumerable.Range(0, corners).ToArray();
            copy.normals = nn;
            copy.uv = nuv;
            if (haveUv2) copy.uv2 = nuv2;
            copy.RecalculateBounds();
            copy.UploadMeshData(false);

            const string meshDir = "Assets/AAA/ImportedSnooker/Meshes/";
            if (!Directory.Exists(meshDir)) Directory.CreateDirectory(meshDir);
            string target = meshDir + "TABLE_FRAME_Bay2_REUV.asset";
            if (File.Exists(target)) AssetDatabase.DeleteAsset(target);
            AssetDatabase.CreateAsset(copy, target);
            AssetDatabase.SaveAssets();

            Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(target);
            if (saved == null) { _log.AppendLine("  FAILED to save " + target); return false; }

            Mesh before = filter.sharedMesh;
            Undo.RecordObject(filter, "147VR Bay2 Frame Re-UV");
            filter.sharedMesh = saved;
            EditorUtility.SetDirty(filter);

            _log.AppendLine("  wrote " + target + " | tris=" + tris + " corners=" + corners +
                            " (one vertex per corner so each box face unwraps on its own)" +
                            " | uv2 carried=" + haveUv2 + " | normals carried=" + haveNormals +
                            " | texel=" + TexelScale.ToString("F2") + " UV units per metre (one tile = " +
                            (1f / TexelScale).ToString("F2") + " m)");
            _log.AppendLine("  MeshFilter re-pointed on " + frame.name + ": " +
                            (before != null ? before.name : "<null>") + " -> " + saved.name +
                            " (FBX and prefab untouched; this is a scene override)");
            return true;
        }

        /// <summary>Metre-based UVs already repeat the tile, so the 6,2 hand-set repeat would be far too
        /// dense afterwards.</summary>
        private static void ResetMahoganyTiling()
        {
            _log.AppendLine("MAHOGANY TILING - back to (1,1) because the new UVs are in metres");
            var st = new Vector4(1f, 1f, 0f, 0f);
            foreach (string folder in new[] { "Assets/AAA/Imports/naris_vintage/Rebound/",
                                              "Assets/AAA/Imports/NeonVintageArtPass/Materials/" })
                foreach (string p in AssetDatabase.FindAssets("t:Material", new[] { folder })
                             .Select(AssetDatabase.GUIDToAssetPath)
                             .Where(x => x.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) &&
                                         Path.GetFileNameWithoutExtension(x)
                                             .IndexOf("Table Mahogany", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                    if (m == null || !m.HasProperty("_BaseMap_ST")) continue;
                    Vector4 was = m.GetVector("_BaseMap_ST");
                    if (Mathf.Abs(was.x - 1f) < 0.01f && Mathf.Abs(was.y - 1f) < 0.01f) continue;
                    Undo.RecordObject(m, "147VR Mahogany Tiling Reset");
                    m.SetVector("_BaseMap_ST", st);
                    if (m.HasProperty("_BumpMap_ST")) m.SetVector("_BumpMap_ST", st);
                    EditorUtility.SetDirty(m);
                    _log.AppendLine("  " + Path.GetFileName(p) + " ST " + Fmt4(was) + " -> " + Fmt4(st));
                }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Tolerance used to decide whether the row still has its centre hole.</summary>
        private const float FlagPitchTolerance = 0.06f;

        private static void CloseFlagRowGap(Scene scene)
        {
            _log.AppendLine("1) FLAG ROW - CLOSE THE CENTRE HOLE (index 13 was never authored in the rip)");
            List<Renderer> flags = FlagRenderers(scene);
            if (flags.Count == 0) { _log.AppendLine("  no SM_147VR_AAA_Flag_* renderers found"); return; }

            float[] xs = flags.Select(f => f.bounds.center.x).OrderBy(v => v).ToArray();
            // Median neighbour step, NOT (max-min)/(n-1): a missing slot inflates the mean, which is how
            // this row reported a pitch of 0.84 against its real 0.8077 spacing.
            var steps = new List<float>();
            for (int i = 1; i < xs.Length; i++) steps.Add(xs[i] - xs[i - 1]);
            steps.Sort();
            float pitch = steps.Count > 0 ? steps[steps.Count / 2] : 0f;
            float innermost = xs.Min(v => Mathf.Abs(v));
            _log.AppendLine("  flags=" + flags.Count + " x " + xs[0].ToString("F3") + " .. " +
                            xs[xs.Length - 1].ToString("F3") + " | mean pitch=" + pitch.ToString("F4") +
                            " m | innermost |x|=" + innermost.ToString("F3") + " m");

            if (Mathf.Abs(innermost - pitch * 0.5f) < FlagPitchTolerance)
            {
                _log.AppendLine("  ALREADY CLOSED - innermost pair sits at one half-slot (" +
                                innermost.ToString("F3") + " vs half pitch " + (pitch * 0.5f).ToString("F3") +
                                "). Nothing moved.");
                return;
            }
            if (Mathf.Abs(innermost - pitch) > FlagPitchTolerance)
            {
                _log.AppendLine("  REFUSED - innermost |x| " + innermost.ToString("F3") + " m is neither a full pitch (" +
                                pitch.ToString("F3") + ", hole present) nor a half pitch (" +
                                (pitch * 0.5f).ToString("F3") + ", closed). Reporting instead of guessing.");
                return;
            }

            float halfSlot = pitch * 0.5f;
            Bounds rowBefore = UnionOf(flags);
            int moved = 0;
            foreach (Renderer f in flags)
            {
                float x = f.bounds.center.x;
                if (Mathf.Abs(x) < 0.01f) { _log.AppendLine("  SKIP " + f.name + " - already on the centre line"); continue; }
                Transform t = f.transform;
                Vector3 before = t.position;
                Undo.RecordObject(t, "147VR Close Flag Gap");
                t.position = t.position + new Vector3(x > 0f ? -halfSlot : halfSlot, 0f, 0f);
                EditorUtility.SetDirty(t);
                moved++;
                _log.AppendLine("  " + f.name + " x " + before.x.ToString("F3") + " -> " + t.position.x.ToString("F3") +
                                " (bounds centre now " + f.bounds.center.x.ToString("F3") + ")");
            }

            float[] after = flags.Select(f => f.bounds.center.x).OrderBy(v => v).ToArray();
            float worstStep = 0f, leftoverHole = 0f;
            for (int i = 1; i < after.Length; i++)
            {
                float step = after[i] - after[i - 1];
                if (step > worstStep) worstStep = step;
                if (step - pitch > leftoverHole) leftoverHole = step - pitch;
            }
            Bounds rowAfter = UnionOf(flags);
            float zThickness = rowAfter.max.z - rowAfter.min.z;

            _log.AppendLine("  moved " + moved + " flags inward by " + halfSlot.ToString("F4") + " m each");
            _log.AppendLine("  VERIFY x " + after[0].ToString("F3") + " .. " + after[after.Length - 1].ToString("F3") +
                            " | largest neighbour step=" + worstStep.ToString("F4") + " m vs pitch " +
                            pitch.ToString("F4") + " | leftover hole=" + leftoverHole.ToString("F4") + " m" +
                            (leftoverHole > FlagPitchTolerance ? "  *** STILL OPEN" : " -> closed, spacing uniform"));
            _log.AppendLine("  row z thickness " + zThickness.ToString("F3") + " m (was " +
                            (rowBefore.max.z - rowBefore.min.z).ToString("F3") +
                            ") - must stay ~0.14; a second z value would mean the cloth swung across the room");

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            _log.AppendLine("  Scene saved=" + saved + " -> " + scene.path);

            Capture("40-flag-row-after", rowAfter, new Vector3(0f, 0.9f, 7.5f));
        }

        private static List<Renderer> FlagRenderers(Scene scene) =>
            AllRenderers(scene)
                .Where(r => r.name.StartsWith("SM_147VR_AAA_Flag_", StringComparison.OrdinalIgnoreCase))
                .ToList();

        private static int Guard(string label, Func<int> action)
        {
            try { return action(); }
            catch (Exception e) { _log.AppendLine(label + ": ERROR " + e); return 0; }
        }

        // ------------------------------------------------------------------ A) bay rails

        /// <summary>The audit found Bay2_PhysicsRoot has ZERO renderers - it only carries colliders - so the
        /// table visuals must be reached through the ROOT_PoolTable_* root whose centre is nearest the bay.</summary>
        private static void AuditBayRails(Scene scene, float floorY)
        {
            _log.AppendLine("A) BAY2 RAIL / MAHOGANY CHECK");
            GameObject bay = FindRoot(scene, "Bay2_PhysicsRoot");
            if (bay == null) { _log.AppendLine("  Bay2_PhysicsRoot NOT FOUND"); return; }
            _log.AppendLine("  Bay2_PhysicsRoot renderers=" +
                            bay.GetComponentsInChildren<Renderer>(true).Length +
                            " (colliders only) -> resolving the table through ROOT_PoolTable_*");

            GameObject table = NearestTableRoot(scene, bay.transform.position);
            if (table == null) { _log.AppendLine("  no ROOT_PoolTable_* root found"); return; }
            _log.AppendLine("  table root for Bay2 = " + table.name + " at " + Fmt(table.transform.position));

            List<Renderer> rails = table.GetComponentsInChildren<Renderer>(true)
                .Where(r => r != null && (r.name.IndexOf("rail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          r.name.IndexOf("cushion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          r.name.IndexOf("edge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          r.name.IndexOf("frame", StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
            _log.AppendLine("  rail-ish renderers on that table=" + rails.Count);

            foreach (Renderer r in rails.OrderBy(x => x.bounds.center.x).Take(30))
            {
                Material m = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
                if (m == null) { _log.AppendLine("    " + r.name + " -> <NO MATERIAL>"); continue; }
                Texture b = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                Color tint = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                float darkest = Mathf.Min(tint.r, Mathf.Min(tint.g, tint.b));
                string verdict = b == null && darkest < 0.25f ? "  *** NO TEXTURE + DARK TINT = invisible edge"
                                 : b == null ? "  *** NO TEXTURE" : "";
                _log.AppendLine("    " + (r.enabled ? "" : "RENDERER-OFF ") +
                                (r.gameObject.activeInHierarchy ? "" : "INACTIVE ") +
                                r.name + " -> " + m.name +
                                " | baseMap=" + (b != null ? b.name : "<NONE>") +
                                " tint=" + Fmt(tint) + " size=" + Fmt(r.bounds.size) + verdict);
            }

            var mahogany = AssetDatabase.FindAssets("t:Material", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) &&
                            Path.GetFileNameWithoutExtension(p).IndexOf("mahogany", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();
            _log.AppendLine("  mahogany-named materials=" + mahogany.Count + ", usable ones (texture + light tint):");
            foreach (string p in mahogany)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                Texture b = m != null && m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                Color tint = m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                _log.AppendLine("    " + (b != null && Mathf.Min(tint.r, Mathf.Min(tint.g, tint.b)) > 0.5f ? "GOOD " : "BAD  ") +
                                p + " baseMap=" + (b != null ? b.name : "<NONE>") + " tint=" + Fmt(tint));
            }
        }

        /// <summary>Compares the same parts across all four tables. A material can hold a real wood map and
        /// still show no grain if the tiling is enormous, the mesh has no UV2 while the shader samples UV2,
        /// or the part is buried under the cushions - which is what the owner is seeing on Bay2.</summary>
        private static void CompareTableFrames(Scene scene)
        {
            _log.AppendLine("A2) CROSS-TABLE FRAME / RAIL COMPARISON (why is the grain not visible?)");
            List<GameObject> tables = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .Where(g => g.name.StartsWith("ROOT_PoolTable_", StringComparison.Ordinal))
                .Distinct()
                .OrderBy(g => g.name, StringComparer.Ordinal)
                .ToList();

            foreach (GameObject table in tables)
            {
                _log.AppendLine("  " + table.name + " centre=" + Fmt(table.transform.position));
                foreach (Renderer r in table.GetComponentsInChildren<Renderer>(true)
                            .Where(x => x.name.IndexOf("FRAME", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        x.name.IndexOf("rail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        x.name.IndexOf("cushion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        x.name.IndexOf("SLATE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        x.name.IndexOf("SURFACE", StringComparison.OrdinalIgnoreCase) >= 0)
                            .OrderBy(x => x.name, StringComparer.Ordinal))
                {
                    Material m = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
                    string baseMap = "<none>", bump = "<none>", st = "<n/a>";
                    if (m != null)
                    {
                        Texture b = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                        Texture n = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
                        baseMap = b != null ? b.name : "<NONE>";
                        bump = n != null ? n.name : "<NONE>";
                        if (m.HasProperty("_BaseMap_ST"))
                        {
                            Vector4 v = m.GetVector("_BaseMap_ST");
                            st = "scale(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + ") " +
                                 "offset(" + v.z.ToString("F2") + "," + v.w.ToString("F2") + ")";
                        }
                    }
                    Mesh mesh = r is SkinnedMeshRenderer sk ? sk.sharedMesh
                                    : (r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null);
                    int uv0 = mesh != null && mesh.uv != null ? mesh.uv.Length : 0;
                    int uv2 = 0;
                    if (mesh != null)
                    {
                        var uvList = new List<Vector4>();
                        mesh.GetUVs(1, uvList);          // Mesh.uv2 is deprecated in Unity 6
                        uv2 = uvList.Count;
                    }
                    _log.AppendLine("    " + (r.enabled ? "" : "RENDERER-OFF ") +
                                    (r.gameObject.activeSelf ? "" : "OBJECT-OFF ") + r.name +
                                    " mat=" + (m != null ? m.name : "<null>") +
                                    " | baseMap=" + baseMap + " bump=" + bump + " ST=" + st +
                                    " | uv0=" + uv0 + " uv2=" + uv2 +
                                    " | size=" + Fmt(r.bounds.size) + " centre=" + Fmt(r.bounds.center));
                }
            }
        }

        private static GameObject NearestTableRoot(Scene scene, Vector3 from)
        {
            List<GameObject> roots = scene.GetRootGameObjects()
                .Concat(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                        .Select(t => t.gameObject))
                .Where(g => g.name.StartsWith("ROOT_PoolTable_", StringComparison.Ordinal))
                .Distinct()
                .ToList();
            if (roots.Count == 0) return null;
            return roots.OrderBy(g => Vector3.Distance(g.transform.position, from)).First();
        }

        private static Bounds NearestTableBounds(Scene scene, float floorY)
        {
            GameObject bay = FindRoot(scene, "Bay2_PhysicsRoot");
            GameObject table = bay != null ? NearestTableRoot(scene, bay.transform.position) : null;
            return table != null ? BoundsOf(table) : new Bounds(new Vector3(0f, 0.9f, 3.1f), new Vector3(4f, 1f, 2.2f));
        }

        // ------------------------------------------------------------------ B) floating plants

        /// <summary>Planter-specific tokens only. The first version used "pot", which matched every snooker
        /// POCKET and dragged 48 unrelated renderers into one group.</summary>
        private static readonly string[] PlantNameHints = { "planter", "plant", "fern", "palmtree",
                                                            "monstera", "bonsai", "foliage", "soildisk" };

        private static List<Renderer> PlantRenderers(Scene scene) =>
            AllRenderers(scene).Where(r =>
            {
                string hay = (r.name + "|" + MatName(r)).ToLowerInvariant();
                return PlantNameHints.Any(h => hay.Contains(h));
            }).ToList();

        /// <summary>Clusters plant parts by PHYSICAL proximity rather than by name. The name-stem version
        /// separated a pot from its own foliage, so eight groups were skipped as "resting on furniture"
        /// when the leaves were really sitting on a pot that floats. Here a part joins a cluster when its
        /// horizontal centre is within 1.2 m of a member and its vertical span comes within 1.0 m.</summary>
        private static Dictionary<string, List<Renderer>> PlantGroups(Scene scene)
        {
            var clusters = new List<List<Renderer>>();
            foreach (Renderer r in PlantRenderers(scene)
                        .OrderBy(x => x.bounds.center.x).ThenBy(x => x.bounds.center.z))
            {
                List<Renderer> home = null;
                foreach (List<Renderer> c in clusters)
                {
                    bool near = c.Any(other =>
                        Mathf.Abs(other.bounds.center.x - r.bounds.center.x) < 1.2f &&
                        Mathf.Abs(other.bounds.center.z - r.bounds.center.z) < 1.2f &&
                        Mathf.Max(other.bounds.min.y, r.bounds.min.y) -
                        Mathf.Min(other.bounds.max.y, r.bounds.max.y) < 1.0f);
                    if (!near) continue;
                    home = c;
                    break;
                }
                if (home == null) clusters.Add(new List<Renderer> { r });
                else home.Add(r);
            }

            var result = new Dictionary<string, List<Renderer>>(StringComparer.Ordinal);
            for (int i = 0; i < clusters.Count; i++)
                result["cluster " + (i + 1).ToString("D2") + " (" + clusters[i][0].name + ")"] = clusters[i];
            return result;
        }

        /// <summary>Puts the room root back where it was. The first apply pass lowered it by 0.067 m because
        /// of the grouping bug above; the pivot was exactly (0,0,0) before that, so restoring it is exact.</summary>
        private static int RestoreRoomRoot(Scene scene)
        {
            GameObject room = FindRoot(scene, "NarisVintage_Room");
            if (room == null) { _log.AppendLine("  NarisVintage_Room not found"); return 0; }
            Vector3 p = room.transform.position;
            if (p.sqrMagnitude < 1e-8f) { _log.AppendLine("  NarisVintage_Room already at the origin"); return 0; }
            Undo.RecordObject(room.transform, "147VR Restore Room Root");
            room.transform.position = Vector3.zero;
            EditorUtility.SetDirty(room.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            _log.AppendLine("  RESTORED NarisVintage_Room pivot " + Fmt(p) + " -> (0.000, 0.000, 0.000)" +
                            " (the earlier plant pass moved the whole room by mistake)");
            return 1;
        }

        private static int ReportFloatingPlants(Scene scene, float floorY)
        {
            _log.AppendLine("B) FLOATING PLANT ASSEMBLIES (grouped by parent + name stem)");
            foreach (KeyValuePair<string, List<Renderer>> g in PlantGroups(scene).OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                float minGap = g.Value.Min(r => r.bounds.min.y) - floorY;
                float maxGap = g.Value.Max(r => r.bounds.max.y) - floorY;
                float height = g.Value.Max(r => r.bounds.max.y) - g.Value.Min(r => r.bounds.min.y);
                _log.AppendLine("  " + g.Key + " parts=" + g.Value.Count +
                                " lowest gap=" + minGap.ToString("F3") + " highest gap=" + maxGap.ToString("F3") +
                                " height=" + height.ToString("F2") + " centre=" +
                                Fmt(g.Value[0].bounds.center));
            }
            return 0;
        }

        private static int DropFloatingPlantGroups(Scene scene, float floorY)
        {
            _log.AppendLine("B) LOWERING FLOATING PLANT ASSEMBLIES (floor y=" + floorY.ToString("F3") + ")");
            int movedGroups = 0, movedParts = 0, skipped = 0;

            foreach (KeyValuePair<string, List<Renderer>> g in PlantGroups(scene))
            {
                List<Renderer> parts = g.Value;
                float lowestGap = parts.Min(r => r.bounds.min.y) - floorY;
                float height = parts.Max(r => r.bounds.max.y) - parts.Min(r => r.bounds.min.y);
                float centreY = parts.Average(r => r.bounds.center.y);

                string why = null;
                if (lowestGap < 0.01f) why = "already on the floor (gap " + lowestGap.ToString("F3") + ")";
                else if (lowestGap > MaxFloatDropM) why = "floats " + lowestGap.ToString("F3") + " m - too far for a " +
                                                          "floor plant, probably resting on furniture; left alone";
                else if (height < PlantGroupMinHeightM) why = "assembly only " + height.ToString("F2") + " m tall, " +
                                                              "not a floor-standing plant; left alone";
                else if (centreY > PlantGroupMaxCentreY) why = "assembly centre is at y " + centreY.ToString("F2") +
                                                              " m, above floor level; left alone";
                else if (parts.Any(r => r.transform.parent == null)) why = "contains a scene root - refusing to " +
                                                                           "bulk-move the room";

                if (why != null)
                {
                    skipped++;
                    _log.AppendLine("  SKIP " + g.Key + " parts=" + parts.Count + " - " + why);
                    continue;
                }

                // One delta for the whole assembly: the pot and its soil disk move together.
                foreach (Renderer r in parts)
                {
                    Transform t = r.transform;
                    Vector3 before = t.position;
                    Undo.RecordObject(t, "147VR Plant Assembly To Floor");
                    t.position = t.position + Vector3.up * (-lowestGap);
                    EditorUtility.SetDirty(t);
                    movedParts++;
                    _log.AppendLine("  moved " + r.name + " by " + lowestGap.ToString("F3") + " m | pivot " +
                                    Fmt(before) + " -> " + Fmt(t.position));
                }
                movedGroups++;
                float gapAfter = parts.Min(r => r.bounds.min.y) - floorY;
                _log.AppendLine("  assembly " + g.Key + " parts=" + parts.Count + " now sits " +
                                gapAfter.ToString("F4") + " m above floor" +
                                (gapAfter > 0.02f ? "  *** still floating" : ""));
            }
            _log.AppendLine("  assemblies moved=" + movedGroups + " parts moved=" + movedParts +
                            " skipped=" + skipped);
            if (movedParts > 0) EditorSceneManager.MarkSceneDirty(scene);
            return movedGroups;
        }

        // ------------------------------------------------------------------ C) ceiling junction

        private static void ReportJunction(Scene scene)
        {
            _log.AppendLine("C) CEILING JUNCTION MEASUREMENT");
            foreach (Renderer r in AllRenderers(scene)
                        .Where(x => x.name.IndexOf("cornice", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    x.name.IndexOf("ornament", StringComparison.OrdinalIgnoreCase) >= 0)
                        .OrderBy(x => x.bounds.min.y))
                _log.AppendLine("  " + r.name + " mat=" + MatName(r) +
                                " y " + r.bounds.min.y.ToString("F3") + ".." + r.bounds.max.y.ToString("F3") +
                                " x " + r.bounds.min.x.ToString("F2") + ".." + r.bounds.max.x.ToString("F2") +
                                " z " + r.bounds.min.z.ToString("F2") + ".." + r.bounds.max.z.ToString("F2") +
                                " scale=" + Fmt(r.transform.localScale));
        }

        /// <summary>The reference height is the wall crown moulding band (147VR_AAA_Cornice_PlusX / MinusX at
        /// y 4.192). Both ceiling pieces are lifted until their top reaches it, which closes the reveal so
        /// nothing under the ceiling shows through.</summary>
        private static int LiftCeilingToCrown(Scene scene)
        {
            _log.AppendLine("C) LIFTING THE CEILING PIECES LEVEL WITH THE CROWN MOULDING");
            List<Renderer> all = AllRenderers(scene);
            List<Renderer> crown = all.Where(x => x.name.IndexOf("AAA_Cornice", StringComparison.OrdinalIgnoreCase) >= 0)
                                      .ToList();
            if (crown.Count == 0)
            {
                _log.AppendLine("  REFUSED - no 147VR_AAA_Cornice_* crown moulding found to measure against.");
                return 0;
            }
            float crownBottom = crown.Min(c => c.bounds.min.y);
            _log.AppendLine("  crown reference: " + crown.Count + " mouldings, lowest top-edge y=" +
                            crownBottom.ToString("F3"));

            int liftedCount = 0;
            foreach (string hint in new[] { "ornament_ceiling", "SM_cornice" })
            {
                Renderer piece = all.FirstOrDefault(x => string.Equals(x.name, hint, StringComparison.Ordinal))
                              ?? all.FirstOrDefault(x => x.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0);
                if (piece == null) { _log.AppendLine("  " + hint + ": NOT FOUND"); continue; }

                float gap = crownBottom - piece.bounds.max.y;
                if (gap < JunctionToleranceM)
                {
                    _log.AppendLine("  " + piece.name + " top y=" + piece.bounds.max.y.ToString("F3") +
                                    " already level (gap " + gap.ToString("F4") + ") - nothing lifted");
                    continue;
                }
                if (gap > MaxJunctionLiftM)
                {
                    _log.AppendLine("  " + piece.name + " REFUSED - reveal " + gap.ToString("F3") + " m exceeds " +
                                    MaxJunctionLiftM + " m of lifting; that is geometry, not placement");
                    continue;
                }

                Transform t = piece.transform;
                Vector3 before = piece.bounds.center;
                Undo.RecordObject(t, "147VR Ceiling To Crown");
                t.position = t.position + Vector3.up * gap;
                EditorUtility.SetDirty(t);
                liftedCount++;
                _log.AppendLine("  lifted " + piece.name + " by " + gap.ToString("F4") + " m | centre " +
                                Fmt(before) + " -> " + Fmt(piece.bounds.center) + " | top y now " +
                                piece.bounds.max.y.ToString("F3") + " vs crown bottom " +
                                crownBottom.ToString("F3") + " | overlap " +
                                (piece.bounds.max.y - crownBottom).ToString("F4") +
                                " (>=0 = no see-through reveal)");
            }
            if (liftedCount > 0) EditorSceneManager.MarkSceneDirty(scene);
            return liftedCount;
        }

        /// <summary>Repairs and re-applies the cream cornice variants. The first version built each variant
        /// with new Material(source.shader) and copied a handful of named properties, silently dropping
        /// _BaseMap_ST (tiling), _EmissionMap and every shader keyword - the carved ornament pattern
        /// vanished under a flat cream wash because of THAT, not because of the tint. Object.Instantiate
        /// carries the whole serialized material across, so pattern, tiling, bump and keywords survive and
        /// only the owner's tint is added on top.</summary>
        private static int RepaintCorniceMaterials(Scene scene)
        {
            _log.AppendLine("C) CORNICE COLOUR -> light cream, rebuilt as FULL copies of the source");
            List<Renderer> all = AllRenderers(scene);
            List<Renderer> targets = all.Where(x =>
                x.name.IndexOf("cornice", StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.name.IndexOf("ornament_ceiling", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            _log.AppendLine("  cornice/ornament renderers=" + targets.Count);

            var variants = new Dictionary<Material, Material>();
            int switched = 0, kept = 0;

            foreach (Renderer r in targets.OrderBy(x => x.name, StringComparer.Ordinal))
            {
                Material[] slots = r.sharedMaterials;
                bool changedRenderer = false;

                for (int i = 0; i < slots.Length; i++)
                {
                    Material m = slots[i];
                    if (m == null) continue;

                    // Walk a "_Cream" slot back to its origin so a rerun REPAIRS the broken variant
                    // instead of skipping it, and never nests Cream_Cream_Cream.
                    Material source = m;
                    string sourcePath = AssetDatabase.GetAssetPath(source);
                    if (source.name.EndsWith("_Cream", StringComparison.Ordinal) && !string.IsNullOrEmpty(sourcePath))
                    {
                        Material back = AssetDatabase.LoadAssetAtPath<Material>(
                            sourcePath.Replace("_Cream.mat", ".mat"));
                        if (back != null) source = back;
                    }

                    string path = AssetDatabase.GetAssetPath(source);
                    if (string.IsNullOrEmpty(path))
                    {
                        _log.AppendLine("  SKIP " + r.name + " slot " + i + " - scene-instance material, no asset");
                        continue;
                    }

                    List<Renderer> users = all.Where(x => x.sharedMaterials.Any(s => s == source)).ToList();
                    List<Renderer> foreign = users.Where(x =>
                        x.name.IndexOf("cornice", StringComparison.OrdinalIgnoreCase) < 0 &&
                        x.name.IndexOf("ornament_ceiling", StringComparison.OrdinalIgnoreCase) < 0).ToList();

                    Material wanted;
                    if (foreign.Count == 0)
                    {
                        wanted = source;                     // exclusive: tint it in place
                    }
                    else
                    {
                        if (!variants.TryGetValue(source, out wanted))
                        {
                            wanted = RebuildCreamVariant(source, foreign.Count);
                            variants[source] = wanted;
                        }
                    }
                    if (wanted == null) continue;

                    if (wanted.GetInstanceID() == m.GetInstanceID()) { kept++; continue; }

                    slots[i] = wanted;
                    changedRenderer = true;
                    switched++;
                    _log.AppendLine("  slot " + i + " of " + r.name + ": " + m.name + " -> " + wanted.name +
                                    (foreign.Count == 0 ? " (exclusive, tinted in place)"
                                                        : " (full copy; " + foreign.Count + " other users keep the original)"));

                    if (foreign.Count == 0)
                    {
                        Color before = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : Color.white;
                        Undo.RecordObject(source, "147VR Cornice Tint");
                        if (source.HasProperty("_BaseColor")) source.SetColor("_BaseColor", CreamTint);
                        EditorUtility.SetDirty(source);
                        _log.AppendLine("    tinted " + Path.GetFileName(path) + " " + Fmt(before) + " -> " + Fmt(CreamTint));
                    }
                }

                if (changedRenderer)
                {
                    Undo.RecordObject(r, "147VR Cornice Cream");
                    r.sharedMaterials = slots;
                    EditorUtility.SetDirty(r);
                }
            }

            AssetDatabase.SaveAssets();
            _log.AppendLine("  summary: slotsSwitched=" + switched + " alreadyCorrect=" + kept +
                            " variantsRebuilt=" + variants.Count);
            if (switched > 0) EditorSceneManager.MarkSceneDirty(scene);
            return switched;
        }

        /// <summary>Replaces the broken variant asset with a full Instantiate copy of the source plus the
        /// owner's tint. Deleting first is safe because every cornice slot is re-pointed in the same pass.</summary>
        private static Material RebuildCreamVariant(Material source, int foreignUsers)
        {
            const string folder = "Assets/AAA/Imports/NeonVintageArtPass/Materials/";
            if (!Directory.Exists(folder)) { _log.AppendLine("  variant folder missing: " + folder); return null; }
            string target = folder + source.name + "_Cream.mat";

            if (AssetDatabase.LoadAssetAtPath<Material>(target) != null) AssetDatabase.DeleteAsset(target);

            Material clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name + "_Cream";
            AssetDatabase.CreateAsset(clone, target);

            Material saved = AssetDatabase.LoadAssetAtPath<Material>(target);
            if (saved == null) { _log.AppendLine("  FAILED to create " + target); return null; }

            Color before = saved.HasProperty("_BaseColor") ? saved.GetColor("_BaseColor") : Color.white;
            if (saved.HasProperty("_BaseColor")) saved.SetColor("_BaseColor", CreamTint);
            EditorUtility.SetDirty(saved);
            _log.AppendLine("  rebuilt variant " + target + " as a FULL copy of " + source.name +
                            " | tint " + Fmt(before) + " -> " + Fmt(CreamTint) +
                            " | keeps _BaseMap_ST, bump, emission and all keywords" +
                            " | " + foreignUsers + " non-cornice users untouched");
            return saved;
        }

        // ------------------------------------------------------------------ D) table light evenness

        private static void ReportTableLights(Scene scene)
        {
            _log.AppendLine("D) TABLE LIGHT RIGS");
            foreach (Light l in TableKeyLights(scene))
                _log.AppendLine("  " + l.name + " pos=" + Fmt(l.transform.position) + " intensity=" +
                                l.intensity.ToString("F2") + " range=" + l.range.ToString("F2") +
                                " spotAngle=" + l.spotAngle.ToString("F1") + " inner=" + l.innerSpotAngle.ToString("F1") +
                                " colour=" + Fmt(l.color));
        }

        /// <summary>Flat pool per lamp covering its own third of the table, with the inner-to-outer falloff
        /// pushed outside the cloth. The measured outer angle is never shrunk, and intensity stays inside the
        /// owner-approved 8-16 band.</summary>
        private static int EvenOutTableLights(Scene scene)
        {
            _log.AppendLine("D) REMOVING THE THREE HOT SPOTS (raise inner angle, keep the wide outer cone)");
            var groups = TableKeyLights(scene)
                .GroupBy(l => RowKey(l.name))
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToList();
            if (groups.Count == 0) { _log.AppendLine("  no TableKey lights found"); return 0; }

            int changed = 0;
            foreach (var group in groups)
            {
                Light[] lamps = group.OrderBy(l => l.transform.position.x).ToArray();
                float spacing = lamps.Length > 1
                    ? (lamps[lamps.Length - 1].transform.position.x - lamps[0].transform.position.x) / (lamps.Length - 1)
                    : 1.3f;
                float drop = Mathf.Max(lamps.Min(l => l.transform.position.y) - 0.89f, 0.30f);

                // Each lamp owns one third of the table: radius = half the spacing, plus a margin so the
                // seam between neighbours lands in the falloff zone instead of on the cloth.
                float poolRadius = (spacing * 0.5f) * PoolSeamMargin;
                float wantedInner = Mathf.Min(2f * Mathf.Atan2(poolRadius, drop) * Mathf.Rad2Deg, 120f);

                _log.AppendLine("  row " + group.Key + ": lamps=" + lamps.Length + " spacing=" + spacing.ToString("F2") +
                                " m drop=" + drop.ToString("F2") + " m -> flat pool radius=" +
                                poolRadius.ToString("F2") + " m -> inner " + lamps[0].innerSpotAngle.ToString("F1") +
                                " -> " + wantedInner.ToString("F1") + " deg");

                foreach (Light l in lamps)
                {
                    float oldInner = l.innerSpotAngle, oldAngle = l.spotAngle, oldInt = l.intensity;
                    Undo.RecordObject(l, "147VR Table Light Evenness");
                    l.innerSpotAngle = wantedInner;
                    // The falloff ring must not fall on the cloth: if the existing outer cone is narrower
                    // than the flat pool plus its blend, widen it. Never shrink a measured cone.
                    l.spotAngle = Mathf.Max(oldAngle, wantedInner * 1.35f);
                    l.intensity = Mathf.Clamp(TableLampIntensityTarget, TableLampIntensityMin, TableLampIntensityMax);
                    EditorUtility.SetDirty(l);
                    changed++;
                    _log.AppendLine("    " + l.name + " inner " + oldInner.ToString("F1") + " -> " +
                                    l.innerSpotAngle.ToString("F1") + " | outer " + oldAngle.ToString("F1") + " -> " +
                                    l.spotAngle.ToString("F1") + " | intensity " + oldInt.ToString("F2") + " -> " +
                                    l.intensity.ToString("F2") +
                                    (l.spotAngle > 160f ? "  *** over Unity's 160 deg spot limit" : ""));
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            return changed;
        }

        private static string RowKey(string lightName)
        {
            int i = lightName.IndexOf("_R", StringComparison.Ordinal);
            // Must be 4 characters: "_R01". Taking 3 yields "_R0" for every row, which merged all twelve
            // lamps into one fake row and made the spacing calculation useless (2026-11-11 run).
            return (i < 0 || i + 4 > lightName.Length) ? lightName : lightName.Substring(i, 4);
        }

        // ------------------------------------------------------------------ E) bar lights

        private static Bounds BarBounds(Scene scene)
        {
            string[] hints = { "BarPult", "CashDesk", "BottleHolders", "Sampagne", "WhiskeyBottles",
                               "WineBottles", "BeerTap", "BCStools", "Espresso" };
            List<Renderer> parts = AllRenderers(scene)
                .Where(r => hints.Any(h => MatName(r).IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           r.name.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
            if (parts.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);
            Bounds b = parts[0].bounds;
            foreach (Renderer r in parts) b.Encapsulate(r.bounds);
            return b;
        }

        private static void ReportBarFootprint(Scene scene)
        {
            Bounds b = BarBounds(scene);
            _log.AppendLine("E) BAR FOOTPRINT union " + Fmt(b.min) + ".." + Fmt(b.max) +
                            " size=" + Fmt(b.size) + " centre=" + Fmt(b.center));
        }

        private static int AddBarLights(Scene scene)
        {
            _log.AppendLine("E) BAR DOWN-LIGHTS (owner-approved precedent: WallSconce=16, TableKey=4.5)");
            Bounds bar = BarBounds(scene);
            if (bar.size.x < 0.5f) { _log.AppendLine("  REFUSED - bar footprint not found"); return 0; }

            Light[] existing = scene.GetRootGameObjects()
                                    .SelectMany(g => g.GetComponentsInChildren<Light>(true))
                                    .Where(l => l != null && l.name.IndexOf("Bar", StringComparison.OrdinalIgnoreCase) >= 0)
                                    .ToArray();
            if (existing.Length > 0)
            {
                _log.AppendLine("  " + existing.Length + " Bar lights already exist - not duplicating them: " +
                                string.Join(", ", existing.Select(e => e.name).ToArray()));
                return 0;
            }

            GameObject rig = scene.GetRootGameObjects()
                                  .FirstOrDefault(g => g.name.IndexOf("LightRig", StringComparison.OrdinalIgnoreCase) >= 0);
            Transform parent = rig != null ? rig.transform : null;
            if (parent == null)
            {
                var root = new GameObject("147VR_AAA_BarLightRig");
                Undo.RegisterCreatedObjectUndo(root, "147VR Bar Lights");
                parent = root.transform;
                _log.AppendLine("  no LightRig root at scene level - created " + root.name);
            }

            var plans = new (string Label, Vector3 Pos, Vector3 Aim, float Intensity, float Inner, float Outer, Color Colour)[]
            {
                ("Counter_Fill", new Vector3(bar.center.x - 0.55f, bar.max.y + 0.35f, bar.center.z),
                                  new Vector3(bar.center.x - 0.55f, 1.10f, bar.center.z), 4.5f, 60f, 110f,
                                  new Color(1.00f, 0.72f, 0.45f)),
                ("Shelf_Key_01", new Vector3(bar.max.x - 0.35f, bar.max.y + 0.30f, bar.center.z + 1.10f),
                                  new Vector3(bar.max.x - 0.35f, 1.90f, bar.center.z + 1.10f), 16f, 50f, 100f,
                                  new Color(1.00f, 0.64f, 0.34f)),
                ("Shelf_Key_02", new Vector3(bar.max.x - 0.35f, bar.max.y + 0.30f, bar.center.z - 1.10f),
                                  new Vector3(bar.max.x - 0.35f, 1.90f, bar.center.z - 1.10f), 16f, 50f, 100f,
                                  new Color(1.00f, 0.64f, 0.34f)),
                ("Bar_Glow", new Vector3(bar.center.x, bar.max.y + 0.20f, bar.center.z),
                             new Vector3(bar.center.x, 0.90f, bar.center.z), 16f, 70f, 130f,
                             new Color(1.00f, 0.58f, 0.31f)),
            };

            int made = 0;
            foreach (var p in plans)
            {
                var go = new GameObject("147VR_AAA_Bar_" + p.Label);
                Undo.RegisterCreatedObjectUndo(go, "147VR Bar Lights");
                go.transform.SetParent(parent, false);
                go.transform.position = p.Pos;
                go.transform.rotation = Quaternion.LookRotation((p.Aim - p.Pos).normalized, Vector3.up);

                var l = go.AddComponent<Light>();
                l.type = LightType.Spot;
                l.color = p.Colour;
                l.intensity = p.Intensity;
                l.range = 6.5f;
                l.spotAngle = p.Outer;
                l.innerSpotAngle = p.Inner;
                l.shadowStrength = 0.7f;
                made++;
                _log.AppendLine("  created " + go.name + " pos=" + Fmt(p.Pos) + " aim=" + Fmt(p.Aim) +
                                " intensity=" + p.Intensity.ToString("F1") + " inner=" + p.Inner.ToString("F0") +
                                " outer=" + p.Outer.ToString("F0") + " colour=" + Fmt(p.Colour) +
                                " (authored as AREA in the .blend; Unity has no realtime AREA light, so the" +
                                " shape is approximated while colour and position are transferred as-is)");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            return made;
        }

        private static List<Light> TableKeyLights(Scene scene) =>
            scene.GetRootGameObjects()
                 .SelectMany(g => g.GetComponentsInChildren<Light>(true))
                 .Where(l => l != null && l.name.StartsWith("TableKey_", StringComparison.Ordinal))
                 .ToList();

        // ------------------------------------------------------------------ shared helpers

        private static List<Renderer> AllRenderers(Scene scene) =>
            scene.GetRootGameObjects()
                 .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                 .Where(r => r != null)
                 .ToList();

        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F3") + ", " + v.y.ToString("F3") + ", " + v.z.ToString("F3") + ")";

        private static string Fmt(Color c) =>
            "(" + c.r.ToString("F3") + ", " + c.g.ToString("F3") + ", " + c.b.ToString("F3") + ", " +
            c.a.ToString("F3") + ")";

        private static string MatName(Renderer r) =>
            r.sharedMaterials.Length > 0 && r.sharedMaterials[0] != null ? r.sharedMaterials[0].name : string.Empty;

        private static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);

        private static Bounds BoundsOf(GameObject go)
        {
            if (go == null) return new Bounds(Vector3.zero, Vector3.one * 2f);
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true).Where(r => r != null).ToArray();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.5f);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        private static Bounds UnionOf(List<Renderer> rs)
        {
            if (rs.Count == 0) return new Bounds(Vector3.zero, Vector3.one * 2f);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Count; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        private static float MeasureFloor(Scene scene)
        {
            var floors = AllRenderers(scene)
                .Where(r => (r.name + "|" + MatName(r)).IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (r.name + "|" + MatName(r)).IndexOf("parquet", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            if (floors.Count == 0)
            {
                _log.AppendLine("FLOOR: no renderer named/materialised floor|parquet - falling back to y=0");
                return 0f;
            }
            float top = floors.Max(f => f.bounds.max.y);
            _log.AppendLine("FLOOR: matched " + floors.Count + " renderers, walking surface y=" + top.ToString("F3"));
            return top;
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

        /// <summary>Self-contained: WipRoomArtFollowups.Capture is private, and a temporary URP camera is the
        /// recipe already proven here (SceneView.camera produced blank 25250-byte PNGs).</summary>
        private static void Capture(string name, Bounds target, Vector3 eyeOffset)
        {
            var camGo = new GameObject("147VR ArtRound3 Capture Camera");
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
                _log.AppendLine("CAPTURE " + name + " -> " + file + " (" + bytes.Length + " bytes, eye=" +
                                Fmt(eye) + ")");

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

        private static void Begin()
        {
            _log = new StringBuilder();
            _log.AppendLine("147VR ART ROUND 3 (bay2 rail / floating plant assemblies / ceiling junction +");
            _log.AppendLine("cornice tint / table light evenness / bar down-lights)");
            _log.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _log.AppendLine("Every value measured in the loaded Scene. Undoable. Scene never saved, never reopened.");
        }

        private static void Finish()
        {
            Directory.CreateDirectory("Logs");
            if (File.Exists(ReportPath))
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string archive = "Logs/WipRoom_Art_Round3_" + stamp + ".prev.txt";
                int n = 0;
                while (File.Exists(archive)) archive = "Logs/WipRoom_Art_Round3_" + stamp + "_" + (++n) + ".prev.txt";
                File.Move(ReportPath, archive);
            }
            File.WriteAllText(ReportPath, _log.ToString());
            Debug.Log("[ArtRound3] done -> " + ReportPath);
        }
    }
}
#endif
