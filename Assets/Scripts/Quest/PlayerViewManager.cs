using UnityEngine;
using PoolPhysics.Unity;
using VR147.Authority;

/// <summary>
/// Positions the XR rig (or the desktop camera as a fallback) so the full snooker table
/// is in view from the shooting player's end.
///
/// Snooker rule: the player who is shooting always plays from the D (baulk) side, so with
/// shooterOnDSide = true both players take their shots from the same D-side standing spot
/// and the rig simply tracks the current turn. Set shooterOnDSide = false to keep Player 1
/// on the D side (-X) and Player 2 on the rack side (+X), switching sides per turn.
///
/// Turns come from SnookerTurnManager (auto-advancing); the old 1 / 2 keys are removed.
/// Works without Meta XR SDK (moves Main Camera for desktop preview); with the SDK it
/// moves the OVRCameraRig root so head tracking is preserved.
/// </summary>
public sealed class PlayerViewManager : MonoBehaviour
{
    [Header("Table")]
    [Tooltip("Root to search for the playing surface (defaults to this transform).")]
    public Transform tableRoot;
    [Tooltip("Exact renderer name of the playing surface.")]
    public string tableSurfaceName = "TABLE SURFACE";

    [Header("View")]
    [Tooltip("Eye height above the floor (m).")]
    public float eyeHeight = 1.6f;
    [Tooltip("How far back the player stands from the short rail (m).")]
    public float standOffFromRail = 1.2f;
    [Tooltip("Starting player (1 = break end, 2 = rack end).")]
    public int activePlayer = 1;
    public bool applyOnStart = true;

    [Header("Turn following")]
    [Tooltip("Reposition automatically whenever SnookerTurnManager changes the turn.")]
    public bool autoFollowTurn = true;
    [Tooltip("The player who is shooting always stands on the D (baulk) side. " +
             "If false, Player 1 = D side and Player 2 = rack side, switching per turn.")]
    public bool shooterOnDSide = true;

    private Bounds _tableBounds;
    private bool _hasBounds;
    private PoolPhysicsAuthorityDirector _authority;
    private PoolPhysicsEventRunner _activeBay;

    private void Start()
    {
        _authority = PoolPhysicsAuthorityDirector.Instance;
        if (_authority != null)
        {
            _authority.ActiveChanged -= OnActiveBayChanged;
            _authority.ActiveChanged += OnActiveBayChanged;
        }
        _activeBay = ResolveActiveBay();

        if (autoFollowTurn)
        {
            SnookerTurnManager turnManager = GetComponent<SnookerTurnManager>();
            if (turnManager == null)
                turnManager = FindObjectOfType<SnookerTurnManager>();
            if (turnManager != null)
            {
                turnManager.TurnChanged += OnTurnChanged;
                activePlayer = turnManager.currentPlayer;
            }
            else
            {
                Debug.LogWarning("[PlayerViewManager] No SnookerTurnManager found — falling back to activePlayer only.");
            }
        }

        if (applyOnStart)
            PositionForPlayer(activePlayer);
    }

    private void OnDestroy()
    {
        SnookerTurnManager turnManager = GetComponent<SnookerTurnManager>();
        if (turnManager != null)
            turnManager.TurnChanged -= OnTurnChanged;
        if (_authority != null)
            _authority.ActiveChanged -= OnActiveBayChanged;
    }

    private void OnActiveBayChanged(PoolPhysicsEventRunner bay)
    {
        if (bay == null || bay == _activeBay) return;
        _activeBay = bay;
        _hasBounds = false;
        if (applyOnStart) PositionForPlayer(activePlayer);
    }

    private PoolPhysicsEventRunner ResolveActiveBay()
    {
        if (_authority == null) _authority = PoolPhysicsAuthorityDirector.Instance;
        if (_authority != null && _authority.Active != null) return _authority.Active;
        PoolPhysicsEventRunner[] bays = FindObjectsByType<PoolPhysicsEventRunner>(FindObjectsSortMode.None);
        if (bays == null || bays.Length == 0) return null;
        Transform reference = Camera.main != null ? Camera.main.transform : transform;
        PoolPhysicsEventRunner nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (PoolPhysicsEventRunner bay in bays)
        {
            if (bay == null || bay.TableRootTransform == null) continue;
            float distance = (bay.TableRootTransform.position - reference.position).sqrMagnitude;
            if (distance < nearestDistance) { nearestDistance = distance; nearest = bay; }
        }
        return nearest;
    }

    private void OnTurnChanged(int player)
    {
        PositionForPlayer(player);
    }

    [ContextMenu("Position Player 1")]
    public void PositionPlayer1() => PositionForPlayer(1);

    [ContextMenu("Position Player 2")]
    public void PositionPlayer2() => PositionForPlayer(2);

