using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "Radar above the table": watches every ball's position and reports when one drops
/// into a pocket (fires BallPotted). Works with plain transforms — no physics needed,
/// though it also works if balls have Rigidbodies.
///
/// Detection rule (per frame, projected onto the table plane):
///   - a ball is "in play" while its centre is above pocketMouthY;
///   - once its centre drops below pocketMouthY it is checked against every pocket:
///     if the XZ distance to a pocket centre is within the pocket's radius, the ball
///     is considered potted in that pocket.
///
/// Pockets are auto-detected from "Pocket Pad*" children of the table root; if fewer
/// than six are found the missing corner(s) are mirrored from the found ones (the
/// snooker pocket set is origin-symmetric). Ball values follow snooker (red=1 … black=7).
/// </summary>
public sealed class SnookerBallTracker : MonoBehaviour
{
    public event Action<BallInfo, PocketInfo> BallPotted;

    [Serializable]
    public sealed class BallInfo
    {
        public string name;
        public Transform transform;
        public int points;
        public bool potted;
        [Tooltip("Original spot captured the first time the ball is seen (used for re-spotting).")]
        public Vector3 homePosition;
        public bool hasHomePosition;
    }

    [Serializable]
    public sealed class PocketInfo
    {
        public string name;
        public Vector3 position;
        public float radius;
    }

    [Header("Table")]
    [Tooltip("Root to search for balls and pocket pads (defaults to this transform).")]
    public Transform tableRoot;
    [Tooltip("Object name prefix that marks pocket pads (auto-detection).")]
    public string pocketNameFilter = "Pocket Pad";
    [Tooltip("How close (XZ plane, metres) a ball must be to a pocket centre to count.")]
    public float pocketRadius = 0.18f;
    [Tooltip("Ball centre below this Y counts as dropped into the pocket zone.")]
    public float pocketMouthY = 0.75f;

    [Header("Balls")]
    [Tooltip("Prefixes used to find balls under the table root.")]
    public string[] ballNameFilters =
    {
        "White_CueBall", "Red", "Yellow", "Green", "Brown", "Blue", "Pink", "Black"
    };
    public bool autoDetectOnStart = true;
    public bool logPotEvents = true;

    private readonly List<BallInfo> _balls = new List<BallInfo>();
    private readonly List<PocketInfo> _pockets = new List<PocketInfo>();
    private readonly Dictionary<Transform, float> _previousY = new Dictionary<Transform, float>();
    private bool _ready;

    private void Start()
    {
        if (autoDetectOnStart)
            Refresh();
    }

    private void Update()
    {
        if (!_ready)
            return;

        for (int i = 0; i < _balls.Count; i++)
        {
            BallInfo ball = _balls[i];
            if (ball == null || ball.transform == null || ball.potted)
                continue;

            Vector3 pos = ball.transform.position;
            float previousY = _previousY.TryGetValue(ball.transform, out float lastY) ? lastY : pos.y;
            _previousY[ball.transform] = pos.y;
            if (pos.y >= pocketMouthY || previousY < pocketMouthY)
                continue;

            PocketInfo pocket = FindNearestPocket(pos);
            if (pocket == null)
                continue;

            ball.potted = true;
            if (logPotEvents)
                Debug.Log($"[BallTracker] {ball.name} potted in {pocket.name} (+{ball.points})");
            BallPotted?.Invoke(ball, pocket);
        }
    }

    /// <summary>Re-scan the scene for balls and pockets. Safe to call anytime.</summary>
    [ContextMenu("Refresh Balls & Pockets")]
    public void Refresh()
    {
        Transform root = tableRoot != null ? tableRoot : transform;
        var previousHomes = new Dictionary<Transform, Vector3>();
        foreach (BallInfo existing in _balls)
        {
            if (existing != null && existing.transform != null && existing.hasHomePosition)
                previousHomes[existing.transform] = existing.homePosition;
        }

        _pockets.Clear();
        _pockets.AddRange(DetectPockets(root));

        _balls.Clear();
        _previousY.Clear();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            // Only active hierarchy is authoritative for gameplay balls. Inactive legacy
            // visual ball sets remain available for presentation but must not enter M5 state.
            if (!child.gameObject.activeInHierarchy)
                continue;

            string name = child.name;
            int points = PointsForName(name);
            if (points >= 0)
            {
                var ball = new BallInfo { name = name, transform = child, points = points };
                ball.homePosition = previousHomes.TryGetValue(child, out Vector3 previousHome) ? previousHome : child.position;
                ball.hasHomePosition = true;
                _previousY[child] = child.position.y;
                _balls.Add(ball);
            }
        }

