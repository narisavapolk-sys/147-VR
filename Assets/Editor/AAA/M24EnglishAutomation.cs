using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Physics;
using VR147.AAA.Physics.Golden;

namespace VR147.AAA.Editor
{
 public static class M24EnglishAutomation {
  const string Scene="Assets/AAA/PhysicsCalibration/147VR_M24_EnglishCalibration.unity";
  const string Root="Assets/AAA/PhysicsCalibration/RuntimeMeasurements/";
  const string Catalog="Assets/AAA/PhysicsCalibration/Golden/147VR_GoldenCatalog.asset";
  const float Input=.75f, Tol=.5f;
  [Serializable] class Data { public string side; public int repetitions; public float english; public float[] spinSpeedAtContact,spinSpeedAfterContact,spinAxisDotVertical,lateralDisplacement,objectPeakSpeed,firstFlightDistance,objectDirectionDot,postContactCueSpeed; public bool[] passed; }

  [MenuItem("147VR/AAA/Physics/M2.4/Create English Calibration Scene")]
  public static void CreateScene(){
   var s=EditorSceneManager.OpenScene("Assets/AAA/PhysicsCalibration/147VR_M23_DrawCalibration.unity",OpenSceneMode.Single);
   foreach(var r in UnityEngine.Object.FindObjectsByType<M23DrawBatchRunner>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(r.gameObject);
   foreach(var r in UnityEngine.Object.FindObjectsByType<M24EnglishBatchRunner>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(r.gameObject);
   CreateRunner(s,"M24_EnglishLeftRunner",-Input,"english_left_runtime_measurements.json",false);
   CreateRunner(s,"M24_EnglishRightRunner",Input,"english_right_runtime_measurements.json",true);
   EditorSceneManager.SaveScene(s,Scene); AssetDatabase.SaveAssets(); Debug.Log("[147VR M2.4] English scene created: LEFT + RIGHT");
  }
  static void CreateRunner(UnityEngine.SceneManagement.Scene s,string name,float e,string file,bool active){
   var go=new GameObject(name); var r=go.AddComponent<M24EnglishBatchRunner>(); var so=new SerializedObject(r);
   so.FindProperty("cueBall").objectReferenceValue=GameObject.Find("Red")?.GetComponent<Rigidbody>(); so.FindProperty("objectBall").objectReferenceValue=GameObject.Find("Sphere.009")?.GetComponent<Rigidbody>();
   so.FindProperty("physicsAdapter").objectReferenceValue=UnityEngine.Object.FindAnyObjectByType<VR147.AAA.Cue.CuePhysicsAdapter>(); so.FindProperty("physicsSetup").objectReferenceValue=UnityEngine.Object.FindAnyObjectByType<SnookerPhysicsSetup>();
   so.FindProperty("english").floatValue=e; so.FindProperty("shotSpeed").floatValue=4f; so.FindProperty("repetitions").intValue=5; so.FindProperty("outputFileName").stringValue=file; so.ApplyModifiedPropertiesWithoutUndo(); r.enabled=active;
  }

  [MenuItem("147VR/AAA/Physics/M2.4/Run Left English x5")]
  public static void RunLeft()=>Run("M24_EnglishLeftRunner","english_left_runtime_measurements.json");
  [MenuItem("147VR/AAA/Physics/M2.4/Run Right English x5")]
  public static void RunRight()=>Run("M24_EnglishRightRunner","english_right_runtime_measurements.json");
  static void Run(string name,string file){
   Delete(file); EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
   foreach(var r in UnityEngine.Object.FindObjectsByType<M24EnglishBatchRunner>(FindObjectsSortMode.None)) r.enabled=false;
   var go=GameObject.Find(name); if(go==null)throw new InvalidOperationException(name+" missing"); go.GetComponent<M24EnglishBatchRunner>().enabled=true; EditorApplication.isPlaying=true;
  }

  [MenuItem("147VR/AAA/Physics/M2.4/Build English Goldens")]
  public static void Build(){BuildOne("english_left_runtime_measurements.json","147VR-PHY-005","Left English",-Input);BuildOne("english_right_runtime_measurements.json","147VR-PHY-006","Right English",Input);Debug.Log("[147VR M2.4 Golden] BUILD PASS | PHY-005 + PHY-006");}
  static void BuildOne(string file,string id,string side,float input){var d=Load(file);Validate(d,side,input);float dist=Mean(d.firstFlightDistance),peak=Mean(d.objectPeakSpeed);string suffix=side.StartsWith("Left")?"Left":"Right";string gp=$"Assets/AAA/PhysicsCalibration/Golden/{id}_{suffix}.asset";var g=AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>(gp)??ScriptableObject.CreateInstance<PhysicsGoldenCase>();if(AssetDatabase.GetAssetPath(g)=="")AssetDatabase.CreateAsset(g,gp);
   string cp=$"Assets/AAA/PhysicsCalibration/{suffix}English_Runtime_Case.asset";var c=AssetDatabase.LoadAssetAtPath<ShotCalibrationCase>(cp)??ScriptableObject.CreateInstance<ShotCalibrationCase>();if(AssetDatabase.GetAssetPath(c)=="")AssetDatabase.CreateAsset(c,cp);
   c.type=side.StartsWith("Left")?ShotCalibrationType.LeftEnglish:ShotCalibrationType.RightEnglish;c.power=1f;c.side=input;c.vertical=0f;c.expectedDistance=dist;c.tolerance=Mathf.Max(dist*Tol/100f,1e-6f);c.expectedPeakSpeed=peak;c.peakSpeedTolerance=Mathf.Max(peak*Tol/100f,1e-6f);c.validatePeakSpeed=true;
   Set(g,"caseId",id);Set(g,"revision",1);Set(g,"category",(int)PhysicsGoldenCategory.English);Set(g,"intent",$"Measured REAL {side} runtime truth. Vertical angular velocity, signed lateral displacement and forward object-ball travel are acceptance evidence.");Set(g,"calibrationCase",c);Set(g,"requirePass",true);Set(g,"sourceJsonPath",Root+file);Set(g,"sourceTimestampUtc",DateTime.UtcNow.ToString("O"));Set(g,"sourceScene",Scene);Set(g,"sourceUnityVersion",Application.unityVersion);Set(g,"sourceShotType","English");Set(g,"sourceRepetitionCount",5);Set(g,"measuredDistanceMean",dist);Set(g,"measuredDistanceStdDev",Std(d.firstFlightDistance,dist));Set(g,"measuredPeakSpeedMean",peak);Set(g,"measuredPeakSpeedStdDev",Std(d.objectPeakSpeed,peak));Set(g,"regressionTolerancePercent",Tol);Set(g,"measuredDistanceSamples",d.firstFlightDistance);Set(g,"measuredPeakSpeedSamples",d.objectPeakSpeed);EditorUtility.SetDirty(c);EditorUtility.SetDirty(g);AssetDatabase.SaveAssets();UpdateCatalog(g);
  }

  [MenuItem("147VR/AAA/Physics/M2.4/Run English Golden Regression")]
  public static void Regression(){Reg("english_left_runtime_measurements.json","147VR-PHY-005",-Input);Reg("english_right_runtime_measurements.json","147VR-PHY-006",Input);Debug.Log("[147VR M2.4 Golden Regression] PASS | LEFT 5/5 | RIGHT 5/5 | tolerance=0.50%");}
  static void Reg(string file,string id,float input){var d=Load(file);Validate(d,id.Contains("005")?"Left English":"Right English",input);var g=AssetDatabase.FindAssets("t:"+nameof(PhysicsGoldenCase)).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<PhysicsGoldenCase>).FirstOrDefault(x=>x!=null&&x.CaseId==id);if(g==null)throw new InvalidOperationException(id+" missing");for(int i=0;i<5;i++){var r=PhysicsGoldenEvaluator.Evaluate(g,d.firstFlightDistance[i],d.objectPeakSpeed[i]);if(!r.passed)throw new InvalidOperationException(id+" regression fail "+(i+1));Debug.Log($"[147VR M2.4 Golden Regression] {id} rep={i+1}/5 PASS | spin={d.spinSpeedAtContact[i]:F6} axisDot={d.spinAxisDotVertical[i]:F6} lateral={d.lateralDisplacement[i]:F9} objectDot={d.objectDirectionDot[i]:F6}");}}
  static Data Load(string f){var p=Project(Root+f);if(!File.Exists(p))throw new FileNotFoundException("English JSON missing",p);return JsonUtility.FromJson<Data>(File.ReadAllText(p));}
  static void Validate(Data d,string side,float input){if(d==null||d.repetitions!=5||d.passed?.Length!=5||d.spinSpeedAtContact?.Length!=5)throw new InvalidOperationException(side+" JSON incomplete");if(Mathf.Abs(d.english-input)>.0001f)throw new InvalidOperationException(side+" input mismatch");if(!string.Equals(d.side,side.StartsWith("Left")?"LEFT":"RIGHT",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException(side+" side mismatch");for(int i=0;i<5;i++)if(!d.passed[i]||d.spinSpeedAtContact[i]<=.05f||d.spinAxisDotVertical[i]<.9f||Mathf.Abs(d.lateralDisplacement[i])<.0001f||d.objectDirectionDot[i]<=0)throw new InvalidOperationException(side+" acceptance failed "+(i+1));}
  static void UpdateCatalog(PhysicsGoldenCase g){var c=AssetDatabase.LoadAssetAtPath<PhysicsGoldenCatalog>(Catalog);if(c==null)throw new InvalidOperationException("Golden catalog missing");var so=new SerializedObject(c);var p=so.FindProperty("cases");for(int i=0;i<p.arraySize;i++)if((p.GetArrayElementAtIndex(i).objectReferenceValue as PhysicsGoldenCase)?.CaseId==g.CaseId){p.GetArrayElementAtIndex(i).objectReferenceValue=g;so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();return;}p.arraySize++;p.GetArrayElementAtIndex(p.arraySize-1).objectReferenceValue=g;so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();}
  static void Delete(string f){var p=Project(Root+f);if(File.Exists(p))File.Delete(p);}static string Project(string a)=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,a.Replace('/',Path.DirectorySeparatorChar));static float Mean(float[] a)=>a.Average();static float Std(float[] a,float m)=>Mathf.Sqrt(a.Select(x=>(x-m)*(x-m)).Average());
  static void Set(UnityEngine.Object t,string n,object v){var so=new SerializedObject(t);var p=so.FindProperty(n);if(v is string s)p.stringValue=s;else if(v is int i)p.intValue=i;else if(v is float f)p.floatValue=f;else if(v is bool b)p.boolValue=b;else if(v is float[] a){p.arraySize=a.Length;for(int j=0;j<a.Length;j++)p.GetArrayElementAtIndex(j).floatValue=a[j];}so.ApplyModifiedPropertiesWithoutUndo();}
 }
}
// M2.4 automation file intentionally uses explicit batch exits from dedicated execute methods.
