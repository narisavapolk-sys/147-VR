using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class LunaV007PlayModeValidation
{
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity",OpenSceneMode.Single);
  EditorApplication.playModeStateChanged+=OnState; EditorApplication.isPlaying=true;
 }
 static void OnState(PlayModeStateChange state){
  if(state!=PlayModeStateChange.EnteredPlayMode)return;
  var v=GameObject.Find("V007_VISUAL_MAIN"); var s=v?Find(v.transform,"TABLE SURFACE"):null; var r=s?s.GetComponent<Renderer>():null;
  Debug.Log($"[V007 PLAYMODE] EnteredPlayMode=true visual={(v?"FOUND":"MISSING")} surface={(s?"FOUND":"MISSING")} bounds={(r?r.bounds.size.ToString("F6"):"-")}");
  EditorApplication.playModeStateChanged-=OnState; EditorApplication.isPlaying=false;
 }
 static Transform Find(Transform r,string n){foreach(var t in r.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
}
