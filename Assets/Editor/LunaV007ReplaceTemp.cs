using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class LunaV007ReplaceTemp
{
 public static void Run(){
  const string src="Assets/Scenes/147VR_MainScene.unity";
  const string tmp="Assets/Scenes/147VR_MainScene_V007_PROMOTED_TEMP.unity";
  const string prefab="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab";
  var s=EditorSceneManager.OpenScene(src,OpenSceneMode.Single); GameObject old=null; Transform parent=null; Vector3 p=Vector3.zero; Quaternion q=Quaternion.identity; Vector3 sc=Vector3.one;
  foreach(var root in s.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name=="V007_VISUAL_MAIN"){old=t.gameObject;parent=t.parent;p=t.localPosition;q=t.localRotation;sc=t.localScale;break;}
  if(old==null)throw new System.Exception("Existing V007_VISUAL_MAIN not found"); var asset=AssetDatabase.LoadAssetAtPath<GameObject>(prefab); if(asset==null)throw new System.Exception("MARKING prefab missing");
  Object.DestroyImmediate(old); var fresh=PrefabUtility.InstantiatePrefab(asset,s) as GameObject; if(fresh==null)throw new System.Exception("Instantiate failed"); fresh.name="V007_VISUAL_MAIN"; fresh.transform.SetParent(parent,false); fresh.transform.localPosition=p; fresh.transform.localRotation=q; fresh.transform.localScale=sc;
  EditorSceneManager.MarkSceneDirty(s); if(!EditorSceneManager.SaveScene(s,tmp,false))throw new System.Exception("Save temp scene failed");
  Debug.Log("[V007 TEMP] SAVED="+tmp+"; replace source after Unity exits.");
 }
}
