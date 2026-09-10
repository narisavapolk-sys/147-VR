using UnityEngine;

/// <summary>
/// Attach to the cue ball. Detects collisions with other balls and reports
/// the first hit to SnookerShotTracker for foul detection.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class CueBallCollision : MonoBehaviour
{
    [Tooltip("Reference to the shot tracker (found automatically if null).")]
    public SnookerShotTracker shotTracker;

    [Tooltip("Reference to the ball tracker (found automatically if null).")]
    public SnookerBallTracker ballTracker;

    private bool _enabled;

    private void Awake()
    {
        if (shotTracker == null)
            shotTracker = FindObjectOfType<SnookerShotTracker>();
        if (ballTracker == null)
            ballTracker = FindObjectOfType<SnookerBallTracker>();
    }

    /// <summary>Enable collision detection (call when a shot starts).</summary>
    public void EnableDetection()
    {
        _enabled = true;
    }

    /// <summary>Disable collision detection (call after a shot is resolved).</summary>
    public void DisableDetection()
    {
        _enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_enabled)
            return;

        if (shotTracker == null || ballTracker == null)
            return;

        // Check if we hit a ball (not the table, rails, etc.)
        string hitName = collision.gameObject.name;
        int points = SnookerBallTracker.PointsForName(hitName);

        // Skip if it's the cue ball itself or not a ball
        if (points < 0 || points == 0)
            return;

        // Find the BallInfo for the hit ball
        SnookerBallTracker.BallInfo hitBall = ballTracker.FindBall(hitName);
        if (hitBall == null)
            return;

        // Report to shot tracker
        shotTracker.OnCueBallHitBall(hitBall);
    }
}
