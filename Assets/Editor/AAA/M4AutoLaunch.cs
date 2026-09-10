using UnityEditor;
using UnityEngine;

namespace VR147.AAA.Editor
{
    [InitializeOnLoad]
    internal static class M4AutoLaunch
    {
        private const string Env = "147VR_M4_AUTORUN";
        private static bool started;

        static M4AutoLaunch()
        {
            if (System.Environment.GetEnvironmentVariable(Env) != "1") return;
            EditorApplication.delayCall += Launch;
        }

        private static void Launch()
        {
            if (started) return;
            started = true;
            Debug.Log("[147VR M4 AUTO] Launching BallCollisionRuntimeRunner after editor/UPM initialization");
            M4BallCollisionRuntimeRunner.RunAll();
        }
    }
}
