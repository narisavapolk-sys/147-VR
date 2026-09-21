using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
public static class YOLO_V007_Candidate_Audit_TMP
{
 [MenuItem("147VR/YOLO/V007 Candidate Audit TMP")]
 public static void Run()
 {
  const string p="Assets/AAA/ImportedSnooker/Source/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.fbx";
  var root=AssetDatabase.LoadAssetAtPath<GameObject>(p);
  Debug.Log("=== V007 MARKING CANDIDATE AUDIT ===");
  if(root==null){Debug.LogError("MISSING CANDIDATE");return;}
  var s=Find(root.transform,"TABLE SURFACE");
  if(s==null){Debug.LogError("MISSING TABLE SURFACE");return;}
  var mf=s.GetComponent<MeshFilter>(); var mesh=mf?mf.sharedMesh:null;
  if(mesh==null){Debug.LogError("MISSING SURFACE MESH");return;}
  var uv0=new List<Vector2>(); var uv1=new List<Vector2>();
  mesh.GetUVs(0,uv0); mesh.GetUVs(1,uv1);
  Debug.Log($"surfaceMesh={mesh.name} vertices={mesh.vertexCount} uv0={uv0.Count} uv1={uv1.Count}");
  if(uv1.Count>0){Vector2 min=uv1[0],max=uv1[0];foreach(var u in uv1){min=Vector2.Min(min,u);max=Vector2.Max(max,u);}Debug.Log($"uv1Bounds min={min} max={max}");}
  var r=s.GetComponent<Renderer>(); if(r)Debug.Log($"surfaceBounds center={r.bounds.center} size={r.bounds.size}");
  Debug.Log($"surfaceTransform localScale={s.localScale} rot={s.eulerAngles}");
 }
 static Transform Find(Transform r,string n){foreach(var t in r.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
}
