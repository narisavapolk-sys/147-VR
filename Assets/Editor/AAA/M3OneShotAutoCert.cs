using UnityEditor;

namespace VR147.AAA.Editor
{
    [InitializeOnLoad]
    internal static class M3OneShotAutoCert
    {
        private static bool queued;
        static M3OneShotAutoCert()
        {
            if (System.Environment.GetEnvironmentVariable("M3_AUTO_CERTIFY") != "1") return;
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += Run;
        }

        private static void Run()
        {
            EditorApplication.delayCall -= Run;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                return;
            }

            UnityEngine.Debug.Log("[147VR M3 AUTO] Starting measured Golden promotion from REAL runtime JSON.");
            M3CushionGoldenCertificationAutomation.PromoteAll();
        }
    }
}