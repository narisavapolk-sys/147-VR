using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VR147.AAA.Cue;

namespace VR147.AAA.Physics
{
    public sealed class M21StunBatchRunner : MonoBehaviour
    {
        [SerializeField] private Rigidbody cueBall;
        [SerializeField] private Rigidbody objectBall;
        [SerializeField] private CuePhysicsAdapter physicsAdapter;
        [SerializeField] private SnookerPhysicsSetup physicsSetup;
        [SerializeField] private int repetitions = 5;
        [SerializeField] private float shotSpeed = 4f;
        [SerializeField] private float settleSpeed = 0.08f;
        [SerializeField] private float settleSeconds = 0.5f;
        [SerializeField] private float resetGap = 0.65f;
        [SerializeField] private float shotTimeoutSeconds = 10f;
        [SerializeField] private string outputFileName = "stun_runtime_measurements.json";

        private readonly List<Sample> samples = new List<Sample>();
        private Vector3 cueStart;
        private Vector3 objectStart;
        private Vector3 shotDirection;
        private bool shotActive;
        private bool collisionObserved;
        private bool settling;
        private float settleTimer;
        private float previousCueSpeed;
        private float previousObjectSpeed;
        private float shotElapsed;
        private float objectPeakAccumulator;
        private float cueSpeedAtCollision;
        private float objectSpeedAtCollision;
        private bool targetCollisionObserved;
        private int completedShots;

        [Serializable]
        private sealed class Sample
        {
            public float cueSpeedAtContact;
            public float cueResidualSpeed;
            public float cueResidualDistance;
            public float objectPeakSpeed;
            public float objectDistance;
            public float objectDirectionDot;
            public bool stunPass;
        }

        [Serializable]
        private sealed class Output
        {
            public string timestampUtc;
            public string caseType = "Stun";
            public string scene;
            public string unityVersion;
            public int repetitions;
            public float shotSpeed;
            public float collisionRestitution;
            public float[] measuredCueSpeedAtContact;
            public float[] measuredCueResidualSpeed;
            public float[] measuredCueResidualDistance;
            public float[] measuredObjectPeakSpeed;
            public float[] measuredObjectDistance;
            public float[] measuredObjectDirectionDot;
            public bool[] passed;
        }

        public bool Completed => completedShots >= repetitions;

        public void Configure(Rigidbody cue, Rigidbody target, CuePhysicsAdapter adapter)
        {
            cueBall = cue;
            objectBall = target;
            physicsAdapter = adapter;
        }

        private void Start()
        {
            physicsSetup ??= FindFirstObjectByType<SnookerPhysicsSetup>();
            if (physicsSetup == null)
            {
                Debug.LogError("[147VR M2.1] SnookerPhysicsSetup missing; cannot certify table surface.", this);
                enabled = false;
                return;
            }
            physicsSetup.EnsurePhysics();
            if (!NormalizeBodiesToAuthoritativeSurface())
            {
                enabled = false;
                return;
            }
            if (!ValidateReferences()) return;
            UnityEngine.Physics.SyncTransforms();
            InstallCollisionProbe(cueBall);
            InstallCollisionProbe(objectBall);
            BeginShot();
        }

        private bool NormalizeBodiesToAuthoritativeSurface()
        {
            var cueCollider = cueBall != null ? cueBall.GetComponent<SphereCollider>() : null;
            var objectCollider = objectBall != null ? objectBall.GetComponent<SphereCollider>() : null;
            if (cueCollider == null || objectCollider == null)
            {
                Debug.LogError("[147VR M2.1] Cue/Object SphereCollider missing.", this);
                return false;
            }
            float surfaceY = physicsSetup.SurfaceTopY;
            Vector3 tableCenter = physicsSetup.TableBounds.center;
            // M2.1 deterministic geometry: centre-line placement prevents inherited source offsets.
            const float calibrationLaneX = 0.30f;
            cueBall.position = new Vector3(tableCenter.x + calibrationLaneX, surfaceY + cueCollider.radius, tableCenter.z - 0.325f);
            objectBall.position = new Vector3(tableCenter.x + calibrationLaneX, surfaceY + objectCollider.radius, tableCenter.z + 0.325f);
            cueBall.linearVelocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
            objectBall.linearVelocity = Vector3.zero;
            objectBall.angularVelocity = Vector3.zero;
            cueBall.isKinematic = false;
            objectBall.isKinematic = false;
            cueBall.detectCollisions = true;
            objectBall.detectCollisions = true;
            cueBall.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            objectBall.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Debug.Log($"[147VR M2.1] AUTHORITATIVE SURFACE | topY={surfaceY:F6} | cueY={cueBall.position.y:F6} | objectY={objectBall.position.y:F6} | cueRadius={cueCollider.radius:F6} | objectRadius={objectCollider.radius:F6}", this);
            UnityEngine.Physics.SyncTransforms();
            return true;
        }

