using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class M5InspectTableFrame_TMP
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity", OpenSceneMode.Single);
        var root = GameObject.Find("Prefab_WPBSA_12Foot_Snooker");
        var quest = GameObject.Find("Quest Setup");
        if (quest != null) quest.SetActive(true);
        var physics = Object.FindFirstObjectByType<SnookerPhysicsSetup>(FindObjectsInactive.Include);
        if (physics != null) physics.EnsurePhysics();
        using (var w = new StreamWriter("Docs/M5_TABLE_FRAME_INSPECT_20260919.txt", false))
        {
            w.WriteLine("PHYS="+(physics != null ? ("bounds="+physics.TableBounds+" topY="+physics.SurfaceTopY) : "NULL"));
            w.WriteLine("ROOT="+Describe(root != null ? root.transform : null));
            if (root != null)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "TABLE SURFACE" || t.name == "Bed_Collider" ||
                        t.name.Contains("V007_VISUAL_MAIN") || t.name == "White_CueBall")
                    {
                        w.WriteLine("MATCH "+PathOf(t));
                        w.WriteLine("  "+Describe(t));
                        var r = t.GetComponent<Renderer>();
                        if (r != null) w.WriteLine("  RENDERER_BOUNDS="+r.bounds);
                        var c = t.GetComponent<Collider>();
                        if (c != null) w.WriteLine("  COLLIDER_BOUNDS="+c.bounds+" type="+c.GetType().Name);
                    }
                }
            }
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name == "TABLE SURFACE" || go.name == "Bed_Collider")
                {
                    var t=go.transform;
                    w.WriteLine("GLOBAL "+PathOf(t));
                    w.WriteLine("  "+Describe(t));
                    var r=t.GetComponent<Renderer>(); if(r!=null) w.WriteLine("  RENDERER_BOUNDS="+r.bounds);
                    var c=t.GetComponent<Collider>(); if(c!=null) w.WriteLine("  COLLIDER_BOUNDS="+c.bounds+" type="+c.GetType().Name);
                }
            }
        }
        EditorApplication.Exit(0);
    }
    static string Describe(Transform t)
    {
        if(t==null) return "NULL";
        return "pos="+t.position+" rotEuler="+t.eulerAngles+" scale="+t.lossyScale+" localPos="+t.localPosition+" localRot="+t.localEulerAngles+" localScale="+t.localScale+" active="+t.gameObject.activeInHierarchy;
    }
    static string PathOf(Transform t)
    {
        var s=t.name; while(t.parent!=null){t=t.parent;s=t.name+"/"+s;} return s;
    }
}