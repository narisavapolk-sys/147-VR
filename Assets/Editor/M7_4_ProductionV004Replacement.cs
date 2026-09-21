using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class M7_4_ProductionV004Replacement
{
 const string ScenePath="Assets/Scenes/SampleScene.unity", PrefabPath="Assets/BlenderTest/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004.prefab", OldName="147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v003", NewName="147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004";
 public static void Execute()
 {
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single); var old=GameObject.Find(OldName); if(old==null)throw new System.Exception("Old V003 visual root not found.");
  var sr=Find(old.transform,"TABLE SURFACE")?.GetComponent<Renderer>(); if(sr==null)throw new System.Exception("Authoritative TABLE SURFACE missing.");
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); if(prefab==null)throw new System.Exception("V004 prefab missing.");
  var existing=GameObject.Find(NewName); if(existing!=null)Object.DestroyImmediate(existing);
  var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene); visual.name=NewName; visual.transform.SetParent(old.transform.parent,true); visual.transform.rotation=Quaternion.identity; visual.transform.localScale=Vector3.one;
  var names=new[]{"Pocket_FL","Pocket_FR","Pocket_ML","Pocket_MR","Pocket_BL","Pocket_BR"}; var pockets=new Transform[6]; for(int i=0;i<6;i++){pockets[i]=Find(visual.transform,names[i]);if(pockets[i]==null)throw new System.Exception("Missing pocket: "+names[i]);}
  var target=sr.bounds; Debug.Log($"M7.4 DIAG target={target.center} size={target.size} top={target.max.y}"); for(int i=0;i<6;i++)Debug.Log($"M7.4 DIAG {names[i]} pos={pockets[i].position} local={pockets[i].localPosition}");
  visual.transform.position+=new Vector3(target.min.x,target.max.y,target.min.z)-pockets[0].position;
  var expected=new[]{new Vector3(target.min.x,target.max.y,target.min.z),new Vector3(target.max.x,target.max.y,target.min.z),new Vector3(target.min.x,target.max.y,target.center.z),new Vector3(target.max.x,target.max.y,target.center.z),new Vector3(target.min.x,target.max.y,target.max.z),new Vector3(target.max.x,target.max.y,target.max.z)};
  float maxErr=0f; for(int i=0;i<6;i++)maxErr=Mathf.Max(maxErr,Vector3.Distance(pockets[i].position,expected[i])); Debug.Log($"M7.4 DIAG aligned pocketMaxError={maxErr:F6} root={visual.transform.position}");
  if(maxErr>0.010f)throw new System.Exception($"Pocket alignment failed: maxError={maxErr:F6}m");
  foreach(var c in visual.GetComponentsInChildren<Collider>(true))c.enabled=false; foreach(var rb in visual.GetComponentsInChildren<Rigidbody>(true))rb.isKinematic=true; foreach(var r in old.GetComponentsInChildren<Renderer>(true))r.enabled=false;
  Debug.Log($"M7.4 ALIGN PASS | physics surface={target.size.x:F6}x{target.size.z:F6} topY={target.max.y:F6} | pocketMaxError={maxErr:F6}m"); Debug.Log($"M7.4 ALIGN AXIS PASS | scale={visual.transform.lossyScale} rotation={visual.transform.eulerAngles}"); Debug.Log("M7.4 ALIGN POCKET PASS | 6/6 pocket anchors aligned to physics playfield.");
  EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene))throw new System.Exception("SampleScene save failed."); AssetDatabase.SaveAssets(); Debug.Log("M7.4 PRODUCTION V004 REPLACEMENT PASS | V003 renderers disabled, V004 active, Physics Authority preserved.");
 }
 static Transform Find(Transform root,string name){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
}
