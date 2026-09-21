using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VR147.AAA.Cue;

namespace VR147.AAA.Physics
{
    public sealed class M24EnglishBatchRunner : MonoBehaviour
    {
        [SerializeField] Rigidbody cueBall;
        [SerializeField] Rigidbody objectBall;
        [SerializeField] CuePhysicsAdapter physicsAdapter;
        [SerializeField] SnookerPhysicsSetup physicsSetup;
        [SerializeField] float english = 0.75f;
        [SerializeField] float shotSpeed = 4f;
        [SerializeField] int repetitions = 5;
        [SerializeField] string outputFileName = "english_left_runtime_measurements.json";
        const float Dt = 0.02f;
        readonly List<Sample> samples = new();
        Vector3 cueStart, objectStart, direction;

        [Serializable] class Sample {
            public float cueSpeedAtContact;
            public Vector3 cueVelocityAtContact, cueVelocityAfterContact;
            public Vector3 objectVelocityAtContact;
            public Vector3 angularVelocityAtContact, angularVelocityAfterContact;
            public float spinSpeedAtContact, spinSpeedAfterContact;
            public float spinAxisDotVertical;
            public float lateralDisplacement, objectPeakSpeed, firstFlightDistance;
            public float objectDirectionDot, postContactCueSpeed;
            public bool passed;
        }
        [Serializable] class Output {
            public string timestampUtc, caseType="English", side, scene, unityVersion;
            public int repetitions; public float shotSpeed, english;
            public float[] cueSpeedAtContact, spinSpeedAtContact, spinSpeedAfterContact;
            public float[] spinAxisDotVertical, lateralDisplacement, objectPeakSpeed;
            public float[] firstFlightDistance, objectDirectionDot, postContactCueSpeed;
            public Vector3[] angularVelocityAtContact, angularVelocityAfterContact;
            public bool[] passed;
        }

        void Start() {
            physicsSetup ??= FindFirstObjectByType<SnookerPhysicsSetup>();
            cueBall ??= GameObject.Find("Red")?.GetComponent<Rigidbody>();
            objectBall ??= GameObject.Find("Sphere.009")?.GetComponent<Rigidbody>();
            physicsAdapter ??= FindFirstObjectByType<CuePhysicsAdapter>();
            if (physicsSetup == null || cueBall == null || objectBall == null || physicsAdapter == null)
                throw new InvalidOperationException("M2.4 missing physics setup, balls, or CuePhysicsAdapter.");
            physicsSetup.EnsurePhysics();
            var cc=cueBall.GetComponent<SphereCollider>(); var oc=objectBall.GetComponent<SphereCollider>();
            if(cc==null || oc==null) throw new InvalidOperationException("M2.4 balls require SphereCollider.");
            float y=physicsSetup.SurfaceTopY; Vector3 c=physicsSetup.TableBounds.center;
            cueStart=new Vector3(c.x+0.30f,y+cc.radius,c.z-0.325f);
            objectStart=new Vector3(c.x+0.30f,y+oc.radius,c.z+0.325f);
            direction=(objectStart-cueStart); direction.y=0; direction.Normalize();
            Run(cc,oc);
        }

        void Run(SphereCollider cueCollider, SphereCollider objectCollider) {
            var old=UnityEngine.Physics.simulationMode;
            UnityEngine.Physics.simulationMode=SimulationMode.Script;
            try {
                for(int rep=0;rep<repetitions;rep++) RunOne(rep,cueCollider,objectCollider);
                Persist();
                Debug.Log($"[147VR M2.4] REAL {Side()} ENGLISH COMPLETE | {repetitions}/{repetitions} | input={english:F2}");
            } finally { UnityEngine.Physics.simulationMode=old; }
            enabled=false; UnityEditor.EditorApplication.Exit(0);
        }

