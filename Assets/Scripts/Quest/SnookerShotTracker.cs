using System;
using System.Collections.Generic;
using UnityEngine;
using M5Rules;

/// <summary>M5 runtime authority: collects observations, evaluates once, then commits once.</summary>
public sealed class SnookerShotTracker : MonoBehaviour
{
    private sealed class PendingPot { public SnookerBallTracker.BallInfo Ball; public SnookerBallTracker.PocketInfo Pocket; }
    private readonly List<PendingPot> _pendingPots = new List<PendingPot>();
    private readonly HashSet<int> _pendingIds = new HashSet<int>();

    public event Action<SnookerBallTracker.BallInfo, bool, bool> ShotResolved;
    public event Action<int> M5ShotResolved;

    [Header("Dependencies")]
    public SnookerBallTracker ballTracker;
    public SnookerScoreManager scoreManager;
    public SnookerTurnManager turnManager;
    public M5ShotLifecycle shotLifecycle;
    public M5ShotEventContract eventContract;

    [Header("Off-table detection")]
    public float offTableY = 0.3f;

    private bool _shotInProgress;
    private SnookerBallTracker.BallInfo _cueBall;
    private SnookerBallTracker.BallInfo _firstBallHit;
    private bool _hitAnyBall;
    private int _lastResolvedSequence;
    private M5ShotStartState _shotStart;
    private M5ShotObservationBuilder _builder;
    private M5ShotTransactionApplier _applier;
    private bool? _forcedCueOnTable;
    private bool _lastSettledCueOnTable;

    private void Awake()
    {
        if (ballTracker == null) ballTracker = GetComponent<SnookerBallTracker>();
        if (scoreManager == null) scoreManager = GetComponent<SnookerScoreManager>();
        if (turnManager == null) turnManager = GetComponent<SnookerTurnManager>();
        if (shotLifecycle == null) shotLifecycle = GetComponent<M5ShotLifecycle>();
        if (shotLifecycle == null) shotLifecycle = FindObjectOfType<M5ShotLifecycle>();
        if (eventContract == null) eventContract = GetComponent<M5ShotEventContract>();
        if (eventContract == null) eventContract = FindObjectOfType<M5ShotEventContract>();
        if (ballTracker != null) ballTracker.RefreshIfNeeded();
        RebuildIntegration();
        InitializeBindings();
    }

    private void OnDestroy() { UnbindEvents(); }

    private void RebuildIntegration()
    {
        if (ballTracker == null || scoreManager == null || turnManager == null) return;
        _builder = new M5ShotObservationBuilder(ballTracker);
        _applier = new M5ShotTransactionApplier(scoreManager, ballTracker, turnManager);
    }

    public void InitializeBindings()
    {
        UnbindEvents();
        if (eventContract != null)
        {
            eventContract.InitializeBindings();
            eventContract.PhysicsSettled += OnPhysicsSettledFromContract;
        }
        else if (shotLifecycle != null) shotLifecycle.ShotSettled += OnPhysicsSettled;
        if (ballTracker != null) ballTracker.BallPotted += OnBallPotted;
    }

    private void UnbindEvents()
    {
        if (eventContract != null) eventContract.PhysicsSettled -= OnPhysicsSettledFromContract;
        if (shotLifecycle != null) shotLifecycle.ShotSettled -= OnPhysicsSettled;
        if (ballTracker != null) ballTracker.BallPotted -= OnBallPotted;
    }
    public void OnShotFired()
    {
        if (shotLifecycle == null || scoreManager == null || turnManager == null || ballTracker == null)
            throw new InvalidOperationException("M5 shot dependencies are incomplete.");
        ballTracker.RefreshIfNeeded();
        RebuildIntegration();
        int sequence = shotLifecycle.ShotSequence;
        if (sequence <= _lastResolvedSequence) throw new InvalidOperationException("Stale shot sequence.");
        _shotStart = _builder.CaptureStartState(sequence, scoreManager, turnManager);
        _shotInProgress = true;
        _firstBallHit = null;
        _hitAnyBall = false;
        _pendingPots.Clear();
        _pendingIds.Clear();
        ResolveCueBall();
        Debug.Log($"[M5 ShotTracker] StartState seq={sequence} striker={_shotStart.Striker} ballOn={_shotStart.BallOn} reds={_shotStart.RedsRemaining} nomination={_shotStart.NominatedColour}");
    }

    private void OnBallPotted(SnookerBallTracker.BallInfo ball, SnookerBallTracker.PocketInfo pocket)
    {
        if (!_shotInProgress || ball == null || pocket == null || _builder == null) return;
        int id = _builder.StableBallId(ball);
        if (!_pendingIds.Add(id)) return;
        _pendingPots.Add(new PendingPot { Ball = ball, Pocket = pocket });
    }

