using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.AAA.Cue;

namespace VR147.AAA.Editor
{
    public static class M23DrawHeadlessAutomation
    {
        private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_M23_DrawCalibration.unity";
        private const string OutputPath = "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/draw_runtime_measurements.json";
        private const float DrawInput = -0.75f;
        private const float ShotSpeed = 4f;
        private const int Repetitions = 5;
        private const float Dt = 0.02f;

        [Serializable] private sealed class Output
        {
            public string timestampUtc;
            public string caseType = "Draw";
            public string scene;
            public string unityVersion;
            public int repetitions;
            public float shotSpeed;
            public float drawEnglish;
            public float[] cueSpeedAtContact;
            public Vector3[] cueVelocityAtContact;
            public Vector3[] cueVelocityAfterContact;
            public Vector3[] objectVelocityAtContact;
            public float[] objectPeakSpeed;
            public float[] firstFlightDistance;
            public float[] cuePostContactDistance;
            public float[] cuePostContactSpeed;
            public float[] drawDot;
            public bool[] passed;
        }

        [Serializable] private sealed class Sample
        {
            public float cueSpeedAtContact;
            public Vector3 cueVelocityAtContact;
            public Vector3 cueVelocityAfterContact;
            public Vector3 objectVelocityAtContact;
            public float objectPeakSpeed;
            public float firstFlightDistance;
            public float cuePostContactDistance;
            public float cuePostContactSpeed;
            public float drawDot;
            public bool passed;
        }

        [MenuItem("147VR/AAA/Physics/M2.3/Run Headless Real Draw x5")]
        public static void Run()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string sceneFile = Path.Combine(root, ScenePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(sceneFile)) throw new FileNotFoundException("M2.3 scene missing", sceneFile);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var setup = UnityEngine.Object.FindAnyObjectByType<SnookerPhysicsSetup>();
            var adapter = UnityEngine.Object.FindAnyObjectByType<CuePhysicsAdapter>();
            var cue = GameObject.Find("Red")?.GetComponent<Rigidbody>();
            var obj = GameObject.Find("Sphere.009")?.GetComponent<Rigidbody>();
            if (setup == null || adapter == null || cue == null || obj == null) throw new InvalidOperationException("M2.3 scene missing physics setup, adapter, cue ball, or object ball.");

            setup.EnsurePhysics();
            if (setup.TableBounds.size == Vector3.zero || setup.SurfaceTopY == 0f) throw new InvalidOperationException("M2.3 physics setup did not initialize.");
            var cueCollider = cue.GetComponent<SphereCollider>();
            var objCollider = obj.GetComponent<SphereCollider>();
            if (cueCollider == null || objCollider == null) throw new InvalidOperationException("M2.3 balls lack SphereCollider.");

            Vector3 direction = obj.position - cue.position;
            direction.y = 0f;
            direction.Normalize();
            Vector3 cueStart = new Vector3(setup.TableBounds.center.x + 0.30f, setup.SurfaceTopY + cueCollider.radius, setup.TableBounds.center.z - 0.325f);
            Vector3 objStart = new Vector3(setup.TableBounds.center.x + 0.30f, setup.SurfaceTopY + objCollider.radius, setup.TableBounds.center.z + 0.325f);
            var contactProbe = cue.gameObject.GetComponent<M23HeadlessContactProbe>() ?? cue.gameObject.AddComponent<M23HeadlessContactProbe>();
            contactProbe.Target = obj;
            cue.position = cueStart; obj.position = objStart;
            UnityEngine.Physics.SyncTransforms();

