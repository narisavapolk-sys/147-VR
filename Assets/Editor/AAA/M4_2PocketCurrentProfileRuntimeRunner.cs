using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VR147.AAA.Physics;

namespace VR147.AAA.Editor
{
    public static class M4_2PocketCurrentProfileRuntimeRunner
    {
        [Serializable] private sealed class Sample
        {
            public string caseName, outcome;
            public float entrySpeed, entryAngleDeg, minMouthDistance, finalX, finalZ;
            public int wallContacts;
            public bool captured, rejected, jawContact, rattle;
        }
        [Serializable] private sealed class Output
        {
            public string timestampUtc, unityVersion, authority, surfaceProfilePath;
            public int repetitions;
            public Sample[] samples;
        }
        private readonly struct CaseSpec
        {
            public readonly string name, outcome; public readonly Vector3 start, velocity;
            public CaseSpec(string n, string o, Vector3 s, Vector3 v)
            { name=n; outcome=o; start=s; velocity=v; }
        }        private const float Radius=0.028575f, Dt=0.005f, MouthX=0.11f, CaptureR=0.034f;
        private const float FloorTop=0.01f, BallY=0.01f+Radius;
        private const int BallLayer=27, WallLayer=28, CaptureLayer=29, FloorLayer=30;
        private static readonly int BallMask=1<<BallLayer;
        private static readonly int WallMask=1<<WallLayer;
        private const string ProfilePath="Assets/AAA/PhysicsCalibration/TableSurfaceProfile_RuntimeBaseline.asset";
        private const string RuntimeDir="Assets/AAA/PhysicsCalibration/RuntimeMeasurements";
        private static readonly CaseSpec[] Cases =
        {
            new("Clean_Capture","Capture",new(-0.16f,BallY,0f),new(1.8f,0f,0f)),
            new("LowSpeed_Capture","Capture",new(-0.12f,BallY,0f),new(0.8f,0f,0f)),
            new("Clean_Reject","Reject",new(-0.16f,BallY,0.13f),new(1.8f,0f,0f)),
            new("Left_Jaw","Jaw",new(-0.16f,BallY,0.052f),new(1.8f,0f,-0.11f)),
            new("Right_Jaw","Jaw",new(-0.16f,BallY,-0.052f),new(1.8f,0f,0.11f)),
            new("Rattle","Rattle",new(-0.16f,BallY,0.038f),new(1.75f,0f,-0.075f))
        };        public static void RunAll()
        {
            var profile=AssetDatabase.LoadAssetAtPath<TableSurfaceProfile>(ProfilePath);
            if(profile==null || !profile.measuredTruthCertified)
                throw new InvalidOperationException("Certified TableSurfaceProfile missing/inactive: "+ProfilePath);
            string root=Directory.GetParent(Application.dataPath).FullName;
            string dir=Path.Combine(root,RuntimeDir.Replace("/","\\"));
            Directory.CreateDirectory(dir);
            bool oldAuto=UnityEngine.Physics.autoSimulation;
            float oldFixed=Time.fixedDeltaTime;
            UnityEngine.Physics.autoSimulation=false;
            Time.fixedDeltaTime=Dt;
            try
            {
                foreach(var spec in Cases) RunCase(spec,dir,profile);
                Debug.Log("[147VR M4.2 CURRENT] COMPLETE | 30 REAL samples | certified cloth profile");
            }
            finally { UnityEngine.Physics.autoSimulation=oldAuto; Time.fixedDeltaTime=oldFixed; }
            if(Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void RunCase(CaseSpec spec,string dir,TableSurfaceProfile profile)
        {
            var samples=new List<Sample>(5);            for(int rep=0;rep<5;rep++)
            {
                GameObject root=null, floor=null, capture=null, ball=null;
                try
                {
                    root=new GameObject("M4.2_CurrentProfile_Rig");
                    floor=CreateFloor(root.transform,profile);
                    CreateWall(root.transform,new Vector3(0.075f,BallY,0.075f),new Vector3(0.17f,0.06f,0.02f));
                    CreateWall(root.transform,new Vector3(0.075f,BallY,-0.075f),new Vector3(0.17f,0.06f,0.02f));
                    CreateWall(root.transform,new Vector3(0.155f,BallY,0f),new Vector3(0.02f,0.06f,0.17f));
                    capture=CreateCapture();
                    ball=CreateBall(spec.start,spec.velocity);
                    var rb=ball.GetComponent<Rigidbody>();
                    bool captured=false,jaw=false,rattle=false,rejected=false;
                    int wallContacts=0,rattleSteps=0;
                    float minDist=float.MaxValue;
                    for(int i=0;i<1600;i++)
                    {
                        Vector3 prePos=ball.transform.position,preVel=rb.linearVelocity;
                        float d=new Vector2(prePos.x-MouthX,prePos.z).magnitude;
                        minDist=Mathf.Min(minDist,d);
                        bool predictedWall=false;                        if(preVel.sqrMagnitude>1e-8f)
                        {
                            Vector3 castDirection=preVel.normalized;
                            float castDist=Mathf.Max(preVel.magnitude*Dt,0.001f);
                            predictedWall=UnityEngine.Physics.SphereCast(prePos,Radius,castDirection,out RaycastHit hit,
                                castDist+0.002f,WallMask,QueryTriggerInteraction.Ignore);
                        }
                        UnityEngine.Physics.Simulate(Dt);
                        Vector3 postVel=rb.linearVelocity;
                        if(predictedWall && (postVel-preVel).magnitude>0.02f)
                        { wallContacts++; jaw=true; }
                        captured=UnityEngine.Physics.OverlapSphere(capture.transform.position,CaptureR,
                            BallMask,QueryTriggerInteraction.Collide).Length>0;
                        float x=ball.transform.position.x,z=ball.transform.position.z;
                        if(wallContacts>=2 && x>0.04f && x<MouthX+0.06f && Mathf.Abs(z)<0.08f)
                            rattleSteps++;
                        if(rattleSteps>=6) { rattle=true; }
                        if(spec.outcome=="Rattle" && rattle && captured) break;
                        if(spec.outcome=="Capture" && captured) break;                        if(spec.outcome=="Jaw" && jaw && x>0.02f && !captured && postVel.magnitude>0.05f)
                        { rejected=true; break; }
                        if(spec.outcome=="Reject" && x>MouthX+0.04f && !captured)
                        { rejected=true; break; }
                        if(x>0.19f || (x>0.02f && postVel.magnitude<0.03f))
                        { rejected=true; break; }
                    }
                    if(spec.outcome=="Capture" && !captured) throw new InvalidOperationException($"{spec.name} rep={rep+1}/5 capture not observed");
                    if(spec.outcome=="Reject" && (!rejected || captured)) throw new InvalidOperationException($"{spec.name} rep={rep+1}/5 clean reject not observed");
                    if(spec.outcome=="Jaw" && (!jaw || !rejected || captured)) throw new InvalidOperationException($"{spec.name} rep={rep+1}/5 jaw reject not observed");
                    if(spec.outcome=="Rattle" && (!rattle || !captured || !jaw))
                        throw new InvalidOperationException($"{spec.name} rep={rep+1}/5 rattle capture/jaw state not observed");
                    samples.Add(new Sample
                    {
                        caseName=spec.name, outcome=spec.outcome,
                        entrySpeed=spec.velocity.magnitude,
                        entryAngleDeg=Mathf.Atan2(spec.velocity.z,spec.velocity.x)*Mathf.Rad2Deg,
                        minMouthDistance=minDist, finalX=ball.transform.position.x, finalZ=ball.transform.position.z,
                        wallContacts=wallContacts,captured=captured,rejected=rejected,jawContact=jaw,rattle=rattle
                    });
                    Debug.Log($"[147VR M4.2 CURRENT] REAL {spec.name} rep={rep+1}/5 | speed={spec.velocity.magnitude:F3} | contacts={wallContacts} | captured={captured} | rejected={rejected} | jaw={jaw} | rattle={rattle}");
                }
                finally
                {
                    if(ball!=null) UnityEngine.Object.DestroyImmediate(ball);
                    if(capture!=null) UnityEngine.Object.DestroyImmediate(capture);
                    if(root!=null) UnityEngine.Object.DestroyImmediate(root);
                }
            }
            string file=Path.Combine(dir,$"m4_2_{spec.name}_runtime_measurements.json");            var output=new Output
            {
                timestampUtc=DateTime.UtcNow.ToString("O"),
                unityVersion=Application.unityVersion,
                authority="Unity PhysX runtime + certified TableSurfaceProfile_RuntimeBaseline",
                surfaceProfilePath=ProfilePath,
                repetitions=samples.Count,
                samples=samples.ToArray()
            };
            File.WriteAllText(file,JsonUtility.ToJson(output,true));
            Debug.Log($"[147VR M4.2 CURRENT] REAL DATA SAVED | {file}");
        }

        private static GameObject CreateFloor(Transform parent,TableSurfaceProfile profile)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name="M4.2_TableFloor"; floor.transform.SetParent(parent);
            floor.transform.localPosition=new Vector3(0f,0f,0f);
            floor.transform.localScale=new Vector3(0.7f,0.02f,0.5f);
            floor.layer=FloorLayer;
            ConfigureMaterial(floor.GetComponent<Collider>(),false);
            var surface=floor.AddComponent<TableSurfaceController>(); surface.Configure(profile);
            return floor;
        }
        private static GameObject CreateCapture()
        {
            var capture=new GameObject("M4.2_Capture"); capture.layer=CaptureLayer;
            capture.transform.position=new Vector3(MouthX,BallY,0f);
            var trigger=capture.AddComponent<SphereCollider>(); trigger.isTrigger=true; trigger.radius=CaptureR;
            return capture;
        }        private static GameObject CreateBall(Vector3 pos,Vector3 velocity)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere); go.layer=BallLayer; go.name="M4.2_Ball";
            go.transform.position=pos; go.transform.localScale=Vector3.one*(Radius*2f);
            var col=go.GetComponent<Collider>(); ConfigureMaterial(col,false);
            var rb=go.AddComponent<Rigidbody>(); rb.mass=0.17f; rb.useGravity=false;
            rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic; rb.linearVelocity=velocity;
            return go;
        }

        private static void CreateWall(Transform parent,Vector3 localPos,Vector3 size)
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.layer=WallLayer;
            wall.transform.SetParent(parent); wall.transform.localPosition=localPos; wall.transform.localScale=size;
            ConfigureMaterial(wall.GetComponent<Collider>(),true);
            var rb=wall.AddComponent<Rigidbody>(); rb.isKinematic=true; rb.useGravity=false;
        }

        private static void ConfigureMaterial(Collider collider,bool wall)
        {
            var mat=new PhysicsMaterial("M4.2_RuntimeZeroFriction");
            mat.dynamicFriction=0f; mat.staticFriction=0f;
            mat.frictionCombine=PhysicsMaterialCombine.Minimum;
            mat.bounciness=wall ? 0f : 0f;
            mat.bounceCombine=PhysicsMaterialCombine.Minimum;
            collider.sharedMaterial=mat;
        }
    }
}