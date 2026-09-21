using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class LunaV007TransformAudit {
 public static void Run(){
  var sc=EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity",OpenSceneMode.Single);
  GameObject v=GameObject.Find("V007_VISUAL_MAIN"); if(!v)throw new Exception("V007 missing");
  Transform surf=null; foreach(var t in v.GetComponentsInChildren<Transform>(true))if(t.name=="TABLE SURFACE")surf=t;
  Dump("V007",v.transform); Dump("Surface",surf);
  for(Transform p=v.transform.parent;p!=null;p=p.parent)Dump("Parent",p);
  var r=surf.GetComponent<Renderer>(); Debug.Log($"[V007 XFORM] worldBounds min={r.bounds.min:F6} max={r.bounds.max:F6} size={r.bounds.size:F6}");
  EditorApplication.Exit(0);
 }
 static void Dump(string label,Transform t){if(!t)return;Debug.Log($"[V007 XFORM] {label} name={t.name} localPos={t.localPosition:F6} localRot={t.localEulerAngles:F3} localScale={t.localScale:F6} worldPos={t.position:F6} worldRot={t.eulerAngles:F3} lossy={t.lossyScale:F6}");}
}
