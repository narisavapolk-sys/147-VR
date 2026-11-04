using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;
using PoolPhysics.Unity;

namespace VR147.Authority
{
    /// <summary>
    /// Phase 1 — PHYSICS AUTHORITY HANDOFF.
    ///
    /// Pass C wires one <see cref="PoolPhysicsEventRunner"/> per bay (Bay1..Bay4_PhysicsRoot)
    /// but leaves every one of them with enabled_override = false, i.e. PhysX still owns the
    /// balls. This component is the thing that takes ownership away from PhysX — for exactly
    /// ONE bay at a time, the bay the player is standing at, so the Quest headset only pays
    /// for one live event simulator instead of four.
    ///
    /// Rules this class lives by:
    ///   * Runtime only. It never writes to the saved scene; its GameObject is created by the
    ///     bootstrap below, so it works even if nobody adds anything in the Editor.
    ///   * Never hand off in the middle of a shot — the outgoing bay must be AtRest.
    ///   * Sticky claim with hysteresis: walking between two tables does not thrash ownership.
    ///   * A released bay goes back to normal PhysX (balls non-kinematic, position/velocity
    ///     handed back by PoolPhysicsEventRunner.ReleaseOwnership), so nothing is left stranded.
    ///
    /// If a director already exists in the scene, the bootstrap reuses it instead of adding one.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-600)]
    public sealed class PoolPhysicsAuthorityDirector : MonoBehaviour
    {
        public static PoolPhysicsAuthorityDirector Instance { get; private set; }

        [Header("Claim")]
        [Tooltip("A player standing this close (XZ, metres) to a table centre can claim it.")]
        [SerializeField] float claimRadius = 3.2f;
        [Tooltip("How much closer a candidate bay must be to steal an existing claim.")]
        [SerializeField] float releaseHysteresis = 0.75f;
        [Tooltip("-1 = claim the bay nearest the player. 0..3 = force a starting bay (Bay1..Bay4 order).")]
        [SerializeField] int preferredBayIndex = -1;
        [Tooltip("Claim from the HMD pose (falls back to Camera.main in Editor play mode).")]
        [SerializeField] bool autoClaimByHeadsetPose = true;

        [Header("Diagnostics")]
        [SerializeField] bool logHandoffs = true;
        [Tooltip("Lock authority to one bay by name (e.g. 'Bay4_PhysicsRoot'). Empty = follow the player.")]
        [SerializeField] string bayPinName = "";

        readonly List<PoolPhysicsEventRunner> _bays = new List<PoolPhysicsEventRunner>();
        readonly List<InputDevice> _devices = new List<InputDevice>();
        PoolPhysicsEventRunner _active;
        string _runtimePin;
        float _nextScan;
        int _handoffCount;

        public PoolPhysicsEventRunner Active => _active;
        public IReadOnlyList<PoolPhysicsEventRunner> Bays => _bays;
        public int HandoffCount => _handoffCount;

        /// <summary>Fires after a bay has taken ownership (the old one is already released).</summary>
        public event Action<PoolPhysicsEventRunner> ActiveChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null)
                return;

            PoolPhysicsAuthorityDirector existing = FindFirstObjectByType<PoolPhysicsAuthorityDirector>();
            if (existing != null)
            {
                existing.Announce();
                if (existing.GetComponent<PoolPhysicsRulesBridge>() == null)
                    existing.gameObject.AddComponent<PoolPhysicsRulesBridge>();
                return;
            }

