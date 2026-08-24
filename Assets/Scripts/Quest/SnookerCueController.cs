using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRDevice = UnityEngine.XR.InputDevice;

/// <summary>
/// A virtual cue stick. The player aims at the table, charges power, then strikes the
/// cue ball with real physics (SnookerPhysicsSetup must be present).
///
/// Controls:
///   Desktop: move the mouse to aim (ray from the camera through the cursor onto the
///            table plane). Hold the LEFT mouse button to charge, release to shoot.
///            Right mouse button cancels the charge. The 1/2 keys still switch players.
///   XR:      the cue follows the right-hand controller (ray down onto the table).
///            Hold the trigger to charge, release to shoot.
///
/// The shot direction is projected onto the table plane, so the cue ball always stays
/// on the table. Aiming point is clamped to the table surface bounds.
/// </summary>
public sealed class SnookerCueController : MonoBehaviour
{
    [Header("Cue")]
    [Tooltip("Visible cue length (m).")]
    public float cueLength = 1.4f;
    [Tooltip("Cue radius (m).")]
    public float cueRadius = 0.015f;
    [Tooltip("How far the tip sits from the cue ball centre before the shot (m).")]
    public float tipGap = 0.09f;
    [Tooltip("How far the cue pulls back at full charge (m).")]
    public float pullBackMax = 0.4f;

    [Header("Shooting")]
    [Tooltip("Maximum cue ball speed at full charge (m/s).")]
    public float maxShotSpeed = 8f;
    [Tooltip("Charge rate per second (0..1).")]
    public float chargeRate = 1.2f;

    [Header("Input")]
    public bool useXRIfAvailable = true;

    private SnookerBallTracker _tracker;
    private SnookerPhysicsSetup _physics;
    private SnookerShotTracker _shotTracker;
    private Camera _camera;
    private Transform _cueBall;
    private Rigidbody _cueBallRb;

    private Transform _cueVisual;
    private Vector3 _aimPoint;
    private bool _hasAim;
    private float _charge;
    private bool _charging;
    private bool _xrActive;

    private void Start()
    {
        _tracker = GetComponent<SnookerBallTracker>();
        if (_tracker == null)
            _tracker = FindObjectOfType<SnookerBallTracker>();
        _physics = GetComponent<SnookerPhysicsSetup>();
        if (_physics == null)
            _physics = FindObjectOfType<SnookerPhysicsSetup>();
        _shotTracker = GetComponent<SnookerShotTracker>();
        if (_shotTracker == null)
            _shotTracker = FindObjectOfType<SnookerShotTracker>();

        CreateCueVisual();

        _xrActive = useXRIfAvailable && XRDevicePresent();
        if (_xrActive)
            Debug.Log("[Cue] XR controller detected — cue follows right hand.");
        else
            Debug.Log("[Cue] Desktop mode — move mouse to aim, hold LMB to charge, release to shoot.");
    }

    private void Update()
    {
        // Ensure physics is ready (scene components may start in any order).
        if (_physics != null)
            _physics.EnsurePhysics();
        if (_tracker != null)
            _tracker.RefreshIfNeeded();

        FindCueBall();
        if (_cueBall == null)
            return;

        if (_xrActive)
            UpdateXr();
        else
            UpdateDesktop();

        UpdateCueVisual();
    }

    // ---- Aiming ----

    private void UpdateDesktop()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return;

        Plane tablePlane = new Plane(Vector3.up, _physics.SurfaceTopY);
        Ray ray = _camera.ScreenPointToRay(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero);

