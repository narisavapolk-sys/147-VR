using System.IO;
using UnityEditor;
namespace VR147.AAA.Editor{
 public static class M24PatchAxis{
  public static void ApplyAndExit(){
   const string p="Assets/Scripts/AAA/Physics/M24EnglishBatchRunnerV2.cs";
   string s=File.ReadAllText(p);s=s.Replace("axis>.9f&&objDot>0&&sideSign","Mathf.Abs(axis)>.9f&&objDot>0&&sideSign");File.WriteAllText(p,s);AssetDatabase.Refresh();AssetDatabase.SaveAssets();EditorApplication.Exit(0);
  }
 }
}
