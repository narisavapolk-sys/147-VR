using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Android;
using UnityEngine;

namespace VR147.AAA.Editor
{
    public static class Build147VRQuestCandidate
    {
        private const string MainScene = "Assets/Scenes/147VR_MainScene.unity";
        private const string OutputPath = "Builds/Quest/147VR-MainScene-Dev.apk";
        private const string PlaceholderApplicationId = "com.UnityTechnologies.com.unity.template.urpblank";

        [MenuItem("Tools/147/APK/Quest Candidate Preflight")]
        public static void Preflight()
        {
            int fail = 0;
            LogCheck("MainScene exists", File.Exists(Path.GetFullPath(MainScene)), ref fail);

            var activeScenes = EditorBuildSettings.scenes;
            Debug.Log("[147VR APK] Global build scene count=" + activeScenes.Length);
            foreach (var scene in activeScenes)
                Debug.Log($"[147VR APK] GLOBAL SCENE enabled={scene.enabled} path={scene.path}");

            string appId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            bool placeholder = string.IsNullOrWhiteSpace(appId) ||
                               string.Equals(appId, PlaceholderApplicationId, StringComparison.OrdinalIgnoreCase);
            LogCheck("Android application identifier is non-placeholder", !placeholder, ref fail);
            Debug.Log("[147VR APK] Android applicationIdentifier=" + appId);

            LogCheck("Android scripting backend is IL2CPP",
                PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP, ref fail);

            LogCheck("Android target architecture is ARM64",
                (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0, ref fail);

            string editorRoot = Path.GetFullPath(Path.Combine(EditorApplication.applicationPath, ".."));
            string androidRoot = Path.Combine(editorRoot, "Data", "PlaybackEngines", "AndroidPlayer");
            LogCheck("AndroidPlayer module exists", Directory.Exists(androidRoot), ref fail);
            LogCheck("Android SDK exists", Directory.Exists(Path.Combine(androidRoot, "SDK")), ref fail);
            LogCheck("Android NDK exists", Directory.Exists(Path.Combine(androidRoot, "NDK")), ref fail);
            LogCheck("Android OpenJDK exists", Directory.Exists(Path.Combine(androidRoot, "OpenJDK")), ref fail);

            var xrSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.XR.Management.XRGeneralSettings>(
                "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            LogCheck("XR general settings asset exists", xrSettings != null, ref fail);

            Debug.Log(fail == 0
                ? "[147VR APK PREFLIGHT] PASS"
                : $"[147VR APK PREFLIGHT] BLOCKED | failures={fail}");

            if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 2);
        }

        [MenuItem("Tools/147/APK/Build Quest Candidate (Dev)")]
        public static void BuildDevelopment()
        {
            if (!File.Exists(Path.GetFullPath(MainScene)))
                throw new FileNotFoundException("MainScene missing", MainScene);

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            var originalBuildScenes = EditorBuildSettings.scenes;
            try
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(MainScene, true)
                };
                Debug.Log("[147VR APK] Candidate build scene list forced to MainScene only");

                var report = BuildPipeline.BuildPlayer(options);
                Debug.Log($"[147VR APK] RESULT={report.summary.result}");
                Debug.Log($"[147VR APK] PATH={OutputPath}");
                Debug.Log($"[147VR APK] SIZE={report.summary.totalSize} bytes");

                if (report.summary.result != BuildResult.Succeeded)
                    throw new Exception("147 VR Quest candidate APK build failed.");
            }
            finally
            {
                EditorBuildSettings.scenes = originalBuildScenes;
                Debug.Log("[147VR APK] Global build scene list restored");
            }
        }

        private static void LogCheck(string label, bool ok, ref int fail)
        {
            if (ok) Debug.Log("[147VR APK] PASS: " + label);
            else
            {
                fail++;
                Debug.LogError("[147VR APK] FAIL: " + label);
            }
        }
    }
}