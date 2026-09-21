using UnityEditor;
using UnityEngine;
using System.IO;

public static class Create147VRTableVisualPrefab
{
    private const string ModelPath = "Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Base_v001.fbx";
    private const string PrefabPath = "Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_12ft_VISUAL.prefab";

    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new System.Exception("Table FBX not imported: " + ModelPath);
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "AAA/ImportedSnooker/Prefabs"));
        AssetDatabase.Refresh();
        var instance = Object.Instantiate(model);
        instance.name = "147VR_Table_WPBSA_12ft_VISUAL";
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        if (prefab == null) throw new System.Exception("Prefab creation failed: " + PrefabPath);
        Debug.Log("147VR TABLE PREFAB CREATED: " + PrefabPath);
    }
}
