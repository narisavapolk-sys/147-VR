using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VR147.AAA.Physics.Golden;

namespace VR147.AAA.Editor
{
 public static class M4_2PocketGoldenCertification
 {
  const string Dir="Assets/AAA/PhysicsCalibration/RuntimeMeasurements", Gold="Assets/AAA/PhysicsCalibration/Golden";
  static readonly (string file,string id,string label,string outcome)[] Cases={
   ("m4_2_Clean_Capture_runtime_measurements.json","147VR-PHY-020","Clean Capture","Capture"),
   ("m4_2_LowSpeed_Capture_runtime_measurements.json","147VR-PHY-021","Low Speed Capture","Capture"),
   ("m4_2_Clean_Reject_runtime_measurements.json","147VR-PHY-022","Clean Reject","Reject"),
   ("m4_2_Left_Jaw_runtime_measurements.json","147VR-PHY-023","Left Jaw","Jaw"),
   ("m4_2_Right_Jaw_runtime_measurements.json","147VR-PHY-024","Right Jaw","Jaw"),
   ("m4_2_Rattle_runtime_measurements.json","147VR-PHY-025","Rattle","Rattle")};
  [Serializable] class FileData{public string timestampUtc,unityVersion,authority;public int repetitions;public Sample[] samples;}
  [Serializable] class Sample{public string caseName,outcome;public float entrySpeed,entryAngleDeg,minMouthDistance,finalX,finalZ;public int wallContacts;public bool captured,rejected,jawContact,rattle;}
  [MenuItem("147VR/AAA/Physics/M4.2/Build And Regress Pocket Goldens")]
  public static void BuildAndRegress(){Ensure();var list=new List<M4PocketGolden>();foreach(var c in Cases){var d=Load(c.file);Validate(d,c.outcome,c.label);var s=d.samples;var g=ScriptableObject.CreateInstance<M4PocketGolden>();g.caseId=c.id;g.caseName=c.label;g.outcome=c.outcome;g.sourceJsonPath=Dir+"/"+c.file;g.sourceTimestampUtc=d.timestampUtc;g.sourceUnityVersion=d.unityVersion;g.repetitions=5;g.entrySpeedMean=Mean(s.Select(x=>x.entrySpeed));g.entryAngleMean=Mean(s.Select(x=>x.entryAngleDeg));g.minMouthDistanceMean=Mean(s.Select(x=>x.minMouthDistance));g.finalXMean=Mean(s.Select(x=>x.finalX));g.finalZMean=Mean(s.Select(x=>x.finalZ));g.wallContactsMean=(int)Mathf.Round(Mean(s.Select(x=>(float)x.wallContacts)));g.capturedExpected=s.All(x=>x.captured);g.rejectedExpected=s.All(x=>x.rejected);g.jawExpected=s.All(x=>x.jawContact);g.rattleExpected=s.All(x=>x.rattle);string path=$"{Gold}/{c.id}_{Safe(c.label)}.asset";if(AssetDatabase.LoadAssetAtPath<M4PocketGolden>(path)!=null)AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(g,path);list.Add(g);Regression(g,s);}
   Symmetry(Load("m4_2_Left_Jaw_runtime_measurements.json"),Load("m4_2_Right_Jaw_runtime_measurements.json"));var cat=AssetDatabase.LoadAssetAtPath<M4PocketGoldenCatalog>($"{Gold}/147VR_M4_2_GoldenCatalog.asset")??CreateCatalog();cat.cases=list;EditorUtility.SetDirty(cat);AssetDatabase.SaveAssets();if(!cat.Validate())throw new InvalidOperationException("M4.2 golden catalog validation failed");Debug.Log("[147VR M4.2 Golden] CERTIFIED | 6/6 cases | 30/30 REAL samples | symmetry PASS");}
  static FileData Load(string f){string p=Path.Combine(Directory.GetParent(Application.dataPath).FullName,Dir.Replace("/","\\"),f);if(!File.Exists(p))throw new FileNotFoundException("M4.2 runtime JSON missing",p);return JsonUtility.FromJson<FileData>(File.ReadAllText(p));}
  static void Validate(FileData d,string outcome,string label){if(d==null||d.repetitions!=5||d.samples==null||d.samples.Length!=5)throw new InvalidOperationException("Invalid M4.2 JSON: "+label);foreach(var x in d.samples){if(x.outcome!=outcome)throw new InvalidOperationException("Outcome mismatch: "+label);if(outcome=="Capture"&&(!x.captured||x.rejected||x.jawContact))throw new InvalidOperationException("Capture evidence invalid: "+label);if(outcome=="Reject"&&(!x.rejected||x.captured||x.jawContact))throw new InvalidOperationException("Reject evidence invalid: "+label);if(outcome=="Jaw"&&(!x.rejected||x.captured||!x.jawContact||x.wallContacts<1))throw new InvalidOperationException("Jaw evidence invalid: "+label);if(outcome=="Rattle"&&(!x.rattle||!x.jawContact||x.wallContacts<2))throw new InvalidOperationException("Rattle evidence invalid: "+label);}}
  static void Regression(M4PocketGolden g,Sample[] s){if(!g.IsValid())throw new InvalidOperationException(g.caseId+" invalid");for(int i=0;i<s.Length;i++){if(Mathf.Abs(s[i].entrySpeed-g.entrySpeedMean)>0.001f||Mathf.Abs(s[i].entryAngleDeg-g.entryAngleMean)>0.001f||Mathf.Abs(s[i].minMouthDistance-g.minMouthDistanceMean)>0.001f||Mathf.Abs(s[i].finalX-g.finalXMean)>0.001f||Mathf.Abs(s[i].finalZ-g.finalZMean)>0.001f||s[i].wallContacts!=g.wallContactsMean)throw new InvalidOperationException(g.caseId+" regression failed rep="+(i+1));}Debug.Log($"[147VR M4.2 Golden Regression] {g.caseId} PASS 5/5");}
  static void Symmetry(FileData left,FileData right){if(left.samples.Length!=5||right.samples.Length!=5)throw new InvalidOperationException("M4.2 symmetry sample count mismatch");for(int i=0;i<5;i++){var a=left.samples[i];var b=right.samples[i];if(Mathf.Abs(Mathf.Abs(a.entryAngleDeg)-Mathf.Abs(b.entryAngleDeg))>0.001f||Mathf.Abs(a.entrySpeed-b.entrySpeed)>0.001f||Mathf.Abs(a.minMouthDistance-b.minMouthDistance)>0.001f||Mathf.Abs(a.finalX-b.finalX)>0.001f||Mathf.Abs(Mathf.Abs(a.finalZ)-Mathf.Abs(b.finalZ))>0.001f||a.wallContacts!=b.wallContacts)throw new InvalidOperationException("M4.2 Left/Right symmetry failed rep="+(i+1));}Debug.Log("[147VR M4.2] SYMMETRY PASS | Left/Right Jaw mirror validated");}
  static float Mean(IEnumerable<float> v){return v.Average();}
  static string Safe(string s)=>s.Replace("/","_").Replace(" ","_");
  static void Ensure(){if(!AssetDatabase.IsValidFolder(Gold))AssetDatabase.CreateFolder("Assets/AAA/PhysicsCalibration","Golden");}
  static M4PocketGoldenCatalog CreateCatalog(){var c=ScriptableObject.CreateInstance<M4PocketGoldenCatalog>();AssetDatabase.CreateAsset(c,$"{Gold}/147VR_M4_2_GoldenCatalog.asset");return c;}
 }
}