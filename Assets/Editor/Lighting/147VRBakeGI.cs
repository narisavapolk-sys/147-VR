#if UNITY_EDITOR
// -----------------------------------------------------------------------------
// 147VR Baked GI pass
// -----------------------------------------------------------------------------
// Why: 12 soft-shadow-casting table lamps are the heaviest thing left in the
// URP MainScene at Quest runtime, and the remaining look-gap vs the Blender
// reference is mostly bounce/ambient light + contact AO — exactly what baked
// GI provides for free at runtime.
//
// Commands (driven via ToolRunner Logs/editor_cmd.txt or the menu below):
//   uv2-ensure      set ModelImporter.generateSecondaryUV = true on every model
//                   under the import folders and reimport, then count meshes
//                   still missing the lightmap UV channel.
//   gi-flags        mark static room/table renderers Contribute GI (skips
//                   balls/cues/XR/hands — they sample live light now).
//   lights-baked    flip the LightRig lamps: the 12 TableKey lamps become
//                   Mixed (realtime direct for the balls, no realtime shadow
//                   cost), the room fixtures become Baked. NOTE: with 0 light
//                   probes in the scene, fully-Baked lamps would leave the
//                   dynamic balls unlit — that is why TableKey is Mixed.
//   bake            create/assign Assets/Settings/147VR_MainScene_LightingSettings,
//                   flip the lamps, run a synchronous CPU Progressive bake,
//                   then save scene + assets.
//
// Report: Logs/gi_bake_report.txt (overwritten per command run).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VR147.EditorTools
{
    public static class BakeGI
    {
        const string ReportFile   = "Logs/gi_bake_report.txt";
        const string RigRootName  = "147VR_LightRig";
        const string SettingsPath = "Assets/Settings/147VR_MainScene_LightingSettings.asset";

        /// <summary>Lightmap UV channel in Unity is "uv2" = VertexAttribute.TexCoord1
        /// (Unity numbers VertexAttribute.TexCoord0/1/2... for UV0/UV1/UV2...).</summary>
        const VertexAttribute Uv2Attribute = VertexAttribute.TexCoord1;

        static readonly string[] ModelFolders =
            { "Assets/AAA/Imports", "Assets/AAA/ImportedSnooker", "Assets/Models/PoolTable" };

        /// <summary>Any renderer whose name/ancestor matches these is dynamic runtime
        /// content (balls, cues, VR) — it must NOT be baked into lightmaps.</summary>
        static readonly string[] SkipTokens =
            { "Ball", "Cue", "Hand", "Camera", "XR", "Runner", "Director", "Volume" };

        [MenuItem("147 VR/Lighting/Bake GI/1 Ensure UV2 (Reimport Models)")]
        public static void MenuEnsureUV2() => EnsureUV2Batch();

        [MenuItem("147 VR/Lighting/Bake GI/2 Mark Contribute GI")]
        public static void MenuFlagGI() => FlagGIBatch();

        [MenuItem("147 VR/Lighting/Bake GI/3 Flip Lamps for Bake")]
        public static void MenuFlipLights() => FlipLightsBatch();

        [MenuItem("147 VR/Lighting/Bake GI/4 Bake Now (Baked GI)")]
        public static void MenuBake() => BatchBake();

        static void Log(StringBuilder sb, string line)
        {
            sb.AppendLine(line);
            Debug.Log("[BakeGI] " + line);
        }

        // ---------------------------------------------------------------- uv2-ensure
        public static void EnsureUV2Batch()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[BakeGI/uv2-ensure] {DateTime.Now:HH:mm:ss}");
            try
            {
                int reimported = 0, already = 0, missingAfter = 0;
                var perFile = new List<string>();

                foreach (var guid in AssetDatabase.FindAssets("t:Model", ModelFolders))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (imp == null) continue;

                    var meshes = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToList();
                    int missing = meshes.Count(m => m != null && !m.HasVertexAttribute(Uv2Attribute));

                    if (!imp.generateSecondaryUV && missing > 0)
                    {
                        // Unity 6 (6000.x): ModelImporter.generateSecondaryUV is the
                        // lightmap-UV (uv2/secondary channel) generator flag.
                        // Log BEFORE the import work — a native crash inside the
                        // unwrap leaves no other trace of which file was involved.
                        Log(sb, $"reimport BEGIN: {path} (meshes={meshes.Count}, uv2Missing={missing})");
                        imp.generateSecondaryUV = true;
                        imp.secondaryUVHardAngle = 65f;   // softer unwrap on the big room FBX
                        imp.secondaryUVPackMargin = 4;
                        imp.SaveAndReimport();
                        reimported++;

                        meshes = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToList();
                        int still = meshes.Count(m => m != null && !m.HasVertexAttribute(Uv2Attribute));
                        missingAfter += still;
                        perFile.Add($"{path}: reimported, meshes={meshes.Count} uv2Missing={still}");
                    }
                    else
                    {
                        already++;
                        missingAfter += missing;
                        perFile.Add($"{path}: ok, meshes={meshes.Count} uv2Missing={missing}");
                    }
                }

                Log(sb, $"model files: {perFile.Count} (reimported with lightmap UVs: {reimported}, already had them: {already})");
                foreach (var p in perFile) Log(sb, "  " + p);
                Log(sb, $"[UV2] meshes still missing UV2 after pass: {missingAfter}");
            }
            catch (Exception e)
            {
                Log(sb, "[ERROR] uv2-ensure failed: " + e);
            }
            Report(sb);
        }

        // -------------------------------------------------------------------- flags
        public static void FlagGIBatch()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[BakeGI/gi-flags] {DateTime.Now:HH:mm:ss}");
            try
            {
                int marked = 0, skippedDynamic = 0, skippedOther = 0;
                var targets = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var root in targets)
                {
                    foreach (var ren in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!(ren is MeshRenderer)) { skippedOther++; continue; }

                        bool dynamic = false;
                        var t = ren.transform;
                        while (t != null)
                        {
                            var n = t.name;
                            foreach (var tok in SkipTokens)
                            {
                                if (n.IndexOf(tok, StringComparison.OrdinalIgnoreCase) >= 0)
                                { dynamic = true; break; }
                            }
                            if (dynamic) break;
                            t = t.parent;
                        }
                        if (dynamic) { skippedDynamic++; continue; }

                        var flags = GameObjectUtility.GetStaticEditorFlags(ren.gameObject);
                        if ((flags & StaticEditorFlags.ContributeGI) == 0)
                        {
                            GameObjectUtility.SetStaticEditorFlags(
                                ren.gameObject, flags | StaticEditorFlags.ContributeGI);
                            marked++;
                        }
                    }
                }

                Log(sb, $"static renderers marked ContributeGI: +{marked}  skipped(dynamic): {skippedDynamic}  skipped(other primitives): {skippedOther}");

                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                if (scene.IsValid()) EditorSceneManager.SaveOpenScenes();
                Log(sb, "[OK] scene saved");
            }
            catch (Exception e)
            {
                Log(sb, "[ERROR] gi-flags failed: " + e);
            }
            Report(sb);
        }

        // ------------------------------------------------------------------ lights
        public static void FlipLightsBatch()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[BakeGI/lights-baked] {DateTime.Now:HH:mm:ss}");
            try
            {
                var rig = GameObject.Find(RigRootName);
                if (rig == null)
                {
                    Log(sb, $"[MISS] no '{RigRootName}' root in the open scene — run rig-a first.");
                    Report(sb);
                    return;
                }

                var lights = rig.GetComponentsInChildren<Light>(true);
                int tableMixed = 0, roomBaked = 0, unchanged = 0;
                foreach (var l in lights)
                {
                    // The 12 table lights stay realtime-direct (Mixed) so the dynamic
                    // balls keep a light source — Mixed bakes nothing realtime-shadowy.
                    var isTableKey = l.name.StartsWith("TableKey_", StringComparison.Ordinal);
                    var want = isTableKey ? LightmapBakeType.Mixed : LightmapBakeType.Baked;
                    if (l.lightmapBakeType == want) { unchanged++; continue; }

                    l.lightmapBakeType = want;
                    if (isTableKey) tableMixed++; else roomBaked++;
                    Log(sb, $"  {l.name}: {l.type}, bakeType={l.lightmapBakeType}, shadows={l.shadows}");
                }

                Log(sb, $"lamps in rig: {lights.Length} -> TableKey->Mixed {tableMixed}, room->Baked {roomBaked}, unchanged {unchanged}");
                Log(sb, "[OK] lights flipped for bake (no realtime soft shadows remain)");
            }
            catch (Exception e)
            {
                Log(sb, "[ERROR] lights-baked failed: " + e);
            }
            Report(sb);
        }

        // --------------------------------------------------------------------- bake
        public static void BatchBake()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[BakeGI/bake] {DateTime.Now:HH:mm:ss}");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // 1) lighting settings asset
                var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
                bool created = settings == null;
                if (created) settings = new LightingSettings();

                settings.name = "147VR_MainScene_LightingSettings";
                settings.autoGenerate = false;               // bake is run explicitly
                settings.bakedGI = true;
                settings.realtimeGI = false;
                settings.ao = true;                          // contact AO replaces soft shadows
                settings.aoMaxDistance = 1.0f;
                settings.aoExponentDirect = 1;
                settings.aoExponentIndirect = 1;
                settings.albedoBoost = 1.0f;
                settings.lightmapResolution = 10;            // pixels/metre — Quest-friendly
                settings.lightmapMaxSize = 512;              // small atlases, 15.4 GB machine
                settings.bounces = 2;
                settings.minBounces = 1;
                settings.maxBounces = 2;
                settings.directSampleCount = 16;             // CPU-friendly but noise-free enough
                settings.indirectSampleCount = 512;
                settings.compressLightmaps = true;

                // Enums whose type placement varies per Unity version — set by reflection
                // so the batch can never fail to compile, just to a named catch below.
                SetEnumByReflection(sb, settings, "lightmapper",      "ProgressiveCPU", " (may fall back to whatever Unity defaults)");
                SetEnumByReflection(sb, settings, "lightmapCompression", "HighQuality", "");
                SetEnumByReflection(sb, settings, "directionalityMode",  "NonDirectional", "");
                SetEnumByReflection(sb, settings, "denoiserTypeDirect",  "None", "");
                SetEnumByReflection(sb, settings, "denoiserTypeIndirect","None", "");
                SetEnumByReflection(sb, settings, "denoiserTypeAO",      "None", "");

                if (created)
                {
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                    Log(sb, $"created lightings settings asset: {SettingsPath}");
                }
                else
                {
                    EditorUtility.SetDirty(settings);
                    Log(sb, $"updated existing lighting settings asset: {SettingsPath}");
                }

                Lightmapping.lightingSettings = settings;
                AssetDatabase.SaveAssets();

                // 2) lamps -> Mixed/Baked so the bake picks them up
                sb.AppendLine();
                sb.AppendLine($"[BakeGI/bake:lights] {DateTime.Now:HH:mm:ss}");
                FlipLightsTo(sb);   // reuse the flip logic, logging into the same report

                // 3) synchronous bake (batchmode-safe, Progressive CPU)
                sb.AppendLine();
                sb.AppendLine($"[BakeGI/bake:progressive] {DateTime.Now:HH:mm:ss}  baking — this can take 10-30+ min on a 15.4 GB machine");
                Debug.Log("[BakeGI] Lightmapping.Bake() starting (synchronous)...");
                Lightmapping.Bake();

                // 4) evidence
                var ldGuids = AssetDatabase.FindAssets("t:LightingDataAsset", new[] { "Assets/Scenes" });
                var ldPaths = ldGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)).ToList();
                foreach (var p in ldPaths)
                {
                    var dataAsset = AssetDatabase.LoadAssetAtPath<Object>(p);
                    Log(sb, $"[LD] {p}");
                    int atlas = AssetDatabase.LoadAllAssetsAtPath(p).Count(o => o is Texture2D tex && !string.IsNullOrEmpty(tex.name) && tex.width > 0);
                    if (atlas > 0) Log(sb, $"    atlases/textures embedded in this data asset: {atlas}");
                }
                if (ldPaths.Count == 0)
                    Log(sb, "[WARN] no LightingDataAsset found after bake — bake likely failed, check Unity log");

                Log(sb, $"[OK] bake finished in {sw.Elapsed.TotalMinutes:F1} min; scene = {Lightmapping.lightingDataAsset}");

                // 5) persist RenderSettings (references the lighting data asset) + lamp states
                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                if (scene.IsValid()) EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                Log(sb, "[OK] scene + assets saved");
            }
            catch (Exception e)
            {
                Log(sb, $"[ERROR] bake failed after {sw.Elapsed.TotalMinutes:F1} min: {e}");
            }
            Report(sb);
        }

        /// <summary>Flip logic shared by lights-baked and bake (pure, no heavy recursion).</summary>
        static void FlipLightsTo(StringBuilder sb)
        {
            var rig = GameObject.Find(RigRootName);
            if (rig == null)
            {
                Log(sb, $"[MISS] no '{RigRootName}' root in the open scene — run rig-a first.");
                return;
            }
            var lights = rig.GetComponentsInChildren<Light>(true);
            int tableMixed = 0, roomBaked = 0, unchanged = 0;
            foreach (var l in lights)
            {
                var isTableKey = l.name.StartsWith("TableKey_", StringComparison.Ordinal);
                var want = isTableKey ? LightmapBakeType.Mixed : LightmapBakeType.Baked;
                if (l.lightmapBakeType == want) { unchanged++; continue; }
                l.lightmapBakeType = want;
                if (isTableKey) tableMixed++; else roomBaked++;
            }
            Log(sb, $"lamps: {lights.Length} total -> TableKey Mixed {tableMixed}, room Baked {roomBaked}, unchanged {unchanged}");
        }

        /// <summary>Sets an enum-valued LightingSettings property without the name of the
        /// enum type hard-coded (Unity moves these enums between packages/versions).</summary>
        static void SetEnumByReflection(StringBuilder sb, LightingSettings target, string prop, string enumName, string note)
        {
            try
            {
                var pi = typeof(LightingSettings).GetProperty(prop);
                if (pi == null) throw new MissingMethodException($"no property {prop}");
                object value;
                try
                {
                    value = Enum.Parse(pi.PropertyType, enumName);
                    Log(sb, $"    {prop} = {enumName}");
                }
                catch (Exception)
                {
                    // Name not present in this Unity version's enum — fall back to
                    // member 0, the "safest" one (ProgressiveCPU / None / NonDirectional
                    // by documented field order).
                    value = Enum.ToObject(pi.PropertyType, 0);
                    Log(sb, $"    {prop}: name '{enumName}' not found, used enum value 0{note}");
                }
                pi.SetValue(target, value);
            }
            catch (Exception e)
            {
                Log(sb, $"    {prop}: could not set '{enumName}' ({e.Message}); left whatever Unity assigned{note}");
            }
        }

        static void Report(StringBuilder sb)
        {
            File.WriteAllText(ReportFile, sb.ToString());
        }
    }
}
#endif
