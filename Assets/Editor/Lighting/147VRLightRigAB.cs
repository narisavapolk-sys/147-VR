#if UNITY_EDITOR
// -----------------------------------------------------------------------------
// 147VR Light Rig A/B loop
// -----------------------------------------------------------------------------
// Goal: make Unity MainScene look like the Blender AAA render by putting real
// Light components on the REAL ripped fixtures (nothing is guessed, nothing is
// modelled), and by giving the scene a skybox-driven IBL environment.
//
// Pipeline (each step is a menu item, so nothing edits the scene unattended):
//
//   1) Dump Fixture Centroids (CSV)
//      scans the open scene for ripped light fixtures and writes
//      Logs/light_fixtures.csv  ->  name, hierarchy path, world bounds CENTRE,
//      world bounds SIZE, active flag, rig group, merge bucket.
//      The centre of Renderer.bounds is used on purpose: ripped meshes carry
//      baked internal offsets, so object.position is NOT the visual position.
//
//   2) Apply Variant A / Apply Variant B
//      reads Logs/light_fixtures.csv (dumps it first if missing) plus
//      Logs/light_variants.csv (variant,key,value rows). Builds one Light per
//      fixture bucket, saves the whole rig as a prefab under
//      Assets/Prefabs/Lighting/PREFAB_147VR_LightRig_<variant>.prefab and leaves
//      a linked prefab instance in the scene. Every number is CSV-driven, so a
//      re-Apply after editing the CSV needs no recompile.
//      Variant A is the Blender-match pass, B is the alternative for comparison.
//
//   3) Capture Current
//      renders the same 5 poses for whichever rig is currently active into
//      Logs/parity/<variant>/NN_*.png and writes Logs/parity/index.txt with the
//      exact camera poses, plus any Blender reference renders it can find.
//
//   4) Revert
//      removes the scene rig instance + clears the skybox/ambient it applied.
//      Prefabs and CSVs stay on disk (they are the review artefacts).
//
// Safety: never deletes user files, never saves the scene (Ctrl+S is yours),
// never adds geometry, and every step prints Logs/lightRig_report.txt.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace VR147.EditorTools
{
    public static class LightRigAB
    {
        const string FixtureCsv   = "Logs/light_fixtures.csv";
        const string VariantCsv   = "Logs/light_variants.csv";
        const string ActiveFile   = "Logs/light_rig_active.txt";
        const string ReportFile   = "Logs/lightRig_report.txt";
        const string PrefabFolder = "Assets/Prefabs/Lighting";
        const string ParityFolder = "Logs/parity";
        const string RigRootName  = "147VR_LightRig";

        sealed class CI : IFormatProvider
        {
            static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
            public object GetFormat(Type t) { return Invariant; }
            public static string N(float v) => v.ToString(Invariant);
            public static string R(Vector3 v) => string.Format(Invariant, "{0:F4} {1:F4} {2:F4}", v.x, v.y, v.z);
        }

        sealed class Fixture
        {
            public string name;
            public string path;
            public Vector3 centre;
            public Vector3 size;
            public bool active;
            public string group;
            public string bucket;
        }

        // Fixture name -> rig group + merge bucket (one Light per bucket centroid).
        static readonly (string pattern, string group, string bucketRegex)[] Groups =
        {
            ("147VR_AAA_LightShaft_R", "TableKey",     @""),          // one light per bulb: 3 per table, 12 total
            ("147VR_F_Sconce_",        "WallSconce",   @""),           // each sconce keeps its own light (they span the wall)
            ("0019_LoftLight",         "GalleryLoft",  @""),
            ("147VR_F_UplightProto",   "CoveUplight",  @""),
            ("147VR_SpotlightMesh",    "StageSpot",    @""),
        };

        // ---- menu ----

        [MenuItem("147 VR/Lighting/AB Loop/1 Dump Fixture Centroids (CSV)", false, 300)]
        static void MenuDump()
        {
            var log = new StringBuilder();
            int n = DumpFixtures(log);
            Finish(log, n > 0);
        }

        [MenuItem("147 VR/Lighting/AB Loop/2a Apply Variant A (Blender match)", false, 301)]
        static void MenuA() => Apply("A");

        [MenuItem("147 VR/Lighting/AB Loop/2b Apply Variant B (alternative)", false, 302)]
        static void MenuB() => Apply("B");

        [MenuItem("147 VR/Lighting/AB Loop/2c Apply A + Save Scene", false, 303)]
        static void MenuASave() => Apply("A", true);

        [MenuItem("147 VR/Lighting/AB Loop/2d Apply B + Save Scene", false, 304)]
        static void MenuBSave() => Apply("B", true);

        /// <summary>Headless entry point:
        /// Unity -batchmode -projectPath &lt;proj&gt; -executeMethod VR147.EditorTools.LightRigAB.BatchApplyA -quit
        /// (batchmode has no GPU, so screenshots still need the Editor GUI.)</summary>
        public static void BatchApplyA() => Apply("A", true);
        public static void BatchApplyB() => Apply("B", true);
        public static void BatchDumpFixtures()
        {
            var log = new StringBuilder();
            int n = DumpFixtures(log);
            Finish(log, n > 0);
        }

        /// <summary>Callable from ToolRunner / batchmode without touching the menus.</summary>
        public static void ApplyVariant(string variant, bool saveScene) => Apply(variant ?? "A", saveScene);
        public static void CaptureNow() => Capture();
        public static void DumpFixturesNow()
        {
            var log = new StringBuilder();
            int n = DumpFixtures(log);
            Finish(log, n > 0);
        }

        [MenuItem("147 VR/Lighting/AB Loop/3 Capture Current Rig", false, 305)]
        static void MenuCapture() => Capture();

        [MenuItem("147 VR/Lighting/AB Loop/4 Revert Scene Rig + Environment", false, 306)]
        static void MenuRevert() => Revert();

        [MenuItem("147 VR/Lighting/AB Loop/5 Write Default Variant CSV (reset numbers)", false, 305)]
        static void MenuDefaults()
        {
            var log = new StringBuilder();
            WriteDefaultVariants(log, true);
            Finish(log, true);
        }

        // ---- 1. fixture dump ----

        static int DumpFixtures(StringBuilder log)
        {
            var fx = ScanFixtures();
            Directory.CreateDirectory("Logs");
            var sb = new StringBuilder();
            sb.AppendLine("name,path,centreX,centreY,centreZ,sizeX,sizeY,sizeZ,active,group,bucket");
            foreach (var f in fx.OrderBy(f => f.group).ThenBy(f => f.name))
            {
                sb.AppendLine(string.Join(",",
                    Csv(f.name), Csv(f.path), CI.N(f.centre.x), CI.N(f.centre.y), CI.N(f.centre.z),
                    CI.N(f.size.x), CI.N(f.size.y), CI.N(f.size.z), f.active ? "1" : "0", f.group, Csv(f.bucket)));
            }
            File.WriteAllText(FixtureCsv, sb.ToString());
            log.AppendLine($"[OK] {fx.Count} ripped fixtures -> {Path.GetFullPath(FixtureCsv)}");
            foreach (var g in fx.GroupBy(f => f.group).OrderBy(g => g.Key))
                log.AppendLine($"       {g.Key,-12} n={g.Count():2}  e.g. {g.First().name} @ ({g.First().centre.x:F2}, {g.First().centre.y:F2}, {g.First().centre.z:F2})");
            foreach (var miss in Groups.Select(g => g.group).Where(grp => !fx.Any(f => f.group == grp)))
                log.AppendLine($"[MISS] no fixture in scene for group {miss} (it may only exist in the .blend)");
            return fx.Count;
        }

        static List<Fixture> ScanFixtures()
        {
            var found = new List<Fixture>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r == null || string.IsNullOrEmpty(r.name))
                    continue;
                foreach (var g in Groups)
                {
                    if (!r.name.StartsWith(g.pattern, StringComparison.Ordinal))
                        continue;
                    found.Add(new Fixture
                    {
                        name   = r.name,
                        path   = HierarchyPath(r.transform),
                        centre = r.bounds.center,
                        size   = r.bounds.size,
                        active = r.gameObject.activeInHierarchy,
                        group  = g.group,
                        bucket = BucketOf(r.name, g.bucketRegex),
                    });
                    break;
                }
            }
            return found;
        }

        static string BucketOf(string name, string regex)
        {
            if (string.IsNullOrEmpty(regex))
                return name;
            var m = System.Text.RegularExpressions.Regex.Match(name, regex);
            return m.Success ? m.Value : name;
        }

        static string HierarchyPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null) { stack.Push(t.name); t = t.parent; }
            return string.Join("/", stack);
        }

        static List<Fixture> ReadFixtures(StringBuilder log)
        {
            if (!File.Exists(FixtureCsv))
            {
                log.AppendLine("[INFO] fixture CSV missing, scanning the scene now");
                if (DumpFixtures(log) == 0)
                    return new List<Fixture>();
            }
            var list = new List<Fixture>();
            var lines = File.ReadAllLines(FixtureCsv);
            for (int i = 1; i < lines.Length; i++)
            {
                var c = SplitCsv(lines[i]);
                if (c.Count < 11)
                    continue;
                list.Add(new Fixture
                {
                    name   = c[0],
                    path   = c[1],
                    centre = new Vector3(F(c[2]), F(c[3]), F(c[4])),
                    size   = new Vector3(F(c[5]), F(c[6]), F(c[7])),
                    active = c[8] == "1",
                    group  = c[9],
                    bucket = c[10],
                });
            }
            return list;
        }

        // ---- 2. apply a variant ----

        const string MainScenePath = "Assets/Scenes/147VR_MainScene.unity";

        /// <summary>Batchmode boots into whichever scene Build Settings lists first; the rig has to
        /// land in MainScene, so open it explicitly when nobody is watching. In the GUI we only warn
        /// (never yank the scene out from under the user).</summary>
        static void EnsureMainScene(StringBuilder log)
        {
            var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (sc.path == MainScenePath)
                return;
            if (Application.isBatchMode)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(MainScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
                log.AppendLine($"[OK] batchmode: opened {MainScenePath}");
                return;
            }
            log.AppendLine($"[WARN] active scene is '{sc.path}' (not MainScene) — applying to the open scene.");
        }

        static void Apply(string variant, bool saveScene = false)
        {
            var log = new StringBuilder();
            log.AppendLine($"[LightRig] Apply variant '{variant}'  {DateTime.Now:HH:mm:ss}");
            EnsureMainScene(log);

            var fixtures = ReadFixtures(log);
            if (fixtures.Count == 0)
            {
                log.AppendLine("[ABORT] no ripped fixtures found — open 147VR_MainScene and use menu 1 first.");
                Finish(log, false);
                return;
            }

            var p = ReadVariants(variant, log);
            ApplyEnvironment(p, log);
            ApplyDirectional(p, log);
            ApplyVolume(p, log);
            ApplyCameraPost(p, log);

            int created = BuildRig(fixtures, p, variant, log);
            if (created == 0)
                log.AppendLine("[WARN] zero lights created — check group/enable rows in light_variants.csv");
            ApplyShaftPass(p, log);

            File.WriteAllText(ActiveFile, variant);
            log.AppendLine($"[OK] active variant = {variant}");

            if (saveScene)
            {
                var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
                bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                log.AppendLine(saved
                    ? $"[OK] scene saved -> {sc.path} (rig + environment are now on disk)"
                    : "[WARN] SaveOpenScenes was refused/cancelled — the rig exists only in memory, Ctrl+S it manually");
            }
            else
            {
                log.AppendLine("[INFO] scene NOT saved (menu 2c/2d saves it, or press Ctrl+S when happy)");
            }
            Finish(log, created > 0);
        }

        static int BuildRig(List<Fixture> fixtures, Dictionary<string, string> p, string variant, StringBuilder log)
        {
            DestroyExistingRig();

            var root = new GameObject(RigRootName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            int n = 0;

            foreach (var grp in fixtures.GroupBy(f => f.group))
            {
                if (!p.TryGetValue($"{grp.Key}.enabled", out string en) || en != "1")
                {
                    log.AppendLine($"[skip] group {grp.Key} (disabled in variant CSV)");
                    continue;
                }
                // Recompute the bucket from the live grouping rule: the CSV stores raw geometry, so
                // changing Groups[] must take effect without re-dumping the fixture file.
                foreach (var bucket in grp.GroupBy(f => BucketOf(f.name, BucketRegexFor(grp.Key))))
                {
                    var members = bucket.Where(f => f.active).ToList();
                    int skipped = bucket.Count() - members.Count;
                    if (skipped > 0)
                        log.AppendLine($"   [skip] {skipped} inactive fixture(s) in {grp.Key}/{bucket.Key}");
                    if (members.Count == 0)
                        continue;

                    Vector3 c = Vector3.zero, s = Vector3.zero;
                    foreach (var f in members)
                    {
                        c += f.centre;
                        s = Vector3.Max(s, f.size);
                    }
                    c /= members.Count;

                    Light l = MakeLight(grp.Key, bucket.Key, c, s, p, log);
                    if (l == null)
                        continue;
                    l.transform.SetParent(root.transform, false);
                    l.name = $"{grp.Key}_{bucket.Key}";
                    n++;
                }
            }

            if (n == 0)
            {
                Object.DestroyImmediate(root);
                return 0;
            }

            Directory.CreateDirectory(PrefabFolder);
            string prefabPath = $"{PrefabFolder}/PREFAB_147VR_LightRig_{variant}.prefab";
            try
            {
                var saved = PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
                log.AppendLine(saved != null
                    ? $"[OK] rig saved as {n} lights -> {prefabPath} (scene object stays linked to the prefab)"
                    : $"[WARN] prefab save returned null ({n} live lights remain in the scene)");
            }
            catch (Exception e)
            {
                log.AppendLine($"[WARN] prefab not saved ({e.Message}); lights are still in the scene");
            }
            log.AppendLine($"[OK] {n} lights created from {fixtures.Count} fixtures");
            return n;
        }

        static Light MakeLight(string group, string bucket, Vector3 centre, Vector3 size, Dictionary<string, string> p, StringBuilder log)
        {
            string key = group + ".";
            string typeStr = Get(p, key + "type", group == "GalleryLoft" ? "point" : "spot");
            LightType type = typeStr == "point" ? LightType.Point : typeStr == "directional" ? LightType.Directional : LightType.Spot;

            var go = new GameObject($"{group}_{bucket}");
            float yLift = F(Get(p, key + "yLift", group == "TableKey" ? "-0.18" : "0.02"));
            // Vertical anchor. The ripped LightShaft mesh IS the whole light cone, so its bounds
            // centre floats mid-air inside the cone while the lamp sits at the TOP of that mesh.
            float anchorY = Get(p, key + "yAnchor", group == "TableKey" ? "top" : "centre") switch
            {
                "top" => centre.y + size.y * 0.5f,
                "bottom" => centre.y - size.y * 0.5f,
                _ => centre.y,
            };
            Vector3 pos = new Vector3(centre.x, anchorY + yLift, centre.z);

            Vector3 dir;
            switch (group)
            {
                case "TableKey":
                    dir = Vector3.down;
                    break;
                case "CoveUplight":
                    dir = Vector3.up;
                    break;
                case "WallSconce":
                    // Do not trust the rip's NX/PX naming — it does not follow world axes. A sconce
                    // washes the wall it is bolted to, so aim from the fixture toward the room
                    // interior with an upward bias, and step off the wall so it is not buried in it.
                    Vector3 inward = RoomInward(pos);
                    pos += inward * F(Get(p, key + "wallStandoff", "0.30"));
                    dir = inward + Vector3.up * F(Get(p, key + "washUp", "0.55"));
                    break;
                default:
                    dir = Vector3.down;
                    break;
            }
            go.transform.position = pos;
            if (dir.sqrMagnitude > 1e-6f && type != LightType.Point)
                go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            Light l = go.AddComponent<Light>();
            l.type = type;
            l.color = ColorTemperature(F(Get(p, key + "kelvin", "2700")));
            l.useColorTemperature = false;          // keep the converted RGB so the Inspector stays readable
            l.intensity = F(Get(p, key + "intensity", "10"));
            l.range = F(Get(p, key + "range", "7"));
            if (type == LightType.Spot)
                l.spotAngle = F(Get(p, key + "angle", "100"));
            l.shadows = Get(p, key + "shadows", group == "TableKey" ? "soft" : "none") switch
            {
                "soft" => LightShadows.Soft,
                "hard" => LightShadows.Hard,
                _ => LightShadows.None,
            };
            l.cullingMask = ~0;
            // renderingLayerMask left at its default (layer 1) — Unity 6 types it as int and the
            // project's renderers are all on the Default layer, so overriding it buys nothing.
            float bounce = F(Get(p, key + "bounceIntensity", "0"));
            if (bounce > 0f)
                l.bounceIntensity = bounce;         // URP lightmaps are off today, so this is usually invisible
            log.AppendLine($"   + {l.name,-28} {type,-11} pos={CI.R(go.transform.position)} dir={CI.R(dir.normalized)} " +
                           $"int={l.intensity:F1} range={l.range:F1} angle={l.spotAngle:F0} shadows={l.shadows}");
            return l;
        }

        static string BucketRegexFor(string group)
        {
            foreach (var g in Groups)
                if (g.group == group)
                    return g.bucketRegex;
            return "";
        }

        /// <summary>Direction from a wall-mounted fixture toward the room interior, derived from the
        /// fixture's own world position on whichever axis dominates its offset from the room centre.
        /// (The rip's NX/PX name tokens do NOT follow world axes, so naming cannot be trusted.)</summary>
        static Vector3 RoomInward(Vector3 fixture)
        {
            float ax = Mathf.Abs(fixture.x), az = Mathf.Abs(fixture.z);
            if (ax < 0.01f && az < 0.01f)
                return Vector3.up;
            return ax >= az
                ? new Vector3(fixture.x >= 0f ? -1f : 1f, 0f, 0f)
                : new Vector3(0f, 0f, fixture.z >= 0f ? -1f : 1f);
        }

        const string ShaftPrefix = "147VR_AAA_LightShaft_";

        /// <summary>In the Blender file the light shafts are additive volumetrics; the FBX brings them
        /// in as solid dark cones, so they eat the pendant light and print a black funnel onto the
        /// cloth. By default they stop casting shadows (and can be hidden entirely) until the material
        /// pass is authored properly.</summary>
        static void ApplyShaftPass(Dictionary<string, string> p, StringBuilder log)
        {
            bool noShadow = Get(p, "env.shaftNoShadow", "1") == "1";
            bool hide = Get(p, "env.hideLightShafts", "0") == "1";
            int shadowOff = 0, hidden = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r == null || !r.name.StartsWith(ShaftPrefix, StringComparison.Ordinal))
                    continue;
                if (noShadow && r.shadowCastingMode != ShadowCastingMode.Off)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    EditorUtility.SetDirty(r);
                    shadowOff++;
                }
                if (hide && r.enabled)
                {
                    r.enabled = false;
                    EditorUtility.SetDirty(r);
                    hidden++;
                }
            }
            log.AppendLine($"[OK] light shafts: shadowCastingOff={shadowOff} hidden={hidden} " +
                           $"(env.shaftNoShadow={noShadow} env.hideLightShafts={hide})");
        }

        static void RestoreShaftPass(StringBuilder log)
        {
            int restored = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r == null || !r.name.StartsWith(ShaftPrefix, StringComparison.Ordinal))
                    continue;
                if (r.shadowCastingMode != ShadowCastingMode.On) { r.shadowCastingMode = ShadowCastingMode.On; EditorUtility.SetDirty(r); restored++; }
                if (!r.enabled) { r.enabled = true; EditorUtility.SetDirty(r); restored++; }
            }
            log.AppendLine($"[OK] light shafts restored to their imported state ({restored} changes)");
        }

        /// <summary>Blackbody -> linear-ish RGB (Tanner Helland approximation), good enough to
        /// match Blender's colour temperature node when tuned from screenshots.</summary>
        static Color ColorTemperature(float kelvin)
        {
            float t = Mathf.Clamp(kelvin, 1000f, 12000f) / 100f;
            float r, g, b;
            if (t <= 66f) { r = 255f; g = 99.4708025861f * (float)Math.Log(t) - 161.1195681661f; }
            else { r = 329.698727446f * (float)Math.Pow(t - 60f, -0.1332047592f); g = 288.1221695283f * (float)Math.Pow(t - 60f, -0.0755148492f); }
            if (t >= 66f) b = 255f;
            else if (t <= 19f) b = 0f;
            else b = 138.5177312231f * (float)Math.Log(t - 10f) - 305.0447927307f;
            return new Color(Mathf.Clamp01(r / 255f), Mathf.Clamp01(g / 255f), Mathf.Clamp01(b / 255f));
        }

        static void DestroyExistingRig()
        {
            var old = GameObject.Find(RigRootName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
                Debug.Log("[LightRig] removed the previous rig root before rebuilding");
            }
        }

        // ---- environment / directional / volume ----

        static void ApplyEnvironment(Dictionary<string, string> p, StringBuilder log)
        {
            var s = new MainSceneEnvBuilder.Settings
            {
                hdriIndex           = (int)F(Get(p, "env.hdriIndex", "0")),
                skyExposure         = F(Get(p, "env.skyExposure", "0.35")),
                ambientIntensity    = F(Get(p, "env.ambientIntensity", "1.15")),
                reflectionIntensity = F(Get(p, "env.reflectionIntensity", "0.9")),
                enableCameraPostAA  = Get(p, "env.cameraPostAA", "1") == "1",
            };
            int count = MainSceneEnvBuilder.HdriOptions.Length;
            if (s.hdriIndex < 0 || s.hdriIndex >= count)
            {
                log.AppendLine($"[WARN] env.hdriIndex {s.hdriIndex} out of range (0..{count - 1}), using 0");
                s.hdriIndex = 0;
            }
            MainSceneEnvBuilder.Apply(s, false, log);
            log.AppendLine($"[OK] IBL: ambientMode=Skybox intensity={s.ambientIntensity:F2} reflection={s.reflectionIntensity:F2}");
        }

        static void ApplyDirectional(Dictionary<string, string> p, StringBuilder log)
        {
            bool on = Get(p, "env.directionalEnabled", "0") == "1";
            float intensity = F(Get(p, "env.directionalIntensity", "0.08"));
            var suns = Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                .Where(l => l != null && l.type == LightType.Directional && !l.transform.name.StartsWith(RigRootName))
                .ToList();
            foreach (var sun in suns)
            {
                sun.enabled = on;
                sun.intensity = intensity;
                log.AppendLine($"[OK] directional '{sun.name}' enabled={on} intensity={intensity:F3} " +
                               "(Blender lights this room with fixtures, not a sun)");
            }
            if (suns.Count == 0)
                log.AppendLine("[INFO] no directional light in the scene");
        }

        static void ApplyVolume(Dictionary<string, string> p, StringBuilder log)
        {
            string path = Get(p, "env.volumeProfile", "Assets/Settings/147VR_Quest_LightingProfile.asset");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                log.AppendLine($"[WARN] VolumeProfile not found at {path} — post/tone mapping untouched");
                return;
            }
            var volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include).ToList();
            Volume target = volumes.FirstOrDefault(v => v != null && v.isGlobal) ?? volumes.FirstOrDefault(v => v != null);
            if (target == null)
            {
                var go = new GameObject("147VR Global Volume (LightRig)");
                target = go.AddComponent<Volume>();
                target.isGlobal = true;
                log.AppendLine("[NEW] no global Volume existed — created one");
            }
            target.profile = profile;
            target.isGlobal = true;
            target.weight = F(Get(p, "env.volumeWeight", "1"));
            EditorUtility.SetDirty(target);

            // Tone curve is the single biggest "does it look like the Blender render" knob: the
            // profile ships as ACES, which clips the cloth to neon green. AgX/Neutral behave like
            // Blender's view transform.
            float wantMode = F(Get(p, "env.tonemapMode", "-1"));
            Tonemapping tonemap = null;
            if (wantMode >= 0f)
            {
                foreach (VolumeComponent c in profile.components)
                {
                    if (c is Tonemapping tm) { tonemap = tm; break; }
                }
            }
            if (wantMode >= 0f && tonemap != null)
            {
                string[] names = System.Enum.GetNames(typeof(TonemappingMode));
                int idx = Mathf.Clamp((int)wantMode, 0, names.Length - 1);
                tonemap.mode.overrideState = true;
                tonemap.mode.value = (TonemappingMode)idx;
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                log.AppendLine($"[OK] Tonemapping -> {names[idx]} (available: {string.Join("/", names)})");
            }
            else if (wantMode >= 0f)
            {
                log.AppendLine("[WARN] profile has no Tonemapping component; env.tonemapMode ignored");
            }

            log.AppendLine($"[OK] global Volume -> {profile.name} weight={target.weight:F2} ({volumes.Count} volume(s) in scene)");
        }

        /// <summary>URP post-processing is per-camera. MainScene's XR cameras are inactive in the
        /// Editor, so a live-camera scan enables nothing — include inactive ones and report it.</summary>
        static void ApplyCameraPost(Dictionary<string, string> p, StringBuilder log)
        {
            if (Get(p, "env.cameraPostAA", "1") != "1")
                return;
            int changed = 0, seen = 0;
            foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (cam == null || cam.name.Contains("Capture"))
                    continue;
                seen++;
                var data = cam.GetUniversalAdditionalCameraData();
                if (data == null)
                    continue;
                if (!data.renderPostProcessing)
                {
                    data.renderPostProcessing = true;
                    changed++;
                }
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                data.antialiasingQuality = AntialiasingQuality.High;
                EditorUtility.SetDirty(cam);
            }
            log.AppendLine(changed > 0
                ? $"[OK] post + FXAA enabled on {changed}/{seen} camera(s) (inactive included)"
                : $"[WARN] post already on (or no camera found) — cameras seen: {seen}");
        }

        // ---- 3. capture ----

        static void Capture()
        {
            var log = new StringBuilder();
            string variant = File.Exists(ActiveFile) ? File.ReadAllText(ActiveFile).Trim() : "unset";
            log.AppendLine($"[Capture] active rig = {variant}  {DateTime.Now:HH:mm:ss}");

            var poses = TablePoses(out string note);
            log.AppendLine(note);

            Directory.CreateDirectory(ParityFolder);
            string folder = Path.Combine(ParityFolder, variant);
            Directory.CreateDirectory(folder);

            var camGo = new GameObject("LightRig Capture Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.allowHDR = true;
            cam.fieldOfView = F(Get(ReadVariants(variant, new StringBuilder()), "capture.fov", "62"));
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderShadows = true;

            int w = (int)F(Get(ReadVariants(variant, new StringBuilder()), "capture.width", "1024"));
            int h = (int)F(Get(ReadVariants(variant, new StringBuilder()), "capture.height", "768"));
            var index = new StringBuilder();
            int shot = 0;
            try
            {
                foreach (var pose in poses)
                {
                    camGo.transform.SetPositionAndRotation(pose.pos, Quaternion.LookRotation((pose.look - pose.pos).normalized, Vector3.up));

                    var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                    rt.Create();
                    var prev = RenderTexture.active;
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;
                    cam.targetTexture = null;

                    string file = $"{shot:D2}_{pose.label}.png";
                    string full = Path.Combine(folder, file);
                    File.WriteAllBytes(full, tex.EncodeToPNG());
                    index.AppendLine($"{file}\tpos={CI.R(pose.pos)}\tlook={CI.R(pose.look)}");
                    log.AppendLine($"   -> {full}");
                    shot++;

                    Object.DestroyImmediate(tex);
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }
            finally
            {
                Object.DestroyImmediate(camGo);
            }

            AppendBlenderReferences(index, log);
            File.WriteAllText(Path.Combine(ParityFolder, "index.txt"), index.ToString());
            log.AppendLine($"[OK] {shot} shots -> {Path.GetFullPath(folder)}");
            Finish(log, shot > 0, "Logs/lightRig_capture.txt");
        }

        sealed class Pose { public string label; public Vector3 pos; public Vector3 look; }

        /// <summary>One hero view per table (from the Pass C bay roots, real data) plus a wide
        /// corner view, all at standing eye height.</summary>
        static List<Pose> TablePoses(out string note)
        {
            var poses = new List<Pose>();
            float eye = 1.62f;
            var roots = Object.FindObjectsByType<PoolPhysics.Unity.PoolPhysicsEventRunner>(FindObjectsInactive.Include)
                          .Where(r => r != null && r.TableRootTransform != null)
                          .OrderBy(r => r.BayName, StringComparer.Ordinal)
                          .ToList();
            foreach (var r in roots)
            {
                Vector3 corner = r.TableRootTransform.position;
                Vector2 size = r.PlayfieldSize;
                if (size.x <= 0.1f || size.y <= 0.1f)
                    continue;
                Vector3 centre = corner + new Vector3(size.x * 0.5f, 0f, size.y * 0.5f);
                float clothY = r.SurfaceTopY;
                poses.Add(new Pose
                {
                    label = r.BayName.Replace("_PhysicsRoot", ""),
                    pos   = centre + new Vector3(0f, eye, centre.z >= 0 ? size.y * 0.5f + 1.35f : -(size.y * 0.5f + 1.35f)),
                    look  = new Vector3(centre.x, clothY + 0.12f, centre.z),
                });
            }
            if (roots.Count > 0)
            {
                var first = roots[0];
                Vector2 s = first.PlayfieldSize;
                Vector3 c = first.TableRootTransform.position + new Vector3(s.x * 0.5f, 0f, s.y * 0.5f);
                poses.Add(new Pose
                {
                    label = "WideCorner",
                    pos   = new Vector3(c.x - 4.2f, eye, c.z + 3.4f),
                    look  = new Vector3(c.x + 1.5f, 0.9f, c.z - 3.0f),
                });
            }
            note = poses.Count == 0
                ? "[WARN] no PoolPhysicsEventRunner bay roots found — nothing to frame (open 147VR_MainScene)"
                : $"[OK] {poses.Count} poses from bay roots (eye height {eye:F2} m)";
            return poses;
        }

        static void AppendBlenderReferences(StringBuilder index, StringBuilder log)
        {
            string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            if (!Directory.Exists(desktop))
                return;
            // Non-recursive on purpose: a recursive Desktop scan froze this machine before.
            var pngs = Directory.GetFiles(desktop, "*AAA*.png").Concat(Directory.GetFiles(desktop, "*147VR*.png"))
                        .Where(f => !f.EndsWith(".meta")).Take(6).ToList();
            if (pngs.Count == 0)
            {
                log.AppendLine("[INFO] no Blender reference renders found on the Desktop (top level only) — " +
                               "drop the .blend screenshot there or pass its path to compare");
                return;
            }
            foreach (var png in pngs)
            {
                index.AppendLine($"{Path.GetFileName(png)}\tBLENDER REF (not copied) {png}");
                log.AppendLine($"   ref -> {png}");
            }
        }

        // ---- 4. revert ----

        static void Revert()
        {
            var log = new StringBuilder();
            DestroyExistingRig();
            RestoreShaftPass(log);
            log.AppendLine("[OK] scene rig instance removed (prefab kept under " + PrefabFolder + ")");
            try
            {
                MainSceneEnvBuilder.Revert(log);
            }
            catch (Exception e)
            {
                log.AppendLine("[WARN] environment revert failed: " + e.Message);
            }
            File.WriteAllText(ActiveFile, "none");
            log.AppendLine("[OK] active variant cleared (Logs/light_rig_active.txt = none)");
            Finish(log, true);
        }

        // ---- variant CSV ----

        static void WriteDefaultVariants(StringBuilder log, bool force)
        {
            Directory.CreateDirectory("Logs");
            if (File.Exists(VariantCsv) && !force)
                return;
            var sb = new StringBuilder();
            sb.AppendLine("variant,key,value");
            foreach (string v in new[] { "A", "B" })
            {
                bool bolder = v == "B";
                Row(sb, v, "env.hdriIndex", bolder ? "1" : "0");
                Row(sb, v, "env.skyExposure", bolder ? "0.20" : "0.35");
                Row(sb, v, "env.ambientIntensity", bolder ? "0.75" : "1.15");
                Row(sb, v, "env.reflectionIntensity", "0.9");
                Row(sb, v, "env.cameraPostAA", "1");
                Row(sb, v, "env.directionalEnabled", "0");
                Row(sb, v, "env.directionalIntensity", "0.08");
                Row(sb, v, "env.volumeProfile", "Assets/Settings/147VR_Quest_LightingProfile.asset");
                Row(sb, v, "env.volumeWeight", "1");

                Row(sb, v, "TableKey.enabled", "1");
                Row(sb, v, "TableKey.type", "spot");
                Row(sb, v, "TableKey.intensity", bolder ? "26" : "16");
                Row(sb, v, "TableKey.range", "6.5");
                Row(sb, v, "TableKey.angle", "112");
                Row(sb, v, "TableKey.kelvin", bolder ? "2500" : "2850");
                Row(sb, v, "TableKey.shadows", "soft");
                Row(sb, v, "TableKey.yAnchor", "top");
                Row(sb, v, "TableKey.yLift", "-0.06");
                Row(sb, v, "TableKey.bounceIntensity", "0");
                Row(sb, v, "env.shaftNoShadow", "1");
                Row(sb, v, "env.hideLightShafts", bolder ? "1" : "0");

                Row(sb, v, "WallSconce.enabled", "1");
                Row(sb, v, "WallSconce.type", "spot");
                Row(sb, v, "WallSconce.intensity", bolder ? "9" : "6");
                Row(sb, v, "WallSconce.range", "4.2");
                Row(sb, v, "WallSconce.angle", "125");
                Row(sb, v, "WallSconce.kelvin", "2400");
                Row(sb, v, "WallSconce.shadows", "none");
                Row(sb, v, "WallSconce.washUp", "0.6");

                Row(sb, v, "GalleryLoft.enabled", bolder ? "0" : "1");
                Row(sb, v, "GalleryLoft.type", "point");
                Row(sb, v, "GalleryLoft.intensity", "8");
                Row(sb, v, "GalleryLoft.range", "4.5");
                Row(sb, v, "GalleryLoft.kelvin", "2300");
                Row(sb, v, "GalleryLoft.shadows", "none");

                Row(sb, v, "CoveUplight.enabled", "1");
                Row(sb, v, "CoveUplight.type", "spot");
                Row(sb, v, "CoveUplight.intensity", "10");
                Row(sb, v, "CoveUplight.range", "5.5");
                Row(sb, v, "CoveUplight.angle", "140");
                Row(sb, v, "CoveUplight.kelvin", "2700");
                Row(sb, v, "CoveUplight.shadows", "none");

                Row(sb, v, "StageSpot.enabled", "0");
                Row(sb, v, "StageSpot.type", "spot");
                Row(sb, v, "StageSpot.intensity", "18");
                Row(sb, v, "StageSpot.range", "8");
                Row(sb, v, "StageSpot.angle", "42");
                Row(sb, v, "StageSpot.kelvin", "3400");
                Row(sb, v, "StageSpot.shadows", "none");

                Row(sb, v, "capture.fov", "62");
                Row(sb, v, "capture.width", "1024");
                Row(sb, v, "capture.height", "768");
            }
            File.WriteAllText(VariantCsv, sb.ToString());
            log.AppendLine($"[OK] variant numbers -> {Path.GetFullPath(VariantCsv)} (edit and re-Apply, no recompile)");
        }

        static Dictionary<string, string> ReadVariants(string variant, StringBuilder log)
        {
            WriteDefaultVariants(log, false);
            var dict = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(VariantCsv))
            {
                var c = SplitCsv(line);
                if (c.Count < 3 || c[0] == "variant")
                    continue;
                if (c[0].Equals(variant, StringComparison.OrdinalIgnoreCase))
                    dict[c[1]] = c[2];
            }
            return dict;
        }

        // ---- small csv / log helpers ----

        static void Row(StringBuilder sb, string v, string k, string val) =>
            sb.AppendLine(string.Join(",", v, Csv(k), Csv(val)));

        static string Get(Dictionary<string, string> p, string key, string fallback) =>
            p.TryGetValue(key, out string v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

        static string Csv(string s) => s == null ? "" : (s.Contains(",") ? "\"" + s.Replace("\"", "\"\"") + "\"" : s);
        static float F(string s) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;

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

        static void Finish(StringBuilder log, bool ok, string reportName = ReportFile)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(reportName, log.ToString());
            foreach (var line in log.ToString().Split('\n'))
                Debug.Log(line.TrimEnd());
            Debug.Log($"[LightRig] {(ok ? "done" : "see warnings above")} — report: {Path.GetFullPath(reportName)}" +
                      (ok ? "\nNext: Ctrl+S the scene when you are happy with the look." : ""));
            if (!Application.isBatchMode && !NoDialog)
                EditorUtility.DisplayDialog(ok ? "LightRig OK" : "LightRig check the log", log.ToString().Trim(), "OK");
        }
    }
}
#endif
