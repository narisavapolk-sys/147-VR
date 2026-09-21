using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class YOLO_Promote_V007_MarkingUV_TMP
{
 [MenuItem("147VR/YOLO/Promote V007 MarkingUV")]
 public static void Run()
 {
  const string scenePath="Assets/Scenes/147VR_MainScene.unity";
  const string candidatePath="Assets/AAA/ImportedSnooker/Source/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.fbx";
  const string matPath="Assets/AAA/ImportedSnooker/Materials/M_V007_TableSurface_Marking.mat";
  var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
  Transform table=null; foreach(var go in scene.GetRootGameObjects()) if(go.name=="Prefab_WPBSA_12Foot_Snooker") table=go.transform;
  var old=table?Find(table,"V007_VISUAL_MAIN"):null;
  if(old==null){Debug.LogError("[V007 PROMOTE] active V007 root missing");return;}
  var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath); var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
  if(candidate==null||mat==null){Debug.LogError("[V007 PROMOTE] candidate or material missing");return;}
  var parent=old.parent; var pos=old.localPosition; var rot=old.localRotation; var scale=old.localScale;
  old.name="V007_VISUAL_MAIN_LEGACY_DISABLED"; old.gameObject.SetActive(false);
  var fresh=PrefabUtility.InstantiatePrefab(candidate,scene) as GameObject;
  if(fresh==null){Debug.LogError("[V007 PROMOTE] instantiate failed");old.gameObject.SetActive(true);old.name="V007_VISUAL_MAIN";return;}
  fresh.transform.SetParent(parent,false); fresh.name="V007_VISUAL_MAIN"; fresh.transform.localPosition=pos; fresh.transform.localRotation=rot; fresh.transform.localScale=scale;
  var surface=Find(fresh.transform,"TABLE SURFACE"); var r=surface?surface.GetComponent<Renderer>():null;
  if(r==null){Debug.LogError("[V007 PROMOTE] TABLE SURFACE renderer missing");Object.DestroyImmediate(fresh);old.gameObject.SetActive(true);old.name="V007_VISUAL_MAIN";return;}
  r.sharedMaterial=mat; EditorSceneManager.MarkSceneDirty(scene); var saved=EditorSceneManager.SaveScene(scene);
  Debug.Log($"[V007 PROMOTE] PASS saved={saved} oldDisabled={!old.gameObject.activeSelf} new={fresh.name} surfaceScale={surface.lossyScale} surfaceBounds={r.bounds.size} shader={mat.shader.name}");
 }
 static Transform Find(Transform r,string n){foreach(var t in r.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
}