        private bool ValidateReferences()
        {
            if (cueBall == null || objectBall == null || physicsAdapter == null)
            {
                Debug.LogError("[147VR M2.1] Missing cue/object/physics adapter reference.", this);
                return false;
            }
            cueStart = cueBall.position;
            objectStart = objectBall.position;
            shotDirection = (objectStart - cueStart);
            shotDirection.y = 0f;
            if (shotDirection.sqrMagnitude < 0.000001f)
            {
                Debug.LogError("[147VR M2.1] Cue and object ball are not separated.", this);
                return false;
            }
            shotDirection.Normalize();
            return true;
        }

        private void BeginShot()
        {
            ResetBodies();
            previousCueSpeed = 0f;
            previousObjectSpeed = 0f;
            collisionObserved = false;
            settling = false;
            settleTimer = 0f;
            shotActive = true;
            shotElapsed = 0f;
            targetCollisionObserved = false;
            cueSpeedAtCollision = 0f;
            objectSpeedAtCollision = 0f;
            CueShotData shot = new CueShotData(shotDirection, cueBall.position, 1f, shotSpeed);
            physicsAdapter.Configure(cueBall);
            if (!physicsAdapter.Apply(shot, Vector2.zero))
            {
                Debug.LogError("[147VR M2.1] Real cue impulse was rejected.", this);
                enabled = false;
                return;
            }
            Debug.Log($"[147VR M2.1] REAL STUN shot {completedShots + 1}/{repetitions} launched | speed={shotSpeed:F3} | direction={shotDirection}");
        }

        private void FixedUpdate()
        {
            if (!shotActive) return;
            shotElapsed += Time.fixedDeltaTime;
            float cueSpeed = cueBall.linearVelocity.magnitude;
            float objectSpeed = objectBall.linearVelocity.magnitude;
            objectPeakAccumulator = Mathf.Max(objectPeakAccumulator, objectSpeed);

            if (!collisionObserved && !targetCollisionObserved && objectSpeed > 0.02f && previousObjectSpeed <= 0.02f)
            {
                NotifyTargetCollision(objectSpeed, cueSpeed, false);
            }

            previousCueSpeed = cueSpeed;
            previousObjectSpeed = objectSpeed;

            if (collisionObserved)
            {
                if (cueSpeed <= settleSpeed && objectSpeed <= settleSpeed)
                {
                    settleTimer += Time.fixedDeltaTime;
                    if (settleTimer >= settleSeconds) CompleteShot();
                }
                else
                {
                    settleTimer = 0f;
                }
            }

            if (shotElapsed >= shotTimeoutSeconds)
            {
                Debug.LogError($"[147VR M2.1] shot timeout | rep={completedShots + 1}/{repetitions} | collision={collisionObserved} | targetCollision={targetCollisionObserved} | cueSpeed={cueSpeed:F5} | objectSpeed={objectSpeed:F5} | cuePos={cueBall.position} | objectPos={objectBall.position}");
                enabled = false;
            }
        }

        private void InstallCollisionProbe(Rigidbody body)
        {
            if (body == null) return;
            var probe = body.GetComponent<M21CollisionProbe>();
            if (probe == null) probe = body.gameObject.AddComponent<M21CollisionProbe>();
            probe.Initialize(this, objectBall);
        }

        internal void NotifyTargetCollision(float objectSpeed, float cueSpeed, bool physicsEvent)
        {
            if (targetCollisionObserved) return;
            targetCollisionObserved = true;
            collisionObserved = true;
            cueSpeedAtCollision = cueSpeed;
            objectSpeedAtCollision = objectSpeed;
            Debug.Log($"[147VR M2.1] Tcollision observed | source={(physicsEvent ? "OnCollisionEnter" : "velocity-transition")} | cueSpeed={cueSpeed:F6} | objectSpeed={objectSpeed:F6}");
        }