            GameObject go = new GameObject("PoolPhysics Authority Director");
            go.AddComponent<PoolPhysicsAuthorityDirector>();
            go.AddComponent<PoolPhysicsRulesBridge>();   // Phase 3 rides on the same host object
        }

        void Awake()
        {
            Instance = this;
            if (FindFirstObjectByType<PoolPhysicsRulesBridge>() == null)
                gameObject.AddComponent<PoolPhysicsRulesBridge>();
        }

        void OnDestroy()
        {
            if (_active != null)
                Release(_active);
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            Collect();
        }

        void Announce()
        {
            Instance = this;
        }

        // ---- Discovery ----

        void Collect()
        {
            _bays.Clear();
            foreach (PoolPhysicsEventRunner r in FindObjectsByType<PoolPhysicsEventRunner>())
                if (r != null)
                    _bays.Add(r);
            _bays.Sort((a, b) => string.CompareOrdinal(a.BayName, b.BayName));

            if (_bays.Count == 0)
            {
                Debug.LogWarning("[Authority] No PoolPhysicsEventRunner found — staying out of the way. " +
                                 "Run '147VR/Physics/Scene Swap Pass C' to wire the bays.");
                return;
            }

            // Everything starts in PhysX hands; only the claimed bay is taken back below.
            for (int i = 0; i < _bays.Count; i++)
                if (_bays[i].OwnsPhysics)
                    Release(_bays[i]);

            string pin = ResolvePinName();
            if (!string.IsNullOrWhiteSpace(pin))
            {
                PoolPhysicsEventRunner pinned = FindBay(pin);
                if (pinned != null)
                {
                    Claim(pinned);
                    Debug.Log($"[Authority] pinned to {pinned.BayName} — auto-claim is disabled while pinned.");
                    return;
                }
                Debug.LogWarning($"[Authority] pin '{pin}' matches no bay; falling back to pose auto-claim.");
            }

            int preferred = preferredBayIndex;
            if (preferred >= 0 && preferred < _bays.Count)
                Claim(_bays[preferred]);
            else if (TryGetHeadPose(out Vector3 head))
                Claim(NearestBay(head, claimRadius));
            else
                Claim(_bays[0]);

            Debug.Log($"[Authority] {(_active != null ? _active.BayName : "NONE")} holds physics authority " +
                      $"({_bays.Count} bays discovered, claim radius {claimRadius:F2} m).");
        }

        /// <summary>Pin order: Inspector value, then a runtime override set through Pin(), then
        /// Logs/authority_pin.txt (lets editor tests/harnesses lock a bay without touching the scene).</summary>
        string ResolvePinName()
        {
            if (!string.IsNullOrWhiteSpace(_runtimePin))
                return _runtimePin;
            if (!string.IsNullOrWhiteSpace(bayPinName))
                return bayPinName;
            try
            {
                const string file = "Logs/authority_pin.txt";
                if (File.Exists(file))
                {
                    string text = File.ReadAllText(file).Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Authority] could not read {ReportFile}: {e.Message}");
            }
            return null;
        }

        const string ReportFile = "Logs/authority_pin.txt";

        /// <summary>Locks authority to one bay (by scene object name, e.g. "Bay2_PhysicsRoot").</summary>
        public bool Pin(string bayName)
        {
            _runtimePin = bayName;
            PoolPhysicsEventRunner target = FindBay(bayName);
            if (target == null)
            {
                Debug.LogWarning($"[Authority] Pin('{bayName}'): unknown bay.");
                return false;
            }
            return Claim(target);
        }

        /// <summary>Clears the pin and lets authority follow the player again.</summary>
        public void Unpin()
        {
            _runtimePin = null;
            bayPinName = null;
        }

        public string PinnedBay => ResolvePinName();

        PoolPhysicsEventRunner FindBay(string bayName)
        {
            if (string.IsNullOrWhiteSpace(bayName))
                return null;
            for (int i = 0; i < _bays.Count; i++)
            {
                string n = _bays[i].BayName;
                if (string.Equals(n, bayName, StringComparison.OrdinalIgnoreCase) ||
                    n.StartsWith(bayName, StringComparison.OrdinalIgnoreCase))
                    return _bays[i];
            }
            return null;
        }

        // ---- Claim / release ----

        void Update()
        {
            if (!autoClaimByHeadsetPose || _bays.Count == 0)
                return;
            if (Time.unscaledTime < _nextScan)
                return;
            _nextScan = Time.unscaledTime + 0.25f;

            // Never hand a table over while its simulator is still moving balls.
            if (_active != null && !_active.AtRest)
                return;
            if (!TryGetHeadPose(out Vector3 head))
                return;

            PoolPhysicsEventRunner candidate = NearestBay(head, claimRadius);
            if (candidate == null || candidate == _active)
                return;

            if (_active != null)
            {
                float currentDistance = XzDistance(head, TableCenter(_active));
                float candidateDistance = XzDistance(head, TableCenter(candidate));
                if (currentDistance <= claimRadius && candidateDistance + releaseHysteresis >= currentDistance)
                    return;   // sticky: keep the bay the player already owns
            }

            Claim(candidate);
        }

        /// <summary>Takes physics authority for <paramref name="target"/>, releasing the previous bay.</summary>
        public bool Claim(PoolPhysicsEventRunner target)
        {
            if (target == null)
                return false;
            if (_active == target)
                return true;

            PoolPhysicsEventRunner previous = _active;
            _active = null;
            if (previous != null)
                Release(previous);

            // Single-backend invariant: anything another tool switched on (the in-editor Golden
            // driver enables a bay itself) must be handed back to PhysX before we take over.
            for (int i = 0; i < _bays.Count; i++)
            {
                PoolPhysicsEventRunner other = _bays[i];
                if (other == null || other == target || !other.OwnsPhysics)
                    continue;
                Debug.Log($"[Authority] releasing {other.BayName}: it was holding authority while {target.BayName} is claimed " +
                          "(a second backend would mean dual physics authority)");
                other.Enabled = false;
            }

            target.Enabled = true;
            if (!target.OwnsPhysics)
            {
                Debug.LogError($"[Authority] {target.BayName} refused ownership (TableSpec/profile/balls missing?). " +
                               "Re-run Pass C wiring; PhysX stays authority.");
                return false;
            }

            _active = target;
            _handoffCount++;
            if (logHandoffs)
                Debug.Log($"[Authority] handoff #{_handoffCount}: PhysX → {target.BayName} " +
                          $"(balls={target.BallCount}, pockets={target.PocketCount}, r={target.BallRadius:F4} m)");
            ActiveChanged?.Invoke(target);
            return true;
        }

        public bool ClaimIndex(int index) => index >= 0 && index < _bays.Count && Claim(_bays[index]);

        /// <summary>Hands a bay back to PhysX (safe to call on a bay that does not own physics).</summary>
        public void Release(PoolPhysicsEventRunner runner)
        {
            if (runner == null)
                return;
            if (runner.OwnsPhysics && logHandoffs)
                Debug.Log($"[Authority] handoff: {runner.BayName} → PhysX (balls non-kinematic, state handed back)");
            runner.Enabled = false;
            if (_active == runner)
                _active = null;
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < _bays.Count; i++)
                Release(_bays[i]);
        }

        PoolPhysicsEventRunner NearestBay(Vector3 worldPos, float maxDistance)
        {
            PoolPhysicsEventRunner best = null;
            float bestDistance = maxDistance;
            for (int i = 0; i < _bays.Count; i++)
            {
                PoolPhysicsEventRunner r = _bays[i];
                if (r == null || r.TableRootTransform == null)
                    continue;
                float d = XzDistance(worldPos, TableCenter(r));
                if (d <= bestDistance)
                {
                    bestDistance = d;
                    best = r;
                }
            }
            return best;
        }

        // ---- Geometry helpers (used by the VR cue bridge for aiming) ----

        /// <summary>World-space centre of the bay playfield (XZ) at the bay root height.</summary>
        public static Vector3 TableCenter(PoolPhysicsEventRunner r)
        {
            Transform root = r.TableRootTransform;
            Vector2 size = r.PlayfieldSize;
            if (root == null)
                return Vector3.zero;
            if (size.x <= 0f || size.y <= 0f)
                return root.position;
            return root.position + new Vector3(size.x * 0.5f, 0f, size.y * 0.5f);
        }

        /// <summary>Cloth height + playable XZ bounds of the bay the player is standing at.
        /// Returns false when no bay owns physics, so callers can fall back to their old path.
        /// The bounds are the union of the authored playfield rect and the real ball spread,
        /// which keeps aiming valid when a TableSpec was generated from renderer bounds.</summary>
        public bool TryGetActiveTable(out float surfaceTopY, out Bounds playfield)
        {
            surfaceTopY = 0f;
            playfield = default;
            PoolPhysicsEventRunner r = _active;
            if (r == null || r.TableRootTransform == null)
                return false;

            Vector2 size = r.PlayfieldSize;
            if (size.x <= 0f || size.y <= 0f)
                return false;

            surfaceTopY = r.SurfaceTopY;
            Vector3 root = r.TableRootTransform.position;
            Vector3 min = new Vector3(root.x, surfaceTopY, root.z);
            Vector3 max = new Vector3(root.x + size.x, surfaceTopY, root.z + size.y);

            float pad = r.BallRadius;
            for (int i = 0; i < r.BallCount; i++)
            {
                Transform t = r.BallTransformAt(i);
                if (t == null)
                    continue;
                Vector3 p = t.position;
                min = Vector3.Min(min, new Vector3(p.x - pad, surfaceTopY, p.z - pad));
                max = Vector3.Max(max, new Vector3(p.x + pad, surfaceTopY, p.z + pad));
            }

            Vector3 centre = (min + max) * 0.5f;
            Vector3 extent = max - min;
            extent.y = 0.1f;
            playfield = new Bounds(centre, extent);
            return true;
        }

        bool TryGetHeadPose(out Vector3 position)
        {
            _devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, _devices);
            for (int i = 0; i < _devices.Count; i++)
            {
                InputDevice device = _devices[i];
                if (device.isValid && device.TryGetFeatureValue(CommonUsages.devicePosition, out position))
                    return true;
            }

            Camera main = Camera.main;
            if (main != null)
            {
                position = main.transform.position;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        static float XzDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void OnDisable()
        {
            if (_active != null)
                Release(_active);
        }
    }
}
