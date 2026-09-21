using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Text;

public static class YOLO_V007_Audit_TMP
{
    [MenuItem("147VR/YOLO/V007 Audit TMP")]
    public static void Run()
    {
        const string scenePath = "Assets/Scenes/147VR_MainScene.unity";
        const string assetPath = "Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v007.fbx";
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        var sb = new StringBuilder();
        sb.AppendLine("=== YOLO V007 AUDIT V2 ===");
        sb.AppendLine("asset=" + assetPath + " loaded=" + (root != null));
        if (root != null)
        {
            var surface = Find(root.transform, "TABLE SURFACE");
            if (surface != null)
            {
                var r = surface.GetComponent<Renderer>();
                var mf = surface.GetComponent<MeshFilter>();
                sb.AppendLine($"assetSurface localScale={surface.localScale} worldScale={surface.lossyScale} rot={surface.eulerAngles}");
                if (r != null) sb.AppendLine($"assetSurfaceRendererBounds center={r.bounds.center} size={r.bounds.size}");
                if (mf != null && mf.sharedMesh != null) sb.AppendLine($"assetSurfaceMeshBounds center={mf.sharedMesh.bounds.center} size={mf.sharedMesh.bounds.size} vertices={mf.sharedMesh.vertexCount}");
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant();
                if (n == "yellow" || n == "green" || n == "brown" || n == "blue" || n == "pink" || n == "black" || n == "white" || n == "cueball")
                    sb.AppendLine($"assetBall name={t.name} local={t.localPosition} world={t.position} scale={t.lossyScale}");
            }
        }
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        sb.AppendLine("scene=" + scene.path);
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var n = t.name.ToLowerInvariant();
            if (n == "v007_visual_main" || n == "table surface" || n == "bed_collider" || n == "yellow" || n == "green" || n == "brown" || n == "blue" || n == "pink" || n == "black" || n == "white" || n == "cueball")
            {
                var r = t.GetComponent<Renderer>();
                var line = $"sceneObj name={t.name} path={Path(t)} active={t.gameObject.activeInHierarchy} localScale={t.localScale} worldScale={t.lossyScale} worldPos={t.position} rot={t.eulerAngles}";
                if (r != null) line += $" boundsCenter={r.bounds.center} boundsSize={r.bounds.size}";
                sb.AppendLine(line);
            }
        }
        Debug.Log(sb.ToString());
    }
    static Transform Find(Transform r, string n){foreach(var t in r.GetComponentsInChildren<Transform>(true)) if(t.name==n)return t; return null;}
    static string Path(Transform t){var s=t.name; while(t.parent!=null){t=t.parent;s=t.name+"/"+s;} return s;}
}
