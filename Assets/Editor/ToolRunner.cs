#if UNITY_EDITOR
// -----------------------------------------------------------------------------
// Editor command runner — lets the CSV tools be driven without menu clicking.
//
// Why: Unity's IMGUI menus are hard to drive reliably from outside (popups lose
// capture when another window overlaps them), and every attempt costs the user a
// click. Instead, an external process writes Logs/editor_cmd.txt and the Editor
// executes those commands on the next domain reload / focus, then archives the
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
    [InitializeOnLoad]
    public static class ToolRunner
    {
        const string CmdFile     = "Logs/editor_cmd.txt";
        const string DoneFile    = "Logs/editor_cmd.last.txt";
        const string ResultFile  = "Logs/editor_cmd_result.txt";
        const string PhysicsFile = "Logs/editor_physics.txt";

        static int _waitFrames;

        static ToolRunner()
        {
            EditorApplication.delayCall += TryRun;
        }

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
            const string MainScene = "Assets/Scenes/147VR_MainScene.unity";
            if (!scene.IsValid() || scene.path != MainScene)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(MainScene,
                    UnityEditor.SceneManagement.OpenSceneMode.Single);
                Debug.Log($"[ToolRunner] opened {MainScene} for batchmode");
            }
            RunNow();
        }

        static void TryRun()
        {
            if (!File.Exists(CmdFile))
                return;

            // The scene may still be loading after a domain reload; give it a few ticks.
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.rootCount == 0)
            {
                if (_waitFrames++ > 40)
                {
                    Write(new StringBuilder("[ABORT] scene never finished loading; commands not run"));
                    Archive();
                    return;
                }
                EditorApplication.delayCall += TryRun;
                return;
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

// touched by ToolRunner queue so Unity recompiles and re-runs Logs/editor_cmd.txt

// round 8 step 2 queue: colour-only materials + 12 lamp lights

// round 9 queue
