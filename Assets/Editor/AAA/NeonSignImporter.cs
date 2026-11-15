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
    /// Places the converted neon sign assembly in the WipRoom scene.
    ///
    /// WHY THIS EXISTS: the "147 VR" logo, the neon rings, the back-wall logo pass and the WELCOME
    /// sign are Blender FONT and CURVE objects. FBX export and Unity's importer do not carry those
    /// types, so the text and the tubes were silently dropped and only the flat backing plate
    /// arrived. A headless Blender pass converted them to real polygons; this script brings them in.
    ///
    /// DESIGN CHOICE: the 23-object assembly FBX keeps every piece at its authored world position,
    /// so the whole thing is instantiated ONCE at the origin instead of placing objects by hand -
    /// that avoids double-applying a transform that is already baked in. The pass then REPORTS where
    /// each piece landed and compares it against the Blender world coordinates converted to Unity.
    ///
    /// COORDINATE CONVENTION (verified 2026-11-14 against the .blend ground truth, md5
    /// 550BE78F3D8D6126ED0619FAD9D8CE77): Blender (x_b, y_b, Z-up) -> Unity (-x_b, z_b, -y_b).
    /// Proof anchors: SM_Stage_Deck .blend x=-10.9 <-> Unity +10.9; LiveStage backdrop .blend
    /// x=-12.4 <-> Unity stage wall +12.4; entrance valance .blend (0.54, -20.94, 0) <-> the
    /// +Z entrance wall. The hero neon is a STAGE landmark: .blend (-12.31, -3.10, 2.22) ->
    /// Unity (12.31, 2.22, 3.10) on the +X stage wall. The 2026-11-13 run reported 18/19
    /// "misplaced" only because this table used the WRONG conversion (x_b, z_b, -y_b) and, for the
    /// LOGO_PASS cluster, the LOCAL coords of parented objects instead of their world coords.
    ///
    /// Materials come from existing rip assets by name. Nothing here invents a colour - Ruby, Ivory
    /// and Gold Hot already carry authored HDR emission.
    /// </summary>
    public static class NeonSignImporter
    {
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string AssemblyFbx = "Assets/AAA/Imports/NeonVintageArtPass/147VR_Converted_Fonts_And_Curves.fbx";
        private const string MinimalFbx = "Assets/AAA/Imports/NeonSign/147VR_Neon_Signs_Converted.fbx";
        private const string ReportPath = "Logs/NeonSign_Import.txt";
        private const string RootName = "147VR_NeonSigns_Imported";

        /// <summary>Placement is only CHECKED against these, never forced. Blender world
        /// (x_b, y_b, Z-up) to Unity (-x_b, z_b, -y_b) - see the class doc for the proof.</summary>
        private static readonly (string NameContains, Vector3 Expected, string Material)[] Expectations =
        {
            // Hero cluster - stage landmark on the +X stage wall (Unity side).
            ("Neon_Hero_147",          new Vector3( 12.31f, 2.22f,  3.10f), "147VR_MAT_Neon_Ruby_Hot"), // matches Hero + Hero_Core
            ("Neon_Sub_VR",            new Vector3( 12.31f, 1.72f,  3.10f), "147VR_MAT_Neon_Gold_Hot"),
            ("InnerRing_Ruby",         new Vector3( 12.32f, 2.05f,  3.10f), "147VR_MAT_Neon_Ruby_Hot"),
            ("OuterRing_Gold",         new Vector3( 12.32f, 2.05f,  3.10f), "147VR_MAT_Neon_Gold_Hot"),
            ("Neon_Backing",           new Vector3( 12.35f, 2.05f,  3.10f), "147VR_MAT_Neon_Ruby_Hot"),
            ("Chassis_BrassRim",       new Vector3( 12.35f, 2.05f,  3.10f), "147VR_Mat_GoldCarved"),
            ("Neon_AccentBall_0",      new Vector3( 12.32f, 1.72f,  3.51f), "147VR_MAT_Neon_Ivory_Hot"),
            ("Neon_AccentBall_1",      new Vector3( 12.28f, 2.48f,  4.11f), "147VR_MAT_Neon_Ivory_Hot"),
            ("Neon_AccentBall_2",      new Vector3( 12.24f, 1.72f,  2.18f), "147VR_MAT_Neon_Ivory_Hot"),
            // Logo pass - .blend WORLD coords (parent 147VR_NeonLogo_Root at (-9.66, 4.89, 0.67)).
            ("LOGO_PASS_CenterInner",  new Vector3(  9.67f, 0.71f, -4.89f), "147VR _ Ruby Neon"),
            ("LOGO_PASS_CenterMain",   new Vector3(  9.65f, 0.63f, -4.82f), "147VR _ Royal Gold"),
            ("LOGO_PASS_CenterOuter",  new Vector3(  9.67f, 0.71f, -4.89f), "147VR _ Royal Gold"),
            ("LOGO_PASS_CenterTag",    new Vector3(  9.65f, 0.39f, -4.89f), "147VR _ Royal Gold"),
            ("LOGO_PASS_CenterVR",     new Vector3(  9.65f, 0.51f, -4.89f), "147VR _ Warm Ivory"),
            ("Welcome_Sign",           new Vector3(  0.04f, 2.63f, 14.49f), "147VR_Mat_WelcomeGold"),
            ("ValanceGoldHem",         new Vector3( -0.54f, 0.00f, 20.94f), "147VR_Mat_GoldCarved"),
            ("ValanceTopGold",         new Vector3( -0.54f, 0.00f, 20.94f), "147VR_Mat_GoldCarved"),
            // Parked at the .blend origin; Run() moves it to the L-finial bracket before checking.
            ("StageCurtain_OpenGathered_GoldRod", new Vector3(0f, 4.55f, -13.5f), "147VR_Mat_GoldCarved"),
        };

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/30) Import Neon Signs (verify + save on pass)")]
        public static void MenuImport()
        {
            Run();
            EditorUtility.DisplayDialog("Neon Sign Import",
                "Report: " + ReportPath + "\n\nScene SAVED if every position check passed - check the renders.", "OK");
        }

        /// <summary>ToolRunner entry point: "neon-import".</summary>
        public static void Run()
        {
            var log = new StringBuilder();
            log.AppendLine("NEON SIGN IMPORT - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                log.AppendLine("REFUSED: loaded scene is '" + scene.path + "'");
                Write(log);
                return;
            }

            string fbxPath = File.Exists(AssemblyFbx) ? AssemblyFbx : MinimalFbx;
            log.AppendLine("FBX source: " + fbxPath +
                           (fbxPath == AssemblyFbx ? " (23-object assembly)" : " (12-object fallback)"));

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (asset == null)
            {
                log.AppendLine("REFUSED: Unity has not imported this FBX yet. Open the project once so it imports.");
                Write(log);
                return;
            }

            // Idempotent: replace a previous import instead of stacking duplicates.
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                log.AppendLine("  removed the previous " + RootName + " (re-import, not a duplicate)");
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = RootName;
            Undo.RegisterCreatedObjectUndo(instance, "Import Neon Signs");
            instance.transform.SetParent(null, false);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            log.AppendLine("  instantiated, root children=" + instance.transform.childCount);

            // The stage-curtain rod is PARKED at the .blend origin (the OpenGathered set is
            // half-placed there: L finial at .blend (-3.76, 13.5, 4.55), R finial missing).
            // Move it to the finial bracket line instead of importing a 6.9 m rod on the floor.
            Transform rod = instance.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name.IndexOf("StageCurtain_OpenGathered_GoldRod",
                                                   StringComparison.OrdinalIgnoreCase) >= 0);
            if (rod != null)
            {
                Vector3 bracket = new Vector3(0f, 4.55f, -13.5f);
                log.AppendLine("  GoldRod: .blend parks it at the origin; moved to the L-finial bracket " +
                               Fmt(bracket) + " (R finial absent in the .blend - adjust by hand if wrong)");
                rod.position = bracket;
            }
            else log.AppendLine("  GoldRod: not present in this FBX");

            int checkedCount = 0, misplaced = 0, matAssigned = 0, matMissing = 0, renderers = 0;

            foreach (Transform t in instance.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject == instance) continue;
                Renderer rend = t.GetComponent<Renderer>();
                if (rend == null) continue;
                renderers++;

                Vector3 world = t.position;
                string note = "";
                (string, Vector3, string)? match = ExpectationFor(t.name);
                if (match.HasValue)
                {
                    float dist = Vector3.Distance(world, match.Value.Item2);
                    checkedCount++;
                    if (dist > 0.5f) { misplaced++; note = "  *** " + dist.ToString("F2") +
                                                            " m off, expected " + Fmt(match.Value.Item2); }
                    else note = "  ok (" + dist.ToString("F2") + " m from expected)";
                }

                if (match.HasValue)
                {
                    Material m = FindMaterial(match.Value.Item3);
                    if (m != null)
                    {
                        Undo.RecordObject(rend, "Neon Material");
                        var slots = new Material[Mathf.Max(1, rend.sharedMaterials.Length)];
                        for (int i = 0; i < slots.Length; i++) slots[i] = m;
                        rend.sharedMaterials = slots;
                        matAssigned++;
                    }
                    else matMissing++;
                }

                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;

                MeshFilter mf = t.GetComponent<MeshFilter>();
                log.AppendLine("  " + t.name + " world=" + Fmt(world) + " scale=" + Fmt(t.lossyScale) +
                               " verts=" + (mf != null && mf.sharedMesh != null ? mf.sharedMesh.vertexCount : 0) +
                               note);
            }

            log.AppendLine();
            log.AppendLine("VERDICT: renderers=" + renderers + " position-checked=" + checkedCount +
                           " misplaced=" + misplaced + " materialsAssigned=" + matAssigned +
                           " materialsMissing=" + matMissing);
            log.AppendLine(misplaced == 0
                ? "  The FBX carries the authored world positions, so instantiating at the origin is correct."
                : "  SOME PIECES ARE NOT WHERE THE .BLEND PUTS THEM - do not trust this placement yet.");

            EditorSceneManager.MarkSceneDirty(scene);
            CaptureStageSign(log);
            // Head-on closeup of the WELCOME sign (was a ~17.9m wide hall shot that hid any glow).
            Capture(log, "Logs/renders/61-neon-hall-from-entrance.png",
                     new Vector3(0.04f, 2.63f, 17.0f), new Vector3(0.04f, 2.63f, 14.49f));
            Capture(log, "Logs/renders/62-neon-logopass.png",
                     new Vector3(5.5f, 1.7f, -8.0f), new Vector3(9.67f, 0.6f, -4.89f));

            if (misplaced == 0 && renderers > 0)
            {
                bool saved = EditorSceneManager.SaveScene(scene);
                log.AppendLine(saved
                    ? "SCENE SAVED: the neon assembly is now on disk (git has the pre-import state)."
                    : "SAVE FAILED - scene still dirty, save by hand after checking the render.");
            }
            else
            {
                log.AppendLine("Scene left DIRTY and NOT saved (verification did not pass). Ctrl+Z reverts.");
            }
            Write(log);
        }

        private static (string, Vector3, string)? ExpectationFor(string objectName)
        {
            foreach (var e in Expectations)
                if (objectName.IndexOf(e.NameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    return (e.NameContains, e.Expected, e.Material);
            return null;
        }

        private static Material FindMaterial(string matName)
        {
            string[] folders = {
                "Assets/AAA/Imports/NeonVintageArtPass/Materials/",
                "Assets/AAA/Imports/naris_vintage/Rebound/",
                "Assets/AAA/ImportedSnooker/Materials/V008/"
            };
            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.Equals(Path.GetFileNameWithoutExtension(p), matName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                    if (m != null) return m;
                }
            }
            return null;
        }

        private static void CaptureStageSign(StringBuilder log)
        {
            // Hero closeup: the stage landmark is on the +X stage wall (Unity side).
            Vector3 focus = new Vector3(12.31f, 2.05f, 3.10f);
            // Tighter head-on closeup so a real glow reads even at low bloom (was eye=focus+(-4.5,0.6,2.5) ~5.2m).
            Capture(log, "Logs/renders/60-neon-stage-sign.png", focus + new Vector3(-2.6f, 0.4f, 1.5f), focus);
        }

        private static void Capture(StringBuilder log, string file, Vector3 eye, Vector3 focus)
        {
            var camGo = new GameObject("NeonSign Capture Camera");
            try
            {
                Directory.CreateDirectory("Logs/renders");
                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.allowHDR = true;
                cam.fieldOfView = 55f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 120f;
                var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (data == null)
                    data = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.renderShadows = true;

                camGo.transform.SetPositionAndRotation(eye,
                    Quaternion.LookRotation((focus - eye).normalized, Vector3.up));

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

                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(file, bytes);
                log.AppendLine("CAPTURE " + file + " (" + bytes.Length + " bytes, eye=" + Fmt(eye) +
                               " focus=" + Fmt(focus) + ")");
                UnityEngine.Object.DestroyImmediate(tex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
            catch (Exception e) { log.AppendLine("CAPTURE error (" + file + "): " + e.Message); }
            finally { UnityEngine.Object.DestroyImmediate(camGo); }
        }

        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";

        private static void Write(StringBuilder log)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, log.ToString());
            Debug.Log("[NeonSign] done -> " + ReportPath);
        }
    }
}
#endif
