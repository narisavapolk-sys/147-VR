using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
public static class YOLO_V007_UV_Audit_TMP
{
 [MenuItem("147VR/YOLO/V007 UV Audit TMP")]
 public static void Run()
 {
  const string p="Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v007.fbx";
  var root=AssetDatabase.LoadAssetAtPath<GameObject>(p);
  Debug.Log("=== V007 UV AUDIT ===");
  if(root==null){Debug.LogError("MISSING V007");return;}
  var s=Find(root.transform,"TABLE SURFACE");
  if(s==null){Debug.LogError("MISSING TABLE SURFACE");return;}
  var mf=s.GetComponent<MeshFilter>(); var mesh=mf?mf.sharedMesh:null;
  if(mesh==null){Debug.LogError("MISSING SURFACE MESH");return;}
  var uv0=new List<Vector2>(); var uv1=new List<Vector2>(); var uv2=new List<Vector2>();
  mesh.GetUVs(0,uv0); mesh.GetUVs(1,uv1); mesh.GetUVs(2,uv2);
  Debug.Log($"surfaceMesh={mesh.name} vertices={mesh.vertexCount} submeshes={mesh.subMeshCount} uv0={uv0.Count} uv1={uv1.Count} uv2={uv2.Count}");
  if(uv1.Count>0){Vector2 min=uv1[0],max=uv1[0];foreach(var u in uv1){min=Vector2.Min(min,u);max=Vector2.Max(max,u);}Debug.Log($"uv1Bounds min={min} max={max}");}
  var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
  var active=FindActive(scene,"V007_VISUAL_MAIN/TABLE SURFACE");
  if(active!=null){var r=active.GetComponent<Renderer>();Debug.Log($"sceneSurface material={(r&&r.sharedMaterial?r.sharedMaterial.name:"<none>")} shader={(r&&r.sharedMaterial&&r.sharedMaterial.shader?r.sharedMaterial.shader.name:"<none>")}");}
 }
 static Transform Find(Transform r,string n){foreach(var t in r.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
 static Transform FindActive(UnityEngine.SceneManagement.Scene scene,string suffix){foreach(var go in scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true)){var path=t.name;var q=t;while(q.parent!=null){q=q.parent;path=q.name+"/"+path;}if(path.EndsWith(suffix))return t;}return null;}
}
