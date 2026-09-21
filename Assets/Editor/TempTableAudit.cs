using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class TempTableAudit
{
    public static void Run()
    {
        var s=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity",OpenSceneMode.Single);
        Debug.Log("[AUDIT] SCENE="+s.path);
        var roots=s.GetRootGameObjects();
        foreach(var r in roots)
        foreach(var t in r.GetComponentsInChildren<Transform>(true))
        {
            if(t.name=="Bed_Collider" || t.name=="TABLE SURFACE" || t.name=="Physics Table (runtime)")
            {
                var c=t.GetComponent<Collider>(); var rr=t.GetComponent<Renderer>();
                Debug.Log($"[AUDIT] {t.name} pos={t.position} scale={t.lossyScale} collider={(c?c.GetType().Name:"NONE")} bounds={(c?c.bounds.ToString():rr?rr.bounds.ToString():"NONE")}");
            }
        }
        EditorApplication.Exit(0);
    }
}
