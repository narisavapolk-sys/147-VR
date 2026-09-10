using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VR147.AAA.Cue;

namespace VR147.AAA.Physics
{
    public sealed class M23DrawBatchRunner : MonoBehaviour
    {
        [SerializeField] private Rigidbody cueBall;
        [SerializeField] private Rigidbody objectBall;
        [SerializeField] private CuePhysicsAdapter physicsAdapter;
        [SerializeField] private SnookerPhysicsSetup physicsSetup;
        [SerializeField] private int repetitions = 5;
        [SerializeField] private float shotSpeed = 4f;
        [SerializeField] private float drawEnglish = -0.75f;
        [SerializeField] private float settleSpeed = 0.08f;
        [SerializeField] private float settleSeconds = 0.5f;
        [SerializeField] private float resetGap = 0.65f;
        [SerializeField] private float shotTimeoutSeconds = 10f;
        [SerializeField] private string outputFileName = "draw_runtime_measurements.json";

        private readonly List<Sample> samples = new List<Sample>();
        private Vector3 cueStart;
        private Vector3 objectStart;
        private Vector3 shotDirection;
        private bool shotActive;
        private bool targetCollisionObserved;
        private float settleTimer;
        private float shotElapsed;
        private float previousObjectSpeed;
        private float objectPeak;
        private int completedShots;
        private float cueSpeedAtContact;
        private Vector3 cueVelocityAtContact;
        private Vector3 objectVelocityAtContact;
        private Vector3 cueVelocityAfterContact;
        private Vector3 cuePositionAtContact;
        private Vector3 objectPositionAtContact;

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
            public float cueResidualSpeed;
            public float drawDot;
            public bool drawPass;
        }

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
            public float[] cueResidualSpeed;
            public float[] drawDot;
            public bool[] passed;
        }

        public bool Completed => completedShots >= repetitions;

        private void Start()
        {
            physicsSetup ??= FindFirstObjectByType<SnookerPhysicsSetup>();
            cueBall ??= FindBall("Red");
            objectBall ??= FindBall("Sphere.009");
            physicsAdapter ??= FindFirstObjectByType<CuePhysicsAdapter>();
            if (physicsSetup == null || cueBall == null || objectBall == null || physicsAdapter == null)
            {
                Debug.LogError("[147VR M2.3] Missing physics setup, balls, or CuePhysicsAdapter.", this);
                enabled = false;
                return;
            }
            physicsSetup.EnsurePhysics();
            if (!NormalizeBodies()) { enabled = false; return; }
            cueStart = cueBall.position;
            objectStart = objectBall.position;
            shotDirection = objectStart - cueStart;
            shotDirection.y = 0f;
            shotDirection.Normalize();
            InstallProbe(cueBall);
            InstallProbe(objectBall);
            BeginShot();
        }

        private Rigidbody FindBall(string preferredName)
        {
            GameObject go = GameObject.Find(preferredName);
            return go != null ? go.GetComponent<Rigidbody>() : null;
        }

        private bool NormalizeBodies()
        {
            var cueCollider = cueBall.GetComponent<SphereCollider>();
            var objectCollider = objectBall.GetComponent<SphereCollider>();
            if (cueCollider == null || objectCollider == null) return false;
            float y = physicsSetup.SurfaceTopY;
            Vector3 center = physicsSetup.TableBounds.center;
            const float laneX = 0.30f;
            cueBall.position = new Vector3(center.x + laneX, y + cueCollider.radius, center.z - 0.325f);
            objectBall.position = new Vector3(center.x + laneX, y + objectCollider.radius, center.z + 0.325f);
            cueBall.isKinematic = false; objectBall.isKinematic = false;
            cueBall.detectCollisions = true; objectBall.detectCollisions = true;
            cueBall.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            objectBall.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ResetBodies();
            UnityEngine.Physics.SyncTransforms();
            Debug.Log($"[147VR M2.3] SURFACE topY={y:F6} cueY={cueBall.position.y:F6} objectY={objectBall.position.y:F6} drawEnglish={drawEnglish:F3}");
            return true;
        }

        private void InstallProbe(Rigidbody body)
        {
            var probe = body.GetComponent<M23DrawCollisionProbe>();
            if (probe == null) probe = body.gameObject.AddComponent<M23DrawCollisionProbe>();
            probe.Initialize(this, objectBall);
        }

        private void BeginShot()
        {
            ResetBodies();
            targetCollisionObserved = false;
            settleTimer = 0f;
            shotElapsed = 0f;
            cueSpeedAtContact = 0f;
            cueVelocityAtContact = Vector3.zero;
            cueVelocityAfterContact = Vector3.zero;
            cuePositionAtContact = Vector3.zero;
            objectPositionAtContact = Vector3.zero;
            objectVelocityAtContact = Vector3.zero;
            objectPeak = 0f;
            shotActive = true;
            CueShotData shot = new CueShotData(shotDirection, cueBall.position, 1f, shotSpeed);
            physicsAdapter.Configure(cueBall);
            if (!physicsAdapter.Apply(shot, new Vector2(0f, drawEnglish)))
            {
                Debug.LogError("[147VR M2.3] Draw impulse rejected.", this);
                enabled = false;
                return;
            }
            Debug.Log($"[147VR M2.3] REAL DRAW shot {completedShots + 1}/{repetitions} speed={shotSpeed:F3} englishY={drawEnglish:F3}");
        }

        private void FixedUpdate()
        {
            if (!shotActive) return;
            shotElapsed += Time.fixedDeltaTime;
            float cueSpeed = cueBall.linearVelocity.magnitude;
            float objectSpeed = objectBall.linearVelocity.magnitude;
            objectPeak = Mathf.Max(objectPeak, objectSpeed);
            if (targetCollisionObserved)
            {
                if (cueSpeed <= settleSpeed && objectSpeed <= settleSpeed)
                {
                    settleTimer += Time.fixedDeltaTime;
                    if (settleTimer >= settleSeconds) CompleteShot();
                }
                else settleTimer = 0f;
            }
            if (shotElapsed >= shotTimeoutSeconds)
            {
                Debug.LogError($"[147VR M2.3] TIMEOUT rep={completedShots + 1}/{repetitions} collision={targetCollisionObserved} cue={cueBall.position} object={objectBall.position}");
                enabled = false;
            }
        }

        internal void NotifyTargetCollision(Collision collision)
        {
            if (targetCollisionObserved || collision.rigidbody != objectBall) return;
            targetCollisionObserved = true;
            cueSpeedAtContact = cueBall.linearVelocity.magnitude;
            cuePositionAtContact = cueBall.position;
            objectPositionAtContact = objectBall.position;
            cueVelocityAtContact = cueBall.linearVelocity;
            objectVelocityAtContact = objectBall.linearVelocity;
            Debug.Log($"[147VR M2.3] Tcollision | other={collision.collider.name} contact={collision.GetContact(0).point} normal={collision.GetContact(0).normal} cueV={cueVelocityAtContact} objectV={objectVelocityAtContact}");
            Invoke(nameof(CapturePostContactVelocity), Time.fixedDeltaTime * 2f);
        }

        private void CapturePostContactVelocity()
        {
            if (!shotActive) return;
            cueVelocityAfterContact = cueBall.linearVelocity;
        }

        private void CompleteShot()
        {
            shotActive = false;
            float objectDistance = Vector3.Distance(objectStart, objectBall.position);
            float cuePostDistance = Vector3.Distance(cuePositionAtContact, cueBall.position);
            float cueResidual = cueBall.linearVelocity.magnitude;
            Vector3 cueForward = cueVelocityAtContact.sqrMagnitude > 0.000001f ? cueVelocityAtContact.normalized : shotDirection;
            float postSpeed = cueVelocityAfterContact.magnitude;
            float drawDot = postSpeed > 0.000001f ? Vector3.Dot(cueVelocityAfterContact.normalized, shotDirection) : 0f;
            float firstFlight = Vector3.Distance(objectStart, objectPositionAtContact);
            bool pass = targetCollisionObserved && postSpeed >= 0.05f && drawDot <= -0.5f;
            samples.Add(new Sample
            {
                cueSpeedAtContact = cueSpeedAtContact,
                cueVelocityAtContact = cueVelocityAtContact,
                cueVelocityAfterContact = cueVelocityAfterContact,
                objectVelocityAtContact = objectVelocityAtContact,
                objectPeakSpeed = objectPeak,
                firstFlightDistance = firstFlight,
                cuePostContactDistance = cuePostDistance,
                cuePostContactSpeed = postSpeed,
                cueResidualSpeed = cueResidual,
                drawDot = drawDot,
                drawPass = pass
            });
            completedShots++;
            Debug.Log($"[147VR M2.3] rep={completedShots}/{repetitions} postCueSpeed={postSpeed:F6} cuePostDistance={cuePostDistance:F6} firstFlight={firstFlight:F6} cueResidual={cueResidual:F6} drawDot={drawDot:F6} objectPeak={objectPeak:F6} {(pass ? "PASS" : "FAIL")}");
            if (completedShots >= repetitions)
            {
                Persist();
                Debug.Log("[147VR M2.3] REAL DRAW MEASUREMENT COMPLETE");
                enabled = false;
                return;
            }
            Invoke(nameof(BeginShot), resetGap);
        }
        private void ResetBodies()
        {
            if (cueBall == null || objectBall == null || cueStart == default || objectStart == default) return;
            cueBall.position = cueStart;
            objectBall.position = objectStart;
            cueBall.linearVelocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
            objectBall.linearVelocity = Vector3.zero;
            objectBall.angularVelocity = Vector3.zero;
            cueBall.isKinematic = false;
            objectBall.isKinematic = false;
            objectPeak = 0f;
            UnityEngine.Physics.SyncTransforms();
        }

        private void Persist()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string dir = Path.Combine(root, "Assets/AAA/PhysicsCalibration/RuntimeMeasurements");
            Directory.CreateDirectory(dir);
            var output = new Output
            {
                timestampUtc = DateTime.UtcNow.ToString("O"),
                scene = "Assets/AAA/PhysicsCalibration/147VR_M23_DrawCalibration.unity",
                unityVersion = Application.unityVersion,
                repetitions = samples.Count,
                shotSpeed = shotSpeed,
                drawEnglish = drawEnglish,
                cueSpeedAtContact = ToArray(s => s.cueSpeedAtContact),
                cueVelocityAtContact = ToArrayV(s => s.cueVelocityAtContact),
                cueVelocityAfterContact = ToArrayV(s => s.cueVelocityAfterContact),
                objectVelocityAtContact = ToArrayV(s => s.objectVelocityAtContact),
                objectPeakSpeed = ToArray(s => s.objectPeakSpeed),
                firstFlightDistance = ToArray(s => s.firstFlightDistance),
                cuePostContactDistance = ToArray(s => s.cuePostContactDistance),
                cuePostContactSpeed = ToArray(s => s.cuePostContactSpeed),
                cueResidualSpeed = ToArray(s => s.cueResidualSpeed),
                drawDot = ToArray(s => s.drawDot),
                passed = ToBoolArray(s => s.drawPass)
            };
            string path = Path.Combine(dir, outputFileName);
            File.WriteAllText(path, JsonUtility.ToJson(output, true));
            Debug.Log($"[147VR M2.3] Persisted REAL Draw JSON: {path}");
        }

        private float[] ToArray(Func<Sample, float> selector)
        {
            var values = new float[samples.Count];
            for (int i = 0; i < samples.Count; i++) values[i] = selector(samples[i]);
            return values;
        }

        private Vector3[] ToArrayV(Func<Sample, Vector3> selector)
        {
            var values = new Vector3[samples.Count];
            for (int i = 0; i < samples.Count; i++) values[i] = selector(samples[i]);
            return values;
        }

        private bool[] ToBoolArray(Func<Sample, bool> selector)
        {
            var values = new bool[samples.Count];
            for (int i = 0; i < samples.Count; i++) values[i] = selector(samples[i]);
            return values;
        }
    }

    public sealed class M23DrawCollisionProbe : MonoBehaviour
    {
        private M23DrawBatchRunner runner;
        private Rigidbody target;

        public void Initialize(M23DrawBatchRunner owner, Rigidbody targetBody)
        {
            runner = owner;
            target = targetBody;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (runner == null || target == null || collision.rigidbody != target) return;
            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Debug.Log($"[147VR M2.3] COLLISION | body={name} other={collision.collider.name} point={contact.point} normal={contact.normal} relV={collision.relativeVelocity.magnitude:F6}");
            runner.NotifyTargetCollision(collision);
        }
    }
}



