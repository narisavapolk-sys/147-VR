using UnityEditor;
using UnityEngine;
using System.IO;
public static class M7_4_CreateVisualV006Prefab
{
    public static void Run()
    {
        const string fbx="Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v006.fbx";
        const string prefab="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006.prefab";
        AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceSynchronousImport);
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if(!src) throw new System.Exception("FBX load failed: "+fbx);
        var dir=Path.GetDirectoryName(prefab).Replace("\\","/");
        if(!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/AAA/ImportedSnooker","Prefabs");
        var inst=(GameObject)PrefabUtility.InstantiatePrefab(src);
        inst.name="147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006";
        bool ok; PrefabUtility.SaveAsPrefabAsset(inst,prefab,out ok);
        Object.DestroyImmediate(inst);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("M7_4 V006 PREFAB: "+(ok?"PASS ":"FAIL ")+prefab);
        if(!ok) throw new System.Exception("Prefab save failed");
    }
}
