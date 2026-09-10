using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class YOLO_Promote_V007_MarkingPrefab_TMP
{
 [MenuItem("147VR/YOLO/Promote V007 Marking Prefab")]
 public static void Run(){
  const string scenePath="Assets/Scenes/147VR_MainScene.unity";
  const string prefabPath="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab";
  var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single); Transform table=null;
  foreach(var go in scene.GetRootGameObjects())if(go.name=="Prefab_WPBSA_12Foot_Snooker")table=go.transform;
  var old=table?FindDirect(table,"V007_VISUAL_MAIN"):null;
  if(old==null){Debug.LogError("[V007 PROMOTE2] V007 root missing");return;}
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath); if(prefab==null){Debug.LogError("[V007 PROMOTE2] prefab missing");return;}
  var parent=old.parent; var pos=old.localPosition; var rot=old.localRotation;
  old.name="V007_VISUAL_MAIN_LEGACY_DISABLED"; old.gameObject.SetActive(false);
  var fresh=PrefabUtility.InstantiatePrefab(prefab,scene) as GameObject; if(fresh==null){Debug.LogError("[V007 PROMOTE2] instantiate failed");old.gameObject.SetActive(true);old.name="V007_VISUAL_MAIN";return;}
  fresh.transform.SetParent(parent,false); fresh.name="V007_VISUAL_MAIN"; fresh.transform.localPosition=pos; fresh.transform.localRotation=rot;
  Transform surface=Find(fresh.transform,"TABLE SURFACE");
  Renderer r=surface!=null?surface.GetComponent<Renderer>():null;
  MeshFilter mf=surface!=null?surface.GetComponent<MeshFilter>():null;
  if(r==null||mf==null||mf.sharedMesh==null){Debug.LogError("[V007 PROMOTE2] surface renderer/mesh missing");Object.DestroyImmediate(fresh);old.gameObject.SetActive(true);old.name="V007_VISUAL_MAIN";return;}
  EditorSceneManager.MarkSceneDirty(scene); var saved=EditorSceneManager.SaveScene(scene);
  Debug.Log($"[V007 PROMOTE2] saved={saved} rootScale={fresh.transform.localScale} surfaceScale={surface.lossyScale} bounds={r.bounds.size} uv2={mf.sharedMesh.uv2.Length} material={r.sharedMaterial.name}");
 }
 static Transform FindDirect(Transform r,string n){foreach(Transform c in r)if(c.name==n)return c;return null;}
 static Transform Find(Transform r,string n){foreach(Transform t in r.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
}
