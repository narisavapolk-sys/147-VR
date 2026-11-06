using System;
using System.Collections.Generic;
using UnityEngine;
using PoolPhysics.Sim;
using SimVector3 = System.Numerics.Vector3;
using SimVector2 = System.Numerics.Vector2;

namespace PoolPhysics.Unity
{
    /// <summary>
    /// NON-DESTRUCTIVE adapter driving balls with the event-based EventSimulator.
    ///
    /// PHYSICS OWNERSHIP HANDOFF (single backend at a time):
    ///   - Enable(true): snapshot current PhysX state -> set each ball Rigidbody
    ///     kinematic, disable the legacy drivers you assign (e.g. BallMotionController),
    ///     then the simulator owns motion.
    ///   - Enable(false)/OnDisable: hand position/velocity/angular/active/pocket back to
    ///     the Rigidbodies and re-enable legacy drivers.
    ///   - Shoot() applies velocity to exactly ONE backend (the sim when enabled; the
    ///     caller's legacy path must skip when Enabled==true).
    ///
    /// Units: the simulator runs in METRES in the table's LOCAL space; requires
    /// tableTransform scale == 1 (warns otherwise). Origin = local (0,0,0) playfield corner.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1000)]
    public sealed class PoolPhysicsEventRunner : MonoBehaviour
    {
        [SerializeField] bool enabled_override = false;
        public bool Enabled
        {
            get => enabled_override;
            set { if (enabled_override == value) return; if (value) TakeOwnership(); else ReleaseOwnership(); }
        }

        [Tooltip("Table root; simulator runs in its local space (scale must be 1).")]
        [SerializeField] Transform tableTransform;
        [SerializeField] TableSpec tableSpec;
        [SerializeField] CueVRPhysicsProfile profileSO;
        [SerializeField] List<Transform> ballTransforms = new List<Transform>();
        [Tooltip("Legacy components to disable while the simulator owns the balls (e.g. BallMotionController).")]
        [SerializeField] List<MonoBehaviour> legacyDrivers = new List<MonoBehaviour>();
        [SerializeField] float defaultBallRadius = 0.028575f;
        [SerializeField] float defaultBallMass = 0.160f;

        [Header("Events (audio / haptics)")]
        [Tooltip("Enable the sim's event pump. Cost is proportional to event count only.")]
        [SerializeField] bool recordEvents = true;
        [Tooltip("Debug-log every drained sim event.")]
        [SerializeField] bool logEvents = false;

        /// <summary>Sim gameplay events drained after each fixed step — wire audio/haptics here.
        /// Fired on the main thread; subscribers should be cheap (queue, don't block).</summary>
        public event Action<EventSimulator.PoolEventSim> SimEvent;
        /// <summary>Fires before CueStrike applies the shot impulse, while the table is still at rest.
        /// RulesBridge uses this as the authoritative pre-shot snapshot boundary.</summary>
        public event Action ShotStarted;
        /// <summary>Edge-triggered when the table reaches rest after a shot (hand-off point for
        /// scoring/respawn logic).</summary>
        public event Action ShotFinished;
        readonly List<EventSimulator.PoolEventSim> _events = new List<EventSimulator.PoolEventSim>();
        bool _shotActive, _wasAtRest;

        EventSimulator _sim;
        readonly List<Rigidbody> _bodies = new List<Rigidbody>();
        readonly List<Transform> _renderTargets = new List<Transform>();   // parallel to sim.Balls (skips nulls)
        readonly List<SimVector3> _prev = new List<SimVector3>();   // for render interpolation
        readonly List<SimVector3> _curr = new List<SimVector3>();
        bool _dirty, _owns;

        public bool AtRest => _sim == null || _sim.AllAtRest;
        public int CollisionCount => _sim?.CollisionCount ?? 0;
        /// <summary>Number of balls owned by this backend (sim if built, else the wired list).
        /// Used by M5ShotLifecycle to poll sim velocities instead of PhysX.</summary>
        public int BallCount => _sim != null ? _sim.Balls.Count : ballTransforms.Count;
        /// <summary>True while this backend owns motion (balls kinematic, sim stepping).</summary>
        public bool OwnsPhysics => _owns;
        /// <summary>Current cue-ball index after canonical sorting; parallel Unity arrays share this order.</summary>
        public int CueBallIndex => _sim != null ? _sim.CueBallIndex : 0;

