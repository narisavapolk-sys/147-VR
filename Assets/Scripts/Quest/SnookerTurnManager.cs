using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tracks whose turn it is in a snooker match and notifies listeners (PlayerViewManager)
/// so the rig automatically moves to the shooting player — no more 1 / 2 keys.
///
/// Advance modes:
///   AfterShot – auto-advance once every tracked ball has come to rest after a shot
///               (requires Rigidbody balls; falls back to Manual when none are found).
///   Timer     – auto-advance after turnSeconds (good while the scene has no ball physics yet).
///   Manual    – advance via NextTurn() or the configured key.
///
/// Snooker rule: the player who is shooting always plays from the D (baulk) side, so
/// PlayerViewManager positions the active player there (shooterOnDSide).
/// </summary>
public sealed class SnookerTurnManager : MonoBehaviour
{
    public event Action<int> TurnChanged;

    public enum AdvanceMode { AfterShot, Timer, Manual }

    [Header("Turn")]
    [Tooltip("Player whose turn it is (1 or 2).")]
    public int currentPlayer = 1;

    [Header("Advance mode")]
    public AdvanceMode advanceMode = AdvanceMode.Timer;
    [Tooltip("Timer mode: seconds before the turn passes automatically.")]
    public float turnSeconds = 30f;
    [Tooltip("Manual/fallback: key to pass the turn (replaces the old 1/2 keys).")]
    public KeyCode nextTurnKey = KeyCode.Space;

    [Header("Shot-end detection (AfterShot mode)")]
    [Tooltip("Substring used to find balls (Rigidbody components).")]
    public string ballNameFilter = "ball";
    [Tooltip("All balls must stay below 0.01 m/s for this long before the turn ends.")]
    public float restTimeSeconds = 1.5f;

    private readonly List<Rigidbody> _balls = new List<Rigidbody>();
    private bool _shotInProgress;
    private float _restTimer;
    private float _turnTimer;
    private bool _strikerContinues;
    private M5ShotLifecycle _m5Lifecycle;

    private void Start()
    {
        _m5Lifecycle = GetComponent<M5ShotLifecycle>();
        if (_m5Lifecycle == null)
            _m5Lifecycle = FindObjectOfType<M5ShotLifecycle>();
        if (_m5Lifecycle != null && advanceMode != AdvanceMode.Manual)
        {
            advanceMode = AdvanceMode.Manual;
            Debug.Log("[M5] Turn timer disabled: M5 ShotLifecycle owns shot completion.");
        }
        FindBalls();
        if (advanceMode == AdvanceMode.AfterShot && _balls.Count == 0)
        {
            Debug.LogWarning("[SnookerTurnManager] No Rigidbody balls found — falling back to Manual mode. Use the configured key to pass the turn.");
            advanceMode = AdvanceMode.Manual;
        }
        BroadcastTurn();
    }

    [Serializable]
    public struct M5TransactionState
    {
        public int CurrentPlayer; public float TurnTimer; public bool StrikerContinues;
        public bool ShotInProgress; public float RestTimer;
    }

    public M5TransactionState CaptureM5TransactionState() => new M5TransactionState
    { CurrentPlayer=currentPlayer, TurnTimer=_turnTimer, StrikerContinues=_strikerContinues, ShotInProgress=_shotInProgress, RestTimer=_restTimer };

    public void RestoreM5TransactionState(M5TransactionState state)
    {
        currentPlayer=state.CurrentPlayer; _turnTimer=state.TurnTimer; _strikerContinues=state.StrikerContinues;
        _shotInProgress=state.ShotInProgress; _restTimer=state.RestTimer;
    }

    public void ApplyM5Decision(bool frameEnd, bool strikerContinues)
    {
        _turnTimer=0f; _restTimer=0f; _shotInProgress=false; _strikerContinues=strikerContinues;
        if (!frameEnd && !strikerContinues) currentPlayer = currentPlayer == 1 ? 2 : 1;
    }

    public void EmitM5TransactionCommitted()
    {
        if (_strikerContinues) TurnChanged?.Invoke(currentPlayer);
        else BroadcastTurn();
        _strikerContinues=false;
    }

    private void Update()
    {
        if (_m5Lifecycle != null && (_m5Lifecycle.CurrentState == M5ShotLifecycle.State.Active || _m5Lifecycle.CurrentState == M5ShotLifecycle.State.Settling)) return;
        if (nextTurnKey != KeyCode.None && Keyboard.current != null && Input.GetKeyDown(nextTurnKey))
        {
            NextTurn();
            return;
        }

        switch (advanceMode)
        {
            case AdvanceMode.AfterShot:
                UpdateAfterShot();
                break;
            case AdvanceMode.Timer:
                _turnTimer += Time.deltaTime;
                if (_turnTimer >= turnSeconds)
                {
                    _turnTimer = 0f;
                    NextTurn();
                }
                break;
        }
    }

    private void UpdateAfterShot()
    {
        float maxSpeed = 0f;
        foreach (Rigidbody ball in _balls)
        {
            if (ball == null)
                continue;
            float speed = ball.linearVelocity.magnitude;
            if (speed > maxSpeed)
                maxSpeed = speed;
        }

        if (maxSpeed > 0.01f)
        {
            _shotInProgress = true;
            _restTimer = 0f;
        }
        else if (_shotInProgress)
        {
            _restTimer += Time.deltaTime;
            if (_restTimer >= restTimeSeconds)
            {
                _shotInProgress = false;
                _restTimer = 0f;
                if (_strikerContinues)
                {
                    // The striker potted a ball legally — keep the table.
                    _strikerContinues = false;
                    Debug.Log("[SnookerTurnManager] Striker keeps the table (legal pot).");
                }
                else
                {
                    NextTurn();
                }
            }
        }
    }

    /// <summary>Called when the striker potted a ball legally — they keep the table.
    /// Prevents AfterShot/Timer modes from passing the turn.</summary>
    public void StrikerContinues()
    {
        _strikerContinues = true;
        _turnTimer = 0f;
        _restTimer = 0f;
        _shotInProgress = false;
    }

    /// <summary>Advances to the other player and fires TurnChanged.</summary>
    public void NextTurn()
    {
        SetTurn(currentPlayer == 1 ? 2 : 1);
    }

    public void SetTurn(int player)
    {
        _strikerContinues = false;
        if (player == currentPlayer)
            return;
        currentPlayer = player;
        _turnTimer = 0f;
        BroadcastTurn();
    }

    private void BroadcastTurn()
    {
        Debug.Log($"[SnookerTurnManager] Turn → Player {currentPlayer}");
        TurnChanged?.Invoke(currentPlayer);
    }

    private void FindBalls()
    {
        _balls.Clear();
        foreach (Rigidbody rb in FindObjectsOfType<Rigidbody>(true))
        {
            if (rb.name.IndexOf(ballNameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                _balls.Add(rb);
        }
        if (_balls.Count > 0)
            Debug.Log($"[SnookerTurnManager] Tracking {_balls.Count} balls for shot-end detection.");
    }
}
