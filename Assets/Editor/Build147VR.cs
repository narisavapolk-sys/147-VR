using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class Build147VR
{
    private const string ProductionScene = "Assets/Scenes/147VR_MainScene_WipRoom.unity";

    public static void QuestDevelopment()
    {
        const string outputPath = "Builds/Quest/147VR-Quest-Dev.apk";
        if (!File.Exists(ProductionScene))
            throw new FileNotFoundException("WipRoom production scene is missing.", ProductionScene);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ProductionScene },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.ConnectWithProfiler
        };

        var report = BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"147VR_BUILD_RESULT={report.summary.result}");
        UnityEngine.Debug.Log($"147VR_BUILD_SCENE={ProductionScene}");
        UnityEngine.Debug.Log($"147VR_BUILD_PATH={outputPath}");
        UnityEngine.Debug.Log($"147VR_BUILD_SIZE={report.summary.totalSize} bytes");

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("147 VR Quest development build failed.");
    }
}