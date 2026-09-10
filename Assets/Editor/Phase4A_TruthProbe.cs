using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class Phase4A_TruthProbe {
 const string ScenePath="Assets/Scenes/147VR_MainScene.unity";
 const string LogPath="C:/Users/mongo/AppData/Local/Temp/147VR_PHASE4A_TRUTH.log";
 public static void Run(){var lines=new List<string>(); Action<string> L=x=>{lines.Add(x);Debug.Log(x);}; try{var sc=EditorSceneManager.OpenScene(ScenePath); var t=GameObject.Find("Prefab_WPBSA_12Foot_Snooker"); L($"ROOT {t.transform.position} local={t.transform.localScale} world={t.transform.lossyScale} rot={t.transform.eulerAngles}"); foreach(var n in new[]{"Yellow","Green","Brown","Blue","Pink","Black"}){var b=Find(t.transform,n); if(b==null){L(n+":NOTFOUND");continue;} L($"BALL {n} world={b.position} local={b.localPosition} scale={b.lossyScale} rot={b.eulerAngles}"); for(var q=b.parent;q!=null&&q!=t.transform;q=q.parent)L($"  P {q.name} local={q.localPosition} scale={q.localScale} world={q.lossyScale} rot={q.localEulerAngles}");} var bed=Find(t.transform,"Bed_Collider"); var s=Find(t.transform,"TABLE SURFACE"); L($"BED local={bed.localPosition} scale={bed.localScale} world={bed.lossyScale}"); L($"SURFACE local={s.localPosition} scale={s.localScale} world={s.lossyScale} rot={s.localEulerAngles}"); File.WriteAllLines(LogPath,lines); EditorApplication.Exit(0);}catch(Exception e){L("ERROR "+e);File.WriteAllLines(LogPath,lines);EditorApplication.Exit(3);}}
 static Transform Find(Transform r,string n){foreach(var q in r.GetComponentsInChildren<Transform>(true))if(q.name==n)return q;return null;}
}
