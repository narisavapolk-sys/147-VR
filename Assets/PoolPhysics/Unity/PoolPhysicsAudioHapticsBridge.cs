using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using PoolPhysics.Sim;

namespace PoolPhysics.Unity
{
    /// <summary>
    /// Bridges PoolPhysicsEventRunner simulation events (SimEvent) directly to:
    /// 1. Spatial 3D Audio SFX with velocity-proportional volume and dynamic pitch scaling.
    /// 2. XR / Meta Quest Controller Haptic Feedback triggered upon striking or ball-ball impact.
    /// </summary>
    [RequireComponent(typeof(PoolPhysicsEventRunner))]
    [DisallowMultipleComponent]
    public sealed class PoolPhysicsAudioHapticsBridge : MonoBehaviour
    {
        [Header("Audio Clips")]
        [SerializeField] private AudioClip ballBallClip;
        [SerializeField] private AudioClip cushionClip;
        [SerializeField] private AudioClip jawClip;
        [SerializeField] private AudioClip pocketClip;
        [SerializeField] private AudioClip cueHitClip;

        [Header("Audio Tuning")]
        [Range(0f, 2f)] [SerializeField] private float masterVolume = 1.0f;
        [SerializeField] private float minSpeedThreshold = 0.05f;
        [SerializeField] private float maxSpeedReference = 3.5f;
        [SerializeField] private float pitchVariation = 0.08f;
        [SerializeField] private int audioSourcePoolSize = 6;

        [Header("XR Haptics")]
        [SerializeField] private bool enableHaptics = true;
        [Range(0f, 1f)] [SerializeField] private float maxHapticAmplitude = 0.8f;
        [SerializeField] private float hapticDurationSec = 0.05f;

        private PoolPhysicsEventRunner _runner;
        private readonly List<AudioSource> _sourcePool = new List<AudioSource>();
        private int _poolIndex = 0;

        private void Awake()
        {
            _runner = GetComponent<PoolPhysicsEventRunner>();

            // Setup reusable spatial audio source pool
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                var go = new GameObject($"PhysAudioSource_{i}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1.0f; // 3D Audio
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 0.5f;
                src.maxDistance = 15f;
                _sourcePool.Add(src);
            }

            // Auto-load default audio assets if not explicitly assigned
            if (ballBallClip == null) ballBallClip = Resources.Load<AudioClip>("Audio/SFX/ball_ball_collision");
            if (cushionClip == null) cushionClip = Resources.Load<AudioClip>("Audio/SFX/ball_cushion_impact");
            if (jawClip == null) jawClip = Resources.Load<AudioClip>("Audio/SFX/ball_pocket_jaw");
            if (pocketClip == null) pocketClip = Resources.Load<AudioClip>("Audio/SFX/ball_potted");
            if (cueHitClip == null) cueHitClip = Resources.Load<AudioClip>("Audio/SFX/cue_stick_hit");
        }

        private void OnEnable()
        {
            if (_runner != null)
                _runner.SimEvent += OnSimEvent;
        }

        private void OnDisable()
        {
            if (_runner != null)
                _runner.SimEvent -= OnSimEvent;
        }

        private void OnSimEvent(EventSimulator.PoolEventSim ev)
        {
            if (ev.ImpactSpeed < minSpeedThreshold && ev.Kind != EventSimulator.PoolEventKind.Pocket)
                return;

            Vector3 worldPos = transform.TransformPoint(new Vector3(ev.Position.X, ev.Position.Y, ev.Position.Z));
            float normalizedSpeed = Mathf.Clamp01(ev.ImpactSpeed / Mathf.Max(0.1f, maxSpeedReference));

            AudioClip clip = null;
            float volumeScale = 1.0f;

            switch (ev.Kind)
            {
                case EventSimulator.PoolEventKind.BallBall:
                    clip = ballBallClip;
                    volumeScale = Mathf.Lerp(0.2f, 1.0f, normalizedSpeed);
                    // Trigger haptics if cue ball (index 0) is involved
                    if (enableHaptics && (ev.BallIndex == 0 || ev.OtherIndex == 0))
                    {
                        TriggerHapticImpulse(normalizedSpeed);
                    }
                    break;

                case EventSimulator.PoolEventKind.Cushion:
                    clip = cushionClip;
                    volumeScale = Mathf.Lerp(0.15f, 0.85f, normalizedSpeed);
                    break;

                case EventSimulator.PoolEventKind.Jaw:
                    clip = jawClip;
                    volumeScale = Mathf.Lerp(0.25f, 0.95f, normalizedSpeed);
                    break;

                case EventSimulator.PoolEventKind.Pocket:
                    clip = pocketClip;
                    volumeScale = 1.0f;
                    break;
            }

            if (clip != null)
            {
                PlaySpatialClip(clip, worldPos, volumeScale * masterVolume);
            }
        }

        private void PlaySpatialClip(AudioClip clip, Vector3 pos, float volume)
        {
            if (_sourcePool.Count == 0 || clip == null) return;

            var src = _sourcePool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _sourcePool.Count;

            src.transform.position = pos;
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
            src.Play();
        }

        /// <summary>
        /// Sends haptic impulse to both Left and Right XR hand controllers
        /// </summary>
        public void TriggerHapticImpulse(float intensity)
        {
            if (!enableHaptics) return;

            float amp = Mathf.Clamp01(intensity * maxHapticAmplitude);
            SendHapticToNode(XRNode.RightHand, amp, hapticDurationSec);
            SendHapticToNode(XRNode.LeftHand, amp, hapticDurationSec);
        }

        private static void SendHapticToNode(XRNode node, float amplitude, float duration)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid && device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
            {
                device.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }
}
