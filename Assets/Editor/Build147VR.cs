using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class Build147VR
{
    public static void QuestDevelopment()
    {
        const string outputPath = "Builds/Quest/147VR-Quest-Dev.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var scenes = new[]
        {
            "Assets/Scenes/PoolTable_8Ball.unity",
            "Assets/Scenes/PoolTable_9Ball.unity",
            "Assets/Scenes/SampleScene.unity"
        };

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.ConnectWithProfiler
        };

        var report = BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"147VR_BUILD_RESULT={report.summary.result}");
        UnityEngine.Debug.Log($"147VR_BUILD_PATH={outputPath}");
        UnityEngine.Debug.Log($"147VR_BUILD_SIZE={report.summary.totalSize} bytes");

        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("147 VR Quest development build failed.");
    }
}