            SimulationMode oldSimulationMode = UnityEngine.Physics.simulationMode;
            UnityEngine.Physics.simulationMode = SimulationMode.Script;
            try
            {
                var samples = new List<Sample>(Repetitions);
                for (int rep = 0; rep < Repetitions; rep++)
                {
                    cue.position = cueStart; obj.position = objStart;
                    cue.linearVelocity = Vector3.zero; cue.angularVelocity = Vector3.zero;
                    obj.linearVelocity = Vector3.zero; obj.angularVelocity = Vector3.zero;
                    UnityEngine.Physics.SyncTransforms();
                    adapter.Configure(cue);
                    var shot = new CueShotData(direction, cue.position, 1f, ShotSpeed);
                    if (!adapter.Apply(shot, new Vector2(0f, DrawInput))) throw new InvalidOperationException($"CuePhysicsAdapter rejected Draw shot {rep + 1}.");

                    float peak = 0f;
                    bool contacted = false;
                    int contactStep = -1;
                    Vector3 cueVContact = Vector3.zero;
                    Vector3 objVContact = Vector3.zero;
                    Vector3 cuePContact = Vector3.zero;
                    Vector3 objPContact = Vector3.zero;
                    float previousDistance = Vector3.Distance(cue.position, obj.position);
                    for (int step = 0; step < 600; step++)
                    {
                        UnityEngine.Physics.Simulate(Dt);
                        peak = Mathf.Max(peak, obj.linearVelocity.magnitude);
                        float distance = Vector3.Distance(cue.position, obj.position);
                        if (!contacted && previousDistance > cueCollider.radius + objCollider.radius + 0.002f && distance <= cueCollider.radius + objCollider.radius + 0.002f)
                        {
                            contacted = true;
                            contactStep = step;
                            cueVContact = cue.linearVelocity;
                            objVContact = obj.linearVelocity;
                            cuePContact = cue.position;
                            objPContact = obj.position;
                        }
                        previousDistance = distance;
                        if (contacted && step >= contactStep + 10) break;
                    }
                    if (!contacted && !contactProbe.Contacted) throw new InvalidOperationException($"Draw shot {rep + 1} never reached cue/object contact.");
                    // Capture the object displacement after a deterministic 10-step post-contact window.
                    // The raw contact frame can still report the pre-flight transform due to discrete solver timing.
                    objPContact = obj.position;
                    if (contactProbe.Contacted)
                    {
                        cueVContact = contactProbe.CueVelocityAtContact;
                        objVContact = contactProbe.ObjectVelocityAtContact;
                        cuePContact = contactProbe.CuePositionAtContact;
                        objPContact = contactProbe.ObjectPositionAtContact;
                    }
                    Vector3 cueAfter = cue.linearVelocity;
                    float postSpeed = cueAfter.magnitude;
                    float dot = postSpeed > 0.000001f ? Vector3.Dot(cueAfter.normalized, direction) : 0f;
                    float postDistance = Vector3.Distance(cuePContact, cue.position);
                    float firstFlight = Vector3.Distance(objStart, objPContact);
                    bool pass = postSpeed >= 0.05f && dot <= -0.5f && peak > 0f && firstFlight > 0f;
                    samples.Add(new Sample { cueSpeedAtContact = cueVContact.magnitude, cueVelocityAtContact = cueVContact, cueVelocityAfterContact = cueAfter, objectVelocityAtContact = objVContact, objectPeakSpeed = peak, firstFlightDistance = firstFlight, cuePostContactDistance = postDistance, cuePostContactSpeed = postSpeed, drawDot = dot, passed = pass });
                    Debug.Log($"[147VR M2.3 HEADLESS] rep={rep + 1}/{Repetitions} contactStep={contactStep} cueContact={cueVContact.magnitude:F9} postCue={postSpeed:F9} drawDot={dot:F9} reverseDist={postDistance:F9} firstFlight={firstFlight:F9} objectPeak={peak:F9} {(pass ? "PASS" : "FAIL")}");
                    if (!pass) throw new InvalidOperationException($"Draw physics acceptance failed on repetition {rep + 1}: postCue={postSpeed}, dot={dot}");
                }
                var output = new Output
                {
                    timestampUtc = DateTime.UtcNow.ToString("O"), scene = ScenePath, unityVersion = Application.unityVersion, repetitions = samples.Count, shotSpeed = ShotSpeed, drawEnglish = DrawInput,
                    cueSpeedAtContact = Get(samples, s => s.cueSpeedAtContact), cueVelocityAtContact = GetV(samples, s => s.cueVelocityAtContact), cueVelocityAfterContact = GetV(samples, s => s.cueVelocityAfterContact), objectVelocityAtContact = GetV(samples, s => s.objectVelocityAtContact), objectPeakSpeed = Get(samples, s => s.objectPeakSpeed), firstFlightDistance = Get(samples, s => s.firstFlightDistance), cuePostContactDistance = Get(samples, s => s.cuePostContactDistance), cuePostContactSpeed = Get(samples, s => s.cuePostContactSpeed), drawDot = Get(samples, s => s.drawDot), passed = GetB(samples, s => s.passed)
                };
                string outFile = Path.Combine(root, OutputPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(outFile));
                File.WriteAllText(outFile, JsonUtility.ToJson(output, true));
                AssetDatabase.Refresh();
                Debug.Log($"[147VR M2.3 HEADLESS] REAL DRAW COMPLETE | {Repetitions}/{Repetitions} | input={DrawInput:F2} | JSON={outFile}");
            }
            finally { UnityEngine.Physics.simulationMode = oldSimulationMode; }
            EditorApplication.Exit(0);
        }

        private static float[] Get(List<Sample> s, Func<Sample,float> f) { var a = new float[s.Count]; for(int i=0;i<s.Count;i++) a[i]=f(s[i]); return a; }
        private static Vector3[] GetV(List<Sample> s, Func<Sample,Vector3> f) { var a = new Vector3[s.Count]; for(int i=0;i<s.Count;i++) a[i]=f(s[i]); return a; }
        private static bool[] GetB(List<Sample> s, Func<Sample,bool> f) { var a = new bool[s.Count]; for(int i=0;i<s.Count;i++) a[i]=f(s[i]); return a; }
    }

    internal sealed class M23HeadlessContactProbe : MonoBehaviour
    {
        public Rigidbody Target;
        public bool Contacted { get; private set; }
        public Vector3 CueVelocityAtContact { get; private set; }
        public Vector3 ObjectVelocityAtContact { get; private set; }
        public Vector3 CuePositionAtContact { get; private set; }
        public Vector3 ObjectPositionAtContact { get; private set; }

        public void ResetCapture()
        {
            Contacted = false;
            CueVelocityAtContact = Vector3.zero;
            ObjectVelocityAtContact = Vector3.zero;
            CuePositionAtContact = Vector3.zero;
            ObjectPositionAtContact = Vector3.zero;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Contacted || Target == null || collision.rigidbody != Target) return;
            Contacted = true;
            var cue = GetComponent<Rigidbody>();
            CueVelocityAtContact = cue != null ? cue.linearVelocity : Vector3.zero;
            ObjectVelocityAtContact = Target.linearVelocity;
            CuePositionAtContact = transform.position;
            ObjectPositionAtContact = Target.position;
            Debug.Log($"[147VR M2.3 HEADLESS] COLLISION callback | cueV={CueVelocityAtContact.magnitude:F9} objectV={ObjectVelocityAtContact.magnitude:F9}");
        }
    }}








