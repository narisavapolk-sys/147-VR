using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class M7_4_CreateCleanMainScene
{
    const string SourcePath = "Assets/Scenes/SampleScene.unity";
    const string MainPath = "Assets/Scenes/147VR_MainScene.unity";
    const string BackupPath = "Assets/Scenes/SampleScene_PRE_M7_4_CLEAN_MAIN_20260903.unity";

    [MenuItem("147VR/M7.4/Create Clean Main Scene")]
    public static void Run()
    {
        string root = Directory.GetCurrentDirectory();
        string sourceFile = Path.Combine(root, SourcePath);
        string mainFile = Path.Combine(root, MainPath);
        string backupFile = Path.Combine(root, BackupPath);

        if (!File.Exists(sourceFile)) throw new System.Exception("Source scene missing: " + SourcePath);
        if (!File.Exists(backupFile)) File.Copy(sourceFile, backupFile);

        var source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Single);
        if (!source.IsValid()) throw new System.Exception("Source scene invalid: " + SourcePath);

        if (File.Exists(mainFile)) AssetDatabase.DeleteAsset(MainPath);
        AssetDatabase.Refresh();

        if (!EditorSceneManager.SaveScene(source, MainPath))
            throw new System.Exception("Failed to duplicate source scene to: " + MainPath);

        var main = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
        if (!main.IsValid()) throw new System.Exception("Created MainScene invalid: " + MainPath);

        var v003 = GameObject.Find("147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v003");
        var v004 = GameObject.Find("147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004");
        var bed = GameObject.Find("147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004/Bed_Collider");
        if (v003) throw new System.Exception("V003 still present in MainScene");
        if (!v004) throw new System.Exception("V004 missing from MainScene");
        if (!bed) throw new System.Exception("Bed_Collider missing from MainScene");

        AddBuildSettings(MainPath);
        EditorSceneManager.SaveScene(main);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("M7.4 CLEAN MAIN CREATED | V004=1 | V003=0 | Bed_Collider=1 | BuildIndex=0 | " + MainPath);
    }

    static void AddBuildSettings(string path)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == path);
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