        void RunOne(int rep,SphereCollider cc,SphereCollider oc) {
            cueBall.position=cueStart; objectBall.position=objectStart;
            cueBall.linearVelocity=Vector3.zero; cueBall.angularVelocity=Vector3.zero;
            objectBall.linearVelocity=Vector3.zero; objectBall.angularVelocity=Vector3.zero;
            UnityEngine.Physics.SyncTransforms();
            var probe=cueBall.GetComponent<M24ContactProbe>() ?? cueBall.gameObject.AddComponent<M24ContactProbe>();
            probe.Target=objectBall; probe.ResetCapture(); physicsAdapter.Configure(cueBall);
            if(!physicsAdapter.Apply(new CueShotData(direction,cueBall.position,1f,shotSpeed),new Vector2(english,0f)))
                throw new InvalidOperationException($"M2.4 adapter rejected {Side()} shot {rep+1}.");
            float peak=0, previousDistance=Vector3.Distance(cueBall.position,objectBall.position); int contactStep=-1;
            for(int step=0;step<600;step++) {
                UnityEngine.Physics.Simulate(Dt); peak=Mathf.Max(peak,objectBall.linearVelocity.magnitude);
                float d=Vector3.Distance(cueBall.position,objectBall.position);
                if(probe.Contacted && contactStep<0) contactStep=step;
                if(probe.Contacted && step>=contactStep+15) break;
                previousDistance=d;
            }
            if(!probe.Contacted) throw new InvalidOperationException($"M2.4 {Side()} shot {rep+1} no collision.");
            Vector3 av=probe.CueAngularVelocityAtContact, avAfter=cueBall.angularVelocity;
            float spin=av.magnitude, spinAfter=avAfter.magnitude;
            float axisDot=spin>0.000001f?Vector3.Dot(av.normalized,Vector3.up):0f;
            Vector3 lateral=Vector3.Cross(Vector3.up,direction).normalized;
            float lateralDisp=Vector3.Dot(objectBall.position-objectStart,lateral);
            Vector3 objV=probe.ObjectVelocityAtContact;
            float objDir=objV.sqrMagnitude>1e-8f?Vector3.Dot(objV.normalized,direction):0f;
            float firstFlight=Vector3.Distance(objectStart,probe.ObjectPositionAtContact);
            float postCue=cueBall.linearVelocity.magnitude;
            bool pass=spin>0.05f && spinAfter>0.05f && axisDot>0.90f && Mathf.Abs(lateralDisp)>0.0001f && objV.sqrMagnitude>1e-8f && objDir>0f;
            samples.Add(new Sample { cueSpeedAtContact=probe.CueVelocityAtContact.magnitude,cueVelocityAtContact=probe.CueVelocityAtContact,
                cueVelocityAfterContact=cueBall.linearVelocity,objectVelocityAtContact=objV,angularVelocityAtContact=av,
                angularVelocityAfterContact=avAfter,spinSpeedAtContact=spin,spinSpeedAfterContact=spinAfter,
                spinAxisDotVertical=axisDot,lateralDisplacement=lateralDisp,objectPeakSpeed=peak,firstFlightDistance=firstFlight,
                objectDirectionDot=objDir,postContactCueSpeed=postCue,passed=pass });
            Debug.Log($"[147VR M2.4] {Side()} rep={rep+1}/{repetitions} spin={spin:F6} spinAfter={spinAfter:F6} axisDot={axisDot:F6} lateral={lateralDisp:F9} objectDot={objDir:F6} peak={peak:F6} {(pass?"PASS":"FAIL")}");
            if(!pass) throw new InvalidOperationException($"M2.4 {Side()} acceptance failed rep {rep+1}: spin={spin} axisDot={axisDot} lateral={lateralDisp}");
        }

        string Side()=>english<0?"LEFT":"RIGHT";
        void Persist(){
            string root=Directory.GetParent(Application.dataPath).FullName; string dir=Path.Combine(root,"Assets/AAA/PhysicsCalibration/RuntimeMeasurements"); Directory.CreateDirectory(dir);
            var o=new Output {timestampUtc=DateTime.UtcNow.ToString("O"),side=Side(),scene="Assets/AAA/PhysicsCalibration/147VR_M24_EnglishCalibration.unity",unityVersion=Application.unityVersion,repetitions=samples.Count,shotSpeed=shotSpeed,english=english,
                cueSpeedAtContact=A(s=>s.cueSpeedAtContact),spinSpeedAtContact=A(s=>s.spinSpeedAtContact),spinSpeedAfterContact=A(s=>s.spinSpeedAfterContact),spinAxisDotVertical=A(s=>s.spinAxisDotVertical),lateralDisplacement=A(s=>s.lateralDisplacement),objectPeakSpeed=A(s=>s.objectPeakSpeed),firstFlightDistance=A(s=>s.firstFlightDistance),objectDirectionDot=A(s=>s.objectDirectionDot),postContactCueSpeed=A(s=>s.postContactCueSpeed),angularVelocityAtContact=V(s=>s.angularVelocityAtContact),angularVelocityAfterContact=V(s=>s.angularVelocityAfterContact),passed=B(s=>s.passed)};
            File.WriteAllText(Path.Combine(dir,outputFileName),JsonUtility.ToJson(o,true)); AssetDatabaseRefresh();
        }
        float[] A(Func<Sample,float> f){var a=new float[samples.Count];for(int i=0;i<a.Length;i++)a[i]=f(samples[i]);return a;}
        Vector3[] V(Func<Sample,Vector3> f){var a=new Vector3[samples.Count];for(int i=0;i<a.Length;i++)a[i]=f(samples[i]);return a;}
        bool[] B(Func<Sample,bool> f){var a=new bool[samples.Count];for(int i=0;i<a.Length;i++)a[i]=f(samples[i]);return a;}
        static void AssetDatabaseRefresh(){UnityEditor.AssetDatabase.Refresh();}
    }

    public sealed class M24ContactProbe:MonoBehaviour {
        public Rigidbody Target; public bool Contacted{get;private set;}
        public Vector3 CueVelocityAtContact{get;private set;} public Vector3 ObjectVelocityAtContact{get;private set;}
        public Vector3 CueAngularVelocityAtContact{get;private set;} public Vector3 ObjectPositionAtContact{get;private set;}
        public void ResetCapture(){Contacted=false;CueVelocityAtContact=Vector3.zero;ObjectVelocityAtContact=Vector3.zero;CueAngularVelocityAtContact=Vector3.zero;ObjectPositionAtContact=Vector3.zero;}
        void OnCollisionEnter(Collision c){if(Contacted||Target==null||c.rigidbody!=Target)return;Contacted=true;var rb=GetComponent<Rigidbody>();CueVelocityAtContact=rb.linearVelocity;CueAngularVelocityAtContact=rb.angularVelocity;ObjectVelocityAtContact=Target.linearVelocity;ObjectPositionAtContact=Target.position;}
    }
}
