#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    public static class PlaceBay2FrameClean
    {
        // Guard: this must run ONLY on the real gameplay scene, never on a preview copy.
        private const string ProductionScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";

        [MenuItem("147VR/AAA/Bay2/Place Clean Bay 2 Frame (Production WipRoom only)")]
        public static void MenuApply()
        {
            // Previously this class had [InitializeOnLoad] + delayCall that auto-ran and SAVED
            // whatever scene was active on every domain reload (it silently modified the
            // VenueArtFix preview). Removed 2026-11-17: now it runs only from this explicit menu,
            // and only when the production gameplay scene is the active scene.
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.path, ProductionScenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("147VR Bay 2 Frame",
                    "Safety stop. Open the actual gameplay Scene first:\n" + ProductionScenePath +
                    "\n(147VR/AAA/NeonVintage/WipRoom/0) Open Actual Game Scene)", "OK");
                return;
            }
            ApplyCleanFrameToScene(true);
        }

        /// <summary>
        /// Headless entry for ToolRunner command "bay2-frame" (Logs/editor_cmd.txt). Opens the
        /// production WipRoom scene itself (so it works regardless of which preview was active),
        /// then applies the clean frame with NO modal dialog. Saves the production scene.
        /// </summary>
        public static void RunBatch()
        {
            Scene s = SceneManager.GetActiveScene();
            // Persist any dirty scene first (VenueArtFix previews are disposable) so switching to
            // the production WipRoom never raises a "Save changes?" modal that would hang the
            // headless ToolRunner dispatch.
            if (s.IsValid() && s.isDirty) EditorSceneManager.SaveScene(s);
            if (!s.IsValid() || !string.Equals(s.path, ProductionScenePath, StringComparison.OrdinalIgnoreCase))
                EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);
            ApplyCleanFrameToScene(false);
        }

        public static void ApplyCleanFrameToScene(bool userClick)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== APPLY CLEAN BAY 2 FRAME ===");
            sb.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            Scene scene = SceneManager.GetActiveScene();
            sb.AppendLine("Active Scene: " + scene.name + " (" + scene.path + ")");

            string fbxPath = "Assets/AAA/ImportedSnooker/Bay2/SM_TABLE_FRAME_Bay2_Clean.fbx";
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxPrefab == null)
            {
                sb.AppendLine("ERROR: Could not load " + fbxPath);
                File.WriteAllText("Logs/Bay2_Place_Result.txt", sb.ToString());
                return;
            }

            // Find ROOT_PoolTable_02 in scene
            var allGOs = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToList();
            var poolTable02 = allGOs.FirstOrDefault(g => g.name == "ROOT_PoolTable_02");
            if (poolTable02 == null)
            {
                sb.AppendLine("ERROR: ROOT_PoolTable_02 not found in scene!");
                File.WriteAllText("Logs/Bay2_Place_Result.txt", sb.ToString());
                return;
            }

            // Find old SM_TABLE FRAME
            var oldFrame = poolTable02.transform.Find("SM_TABLE FRAME")?.gameObject;
            Vector3 targetCenter = new Vector3(-0.14f, 0.83f, 3.10f);
            if (oldFrame != null)
            {
                var oldMr = oldFrame.GetComponent<MeshRenderer>();
                if (oldMr != null)
                {
                    targetCenter = oldMr.bounds.center;
                    sb.AppendLine("Found old SM_TABLE FRAME. Old bounds center: " + targetCenter);
                }
                // Disable old frame renderer so it doesn't overlap
                Undo.RecordObject(oldFrame, "Disable old frame");
                oldFrame.SetActive(false);
                sb.AppendLine("Disabled old SM_TABLE FRAME GameObject.");
            }

            // Check if clean frame already placed
            var existingClean = poolTable02.transform.Find("SM_TABLE_FRAME_CLEAN")?.gameObject;
            if (existingClean != null)
            {
                Undo.DestroyObjectImmediate(existingClean);
                sb.AppendLine("Removed previous SM_TABLE_FRAME_CLEAN.");
            }

            // Instantiate clean FBX
            GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
            newInstance.name = "SM_TABLE_FRAME_CLEAN";
            Undo.RegisterCreatedObjectUndo(newInstance, "Place clean Bay 2 frame");

            // Parent to ROOT_PoolTable_02
            newInstance.transform.SetParent(poolTable02.transform, true);

            // Align the WHOLE instance (every renderer) and bind a TEXTURED Mahogany material.
            // The old code used only the first MeshRenderer's bounds and assigned the shared
            // "Table Mahogany" material, which is texture-less (renders white plastic). That is
            // why the frame had no visible wood edge. Fix: combined bounds + a dedicated material
            // built from Wood_Mahogany_Diffuse + Normal.
            MeshRenderer[] renderers = newInstance.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0)
            {
                sb.AppendLine("WARNING: clean frame instance has no MeshRenderer.");
            }
            else
            {
                Bounds comb = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) comb.Encapsulate(renderers[i].bounds);
                Vector3 delta = targetCenter - comb.center;
                newInstance.transform.position += delta;
                comb = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) comb.Encapsulate(renderers[i].bounds);
                sb.AppendLine("renderers=" + renderers.Length + " | delta=" + Fmt(delta) +
                              " -> combined centre " + Fmt(comb.center) + " size " + Fmt(comb.size));

                Material wood = GetOrCreateFrameMahoganyMaterial();
                if (wood != null)
                {
                    int bound = 0;
                    foreach (MeshRenderer mr in renderers)
                    {
                        int slots = mr.sharedMaterials.Length;
                        if (slots < 1) slots = 1;
                        Material[] arr = new Material[slots];
                        for (int s = 0; s < slots; s++) arr[s] = wood;
                        mr.sharedMaterials = arr;
                        bound++;
                    }
                    sb.AppendLine("bound textured Mahogany wood to " + bound + " renderer(s), all slots.");
                }
                else
                {
                    sb.AppendLine("ERROR: could not build textured Mahogany material (wood textures missing?).");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            sb.AppendLine("SUCCESS: Placed SM_TABLE_FRAME_CLEAN and saved scene " + scene.name);

            // Render camera capture to confirm visual
            CapturePreview(scene, targetCenter);

            File.WriteAllText("Logs/Bay2_Place_Result.txt", sb.ToString());
            Debug.Log(sb.ToString());
            if (userClick)
            {
                EditorUtility.DisplayDialog("147VR Bay 2 Frame", "Clean Bay 2 table frame placed successfully!\nCheck Logs/Bay2_Place_Result.txt", "OK");
            }
        }

        /// <summary>
        /// Dedicated textured Mahogany material for the Bay2 frame, built from the real wood
        /// diffuse + normal (the shared "Table Mahogany" assets are texture-less). Created once,
        /// reused on every run. Never touches other materials.
        /// </summary>
        private static Material GetOrCreateFrameMahoganyMaterial()
        {
            const string path = "Assets/AAA/ImportedSnooker/Bay2/SM_TABLE_FRAME_Bay2_Mahogany.mat";
            const string diffPath = "Assets/147 main/RoomTextures/Wood_Mahogany_Diffuse_mqt_2048.png";
            const string nrmPath = "Assets/147 main/RoomTextures/Wood_Mahogany_Normal_mqt_2048.png";
            Texture diff = AssetDatabase.LoadAssetAtPath<Texture>(diffPath);
            Texture nrm = AssetDatabase.LoadAssetAtPath<Texture>(nrmPath);
            if (diff == null) { Debug.LogWarning("[Bay2] missing wood diffuse " + diffPath); return null; }
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return null;

            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
            m.shader = lit;
            m.SetTexture("_BaseMap", diff);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", diff);
            if (nrm != null) { m.SetTexture("_BumpMap", nrm); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.7f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            m.color = Color.white;
            // Wood grain density: the frame is ~3.85 m long; one 2048 tile stretched over that
            // (ST 1,1) looks like flat brown, not planks. Repeat the grain along the rail.
            // ST (6,2) matches the project's validated mahogany density in WipRoomArtRound3
            // (MahoganyTilingX=6, MahoganyTilingY=2 for the same 3.847 m frame). The old (3,1.5)
            // was too sparse - the vertical planks read as flat brown in the close-up (owner check).
            m.SetTextureScale("_BaseMap", new Vector2(6f, 2f));
            m.SetTextureScale("_BumpMap", new Vector2(6f, 2f));
            if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", new Vector2(6f, 2f));
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";

        private static void CapturePreview(Scene scene, Vector3 center)
        {
            // Wide: whole table. Close: tight on the near rail so the wooden edge height is judgeable
            // (the single wide shot was too far to prove the edge exists - owner feedback).
            RenderShot(center + new Vector3(3.6f, 2.6f, 3.6f), center + new Vector3(0f, 0.15f, 0f), 55f, "Logs/renders/bay2_clean_placed.png");
            RenderShot(center + new Vector3(0.8f, 0.5f, 1.6f), center + new Vector3(0f, 0.05f, 0.98f), 38f, "Logs/renders/bay2_rail_closeup.png");
        }

        private static void RenderShot(Vector3 camPos, Vector3 lookAt, float fov, string outPath)
        {
            try
            {
                var camGO = new GameObject("Temp_Bay2_Capture_Cam");
                var cam = camGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.fieldOfView = fov;
                cam.nearClipPlane = 0.05f;
                camGO.transform.position = camPos;
                camGO.transform.LookAt(lookAt);

                int width = 1280, height = 720;
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                Directory.CreateDirectory("Logs/renders");
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(camGO);
            }
            catch (Exception ex) { Debug.LogWarning("Capture failed (" + outPath + "): " + ex.Message); }
        }
    }
}
#endif
