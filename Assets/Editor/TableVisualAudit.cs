using UnityEditor;
using UnityEngine;
using System.Linq;
public static class TableVisualAudit {
 [MenuItem("147VR/Audit Table Visual")]
 public static void Run() {
  const string p="Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v006.fbx";
  var root=AssetDatabase.LoadAssetAtPath<GameObject>(p);
  if(root==null){Debug.LogError("AUDIT_FAIL missing "+p); return;}
  var rs=root.GetComponentsInChildren<Renderer>(true);
  var mf=root.GetComponentsInChildren<MeshFilter>(true);
  var smr=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
  var b=new Bounds(root.transform.position,Vector3.zero);
  foreach(var r in rs)b.Encapsulate(r.bounds);
  Debug.Log($"AUDIT_ROOT={root.name} renderers={rs.Length} meshFilters={mf.Length} skinned={smr.Length}");
  Debug.Log($"AUDIT_WORLD_BOUNDS center={b.center} size={b.size}");
  foreach(var t in root.GetComponentsInChildren<Transform>(true)) {
   var n=t.name.ToLowerInvariant();
   if(n.Contains("leg")||n.Contains("pocket")||n.Contains("rail")||n.Contains("cushion")||n.Contains("cloth")||n.Contains("baulk")||n.Contains("marker"))
    Debug.Log("AUDIT_PART "+t.name+" path="+GetPath(t,root.transform));
  }
 }
 static string GetPath(Transform t,Transform root){var a=t.name; while(t.parent!=null&&t.parent!=root){t=t.parent;a=t.name+"/"+a;} return a;}
}
