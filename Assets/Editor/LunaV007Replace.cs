using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class LunaV007Replace
{
 [MenuItem("147VR/LUNA/Replace V007 Visual With Marking Prefab")]
 public static void Run(){
  const string scenePath="Assets/Scenes/147VR_MainScene.unity";
  const string prefabPath="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab";
  var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
  GameObject old=null; Transform parent=null; Vector3 pos=Vector3.zero; Quaternion rot=Quaternion.identity; Vector3 scale=Vector3.one;
  foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name=="V007_VISUAL_MAIN"){old=t.gameObject;parent=t.parent;pos=t.localPosition;rot=t.localRotation;scale=t.localScale;break;}
  if(old==null){Debug.LogError("[V007 REPLACE] Existing V007_VISUAL_MAIN not found");return;}
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath); if(prefab==null){Debug.LogError("[V007 REPLACE] MARKING prefab missing");return;}
  Object.DestroyImmediate(old);
  var fresh=PrefabUtility.InstantiatePrefab(prefab,scene) as GameObject; if(fresh==null)throw new System.Exception("Instantiate MARKING prefab failed");
  fresh.name="V007_VISUAL_MAIN"; fresh.transform.SetParent(parent,false); fresh.transform.localPosition=pos; fresh.transform.localRotation=rot; fresh.transform.localScale=scale;
  EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene))throw new System.Exception("SaveScene failed");
  Debug.Log("[V007 REPLACE] SAVED correct MARKING prefab instance under original parent.");
 }
}
