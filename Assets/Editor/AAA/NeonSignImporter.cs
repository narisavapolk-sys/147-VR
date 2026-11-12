#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Imports the converted neon sign FBX and places the objects in the WipRoom scene.
    /// The Font/Curve objects were invisible in Unity because FBX import skips them.
    /// This script brings them back as proper meshes with emissive materials.
    /// </summary>
    public static class NeonSignImporter
    {
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string FbxPath = "Assets/AAA/Imports/NeonSign/147VR_Neon_Signs_Converted.fbx";
        private const string ReportPath = "Logs/NeonSign_Import.txt";

        /// <summary>
        /// Each entry: FBX child name -> Unity world position, rotation, material name to assign.
        /// Positions converted from Blender (Y-up) to Unity (Y-up, -Z forward).
        /// </summary>
        private static readonly (string FbxChildName, Vector3 Position, Quaternion Rotation, string MaterialName, string Label)[] Placements =
        {
            // === STAGE NEON SIGN (x=-12.31, the -X wall near the stage) ===
            ("147VR_Neon_Hero_147",
                new Vector3(-12.31f, 2.22f, 3.10f),
                Quaternion.Euler(0f, -90f, 0f),  // face into room from -X wall
                "147VR_MAT_Neon_Ruby_Hot",
                "Stage: '147' text (Ruby)"),

            ("147VR_Neon_Hero_147_Core",
                new Vector3(-12.30f, 2.22f, 3.10f),
                Quaternion.Euler(0f, -90f, 0f),
                "147VR_MAT_Neon_Ivory_Hot",
                "Stage: '147' core (Ivory)"),

            ("147VR_Neon_Sub_VR",
                new Vector3(-12.31f, 1.72f, 3.10f),
                Quaternion.Euler(0f, -90f, 0f),
                "147VR_MAT_Neon_Gold_Hot",
                "Stage: 'VR' text (Gold)"),

            ("147VR_Neon_InnerRing_Ruby",
                new Vector3(-12.31f, 2.05f, 3.10f),
                Quaternion.Euler(0f, -90f, 0f),
                "147VR_MAT_Neon_Ruby_Hot",
                "Stage: inner ring (Ruby)"),

            ("147VR_Neon_OuterRing_Gold",
                new Vector3(-12.32f, 2.05f, 3.10f),
                Quaternion.Euler(0f, -90f, 0f),
                "147VR_MAT_Neon_Gold_Hot",
                "Stage: outer ring (Gold)"),

            // === BACK WALL LOGO (z=-11.82, the -Z wall behind the bar) ===
            ("147VR_LOGO_PASS_CenterMain",
                new Vector3(0.01f, 2.02f, -11.82f),
                Quaternion.identity,  // face +Z into room
                "147VR _ Royal Gold",
                "Back wall: '147' (Gold)"),

            ("147VR_LOGO_PASS_CenterVR",
                new Vector3(0.08f, 1.90f, -11.82f),
                Quaternion.identity,
                "147VR _ Warm Ivory",
                "Back wall: 'VR' (Ivory)"),

            ("147VR_LOGO_PASS_CenterTag",
                new Vector3(0.08f, 1.78f, -11.82f),
                Quaternion.identity,
                "147VR _ Royal Gold",
                "Back wall: 'BILLIARDS MUSIC SOCIAL' (Gold)"),

            ("147VR_LOGO_PASS_CenterInner",
                new Vector3(0.08f, 2.10f, -11.84f),
                Quaternion.identity,
                "147VR _ Ruby Neon",
                "Back wall: inner ring (Ruby)"),

            ("147VR_LOGO_PASS_CenterOuter",
                new Vector3(0.08f, 2.10f, -11.84f),
                Quaternion.identity,
                "147VR _ Royal Gold",
                "Back wall: outer ring (Gold)"),

            // === WELCOME SIGN (z=-14.49, the -Z entrance wall) ===
            ("147VR_Welcome_Sign",
                new Vector3(-0.04f, 2.63f, -14.49f),
                Quaternion.identity,
                "147VR_Mat_WelcomeGold",
                "Entrance: 'WELCOME' (Gold)"),

            // === STAGE CURTAIN TRIM (position 0,0,0 = local to curtain parent) ===
            ("147vr_StageCurtain_OpenGathered_GoldRod",
                new Vector3(0f, 0f, 0f),
                Quaternion.identity,
                "147VR_Mat_GoldCarved",
                "Stage curtain: gold rod"),
        };

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/30) Import Neon Signs from FBX (Undoable)")]
        public static void MenuImport()
        {
            Run();
            EditorUtility.DisplayDialog("Neon Sign Import",
                "Report: " + ReportPath + "\n\nScene is DIRTY. Ctrl+S to save, Ctrl+Z to undo.", "OK");
        }

        /// <summary>ToolRunner entry point: "neon-import".</summary>
        public static void Run()
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine("NEON SIGN IMPORT - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                log.AppendLine("REFUSED: loaded scene is '" + scene.path + "'");
                Write(log);
                return;
            }

            // Load the FBX
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbxAsset == null)
            {
                log.AppendLine("REFUSED: FBX not found at " + FbxPath);
                log.AppendLine("  Run the Blender conversion script first.");
                Write(log);
                return;
            }

            log.AppendLine("FBX loaded: " + FbxPath);
            log.AppendLine("  Root children: " + fbxAsset.transform.childCount);

            // List what's inside
            foreach (Transform child in fbxAsset.transform)
                log.AppendLine("    child: " + child.name + " (children=" + child.childCount + ")");

            // Find or create the parent container
            GameObject neonRoot = GameObject.Find("147VR_NeonSigns_Imported");
            if (neonRoot == null)
            {
                neonRoot = new GameObject("147VR_NeonSigns_Imported");
                Undo.RegisterCreatedObjectUndo(neonRoot, "Create Neon Signs Root");
                log.AppendLine("  Created root container: 147VR_NeonSigns_Imported");
            }

            int placed = 0, skipped = 0, missingMat = 0;

            foreach (var (fbxName, pos, rot, matName, label) in Placements)
            {
                // Find the mesh in the FBX (could be root child or grandchild)
                Transform fbxChild = FindDeep(fbxAsset.transform, fbxName);
                if (fbxChild == null)
                {
                    // Try with Blender's conversion suffix
                    fbxChild = FindDeep(fbxAsset.transform, fbxName + "_Mesh");
                    if (fbxChild == null) fbxChild = FindDeep(fbxAsset.transform, fbxName + ".001");
                }

                if (fbxChild == null)
                {
                    log.AppendLine("  SKIP (not in FBX): " + fbxName + " [" + label + "]");
                    skipped++;
                    continue;
                }

                // Find material
                Material mat = FindMaterial(matName);
                if (mat == null)
                {
                    log.AppendLine("  MAT MISSING: '" + matName + "' for " + fbxName);
                    missingMat++;
                }

                // Instantiate the mesh
                GameObject instance = UnityEngine.Object.Instantiate(fbxChild.gameObject);
                instance.name = fbxName;
                Undo.RegisterCreatedObjectUndo(instance, "Place Neon: " + fbxName);
                instance.transform.SetParent(neonRoot.transform, false);
                instance.transform.position = pos;
                instance.transform.rotation = rot;

                // Assign material to all renderers
                if (mat != null)
                {
                    foreach (Renderer rend in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] mats = new Material[rend.sharedMaterials.Length];
                        for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                        Undo.RecordObject(rend, "Neon Material");
                        rend.sharedMaterials = mats;
                    }
                }

                // Make it emissive-friendly
                foreach (Renderer rend in instance.GetComponentsInChildren<Renderer>(true))
                {
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rend.receiveShadows = false;
                    rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                }

                placed++;
                log.AppendLine("  PLACED: " + fbxName + " at " + pos + " mat=" + (mat != null ? mat.name : "<none>") +
                               " [" + label + "]");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            log.AppendLine();
            log.AppendLine("RESULT: placed=" + placed + " skipped=" + skipped + " missingMaterials=" + missingMat);
            log.AppendLine("Scene marked dirty. Ctrl+S to save.");
            Write(log);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Material FindMaterial(string matName)
        {
            // Search in known material folders
            string[] folders = {
                "Assets/AAA/Imports/NeonVintageArtPass/Materials/",
                "Assets/AAA/Imports/naris_vintage/Rebound/",
                "Assets/AAA/ImportedSnooker/Materials/V008/"
            };

            foreach (string folder in folders)
            {
                string[] guids = AssetDatabase.FindAssets(matName + " t:Material", new[] { folder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (m != null && m.name == matName) return m;
                }
            }

            // Fallback: search all
            string[] allGuids = AssetDatabase.FindAssets(matName + " t:Material");
            foreach (string guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null && m.name.Contains(matName)) return m;
            }
            return null;
        }

        private static void Write(System.Text.StringBuilder log)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, log.ToString());
            Debug.Log("[NeonSign] done -> " + ReportPath);
        }
    }
}
#endif
