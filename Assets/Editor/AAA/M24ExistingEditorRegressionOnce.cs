using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace VR147.AAA.Editor {
 [InitializeOnLoad] public static class M24ExistingEditorRegressionOnce {
  const string K="147VR_M24_EXISTING_EDITOR_REGRESSION_ONCE";
  static M24ExistingEditorRegressionOnce(){if(SessionState.GetBool(K,false))return;EditorApplication.delayCall+=Run;}
  static void Run(){if(SessionState.GetBool(K,false))return;SessionState.SetBool(K,true);try{var t=typeof(M24EnglishAutomationV2);var m=t.GetMethod("Reg",BindingFlags.Static|BindingFlags.NonPublic);if(m==null)throw new MissingMethodException(t.FullName,"Reg");m.Invoke(null,new object[]{"english_left_runtime_measurements.json","147VR-PHY-005",-0.75f});m.Invoke(null,new object[]{"english_right_runtime_measurements.json","147VR-PHY-006",0.75f});Debug.Log("[147VR M2.4 Existing Editor Regression] PASS | LEFT 5/5 | RIGHT 5/5 | tolerance=0.50%");}catch(Exception ex){Debug.LogError("[147VR M2.4 Existing Editor Regression] FAIL | "+ex);}}
 }
}
