using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// S2 #2 — BHP (BLACK HOLE POOL)-style cue grip controller (scaffold).
///
/// Implements the control SCHEME borrowed from BLACK HOLE POOL (see
/// Docs/AI_TEAM/BHP_CONTROL_MAP.md for the evidence and the mapping table):
///   * AIM        — free hands / head aim (no button)
///   * LINE UP    — HOLD LEFT GRIP = fine-aim lock; left-hand left/right rotates the lock
///   * SHOOT      — HOLD RIGHT GRIP to grip the cue; physical back-and-forth stroke;
///                  the forward-pass peak velocity along the cue axis powers the shot
///                  (Right Trigger charge remains the accessibility fallback elsewhere)
///   * PICK UP    — press LEFT X (primary) to lift/replace the cue ball when rules allow
///   * RECENTER   — LEFT GRIP + RIGHT GRIP pressed together
///   * LASER      — HOLD LEFT Y (secondary) for the laser pointer (not staged)
///
/// It is STATE-GATED: buttons mean different things depending on Stage
/// (Walking vs StagedOnBall), exactly per the mapping doc.
///
/// This class only reads raw Quest Touch features and publishes semantic events —
/// it does not move the cue or call the physics bridge yet (deliberate: wiring
/// into SnookerShotTracker/SnookerCueController is one reviewed change in the next
/// Unity session). Input style mirrors SnookerCueController (XR InputDevices).
/// </summary>
public sealed class BhpCueGripController : MonoBehaviour
{
    public enum Stage { Walking, StagedOnBall }

    // ---- semantic events (consumers wire in the next Unity session) ----
    /// <summary>Right Grip pressed while staged: cue is now in-hand; stroke tracking starts.</summary>
    public event Action OnCueGripped;
    /// <summary>Right Grip released while staged without firing.</summary>
    public event Action OnCueReleased;
    /// <summary>Physical stroke fired while staged: power01 = forward-pass peak / max, 0..1.</summary>
    public event Action<float> OnStrokeFired;
    /// <summary>Left Grip pressed while staged: fine-aim lock engaged (orbit mode on).</summary>
    public event Action OnFineAimLock;
    /// <summary>Left Grip released: aim locked for the shot.</summary>
    public event Action OnFineAimReleased;
    /// <summary>Left X toggle while staged with ball-in-hand: true=lift, false=drop.</summary>
    public event Action<bool> OnCueBallLiftToggle;
    /// <summary>Left Grip + Right Grip pressed in the same window: recenter requested.</summary>
    public event Action OnRecenterCombo;
    /// <summary>Laser pointer toggled on/off (holding Y while walking).</summary>
    public event Action<bool> OnLaserActive;

    [Header("BHP scheme toggles")]
    public Stage stage = Stage.Walking;           // SnookerShotTracker sets this per shot
    [Range(0.05f, 3f)] public float FINE_AIM_SENS = 1.2f;   // left-hand slide → aim rotation gain
    [Range(0.5f, 6f)] public float STROKE_MAX_SPEED = 2.6f; // m/s forward peak mapped to power 1
    [Range(0.02f, 1f)] public float STROKE_MIN_SPEED = 0.12f; // below this = no accidental fire
    public bool STAGE_ENABLED = true;             // master switch while the mapping is under playtest

    // ---- live semantic state (queryable; some consumers poll instead of events) ----
    public bool CueGripped => _rightGripHeld;
    public bool FineAimLocked => _fineAimHeld;
    public bool LaserActive => _laserHeld;
    /// <summary>Signed fine-aim rotation request accumulated this frame (radians, world-Y).</summary>
    public float FineAimDeltaRad { get; private set; }
    /// <summary>This frame's forward-pass stroke sample in m/s (0 when not stroking).</summary>
    public float StrokeForwardSpeed { get; private set; }

    XRDevice _left, _right;
    bool _leftGrip, _rightGrip, _fineAimHeld, _laserHeld;
    bool _prevLeftGrip, _prevRightGrip, _prevLaser, _prevLeftPrimary;
    bool _leftPrimary;
    bool _leftPrimaryLiftState;
    Vector3 _fineAimAnchor;
    Vector3 _prevRightPos;
    bool _hasRightPos;

