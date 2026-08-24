using UnityEngine;

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

    private void Start()
    {
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
        if (_hasBounds)
            return true;

        Transform root = tableRoot != null ? tableRoot : transform;

        Renderer match = null;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.name == tableSurfaceName)
            {
                match = renderer;
                break;
            }
        }

        if (match == null)
        {
            Debug.LogWarning($"[PlayerViewManager] '{tableSurfaceName}' not found under '{root.name}'; falling back to first TABLE* renderer.");
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.Contains("TABLE"))
                {
                    match = renderer;
                    break;
                }
            }
        }

        if (match == null)
        {
            Debug.LogError($"[PlayerViewManager] No table renderer found under '{root.name}'.");
            return false;
        }

        _tableBounds = match.bounds;
        _hasBounds = true;
        Debug.Log($"[PlayerViewManager] Table bounds centre=({_tableBounds.center.x:F3}, {_tableBounds.center.y:F3}, {_tableBounds.center.z:F3}) size=({_tableBounds.size.x:F3}, {_tableBounds.size.y:F3}, {_tableBounds.size.z:F3})");
        return true;
    }

    private static Transform FindRigRoot()
    {
        GameObject rig = GameObject.Find("OVRCameraRig");
        return rig != null ? rig.transform : null;
    }
}
