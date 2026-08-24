using System;
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
    /// <summary>Fired when a shot is fully resolved: (firstBallHit, hitAnyBall, cueBallOnTable).</summary>
    public event Action<SnookerBallTracker.BallInfo, bool, bool> ShotResolved;

    [Header("Dependencies")]
    public SnookerBallTracker ballTracker;
    public SnookerScoreManager scoreManager;
    public SnookerTurnManager turnManager;

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
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
            scoreManager.PotRecorded -= OnPotRecorded;
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
        ResolveCueBall();

        // Listen for pots so we know the ScoreManager already handled turn
        if (scoreManager != null)
        {
            scoreManager.PotRecorded -= OnPotRecorded;
            scoreManager.PotRecorded += OnPotRecorded;
        }
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
            // No ball was hit — wait for the cue ball to stop, then resolve as miss.
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

    private void OnPotRecorded(string ball, string pocket, int points, int player)
    {
        if (_shotInProgress)
            _potOccurredDuringShot = true;
    }

    private void ResolveShot(SnookerBallTracker.BallInfo firstBall, bool hitAny, bool cueOnTable)
    {
        _shotInProgress = false;

        // Unsubscribe from pot events
        if (scoreManager != null)
            scoreManager.PotRecorded -= OnPotRecorded;

        if (scoreManager == null || turnManager == null)
            return;

        int striker = turnManager.currentPlayer;
        int opponent = striker == 1 ? 2 : 1;

        // If a pot was recorded during this shot, the ScoreManager already handled
        // turn passing / striker continuation — do not double-call NextTurn.
        if (_potOccurredDuringShot)
        {
            Debug.Log($"[ShotTracker] Pot occurred during shot — ScoreManager already handled turn.");
            ShotResolved?.Invoke(firstBall, hitAny, cueOnTable);
            return;
        }

        // ---- Foul: cue ball off the table ----
        if (!cueOnTable)
        {
            int penalty = Mathf.Max(scoreManager.foulPenalty, scoreManager.BallOnValue());
            scoreManager.AddScorePublic(opponent, penalty);
            Debug.Log($"[ShotTracker] Foul! Cue ball off the table — Player {opponent} awarded {penalty} points (turn passes)");
            turnManager.NextTurn();
            ShotResolved?.Invoke(firstBall, hitAny, cueOnTable);
            return;
        }

        // ---- Foul: missed all balls ----
        if (!hitAny)
        {
            int penalty = Mathf.Max(scoreManager.foulPenalty, scoreManager.BallOnValue());
            scoreManager.AddScorePublic(opponent, penalty);
            Debug.Log($"[ShotTracker] Foul! Missed all balls — Player {opponent} awarded {penalty} points (turn passes)");
            turnManager.NextTurn();
            ShotResolved?.Invoke(firstBall, hitAny, cueOnTable);
            return;
        }

        // ---- Foul: hit wrong ball first ----
        if (firstBall != null && !scoreManager.IsLegalFirstHit(firstBall))
        {
            int penalty = Mathf.Max(scoreManager.foulPenalty, scoreManager.BallOnValue(), firstBall.points);
            scoreManager.AddScorePublic(opponent, penalty);
            Debug.Log($"[ShotTracker] Foul! Wrong ball first ({firstBall.name} when ball on was {scoreManager.BallOnName()}) — Player {opponent} awarded {penalty} points (turn passes)");
            turnManager.NextTurn();
            ShotResolved?.Invoke(firstBall, hitAny, cueOnTable);
            return;
        }

        // ---- Legal shot (first ball hit is correct) — no action needed here;
        //      pots are handled by SnookerScoreManager via BallPotted events. ----
        Debug.Log($"[ShotTracker] Legal shot — hit {firstBall?.name ?? "?"} first");
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
