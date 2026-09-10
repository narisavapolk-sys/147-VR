using UnityEditor;
using VR147.AAA.Editor;

namespace VR147.AAA.Editor
{
    public static class M24BatchEntryPoints
    {
        public static void CreateSceneAndExit()
        {
            M24EnglishAutomation.CreateScene();
            EditorApplication.Exit(0);
        }

        public static void BuildGoldensAndExit()
        {
            M24EnglishAutomation.Build();
            EditorApplication.Exit(0);
        }

        public static void RegressionAndExit()
        {
            M24EnglishAutomation.Regression();
            EditorApplication.Exit(0);
        }
    }
}
