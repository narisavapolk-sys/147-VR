using UnityEditor;
using UnityEngine;
public static class M7_4_CreateV007ScaleWrapper {
 [MenuItem("147VR/M7.4 Create V007 Scale Wrapper")]
 public static void Run(){
  const string src="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab";
  const string dst="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING_NORMALIZED.prefab";
  GameObject instance=null;
  try {
   var srcAsset=AssetDatabase.LoadAssetAtPath<GameObject>(src);
   if(srcAsset==null) throw new System.Exception("V007 source prefab missing");
   var wrapper=new GameObject("147VR_Table_WPBSA_Visual_Clean_v007_MARKING_NORMALIZED");
   instance=(GameObject)PrefabUtility.InstantiatePrefab(srcAsset);
   instance.name=srcAsset.name;
   instance.transform.SetParent(wrapper.transform,false);
   instance.transform.localScale=Vector3.one*0.01f;
   Debug.Log($"WRAPPER_CHILD local={instance.transform.localScale} world={instance.transform.lossyScale}");
   PrefabUtility.SaveAsPrefabAsset(wrapper,dst);
   Object.DestroyImmediate(wrapper);
   AssetDatabase.SaveAssets();
   AssetDatabase.Refresh();
   Debug.Log("V007_WRAPPER_COMPLETED "+dst);
  } catch(System.Exception ex){
   Debug.LogError("V007_WRAPPER_FAILED "+ex);
   if(instance!=null) Object.DestroyImmediate(instance);
   EditorApplication.Exit(1); return;
  }
  EditorApplication.Exit(0);
 }
}