        // ---- External read API (Authority Director / Rules Bridge / VR cue). Read-only. ----

        /// <summary>Scene object name of this bay, e.g. "Bay2_PhysicsRoot".</summary>
        public string BayName => name;
        /// <summary>Table root transform; its position is the playfield corner in world space.</summary>
        public Transform TableRootTransform => tableTransform;
        /// <summary>Playfield size in metres (x = width, y = length along world Z).</summary>
        public Vector2 PlayfieldSize => tableSpec != null ? tableSpec.playfieldSize : Vector2.zero;
        /// <summary>Nominal ball radius this backend simulates.</summary>
        public float BallRadius => defaultBallRadius;
        /// <summary>Wired table discipline asset (may be null before wiring).</summary>
        public TableSpec Spec => tableSpec;

        /// <summary>Ball transform at SIM index. After Build() this is the list parallel to
        /// sim.Balls, so simulator index == returned index. Null when out of range.</summary>
        public Transform BallTransformAt(int index)
        {
            if (index < 0) return null;
            if (_sim != null) return index < _renderTargets.Count ? _renderTargets[index] : null;
            return index < ballTransforms.Count ? ballTransforms[index] : null;
        }

        /// <summary>Pocket count from the wired TableSpec (simulator source of truth).</summary>
        public int PocketCount => tableSpec != null ? tableSpec.pockets.Count : 0;

        /// <summary>Pocket centre in WORLD space (spec stores table-local XZ).</summary>
        public Vector3 PocketWorldPosition(int index)
        {
            if (tableSpec == null || index < 0 || index >= tableSpec.pockets.Count) return Vector3.zero;
            TableSpec.PocketEntry p = tableSpec.pockets[index];
            Vector3 local = new Vector3(p.xz.x, 0f, p.xz.y);
            return tableTransform != null ? tableTransform.TransformPoint(local) : local;
        }

        /// <summary>Pocket capture radius from the spec.</summary>
        public float PocketWorldRadius(int index)
        {
            if (tableSpec == null || index < 0 || index >= tableSpec.pockets.Count) return 0f;
            return tableSpec.pockets[index].radius;
        }

        /// <summary>Convert a simulator position (table-local metres) to world space.</summary>
        public Vector3 SimPositionToWorld(Vector3 simLocal) => ToUnity(new SimVector3(simLocal.x, simLocal.y, simLocal.z));

        /// <summary>Cloth surface height (world Y) inferred from the cue ball centre minus its
        /// radius. Falls back to the wired root Y when no ball is available.</summary>
        public float SurfaceTopY
        {
            get
            {
                Transform cue = BallTransformAt(CueBallIndex);
                if (cue != null) return cue.position.y - defaultBallRadius;
                return tableTransform != null ? tableTransform.position.y : 0f;
            }
        }

        void OnEnable() { if (enabled_override) TakeOwnership(); }
        void OnDisable() { if (_owns) ReleaseOwnership(); }

