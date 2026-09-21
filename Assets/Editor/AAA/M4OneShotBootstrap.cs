using System;
using System.IO;
using UnityEditor;
namespace VR147.AAA.Editor
{
 [InitializeOnLoad]
 static class M4OneShotBootstrap
 {
  const string Marker="M4_RUN_ON_LOAD.marker";
  static M4OneShotBootstrap()
  {
   EditorApplication.delayCall+=RunIfArmed;
  }
  static void RunIfArmed()
  {
   var path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"147VR",Marker);
   if(!File.Exists(path))return;
   try{File.Delete(path);EditorApplication.delayCall+=M4BallCollisionRuntimeRunner.RunAll;}
   catch(Exception ex){UnityEngine.Debug.LogException(ex);}
  }
 }
}
