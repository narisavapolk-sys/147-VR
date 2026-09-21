using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class M5FinalReal10MainSceneRunner
{
    private const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
    private const int ShotCount = 10;
    private const float ShotPower = 0.35f;
    private static int _resolved;
    private static int _fired;
    private static int _settled;
    private static int _errors;
    private static double _nextAction;
    private static bool _started;
    private static SnookerShotTracker _tracker;
    private static SnookerBallTracker _ballTracker;
    private static M5ShotLifecycle _lifecycle;
    private static SnookerPhysicsSetup _physics;
    private static SnookerCueController _cue;
    private static string _logPath;

    private const string SessionActiveKey = "M5_FINAL10_ACTIVE";
    private const string SessionFiredKey = "M5_FINAL10_FIRED";
    private const string SessionResolvedKey = "M5_FINAL10_RESOLVED";
    private const string SessionSettledKey = "M5_FINAL10_SETTLED";
    private const string SessionErrorsKey = "M5_FINAL10_ERRORS";

    static M5FinalReal10MainSceneRunner()
    {
        _logPath = Path.GetFullPath("Docs/M5_REAL_10SHOT_FINAL_20260920.json");
        if (SessionState.GetBool(SessionActiveKey, false))
        {
            _fired = SessionState.GetInt(SessionFiredKey, 0);
            _resolved = SessionState.GetInt(SessionResolvedKey, 0);
            _settled = SessionState.GetInt(SessionSettledKey, 0);
            _errors = SessionState.GetInt(SessionErrorsKey, 0);
            _started = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
    }

    private static void SaveSession()
    {
        SessionState.SetBool(SessionActiveKey, true);
        SessionState.SetInt(SessionFiredKey, _fired);
        SessionState.SetInt(SessionResolvedKey, _resolved);
        SessionState.SetInt(SessionSettledKey, _settled);
        SessionState.SetInt(SessionErrorsKey, _errors);
    }

    public static void Run()
    {
        _resolved = _fired = _settled = _errors = 0;
        _started = false;
        SaveSession();
        _logPath = Path.GetFullPath("Docs/M5_REAL_10SHOT_FINAL_20260920.json");
        File.WriteAllText(_logPath, "{\n  \"status\": \"RUNNING\",\n  \"scene\": \"147VR_MainScene.unity\",\n  \"shots\": []\n}\n");

        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.delayCall += RequestPlayMode;
    }

    private static void RequestPlayMode()
    {
        EditorApplication.delayCall -= RequestPlayMode;
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            if (_started)
                Finish("PLAYMODE_EXITED");
            return;
        }

        if (!_started)
        {
            _cue = UnityEngine.Object.FindFirstObjectByType<SnookerCueController>(FindObjectsInactive.Include);
            _tracker = UnityEngine.Object.FindFirstObjectByType<SnookerShotTracker>(FindObjectsInactive.Include);
            _ballTracker = UnityEngine.Object.FindFirstObjectByType<SnookerBallTracker>(FindObjectsInactive.Include);
            _lifecycle = UnityEngine.Object.FindFirstObjectByType<M5ShotLifecycle>(FindObjectsInactive.Include);
            _physics = UnityEngine.Object.FindFirstObjectByType<SnookerPhysicsSetup>(FindObjectsInactive.Include);
            if (_cue == null || _tracker == null || _ballTracker == null || _lifecycle == null || _physics == null)
            {
                if (EditorApplication.timeSinceStartup < 15.0)
                    return;
                _errors++;
                Finish("RUNTIME_COMPONENT_MISSING");
                return;
            }

            if (!_cue.gameObject.activeInHierarchy)
            {
                _cue.gameObject.SetActive(true);
                Debug.Log("[M5 REAL10] Activated inactive Quest Setup root for runtime certification");
            }

            // Final10 must enter the same production runtime physics path before the first shot.
            // White_CueBall is a visual prefab object; SnookerPhysicsSetup creates its Rigidbody
            // and SphereCollider at runtime. Do not let the editor runner race that initialization.
            _physics.EnsurePhysics();
            _ballTracker.RefreshIfNeeded();
            Debug.Log($"[M5 REAL10] Physics prepared: cue={_ballTracker.FindBall("White_CueBall") != null} balls={_ballTracker.BallsCount()}");

            _tracker.M5ShotResolved += OnResolved;
            _lifecycle.ShotSettled += OnSettled;
            _started = true;
            SaveSession();
            Debug.Log("[M5 REAL10] Main Scene runtime components found");
            _nextAction = EditorApplication.timeSinceStartup + 2.0;
        }

        if (_fired >= ShotCount)
        {
            if (_resolved >= ShotCount && _settled >= ShotCount)
                Finish(_errors == 0 ? "PASS" : "FAIL_ERRORS");
            else if (EditorApplication.timeSinceStartup > _nextAction + 60.0)
            {
                _errors++;
                Finish("RESOLVE_TIMEOUT");
            }
            return;
        }

        if (_fired == _resolved && EditorApplication.timeSinceStartup >= _nextAction)
        {
            Vector3 aim;
            if (!TryGetAimPoint(out aim))
            {
                _errors++;
                Finish("AIM_TARGET_MISSING");
                return;
            }

            int shotNo = _fired + 1;
            Debug.Log($"[M5 REAL10] FIRE {shotNo}/{ShotCount} aim=({aim.x:F3},{aim.y:F3},{aim.z:F3}) power={ShotPower:F2}");
            int seqBefore = _lifecycle.ShotSequence;
            _cue.ShootAt(aim, ShotPower);
            if (_lifecycle.ShotSequence <= seqBefore || !_tracker.ShotInProgress)
            {
                _errors++;
                Debug.LogError($"[M5 REAL10] SHOT_REJECTED {shotNo}/{ShotCount} seqBefore={seqBefore} seqAfter={_lifecycle.ShotSequence}");
                Finish("SHOT_REJECTED");
                return;
            }
            _fired++;
            SaveSession();
            _nextAction = EditorApplication.timeSinceStartup + 0.25;
        }
    }

    private static bool TryGetAimPoint(out Vector3 aim)
    {
        aim = Vector3.zero;
        if (_tracker == null || _ballTracker == null || _cue == null)
            return false;

        var balls = _ballTracker.Balls;
        Vector3 cuePos = Vector3.zero;
        bool foundCue = false;
        float best = float.MaxValue;
        SnookerBallTracker.BallInfo bestBall = null;

        foreach (var b in balls)
        {
            if (b == null || b.transform == null || b.potted)
                continue;
            if (b.points == 0)
            {
                cuePos = b.transform.position;
                foundCue = true;
                continue;
            }
            float d = (b.transform.position - cuePos).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestBall = b;
            }
        }

        if (!foundCue || bestBall == null)
            return false;

        aim = bestBall.transform.position;
        aim.y = cuePos.y;
        return true;
    }

    private static void OnSettled()
    {
        _settled++;
        SaveSession();
        Debug.Log($"[M5 REAL10] SETTLED {_settled}/{ShotCount} seq={_lifecycle.ShotSequence}");
    }

    private static void OnResolved(int seq)
    {
        _resolved++;
        SaveSession();
        Debug.Log($"[M5 REAL10] RESOLVED {_resolved}/{ShotCount} seq={seq}");
        _nextAction = EditorApplication.timeSinceStartup + 0.4;
    }

    private static void Finish(string status)
    {
        EditorApplication.update -= Tick;
        EditorApplication.delayCall -= RequestPlayMode;
        SessionState.SetBool(SessionActiveKey, false);
        if (_tracker != null) _tracker.M5ShotResolved -= OnResolved;
        if (_lifecycle != null) _lifecycle.ShotSettled -= OnSettled;

        string json = "{\n"
            + $"  \"status\": \"{status}\",\n"
            + "  \"scene\": \"147VR_MainScene.unity\",\n"
            + $"  \"fired\": {_fired},\n"
            + $"  \"resolved\": {_resolved},\n"
            + $"  \"settled\": {_settled},\n"
            + $"  \"errors\": {_errors}\n"
            + "}\n";
        File.WriteAllText(_logPath, json);
        Debug.Log($"[M5 REAL10] COMPLETE status={status} fired={_fired} resolved={_resolved} settled={_settled} errors={_errors}");
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(status == "PASS" ? 0 : 1);
    }
}

