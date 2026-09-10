using UnityEditor;
using UnityEngine;
public static class M7_4_NormalizeV007VisualRoot {
 [MenuItem("147VR/M7.4 Normalize V007 Visual Root")]
 public static void Run(){
  const string path="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab";
  GameObject root=null;
  try {
   root=PrefabUtility.LoadPrefabContents(path);
   var t=Find(root.transform,"147VR_TABLE_VISUAL_ROOT");
   if(t==null) throw new System.Exception("147VR_TABLE_VISUAL_ROOT not found");
   Debug.Log($"V007_BEFORE local={t.localScale} world={t.lossyScale}");
   t.localScale=Vector3.one*0.01f;
   EditorUtility.SetDirty(root);
   PrefabUtility.SaveAsPrefabAsset(root,path);
   Debug.Log($"V007_AFTER local={t.localScale} world={t.lossyScale}");
  } catch(System.Exception ex){ Debug.LogError("V007_NORMALIZE_FAILED "+ex); EditorApplication.Exit(1); return; }
  finally { if(root!=null) PrefabUtility.UnloadPrefabContents(root); }
  Debug.Log("V007_NORMALIZE_COMPLETED");
  EditorApplication.Exit(0);
 }
 static Transform Find(Transform t,string n){ if(t.name==n)return t; foreach(Transform c in t){var r=Find(c,n);if(r!=null)return r;} return null; }
}
