using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class M7_4_TruthAudit {
 [MenuItem("147VR/M7.4 Truth Audit")]
 public static void Run(){
  Debug.Log(">>> [TRUTH_AUDIT_ENTRY_POINT_TRIGGERED] <<<");
  try {
   var s=EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity",OpenSceneMode.Single);
   Debug.Log("AUDIT_SCENE="+s.path);
   var roots=s.GetRootGameObjects();
   foreach(var r in roots) if(r.name.Contains("ConcertRoom")||r.name.Contains("Prefab_WPBSA")||r.name.Contains("tableRoot")) Dump(r.transform,0);
   foreach(var c in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
    if(c.name=="Bed_Collider") Debug.Log($"BED worldScale={c.transform.lossyScale} size={c.size} bounds={c.bounds.size} center={c.bounds.center}");
   foreach(var tr in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
    if(tr.name=="TABLE SURFACE") Debug.Log($"SURFACE worldScale={tr.lossyScale} pos={tr.position}");
   Debug.Log(">>> [TRUTH_AUDIT_COMPLETED] <<<");
   EditorApplication.Exit(0);
  } catch (System.Exception ex) {
   Debug.LogError(">>> [TRUTH_AUDIT_CRASHED] : "+ex);
   EditorApplication.Exit(1);
  }
 }
 static void Dump(Transform t,int d){
  Debug.Log(new string(' ',d*2)+t.name+" local="+t.localScale+" world="+t.lossyScale+" pos="+t.position);
  foreach(Transform x in t) Dump(x,d+1);
 }
}
