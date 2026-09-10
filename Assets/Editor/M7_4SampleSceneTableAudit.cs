using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Text;

public static class M7_4SampleSceneTableAudit
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var sb = new StringBuilder();
        sb.AppendLine("=== M7.4 SampleScene TABLE AUDIT ===");
        sb.AppendLine("Scene: " + scene.path);
        foreach (var root in scene.GetRootGameObjects())
        {
            sb.AppendLine($"ROOT name='{root.name}' active={root.activeSelf} children={root.transform.childCount}");
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var n=t.name;
                if (n.ToLowerInvariant().Contains("table") || n.ToLowerInvariant().Contains("snooker") || n.ToLowerInvariant().Contains("pool"))
                {
                    var go=t.gameObject;
                    var src=PrefabUtility.GetCorrespondingObjectFromSource(go);
                    sb.AppendLine($"MATCH name='{go.name}' path='{GetPath(t)}' prefab='{(src?src.name:"<none>")}' asset='{(src?AssetDatabase.GetAssetPath(src):"<none>")}'");
                }
            }
        }
        Debug.Log(sb.ToString());
        Debug.Log("=== END AUDIT ===");
    }
    static string GetPath(Transform t){var s=t.name;while(t.parent!=null){t=t.parent;s=t.name+"/"+s;}return s;}
}