        private void CompleteShot()
        {
            shotActive = false;
            settling = true;
            float cueDistance = Vector3.Distance(cueStart, cueBall.position);
            float objectDistance = Vector3.Distance(objectStart, objectBall.position);
            float cueResidual = cueBall.linearVelocity.magnitude;
            float objectPeak = objectPeakAccumulator;
            float directionDot = Vector3.Dot((objectBall.position - objectStart).normalized, shotDirection);
            bool stunPass = cueResidual <= settleSpeed;
            samples.Add(new Sample
            {
                cueSpeedAtContact = cueSpeedAtCollision > 0f ? cueSpeedAtCollision : previousCueSpeed,
                cueResidualSpeed = cueResidual,
                cueResidualDistance = cueDistance,
                objectPeakSpeed = objectPeak,
                objectDistance = objectDistance,
                objectDirectionDot = directionDot,
                stunPass = stunPass
            });
            completedShots++;
            Debug.Log($"[147VR M2.1] rep={completedShots}/{repetitions} | cueResidual={cueResidual:F6} | cueDistance={cueDistance:F6} | objectDistance={objectDistance:F6} | objectPeak={objectPeak:F6} | directionDot={directionDot:F6} | {(stunPass ? "PASS" : "FAIL")}");
            if (completedShots >= repetitions)
            {
                Persist();
                Debug.Log("[147VR M2.1] REAL MEASUREMENT COMPLETE");
                enabled = false;
                return;
            }
            Invoke(nameof(BeginShot), resetGap);
        }

        private void LateUpdate()
        {
            if (!shotActive) return;
            objectPeakAccumulator = Mathf.Max(objectPeakAccumulator, objectBall.linearVelocity.magnitude);
        }

        private void ResetBodies()
        {
            cueBall.position = cueStart;
            objectBall.position = objectStart;
            cueBall.rotation = Quaternion.identity;
            objectBall.rotation = Quaternion.identity;
            cueBall.linearVelocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
            objectBall.linearVelocity = Vector3.zero;
            objectBall.angularVelocity = Vector3.zero;
            cueBall.isKinematic = false;
            objectBall.isKinematic = false;
            cueBall.detectCollisions = true;
            objectBall.detectCollisions = true;
            objectPeakAccumulator = 0f;
            shotElapsed = 0f;
            targetCollisionObserved = false;
            cueSpeedAtCollision = 0f;
            objectSpeedAtCollision = 0f;
            UnityEngine.Physics.SyncTransforms();
        }

        private void Persist()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(root, "Assets/AAA/PhysicsCalibration/RuntimeMeasurements/" + outputFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var output = new Output
            {
                timestampUtc = DateTime.UtcNow.ToString("O"),
                scene = "Assets/AAA/PhysicsCalibration/147VR_M21_StunCalibration.unity",
                unityVersion = Application.unityVersion,
                repetitions = samples.Count,
                shotSpeed = shotSpeed,
                collisionRestitution = 0.8f,
                measuredCueSpeedAtContact = ToArray(s => s.cueSpeedAtContact),
                measuredCueResidualSpeed = ToArray(s => s.cueResidualSpeed),
                measuredCueResidualDistance = ToArray(s => s.cueResidualDistance),
                measuredObjectPeakSpeed = ToArray(s => s.objectPeakSpeed),
                measuredObjectDistance = ToArray(s => s.objectDistance),
                measuredObjectDirectionDot = ToArray(s => s.objectDirectionDot),
                passed = ToBoolArray(s => s.stunPass)
            };
            File.WriteAllText(path, JsonUtility.ToJson(output, true));
            Debug.Log($"[147VR M2.1] Persisted REAL measurement JSON: {path}");
        }

        private float[] ToArray(Func<Sample, float> selector)
        {
            var values = new float[samples.Count];
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

    public sealed class M21CollisionProbe : MonoBehaviour
    {
        private M21StunBatchRunner runner;
        private Rigidbody target;

        public void Initialize(M21StunBatchRunner owner, Rigidbody targetBody)
        {
            runner = owner;
            target = targetBody;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (runner == null) return;
            var rb = GetComponent<Rigidbody>();
            var contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Debug.Log($"[147VR M2.1] COLLISION | body={name} other={collision.collider.name} otherRB={(collision.rigidbody != null ? collision.rigidbody.name : "none")} y={transform.position.y:F6} normal={contact.normal} relV={collision.relativeVelocity.magnitude:F6}");
            if (target == null || collision.rigidbody != target) return;
            runner.NotifyTargetCollision(target.linearVelocity.magnitude, rb.linearVelocity.magnitude, true);
        }
    }
}


