using UnityEditor;
using UnityEngine;
using System.IO;

public static class M7_4CreateV007MarkingAsset
{
    public static void Run()
    {
        const string fbxPath = "Assets/AAA/ImportedSnooker/Source/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.fbx";
        const string basePath = "Assets/AAA/ImportedSnooker/Textures/Green_Felt_Texture_V007.png";
        const string markPath = "Assets/AAA/ImportedSnooker/Textures/Snooker_Markings_V007.png";
        const string matPath = "Assets/AAA/ImportedSnooker/Materials/M_V007_TableSurface_Marking.mat";
        const string prefabPath = "Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab";
        Directory.CreateDirectory(Path.GetDirectoryName(matPath));
        Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
        AssetDatabase.Refresh();
        var shader = Shader.Find("147VR/Table Surface Marking");
        if (shader == null) throw new System.Exception("Marking shader not found");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath) ?? new Material(shader);
        mat.shader = shader;
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(basePath));
        mat.SetTexture("_MarkingMap", AssetDatabase.LoadAssetAtPath<Texture2D>(markPath));
        mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_MarkingStrength", 1f); mat.SetFloat("_Smoothness", .35f);
        CreateAssetIfNeeded(mat, matPath); EditorUtility.SetDirty(mat); AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (source == null) throw new System.Exception("V007 FBX not imported");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        int applied = 0;
        foreach (var r in renderers)
            if (r.name == "TABLE SURFACE") { r.sharedMaterial = mat; applied++; }
        if (applied != 1) throw new System.Exception("Expected exactly one TABLE SURFACE, found " + applied);
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("[M7.4] V007 marking candidate created: " + prefabPath);
    }
    static void CreateAssetIfNeeded(Object obj, string path) { if (AssetDatabase.LoadAssetAtPath<Object>(path) == null) AssetDatabase.CreateAsset(obj, path); }
}