        void Build()
        {
            _sim = null;
            if (tableSpec == null || profileSO == null || ballTransforms.Count == 0) return;
            if (tableTransform != null && !Mathf.Approximately(tableTransform.lossyScale.x, 1f))
                Debug.LogWarning($"[PoolPhysics] tableTransform scale {tableTransform.lossyScale} != 1; sim assumes metres. Fix scale or bake it.");
            var tmp = new EventSimulator(tableSpec.ToData(), profileSO.ToProfile());
            var prevT = new List<SimVector3>(); var currT = new List<SimVector3>(); var bodies = new List<Rigidbody>(); var rts = new List<Transform>();
            for (int i = 0; i < ballTransforms.Count; i++)
            {
                var t = ballTransforms[i]; if (t == null) { Debug.LogError($"[PoolPhysics] ballTransform[{i}] is null (skipped)"); continue; }
                float r = defaultBallRadius;
                var sc = t.GetComponent<SphereCollider>();
                if (sc != null) { float wr = sc.radius * t.lossyScale.x; if (Mathf.Abs(wr - defaultBallRadius) > 0.001f) { Debug.LogWarning($"[PoolPhysics] '{t.name}' collider r {wr:F4} != default {defaultBallRadius:F4}"); r = wr; } else { r = defaultBallRadius; } }
                var p = ToSim(t.position);
                tmp.Balls.Add(new Ball { Position = p, Radius = r, Mass = defaultBallMass, Active = true, Phase = BallPhase.Rest });
                prevT.Add(p); currT.Add(p); bodies.Add(t.GetComponent<Rigidbody>()); rts.Add(t);
            }
            tmp.CueBallIndex = 0;
            tmp.RecordEvents = recordEvents;
            _sim = tmp; _prev.Clear(); _prev.AddRange(prevT); _curr.Clear(); _curr.AddRange(currT);
            _bodies.Clear(); _bodies.AddRange(bodies); _renderTargets.Clear(); _renderTargets.AddRange(rts);
            ApplySimPermutation(_sim.BeginShotWithPermutation());
            _dirty = false;
        }

        void TakeOwnership()
        {
            if (_owns) return;
            if (_dirty || _sim == null) Build();
            if (_sim == null) { Debug.LogError("[PoolPhysics] cannot take ownership (no sim)"); return; }
            _sim.RecordEvents = recordEvents;
            for (int i = 0; i < _bodies.Count; i++)
            {
                var rb = _bodies[i]; if (rb == null) continue;
                // snapshot PhysX state INTO the sim (velocity/angular are DIRECTIONS, not points), then go kinematic
                var b = _sim.Balls[i];
                b.Position = ToSim(rb.position); b.Velocity = ToSimDir(rb.linearVelocity); b.AngularVelocity = ToSimDir(rb.angularVelocity);
                b.Active = rb.gameObject.activeInHierarchy; _sim.Balls[i] = b;
                rb.isKinematic = true;
            }
            foreach (var d in legacyDrivers) if (d != null) d.enabled = false;
            ApplySimPermutation(_sim.BeginShotWithPermutation());
            _owns = true; enabled_override = true;
        }

        /// <summary>Apply EventSimulator's new-index -> old-index permutation to every
        /// Unity-side array that is parallel to sim.Balls. This preserves object identity
        /// after canonical sorting (including cue-ball index remapping).</summary>
        void ApplySimPermutation(int[] newToOld)
        {
            if (newToOld == null || newToOld.Length == 0) return;
            int count = newToOld.Length;
            if (_bodies.Count != count || _renderTargets.Count != count ||
                _prev.Count != count || _curr.Count != count)
            {
                Debug.LogError($"[PoolPhysics] cannot apply ball permutation: sim={count}, bodies={_bodies.Count}, transforms={_renderTargets.Count}, prev={_prev.Count}, curr={_curr.Count}");
                return;
            }

            bool identity = true;
            for (int i = 0; i < count; i++)
                if (newToOld[i] != i) { identity = false; break; }
            if (identity) return;

            var bodies = new Rigidbody[count];
            var targets = new Transform[count];
            var previous = new SimVector3[count];
            var current = new SimVector3[count];
            for (int i = 0; i < count; i++)
            {
                int oldIndex = newToOld[i];
                if (oldIndex < 0 || oldIndex >= count)
                {
                    Debug.LogError($"[PoolPhysics] invalid ball permutation entry {oldIndex} at {i}");
                    return;
                }
                bodies[i] = _bodies[oldIndex];
                targets[i] = _renderTargets[oldIndex];
                previous[i] = _prev[oldIndex];
                current[i] = _curr[oldIndex];
            }

            _bodies.Clear(); _bodies.AddRange(bodies);
            _renderTargets.Clear(); _renderTargets.AddRange(targets);
            _prev.Clear(); _prev.AddRange(previous);
            _curr.Clear(); _curr.AddRange(current);
        }