    void Update()
    {
        if (!STAGE_ENABLED) return;
        if (!RefreshDevices()) return;

        bool lg = Feature(_left, CommonUsages.gripButton);
        bool rg = Feature(_right, CommonUsages.gripButton);
        bool lp = Feature(_left, CommonUsages.primaryButton);          // X
        bool ls = Feature(_left, CommonUsages.secondaryButton);        // Y
        bool rt = Feature(_right, CommonUsages.triggerButton);

        _leftGrip = lg;
        _rightGrip = rg;
        _leftPrimary = lp;
        bool laserHeld = ls && stage == Stage.Walking;
        _laserHeld = laserHeld;
        FineAimDeltaRad = 0f;
        StrokeForwardSpeed = 0f;

        // ---- RECENTER combo: both grips pressed in the same window ----
        if (lg && rg && (!_prevLeftGrip || !_prevRightGrip)) OnRecenterCombo?.Invoke();

        // ---- LASER (walking only): hold Y ----
        if (laserHeld != _prevLaser) { OnLaserActive?.Invoke(laserHeld); _prevLaser = laserHeld; }

        // ---- LEFT X: pickup/drop toggle (staged + ball-in-hand semantics live in the tracker) ----
        if (stage == Stage.StagedOnBall && lp && !_prevLeftPrimary)
        {
            _leftPrimaryLiftState = !_leftPrimaryLiftState;
            OnCueBallLiftToggle?.Invoke(_leftPrimaryLiftState);
        }
        _prevLeftPrimary = lp;

        // ---- staging/no-staging transitions per hand ----
        if (stage == Stage.StagedOnBall)
        {
            // LINE UP: HOLD Left Grip = fine-aim lock; left-hand slide drives orbit rotation
            if (lg && !_fineAimHeld) { _fineAimHeld = true; _fineAimAnchor = LeftPos(); OnFineAimLock?.Invoke(); }
            if (!lg && _fineAimHeld) { _fineAimHeld = false; OnFineAimReleased?.Invoke(); }
            if (_fineAimHeld) TickFineAim();

            // SHOOT: HOLD Right Grip to grip the cue; back-and-forth = stroke
            if (rg && !_prevRightGrip) OnCueGripped?.Invoke();
            if (!rg && _prevRightGrip && !FireStrokeIfAny()) { OnCueReleased?.Invoke(); }
            if (rg) TickStroke();
        }
        else
        {
            if (_fineAimHeld) { _fineAimHeld = false; OnFineAimReleased?.Invoke(); }
            if (_prevRightGrip && !rg) OnCueReleased?.Invoke();
        }

        _prevLeftGrip = lg;
        _prevRightGrip = rg;
    }

    // left-hand left/right slide (world XZ) rotated into the head frame = orbit request
    void TickFineAim()
    {
        Vector3 pos = LeftPos();
        Vector3 d = pos - _fineAimAnchor;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0004f) return;              // deadzone ~2 cm
        Transform head = ResolveHead();
        if (head == null) return;
        Vector3 fwd = head.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);    // player-local "screen right"
        float lateral = Vector3.Dot(d, right);
        _fineAimAnchor = pos;                              // integrate incrementally
        FineAimDeltaRad = lateral * FINE_AIM_SENS * 0.04f; // ~0.05 rad per 4 cm at gain 1.2
    }

    // right-hand device velocity along the cue axis proxy (BHP: "back and forth")
    void TickStroke()
    {
        Vector3 pos;
        if (!_right.TryGetFeatureValue(CommonUsages.devicePosition, out pos)) return;
        if (!_hasRightPos) { _prevRightPos = pos; _hasRightPos = true; return; }

        Vector3 vel = (pos - _prevRightPos) / Mathf.Max(Time.deltaTime, 1e-4f);
        _prevRightPos = pos;

        // the consumer owns the real cue axis; until wired, device +Z (grip axis proxy)
        // is measured and exposed — SnookerCueController will dot this with the true axis.
        Transform head = ResolveHead();
        Vector3 axis = head != null ? head.forward : Vector3.forward;
        axis.y = 0f;
        if (axis.sqrMagnitude > 0.0001f) axis.Normalize();
        StrokeForwardSpeed = Vector3.Dot(vel, axis);
    }

    /// <summary>Fire on release if the last forward pass beat the min speed. Returns true if fired.</summary>
    bool FireStrokeIfAny()
    {
        float fwd = StrokeForwardSpeed;
        StrokeForwardSpeed = 0f;                 // one flag, one fire
        if (fwd >= STROKE_MIN_SPEED)
        {
            float power01 = Mathf.Clamp01(fwd / STROKE_MAX_SPEED);
            OnStrokeFired?.Invoke(power01);
            return true;
        }
        return false;
    }

    bool RefreshDevices()
    {
        // refresh every frame (cheap; controllers hot-attach/detach)
        _left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        _right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        return _left.isValid && _right.isValid;
    }

    static bool Feature(XRDevice d, InputFeatureUsage<bool> usage)
        => d.isValid && d.TryGetFeatureValue(usage, out bool v) && v;

    Vector3 LeftPos()
        => _left.isValid && _left.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p) ? p : Vector3.zero;

    Transform ResolveHead()
    {
        var cam = Camera.main; // optional ReferenceCamera field can replace this later
        return cam != null ? cam.transform : null;
    }
}
