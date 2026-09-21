using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class M7_4_OneShotFixAndAudit
{
 const string ScenePath="Assets/Scenes/147VR_MainScene.unity";
 [MenuItem("147VR/M7.4 One Shot Fix And Audit")]
 public static void Run(){
  Debug.Log(">>> [M7.4 ONE-SHOT START] <<<");
  try{
   var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
   var root=GameObject.Find("Prefab_WPBSA_12Foot_Snooker");
   if(root==null){Fail("Physics root missing");return;}
   var bed=FindDeep(root.transform,"Bed_Collider");
   var surface=FindDeep(root.transform,"TABLE SURFACE");
   var col=bed?bed.GetComponent<BoxCollider>():null;
   Debug.Log($"[BEFORE] Bed scale={bed?.lossyScale} size={col?.size} bounds={col?.bounds.size}");
   Debug.Log($"[BEFORE] Surface scale={surface?.lossyScale} localRot={surface?.localEulerAngles} bounds={(surface?.GetComponent<Renderer>()?.bounds.size.ToString()??"NULL")}");
   if(bed==null||surface==null||col==null){Fail("Required physics/visual object missing");return;}
   Undo.RecordObject(surface,"M7.4 Align Table Surface To Physics");
   var e=surface.localEulerAngles;
   surface.localRotation=Quaternion.Euler(e.x,90f,e.z);
   PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
   EditorSceneManager.MarkSceneDirty(scene);
   EditorSceneManager.SaveScene(scene);
   var r=surface.GetComponent<Renderer>()??surface.GetComponentInChildren<Renderer>(true);
   var bs=r?r.bounds.size:Vector3.zero;
   Debug.Log($"[AFTER] Surface scale={surface.lossyScale} localRot={surface.localEulerAngles} bounds={bs}");
   Debug.Log($"[PHYSICS] Bed WorldScale={bed.lossyScale}");
   Debug.Log($"[PHYSICS] Bed Size={col.size}");
   Debug.Log($"[PHYSICS] Bed WorldBounds={col.bounds.size}");
   Debug.Log($"[VISUAL] TABLE SURFACE WorldBounds={bs}");
   bool p1=Approx(bed.lossyScale,Vector3.one),p2=Approx(col.size,new Vector3(1.778f,.05f,3.569f));
   bool p3=Mathf.Abs(bs.x-1.778f)<.01f&&Mathf.Abs(bs.z-3.569f)<.01f;
   Debug.Log($"[GATE] Physics WorldScale OK = {p1}");
   Debug.Log($"[GATE] Physics Collider Size OK = {p2}");
   Debug.Log($"[GATE] Visual Surface Size OK = {p3}");
   bool pass=p1&&p2&&p3;
   Debug.Log(pass?">>> [M7.4 ONE-SHOT PASS] <<<":">>> [M7.4 ONE-SHOT FAIL] <<<");
   Debug.Log(">>> [M7.4 ONE-SHOT COMPLETED] <<<");
   EditorApplication.Exit(pass?0:1);
  }catch(System.Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
 }
 static Transform FindDeep(Transform r,string n){if(r.name==n)return r;foreach(Transform c in r){var x=FindDeep(c,n);if(x!=null)return x;}return null;}
 static bool Approx(Vector3 a,Vector3 b,float t=.0005f)=>Mathf.Abs(a.x-b.x)<=t&&Mathf.Abs(a.y-b.y)<=t&&Mathf.Abs(a.z-b.z)<=t;
 static void Fail(string s){Debug.LogError(">>> [M7.4 ONE-SHOT FAIL] <<< "+s);EditorApplication.Exit(1);}
}