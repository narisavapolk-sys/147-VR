using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class Phase4A_FixTableSurface {
 const string TablePrefab="Assets/AAA/ImportedSnooker/Prefab_WPBSA_12Foot_Snooker.prefab";
 const string ModelAsset="Assets/AAA/ImportedSnooker/Source/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.fbx";
 const string MeshDir="Assets/AAA/ImportedSnooker/Meshes";
 [MenuItem("147VR/Phase4/Fix TABLE SURFACE Scale")]
 public static void Run(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity",OpenSceneMode.Single);
  var root=PrefabUtility.LoadPrefabContents(TablePrefab); if(root==null){Debug.LogError("[4A FIX] Prefab load failed");return;}
  try{
   var holder=Find(root.transform,"Visual_Meshes_Drop_Here"); if(holder==null){Debug.LogError("[4A FIX] Visual holder not found");return;}
   var old=Find(holder,"147VR_Table_WPBSA_12ft_VISUAL_MAIN"); if(old!=null) Object.DestroyImmediate(old.gameObject);
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelAsset); if(model==null){Debug.LogError("[4A FIX] Model asset not found");return;}
   var visual=PrefabUtility.InstantiatePrefab(model,root.scene) as GameObject; if(visual==null){Debug.LogError("[4A FIX] Model instantiate failed");return;}
   visual.transform.SetParent(holder,false); visual.name="147VR_Table_WPBSA_12ft_VISUAL_MAIN";
   PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   visual.transform.localPosition=Vector3.zero; visual.transform.localRotation=Quaternion.Euler(0f,90f,0f); visual.transform.localScale=Vector3.one;
   var surface=Find(visual.transform,"TABLE SURFACE"); if(surface==null){Debug.LogError("[4A FIX] New SURFACE not found");return;}
   var bakeScale=surface.localScale;
   var filters=surface.GetComponentsInChildren<MeshFilter>(true);
   if(filters.Length==0){Debug.LogError("[4A FIX] No MeshFilter under SURFACE");return;}
   AssetDatabase.CreateFolder("Assets/AAA/ImportedSnooker","Meshes");
   int baked=0;
   foreach(var mf in filters){
    if(mf.sharedMesh==null) continue;
    var mesh=Object.Instantiate(mf.sharedMesh); mesh.name=mf.sharedMesh.name+"_PHASE4A_BAKED";
    var v=mesh.vertices; for(int i=0;i<v.Length;i++) v[i]=Vector3.Scale(v[i],bakeScale); mesh.vertices=v; mesh.RecalculateBounds(); mesh.RecalculateNormals();
    var path=AssetDatabase.GenerateUniqueAssetPath(MeshDir+"/"+mesh.name+".asset"); AssetDatabase.CreateAsset(mesh,path); mf.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path); EditorUtility.SetDirty(mf); baked++;
   }
   surface.localPosition=Vector3.zero; surface.localRotation=Quaternion.identity; surface.localScale=Vector3.one;
   Debug.Log($"[4A FIX] BAKED {baked} mesh(es) by source surface scale {bakeScale}");
   Debug.Log($"[4A FIX] SOURCE VISUAL rebuilt: mainScale={visual.transform.localScale} mainRot={visual.transform.eulerAngles} surfaceScale={surface.localScale} surfaceRot={surface.eulerAngles}");
   bool ok; PrefabUtility.SaveAsPrefabAsset(root,TablePrefab,out ok); Debug.Log($"[4A FIX] PrefabSave={ok}");
  } finally { PrefabUtility.UnloadPrefabContents(root); }
  AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
  var inst=GameObject.Find("Prefab_WPBSA_12Foot_Snooker");
  if(inst!=null){var v=Find(inst.transform,"147VR_Table_WPBSA_12ft_VISUAL_MAIN"); var s=Find(inst.transform,"TABLE SURFACE"); if(v!=null&&s!=null)Debug.Log($"[4A FIX] SCENE VERIFY main={v.lossyScale} rot={v.eulerAngles}; surface={s.localScale} world={s.lossyScale} rot={s.eulerAngles}");}
  Debug.Log($"[4A FIX] SceneSave={EditorSceneManager.SaveScene(scene)}");
 }
 static Transform Find(Transform r,string n){foreach(var t in r.GetComponentsInChildren<Transform>(true)) if(t.name==n)return t; return null;}
}