        if (tablePlane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            _aimPoint = ClampToTable(hit);
            _hasAim = true;
        }

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _charging = true;
            _charge = 0f;
        }
        else if (Mouse.current.leftButton.isPressed && _charging)
        {
            _charge = Mathf.Min(1f, _charge + chargeRate * Time.deltaTime);
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame && _charging)
        {
            _charging = false;
            Shoot(_charge);
            _charge = 0f;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            _charging = false;
            _charge = 0f;
        }
    }

    private void UpdateXr()
    {
        XRDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!device.isValid)
            return;

        if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 pos))
            return;
        if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion rot))
            return;

        // Aim along the controller's forward, projected onto the table plane.
        Vector3 dir = rot * Vector3.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            dir.Normalize();

        Plane tablePlane = new Plane(Vector3.up, _physics.SurfaceTopY);
        Vector3 ball = _cueBall.position;
        Ray aimRay = new Ray(ball + dir * 10f, -dir);
        if (tablePlane.Raycast(aimRay, out float enter))
        {
            _aimPoint = ClampToTable(aimRay.GetPoint(enter));
            _hasAim = true;
        }

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool trigger))
        {
            if (trigger && !_charging)
            {
                _charging = true;
                _charge = 0f;
            }
            else if (trigger && _charging)
            {
                _charge = Mathf.Min(1f, _charge + chargeRate * Time.deltaTime);
            }
            else if (!trigger && _charging)
            {
                _charging = false;
                Shoot(_charge);
                _charge = 0f;
            }
        }
    }

    // ---- Shooting ----

    public void Shoot(float power)
    {
        power = Mathf.Clamp01(power);
        FindCueBall(); // ensure we have the cue ball even before the first Update
        if (_cueBallRb == null || !_hasAim)
            return;

        Vector3 from = _cueBall.position;
        from.y = 0f;
        Vector3 to = _aimPoint;
        to.y = 0f;
        Vector3 dir = to - from;
        if (dir.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("[Cue] Aim point is at the cue ball — nothing to shoot at.");
            return;
        }
        dir.Normalize();

        _cueBallRb.linearVelocity = dir * (power * maxShotSpeed);
        Debug.Log($"[Cue] Shot! power={power:F2} speed={power * maxShotSpeed:F2} m/s dir=({dir.x:F2}, {dir.z:F2})");

        // Notify shot tracker that a shot has been fired
        if (_shotTracker != null)
            _shotTracker.OnShotFired();

        // Enable cue ball collision detection
        CueBallCollision cbc = _cueBall.GetComponent<CueBallCollision>();
        if (cbc != null)
            cbc.EnableDetection();
    }

    /// <summary>Strikes at full power toward the given aim point (used by tests).</summary>
    public void ShootAt(Vector3 aimPoint, float power)
    {
        _aimPoint = aimPoint;
        _hasAim = true;
        Shoot(power);
    }

    // ---- Helpers ----

    private void FindCueBall()
    {
        if (_cueBall != null && _cueBallRb != null)
            return;

        // Resolve lazily (Start may not have run in batch/editor tooling).
        if (_tracker == null)
        {
            _tracker = GetComponent<SnookerBallTracker>();
            if (_tracker == null)
                _tracker = FindObjectOfType<SnookerBallTracker>();
        }
        if (_tracker == null)
            return;
        _tracker.RefreshIfNeeded();

        SnookerBallTracker.BallInfo cue = _tracker.FindBall("White_CueBall");
        if (cue?.transform != null)
        {
            _cueBall = cue.transform;
            _cueBallRb = _cueBall.GetComponent<Rigidbody>();
        }
    }

    private void CreateCueVisual()
    {
        _cueVisual = new GameObject("Cue Stick (runtime)").transform;
        _cueVisual.SetParent(transform, false);

        // Shaft: a long thin cylinder.
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Cue Shaft";
        shaft.transform.SetParent(_cueVisual, false);
        shaft.transform.localScale = new Vector3(cueRadius * 2f, cueLength * 0.5f, cueRadius * 2f);
        shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lie along Z
        Collider col = shaft.GetComponent<Collider>();
        if (col != null)
            Object.Destroy(col);
        Renderer renderer = shaft.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.55f, 0.38f, 0.2f)
            };

        // Tip: small sphere at the front.
        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tip.name = "Cue Tip";
        tip.transform.SetParent(_cueVisual, false);
        tip.transform.localScale = Vector3.one * (cueRadius * 2.4f);
        tip.transform.localPosition = new Vector3(0f, 0f, cueLength * 0.5f);
        Collider tipCol = tip.GetComponent<Collider>();
        if (tipCol != null)
            Object.Destroy(tipCol);

        _cueVisual.gameObject.SetActive(true);
    }

    private void UpdateCueVisual()
    {
        if (_cueVisual == null || _cueBall == null || !_hasAim)
            return;

        Vector3 from = _cueBall.position;
        from.y = 0f;
        Vector3 to = _aimPoint;
        to.y = 0f;
        Vector3 dir = to - from;
        if (dir.sqrMagnitude < 0.0001f)
            return;
        dir.Normalize();

        // Tip position: in front of the cue ball, pulled back by charge.
        float pull = _charging ? _charge * pullBackMax : 0f;
        Vector3 tipPos = from + dir * (tipGap + pull);
        tipPos.y = _cueBall.position.y;

        _cueVisual.position = tipPos + dir * (cueLength * 0.5f - tipGap - pull);
        _cueVisual.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // Visual feedback: colour shifts toward red as power grows.
        Renderer r = _cueVisual.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            float t = _charging ? _charge : 0f;
            r.material.color = Color.Lerp(new Color(0.55f, 0.38f, 0.2f), new Color(0.9f, 0.2f, 0.15f), t);
        }
    }

    private Vector3 ClampToTable(Vector3 p)
    {
        Bounds b = _physics.TableBounds;
        p.x = Mathf.Clamp(p.x, b.min.x + 0.05f, b.max.x - 0.05f);
        p.z = Mathf.Clamp(p.z, b.min.z + 0.05f, b.max.z - 0.05f);
        p.y = _physics.SurfaceTopY;
        return p;
    }

    private static bool XRDevicePresent()
    {
        var devices = new System.Collections.Generic.List<XRDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
        foreach (XRDevice device in devices)
        {
            if (device.isValid)
                return true;
        }
        return false;
    }
}
