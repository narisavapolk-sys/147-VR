using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Text;

public static class M7_4SceneAudit
{
    public static void Run()
    {
        const string scenePath="Assets/Scenes/147VR_MainScene.unity";
        const string outPath="Docs/M7_4_MAINSCENE_VISUAL_AUDIT.txt";
        var scene=EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var sb=new StringBuilder();
        sb.AppendLine("M7.4 MAIN SCENE VISUAL AUDIT");
        sb.AppendLine("Scene="+scene.path);
        foreach(var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            string n=go.name.ToLowerInvariant();
            if(n.Contains("table")||n.Contains("surface")||n.Contains("concert")||n.Contains("physics"))
            {
                var r=go.GetComponent<Renderer>();
                if(r!=null)
                    sb.AppendLine($"RENDERER|active={go.activeInHierarchy}|name={go.name}|parent={(go.transform.parent?go.transform.parent.name:"<root>")}|pos={go.transform.position}|scale={go.transform.lossyScale}|bounds={r.bounds.size}|mat={(r.sharedMaterial?r.sharedMaterial.name:"<none>")}");
                else
                    sb.AppendLine($"GO|active={go.activeInHierarchy}|name={go.name}|parent={(go.transform.parent?go.transform.parent.name:"<root>")}|pos={go.transform.position}|scale={go.transform.lossyScale}");
            }
        }
        File.WriteAllText(outPath,sb.ToString(),Encoding.UTF8);
        Debug.Log("[M7.4] Scene audit written: "+outPath);
        EditorApplication.Exit(0);
    }
}
