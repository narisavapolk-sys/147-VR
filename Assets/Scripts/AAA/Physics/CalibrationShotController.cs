using UnityEngine;
using VR147.AAA.Cue;

namespace VR147.AAA.Physics
{
    public sealed class CalibrationShotController : MonoBehaviour
    {
        [SerializeField] private Rigidbody targetBall;
        [SerializeField] private ShotMeasurementTracker tracker;
        [SerializeField] private ShotCalibrationCase calibrationCase;
        [SerializeField] private Transform shotOrigin;
        [SerializeField] private CueStrokeModel strokeModel;
        [SerializeField] private CuePhysicsAdapter physicsAdapter;
        [SerializeField] private Vector3 resetPosition;
        [SerializeField] private bool resetOnStart = true;
        [SerializeField] private SnookerPhysicsSetup physicsSetup;
        private bool environmentInitialized;

        public bool Completed { get; private set; }
        public ShotCalibrationResult Result { get; private set; }
        public bool HasExecutedShot { get; private set; }

        private void ResolveTargetBall()
        {
            if (targetBall != null)
            {
                tracker?.ConfigureTarget(targetBall);
                return;
            }
            var namedCue = UnityEngine.GameObject.Find("Sphere.009");
            targetBall = namedCue != null ? namedCue.GetComponent<Rigidbody>() : null;
            var ballTracker = tracker.GetComponent<SnookerBallTracker>();
            var info = ballTracker != null ? ballTracker.FindBall("White_CueBall") : null;
            if (targetBall == null)
                targetBall = info != null && info.transform != null ? info.transform.GetComponent<Rigidbody>() : null;
            if (targetBall == null)
            {
                foreach (var body in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
                {
                    if (body == null || body.isKinematic) continue;
                    var sphere = body.GetComponent<SphereCollider>();
                    if (sphere != null && sphere.radius > 0.02f && sphere.radius < 0.04f)
                    {
                        targetBall = body;
                        break;
                    }
                }
            }
            tracker.ConfigureTarget(targetBall);
            if (resetPosition == Vector3.zero && targetBall != null) resetPosition = targetBall.position;
        }

        private void Start()
        {
            InitializeCalibrationEnvironment();
            if (resetOnStart) ResetShot();
        }

        private void InitializeCalibrationEnvironment()
        {
            if (environmentInitialized) return;
            if (physicsSetup == null)
                physicsSetup = UnityEngine.Object.FindFirstObjectByType<SnookerPhysicsSetup>();
            if (physicsSetup == null)
            {
                Debug.LogError("[147VR Calibration] Environment init failed: SnookerPhysicsSetup missing.", this);
                return;
            }

            physicsSetup.EnsurePhysics();
            ResolveTargetBall();
            if (targetBall == null) return;
            if (!CalibrationEnvironmentIsolator.Isolate(targetBall, physicsSetup)) return;

            Vector3 safeStart = targetBall.position;
            safeStart.x = physicsSetup.TableBounds.center.x;
            safeStart.z = physicsSetup.TableBounds.center.z - 1.0f;
            safeStart.y = physicsSetup.SurfaceTopY + 0.02725f;
            targetBall.position = safeStart;
            targetBall.linearVelocity = Vector3.zero;
            targetBall.angularVelocity = Vector3.zero;
            UnityEngine.Physics.SyncTransforms();
            resetPosition = safeStart;
            environmentInitialized = true;
        }

        private void FixedUpdate()
        {
            if (tracker == null || !tracker.IsMeasuring) return;
            if (!tracker.TryCapture()) return;
            if (!ShotCalibrationEvaluator.IsValidCase(calibrationCase))
            {
                Result = new ShotCalibrationResult(false, tracker.Distance,
                    float.PositiveInfinity, tracker.PeakSpeed, float.PositiveInfinity);
                Completed = true;
                Debug.LogError("[147VR Calibration] Invalid calibration case.", this);
                return;
            }
            Result = ShotCalibrationEvaluator.Evaluate(
                calibrationCase, tracker.Distance, tracker.PeakSpeed);
            Completed = true;
            Debug.Log($"[147VR Calibration] {calibrationCase?.type}: " +
                      $"{(Result.passed ? "PASS" : "FAIL")} | " +
                      $"distance={Result.measuredDistance:F4} (error={Result.error:F4}) | " +
                      $"peakSpeed={Result.measuredPeakSpeed:F4} (error={Result.peakSpeedError:F4})", this);
        }

        public bool ExecuteShot()
        {
            ResolveTargetBall();
            if (targetBall == null) { Debug.LogError("[147VR Calibration] ExecuteShot: targetBall unresolved.", this); return false; }
            if (tracker == null) { Debug.LogError("[147VR Calibration] ExecuteShot: tracker missing.", this); return false; }
            if (calibrationCase == null) { Debug.LogError("[147VR Calibration] ExecuteShot: calibrationCase missing.", this); return false; }
            tracker.ConfigureTarget(targetBall);
            if (resetPosition == Vector3.zero && targetBall != null) resetPosition = targetBall.position;
            if (!ShotCalibrationEvaluator.IsValidCase(calibrationCase)) { Debug.LogError("[147VR Calibration] ExecuteShot: calibration case invalid.", this); return false; }
            if (strokeModel == null || strokeModel.EvaluateSpeed(0.5f) <= 0f)
            {
                strokeModel = new CueStrokeModel();
                var runtimeProfile = ScriptableObject.CreateInstance<CueAimProfile>();
                runtimeProfile.maxShotSpeed = 8f;
                runtimeProfile.minShotSpeed = 0.15f;
                runtimeProfile.chargeSeconds = 0.85f;
                runtimeProfile.deadZone = 0.02f;
                strokeModel.SetProfile(runtimeProfile);
            }
        if (physicsAdapter == null) physicsAdapter = GetComponent<CuePhysicsAdapter>();
        if (physicsAdapter == null) { Debug.LogError("[147VR Calibration] ExecuteShot: physicsAdapter missing.", this); return false; }
        physicsAdapter.Configure(targetBall);
            Vector3 direction = shotOrigin != null ? shotOrigin.forward : transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.000001f) return false;
            direction.Normalize();
            Vector3 aimPoint = targetBall.position + direction;
            if (!CueShotValidator.TryCreate(targetBall.position, aimPoint,
                    Mathf.Clamp01(calibrationCase.power), strokeModel, out CueShotData shot))
            {
                Debug.LogError($"[147VR Calibration] ExecuteShot: validator rejected. power={calibrationCase.power:F3}", this);
                return false;
            }
            Completed = false;
            HasExecutedShot = true;
            tracker.Begin();
            if (!physicsAdapter.Apply(shot, new Vector2(calibrationCase.side, calibrationCase.vertical)))
            {
                Debug.LogError($"[147VR Calibration] ExecuteShot: physics Apply rejected. mass={targetBall.mass:F3} speed={shot.speed:F3} impulse={targetBall.mass * shot.speed:F3}", this);
                return false;
            }
            return true;
        }

        [ContextMenu("Reset Shot")]
        public void ResetShot()
        {
            if (targetBall == null) return;
            targetBall.position = resetPosition;
            targetBall.rotation = Quaternion.identity;
            targetBall.linearVelocity = Vector3.zero;
            targetBall.angularVelocity = Vector3.zero;
            Completed = false;
            HasExecutedShot = false;
            tracker?.Begin();
        }
    }
}




