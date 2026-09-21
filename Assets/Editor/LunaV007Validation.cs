using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public static class LunaV007Validation
{
 public static void Run(){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity",OpenSceneMode.Single); GameObject v=null;
  foreach(var root in s.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name=="V007_VISUAL_MAIN"){v=t.gameObject;break;}
  if(!v)throw new System.Exception("V007_VISUAL_MAIN missing");
  var surf=Find(v.transform,"TABLE SURFACE"); var mf=surf?surf.GetComponent<MeshFilter>():null; var r=surf?surf.GetComponent<Renderer>():null; var uv1=new List<Vector2>(); if(mf&&mf.sharedMesh)mf.sharedMesh.GetUVs(1,uv1);
  Debug.Log($"[V007] parent={v.transform.parent?.name} rootScale={v.transform.localScale.ToString("F6")} surfaceBounds={(r?r.bounds.size.ToString("F6"):"-")} uv1={uv1.Count} material={(r&&r.sharedMaterial?r.sharedMaterial.name:"-")} shader={(r&&r.sharedMaterial&&r.sharedMaterial.shader?r.sharedMaterial.shader.name:"-")}");
  string[] names={"White_CueBall","Yellow","Green","Brown","Blue","Pink","Black"}; foreach(var n in names){var t=FindScene(s,n);if(t)Debug.Log($"[BALLCENTER] {n} pos={t.position.ToString("F6")}");else Debug.Log($"[BALLCENTER] {n} MISSING");}
  var beds=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.name=="Bed_Collider").ToArray(); Debug.Log($"[PHYSICS] Bed_Collider count={beds.Length}"); foreach(var b in beds)Debug.Log($"[PHYSICS] Bed {b.transform.position.ToString("F6")} scale={b.transform.lossyScale.ToString("F6")} enabled={b.enabled}");
  EditorApplication.Exit(0);
 }
 static Transform Find(Transform r,string n){foreach(var t in r.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
 static Transform FindScene(UnityEngine.SceneManagement.Scene s,string n){foreach(var root in s.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
}
