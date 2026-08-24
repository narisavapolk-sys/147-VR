using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds real physics to the snooker table at runtime:
///   - Every ball gets a Rigidbody + SphereCollider (existing FBX colliders are disabled).
///   - An invisible table surface lets balls roll; four rails contain them, with gaps at
///     the six pocket positions.
///   - Pocket catch triggers (just below the surface at each pocket) pull a ball in by
///     teleporting it below pocketMouthY — SnookerBallTracker then reports the pot.
///   - A catcher floor far below keeps potted balls from falling forever.
///
/// Run EnsurePhysics() once (called from Start). Works in editor/batch via the public API.
/// </summary>
public sealed class SnookerPhysicsSetup : MonoBehaviour
{
    [Header("Table")]
    [Tooltip("Root to search for balls and the playing surface.")]
    public Transform tableRoot;
    [Tooltip("Exact renderer name of the playing surface.")]
    public string tableSurfaceName = "TABLE SURFACE";

    [Header("Balls")]
    [Tooltip("Snooker ball radius (m).")]
    public float ballRadius = 0.026f;
    public float ballMass = 0.14f;
    [Range(0f, 1f)]
    public float ballBounciness = 0.8f;
    [Range(0f, 1f)]
    public float ballFriction = 0.05f;

    [Header("Rails")]
    public float railThickness = 0.06f;
    public float railHeight = 0.12f;
    [Tooltip("Half-width of the gap left in each rail for a pocket (m).")]
    public float pocketGapHalf = 0.17f;

    [Header("Pocket catch")]
    [Tooltip("Radius of the invisible catch trigger at each pocket (m).")]
    public float catchRadius = 0.15f;
    [Tooltip("Where a caught ball is placed (below pocketMouthY).")]
    public float catchY = 0.5f;

    public bool autoSetupOnStart = true;

    private Transform _surface;
    private float _surfaceTopY;
    private Bounds _tableBounds;
    private bool _setup;

    public float SurfaceTopY => _setup ? _surfaceTopY : 0f;
    public Bounds TableBounds => _tableBounds;

    private void Start()
    {
        if (autoSetupOnStart)
            EnsurePhysics();
    }

    /// <summary>Builds the physics table and ball bodies. Idempotent.</summary>
    public void EnsurePhysics()
    {
        if (_setup)
            return;

        Transform root = tableRoot != null ? tableRoot : transform;

        // Find the playing surface and derive the table plane.
        Renderer surfaceRenderer = FindSurface(root);
        if (surfaceRenderer == null)
        {
            Debug.LogError("[Physics] Playing surface not found — physics not built.");
            return;
        }
        _surface = surfaceRenderer.transform;
        _tableBounds = surfaceRenderer.bounds;
        _surfaceTopY = _tableBounds.max.y;

        // Disable any existing FBX colliders so our physics is the only one.
        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        Transform physicsRoot = new GameObject("Physics Table (runtime)").transform;
        physicsRoot.SetParent(root, false);

        BuildSurface(physicsRoot);
        BuildRails(physicsRoot);
        BuildPocketCatchers(physicsRoot);
        BuildCatcherFloor(physicsRoot);

        SetupBalls(root);

        _setup = true;
        Debug.Log($"[Physics] Ready: surface top y={_surfaceTopY:F3}, bounds=({_tableBounds.size.x:F2} x {_tableBounds.size.z:F2})");
    }

