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
    /// <summary>
    /// Ceiling parity pass. Rebinds the ceiling coffer material(s) whose _BaseMap GUID is dead
    /// (renders white) to the Blender-extracted coffer texture + normal, using the authored
    /// values from the room manifest (147VR_Ceiling_Coffer: roughness 0.62 -> smoothness 0.38,
    /// metallic 0). Renders a ceiling view so the result can be compared image-to-image against
    /// the Blender reference. Also reports every ceiling/coffer renderer's bounds and current
    /// _BaseMap_ST so the "position/pattern scale" question is decided from evidence, not guess.
    ///
    /// Runs headless via ToolRunner command "ceiling-parity" (Logs/editor_cmd.txt). No modal
    /// dialog. Opens the production WipRoom scene itself (saving any dirty preview first).
    /// </summary>
    public static class WipRoomCeilingParity
    {
        private const string ProductionScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string CofferTex = "Assets/147 main/BlendTextures/147VR_ceiling.png.png";
        private const string CofferNrm = "Assets/147 main/BlendTextures/147VR_ceiling_nrm.png.png";
        private const string ReportPath = "Logs/WipRoom_Ceiling_Parity.txt";
        private const string RenderPath = "Logs/renders/ceiling_parity.png";
        private const float CofferSmoothness = 0.38f;   // 1 - manifest roughness 0.62

        public static void RunBatch()
        {
            Scene cur = SceneManager.GetActiveScene();
            if (cur.IsValid() && cur.isDirty) EditorSceneManager.SaveScene(cur);
            if (!cur.IsValid() || !string.Equals(cur.path, ProductionScenePath, StringComparison.OrdinalIgnoreCase))
                EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);

            var sb = new StringBuilder();
            sb.AppendLine("147VR CEILING PARITY (colour + render) " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Scene scene = SceneManager.GetActiveScene();
            sb.AppendLine("Scene: " + scene.path);

            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(CofferTex);
            Texture nrm = AssetDatabase.LoadAssetAtPath<Texture>(CofferNrm);
            sb.AppendLine("coffer texture present=" + (tex != null) + " | normal present=" + (nrm != null));

            sb.AppendLine("== A) COFFER MATERIALS (rebind dead _BaseMap) ==");
            int rebound = 0;
            foreach (string g in AssetDatabase.FindAssets("Ceiling_Coffer t:Material"))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null) continue;
                Texture cur0 = m.GetTexture("_BaseMap");
                bool dead = HasDeadBaseMap(m);
                sb.AppendLine("  " + p);
                sb.AppendLine("      _BaseMap now = " + (cur0 != null ? cur0.name : "<none>") + (dead ? "  ***DEAD GUID" : ""));
                sb.AppendLine("      _BaseMap_ST  = " + Vec(m.GetVector("_BaseMap_ST")) +
                              " | _BaseColor = " + Col(m.GetColor("_BaseColor")) +
                              " | _Smoothness = " + m.GetFloat("_Smoothness").ToString("F2"));
                if (tex != null && (dead || cur0 == null))
                {
                    Undo.RecordObject(m, "147VR Ceiling Parity");
                    m.SetTexture("_BaseMap", tex);
                    if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                    if (nrm != null) { m.SetTexture("_BumpMap", nrm); m.EnableKeyword("_NORMALMAP"); }
                    if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                    if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", CofferSmoothness);
                    EditorUtility.SetDirty(m);
                    rebound++;
                    sb.AppendLine("      -> REBOUND to " + tex.name + " (smoothness " + CofferSmoothness + ", metallic 0)");
                }
            }
            if (rebound > 0) AssetDatabase.SaveAssets();
            sb.AppendLine("  coffer materials rebounded = " + rebound);

            sb.AppendLine();
            sb.AppendLine("== B) CEILING / COFFER RENDERERS (position + facing, measured only) ==");
            var ceil = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Renderer>(true))
                .Where(r => r.name.IndexOf("Coffer", StringComparison.OrdinalIgnoreCase) >= 0
                         || r.name.IndexOf("Ceiling", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(r => r.name).ToArray();
            foreach (Renderer r in ceil)
            {
                Bounds b = r.bounds;
                string mats = string.Join("|", r.sharedMaterials.Select(x => x == null ? "-" : x.name));
                sb.AppendLine("  " + r.name + "  mat={" + mats + "}  center=" + V(b.center) +
                              "  size=" + V(b.size) + "  activeInHierarchy=" + r.gameObject.activeInHierarchy);
            }
            sb.AppendLine("  ceiling-family renderers = " + ceil.Length);

            CaptureCeiling(scene);

            sb.AppendLine();
            sb.AppendLine("SUMMARY rebound=" + rebound + " ceilingRenderers=" + ceil.Length + " render=" + RenderPath);
            bool saved = EditorSceneManager.SaveScene(scene);
            sb.AppendLine("Scene saved=" + saved);

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log("[CeilingParity]\n" + sb);
        }

        private static void CaptureCeiling(Scene scene)
        {
            try
            {
                GameObject camGO = new GameObject("Temp_Ceiling_Parity_Cam");
                Camera cam = camGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 80f;
                cam.fieldOfView = 70f;
                // Look UP at the real coffer ceiling (CofferTray sits at y ~ 5.25), from mid-room.
                // The old (0,1.5,4)->(0,4,0) shot framed a circular medallion at y~4, not the coffer.
                camGO.transform.position = new Vector3(0f, 1.2f, 2.5f);
                camGO.transform.LookAt(new Vector3(0f, 5.25f, -1.5f));

                int w = 1280, h = 720;
                RenderTexture rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                Directory.CreateDirectory("Logs/renders");
                File.WriteAllBytes(RenderPath, tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(camGO);
            }
            catch (Exception e) { Debug.LogWarning("[CeilingParity] capture failed: " + e.Message); }
        }

        private static bool HasDeadBaseMap(Material m)
        {
            string path = AssetDatabase.GetAssetPath(m);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            string txt = File.ReadAllText(path);
            var mt = System.Text.RegularExpressions.Regex.Match(
                txt, @"_BaseMap:\s*\n\s*m_Texture: \{fileID: 2800000, guid: ([0-9a-f]{32})");
            if (!mt.Success) return false;
            return string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(mt.Groups[1].Value));
        }

        private static string V(Vector3 v) => "(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + "," + v.z.ToString("F2") + ")";
        private static string Vec(Vector4 v) => "(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + "," + v.z.ToString("F2") + "," + v.w.ToString("F2") + ")";
        private static string Col(Color c) => "(" + c.r.ToString("F2") + "," + c.g.ToString("F2") + "," + c.b.ToString("F2") + ")";
    }
}
#endif