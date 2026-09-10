using UnityEditor;
using UnityEngine;
using System;

public static class M7_4_NormalizeV006ImportScale
{
    [MenuItem("Tools/147VR/M7.4 Normalize V006 Import Scale")]
    public static void Run()
    {
        const string path = "Assets/BlenderTest/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006.fbx";
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) throw new Exception("V006 ModelImporter not found");
        Debug.Log($"M7.4 SCALE BEFORE | globalScale={importer.globalScale}");
        importer.globalScale = 0.01f;
        importer.SaveAndReimport();
        var check = AssetImporter.GetAtPath(path) as ModelImporter;
        Debug.Log($"M7.4 SCALE AFTER | globalScale={check.globalScale}");
        if (Mathf.Abs(check.globalScale - 0.01f) > 0.00001f)
            throw new Exception("V006 globalScale verification failed");
        AssetDatabase.SaveAssets();
        Debug.Log("M7.4 V006 IMPORT SCALE NORMALIZED | 0.01 | Unity importer API");
    }
}
