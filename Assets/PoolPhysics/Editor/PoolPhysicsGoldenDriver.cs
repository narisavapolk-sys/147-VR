using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace PoolPhysics.Unity.EditorTools
{
    /// <summary>
    /// IN-EDITOR GOLDEN PASS — replays the headless golden baseline through the LIVE Unity
    /// runner (PoolPhysicsEventRunner) in Play Mode and diffs final ball states.
    /// This is the missing gate between "headless suite green" and "physics correct in game".
    ///
    /// Why headless numbers should reproduce here: the event model is time-chunk invariant
    /// (props determinism fail=0 proves Step(T)==n*Step(T/n)), so Unity's fixed-dt stepping
    /// must land within 1e-3 m of the recorded finals. If it does NOT, the divergence is in
    /// the ADAPTER/scene (scale, units, transform mapping, ball order), not the sim — which
    /// is exactly what this tool is designed to localize.
    ///
    /// Usage (Play Mode):
    ///   1. Auto-Wire done (see PoolPhysicsAutoWire), scene table + balls present.
    ///   2. Menu: 147VR/Physics/Run In-Editor Golden (play mode)
    ///   3. Pick golden_regression_baseline.json (default: PoolPhysics/Tests/… in repo root).
    ///   4. Report per shot: v/x per ball vs expected (pos 1e-3 m, vel 1e-2 m/s, spin 1 rad/s).
    ///
    /// Destructive-free: mutates profileSO in memory for per-shot values and RESTORES it;
    /// moves scene ball transforms to each shot's rack; at the end releases ownership,
    /// restores nothing in the scene (Play Mode state is discarded on stop).
    /// </summary>
    public static class PoolPhysicsGoldenDriver
    {
        const float PosTol = 1e-3f, VelTol = 1e-2f, SpinTol = 1f;
        const float RestTimeoutSec = 30f;

        [Serializable] class GGolden { public List<GShot> shots = new List<GShot>(); }
        [Serializable] class GShot { public string name; public GTable table; public List<GBall> balls;
                                     public GCue cue; public List<GExp> expected; public int collisionCount;
                                     public float energyError; public GProfile profile; }
        [Serializable] class GTable  { public float length, width, napHeading; }
        [Serializable] class GBall   { public float x, z, r, m; }
        [Serializable] class GCue    { public int index; public float[] dir; public float speed; public float[] tip; public float elev; }
        [Serializable] class GExp    { public float x, z; public bool pocketed; public float vx, vz, wx, wy, wz; public string phase; }
        [Serializable] class GProfile{ public float slidingFriction, rollingFriction, spinDecay, ballBallRestitution,
                                       ballBallFriction, cushionRestitution, cushionFriction, napAnisotropy, napSteer; }

        static GGolden _g; static int _si; static bool _waiting, _sawMotion; static double _deadline;
        static SerializedObject _runnerSO, _profSO; static string _jsonPath;
        static readonly StringBuilder _report = new StringBuilder();

        static PoolPhysicsEventRunner FindRunner()
        {
            var all = Object.FindObjectsByType<PoolPhysicsEventRunner>(FindObjectsSortMode.None);
            var bay4 = all.FirstOrDefault(r => r.name == "Bay4_PhysicsRoot");
            return bay4 != null ? bay4 : (all.Length > 0 ? all[0] : null);
        }

        [MenuItem("147VR/Physics/Run In-Editor Golden (play mode)", false, 3)]
        public static void StartRun()
        {
            if (!Application.isPlaying) { Debug.LogError("[Golden] Enter Play Mode first — this drives the live runner."); return; }
            var runner = FindRunner();
            if (runner == null) { Debug.LogError("[Golden] No PoolPhysicsEventRunner in scene. Run Auto-Wire first (menu 1)."); return; }

            _jsonPath = EditorUtility.OpenFilePanel("Golden baseline",
                Path.Combine(Directory.GetParent(Application.dataPath).FullName, "PoolPhysics", "Tests"),
                "json");
            if (string.IsNullOrEmpty(_jsonPath)) return;

            try { _g = JsonUtility.FromJson<GGolden>(File.ReadAllText(_jsonPath)); }
            catch (Exception e) { Debug.LogError($"[Golden] JSON parse failed: {e.Message}"); return; }
            if (_g?.shots == null || _g.shots.Count == 0) { Debug.LogError("[Golden] no shots in file"); return; }

            _runnerSO = new SerializedObject(runner);
            var prof = _runnerSO.FindProperty("profileSO").objectReferenceValue as CueVRPhysicsProfile;
            if (prof == null) { Debug.LogError("[Golden] runner.profileSO empty — Auto-Wire first."); return; }
            _profSO = new SerializedObject(prof);

            _si = 0; _report.Clear();
            _report.AppendLine($"[Golden] file={Path.GetFileName(_jsonPath)} shots={_g.shots.Count} tol: pos {PosTol} vel {VelTol} spin {SpinTol}");
            EditorApplication.update += Tick;
            StartNextShot();
        }

        static void StartNextShot()
        {
            var runner = FindRunner();
            var shot = _g.shots[_si];

            // 0) geometry sanity: scene table must be the same size as the shot's table
            var spec = _runnerSO.FindProperty("tableSpec").objectReferenceValue as TableSpec;
            if (spec != null && (Mathf.Abs(spec.playfieldSize.x - shot.table.length) > 1e-3f ||
                                 Mathf.Abs(spec.playfieldSize.y - shot.table.width)  > 1e-3f))
                _report.AppendLine($"  !! '{shot.name}': TableSpec {spec.playfieldSize} != golden table ({shot.table.length}x{shot.table.width}) — positions will differ");

            // 1) apply per-shot profile in memory (restored at end)
            ApplyProfile(shot.profile);

            // 2) release if owned, then rack the scene balls to this shot's layout
            SetEnabled(runner, false);
            var balls = BallTransforms(runner);
            if (balls.Count < shot.balls.Count)
                _report.AppendLine($"  !! '{shot.name}': shot needs {shot.balls.Count} balls, scene has {balls.Count} — skipping extras");
            // DEACTIVATED unused balls: runner snapshots activeInHierarchy into Ball.Active,
            // so inactive ones are excluded from physics/AllAtRest exactly like pocketed.
            int placed = 0;
            for (; placed < Math.Min(balls.Count, shot.balls.Count); placed++)
            {
                var b = shot.balls[placed];
                if (!balls[placed].gameObject.activeSelf) balls[placed].gameObject.SetActive(true);
                balls[placed].SetPositionAndRotation(WorldFromTableLocal(runner, b.x, b.z), Quaternion.identity);
            }
            for (int i = placed; i < balls.Count; i++)
                if (balls[i].gameObject.activeSelf) balls[i].gameObject.SetActive(false);

            // 3) force the runner to rebuild from the freshly racked transforms, take
            //    ownership, then fire the cue shot
            Physics.SyncTransforms(); // rack moved Transforms; TakeOwnership snapshots Rigidbody.position
            ForceRebuild(runner);
            SetEnabled(runner, true);
            var tableT = _runnerSO.FindProperty("tableTransform").objectReferenceValue as Transform;
            Vector2 dir = shot.cue.dir != null && shot.cue.dir.Length == 2 ? new Vector2(shot.cue.dir[0], shot.cue.dir[1]) : Vector2.right;
            Vector2 tip = shot.cue.tip != null && shot.cue.tip.Length == 2 ? new Vector2(shot.cue.tip[0], shot.cue.tip[1]) : Vector2.zero;
            Vector3 worldDir = tableT ? tableT.TransformDirection(new Vector3(dir.x, 0, dir.y)) : new Vector3(dir.x, 0, dir.y);
            runner.Shoot(worldDir, shot.cue.speed, tip, shot.cue.elev);
        if (runner.TryGetSimBall(0, out var sp0, out var sv0, out var sa0, out var pk0))
            Debug.Log($"[Golden] diag '{shot.name}': json0=({shot.balls[0].x:F3},{shot.balls[0].z:F3}) sim0=({sp0.x:F3},{sp0.y:F3},{sp0.z:F3}) simV0=({sv0.x:F2},{sv0.z:F2}) root={runner.transform.position} worldCue0={(balls.Count>0?balls[0].position.ToString():"?")}");

            _waiting = true; _sawMotion = false; _deadline = EditorApplication.timeSinceStartup + RestTimeoutSec;
        var c0 = runner as MonoBehaviour;
        if (c0 != null)
        {
            var rso = new SerializedObject(runner);
            var bt = rso.FindProperty("ballTransforms").GetArrayElementAtIndex(0).objectReferenceValue as Transform;
            var sc = bt != null ? bt.GetComponent<SphereCollider>() : null;
            Debug.Log($"[Golden] shot '{shot.name}': cue='{(bt?bt.name:"?")}' colliderR={(sc? sc.radius*bt.lossyScale.x : -1):F5} defaultR={rso.FindProperty("defaultBallRadius").floatValue:F5}");
        }
        }

        static void Tick()
        {
            if (!_waiting) return;
            var runner = FindRunner();
            if (runner == null) { Finish("runner destroyed"); return; }
            if (!runner.AtRest) _sawMotion = true;
            if (runner.AtRest && _sawMotion) { ScoreShot(runner, timedOut: false); return; }
            if (EditorApplication.timeSinceStartup > _deadline) ScoreShot(runner, timedOut: true);
        }

        static void ScoreShot(PoolPhysicsEventRunner runner, bool timedOut)
        {
            _waiting = false;
            var shot = _g.shots[_si];
            int n = Math.Min(shot.expected.Count, BallTransforms(runner).Count);
            int bad = 0; var line = new StringBuilder($"  {(timedOut ? "TIMEOUT" : "shot")} '{shot.name}' (balls {n}): ");
            for (int i = 0; i < n; i++)
            {
                if (!runner.TryGetSimBall(i, out var pos, out var vel, out var ang, out var pocketed))
                { line.Append($"[b{i}: no sim data] "); bad++; continue; }
                var e = shot.expected[i];
                float dpos = new Vector2(pos.x - e.x, pos.z - e.z).magnitude;
                float dvel = new Vector2(vel.x - e.vx, vel.z - e.vz).magnitude;
                float dspin = (ang - new Vector3(e.wx, e.wy, e.wz)).magnitude;
                bool ok = dpos <= PosTol && (pocketed == e.pocketed) &&
                          (timedOut || (dvel <= VelTol && (e.phase != "Rest" || dspin <= SpinTol)));
                if (!ok) { bad++; line.Append($"[b{i} x{dpos:F4}m v{dvel:F3} spin{dspin:F1} pk:{pocketed}e:{e.pocketed}] "); }
            }
            line.Append(bad == 0 ? "PASS" : $"FAIL ({bad} ball(s))");
            _report.AppendLine(line.ToString());
            if (++_si < _g.shots.Count) StartNextShot(); else Finish("all shots done");
        }

        static void Finish(string how)
        {
            EditorApplication.update -= Tick;
            var runner = FindRunner();
            if (runner != null) SetEnabled(runner, false);
            RestoreProfile();
            _report.AppendLine($"[Golden] done ({how}). STOP Play Mode to discard scene changes.");
            Debug.Log(_report.ToString());
            EditorUtility.DisplayDialog("In-Editor Golden", _report.ToString(), "OK");
        }

        // ---- plumbing ----
        static void SetEnabled(PoolPhysicsEventRunner r, bool on)
        {
            // The Enabled property setter performs Take/ReleaseOwnership (snapshot + kinematic flip).
            if (r.Enabled != on) r.Enabled = on;
        }
        static readonly System.Reflection.FieldInfo F_Sim   = typeof(PoolPhysicsEventRunner).GetField("_sim",   System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        static readonly System.Reflection.FieldInfo F_Dirty = typeof(PoolPhysicsEventRunner).GetField("_dirty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        static void ForceRebuild(PoolPhysicsEventRunner r)
        {
            // editor-only test hook: nulling _sim makes TakeOwnership()->Build() re-read the
            // freshly racked transforms (Build otherwise reuses the cached sim).
            F_Sim?.SetValue(r, null);
            if (F_Dirty != null) F_Dirty.SetValue(r, true);
        }
        static List<Transform> BallTransforms(PoolPhysicsEventRunner r)
        {
            var list = new List<Transform>();
            var arr = _runnerSO.FindProperty("ballTransforms");
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue is Transform t) list.Add(t);
            return list;
        }
        static Vector3 WorldFromTableLocal(PoolPhysicsEventRunner r, float lx, float lz)
        {
            var tableT = _runnerSO.FindProperty("tableTransform").objectReferenceValue as Transform;
            var p = new Vector3(lx, 0.028575f, lz);
            return tableT ? tableT.TransformPoint(p) : p;
        }
        static void ApplyProfile(GProfile p)
        {
            if (p == null) return;
            Set("_slidingFriction", p.slidingFriction); Set("slidingFriction", p.slidingFriction);
            Set("rollingFriction", p.rollingFriction); Set("spinDecay", p.spinDecay);
            Set("ballBallRestitution", p.ballBallRestitution); Set("ballBallFriction", p.ballBallFriction);
            Set("cushionRestitution", p.cushionRestitution); Set("cushionFriction", p.cushionFriction);
            Set("napAnisotropy", p.napAnisotropy); Set("napSteer", p.napSteer);
            _profSO.ApplyModifiedPropertiesWithoutUndo();
            static void Set(string prop, float v) { var s = _profSO.FindProperty(prop); if (s != null) s.floatValue = v; }
        }
        static void RestoreProfile()
        {
            _profSO.Update();
            // baseline used CueVR RE defaults; restore from a fresh instance's values
            var tmp = ScriptableObject.CreateInstance<CueVRPhysicsProfile>();
            Copy(tmp, "slidingFriction"); Copy(tmp, "rollingFriction"); Copy(tmp, "spinDecay");
            Copy(tmp, "ballBallRestitution"); Copy(tmp, "ballBallFriction");
            Copy(tmp, "cushionRestitution"); Copy(tmp, "cushionFriction");
            Copy(tmp, "napAnisotropy"); Copy(tmp, "napSteer");
            _profSO.ApplyModifiedPropertiesWithoutUndo();
            UnityEngine.Object.DestroyImmediate(tmp);
            static void Copy(CueVRPhysicsProfile src, string prop)
            {
                var d = _profSO.FindProperty(prop); if (d == null) return;
                var s = new SerializedObject(src).FindProperty(prop); if (s != null) d.floatValue = s.floatValue;
            }
        }
    }
}
