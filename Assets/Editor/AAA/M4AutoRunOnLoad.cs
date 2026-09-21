using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VR147.AAA.Editor
{
    [InitializeOnLoad]
    public static class M4AutoRunOnLoad
    {
        private static readonly string Marker = Path.Combine(Path.GetTempPath(), "147VR_M4_AUTORUN.marker");
        private static readonly string Log = Path.Combine(Path.GetTempPath(), "147VR_M4_GUI_AUTO.log");
        static M4AutoRunOnLoad()
        {
            if (!File.Exists(Marker)) return;
            EditorApplication.delayCall += Run;
        }
        private static void Run()
        {
            if (!File.Exists(Marker)) return;
            File.Delete(Marker);
            try
            {
                Debug.Log("[147VR M4 AUTO] Starting M4BallCollisionRuntimeRunner.RunAll");
                M4BallCollisionRuntimeRunner.RunAll();
                File.WriteAllText(Log, "M4_RUNNER_RETURNED_OK\n" + DateTime.UtcNow.ToString("O"));
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Log, "M4_RUNNER_EXCEPTION\n" + ex);
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }
    }
}