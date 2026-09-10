using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
public static class M7_4_MainSceneAuthorityAudit
{
 [MenuItem("147VR/M7.4 Authority Audit")]
 public static void Run(){
  var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
  var sb=new StringBuilder(); sb.AppendLine("MAINSCENE AUTHORITY AUDIT");
  var setups=Object.FindObjectsByType<SnookerPhysicsSetup>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  sb.AppendLine("SETUPS="+setups.Length);
  foreach(var s in setups){sb.AppendLine("SETUP="+s.name+" tableRoot="+(s.tableRoot?s.tableRoot.name:"NULL")); if(!s.tableRoot) continue; var bed=FindBed(s.tableRoot); if(bed){sb.AppendLine("BED_PATH="+Path(bed.transform,s.tableRoot));sb.AppendLine("BED_BOUNDS_CENTER="+bed.bounds.center+" SIZE="+bed.bounds.size+" SCALE="+bed.transform.lossyScale);}else sb.AppendLine("BED_NOT_FOUND");}
  var ghosts=Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None); int count=0; foreach(var g in ghosts) if(g.name=="Physics Table (runtime)"){count++; sb.AppendLine("GHOST="+Path(g.transform,null)+" ACTIVE="+g.activeSelf);}
  sb.AppendLine("STATIC_PHYSICS_TABLE_COUNT="+count);
  Directory.CreateDirectory("Docs"); File.WriteAllText("Docs/M7_4_MainScene_Authority_Audit.txt",sb.ToString()); Debug.Log(sb.ToString());
 }
 static Collider FindBed(Transform root){foreach(var c in root.GetComponentsInChildren<Collider>(true)) if(c.name=="Bed_Collider") return c; return null;}
 static string Path(Transform t,Transform stop){var s=t.name; while(t.parent!=null&&t.parent!=stop){t=t.parent;s=t.name+"/"+s;} return s;}
}