    public void OnCueBallHitBall(SnookerBallTracker.BallInfo hitBall)
    {
        if (!_shotInProgress) return;
        _hitAnyBall = true;
        if (_firstBallHit == null) _firstBallHit = hitBall;
    }

    // M5ShotLifecycle owns settling. There is intentionally no second rest poll here.
    private void Update() { if (!_shotInProgress || shotLifecycle != null) return; }

    private void OnPhysicsSettledFromContract(int sequence)
    {
        if (sequence <= 0 || sequence <= _lastResolvedSequence)
        {
            Debug.Log($"[M5 ShotTracker] Ignore stale settle seq={sequence} last={_lastResolvedSequence}");
            return;
        }
        if (!_shotInProgress || _shotStart == null || sequence != _shotStart.ShotSequence)
        {
            Debug.LogWarning($"[M5 ShotTracker] Reject settle without matching active shot seq={sequence}");
            return;
        }
        ResolveShot(sequence);
    }

    private void OnPhysicsSettled()
    {
        if (shotLifecycle != null) OnPhysicsSettledFromContract(shotLifecycle.ShotSequence);
    }

    private void ResolveShot(int sequence)
    {
        if (scoreManager == null || turnManager == null || _builder == null || _applier == null)
        {
            Debug.LogError("[M5 ShotTracker] Transaction dependencies missing; sequence remains retryable.");
            return;
        }
        try
        {
            bool settledCueOnTable = _forcedCueOnTable ?? (_cueBall == null || _cueBall.transform == null || _cueBall.transform.position.y >= offTableY);
            var pots = new List<SnookerBallTracker.BallInfo>();
            foreach (var pending in _pendingPots) pots.Add(pending.Ball);
            _lastSettledCueOnTable = settledCueOnTable;
            ShotObservation observation = _builder.Build(_shotStart, _firstBallHit, _hitAnyBall, settledCueOnTable, pots);
            ShotDecision decision = M5SnookerRulesEngine.Evaluate(observation);
            if (decision.ShotSequence != sequence) throw new InvalidOperationException("Rules decision sequence mismatch.");
            _applier.Apply(decision, _shotStart);
            // Sequence becomes consumed only after the transaction returns successfully.
            _lastResolvedSequence = sequence;
            _forcedCueOnTable = null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[M5 ShotTracker] Transaction failed; seq={sequence} remains retryable. {ex}");
            return;
        }
        _shotInProgress = false;
        _pendingPots.Clear();
        _pendingIds.Clear();
        M5ShotResolved?.Invoke(sequence);
        ShotResolved?.Invoke(_firstBallHit, _hitAnyBall, _lastSettledCueOnTable);
        Debug.Log($"[M5 ShotTracker] ShotResolved seq={sequence} rules=1 transaction=1");
    }

    private Rigidbody _cueBallRb;
    private void ResolveCueBall()
    {
        if (ballTracker == null)
            throw new InvalidOperationException("[M5 ShotTracker] Cannot resolve cue ball because ballTracker is null.");

        ballTracker.RefreshIfNeeded();
        var cueCandidates = new List<SnookerBallTracker.BallInfo>();
        foreach (var ball in ballTracker.Balls)
        {
            if (ball == null || ball.transform == null)
                continue;
            if (ball.points == 0)
                cueCandidates.Add(ball);
        }

        if (cueCandidates.Count == 0)
            throw new InvalidOperationException("[M5 ShotTracker] Cue ball identity not found: expected exactly one BallInfo with points == 0 and a non-null transform.");
        if (cueCandidates.Count > 1)
            throw new InvalidOperationException($"[M5 ShotTracker] Duplicate cue identity: expected exactly one BallInfo with points == 0; found {cueCandidates.Count}.");

        _cueBall = cueCandidates[0];
        _cueBallRb = _cueBall.transform.GetComponent<Rigidbody>();
    }

    public bool ShotInProgress => _shotInProgress;
    public SnookerBallTracker.BallInfo FirstBallHit => _firstBallHit;
    public bool HitAnyBall => _hitAnyBall;
    public int LastResolvedSequence => _lastResolvedSequence;

    public void ForceResolveForTest()
    {
        if (_shotInProgress && _shotStart != null) OnPhysicsSettledFromContract(_shotStart.ShotSequence);
    }

    public void ForceResolveForTest(bool cueOnTable)
    {
        _forcedCueOnTable = cueOnTable;
        ForceResolveForTest();
    }
}
