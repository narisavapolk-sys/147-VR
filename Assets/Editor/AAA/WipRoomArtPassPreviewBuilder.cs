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
    /// Owner-clicked, reversible art-pass preview for WipRoom.
    /// Saves the currently dirty WipRoom state (including the already-applied A pass)
    /// to a NEW preview Scene, then adds correctly textured Bay3/Bay4 materials and flags.
    /// Never writes the production WipRoom Scene or changes Build Settings.
    /// </summary>
    public static class WipRoomArtPassPreviewBuilder
    {
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string PreviewSceneStem = "Assets/Scenes/147VR_MainScene_WipRoom_ArtPassPreview";
        private const string RoomRootName = "NarisVintage_Room";
        private const string ArtRoot = "Assets/AAA/Imports/NeonVintageArtPass";
        private const string ReboundRoot = "Assets/AAA/Imports/naris_vintage/Rebound";

        private static readonly string[] Countries =
        {
            "argentina", "austria", "brazil", "canada", "denmark", "finland",
            "france", "germany", "greece", "india", "ireland", "italy",
            "japan", "netherlands", "norway", "poland", "portugal", "russia",
            "southafrica", "spain", "switzerland", "thailand", "turkey",
            "uk", "ukraine", "usa"
        };

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/3) Build A+B ArtPass Preview Copy")]
        public static void BuildPreviewCopy()
        {
            Scene source = SceneManager.GetActiveScene();
            if (!source.IsValid() || !source.isLoaded ||
                !string.Equals(source.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "Safety stop. Open the actual gameplay Scene first:\n" + SourceScenePath, "OK");
                return;
            }

            if (source.GetRootGameObjects().Any(g => g.name == "147VR_AAA_Flags" || g.name == "147VR_AAA_FlagRod"))
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "Flag assets already exist in this Scene. Preview build stopped to prevent duplicates.", "OK");
                return;
            }

            string previewPath = GetUnusedPreviewPath();
            string backupRoot = Path.Combine(@"C:\Users\mongo\147VR_Backups",
                "WipRoom_ArtPassPreview_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(backupRoot);
            BackupIfExists(SourceScenePath, backupRoot);
            BackupIfExists(SourceScenePath + ".meta", backupRoot);
            BackupIfExists("Assets/Settings/SampleSceneProfile.asset", backupRoot);
            BackupIfExists("Assets/Settings/SampleSceneProfile.asset.meta", backupRoot);

            // Save current unsaved A material work as a copy. The production scene file remains untouched.
            if (!EditorSceneManager.SaveScene(source, previewPath, true))
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "Could not create the preview Scene copy. Production Scene was not saved.", "OK");
                return;
            }

            // Open the copy additively, make it active, then discard only the in-memory source session.
            Scene preview = EditorSceneManager.OpenScene(previewPath, OpenSceneMode.Additive);
            if (!preview.IsValid() || !preview.isLoaded)
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "Preview Scene was written but could not be reopened:\n" + previewPath, "OK");
                return;
            }
            SceneManager.SetActiveScene(preview);
            EditorSceneManager.CloseScene(source, true);

            GameObject room = preview.GetRootGameObjects().FirstOrDefault(g => g.name == RoomRootName);
            if (room == null)
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "NarisVintage_Room root missing in preview. No art-pass objects were added.", "OK");
                return;
            }

            int collidersBefore = Count<Collider>(preview);
            int bodiesBefore = Count<Rigidbody>(preview);
            int lightsBefore = Count<Light>(preview);
            var materialChanges = new List<Tuple<Renderer, Material[]>>();
            var createdRoots = new List<GameObject>();
            var report = new StringBuilder();

            try
            {
                int bay3Renderers = ApplyTableMaterial(room, "SM_PoolTable_03", "PoolTable_03", "b3_poolSkin",
                    ReboundRoot + "/147VR_147vr_b3_poolSkin.mat", materialChanges, report);
                int bay4Renderers = ApplyTableMaterial(room, "SM_PoolTable_04", "PoolTable_04", "Pool_VR_Material",
                    ReboundRoot + "/147VR_147VR_Pool_VR_Material.mat", materialChanges, report);
                if (bay3Renderers == 0 || bay4Renderers == 0)
                    throw new InvalidOperationException("Could not uniquely locate both Bay3/Bay4 table renderers. Bay3=" + bay3Renderers + ", Bay4=" + bay4Renderers);

                GameObject flagsAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ArtRoot + "/147VR_AAA_Flags.fbx");
                GameObject rodAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ArtRoot + "/147VR_AAA_FlagRod.fbx");
                if (flagsAsset == null || rodAsset == null)
                    throw new InvalidOperationException("Required flag FBX asset is missing or not imported.");

                GameObject flags = PrefabUtility.InstantiatePrefab(flagsAsset, preview) as GameObject;
                GameObject rod = PrefabUtility.InstantiatePrefab(rodAsset, preview) as GameObject;
                if (flags == null || rod == null)
                    throw new InvalidOperationException("Could not instantiate the imported flags / flag rod FBX.");

                createdRoots.Add(flags);
                createdRoots.Add(rod);
                flags.name = "147VR_AAA_Flags";
                rod.name = "147VR_AAA_FlagRod";
                flags.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                flags.transform.localScale = Vector3.one;
                rod.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                rod.transform.localScale = Vector3.one;

                int mappedFlags = ApplyFlagMaterials(flags, report);
                int rodMaterialApplied = ApplyRodMaterial(rod, report);
                if (mappedFlags != Countries.Length)
                    throw new InvalidOperationException("Flag country material mapping incomplete: " + mappedFlags + " / " + Countries.Length);
                if (rodMaterialApplied == 0)
                    throw new InvalidOperationException("Flag rod gold material was not applied.");

                int collidersAfter = Count<Collider>(preview);
                int bodiesAfter = Count<Rigidbody>(preview);
                int lightsAfter = Count<Light>(preview);
                if (collidersBefore != collidersAfter || bodiesBefore != bodiesAfter || lightsBefore != lightsAfter)
                    throw new InvalidOperationException("Physics/light invariant changed. Art-pass preview rolled back.");

                report.AppendLine();
                report.AppendLine("SCENE BOUNDARY");
                report.AppendLine("Colliders: " + collidersBefore + " -> " + collidersAfter);
                report.AppendLine("Rigidbodies: " + bodiesBefore + " -> " + bodiesAfter);
                report.AppendLine("Lights: " + lightsBefore + " -> " + lightsAfter);
                report.AppendLine("Transforms added (flags/rod): " +
                    (flags.GetComponentsInChildren<Transform>(true).Length + rod.GetComponentsInChildren<Transform>(true).Length));
                report.AppendLine("Build Settings changed: NO");
                report.AppendLine("Production WipRoom saved: NO");
                report.AppendLine("Preview Scene: " + previewPath);
                report.AppendLine("Source backup: " + backupRoot);

                report.AppendLine();
                report.AppendLine("REQUIRED PHYSICS ROOTS");
                foreach (string rootName in new[] { "Bay1_PhysicsRoot", "Bay2_PhysicsRoot", "Bay3_PhysicsRoot", "Bay4_PhysicsRoot", "Quest Setup", "OVRCameraRig", "147VR_LightRig", "Global Volume" })
                {
                    GameObject root = preview.GetRootGameObjects().FirstOrDefault(g => g.name == rootName);
                    report.AppendLine(rootName + ": " + (root == null ? "MISSING" : (root.activeSelf ? "ACTIVE" : "INACTIVE")));
                }

                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/WipRoom_ArtPassPreview_Build.txt", report.ToString());
                EditorSceneManager.MarkSceneDirty(preview);
                if (!EditorSceneManager.SaveScene(preview))
                    throw new IOException("Could not save the preview Scene.");

                Selection.activeGameObject = room;
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected();
                SceneView.RepaintAll();

                Debug.Log("[WipRoomArtPassPreview]\n" + report);
                EditorUtility.DisplayDialog("147VR WipRoom",
                    "Preview copy created and saved.\n\nBay3 renderers: " + bay3Renderers +
                    "\nBay4 renderers: " + bay4Renderers +
                    "\nFlag materials mapped: " + mappedFlags + " / " + Countries.Length +
                    "\nRod material applied: " + (rodMaterialApplied > 0 ? "YES" : "NO") +
                    "\nPhysics counts unchanged.\n\nProduction WipRoom remains untouched.\nInspect this preview before any production adoption.",
                    "OK");
            }
            catch (Exception exception)
            {
                foreach (var pair in materialChanges)
                    if (pair.Item1 != null) pair.Item1.sharedMaterials = pair.Item2;
                foreach (GameObject root in createdRoots)
                    if (root != null) UnityEngine.Object.DestroyImmediate(root);
                EditorSceneManager.MarkSceneDirty(preview);
                Debug.LogError("[WipRoomArtPassPreview] " + exception);
                EditorUtility.DisplayDialog("147VR WipRoom", "Preview build failed. Changes were rolled back in the preview session:\n" + exception.Message +
                    "\nThe production WipRoom file remains untouched.", "OK");
            }
        }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/4) Audit Preview Physics Boundary")]
        public static void AuditPreviewPhysicsBoundary()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || !scene.path.Contains("WipRoom_ArtPassPreview"))
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "Open the generated WipRoom_ArtPassPreview Scene first.", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("147VR WipRoom ArtPass Preview Physics Boundary Audit");
            sb.AppendLine("Scene: " + scene.path);
            foreach (string name in new[] { "Bay1_PhysicsRoot", "Bay2_PhysicsRoot", "Bay3_PhysicsRoot", "Bay4_PhysicsRoot", "Quest Setup", "OVRCameraRig", "147VR_LightRig", "Global Volume" })
            {
                GameObject found = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
                sb.AppendLine(name + ": " + (found == null ? "MISSING" : (found.activeSelf ? "ACTIVE" : "INACTIVE")));
            }
            sb.AppendLine("Scene colliders: " + Count<Collider>(scene));
            sb.AppendLine("Scene rigidbodies: " + Count<Rigidbody>(scene));
            sb.AppendLine("Scene lights: " + Count<Light>(scene));
            sb.AppendLine("Room transforms: " + (scene.GetRootGameObjects().FirstOrDefault(g => g.name == RoomRootName)?.GetComponentsInChildren<Transform>(true).Length ?? 0));
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/WipRoom_ArtPassPreview_PhysicsBoundary.txt", sb.ToString());
            Debug.Log("[WipRoomArtPassPreview]\n" + sb);
            EditorUtility.DisplayDialog("147VR WipRoom", "Physics Boundary audit saved to Logs/WipRoom_ArtPassPreview_PhysicsBoundary.txt. No Scene changes.", "OK");
        }

        private static int ApplyTableMaterial(GameObject room, string exactName, string objectToken, string materialToken, string materialPath,
            List<Tuple<Renderer, Material[]>> changes, StringBuilder report)
        {
            Material replacement = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (replacement == null || replacement.mainTexture == null)
                throw new InvalidOperationException("Bay material missing or has no albedo texture: " + materialPath);

            var renderers = room.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.gameObject.name.Equals(exactName, StringComparison.OrdinalIgnoreCase) ||
                            r.gameObject.name.IndexOf(objectToken, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            r.sharedMaterials.Any(m => m != null && m.name.IndexOf(materialToken, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToArray();
            int applied = 0;
            foreach (Renderer renderer in renderers)
            {
                Material[] current = renderer.sharedMaterials;
                if (current.Length == 0) continue;
                changes.Add(Tuple.Create(renderer, current.ToArray()));
                for (int i = 0; i < current.Length; i++) current[i] = replacement;
                renderer.sharedMaterials = current;
                applied++;
            }
            report.AppendLine(exactName + " material: " + materialPath + " | renderers=" + applied +
                " | albedo=" + replacement.mainTexture.name);
            return applied;
        }

        private static int ApplyFlagMaterials(GameObject flagsRoot, StringBuilder report)
        {
            int mappedCountries = 0;
            int mappedRenderers = 0;
            foreach (string country in Countries)
            {
                string materialPath = ReboundRoot + "/147VR_147VR_FlagMat_" + country + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null || material.mainTexture == null)
                {
                    report.AppendLine("FLAG MATERIAL MISSING/TEXTURELESS: " + country + " -> " + materialPath);
                    continue;
                }

                Renderer[] renderers = flagsRoot.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.gameObject.name.EndsWith("_" + country, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (renderers.Length == 0)
                {
                    report.AppendLine("FLAG MESH NOT FOUND: " + country);
                    continue;
                }
                foreach (Renderer renderer in renderers)
                    renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                mappedCountries++;
                mappedRenderers += renderers.Length;
            }
            report.AppendLine("Flag countries mapped: " + mappedCountries + " / " + Countries.Length +
                " | mesh renderers mapped=" + mappedRenderers);
            return mappedCountries;
        }

        private static int ApplyRodMaterial(GameObject rodRoot, StringBuilder report)
        {
            const string materialPath = ReboundRoot + "/147VR_147VR_AAA_MAT_RealCurtain_Gold.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null || material.mainTexture == null)
            {
                report.AppendLine("Flag rod gold material missing or textureless: " + materialPath);
                return 0;
            }
            var renderers = rodRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            report.AppendLine("Flag rod gold material: " + materialPath + " | renderers=" + renderers.Length);
            return renderers.Length;
        }

        private static int Count<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<T>(true).Length);

        private static string GetUnusedPreviewPath()
        {
            int version = 1;
            string path;
            do
            {
                path = PreviewSceneStem + "_v" + version.ToString("000") + ".unity";
                version++;
            } while (File.Exists(path));
            return path;
        }

        private static void BackupIfExists(string assetPath, string backupRoot)
        {
            string absolute = Path.GetFullPath(assetPath);
            if (!File.Exists(absolute)) return;
            string target = Path.Combine(backupRoot, Path.GetFileName(assetPath));
            File.Copy(absolute, target, true);
        }
    }
}
#endif
