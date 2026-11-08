#if UNITY_EDITOR
// NeonVintageAudit.cs — evidence dump for the AAA ArtPass scene ("ออกทะเล" audit).
// Writes Logs/aaa_audit.json:
//   scene roots, per-object world positions under 147VR_AAA_NeonVintage,
//   light group contents, ambient/post settings, material binding stats.
// Driven via ToolRunner command "aaa-audit" (or menu 147VR/AAA/NeonVintage/Audit).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    public static class NeonVintageAudit
    {
        const string SceneRootName = "147VR_AAA_NeonVintage";
        const string OutFile = "Logs/aaa_audit.json";

        [MenuItem("147VR/AAA/NeonVintage/Audit (dump scene report)")]
        public static void MenuAudit() { Run(); }

        public static void Run()
        {
            var sb = new StringBuilder();
            var scene = SceneManager.GetActiveScene();
            sb.Append("{\n");
            sb.Append($"\"scene\": \"{scene.path}\",\n");
            sb.Append($"\"time\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");

            // roots
            sb.Append("\"roots\": [\n");
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var r = roots[i];
                sb.Append($"  {{\"name\": {J(r.name)}, \"active\": {r.activeSelf.ToString().ToLower()}, \"children\": {r.transform.childCount}}}");
                sb.Append(i < roots.Length - 1 ? ",\n" : "\n");
            }
            sb.Append("],\n");

            var root = roots.FirstOrDefault(g => g.name == SceneRootName);
            sb.Append($"\"root_found\": {root != null},\n");

            if (root != null)
            {
                // per-instance + full descendant world positions
                sb.Append("\"instances\": [\n");
                var insts = new List<Transform>();
                for (int i = 0; i < root.transform.childCount; i++) insts.Add(root.transform.GetChild(i));
                for (int i = 0; i < insts.Count; i++)
                {
                    var t = insts[i];
                    sb.Append("  {\n");
                    sb.Append($"    \"name\": {J(t.name)},\n");
                    sb.Append($"    \"pos\": {V(t.position)}, \"rot_euler\": {V(t.eulerAngles)}, \"scale\": {V(t.lossyScale)},\n");
                    sb.Append($"    \"active\": {t.gameObject.activeSelf.ToString().ToLower()},\n");
                    // renderers / tris
                    var mfs = t.GetComponentsInChildren<MeshFilter>(true);
                    long tris = 0;
                    foreach (var mf in mfs) if (mf.sharedMesh != null) tris += (long)mf.sharedMesh.triangles.Length / 3;
                    var rends = t.GetComponentsInChildren<Renderer>(true);
                    int nullMat = 0, errMat = 0;
                    foreach (var rd in rends)
                        foreach (var mm in rd.sharedMaterials)
                        {
                            if (mm == null) { nullMat++; continue; }
                            if (mm.shader == null || mm.shader.name.Contains("Hidden/InternalErrorShader")) errMat++;
                        }
                    sb.Append($"    \"renderers\": {rends.Length}, \"meshes\": {mfs.Length}, \"tris\": {tris}, \"null_mats\": {nullMat}, \"error_mats\": {errMat},\n");
                    // world bounds (encapsulate)
                    var b = default(Bounds);
                    bool has = false;
                    foreach (var rd in rends)
                        if (rd is MeshRenderer || rd is SkinnedMeshRenderer)
                        {
                            var bb = rd.bounds;
                            if (!has) { b = bb; has = true; } else b.Encapsulate(bb);
                        }
                    sb.Append($"    \"bounds_center\": {(has ? V(b.center) : "null")}, \"bounds_size\": {(has ? V(b.size) : "null")},\n");
                    // descendants with world pos (name match against blender analysis)
                    sb.Append("    \"objects\": [\n");
                    var all = t.GetComponentsInChildren<Transform>(true);
                    int cap = Math.Min(all.Length, 4000);
                    for (int k = 0; k < cap; k++)
                    {
                        var c = all[k];
                        sb.Append($"      {{\"name\": {J(c.name)}, \"pos\": {V(c.position)}, \"scale\": {V(c.lossyScale)}}}");
                        sb.Append(k < cap - 1 ? ",\n" : "\n");
                    }
                    sb.Append("    ]\n");
                    sb.Append("  }");
                    sb.Append(i < insts.Count - 1 ? ",\n" : "\n");
                }
                sb.Append("],\n");

                // lights
                var lg = root.transform.Find("147VR_AAA_NeonVintage_Lights");
                sb.Append("\"lights\": [\n");
                if (lg != null)
                {
                    var ls = lg.GetComponentsInChildren<Light>(true);
                    for (int i = 0; i < ls.Length; i++)
                    {
                        var l = ls[i];
                        sb.Append($"  {{\"name\": {J(l.gameObject.name)}, \"type\": \"{l.type}\", \"color\": {C(l.color)}, \"intensity\": {l.intensity}, \"range\": {l.range}, \"angle\": {l.spotAngle}, \"shadows\": \"{l.shadows}\", \"pos\": {V(l.transform.position)}}}");
                        sb.Append(i < ls.Length - 1 ? ",\n" : "\n");
                    }
                }
                sb.Append("],\n");
            }

            // render settings + volumes
            sb.Append("\"ambient\": {\"mode\": \"" + RenderSettings.ambientMode + "\", \"color\": " + C(RenderSettings.ambientLight) + ", \"intensity\": " + RenderSettings.ambientIntensity + "},\n");
            var vols = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            sb.Append("\"volumes\": [\n");
            for (int i = 0; i < vols.Length; i++)
            {
                var v = vols[i];
                var comps = v.sharedProfile != null ? v.sharedProfile.components.ToList() : new List<UnityEngine.Rendering.VolumeComponent>();
                var cl = string.Join(",", comps.Select(c => c.GetType().Name));
                sb.Append($"  {{\"name\": {J(v.gameObject.name)}, \"global\": {v.isGlobal}, \"components\": \"{cl}\"}}");
                sb.Append(i < vols.Length - 1 ? ",\n" : "\n");
            }
            sb.Append("]\n");
            sb.Append("}\n");

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutFile, sb.ToString());
            Debug.Log($"[NeonVintageAudit] wrote {OutFile} ({(new FileInfo(OutFile).Length / 1024)} KB)");
            EditorUtility.DisplayDialog("NeonVintageAudit", "เขียน " + OutFile + " เรียบร้อย", "ปิด");
        }

        static string J(string s) { return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }
        static string V(Vector3 v) { return string.Format("[{0:0.###},{1:0.###},{2:0.###}]", v.x, v.y, v.z); }
        static string C(Color c) { return string.Format("[{0:0.###},{1:0.###},{2:0.###},{3:0.###}]", c.r, c.g, c.b, c.a); }
    }
}
#endif
