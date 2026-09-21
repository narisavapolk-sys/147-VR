using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks what happens after the cue ball is struck:
///   - Which ball is hit first (for wrong-ball-first fouls)
///   - Whether any ball is hit at all (miss = foul)
///   - Whether the cue ball falls off the table (off-table foul)
///
/// Attach to the same GameObject as SnookerScoreManager.
/// Fires ShotResolved after every shot once all balls come to rest.
/// </summary>
public sealed class SnookerShotTracker : MonoBehaviour
{
    private sealed class PendingPot
    {
        public SnookerBallTracker.BallInfo Ball;
        public SnookerBallTracker.PocketInfo Pocket;
    }

    private readonly List<PendingPot> _pendingPots = new List<PendingPot>();

    /// <summary>Fired when a shot is fully resolved: (firstBallHit, hitAnyBall, cueBallOnTable).</summary>
    public event Action<SnookerBallTracker.BallInfo, bool, bool> ShotResolved;

    [Header("Dependencies")]
    public SnookerBallTracker ballTracker;
    public SnookerScoreManager scoreManager;
    public SnookerTurnManager turnManager;
    public M5ShotLifecycle shotLifecycle;
    public M5ShotEventContract eventContract;

    [Header("Off-table detection")]
    [Tooltip("If the cue ball centre drops below this Y, it counts as off the table.")]
    public float offTableY = 0.3f;

    // Shot state
    private bool _shotInProgress;
    private SnookerBallTracker.BallInfo _cueBall;
    private SnookerBallTracker.BallInfo _firstBallHit;
    private bool _hitAnyBall;
    private float _restTimer;
    private bool _waitingForRest;
    private bool _potOccurredDuringShot; // true if any ball was potted during this shot
    private int _lastResolvedSequence;

    [Header("Rest detection")]
    [Tooltip("All balls must stay below this speed for this long before the shot is resolved.")]
    public float restThreshold = 0.01f;
    public float restTimeSeconds = 1.5f;

