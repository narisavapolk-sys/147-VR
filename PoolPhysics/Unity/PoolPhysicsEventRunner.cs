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

        EventSimulator _sim;
        readonly List<Rigidbody> _bodies = new List<Rigidbody>();
        readonly List<Transform> _renderTargets = new List<Transform>();   // parallel to sim.Balls (skips nulls)
        readonly List<SimVector3> _prev = new List<SimVector3>();   // for render interpolation
        readonly List<SimVector3> _curr = new List<SimVector3>();
        bool _dirty, _owns;

        public bool AtRest => _sim == null || _sim.AllAtRest;
        public int CollisionCount => _sim?.CollisionCount ?? 0;

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
                if (sc != null) { float wr = sc.radius * t.lossyScale.x; if (!Mathf.Approximately(wr, defaultBallRadius)) Debug.LogWarning($"[PoolPhysics] '{t.name}' collider r {wr:F4} != default {defaultBallRadius:F4}"); r = wr; }
                var p = ToSim(t.position);
                tmp.Balls.Add(new Ball { Position = p, Radius = r, Mass = defaultBallMass, Active = true, Phase = BallPhase.Rest });
                prevT.Add(p); currT.Add(p); bodies.Add(t.GetComponent<Rigidbody>()); rts.Add(t);
            }
            tmp.CueBallIndex = 0;
            _sim = tmp; _prev.Clear(); _prev.AddRange(prevT); _curr.Clear(); _curr.AddRange(currT);
            _bodies.Clear(); _bodies.AddRange(bodies); _renderTargets.Clear(); _renderTargets.AddRange(rts);
            _dirty = false;
        }

        void TakeOwnership()
        {
            if (_owns) return;
            if (_dirty || _sim == null) Build();
            if (_sim == null) { Debug.LogError("[PoolPhysics] cannot take ownership (no sim)"); return; }
            for (int i = 0; i < _bodies.Count; i++)
            {
                var rb = _bodies[i]; if (rb == null) continue;
                // snapshot PhysX state INTO the sim (velocity/angular are DIRECTIONS, not points), then go kinematic
                var b = _sim.Balls[i];
                b.Position = ToSim(rb.position); b.Velocity = ToSimDir(rb.velocity); b.AngularVelocity = ToSimDir(rb.angularVelocity);
                b.Active = rb.gameObject.activeInHierarchy; _sim.Balls[i] = b;
                rb.isKinematic = true;
            }
            foreach (var d in legacyDrivers) if (d != null) d.enabled = false;
            _sim.BeginShot(); _owns = true; enabled_override = true;
        }

        void ReleaseOwnership()
        {
            if (!_owns) { enabled_override = false; return; }
            for (int i = 0; i < _bodies.Count && _sim != null && i < _sim.Balls.Count; i++)
            {
                var rb = _bodies[i]; if (rb == null) continue;
                var b = _sim.Balls[i];
                rb.isKinematic = false;
                rb.position = ToUnity(b.Position); rb.velocity = ToUnityDir(b.Velocity); rb.angularVelocity = ToUnityDir(b.AngularVelocity);
            }
            foreach (var d in legacyDrivers) if (d != null) d.enabled = true;
            _owns = false; enabled_override = false;
        }

        /// <summary>Fire the cue ball (world dir, m/s, tip=(side,follow/draw)). Only acts when this backend owns physics.</summary>
        public void Shoot(Vector3 worldDir, float speed, Vector2 tip, float elevation = 0f)
        {
            if (!_owns) return;                       // single backend: legacy path handles the shot when we don't own it
            if (_dirty || _sim == null) Build();
            if (_sim == null) return;
            var cue = _sim.Balls[_sim.CueBallIndex];
            CueStrike.Apply(ref cue, ToSimDir(worldDir), speed, new SimVector2(tip.x, tip.y), _sim.Profile, elevation);
            _sim.Balls[_sim.CueBallIndex] = cue;
            _sim.BeginShot();
        }

        void FixedUpdate()
        {
            if (!_owns || _sim == null) return;
            if (_dirty) Build();
            for (int i = 0; i < _sim.Balls.Count && i < _prev.Count; i++) _prev[i] = _curr[i];
            _sim.Step(Time.fixedDeltaTime);           // advance exactly one physics frame; no per-frame energy reset
            for (int i = 0; i < _sim.Balls.Count && i < _curr.Count; i++) _curr[i] = _sim.Balls[i].Position;
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