        // Calibration scenes may keep the ball rack as a sibling prefab rather than
        // under the table hierarchy, and imported ball meshes may use generic names.
        // Fall back to sphere Rigidbody bodies so the tracker remains authoritative
        // for M5 shot lifecycle even when presentation naming is not production-ready.
        if (_balls.Count == 0)
        {
            foreach (Rigidbody rb in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (rb == null)
                    continue;

                SphereCollider sphere = rb.GetComponent<SphereCollider>();
                if (sphere == null || sphere.radius < 0.02f || sphere.radius > 0.04f)
                    continue;

                string name = rb.gameObject.name;
                int points = PointsForName(name);
                var ball = new BallInfo
                {
                    name = name,
                    transform = rb.transform,
                    points = points >= 0 ? points : 0
                };
                ball.homePosition = previousHomes.TryGetValue(rb.transform, out Vector3 previousRigidbodyHome) ? previousRigidbodyHome : rb.position;
                ball.hasHomePosition = true;
                _previousY[rb.transform] = rb.position.y;
                _balls.Add(ball);
            }
        }

        // Calibration scenes use Sphere.009 as the authoritative cue ball while
        // production naming uses White_CueBall. Keep the cue in the tracker so
        // lifecycle/settled logic observes the same Rigidbody that receives shots.
        GameObject calibrationCue = GameObject.Find("Sphere.009");
        if (calibrationCue != null)
        {
            Transform cueTransform = calibrationCue.transform;
            bool alreadyTracked = _balls.Exists(ball => ball != null && ball.transform == cueTransform);
            if (!alreadyTracked)
            {
                var cueBall = calibrationCue.GetComponent<Rigidbody>();
                if (cueBall != null)
                {
                    _balls.Add(new BallInfo
                    {
                        name = calibrationCue.name,
                        transform = cueTransform,
                        points = 0,
                        homePosition = cueTransform.position,
                        hasHomePosition = true
                    });
                }
            }
        }

