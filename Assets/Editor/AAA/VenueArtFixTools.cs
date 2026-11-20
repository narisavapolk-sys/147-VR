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
    /// Owner-clicked venue art fixes for the welcome curtain, ceiling haze volume,
    /// cornice/frieze pattern and statue uplights.
    ///
    ///  - Menu 5) Diagnose: READ ONLY. Reports the real current material colours,
    ///    renderer bounds/scales and light parameters for the four targets.
    ///  - Menu 6) Apply to Preview: clones WipRoom into a NEW preview Scene, applies
    ///    the four fixes there, writes a before/after report, never touches production.
    ///
    /// Geometry, colliders and rigidbodies are treated as invariants (must not change).
    /// Lights may change on purpose (statue uplights) and that delta is reported.
    /// </summary>
    public static class VenueArtFixTools
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string PreviewSceneStem = "Assets/Scenes/147VR_MainScene_WipRoom_VenueArtFix";
        private const string ArtRoot = "Assets/AAA/Imports/NeonVintageArtPass";
        private const string MatDir = ArtRoot + "/Materials";
        private const string TexDir = ArtRoot + "/textures";

        // ---- Tunable targets (locked after reading the Diagnose report) ----
        // Curtain welcome -> burgundy velvet material.
        private const string CurtainVelvetMat = MatDir + "/AAA_Curtain_Velvet_Burgundy.mat";
        // Fallback velvet if the primary is missing.
        private const string CurtainVelvetMatAlt = MatDir + "/147 VR Burgundy Red Velvet _ Sketchfab Curtains.mat";

        // Cornice / frieze -> cream plaster with the gold frieze pattern + damask relief.
        private static readonly Color CorniceBaseColor = new Color(1.00f, 0.96f, 0.88f, 1f);
        private const float CorniceSmoothness = 0.45f;
        private const float CorniceMetallic = 0.05f;
        private const string FriezeTex = TexDir + "/frieze_gold.png";
        private const string DamaskNormalTex = TexDir + "/147VR_damask_normal.png";

        // Haze volume -> grow to balance the ceiling pin diffuser.
        private const float HazeWidthRatio = 2.2f;   // haze horizontal size / diffuser horizontal size
        private const float HazeDownwardLength = 2.6f; // metres the shaft should fall from the pin

        // Statue uplight -> warm floor spot aimed up at the statue.
        private static readonly Color UplightColor = new Color(1.00f, 0.72f, 0.45f, 1f); // ~3000K
        private const float UplightIntensity = 6f;
        private const float UplightSpotAngle = 42f;
        private const float UplightRangeFactor = 2.6f; // range = statueHeight * factor

        // ---- Name tokens (case-insensitive substring match on the loaded scene) ----
        private static readonly string[] CurtainTokens = { "welcome", "ผ้าม่าน", "defaultmaterial029" };
        private static readonly string[] HazeTokens = { "haze_volume", "haze" };
        private static readonly string[] DiffuserTokens = { "ceilingpin", "diffuser" };
        private static readonly string[] CorniceTokens = { "cornice", "frieze" };
        private static readonly string[] StatueTokens = { "statue", "bronze_ray" };
        private static readonly string[] UplightTokens = { "statueuplig", "uplight" };

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/5) Venue Art Fix - Diagnose (report only, no changes)")]
        public static void Diagnose()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!IsWipRoom(scene))
            {
                EditorUtility.DisplayDialog("147VR Venue Art", "Open the actual game Scene first:\n" + ScenePath +
                    "\n(use 147VR/AAA/NeonVintage/WipRoom/0) Open Actual Game Scene)", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("147VR VENUE ART DIAGNOSE  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Scene: " + scene.path);
            sb.AppendLine();

            var allRenderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray();
            var allLights = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).ToArray();
            sb.AppendLine("Scene renderers: " + allRenderers.Length + " | lights: " + allLights.Length);
            sb.AppendLine();

            sb.AppendLine("== 1) WELCOME CURTAIN (expect burgundy velvet red) ==");
            foreach (Renderer r in allRenderers.Where(x => Matches(x.name, CurtainTokens) || HasMaterialToken(x, CurtainTokens)))
                DescribeRenderer(sb, r);
            if (!allRenderers.Any(x => Matches(x.name, CurtainTokens) || HasMaterialToken(x, CurtainTokens)))
                sb.AppendLine("  NOT FOUND by tokens " + string.Join(",", CurtainTokens));
            sb.AppendLine();

            sb.AppendLine("== 2) HAZE VOLUME vs CEILING PIN DIFFUSER ==");
            foreach (Renderer r in allRenderers.Where(x => Matches(x.name, HazeTokens)))
                DescribeBounds(sb, "HAZE", r);
            foreach (Renderer r in allRenderers.Where(x => Matches(x.name, DiffuserTokens)))
                DescribeBounds(sb, "DIFFUSER", r);
            sb.AppendLine();

            sb.AppendLine("== 3) CORNICE / FRIEZE (pattern visibility) ==");
            foreach (Renderer r in allRenderers.Where(x => Matches(x.name, CorniceTokens)))
                DescribeRenderer(sb, r);
            if (!allRenderers.Any(x => Matches(x.name, CorniceTokens)))
                sb.AppendLine("  NOT FOUND by tokens " + string.Join(",", CorniceTokens));
            sb.AppendLine();

            sb.AppendLine("== 4) STATUE + UPLIGHTS ==");
            foreach (Renderer r in allRenderers.Where(x => Matches(x.name, StatueTokens)))
                DescribeBounds(sb, "STATUE", r);
            foreach (Light l in allLights.Where(x => Matches(x.name, UplightTokens)))
                DescribeLight(sb, l);
            if (!allLights.Any(x => Matches(x.name, UplightTokens)))
                sb.AppendLine("  No light named like an uplight found -> menu 6 will CREATE them.");

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/VenueArt_Diagnose.txt", sb.ToString());
            Debug.Log("[VenueArtFix]\n" + sb);
            EditorUtility.DisplayDialog("147VR Venue Art",
                "Diagnose written to Logs/VenueArt_Diagnose.txt.\nNo Scene changes were made.\n" +
                "Send me that report so I can lock the exact targets before running menu 6.", "OK");
        }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/6) Venue Art Fix - Apply to Preview Copy (non-destructive)")]
        public static void ApplyToPreview()
        {
            Scene source = SceneManager.GetActiveScene();
            if (!IsWipRoom(source))
            {
                EditorUtility.DisplayDialog("147VR Venue Art", "Open the actual game Scene first:\n" + ScenePath, "OK");
                return;
            }

            string previewPath = GetUnusedPreviewPath();
            string backupRoot = System.IO.Path.Combine(@"C:\Users\mongo\147VR_Backups",
                "VenueArtFix_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(backupRoot);
            BackupIfExists(ScenePath, backupRoot);
            BackupIfExists(ScenePath + ".meta", backupRoot);

            if (!EditorSceneManager.SaveScene(source, previewPath, true))
            {
                EditorUtility.DisplayDialog("147VR Venue Art", "Could not create the preview Scene. Production was NOT saved.", "OK");
                return;
            }

            Scene preview = EditorSceneManager.OpenScene(previewPath, OpenSceneMode.Additive);
            if (!preview.IsValid() || !preview.isLoaded)
            {
                EditorUtility.DisplayDialog("147VR Venue Art", "Preview written but could not reopen:\n" + previewPath, "OK");
                return;
            }
            SceneManager.SetActiveScene(preview);
            EditorSceneManager.CloseScene(source, true);

            int collidersBefore = Count<Collider>(preview);
            int bodiesBefore = Count<Rigidbody>(preview);
            int lightsBefore = Count<Light>(preview);

            var matChanges = new List<Tuple<Renderer, Material[]>>();
            var transformChanges = new List<Tuple<Transform, Vector3>>();
            var createdLights = new List<GameObject>();
            var report = new StringBuilder();
            report.AppendLine("147VR VENUE ART FIX (PREVIEW)  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine("Preview Scene: " + previewPath);
            report.AppendLine("Source backup: " + backupRoot);

            try
            {
                FixCurtain(preview, matChanges, report);
                FixCornice(preview, matChanges, report);
                FixHaze(preview, transformChanges, report);
                FixStatueUplights(preview, createdLights, report);

                int collidersAfter = Count<Collider>(preview);
                int bodiesAfter = Count<Rigidbody>(preview);
                int lightsAfter = Count<Light>(preview);
                if (collidersBefore != collidersAfter || bodiesBefore != bodiesAfter)
                    throw new InvalidOperationException("Collider/Rigidbody invariant changed; rolling back. " +
                        collidersBefore + "->" + collidersAfter + " bodies " + bodiesBefore + "->" + bodiesAfter);

                report.AppendLine();
                report.AppendLine("INVARIANTS");
                report.AppendLine("Colliders: " + collidersBefore + " -> " + collidersAfter + " (must match)");
                report.AppendLine("Rigidbodies: " + bodiesBefore + " -> " + bodiesAfter + " (must match)");
                report.AppendLine("Lights: " + lightsBefore + " -> " + lightsAfter + " (uplights added on purpose)");
                report.AppendLine("Production WipRoom saved: NO");

                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/VenueArt_Fix_Preview.txt", report.ToString());
                EditorSceneManager.MarkSceneDirty(preview);
                if (!EditorSceneManager.SaveScene(preview))
                    throw new IOException("Could not save the preview Scene.");

                if (SceneView.lastActiveSceneView != null) SceneView.RepaintAll();
                Debug.Log("[VenueArtFix]\n" + report);
                EditorUtility.DisplayDialog("147VR Venue Art",
                    "Preview applied and saved.\n\nCurtain / Cornice / Haze / Statue uplights adjusted in the PREVIEW only.\n" +
                    "Physics unchanged. Production WipRoom untouched.\n" +
                    "Inspect this preview + Logs/VenueArt_Fix_Preview.txt before any adoption.", "OK");
            }
            catch (Exception ex)
            {
                foreach (var pair in matChanges) if (pair.Item1 != null) pair.Item1.sharedMaterials = pair.Item2;
                foreach (var pair in transformChanges) if (pair.Item1 != null) pair.Item1.localScale = pair.Item2;
                foreach (GameObject g in createdLights) if (g != null) UnityEngine.Object.DestroyImmediate(g);
                EditorSceneManager.MarkSceneDirty(preview);
                Debug.LogError("[VenueArtFix] rolled back: " + ex);
                EditorUtility.DisplayDialog("147VR Venue Art", "Apply failed, rolled back in the preview:\n" + ex.Message +
                    "\nProduction WipRoom untouched.", "OK");
            }
        }

        // ---------------- fix implementations ----------------

        private static void FixCurtain(Scene scene, List<Tuple<Renderer, Material[]>> changes, StringBuilder report)
        {
            Material velvet = LoadMat(CurtainVelvetMat) ?? LoadMat(CurtainVelvetMatAlt);
            if (velvet == null) { report.AppendLine("CURTAIN: velvet material missing, skipped."); return; }

            // The welcome curtain meshes all live under a hierarchy node named "...ผ้าม่าน welcome...".
            // Match on the FULL PATH (not the material name): the fabric/rod meshes use generic material
            // names (147VR_147VR_material(_1), "...Burgundy Red Velvet...") that do NOT contain the tokens,
            // while the gold WELCOME neon sign lives under "147VR_NeonSigns_Imported" (no "ผ้าม่าน"),
            // so path matching repaints the real curtain and spares the sign.
            string curtainKey = Normalize("ผ้าม่าน");
            var renderers = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => Normalize(HierarchyPath(r.transform)).Contains(curtainKey))
                .ToArray();
            if (renderers.Length == 0)
            {
                report.AppendLine("CURTAIN: no renderers under the curtain group, skipped.");
                return;
            }

            int slots = 0, touched = 0, spared = 0;
            foreach (Renderer r in renderers)
            {
                Material[] cur = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < cur.Length; i++)
                {
                    Material m = cur[i];
                    if (m == null || m == velvet) continue;
                    string mn = Normalize(m.name);
                    // Leave gold/brass trim and the welcome sign material intact.
                    if (mn.Contains("gold") || mn.Contains("brass")) { spared++; continue; }
                    if (!changed) { changes.Add(Tuple.Create(r, r.sharedMaterials.ToArray())); changed = true; }
                    cur[i] = velvet; slots++;
                }
                if (changed) { r.sharedMaterials = cur; touched++; }
            }
            report.AppendLine("CURTAIN: applied " + velvet.name + " to " + slots + " slot(s) across " +
                touched + " curtain renderer(s); gold/brass slots spared: " + spared + "; sign untouched.");
        }

        private static void FixCornice(Scene scene, List<Tuple<Renderer, Material[]>> changes, StringBuilder report)
        {
            var renderers = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => Matches(r.name, CorniceTokens))
                .ToArray();
            if (renderers.Length == 0) { report.AppendLine("CORNice: no renderers matched tokens " + string.Join(",", CorniceTokens) + ", skipped."); return; }
            Material cornice = GetOrCreateCorniceMaterial();
            if (cornice == null) { report.AppendLine("CORNice: could not build cornice material (frieze texture missing?), skipped."); return; }
            foreach (Renderer r in renderers)
            {
                Material[] cur = r.sharedMaterials;
                changes.Add(Tuple.Create(r, cur.ToArray()));
                for (int i = 0; i < cur.Length; i++) cur[i] = cornice;
                r.sharedMaterials = cur;
            }
            report.AppendLine("CORNice: applied cornice material to " + renderers.Length + " renderer(s).");
        }

        private static void FixHaze(Scene scene, List<Tuple<Transform, Vector3>> changes, StringBuilder report)
        {
            var allR = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray();
            Renderer haze = allR.FirstOrDefault(r => Matches(r.name, HazeTokens));
            if (haze == null) { report.AppendLine("HAZE: not found, skipped."); return; }

            // Owner decision (2026-11-17): keep the haze as a full-room soft atmosphere.
            // The previous diffuser-proportional math (target = diffuser width * ratio) collapsed the
            // 25 x 4 x 34 m room volume down to a ~0.3 m sliver, which killed the room fog. We now
            // intentionally leave the haze transform untouched and only report its current bounds.
            Bounds hb = haze.bounds;
            report.AppendLine("HAZE: kept full-room atmosphere (size=" + Fmt(hb.size) + ", center=" + Fmt(hb.center) +
                "); no scale change (owner wants room-scale fog).");
        }

        private static void FixStatueUplights(Scene scene, List<GameObject> created, StringBuilder report)
        {
            var allR = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray();
            var allL = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).ToArray();
            var statues = allR.Where(r => Matches(r.name, StatueTokens)).ToArray();
            if (statues.Length == 0) { report.AppendLine("STATUE: no statue renderers found, skipped."); return; }

            int retuned = 0, added = 0;
            foreach (Renderer s in statues)
            {
                Bounds b = s.bounds;
                float height = Mathf.Max(0.5f, b.size.y);
                Vector3 basePos = new Vector3(b.center.x, b.min.y + 0.06f, b.center.z);

                Light existing = allL.FirstOrDefault(l => Matches(l.name, UplightTokens) &&
                    Vector3.Distance(l.transform.position, basePos) < height);
                if (existing != null)
                {
                    existing.color = UplightColor;
                    existing.intensity = UplightIntensity;
                    existing.range = height * UplightRangeFactor;
                    if (existing.type != LightType.Spot) existing.type = LightType.Spot;
                    existing.spotAngle = UplightSpotAngle;
                    existing.shadows = LightShadows.Soft;
                    retuned++;
                    continue;
                }
                var go = new GameObject("147VR_F_StatueUplight");
                go.transform.position = basePos;
                go.transform.rotation = Quaternion.LookRotation(new Vector3(b.center.x - basePos.x, height, b.center.z - basePos.z).normalized);
                var light = go.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = UplightColor;
                light.intensity = UplightIntensity;
                light.range = height * UplightRangeFactor;
                light.spotAngle = UplightSpotAngle;
                light.shadows = LightShadows.Soft;
                created.Add(go);
                added++;
            }
            report.AppendLine("STATUE UPLIGHT: retuned " + retuned + " existing, created " + added + " new (warm " +
                Fmt((int)(UplightColor.g * 100)) + "K-ish, intensity " + UplightIntensity + ", angle " + UplightSpotAngle + ").");
        }

        // ---------------- helpers ----------------

        private static Material GetOrCreateCorniceMaterial()
        {
            const string path = MatDir + "/147VR_AAA_MAT_Cornice_Cream_Frieze.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return null;
            if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = lit;
            mat.SetColor("_BaseColor", CorniceBaseColor);
            mat.SetFloat("_Metallic", CorniceMetallic);
            mat.SetFloat("_Smoothness", CorniceSmoothness);
            var frieze = AssetDatabase.LoadAssetAtPath<Texture2D>(FriezeTex);
            if (frieze != null) mat.SetTexture("_BaseMap", frieze);
            var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(DamaskNormalTex);
            if (nrm != null) { mat.SetTexture("_BumpMap", nrm); mat.EnableKeyword("_NORMALMAP"); }
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static bool IsWipRoom(Scene s) =>
            s.IsValid() && s.isLoaded && string.Equals(s.path, ScenePath, StringComparison.OrdinalIgnoreCase);

        private static bool Matches(string name, string[] tokens)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = Normalize(name);
            return tokens.Any(t => n.Contains(Normalize(t)));
        }

        private static bool HasMaterialToken(Renderer r, string[] tokens)
        {
            foreach (var m in r.sharedMaterials)
                if (m != null && Matches(m.name, tokens)) return true;
            return false;
        }

        private static string Normalize(string v) =>
            string.IsNullOrEmpty(v) ? "" : new string(v.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

        private static Material LoadMat(string path) => AssetDatabase.LoadAssetAtPath<Material>(path);

        private static void DescribeRenderer(StringBuilder sb, Renderer r)
        {
            var mats = r.sharedMaterials.Select(m => m == null ? "<null>" : m.name).ToArray();
            string colors = string.Join(",", r.sharedMaterials.Select(m =>
                m == null ? "-" : Fmt(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                                 : (m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white))));
            string tex = string.Join(",", r.sharedMaterials.Select(m =>
                m == null ? "-" : (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "NONE")));
            sb.AppendLine("  [" + HierarchyPath(r.transform) + "] mats={" + string.Join("|", mats) +
                "} baseColor={" + colors + "} baseTex={" + tex + "}");
        }

        private static void DescribeBounds(StringBuilder sb, string tag, Renderer r)
        {
            Bounds b = r.bounds;
            sb.AppendLine("  " + tag + " [" + HierarchyPath(r.transform) + "] center=" + Fmt(b.center) +
                " size=" + Fmt(b.size) + " localScale=" + Fmt(r.transform.localScale));
        }

        private static void DescribeLight(StringBuilder sb, Light l)
        {
            sb.AppendLine("  LIGHT [" + HierarchyPath(l.transform) + "] type=" + l.type + " color=" + Fmt(l.color) +
                " intensity=" + l.intensity.ToString("F2") + " range=" + l.range.ToString("F2") +
                " spotAngle=" + l.spotAngle.ToString("F1") + " pos=" + Fmt(l.transform.position) +
                " fwd=" + Fmt(l.transform.forward));
        }

        private static string HierarchyPath(Transform t)
        {
            var parts = new List<string>();
            for (Transform c = t; c != null; c = c.parent) parts.Insert(0, c.name);
            return string.Join("/", parts);
        }

        private static int Count<T>(Scene s) where T : Component =>
            s.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<T>(true).Length);

        private static string GetUnusedPreviewPath()
        {
            int v = 1; string p;
            do { p = PreviewSceneStem + "_v" + v.ToString("000") + ".unity"; v++; } while (File.Exists(p));
            return p;
        }

        private static void BackupIfExists(string rel, string root)
        {
            string abs = Path2(rel);
            if (!File.Exists(abs)) return;
            File.Copy(abs, System.IO.Path.Combine(root, System.IO.Path.GetFileName(rel)), true);
        }

        private static string Path2(string p) => System.IO.Path.GetFullPath(p);

        private static string Fmt(Vector3 v) => "(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + "," + v.z.ToString("F2") + ")";
        private static string Fmt(Color c) => "(" + c.r.ToString("F2") + "," + c.g.ToString("F2") + "," + c.b.ToString("F2") + ")";
        private static string Fmt(int i) => i.ToString();
    }
}
#endif
