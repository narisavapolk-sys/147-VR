using System.IO;using UnityEditor;
namespace VR147.AAA.Editor{public static class M24PatchAxisAutomation{public static void ApplyAndExit(){const string p="Assets/Editor/AAA/M24EnglishAutomationV2.cs";var s=File.ReadAllText(p);s=s.Replace("d.spinAxisDotVertical[i]<.9f","Mathf.Abs(d.spinAxisDotVertical[i])<.9f");File.WriteAllText(p,s);AssetDatabase.Refresh();EditorApplication.Exit(0);}}}
