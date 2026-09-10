using UnityEngine;

/// <summary>
/// Places the two display characters near the centre pocket area.
/// The same positions are also serialized in SampleScene so the editor view matches Play mode.
///
/// Characters are pushed back from the table by <see cref="backOffset"/> (default ~3 steps = 1.5 m)
/// so they do not block the shooting player's view.
/// </summary>
public sealed class QuestSpawnSetup : MonoBehaviour
{
    [Header("Near Center Pocket Spawn Points")]
    public Transform player1Spawn;
    public Transform player2Spawn;
    public string player1Name = "CuteGirl_Dancing";
    public string player2Name = "ChubbyGirl_Dancing";
    public bool applyOnStart = true;

    [Header("Offset from table")]
    [Tooltip("How far (metres) to push each character back from their spawn point along the table's negative-Z axis. Default ~1.5 m ≈ 3 steps.")]
    public float backOffset = 1.5f;

    private void Start()
    {
        if (applyOnStart)
            ApplySpawnPositions();
    }

    [ContextMenu("Apply Spawn Positions")]
    public void ApplySpawnPositions()
    {
        Place(player1Name, player1Spawn);
        Place(player2Name, player2Spawn);
    }

    private void Place(string objectName, Transform spawn)
    {
        if (spawn == null)
        {
            Debug.LogWarning($"[QuestSpawnSetup] Spawn point missing for {objectName}.");
            return;
        }

        GameObject player = GameObject.Find(objectName);
        if (player == null)
        {
            Debug.LogWarning($"[QuestSpawnSetup] Could not find {objectName}.");
            return;
        }

        // Push the character back along the spawn's local -Z axis (away from the table)
        Vector3 offsetPos = spawn.position + spawn.forward * (-backOffset);
        player.transform.SetPositionAndRotation(offsetPos, spawn.rotation);
        Debug.Log($"[QuestSpawnSetup] Placed {objectName} at ({offsetPos.x:F2}, {offsetPos.y:F2}, {offsetPos.z:F2}) — offset {backOffset:F2} m back from table.");
    }
}
