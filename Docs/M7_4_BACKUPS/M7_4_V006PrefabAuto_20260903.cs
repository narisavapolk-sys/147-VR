using UnityEditor;
using UnityEngine;
using System.IO;
[InitializeOnLoad]
public static class M7_4_V006PrefabAuto
{
    const string Done="M7_4_V006_PREFAB_DONE";
    static M7_4_V006PrefabAuto(){ EditorApplication.delayCall+=RunOnce; }
    static void RunOnce(){
        if(EditorPrefs.GetBool(Done,false)) return;
        const string fbx="Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v006.fbx";
        const string prefab="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006.prefab";
        const string compat="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004.prefab";
        AssetDatabase.ImportAsset(fbx,ImportAssetOptions.ForceSynchronousImport);
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if(!src){ Debug.LogError("V006 FBX load failed"); return; }
        var dir=Path.GetDirectoryName(prefab).Replace("\\","/");
        if(!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/AAA/ImportedSnooker","Prefabs");
        var inst=(GameObject)PrefabUtility.InstantiatePrefab(src); inst.name="147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006";
        bool ok; PrefabUtility.SaveAsPrefabAsset(inst,prefab,out ok); Object.DestroyImmediate(inst);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        if(ok){
            var inst2=(GameObject)PrefabUtility.InstantiatePrefab(src); inst2.name="147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004";
            bool ok2; PrefabUtility.SaveAsPrefabAsset(inst2,compat,out ok2); Object.DestroyImmediate(inst2);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            if(ok2){ EditorPrefs.SetBool(Done,true); Debug.Log("M7_4 V006 + V004 COMPAT PREFABS PASS"); }
            else Debug.LogError("V004 COMPAT PREFAB FAIL");
        } else Debug.LogError("M7_4 V006 PREFAB FAIL");
    }
}