    private Renderer FindSurface(Transform root)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name == tableSurfaceName)
                return r;
        }
        return null;
    }

    private void BuildSurface(Transform parent)
    {
        GameObject go = new GameObject("Surface");
        go.transform.SetParent(parent, false);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(_tableBounds.center.x, _surfaceTopY - 0.02f, _tableBounds.center.z);
        box.size = new Vector3(_tableBounds.size.x, 0.04f, _tableBounds.size.z);
        box.sharedMaterial = MakeMaterial();
    }

    private void BuildRails(Transform parent)
    {
        float halfX = _tableBounds.size.x * 0.5f;
        float halfZ = _tableBounds.size.z * 0.5f;
        float cx = _tableBounds.center.x;
        float cz = _tableBounds.center.z;
        float y = _surfaceTopY + railHeight * 0.5f - 0.02f;

        // Long rails (along X): two segments each, leaving the middle-pocket gap at x = cx.
        BuildRail(parent, "Rail N", new Vector3(cx - (halfX - pocketGapHalf) * 0.5f, y, cz + halfZ),
            new Vector3(halfX - pocketGapHalf, railHeight, railThickness));
        BuildRail(parent, "Rail N", new Vector3(cx + (halfX - pocketGapHalf) * 0.5f, y, cz + halfZ),
            new Vector3(halfX - pocketGapHalf, railHeight, railThickness));
        BuildRail(parent, "Rail S", new Vector3(cx - (halfX - pocketGapHalf) * 0.5f, y, cz - halfZ),
            new Vector3(halfX - pocketGapHalf, railHeight, railThickness));
        BuildRail(parent, "Rail S", new Vector3(cx + (halfX - pocketGapHalf) * 0.5f, y, cz - halfZ),
            new Vector3(halfX - pocketGapHalf, railHeight, railThickness));

        // Short rails (along Z): single segment each, leaving corner gaps at both ends.
        BuildRail(parent, "Rail W", new Vector3(cx - halfX, y, cz),
            new Vector3(railThickness, railHeight, halfZ - pocketGapHalf));
        BuildRail(parent, "Rail E", new Vector3(cx + halfX, y, cz),
            new Vector3(railThickness, railHeight, halfZ - pocketGapHalf));
    }

    private static void BuildRail(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.sharedMaterial = MakeMaterial();
    }

    private void BuildPocketCatchers(Transform parent)
    {
        // Six pockets: 4 corners + 2 middle. Mirror symmetric about the table centre.
        float halfX = _tableBounds.size.x * 0.5f;
        float halfZ = _tableBounds.size.z * 0.5f;
        float cx = _tableBounds.center.x;
        float cz = _tableBounds.center.z;

        var positions = new[]
        {
            new Vector3(cx - halfX - 0.03f, _surfaceTopY - 0.02f, cz - halfZ - 0.03f), // SW corner
            new Vector3(cx - halfX - 0.03f, _surfaceTopY - 0.02f, cz + halfZ + 0.03f), // NW corner
            new Vector3(cx + halfX + 0.03f, _surfaceTopY - 0.02f, cz - halfZ - 0.03f), // SE corner
            new Vector3(cx + halfX + 0.03f, _surfaceTopY - 0.02f, cz + halfZ + 0.03f), // NE corner
            new Vector3(cx, _surfaceTopY - 0.02f, cz - halfZ - 0.03f),                 // middle S
            new Vector3(cx, _surfaceTopY - 0.02f, cz + halfZ + 0.03f),                 // middle N
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject go = new GameObject($"Pocket Catch {i}");
            go.transform.SetParent(parent, false);
            go.transform.position = positions[i];
            SphereCollider trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = catchRadius;
            var catchComp = go.AddComponent<SnookerPocketCatch>();
            catchComp.Setup(this);
        }
    }

    private void BuildCatcherFloor(Transform parent)
    {
        GameObject go = new GameObject("Catcher Floor");
        go.transform.SetParent(parent, false);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(_tableBounds.center.x, -1.5f, _tableBounds.center.z);
        box.size = new Vector3(_tableBounds.size.x + 1f, 0.1f, _tableBounds.size.z + 1f);
    }

    private void SetupBalls(Transform root)
    {
        PhysicsMaterial ballMat = MakeMaterial();

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (SnookerBallTracker.PointsForName(child.name) < 0)
                continue;

            // Remove any existing colliders on the ball mesh.
            foreach (Collider col in child.GetComponents<Collider>())
                Object.DestroyImmediate(col);

            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb == null)
                rb = child.gameObject.AddComponent<Rigidbody>();
            rb.mass = ballMass;
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.2f;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            SphereCollider sphere = child.gameObject.AddComponent<SphereCollider>();
            sphere.radius = ballRadius;
            sphere.sharedMaterial = ballMat;

            // Add collision detection to cue ball
            if (SnookerBallTracker.PointsForName(child.name) == 0)
            {
                CueBallCollision collision = child.gameObject.AddComponent<CueBallCollision>();
                collision.shotTracker = FindObjectOfType<SnookerShotTracker>();
                collision.ballTracker = FindObjectOfType<SnookerBallTracker>();
            }
        }
    }

    private static PhysicsMaterial _sharedMaterial;

    private static PhysicsMaterial MakeMaterial()
    {
        if (_sharedMaterial == null)
        {
            _sharedMaterial = new PhysicsMaterial("Snooker")
            {
                bounciness = 0.8f,
                dynamicFriction = 0.05f,
                staticFriction = 0.05f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Maximum,
            };
        }
        return _sharedMaterial;
    }

    /// <summary>Called by SnookerPocketCatch when a ball enters a pocket trigger.</summary>
    public void CatchBall(Collider ballCollider, Vector3 pocketCentre)
    {
        Rigidbody rb = ballCollider.attachedRigidbody;
        if (rb == null)
            return;
        Vector3 spot = new Vector3(pocketCentre.x, catchY, pocketCentre.z);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = spot;
        // Sync the transform immediately so the tracker sees the new position this frame.
        ballCollider.transform.position = spot;
        Debug.Log($"[Physics] Caught ball {ballCollider.name} at pocket ({pocketCentre.x:F2}, {pocketCentre.z:F2})");
    }

    private void OnDrawGizmosSelected()
    {
        if (!_setup)
            return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(_tableBounds.center, _tableBounds.size);
    }
}

/// <summary>Trigger component placed at each pocket; pulls balls in via the physics setup.</summary>
public sealed class SnookerPocketCatch : MonoBehaviour
{
    private SnookerPhysicsSetup _setup;

    public void Setup(SnookerPhysicsSetup setup) => _setup = setup;

    private void OnTriggerEnter(Collider other)
    {
        if (_setup == null || other.attachedRigidbody == null)
            return;
        if (SnookerBallTracker.PointsForName(other.name) < 0)
            return;
        _setup.CatchBall(other, transform.position);
    }
}