        void ReleaseOwnership()
        {
            if (!_owns) { enabled_override = false; return; }
            for (int i = 0; i < _bodies.Count && _sim != null && i < _sim.Balls.Count; i++)
            {
                var rb = _bodies[i]; if (rb == null) continue;
                var b = _sim.Balls[i];
                rb.isKinematic = false;
                if (b.Pocketed)
                {
                    // ball fell during sim ownership: hand it back PARKED and HIDDEN with zero
                    // momentum so the legacy path / respawn code sees a consistent pocketed state.
                    rb.position = ToUnity(b.Position);
                    rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
                    if (rb.gameObject.activeSelf) rb.gameObject.SetActive(false);
                }
                else
                {
                    if (!rb.gameObject.activeSelf) rb.gameObject.SetActive(true);
                    rb.position = ToUnity(b.Position); rb.linearVelocity = ToUnityDir(b.Velocity); rb.angularVelocity = ToUnityDir(b.AngularVelocity);
                }
            }
            foreach (var d in legacyDrivers) if (d != null) d.enabled = true;
            _owns = false; enabled_override = false; _shotActive = false;
        }

        /// <summary>Fire the cue ball (world dir, m/s, tip=(side,follow/draw)). Only acts when this backend owns physics.</summary>
        public void Shoot(Vector3 worldDir, float speed, Vector2 tip, float elevation = 0f)
        {
            if (!_owns) return;                       // single backend: legacy path handles the shot when we don't own it
            if (_dirty || _sim == null) Build();
            if (_sim == null) return;
            ApplySimPermutation(_sim.BeginShotWithPermutation());
            ShotStarted?.Invoke();              // snapshot boundary: after canonical reorder, before impulse
            var cue = _sim.Balls[_sim.CueBallIndex];
            CueStrike.Apply(ref cue, ToSimDir(worldDir), speed, new SimVector2(tip.x, tip.y), _sim.Profile, elevation);
            _sim.Balls[_sim.CueBallIndex] = cue;
            _sim.ResetShotTelemetry();            // energy baseline includes the cue impulse, as before
            _shotActive = true; _wasAtRest = false;
        }

        void FixedUpdate()
        {
            if (!_owns || _sim == null) return;
            if (_dirty) Build();
            for (int i = 0; i < _sim.Balls.Count && i < _prev.Count; i++) _prev[i] = _curr[i];
            _sim.Step(Time.fixedDeltaTime);           // advance exactly one physics frame; no per-frame energy reset
            for (int i = 0; i < _sim.Balls.Count && i < _curr.Count; i++) _curr[i] = _sim.Balls[i].Position;

            // drain the event pump and surface gameplay events (audio/haptics/scoring)
            if (recordEvents && _sim.EventLog.Count > 0)
            {
                _events.Clear(); _sim.DrainEvents(_events);
                for (int i = 0; i < _events.Count; i++)
                {
                    var ev = _events[i];
                    if (logEvents) Debug.Log($"[PoolPhysics] sim {ev.Kind}: ball {ev.BallIndex}{(ev.OtherIndex >= 0 ? $" vs {ev.OtherIndex}" : "")} speed {ev.ImpactSpeed:F2} m/s");
                    SimEvent?.Invoke(ev);
                }
            }
            bool rest = _sim.AllAtRest;
            if (_shotActive && rest && !_wasAtRest)
            {
                _shotActive = false;
                ShotFinished?.Invoke();
            }
            _wasAtRest = rest;
        }

        /// <summary>Read one ball's simulated state in TABLE-LOCAL space (for editor tests/AI).
        /// pos = metres from the playfield corner; vel/ang in local space.</summary>
        public bool TryGetSimBall(int index, out Vector3 pos, out Vector3 vel, out Vector3 ang, out bool pocketed)
        {
            pos = vel = ang = default; pocketed = false;
            if (_sim == null || index < 0 || index >= _sim.Balls.Count) return false;
            var b = _sim.Balls[index];
            pos = new Vector3(b.Position.X, b.Position.Y, b.Position.Z);
            vel = new Vector3(b.Velocity.X, b.Velocity.Y, b.Velocity.Z);
            ang = new Vector3(b.AngularVelocity.X, b.AngularVelocity.Y, b.AngularVelocity.Z);
            pocketed = b.Pocketed;
            return true;
        }