    private void Awake()
    {
        if (ballTracker == null)
            ballTracker = GetComponent<SnookerBallTracker>();
        if (scoreManager == null)
            scoreManager = GetComponent<SnookerScoreManager>();
        if (turnManager == null)
            turnManager = GetComponent<SnookerTurnManager>();
        if (shotLifecycle == null)
            shotLifecycle = GetComponent<M5ShotLifecycle>();
        if (shotLifecycle == null)
            shotLifecycle = FindObjectOfType<M5ShotLifecycle>();
        if (eventContract == null)
            eventContract = GetComponent<M5ShotEventContract>();
        if (eventContract == null)
            eventContract = FindObjectOfType<M5ShotEventContract>();
        if (eventContract != null)
        {
            eventContract.InitializeBindings();
            eventContract.PhysicsSettled += OnPhysicsSettledFromContract;
        }
        else if (shotLifecycle != null)
            shotLifecycle.ShotSettled += OnPhysicsSettled;
        if (ballTracker != null)
            ballTracker.BallPotted += OnBallPotted;
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    /// <summary>Rebinds event sources after dependencies are assigned by scene/bootstrap code.</summary>
    public void InitializeBindings()
    {
        UnbindEvents();
        if (eventContract != null)
        {
            eventContract.InitializeBindings();
            eventContract.PhysicsSettled += OnPhysicsSettledFromContract;
        }
        else if (shotLifecycle != null)
            shotLifecycle.ShotSettled += OnPhysicsSettled;
        if (ballTracker != null)
            ballTracker.BallPotted += OnBallPotted;
    }

    private void UnbindEvents()
    {
        if (eventContract != null)
            eventContract.PhysicsSettled -= OnPhysicsSettledFromContract;
        if (shotLifecycle != null)
            shotLifecycle.ShotSettled -= OnPhysicsSettled;
        if (ballTracker != null)
            ballTracker.BallPotted -= OnBallPotted;
    }

    /// <summary>Call when a shot begins (from SnookerCueController.Shoot or equivalent).</summary>
    public void OnShotFired()
    {
        _shotInProgress = true;
        _firstBallHit = null;
        _hitAnyBall = false;
        _restTimer = 0f;
        _waitingForRest = false;
        _potOccurredDuringShot = false;
        _pendingPots.Clear();
        ResolveCueBall();

    }

    private void OnBallPotted(SnookerBallTracker.BallInfo ball, SnookerBallTracker.PocketInfo pocket)
    {
        if (!_shotInProgress || ball == null || pocket == null)
            return;

        _pendingPots.Add(new PendingPot { Ball = ball, Pocket = pocket });
        _potOccurredDuringShot = true;
    }

    /// <summary>Register that the cue ball contacted another ball. Call from collision callback.</summary>
    public void OnCueBallHitBall(SnookerBallTracker.BallInfo hitBall)
    {
        if (!_shotInProgress)
            return;

        _hitAnyBall = true;

        // Record only the first ball hit.
        if (_firstBallHit == null)
            _firstBallHit = hitBall;
    }

    private void Update()
    {
        if (!_shotInProgress)
            return;

        // M5 owns the settled boundary. Legacy local rest polling is disabled when M5 exists.
        if (shotLifecycle != null)
            return;

        // Check if cue ball has fallen off the table.
        if (_cueBall != null && _cueBall.transform != null)
        {
            if (_cueBall.transform.position.y < offTableY)
            {
                ResolveShot(firstBall: _firstBallHit, hitAny: _hitAnyBall, cueOnTable: false);
                return;
            }
        }

        // Once any ball has moved, wait for all to rest.
        if (!_waitingForRest && _hitAnyBall)
        {
            float maxSpeed = GetMaxBallSpeed();
            if (maxSpeed > restThreshold)
            {
                _waitingForRest = true;
                _restTimer = 0f;
            }
        }

        if (_waitingForRest)
        {
            float maxSpeed = GetMaxBallSpeed();
            if (maxSpeed > restThreshold)
            {
                _restTimer = 0f;
            }
            else
            {
                _restTimer += Time.deltaTime;
                if (_restTimer >= restTimeSeconds)
                {
                    ResolveShot(firstBall: _firstBallHit, hitAny: _hitAnyBall, cueOnTable: true);
                    return;
                }
            }
        }
        else if (!_hitAnyBall)
        {
            // No ball was hit ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â wait for the cue ball to stop, then resolve as miss.
            bool ballAtRest = true; // if no rigidbody, treat as immediately at rest
            if (_cueBallRb != null)
                ballAtRest = _cueBallRb.linearVelocity.magnitude < restThreshold;

            if (ballAtRest)
            {
                _restTimer += Time.deltaTime;
                if (_restTimer >= restTimeSeconds)
                {
                    ResolveShot(firstBall: null, hitAny: false, cueOnTable: true);
                    return;
                }
            }
            else
            {
                _restTimer = 0f;
            }
        }
    }

    private void OnPhysicsSettledFromContract(int shotSequence)
    {
        if (shotSequence <= 0 || shotSequence <= _lastResolvedSequence)
        {
            Debug.Log("[M5 ShotTracker] Ignore duplicate/stale settle seq=" + shotSequence + " last=" + _lastResolvedSequence);
            return;
        }
        _lastResolvedSequence = shotSequence;
        OnPhysicsSettled();
    }

    private void OnPhysicsSettled()
    {
        if (!_shotInProgress)
            return;

        bool cueOnTable = _cueBall == null || _cueBall.transform == null ||
                          _cueBall.transform.position.y >= offTableY;
        ResolveShot(_firstBallHit, _hitAnyBall, cueOnTable);
    }

    private void ResolveShot(SnookerBallTracker.BallInfo firstBall, bool hitAny, bool cueOnTable)
    {
        _shotInProgress = false;

        if (scoreManager == null || turnManager == null)
            return;

        int striker = turnManager.currentPlayer;
        int opponent = striker == 1 ? 2 : 1;

        // Rules are evaluated only after the M5 settled boundary.
        if (!cueOnTable)
        {
            int penalty = Mathf.Max(scoreManager.foulPenalty, scoreManager.BallOnValue());
            scoreManager.AddScorePublic(opponent, penalty);
            turnManager.NextTurn();
            ShotResolved?.Invoke(firstBall, hitAny, cueOnTable);
            return;
        }

        if (!hitAny)
        {
            int penalty = Mathf.Max(scoreManager.foulPenalty, scoreManager.BallOnValue());
            scoreManager.AddScorePublic(opponent, penalty);
            turnManager.NextTurn();
            ShotResolved?.Invoke(firstBall, hitAny, cueOnTable);
            return;
        }

        if (firstBall != null && !scoreManager.IsLegalFirstHit(firstBall))
        {
            int penalty = Mathf.Max(scoreManager.foulPenalty, scoreManager.BallOnValue(), firstBall.points);
            scoreManager.AddScorePublic(opponent, penalty);
            Debug.Log($"[ShotTracker] M5 Rules: wrong ball first ({firstBall.name}) ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â Player {opponent} +{penalty}");
            turnManager.NextTurn();
            ShotResolved?.Invoke(firstBall, hitAny, cueOnTable);
            return;
        }

        bool foulFromPot = false;
        bool legalPot = false;

        // Scoring is now a post-settle transaction. ScoreManager never sees BallPotted directly.
        foreach (PendingPot pot in _pendingPots)
        {
            SnookerScoreManager.PotResolution result =
                scoreManager.ResolvePottedBall(pot.Ball, pot.Pocket, striker);
            foulFromPot |= result == SnookerScoreManager.PotResolution.Foul;
            legalPot |= result == SnookerScoreManager.PotResolution.Legal;
            if (result == SnookerScoreManager.PotResolution.FrameOver)
                legalPot = true;
        }

        if (foulFromPot)
            turnManager.NextTurn();
        else if (legalPot)
            turnManager.StrikerContinues();
        else
            turnManager.NextTurn();

        Debug.Log($"[ShotTracker] M5 transaction committed. pots={_pendingPots.Count} foul={foulFromPot} legalPot={legalPot}");
        _pendingPots.Clear();
        ShotResolved?.Invoke(firstBall, hitAny, cueOnTable);
    }

    private Rigidbody _cueBallRb;

    private void ResolveCueBall()
    {
        if (ballTracker == null)
            return;
        _cueBall = ballTracker.FindBall("White_CueBall");
        if (_cueBall?.transform != null)
            _cueBallRb = _cueBall.transform.GetComponent<Rigidbody>();
    }

    private float GetMaxBallSpeed()
    {
        float max = 0f;
        if (ballTracker == null)
            return max;
        foreach (var ball in ballTracker.Balls)
        {
            if (ball?.transform == null || ball.potted)
                continue;
            Rigidbody rb = ball.transform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                float speed = rb.linearVelocity.magnitude;
                if (speed > max)
                    max = speed;
            }
        }
        return max;
    }

    // ---- Public accessors for tests ----

    public bool ShotInProgress => _shotInProgress;
    public SnookerBallTracker.BallInfo FirstBallHit => _firstBallHit;
    public bool HitAnyBall => _hitAnyBall;

    /// <summary>
    /// Force-resolve the current shot immediately (for editor tests where Time.deltaTime is 0).
    /// Call after OnShotFired + OnCueBallHitBall to trigger the foul check.
    /// </summary>
    public void ForceResolveForTest()
    {
        if (!_shotInProgress)
            return;
        ResolveShot(firstBall: _firstBallHit, hitAny: _hitAnyBall, cueOnTable: true);
    }

    /// <summary>Force-resolve with an explicit cue-on-table flag (for off-table tests).</summary>
    public void ForceResolveForTest(bool cueOnTable)
    {
        if (!_shotInProgress)
            return;
        ResolveShot(firstBall: _firstBallHit, hitAny: _hitAnyBall, cueOnTable: cueOnTable);
    }
}
