using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Text;
public static class M7_4_V006AlignmentAudit_TMP
{
 const string ScenePath="Assets/Scenes/147VR_MainScene.unity";
 public static void Execute(){
  var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single); var b=new StringBuilder();
  b.AppendLine("=== V006 ALIGNMENT AUDIT ===");
  foreach(var root in s.GetRootGameObjects()){
   if(root.name.Contains("WPBSA")||root.name.Contains("Physics")||root.name.Contains("Table")) Audit(root.transform,b);
  }
  Debug.Log(b.ToString());
 }
 static void Audit(Transform t,StringBuilder b){
  foreach(var x in t.GetComponentsInChildren<Transform>(true)){
   string n=x.name.ToLowerInvariant(); if(n.Contains("wpbsa")||n.Contains("visual_meshes")||n.Contains("bed_collider")||n.Contains("cushion")||n.Contains("pocket")||n.Contains("rail")||n.Contains("table surface")){
    var rs=x.GetComponentsInChildren<Renderer>(true); var cs=x.GetComponentsInChildren<Collider>(true);
    b.AppendLine($"NODE {x.name} path={Path(x)} pos={x.position} scale={x.lossyScale} renderers={rs.Length} colliders={cs.Length}");
    foreach(var r in rs) b.AppendLine($"  RENDER {r.name} bounds={r.bounds.size} center={r.bounds.center} enabled={r.enabled}");
    foreach(var c in cs) b.AppendLine($"  COLL {c.GetType().Name} {c.name} enabled={c.enabled} trigger={c.isTrigger} center={c.bounds.center} size={c.bounds.size}");
   }
  }
 }
 static string Path(Transform t){var s=t.name;while(t.parent!=null){t=t.parent;s=t.name+"/"+s;}return s;}
}