#if UNITY_EDITOR
// -----------------------------------------------------------------------------
// Editor command runner — queued commands are manual-only and require explicit confirmation.
//
// Why: Unity's IMGUI menus are hard to drive reliably from outside (popups lose
// capture when another window overlaps them), and every attempt costs the user a
// click. Instead, an external process writes Logs/editor_cmd.txt and the Editor
// executes those commands only after an explicit owner action, then archives the
// file and writes Logs/editor_cmd_result.txt.
//
// Commands (one per line, '#' starts a comment):
//   material-dump     dump renderer/material inventory + texture catalogue
//   material-map      auto-match materials to .fbm textures -> Logs/material_map.csv
//   material-apply    rebind to URP Lit (scene overrides) + save scene
//   material-revert   put every rebound renderer back on its prefab material
//   rig-a             apply light variant A (no save)
//   rig-a-save        apply light variant A + save scene
//   rig-b-save        apply light variant B + save scene
//   rig-capture       render the parity screenshots
//   preflight         run the Script Verification & Quest Build Preflight
//   reload-physics    force the authority director to re-collect bays (play mode)
//   uv2-ensure        ensure ModelImporter.generateLightmapUVs on imported models
//   gi-flags          mark static renderers Contribute GI + save scene
//   lights-baked      flip LightRig lamps to Mixed (TableKey) / Baked (room)
//   bake              create lighting settings asset + flip lamps + Lightmapping.Bake
//   bay2-frame        replace Bay2 frame + save WipRoom and capture previews
//   ceiling-parity    rebind coffer materials + save WipRoom and capture preview
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VR147.EditorTools
{
    public static class ToolRunner
    {
        const string CmdFile     = "Logs/editor_cmd.txt";
        const string DoneFile    = "Logs/editor_cmd.last.txt";
        const string ResultFile  = "Logs/editor_cmd_result.txt";
        const string PhysicsFile = "Logs/editor_physics.txt";
        /// <summary>Batchmode entry point:
        /// Unity -batchmode -projectPath &lt;proj&gt; -executeMethod VR147.EditorTools.ToolRunner.RunCommands -quit
        /// Runs whatever is queued in Logs/editor_cmd.txt right away (no delayCall waiting).</summary>
        public static void RunCommands()
        {
            if (!File.Exists(CmdFile))
            {
                Debug.Log($"[ToolRunner] nothing queued ({CmdFile} missing). Last run: " +
                          (File.Exists(ResultFile) ? File.ReadAllText(ResultFile) : "none"));
                return;
            }

            var scene = SceneManager.GetActiveScene();
            const string MainScene = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
            if (!scene.IsValid() || scene.path != MainScene)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(MainScene,
                    UnityEditor.SceneManagement.OpenSceneMode.Single);
                Debug.Log($"[ToolRunner] opened {MainScene} for batchmode");
            }
            RunNow();
        }

        [MenuItem("147VR/Tools/Run Queued Editor Commands (Confirm)")]
        public static void RunQueuedCommandsFromMenu()
        {
            if (!File.Exists(CmdFile))
            {
                EditorUtility.DisplayDialog("ToolRunner", "No queued commands in " + CmdFile, "OK");
                return;
            }

            var lines = File.ReadAllLines(CmdFile)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("#"))
                .ToList();
            if (lines.Count == 0)
            {
                EditorUtility.DisplayDialog("ToolRunner", "The command queue is empty.", "OK");
                return;
            }

            string queue = string.Join("\n", lines.Select(line => "  - " + line));
            bool confirmed = EditorUtility.DisplayDialog(
                "Run queued Unity commands?",
                "Commands:\n" + queue +
                "\n\nSome commands can modify/save WipRoom, materials, lighting settings, or build assets. " +
                "Nothing runs on domain reload anymore. Continue only if these exact commands are intended.",
                "Run Commands", "Cancel");
            if (!confirmed) return;

            const string productionScene = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, productionScene, StringComparison.OrdinalIgnoreCase))
            {
                if (scene.IsValid() && scene.isDirty)
                {
                    bool save = EditorUtility.DisplayDialog(
                        "Unsaved scene",
                        "The active scene has unsaved changes. Save it before switching to WipRoom?",
                        "Save & Open WipRoom", "Cancel");
                    if (!save) return;
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                }
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    productionScene, UnityEditor.SceneManagement.OpenSceneMode.Single);
            }

            RunNow();
        }
        static void RunNow()
        {
            var log = new StringBuilder();
            log.AppendLine($"[ToolRunner] {DateTime.Now:HH:mm:ss}  scene='{SceneManager.GetActiveScene().path}' roots={SceneManager.GetActiveScene().rootCount}");
            var lines = File.ReadAllLines(CmdFile).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")).ToList();
            Archive();
            MaterialPass.NoDialog = true;
            LightRigAB.NoDialog = true;
            _147VR.EditorTools.WipRoomArtFollowups.SilenceDialogs = true;

            foreach (string cmd in lines)
            {
                log.AppendLine($"-> {cmd}");
                try
                {
                    switch (cmd)
                    {
                        case "material-dump":  MaterialPass.Dump(); break;
                        case "material-map":   MaterialPass.AutoMap(); break;
                        case "material-apply": MaterialPass.ApplyRebind(true); break;
                        case "material-revert":MaterialPass.RevertOverrides(); break;
                        case "rig-a":          LightRigAB.ApplyVariant("A", false); break;
                        case "rig-a-save":     LightRigAB.ApplyVariant("A", true); break;
                        case "rig-b-save":     LightRigAB.ApplyVariant("B", true); break;
                        case "rig-capture":    LightRigAB.CaptureNow(); break;
                        case "preflight":      _147VR.QA.PhysicsAuthorityPreflight.Run(); break;
                        case "reload-physics": File.WriteAllText(PhysicsFile, DateTime.Now.ToString("HH:mm:ss")); break;
                        case "uv2-ensure":     BakeGI.EnsureUV2Batch(); break;
                        case "gi-flags":       BakeGI.FlagGIBatch(); break;
                        case "lights-baked":   BakeGI.FlipLightsBatch(); break;
                        case "bake":           BakeGI.BatchBake(); break;
                        case "aaa-audit":      _147VR.EditorTools.NeonVintageAudit.Run(); break;
                        case "aaa-prepare-assets": _147VR.EditorTools.NeonVintageArtPassImporter.MenuPrepareAssets(); break;
                        case "wiproom-visual-audit": _147VR.EditorTools.WipRoomVisualAudit.Run(); break;
                        case "art-followups": _147VR.EditorTools.WipRoomArtFollowups.Run(); break;
                        case "emission-fix": _147VR.EditorTools.WipRoomArtFollowups.RunEmissionFix(); break;
                        case "art-capture": _147VR.EditorTools.WipRoomArtFollowups.RunCapture(); break;
                        case "art-fix-round2": _147VR.EditorTools.WipRoomArtFollowups.RunArtFixRound2(); break;
                        case "lamp-audit": _147VR.EditorTools.WipRoomArtFollowups.RunLampAudit(); break;
                        case "art-round3-audit": _147VR.EditorTools.WipRoomArtRound3.RunAudit(); break;
                        case "art-round3": _147VR.EditorTools.WipRoomArtRound3.Run(); break;
                        case "flag-gap": _147VR.EditorTools.WipRoomArtRound3.RunFlagGap(); break;
                        case "flag-verify": _147VR.EditorTools.WipRoomArtRound3.RunFlagVerify(); break;
                        case "sprint23": _147VR.EditorTools.WipRoomArtRound3.RunSprint23(); break;
                        case "frame-reuv": _147VR.EditorTools.WipRoomArtRound3.RunFrameReUv(); break;
                        case "sprint4": _147VR.EditorTools.WipRoomArtSprint4.Run(); break;
                         case "neon-import": _147VR.EditorTools.NeonSignImporter.Run(); break;
                         case "bay2-frame": _147VR.EditorTools.PlaceBay2FrameClean.RunBatch(); break;
                         case "ceiling-parity": _147VR.EditorTools.WipRoomCeilingParity.RunBatch(); break;
                        default: log.AppendLine("   [skip] unknown command"); break;
                    }
                }
                catch (Exception e)
                {
                    log.AppendLine($"   [ERROR] {e.GetType().Name}: {e.Message}");
                }
            }

            // The individual tools write their own reports; this one only records the dispatch.
            Write(log);
            Debug.Log($"[ToolRunner] ran {lines.Count} command(s); see {ResultFile}");
        }

        static void Archive()
        {
            try
            {
                // Never delete: each run is kept as a timestamped record so the history is auditable.
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string target = $"Logs/editor_cmd_{stamp}.last.txt";
                int n = 0;
                while (File.Exists(target)) target = $"Logs/editor_cmd_{stamp}_{++n}.last.txt";
                File.Move(CmdFile, target);
                File.WriteAllText(DoneFile, target);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ToolRunner] could not archive {CmdFile}: {e.Message}");
            }
        }

        static void Write(StringBuilder log)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ResultFile, log.ToString());
            foreach (var line in log.ToString().Split('\n'))
                Debug.Log(line.TrimEnd());
        }
    }
}
#endif

// Queue commands are explicit; domain reload is inert.
