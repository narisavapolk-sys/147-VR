using System;
using UnityEngine;

/// <summary>
/// M5 shot lifecycle authority. Owns the shot state only; it never scores or changes turns.
/// Physics is the source of truth and the settled boundary is the only downstream gate.
/// </summary>
public sealed class M5ShotLifecycle : MonoBehaviour
{
    public enum State { Idle, Active, Settling, Settled }

    public event Action ShotStarted;
    public event Action ShotSettled;

    [Header("Dependencies")]
    public SnookerBallTracker ballTracker;

    [Header("Settled Boundary")]
    public float settledSpeedThreshold = 0.01f;
    public float settledDuration = 1.5f;

    public State CurrentState { get; private set; } = State.Idle;
    public int ShotSequence { get; private set; }

    private float _settledTimer;
    private int _physicsSteps;

    private void Awake()
    {
        if (ballTracker == null)
            ballTracker = GetComponent<SnookerBallTracker>();
    }

    public bool BeginShot()
    {
        if (CurrentState != State.Idle && CurrentState != State.Settled)
            return false;

        ShotSequence++;
        _settledTimer = 0f;
        _physicsSteps = 0;
        CurrentState = State.Active;
        Debug.Log($"[M5 LIFECYCLE] BeginShot seq={ShotSequence} trackerCount={(ballTracker != null ? ballTracker.BallsCount() : -1)}");
        ShotStarted?.Invoke();
        return true;
    }

    // The settled boundary is measured on the physics clock, not the render clock.
    // Running this in FixedUpdate with Time.fixedDeltaTime is what makes the same
    // shot resolve identically at 72 Hz (Quest 2) and 90/120 Hz (Quest 3).
    private void FixedUpdate()
    {
        if (CurrentState != State.Active && CurrentState != State.Settling)
            return;

        float maxSpeed = GetMaxBallSpeed();
        _physicsSteps++;

        if ((_physicsSteps % 60) == 0)
            Debug.Log($"[M5 LIFECYCLE] tick seq={ShotSequence} state={CurrentState} step={_physicsSteps} maxSpeed={maxSpeed:F6} settledFor={_settledTimer:F3} trackerCount={(ballTracker != null ? ballTracker.BallsCount() : -1)}");

        // Settled is a measured-speed boundary; PhysX sleep state is not authoritative.
        // Contact/solver jitter can keep a resting ball awake above the sleep heuristic.
        // bool allDynamicsSleeping = AreAllDynamicsSleeping();
        if (maxSpeed > settledSpeedThreshold)
        {
            CurrentState = State.Settling;
            _settledTimer = 0f;
            return;
        }

        _settledTimer += Time.fixedDeltaTime;
        if (_settledTimer < settledDuration)
            return;

        CurrentState = State.Settled;
        Debug.Log($"[M5 LIFECYCLE] Settled seq={ShotSequence} steps={_physicsSteps} settledFor={_settledTimer:F3}");
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