        _ready = true;
        Debug.Log($"[BallTracker] Ready: {_balls.Count} balls, {_pockets.Count} pockets.");
    }

    /// <summary>Runs one detection pass immediately (used by tests / editor tools).</summary>
    public void ManualUpdate()
    {
        if (!_ready)
            Refresh();
        Update();
    }

    public int BallsCount() => _balls.Count;

    public int PocketsCount() => _pockets.Count;

    public IReadOnlyList<BallInfo> Balls => _balls;

    public IReadOnlyList<PocketInfo> Pockets => _pockets;

    /// <summary>Ensures the tracker has scanned the scene at least once.</summary>
    public void RefreshIfNeeded()
    {
        if (!_ready)
            Refresh();
    }

    /// <summary>Marks a ball as back on the table so it can be potted again (used by re-spot).</summary>
    public void Unpot(BallInfo ball)
    {
        if (ball != null)
            ball.potted = false;
    }

    /// <summary>First in-play ball whose name starts with the given prefix, or null.</summary>
    public BallInfo FindBall(string namePrefix)
    {
        foreach (BallInfo ball in _balls)
        {
            if (ball == null || ball.transform == null || ball.potted)
                continue;
            if (ball.name.StartsWith(namePrefix, System.StringComparison.Ordinal))
                return ball;
        }
        return null;
    }

    [ContextMenu("Reset Potted Balls")]
    public void ResetPotted()
    {
        foreach (BallInfo ball in _balls)
            if (ball != null)
                ball.potted = false;
        Debug.Log("[BallTracker] Potted flags cleared.");
    }

    /// <summary>Teleports an in-play ball (first match of prefix) into a pocket. Returns the ball or null.</summary>
    public BallInfo SimulatePot(string namePrefix, int pocketIndex = 0)
    {
        if (!_ready)
            Refresh();

        BallInfo ball = FindBall(namePrefix);
        if (ball == null)
        {
            Debug.LogWarning($"[BallTracker] No in-play ball starting with '{namePrefix}' found.");
            return null;
        }
        PocketInfo target = pocketIndex >= 0 && pocketIndex < _pockets.Count
            ? _pockets[pocketIndex]
            : (FindNearestPocket(ball.transform.position) ?? (_pockets.Count > 0 ? _pockets[0] : null));
        if (target == null)
        {
            Debug.LogWarning("[BallTracker] No pocket found to simulate into.");
            return null;
        }
        _previousY[ball.transform] = pocketMouthY + 0.01f;
        ball.transform.position = new Vector3(target.position.x, pocketMouthY - 0.15f, target.position.z);
        Debug.Log($"[BallTracker] Simulated: {ball.name} → {target.name}");
        return ball;
    }

    /// <summary>Teleports the first red ball into the nearest pocket to prove detection works.</summary>
    [ContextMenu("Simulate Pot First Red")]
    public void SimulatePotFirstRed()
    {
        Refresh();
        SimulatePot("Red");
    }

    private List<PocketInfo> DetectPockets(Transform root)
    {
        var result = new List<PocketInfo>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (!child.name.StartsWith(pocketNameFilter, StringComparison.Ordinal))
                continue;
            if (child.position.y < 0.5f) // skip floor-level pads
                continue;

            Vector3 pos = child.position;
            if (result.Exists(p => Vector3.Distance(p.position, pos) < 0.05f))
                continue;

            result.Add(new PocketInfo { name = child.name, position = pos, radius = pocketRadius });
        }

        // Mirror missing corners — the 6 snooker pockets are symmetric about the origin.
        for (int i = 0; i < result.Count; i++)
        {
            Vector3 mirrored = new Vector3(-result[i].position.x, result[i].position.y, -result[i].position.z);
            if (!result.Exists(p => Vector3.Distance(p.position, mirrored) < 0.05f))
                result.Add(new PocketInfo { name = result[i].name + " (mirror)", position = mirrored, radius = pocketRadius });
        }

        return result;
    }

    private PocketInfo FindNearestPocket(Vector3 pos)
    {
        PocketInfo best = null;
        float bestDist = float.MaxValue;
        foreach (PocketInfo pocket in _pockets)
        {
            Vector3 delta = pocket.position - pos;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = pocket;
            }
        }
        return best != null && bestDist <= best.radius ? best : null;
    }

    /// <summary>Snooker value for a ball name, or -1 if it is not a ball.</summary>
    public static int PointsForName(string name)
    {
        if (name.StartsWith("White_CueBall", StringComparison.Ordinal) || name.Equals("Cue", StringComparison.Ordinal))
            return 0;
        if (name.StartsWith("Red", StringComparison.Ordinal))
            return 1;
        if (name.StartsWith("Yellow", StringComparison.Ordinal))
            return 2;
        if (name.StartsWith("Green", StringComparison.Ordinal))
            return 3;
        if (name.StartsWith("Brown", StringComparison.Ordinal))
            return 4;
        if (name.StartsWith("Blue", StringComparison.Ordinal))
            return 5;
        if (name.StartsWith("Pink", StringComparison.Ordinal))
            return 6;
        if (name.StartsWith("Black", StringComparison.Ordinal))
            return 7;
        return -1;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.7f);
        foreach (PocketInfo pocket in _pockets)
        {
            Gizmos.DrawWireSphere(pocket.position, pocket.radius);
            Gizmos.DrawLine(pocket.position, pocket.position + Vector3.up * 0.4f);
        }

        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.8f);
        foreach (BallInfo ball in _balls)
        {
            if (ball?.transform == null)
                continue;
            Gizmos.DrawWireSphere(ball.transform.position, 0.03f);
        }
    }
}
