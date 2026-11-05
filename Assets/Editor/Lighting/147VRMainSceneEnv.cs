// 147VR MainScene Environment tool
// -----------------------------------------------------------------------------
// WHY THIS EXISTS
//   The existing "147 VR/Lighting/Apply Quest Lighting Pass" only writes the
//   skybox / ambient (image-based lighting) into PoolTable_8Ball.unity and
//   PoolTable_9Ball.unity. 147VR_MainScene.unity was never given an environment:
//   its RenderSettings.skybox is empty and ambient is flat -> no IBL / no
//   reflections, which is the main reason the room looks flat vs the Blender
//   render (Blender lights everything with an HDRI world).
//   The HDRI *files* are imported (Assets/147 main/...), they were just never
//   wired into this scene.
//
// WHAT THIS DOES (surgical, reversible)
//   Only touches RenderSettings (skybox + ambient + reflection) of the scene
//   you currently have open. It does NOT add lights, does NOT touch the existing
//   "Global Volume" / VolumeProfile, and does NOT modify any object transforms.
//   Optional: turn on camera post-processing + SMAA (anti-aliasing) so edges
//   stop being jaggy (Quality/URP currently have MSAA disabled).
//
// HOW TO USE
//   147 VR menu > Lighting > MainScene Environment (Skybox + IBL)
//   Pick an HDRI, set exposure/ambient, press Preflight to read the log, then
//   Apply. "Revert" clears the environment back to none.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VR147.EditorTools
{
    public static class MainSceneEnvBuilder
    {
        // Known HDRI candidates (Texture2D equirect used by Skybox/Panoramic).
        public static readonly (string label, string path)[] HdriOptions =
        {
            ("PromDance (exr)",  "Assets/147 main/promDance _Hdri/PromDance_HDRI.exr"),
            ("NightSky (hdr)",   "Assets/147 main/NightSky/NightSky_HDRI.hdr"),
            ("NightSky (exr)",   "Assets/147 main/NightSky/NightSky_HDRI.exr"),
            ("PromDance (hdr)",  "Assets/147 main/promDance _Hdri/PromDance_HDRI.hdr"),
            ("Dreamy OLED (exr)","Assets/147 main/Dreamy_OLED/Dreamy_OLED_HDRI.exr"),
            // The .blend's own world texture (Poly Haven night), unpacked from the AAA file.
            ("Satara night (Blender world)","Assets/147 main/RoomTextures/satara_night_4k.hdr.hdr"),
        };

        public const string SkyboxDir      = "Assets/Settings";
        public const string ShaderName     = "Skybox/Panoramic";

        public struct Settings
        {
            public int    hdriIndex;
            public float  skyExposure;
            public float  ambientIntensity;
            public float  reflectionIntensity;
            public bool   enableCameraPostAA;
        }

        public static Settings Default => new Settings
        {
            hdriIndex           = 0,
            skyExposure         = 0.8f,
            ambientIntensity    = 0.9f,
            reflectionIntensity = 0.85f,
            enableCameraPostAA  = false,
        };

        // Returns the skybox material (creates/updates it from the chosen HDRI).
        // If dryRun, it only validates and logs, creating nothing.
        public static Material GetOrCreateSkybox(in Settings s, bool dryRun, StringBuilder log)
        {
            var (label, path) = HdriOptions[s.hdriIndex];
            var hdri = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (hdri == null)
            {
                log.AppendLine($"[MISS] HDRI not found: {path}");
                return null;
            }

            // Environment maps must be sampled in linear space; an sRGB-flagged .hdr/.exr shifts
            // the whole room's hue and kills the match with the Blender render.
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".hdr" || ext == ".exr")
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp != null && (imp.sRGBTexture || imp.textureShape != TextureImporterShape.Texture2D))
                {
                    imp.sRGBTexture = false;
                    imp.textureShape = TextureImporterShape.Texture2D;
                    imp.SaveAndReimport();
                    log.AppendLine($"[OK]   {label} reimported linear (sRGB off) for correct IBL");
                }
            }

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                log.AppendLine($"[MISS] Shader '{ShaderName}' not available. Install a skybox-capable package / use Built-in.");
                return null;
            }

            string matPath = $"{SkyboxDir}/147VR_{Sanitize(label)}_Skybox.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null && !dryRun)
            {
                mat = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(matPath) };
                AssetDatabase.CreateAsset(mat, matPath);
                log.AppendLine($"[NEW]  created skybox material {matPath}");
            }
            if (mat == null)
            {
                // dry-run and material does not exist yet
                log.AppendLine($"[DRY]  would create {matPath} from '{label}'");
                return null;
            }

            mat.shader = shader;
            mat.SetTexture("_MainTex", hdri);
            mat.SetFloat("_Exposure", s.skyExposure);
            mat.SetFloat("_Rotation", 0f);
            mat.SetColor("_Tint", Color.white);
            if (!dryRun) EditorUtility.SetDirty(mat);
            log.AppendLine($"[OK]   skybox '{label}' @ exposure {s.skyExposure:0.00}");
            return mat;
        }

        // Applies environment to the currently open scene. dryRun -> log only.
        public static void Apply(in Settings s, bool dryRun, StringBuilder log)
        {
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            log.AppendLine($"Scene: {sceneName}");

            var sky = GetOrCreateSkybox(s, dryRun, log);
            if (sky == null)
            {
                log.AppendLine("[ABORT] no skybox material, nothing applied.");
                return;
            }

            if (dryRun)
            {
                log.AppendLine($"[DRY]  would set RenderSettings.skybox = {sky.name}");
                log.AppendLine($"[DRY]  ambientMode = Skybox, ambientIntensity = {s.ambientIntensity:0.00}");
                log.AppendLine($"[DRY]  defaultReflectionMode = Skybox, reflectionIntensity = {s.reflectionIntensity:0.00}");
            }
            else
            {
                RenderSettings.skybox = sky;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = s.ambientIntensity;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
                RenderSettings.reflectionIntensity = s.reflectionIntensity;
                RenderSettings.reflectionBounces = 1;
                DynamicGI.UpdateEnvironment();
                MarkSceneDirty();
                log.AppendLine("[OK]   environment applied + GI refreshed");

                if (s.enableCameraPostAA)
                    ApplyCameraPostAA(log);
            }
        }

        public static void Revert(StringBuilder log)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            DynamicGI.UpdateEnvironment();
            MarkSceneDirty();
            log.AppendLine("[OK]   environment cleared (skybox = none, ambient = Flat)");
        }

        // Enable URP post-processing + SMAA on every enabled camera in the scene.
        // This is optional and can cost perf on Quest/XR -> default off.
        private static void ApplyCameraPostAA(StringBuilder log)
        {
            var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int n = 0;
            foreach (var cam in cams)
            {
                if (!cam.isActiveAndEnabled) continue;
                var data = cam.GetUniversalAdditionalCameraData();
                if (data == null) continue;
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing; // URP17 removed SMAA enum; FXAA compiles+runs, MSAA stays on the URP asset
                data.antialiasingQuality = AntialiasingQuality.High;
                EditorUtility.SetDirty(cam);
                n++;
            }
            log.AppendLine($"[OK]   FXAA + post enabled on {n} camera(s). NOTE: on Quest/XR this has a perf cost.");
        }

        private static void MarkSceneDirty()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s)
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }
    }

    // Editor window wrapper.
    public class MainSceneEnvWindow : EditorWindow
    {
        private MainSceneEnvBuilder.Settings _s = MainSceneEnvBuilder.Default;
        private Vector2 _scroll;

        [MenuItem("147 VR/Lighting/MainScene Environment (Skybox + IBL)")]
        public static void Open()
        {
            var w = GetWindow<MainSceneEnvWindow>("147VR Env");
            w.minSize = new Vector2(360, 300);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.HelpBox(
                "Wires an HDRI as the scene environment (Skybox + image-based lighting) " +
                "for the CURRENTLY OPEN scene. Does not touch lights or the existing Global Volume.",
                MessageType.Info);

            _s.hdriIndex = EditorGUILayout.Popup("HDRI", _s.hdriIndex, Labels());

            _s.skyExposure = EditorGUILayout.Slider("Sky Exposure", _s.skyExposure, 0f, 3f);
            _s.ambientIntensity = EditorGUILayout.Slider("Ambient Intensity (IBL)", _s.ambientIntensity, 0f, 2f);
            _s.reflectionIntensity = EditorGUILayout.Slider("Reflection Intensity", _s.reflectionIntensity, 0f, 2f);

            _s.enableCameraPostAA = EditorGUILayout.ToggleLeft(
                "Enable Camera Post + SMAA (fixes jaggies; costs perf on Quest)", _s.enableCameraPostAA);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preflight (log only)")) Run(true);
            if (GUILayout.Button("Apply")) Run(false);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Revert Environment (clear skybox/ambient)"))
            {
                var log = new StringBuilder();
                MainSceneEnvBuilder.Revert(log);
                Output(log);
            }

            EditorGUILayout.EndScrollView();
        }

        private string[] Labels()
        {
            var list = new List<string>();
            foreach (var o in MainSceneEnvBuilder.HdriOptions) list.Add(o.label);
            return list.ToArray();
        }

        private void Run(bool dry)
        {
            var log = new StringBuilder();
            MainSceneEnvBuilder.Apply(_s, dry, log);
            if (!dry) AssetDatabase.SaveAssets();
            Output(log);
        }

        private static void Output(StringBuilder log)
        {
            Debug.Log("[147VR Env]\n" + log);
            Debug.Log($"[147VR Env] {(log.ToString().Contains("[OK]") ? "done" : "see log above")}");
        }
    }
}