        /// <summary>Put a ball back in play at a world position (re-spot) and clear its pocketed
        /// state. Only valid while this backend owns physics — the rules code teleports the
        /// transform, which the simulator would otherwise ignore. Interpolation is re-seeded so
        /// the restored ball does not streak from its pocket.</summary>
        public bool RestoreBall(int index, Vector3 worldPosition)
        {
            if (!_owns || _sim == null || index < 0 || index >= _sim.Balls.Count) return false;
            var b = _sim.Balls[index];
            b.Position = ToSim(worldPosition);
            b.Velocity = SimVector3.Zero;
            b.AngularVelocity = SimVector3.Zero;
            b.Active = true;
            b.Pocketed = false;
            b.Phase = BallPhase.Rest;
            _sim.Balls[index] = b;
            if (index < _prev.Count) _prev[index] = b.Position;
            if (index < _curr.Count) _curr[index] = b.Position;
            var tr = index < _renderTargets.Count ? _renderTargets[index] : null;
            if (tr != null)
            {
                if (!tr.gameObject.activeSelf) tr.gameObject.SetActive(true);
                tr.position = worldPosition;
            }
            _dirty = false;
            return true;
        }

        /// <summary>Mark a ball pocketed (hidden, zero motion) when an external authority — the
        /// rules/scoring layer — considers it gone but the simulator still has it on the table.</summary>
        public bool ParkBall(int index)
        {
            if (!_owns || _sim == null || index < 0 || index >= _sim.Balls.Count) return false;
            var b = _sim.Balls[index];
            b.Velocity = SimVector3.Zero;
            b.AngularVelocity = SimVector3.Zero;
            b.Pocketed = true;
            b.Phase = BallPhase.Rest;
            _sim.Balls[index] = b;
            var tr = index < _renderTargets.Count ? _renderTargets[index] : null;
            if (tr != null && tr.gameObject.activeSelf) tr.gameObject.SetActive(false);
            return true;
        }

        /// <summary>True when the simulator considers this ball pocketed (in-play = false).</summary>
        public bool IsBallPocketed(int index)
        {
            if (_sim == null || index < 0 || index >= _sim.Balls.Count) return false;
            return _sim.Balls[index].Pocketed;
        }

        void Update()
        {
            if (!_owns || _sim == null) return;
            float a = Time.deltaTime > 0 ? Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime) : 1f;
            for (int i = 0; i < _sim.Balls.Count && i < _renderTargets.Count; i++)
            {
                var b = _sim.Balls[i]; var tr = _renderTargets[i]; if (tr == null) continue;
                if (b.Pocketed) { tr.gameObject.SetActive(false); continue; }
                var lerp = SimVector3.Lerp(_prev[i], _curr[i], a);
                tr.position = ToUnity(lerp);
                float spin = b.AngularVelocity.Length();
                if (spin > 1e-3f) tr.Rotate(ToUnityDir(b.AngularVelocity), spin * Time.deltaTime * Mathf.Rad2Deg, Space.World);
            }
        }

        SimVector3 ToSim(Vector3 w)  { var v = tableTransform ? tableTransform.InverseTransformPoint(w) : w; return new SimVector3(v.x, v.y, v.z); }
        Vector3    ToUnity(SimVector3 l){ var v = tableTransform ? tableTransform.TransformPoint(new Vector3(l.X, l.Y, l.Z)) : new Vector3(l.X, l.Y, l.Z); return v; }
        SimVector3 ToSimDir(Vector3 w)  { var v = tableTransform ? tableTransform.InverseTransformDirection(w) : w; return new SimVector3(v.x, v.y, v.z); }
        Vector3    ToUnityDir(SimVector3 l){ return tableTransform ? tableTransform.TransformDirection(new Vector3(l.X, l.Y, l.Z)) : new Vector3(l.X, l.Y, l.Z); }

#if UNITY_EDITOR
        void OnValidate() { if (Application.isPlaying) _dirty = true; }
#endif
    }
}