    public void PositionForPlayer(int player)
    {
        if (!FindTableBounds())
            return;

        // Snooker rule: whoever is shooting plays from the D (baulk) side.
        if (shooterOnDSide)
            player = 1;

        Vector3 center = _tableBounds.center;
        Vector3 size = _tableBounds.size;
        bool longAxisIsX = size.x >= size.z;
        float halfLen = (longAxisIsX ? size.x : size.z) * 0.5f;
        float sign = player == 1 ? -1f : 1f;

        // Standing spot just beyond the short rail, at eye height, on the table's centre line.
        Vector3 eye = longAxisIsX
            ? new Vector3(center.x + sign * (halfLen + standOffFromRail), eyeHeight, center.z)
            : new Vector3(center.x, eyeHeight, center.z + sign * (halfLen + standOffFromRail));

        // Horizontal heading toward the table centre (rig keeps pitch 0 so head tracking stays level).
        float yaw = longAxisIsX
            ? (player == 1 ? 90f : -90f)
            : (player == 1 ? 0f : 180f);

        Transform rig = FindRigRoot();
        if (rig != null)
        {
            rig.SetPositionAndRotation(eye, Quaternion.Euler(0f, yaw, 0f));
            Debug.Log($"[PlayerViewManager] Rig at ({eye.x:F2}, {eye.y:F2}, {eye.z:F2}) yaw {yaw} — player {player}");
        }
        else
        {
            // Desktop preview: look straight at the table centre so the whole table is framed.
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[PlayerViewManager] No rig and no main camera found.");
                return;
            }

            Transform t = cam.transform;
            t.position = eye;
            t.rotation = Quaternion.LookRotation(center - eye, Vector3.up);
            Debug.Log($"[PlayerViewManager] Camera at ({eye.x:F2}, {eye.y:F2}, {eye.z:F2}) facing table centre — player {player}");
        }

        activePlayer = player;
    }

    private bool FindTableBounds()
    {
        if (_hasBounds) return true;
        _activeBay = ResolveActiveBay();
        // Physics authority identifies the current bay; its table root is the playfield origin.
        if (_activeBay != null && _activeBay.TableRootTransform != null)
        {
            Vector2 size = _activeBay.PlayfieldSize;
            if (size.x > 0f && size.y > 0f)
            {
                Transform root = _activeBay.TableRootTransform;
                Vector3 p0 = root.TransformPoint(Vector3.zero);
                Vector3 p1 = root.TransformPoint(new Vector3(size.x, 0f, 0f));
                Vector3 p2 = root.TransformPoint(new Vector3(0f, 0f, size.y));
                Vector3 p3 = root.TransformPoint(new Vector3(size.x, 0f, size.y));
                Bounds bounds = new Bounds(p0, Vector3.zero);
                bounds.Encapsulate(p1); bounds.Encapsulate(p2); bounds.Encapsulate(p3);
                bounds.Expand(new Vector3(0f, 0.05f, 0f));
                _tableBounds = bounds;
                _hasBounds = true;
                Debug.Log($"[PlayerViewManager] Using active bay '{_activeBay.BayName}' playfield bounds " +
                    $"centre=({_tableBounds.center.x:F3}, {_tableBounds.center.y:F3}, {_tableBounds.center.z:F3}) " +
                    $"size=({_tableBounds.size.x:F3}, {_tableBounds.size.y:F3}, {_tableBounds.size.z:F3}).");
                return true;
            }
            Debug.LogWarning($"[PlayerViewManager] Active bay '{_activeBay.BayName}' has no valid PlayfieldSize.");
        }
        // Explicit visual root is a fallback only; Quest Setup is never searched implicitly.
        if (tableRoot == null)
        {
            Debug.LogWarning("[PlayerViewManager] No active physics bay or explicit tableRoot; view positioning skipped.");
            return false;
        }
        Renderer match = null;
        foreach (Renderer renderer in tableRoot.GetComponentsInChildren<Renderer>(true))
            if (renderer.name == tableSurfaceName) { match = renderer; break; }
        if (match == null)
            foreach (Renderer renderer in tableRoot.GetComponentsInChildren<Renderer>(true))
                if (renderer.name.IndexOf("TABLE", System.StringComparison.OrdinalIgnoreCase) >= 0) { match = renderer; break; }
        if (match == null)
        {
            Debug.LogWarning($"[PlayerViewManager] No table renderer under explicitly assigned root '{tableRoot.name}'.");
            return false;
        }
        _tableBounds = match.bounds;
        _hasBounds = true;
        Debug.Log($"[PlayerViewManager] Using explicitly assigned table root '{tableRoot.name}', renderer '{match.name}'.");
        return true;
    }

    private static Transform FindRigRoot()
    {
        GameObject rig = GameObject.Find("OVRCameraRig");
        return rig != null ? rig.transform : null;
    }
}
