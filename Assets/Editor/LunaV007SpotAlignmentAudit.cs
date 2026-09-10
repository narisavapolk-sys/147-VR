using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class LunaV007SpotAlignmentAudit
{
 static readonly (string name,float longZ,float widthX)[] Spots={("Yellow",-1.0475f,-.292f),("Green",-1.0475f,.292f),("Brown",-1.0475f,0f),("Blue",0f,0f),("Pink",.89225f,0f),("Black",1.4605f,0f)};
 public static void Run(){var s=EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity",OpenSceneMode.Single);var t=GameObject.Find("Prefab_WPBSA_12Foot_Snooker");var bed=Find(t.transform,"Bed_Collider");var bc=bed.GetComponent<Collider>().bounds.center;Debug.Log($"[SPOT AUDIT] bedCenter={bc} size={bed.GetComponent<Collider>().bounds.size}");foreach(var q in Spots){var b=Find(t.transform,q.name);if(!b){Debug.Log($"[SPOT] {q.name} MISSING");continue;}var e=new Vector3(bc.x+q.widthX,b.position.y,bc.z-q.longZ);var d=Vector2.Distance(new Vector2(b.position.x,b.position.z),new Vector2(e.x,e.z));Debug.Log($"[SPOT] {q.name} actual={b.position} expected={e} delta={d*1000f:F2}mm");}EditorApplication.Exit(0);}
 static Transform Find(Transform r,string n){foreach(var q in r.GetComponentsInChildren<Transform>(true))if(q.name==n)return q;return null;}
}
