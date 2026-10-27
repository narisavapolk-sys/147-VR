using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using PoolPhysics.Sim;

namespace PoolPhysics.Unity
{
    /// <summary>
    /// Thin MonoBehaviour bridge: owns a PoolSimulator, syncs ball Transforms,
    /// and exposes a Shoot() for the cue input. Sim runs on a fixed accumulator
    /// (deterministic); rendering interpolates. Assign one TableSpec + Profile
    /// and a list of ball Transforms (index 0 = cue ball).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoolPhysicsRunner : MonoBehaviour
    {
        [SerializeField] TableSpec tableSpec;
        [SerializeField] CueVRPhysicsProfile profile;
        [SerializeField] List<Transform> ballTransforms = new List<Transform>();
        [SerializeField] float ballRadius = 0.028575f;

        PoolSimulator _sim;
        readonly List<Vector3> _prev = new List<Vector3>();
        float _alpha;

        public bool AtRest => _sim != null && _sim.AllAtRest;

        void Awake()
        {
            var spec = tableSpec.ToData();
            var prof = profile.ToProfile();
            _sim = new PoolSimulator(spec, prof);
            for (int i = 0; i < ballTransforms.Count; i++)
            {
                var t = ballTransforms[i];
                var b = new Ball {
                    Position = ToSim(t.position),
                    Velocity = Vector3.Zero,
                    AngularVelocity = Vector3.Zero,
                    Radius = ballRadius,
                    Mass = prof.BallMass,
                    Active = true,
                };
                _sim.Balls.Add(b);
                _prev.Add(ToSim(t.position));
            }
            _sim.CueBallIndex = 0;
        }

        void FixedUpdate()
        {
            if (_sim == null) return;
            for (int i = 0; i < _sim.Balls.Count; i++) _prev[i] = _sim.Balls[i].Position;
            _sim.Step(Time.fixedDeltaTime);
            _alpha = 0f;
        }

        void Update()
        {
            if (_sim == null) return;
            _alpha = Mathf.Clamp01(Time.time - Time.fixedTime / Time.fixedDeltaTime);
            for (int i = 0; i < _sim.Balls.Count && i < ballTransforms.Count; i++)
            {
                var b = _sim.Balls[i];
                if (b.Pocketed) { if (ballTransforms[i]) ballTransforms[i].gameObject.SetActive(false); continue; }
                var interp = Vector3.Lerp(_prev[i], b.Position, _alpha);
                if (ballTransforms[i]) ballTransforms[i].position = ToUnity(interp);
            }
        }

        /// <summary>Fire the cue ball. dir/speed in world; tip = (side, follow/draw) in [-1,1].</summary>
        public void Shoot(Vector3 dir, float speed, Vector2 tip, float elevation = 0f)
        {
            if (_sim == null || _sim.Balls.Count == 0) return;
            var cue = _sim.Balls[_sim.CueBallIndex];
            CueStrike.Apply(ref cue, ToSim(dir), speed, tip, _sim.Profile, elevation);
            _sim.Balls[_sim.CueBallIndex] = cue;
        }

        static Vector3 ToSim(Vector3 u) => new Vector3(u.x, u.y, u.z);
        static Vector3 ToUnity(Vector3 s) => new Vector3(s.X, s.Y, s.Z);
    }
}
