using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VR147.AAA.Physics
{
    public sealed class ClothResponseMatrixRunner : MonoBehaviour
    {
        [SerializeField] private SnookerPhysicsSetup physicsSetup;
        [SerializeField] private TableSurfaceProfile profile;
        [SerializeField] private Rigidbody ball;
        [SerializeField] private int repetitions = 5;
        [SerializeField] private float rollSpeed = 4f;
        [SerializeField] private float slideSpeed = 1f;
        [SerializeField] private float spinSpeed = 10f;
        [SerializeField] private float spinProbeLinearSpeed = 0f;
        [SerializeField] private string outputFileName = "cloth_runtime_equivalent_matrix.json";

        private const float DefaultLinearDamping = 0.20f;
        private const float DefaultAngularDamping = 0.20f;
        private const float DefaultMaterialFriction = 0.05f;
        private const float MinFitCoefficient = 0.001f;
        private const float MaxFitCoefficient = 20f;
        private const float AcceptanceRelativeError = 0.02f;

        private static readonly float[] RollTimes = { 0.04f, 0.08f, 0.12f, 0.16f };
        private static readonly float[] SlideTimes = { 0.04f, 0.08f, 0.12f, 0.16f };
        private static readonly float[] SpinTimes = { 0.10f, 0.20f, 0.30f, 0.40f };

        [Serializable]
        private sealed class SampleSet
        {
            public float[] values;
        }

        [Serializable]
        private sealed class MatrixFile
        {
            public string timestampUtc;
            public string calibrationKind;
            public string unityVersion;
            public int repetitions;
            public float rollInitialSpeed;
            public float slideInitialSpeed;
            public float spinInitialSpeed;
            public float[] rollTimes;
            public float[] slideTimes;
            public float[] spinTimes;
            public SampleSet[] baselineRollSpeed;
            public SampleSet[] baselineSlideSpeed;
            public SampleSet[] baselineSpinSpeed;
            public SampleSet[] equivalentRollSpeed;
            public SampleSet[] equivalentSlideSpeed;
            public SampleSet[] equivalentSpinSpeed;
            public float fittedRollingFriction;
            public float fittedRollingDamping;
            public float fittedSlidingFriction;
            public float fittedSpinFriction;
            public float maxRollingRelativeError;
            public float maxSlidingRelativeError;
            public float maxSpinRelativeError;
            public bool baselineMotionValid;
            public bool controllerReady;
            public bool candidateAccepted;
        }

        private readonly List<float[]> baselineRoll = new();
        private readonly List<float[]> baselineSlide = new();
        private readonly List<float[]> baselineSpin = new();
        private readonly List<float[]> equivalentRoll = new();
        private readonly List<float[]> equivalentSlide = new();
        private readonly List<float[]> equivalentSpin = new();

        private TableSurfaceController surfaceController;
        private void Start()
        {
            if (physicsSetup == null)
                physicsSetup = FindFirstObjectByType<SnookerPhysicsSetup>();

            if (physicsSetup == null || profile == null)
            {
                Debug.LogError("[147VR Cloth Matrix] Missing physics setup/profile.", this);
                return;
            }

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (ball == null)
                ball = ResolveBall();

            if (ball == null)
            {
                Debug.LogError("[147VR Cloth Matrix] Cue ball not found.", this);
                yield break;
            }

            physicsSetup.tableSurfaceProfile = profile;
            profile.measuredTruthCertified = false;
            physicsSetup.EnsurePhysics();

            surfaceController = EnsureSurfaceController();
            if (surfaceController == null)
            {
                Debug.LogError("[147VR Cloth Matrix] Surface controller could not be created.", this);
                yield break;
            }

            IsolateProbeBall();
            Debug.Log($"[147VR Cloth Matrix] Probe ball={ball.name} worldRadius={GetWorldBallRadius():F6}.");
            PrepareBallBaseline();
            yield return new WaitForFixedUpdate();

            yield return RunProbeBatch(ProbeKind.Roll, baselineRoll);
            yield return RunProbeBatch(ProbeKind.Slide, baselineSlide);
            yield return RunProbeBatch(ProbeKind.Spin, baselineSpin);

            bool baselineValid =
                ValidateTrajectory(baselineRoll, rollSpeed, requireDecay: true) &&
                ValidateTrajectory(baselineSlide, slideSpeed, requireDecay: true) &&
                ValidateTrajectory(baselineSpin, spinSpeed, requireDecay: true);

            float rollingDampingFit;
            float rollingFit = FitRollingModel(
                FlattenMean(baselineRoll),
                RollTimes,
                rollSpeed,
                out rollingDampingFit);
            float slidingFit = FitConstantDeceleration(FlattenMean(baselineSlide), SlideTimes, slideSpeed);
            float spinFit = FitExponentialDecay(FlattenMean(baselineSpin), SpinTimes, spinSpeed);

            bool coefficientValid =
                IsFiniteBounded(rollingFit) &&
                IsFiniteBounded(rollingDampingFit) &&
                IsFiniteBounded(slidingFit) &&
                IsFiniteBounded(spinFit);

            if (!baselineValid || !coefficientValid)
            {
                Persist(rollingFit, rollingDampingFit, slidingFit, spinFit, 100f, 100f, 100f, baselineValid, true, false);
                Debug.LogError($"[147VR Cloth Matrix] REJECT | baselineValid={baselineValid} coefficientValid={coefficientValid} | rolling={rollingFit:F6} rollingDamping={rollingDampingFit:F6} sliding={slidingFit:F6} spin={spinFit:F6}");
                StopEditorPlayMode();
                yield break;
            }

            rollingFit = Mathf.Clamp(rollingFit, MinFitCoefficient, MaxFitCoefficient);
            rollingDampingFit = Mathf.Clamp(rollingDampingFit, MinFitCoefficient, MaxFitCoefficient);
            slidingFit = Mathf.Clamp(slidingFit, MinFitCoefficient, MaxFitCoefficient);
            spinFit = Mathf.Clamp(spinFit, MinFitCoefficient, MaxFitCoefficient);

            profile.rollingFriction = rollingFit;
            profile.rollingDamping = rollingDampingFit;
            profile.slidingFriction = slidingFit;
            profile.spinFriction = spinFit;
            profile.measuredTruthCertified = true;
            surfaceController.Configure(profile);

            PrepareBallEquivalent();
            yield return new WaitForFixedUpdate();

            yield return RunProbeBatch(ProbeKind.Roll, equivalentRoll);
            yield return RunProbeBatch(ProbeKind.Slide, equivalentSlide);
            yield return RunProbeBatch(ProbeKind.Spin, equivalentSpin);

            float rollError = MaxRelativeError(baselineRoll, equivalentRoll);
            float slideError = MaxRelativeError(baselineSlide, equivalentSlide);
            float spinError = MaxRelativeError(baselineSpin, equivalentSpin);

            bool equivalentValid =
                ValidateTrajectory(equivalentRoll, rollSpeed, requireDecay: true) &&
                ValidateTrajectory(equivalentSlide, slideSpeed, requireDecay: true) &&
                ValidateTrajectory(equivalentSpin, spinSpeed, requireDecay: true);

            bool accepted =
                surfaceController != null &&
                baselineValid &&
                equivalentValid &&
                coefficientValid &&
                rollError <= AcceptanceRelativeError &&
                slideError <= AcceptanceRelativeError &&
                spinError <= AcceptanceRelativeError;

            Persist(
                rollingFit,
                rollingDampingFit,
                slidingFit,
                spinFit,
                rollError,
                slideError,
                spinError,
                baselineValid,
                surfaceController != null,
                accepted);

            Debug.Log(
                $"[147VR Cloth Matrix] {(accepted ? "ACCEPT" : "REJECT")} | " +
                $"rolling={rollingFit:F6} rollingDamping={rollingDampingFit:F6} sliding={slidingFit:F6} spin={spinFit:F6} | " +
                $"maxErr roll={rollError:P2} slide={slideError:P2} spin={spinError:P2}");

            StopEditorPlayMode();
        }

        private enum ProbeKind { Roll, Slide, Spin }

        private IEnumerator RunProbeBatch(ProbeKind kind, List<float[]> destination)
        {
            // Prime the contact manifold after switching probe modes. The
            // warm-up run is deliberately discarded so only settled contact
            // responses enter the 5x acceptance dataset.
            var warmup = new List<float[]>();
            yield return RunProbe(kind, warmup);

            for (int i = 0; i < repetitions; i++)
                yield return RunProbe(kind, destination);
        }

        private IEnumerator RunProbe(ProbeKind kind, List<float[]> destination)
        {
            ResetBall();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            float[] times = kind == ProbeKind.Roll ? RollTimes :
                            kind == ProbeKind.Slide ? SlideTimes : SpinTimes;

            float initialLinear = kind == ProbeKind.Roll ? rollSpeed :
                                  kind == ProbeKind.Slide ? slideSpeed : spinProbeLinearSpeed;
            float initialAngular = kind == ProbeKind.Roll
                ? rollSpeed / Mathf.Max(GetWorldBallRadius(), 0.001f)
                : kind == ProbeKind.Spin ? spinSpeed : 0f;

            ball.isKinematic = false;
            ball.detectCollisions = true;
            ball.linearVelocity = Vector3.forward * initialLinear;
            ball.angularVelocity = kind == ProbeKind.Roll
                ? Vector3.left * initialAngular
                : Vector3.up * initialAngular;
            UnityEngine.Physics.SyncTransforms();

            float[] samples = new float[times.Length];
            int index = 0;
            float elapsed = 0f;

            while (index < times.Length)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;

                if (elapsed + 0.000001f >= times[index])
                {
                    samples[index] = kind == ProbeKind.Spin
                        ? ball.angularVelocity.magnitude
                        : new Vector3(ball.linearVelocity.x, 0f, ball.linearVelocity.z).magnitude;
                    index++;
                }
            }

            ResetBall();
            destination.Add(samples);
        }

        private TableSurfaceController EnsureSurfaceController()
        {
            var surface = GameObject.Find("Surface");
            if (surface == null)
                return null;

            var controller = surface.GetComponent<TableSurfaceController>();
            if (controller == null)
                controller = surface.AddComponent<TableSurfaceController>();

            controller.Configure(profile);
            return controller;
        }

        private void PrepareBallBaseline()
        {
            var collider = ball.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.enabled = true;
                if (collider.sharedMaterial != null)
                {
                    collider.sharedMaterial.dynamicFriction = DefaultMaterialFriction;
                    collider.sharedMaterial.staticFriction = DefaultMaterialFriction;
                }
            }

            ball.linearDamping = DefaultLinearDamping;
            ball.angularDamping = DefaultAngularDamping;
            // A 4 m/s true-rolling probe requires ~140 rad/s for this ball radius.
            // Unity/PhysX may otherwise clamp angular velocity before the probe starts.
            ball.maxAngularVelocity = Mathf.Max(ball.maxAngularVelocity, 250f);
            ball.isKinematic = false;
            ball.detectCollisions = true;
            profile.measuredTruthCertified = false;
            surfaceController.Configure(profile);
        }

        private void PrepareBallEquivalent()
        {
            var collider = ball.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.enabled = true;
                if (collider.sharedMaterial != null)
                {
                    collider.sharedMaterial.dynamicFriction = 0f;
                    collider.sharedMaterial.staticFriction = 0f;
                }
            }

            ball.linearDamping = 0f;
            ball.angularDamping = 0f;
            ball.maxAngularVelocity = Mathf.Max(ball.maxAngularVelocity, 250f);
            ball.isKinematic = false;
            ball.detectCollisions = true;
            profile.measuredTruthCertified = true;
            surfaceController.Configure(profile);
        }

        private void IsolateProbeBall()
        {
            foreach (var candidate in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (candidate == null || candidate == ball)
                    continue;

                var sphere = candidate.GetComponent<SphereCollider>();
                if (sphere == null)
                    continue;

                candidate.linearVelocity = Vector3.zero;
                candidate.angularVelocity = Vector3.zero;
                candidate.isKinematic = true;
                candidate.detectCollisions = false;
            }
        }

        private void ResetBall()
        {
            Vector3 p = ball.position;
            p.x = physicsSetup.TableBounds.center.x;
            p.z = physicsSetup.TableBounds.center.z - 1f;
            p.y = physicsSetup.SurfaceTopY + GetWorldBallRadius() + 0.0001f;
            ball.position = p;
            ball.rotation = Quaternion.identity;
            ball.linearVelocity = Vector3.zero;
            ball.angularVelocity = Vector3.zero;
            UnityEngine.Physics.SyncTransforms();
        }

        private float GetWorldBallRadius()
        {
            var sphere = ball != null ? ball.GetComponent<SphereCollider>() : null;
            if (sphere == null)
                return Mathf.Max(physicsSetup.ballRadius, 0.026f);

            Vector3 lossy = ball.transform.lossyScale;
            float scale = Mathf.Max(
                0.0001f,
                (Mathf.Abs(lossy.x) + Mathf.Abs(lossy.y) + Mathf.Abs(lossy.z)) / 3f);
            return sphere.radius * scale;
        }

        private Rigidbody ResolveBall()
        {
            var named = GameObject.Find("Sphere.009");
            if (named != null)
            {
                var rb = named.GetComponent<Rigidbody>();
                if (rb != null) return rb;
            }

            foreach (var candidate in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (candidate == null || candidate.isKinematic) continue;
                var sphere = candidate.GetComponent<SphereCollider>();
                if (sphere != null && sphere.radius > 0.02f && sphere.radius < 0.04f)
                    return candidate;
            }

            return null;
        }

        private static float[] FlattenMean(List<float[]> samples)
        {
            if (samples.Count == 0) return Array.Empty<float>();

            int n = samples[0].Length;
            var mean = new float[n];
            for (int i = 0; i < samples.Count; i++)
                for (int j = 0; j < n; j++)
                    mean[j] += samples[i][j];

            for (int j = 0; j < n; j++)
                mean[j] /= samples.Count;

            return mean;
        }

        private static float FitRollingModel(
            float[] speeds,
            float[] times,
            float initial,
            out float damping)
        {
            float bestFriction = float.NaN;
            float bestDamping = float.NaN;
            float bestError = float.MaxValue;

            // Fit dv/dt = -friction - damping*v directly against the
            // measured baseline trajectory. This preserves both the
            // cloth-like constant term and the baseline's viscous term.
            for (int dampingStep = 1; dampingStep <= 250; dampingStep++)
            {
                float candidateDamping = dampingStep * 0.002f;

                for (int frictionStep = 1; frictionStep <= 1000; frictionStep++)
                {
                    float candidateFriction = frictionStep * 0.002f;
                    float maxError = 0f;

                    for (int i = 0; i < speeds.Length; i++)
                    {
                        float t = times[i];
                        float predicted;
                        if (candidateDamping > 0.000001f)
                        {
                            float ratio = candidateFriction / candidateDamping;
                            predicted = (initial + ratio) *
                                Mathf.Exp(-candidateDamping * t) - ratio;
                        }
                        else
                        {
                            predicted = initial - candidateFriction * t;
                        }

                        float denom = Mathf.Max(Mathf.Abs(speeds[i]), 0.001f);
                        maxError = Mathf.Max(
                            maxError,
                            Mathf.Abs(predicted - speeds[i]) / denom);
                    }

                    if (maxError < bestError)
                    {
                        bestError = maxError;
                        bestFriction = candidateFriction;
                        bestDamping = candidateDamping;
                    }
                }
            }

            damping = bestDamping;
            return bestFriction;
        }

        private static float FitConstantDeceleration(float[] speeds, float[] times, float initial)
        {
            double num = 0d;
            double den = 0d;

            for (int i = 0; i < speeds.Length; i++)
            {
                double t = times[i];
                num += t * (initial - speeds[i]);
                den += t * t;
            }

            return den > 0d ? Mathf.Max(0f, (float)(num / den)) : float.NaN;
        }

        private static float FitExponentialDecay(float[] speeds, float[] times, float initial)
        {
            double num = 0d;
            double den = 0d;

            for (int i = 0; i < speeds.Length; i++)
            {
                if (speeds[i] <= 0.000001f) continue;

                double t = times[i];
                double y = -Math.Log(speeds[i] / initial);
                num += t * y;
                den += t * t;
            }

            return den > 0d ? Mathf.Max(0f, (float)(num / den)) : float.NaN;
        }

        private static bool IsFiniteBounded(float value) =>
            !float.IsNaN(value) &&
            !float.IsInfinity(value) &&
            value >= MinFitCoefficient &&
            value <= MaxFitCoefficient;

        private static bool ValidateTrajectory(List<float[]> samples, float initial, bool requireDecay)
        {
            if (samples.Count == 0) return false;

            for (int i = 0; i < samples.Count; i++)
            {
                var values = samples[i];
                if (values == null || values.Length < 2) return false;

                float first = values[0];
                float last = values[values.Length - 1];

                if (float.IsNaN(first) || float.IsInfinity(first) ||
                    float.IsNaN(last) || float.IsInfinity(last))
                    return false;

                if (first <= 0.0005f || last <= 0.0001f)
                    return false;

                if (first > initial * 1.05f)
                    return false;

                if (requireDecay && last >= first)
                    return false;
            }

            return true;
        }

        private static float MaxRelativeError(List<float[]> a, List<float[]> b)
        {
            if (a.Count == 0 || b.Count == 0 || a.Count != b.Count)
                return 100f;

            float max = 0f;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] == null || b[i] == null || a[i].Length != b[i].Length)
                    return 100f;

                for (int j = 0; j < a[i].Length; j++)
                {
                    float denom = Mathf.Max(Mathf.Abs(a[i][j]), 0.001f);
                    max = Mathf.Max(max, Mathf.Abs(a[i][j] - b[i][j]) / denom);
                }
            }

            return max;
        }

        private void Persist(
            float rolling,
            float rollingDamping,
            float sliding,
            float spin,
            float rollError,
            float slideError,
            float spinError,
            bool baselineValid,
            bool controllerReady,
            bool accepted)
        {
            var payload = new MatrixFile
            {
                timestampUtc = DateTime.UtcNow.ToString("O"),
                calibrationKind = "RuntimeEquivalent",
                unityVersion = Application.unityVersion,
                repetitions = repetitions,
                rollInitialSpeed = rollSpeed,
                slideInitialSpeed = slideSpeed,
                spinInitialSpeed = spinSpeed,
                rollTimes = RollTimes,
                slideTimes = SlideTimes,
                spinTimes = SpinTimes,
                baselineRollSpeed = ToSampleSets(baselineRoll),
                baselineSlideSpeed = ToSampleSets(baselineSlide),
                baselineSpinSpeed = ToSampleSets(baselineSpin),
                equivalentRollSpeed = ToSampleSets(equivalentRoll),
                equivalentSlideSpeed = ToSampleSets(equivalentSlide),
                equivalentSpinSpeed = ToSampleSets(equivalentSpin),
                fittedRollingFriction = rolling,
                fittedRollingDamping = rollingDamping,
                fittedSlidingFriction = sliding,
                fittedSpinFriction = spin,
                maxRollingRelativeError = rollError,
                maxSlidingRelativeError = slideError,
                maxSpinRelativeError = spinError,
                baselineMotionValid = baselineValid,
                controllerReady = controllerReady,
                candidateAccepted = accepted
            };

            string dir = Path.Combine(
                Application.dataPath,
                "AAA",
                "PhysicsCalibration",
                "RuntimeMeasurements");

            Directory.CreateDirectory(dir);

            File.WriteAllText(
                Path.Combine(dir, outputFileName),
                JsonUtility.ToJson(payload, true));
        }

        private static SampleSet[] ToSampleSets(List<float[]> source)
        {
            var result = new SampleSet[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = new SampleSet { values = source[i] };
            return result;
        }

#if UNITY_EDITOR
        private static void StopEditorPlayMode()
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
#else
        private static void StopEditorPlayMode() { }
#endif
    }
}
