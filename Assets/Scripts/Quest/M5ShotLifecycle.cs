using System;
using UnityEngine;
using PoolPhysics.Unity;

/// <summary>
/// M5 shot lifecycle authority. Owns the shot state only; it never scores or changes turns.
/// Physics is the source of truth and the settled boundary is the only downstream gate.
/// When a PoolPhysicsEventRunner is assigned and Enabled, the sim's AtRest flag and
/// TryGetSimBall velocities are used instead of PhysX Rigidbody state.
/// </summary>
public sealed class M5ShotLifecycle : MonoBehaviour
{
    public enum State { Idle, Active, Settling, Settled }

    public event Action ShotStarted;
    public event Action ShotSettled;

    [Header("Dependencies")]
    public SnookerBallTracker ballTracker;

    [Header("Event Runner (optional — overrides PhysX when Enabled)")]
    [SerializeField] public PoolPhysicsEventRunner eventRunner;

    [Header("Settled Boundary")]
    public float settledSpeedThreshold = 0.01f;
    public float settledDuration = 1.5f;

    public State CurrentState { get; private set; } = State.Idle;
    public int ShotSequence { get; private set; }

    private float _settledTimer;
    private int _debugFrames;

    private void Awake()
    {
        if (ballTracker == null)
            ballTracker = GetComponent<SnookerBallTracker>();
        if (eventRunner == null)
            eventRunner = GetComponentInParent<PoolPhysicsEventRunner>();
    }

    private void OnEnable()
    {
        if (eventRunner != null)
            eventRunner.ShotFinished += OnSimShotFinished;
    }

    private void OnDisable()
    {
        if (eventRunner != null)
            eventRunner.ShotFinished -= OnSimShotFinished;
    }

    /// <summary>Re-points this lifecycle at the backend that currently owns physics (called by
    /// PoolPhysicsAuthorityDirector / PoolPhysicsRulesBridge when a bay takes authority).
    /// Idempotent: re-binding the same runner does nothing.</summary>
    public void BindRunner(PoolPhysicsEventRunner runner)
    {
        if (eventRunner == runner)
            return;

        if (eventRunner != null)
            eventRunner.ShotFinished -= OnSimShotFinished;

        eventRunner = runner;

        if (eventRunner != null && isActiveAndEnabled)
            eventRunner.ShotFinished += OnSimShotFinished;

        Debug.Log($"[M5 LIFECYCLE] settled source → {(eventRunner != null ? eventRunner.BayName + " (EventRunner)" : "PhysX polling")}");
    }

    /// <summary>Called by EventRunner edge-trigger when sim transitions to AtRest.</summary>
    private void OnSimShotFinished()
    {
        if (CurrentState == State.Active || CurrentState == State.Settling)
        {
            Debug.Log($"[M5 LIFECYCLE] EventRunner AtRest → Settled seq={ShotSequence}");
            CurrentState = State.Settled;
            ShotSettled?.Invoke();
        }
    }

    public bool BeginShot()
    {
        if (CurrentState != State.Idle && CurrentState != State.Settled)
            return false;

        ShotSequence++;
        _settledTimer = 0f;
        _debugFrames = 0;
        CurrentState = State.Active;
        Debug.Log($"[M5 LIFECYCLE] BeginShot seq={ShotSequence} trackerCount={(ballTracker != null ? ballTracker.BallsCount() : -1)} runner={(eventRunner != null && eventRunner.Enabled ? "EVENT" : "PhysX")}");
        ShotStarted?.Invoke();
        return true;
    }

    private void Update()
    {
        if (CurrentState != State.Active && CurrentState != State.Settling)
            return;

        // When EventRunner owns physics, ShotFinished event drives settlement — no polling needed.
        if (eventRunner != null && eventRunner.Enabled)
            return;

        float maxSpeed = GetMaxBallSpeed();
        _debugFrames++;
        if ((_debugFrames % 60) == 0)
            Debug.Log($"[M5 LIFECYCLE] tick seq={ShotSequence} state={CurrentState} maxSpeed={maxSpeed:F6} settledFor={_settledTimer:F3}");

        if (maxSpeed > settledSpeedThreshold)
        {
            CurrentState = State.Settling;
            _settledTimer = 0f;
            return;
        }

        _settledTimer += Physics.simulationMode == SimulationMode.Script ? Time.fixedDeltaTime : Time.deltaTime;
        if (_settledTimer < settledDuration)
            return;

        CurrentState = State.Settled;
        ShotSettled?.Invoke();
    }

    private bool AreAllDynamicsSleeping()
    {
        if (ballTracker == null)
            return true;

        foreach (var ball in ballTracker.Balls)
        {
            if (ball?.transform == null || ball.potted)
                continue;
            Rigidbody rb = ball.transform.GetComponent<Rigidbody>();
            if (rb == null || rb.isKinematic)
                continue;
            if (!rb.IsSleeping())
                return false;
        }
        return true;
    }

    public void ResetToIdle()
    {
        _settledTimer = 0f;
        CurrentState = State.Idle;
    }

    private float GetMaxBallSpeed()
    {
        // --- EventRunner path: read sim velocities ---
        if (eventRunner != null && eventRunner.Enabled)
        {
            float maxSim = 0f;
            int count = eventRunner.BallCount;
            for (int i = 0; i < count; i++)
            {
                if (eventRunner.TryGetSimBall(i, out _, out Vector3 vel, out _, out bool pocketed) && !pocketed)
                    maxSim = Mathf.Max(maxSim, vel.magnitude);
            }
            return maxSim;
        }

        // --- Legacy PhysX path ---
        float max = 0f;
        if (ballTracker == null)
            return max;

        foreach (var ball in ballTracker.Balls)
        {
            if (ball?.transform == null || ball.potted)
                continue;

            Rigidbody rb = ball.transform.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

            max = Mathf.Max(max, rb.linearVelocity.magnitude);
        }

        return max;
    }
}