#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class _147VR_OneShotSceneAudit
{
    private const string MainScenePath = "Assets/Scenes/147VR_MainScene.unity";
    private const string OutputPath = "Temp/147VR_MainScene_StageAudit.txt";

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[147VR Audit] Failed to open " + MainScenePath);
            EditorApplication.Exit(2);
            return;
        }

        var sb = new StringBuilder(32768);
        sb.AppendLine("147VR MAIN SCENE AUDIT");
        sb.AppendLine("Unity=" + Application.unityVersion);
        sb.AppendLine("Scene=" + scene.path);
        sb.AppendLine();

        foreach (var root in scene.GetRootGameObjects())
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var bounds = GetBounds(renderers);
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            sb.AppendLine("ROOT name=" + root.name +
                " active=" + root.activeInHierarchy +
                " pos=" + V(root.transform.position) +
                " rot=" + V(root.transform.eulerAngles) +
                " scale=" + V(root.transform.lossyScale) +
                " prefab=" + prefabPath +
                " renderers=" + renderers.Length +
                " bounds=" + B(bounds));
        }

        sb.AppendLine();
        var all = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t != null)
            .ToArray();

        foreach (var t in all.Where(t =>
            ContainsAny(t.name, "stage", "platform", "welcome", "curtain", "sign", "cue", "rack", "venue", "crucible", "screen", "podium")))
        {
            var rs = t.GetComponentsInChildren<Renderer>(true);
            sb.AppendLine("MATCH path=" + HierarchyPath(t) +
                " pos=" + V(t.position) +
                " rot=" + V(t.eulerAngles) +
                " scale=" + V(t.lossyScale) +
                " renderers=" + rs.Length +
                " bounds=" + B(GetBounds(rs)));
        }

        var venue = all.FirstOrDefault(t => string.Equals(t.name, "147VR_VENUE_CRUCIBLE", StringComparison.OrdinalIgnoreCase));
        if (venue != null)
        {
            sb.AppendLine();
            sb.AppendLine("VENUE DESCENDANTS (renderer-bearing, max 120)");
            var venueRenderers = venue.GetComponentsInChildren<Renderer>(true)
                .OrderByDescending(r => Volume(r.bounds.size))
                .Take(120);
            foreach (var r in venueRenderers)
            {
                var mf = r.GetComponent<MeshFilter>();
                string meshName = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "<no MeshFilter>";
                int verts = mf != null && mf.sharedMesh != null ? mf.sharedMesh.vertexCount : 0;
                sb.AppendLine("RENDER path=" + HierarchyPath(r.transform) +
                    " mesh=" + meshName + " verts=" + verts +
                    " pos=" + V(r.transform.position) +
                    " bounds=" + B(r.bounds));
            }
        }

        var rack = all.FirstOrDefault(t => t.name.IndexOf("CUERACK", StringComparison.OrdinalIgnoreCase) >= 0);
        if (rack != null)
        {
            sb.AppendLine();
            sb.AppendLine("CUE RACK DESCENDANTS");
            foreach (var t in rack.GetComponentsInChildren<Transform>(true))
            {
                var rs = t.GetComponents<Renderer>();
                if (rs.Length == 0) continue;
                foreach (var r in rs)
                {
                    var mf = r.GetComponent<MeshFilter>();
                    string meshName = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "<no MeshFilter>";
                    int verts = mf != null && mf.sharedMesh != null ? mf.sharedMesh.vertexCount : 0;
                    sb.AppendLine("RACK path=" + HierarchyPath(t) +
                        " mesh=" + meshName + " verts=" + verts +
                        " localPos=" + V(t.localPosition) +
                        " localRot=" + V(t.localEulerAngles) +
                        " localScale=" + V(t.localScale) +
                        " bounds=" + B(r.bounds));
                }
            }
        }

        Directory.CreateDirectory("Temp");
        File.WriteAllText(OutputPath, sb.ToString());
        Debug.Log("[147VR Audit] Wrote " + OutputPath + "\n" + sb);
        EditorApplication.Exit(0);
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static Bounds GetBounds(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static float Volume(Vector3 v) => Mathf.Abs(v.x * v.y * v.z);
    private static string V(Vector3 v) => "(" + v.x.ToString("F3") + "," + v.y.ToString("F3") + "," + v.z.ToString("F3") + ")";
    private static string B(Bounds b) => "center=" + V(b.center) + " size=" + V(b.size);
    private static string HierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
#endif
