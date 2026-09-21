using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class LunaV007FrameAudit {
 public static void Run(){
  var sc=EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity",OpenSceneMode.Single);
  var root=GameObject.Find("Prefab_WPBSA_12Foot_Snooker"); if(!root)throw new Exception("table root missing");
  Dump("TABLE",root.transform);
  foreach(var t in root.GetComponentsInChildren<Transform>(true)){
   if(t.name=="Bed_Collider"||t.name=="TABLE SURFACE"||t.name=="Anchor_Playfield_Center"){
    Dump(t.name,t); var c=t.GetComponent<Collider>(); if(c)Debug.Log($"[FRAME] {t.name} colliderBounds center={c.bounds.center:F6} size={c.bounds.size:F6} enabled={c.enabled}"); var r=t.GetComponent<Renderer>(); if(r)Debug.Log($"[FRAME] {t.name} rendererBounds center={r.bounds.center:F6} size={r.bounds.size:F6}");
   }
  }
  foreach(var n in new[]{"Yellow","Green","Brown","Blue","Pink","Black","White_CueBall"}){var t=Find(root.transform,n);if(t)Debug.Log($"[FRAME BALL] {n} local={t.localPosition:F6} world={t.position:F6}");}
  EditorApplication.Exit(0);
 }
 static Transform Find(Transform r,string n){foreach(var t in r.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
 static void Dump(string n,Transform t){Debug.Log($"[FRAME] {n} localPos={t.localPosition:F6} localRot={t.localEulerAngles:F3} localScale={t.localScale:F6} worldPos={t.position:F6} worldRot={t.eulerAngles:F3} lossy={t.lossyScale:F6}");}
}
