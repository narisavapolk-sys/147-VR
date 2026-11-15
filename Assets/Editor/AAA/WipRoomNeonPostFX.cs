#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Adds a GLOBAL post-processing volume to the WipRoom scene so the HDR neon emission
    /// actually blooms and the image gets warm filmic contrast.
    ///
    /// WHY: the scene has NO global Volume, so the capture camera (and the game) render the
    /// neon's HDR _EmissionColor as flat tubes with no halo and a cold/clipped tone. The fix
    /// is a global Volume bound to the existing ClubVolumeProfile (Tonemapping=ACES, Bloom
    /// threshold 1 / intensity 0.25 / scatter 0.7, ColorAdjustments, Vignette). Bloom needs
    /// HDR headroom, which PC_RPAsset (the active quality level) already provides.
    ///
    /// Owner-click menu. Marks the scene DIRTY but does NOT auto-save (project convention:
    /// the owner reviews, then saves).
    /// </summary>
    public static class WipRoomNeonPostFX
    {
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string ProfilePath = "Assets/Settings/ClubVolumeProfile.asset";
        private const string RootName = "147VR_PostFX_GlobalVolume";
        private const string ReportPath = "Logs/WipRoom_PostFX.txt";

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/31) Add Global Bloom + ACES Volume (WipRoom)")]
        public static void MenuRun()
        {
            Run();
            EditorUtility.DisplayDialog("WipRoom Post-FX",
                "Report: " + ReportPath + "\n\nSave the scene, then re-run menu 30 to re-render with bloom.", "OK");
        }

        /// <summary>ToolRunner entry point: "wiproom-postfx".</summary>
        public static void Run()
        {
            var log = new StringBuilder();
            log.AppendLine("WIPROOM POST-FX - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                log.AppendLine("REFUSED: loaded scene is '" + scene.path + "' (must be WipRoom).");
                Write(log);
                return;
            }

            // HDR headroom check: bloom only fires when emission exceeds the threshold (1.0).
            var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            int q = QualitySettings.GetQualityLevel();
            if (rp != null)
                log.AppendLine("Active pipeline HDR support: " + rp.supportsHDR +
                               "  (quality " + q + " '" + QualitySettings.names[q] + "')");
            else
                log.AppendLine("WARNING: GraphicsSettings.currentRenderPipeline is null (not URP?).");
            if (rp != null && !rp.supportsHDR)
                log.AppendLine("  HDR is OFF -> emission clamps to 1.0 and bloom never fires. " +
                               "Switch Quality to the PC_RPAsset level (HDR=1) before expecting glow.");

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                log.AppendLine("REFUSED: profile not found at " + ProfilePath);
                Write(log);
                return;
            }

            // Idempotent: replace a previous global volume we added instead of stacking.
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                log.AppendLine("  removed the previous " + RootName + " (re-add, not a duplicate)");
            }

            var go = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(go, "Add Global PostFX Volume");
            go.transform.SetParent(null, false);
            var vol = go.AddComponent<Volume>();
            Undo.RecordObject(vol, "Global Volume");
            vol.isGlobal = true;
            vol.priority = 1000f;
            vol.weight = 1f;
            // sharedProfile is the ONLY serialized field. Setting vol.profile populates just the
            // runtime instance, so the reference is lost on save (scene reloads with
            // sharedProfile: {fileID: 0} and no bloom). Set sharedProfile and nothing else.
            vol.sharedProfile = profile;
            log.AppendLine("  added global Volume bound to " + profile.name +
                           " (sharedProfile=" + (vol.sharedProfile != null ? "SET" : "NULL") + ", priority 1000)");

            foreach (var c in profile.components)
                log.AppendLine("    component: " + c.GetType().Name + "  active=" + c.active);

            // Make sure the scene cameras actually run post-processing with HDR.
            int cams = 0;
            foreach (var cam in GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                Undo.RecordObject(data, "Camera PostFX");
                data.renderPostProcessing = true;
                cam.allowHDR = true;
                cams++;
            }
            log.AppendLine("  cameras forced renderPostProcessing + allowHDR ON: " + cams);

            EditorSceneManager.MarkSceneDirty(scene);
            log.AppendLine("Scene marked DIRTY (NOT saved). Save it, then re-run menu 30 to re-render.");
            Write(log);
        }

        private static void Write(StringBuilder log)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, log.ToString());
            Debug.Log("[WipRoomPostFX] done -> " + ReportPath);
        }
    }
}
#endif
