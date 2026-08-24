using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arranges pool balls into a standard triangular rack on the table felt and
/// places the cue ball behind the head string.
///
/// Works both at edit time (right-click → Rack 9-Ball / Rack 8-Ball on the
/// balls root, or via the Inspector context menu) and at runtime (RackNineBall /
/// RackEightBall).
///
/// Ball detection: direct children of <see cref="ballsRoot"/> whose renderer
/// material is named "ballN{1..15}" or "ballCue".
/// </summary>
public class BallRack : MonoBehaviour
{
    /// <summary>Table bed (the felt). Its renderer bounds define the playing surface.</summary>
    [Tooltip("Table bed/felt object. Auto-finds one named 'tableBed' if empty.")]
    public Transform tableBed;

    /// <summary>Root holding the ball objects. Defaults to this transform.</summary>
    [Tooltip("Root object holding the balls. Defaults to this transform.")]
    public Transform ballsRoot;

    /// <summary>
    /// When true, balls are shuffled within the rack layout (1, 8, and 9 stay
    /// at their fixed positions per official rules; all other balls are randomly
    /// assigned to the remaining slots).
    /// </summary>
    [Tooltip("Shuffle non-anchor balls (1/8/9 stay fixed per rules).")]
    public bool shuffle = true;

    private const float BallDiameter = 0.05715f; // WPA 2.25"
    private static int _shuffleCount;

    private void Reset()
    {
        ballsRoot = transform;
    }

    private void Awake()
    {
        if (ballsRoot == null)
            ballsRoot = transform;
    }

    [ContextMenu("Rack 9-Ball")]
    public void RackNineBall()
    {
        Rack(true);
    }

    [ContextMenu("Rack 8-Ball")]
    public void RackEightBall()
    {
        Rack(false);
    }

    /// <summary>Standard 9-ball rack (1 apex, 9 center, rows 1-2-3-3).</summary>
    public void RackNineBallLayout()
    {
        Rack(true);
    }

    /// <summary>Standard 8-ball rack (1 apex, 8 center, corners solid+stripe, rows 1-2-3-4-5).</summary>
    public void RackEightBallLayout()
    {
        Rack(false);
    }

    private void Rack(bool nineBall)
    {
        if (ballsRoot == null)
            ballsRoot = transform;
        if (tableBed == null)
        {
            var go = GameObject.Find("tableBed");
            if (go != null)
                tableBed = go.transform;
        }
        if (tableBed == null)
        {
            Debug.LogWarning("[BallRack] No tableBed found — assign it in the Inspector.", this);
            return;
        }

        // Locate balls by material name.
        var balls = new Dictionary<int, Transform>();
        Transform cueBall = null;
        foreach (Transform child in ballsRoot)
        {
            var mr = child.GetComponent<MeshRenderer>();
            if (mr == null || mr.sharedMaterial == null)
                continue;
            string matName = mr.sharedMaterial.name;
            int ni = matName.LastIndexOf("ballN");
            if (ni >= 0 && int.TryParse(matName.Substring(ni + 5), out int num))
                balls[num] = child;
            else if (matName.EndsWith("ballCue"))
                cueBall = child;
        }

        // Playing surface from the bed bounds.
        Bounds bed = tableBed.GetComponent<Renderer>().bounds;
        float topY = bed.max.y;
        float footX = bed.center.x - bed.size.x * 0.25f; // foot spot: 1/4 of length from foot rail
        float headX = bed.center.x + bed.size.x * 0.25f; // head spot: 3/4 of length
        float centerZ = bed.center.z;
        float ballY = topY + BallDiameter * 0.5f;
        float rowSpacing = BallDiameter * Mathf.Sqrt(3f) * 0.5f;

        // Build the rack layout: (row, column, ballNumber).
        var slots = new List<(int row, int col)>();
        var fixedBalls = new Dictionary<int, int>(); // slotIndex -> ballNumber
        List<int> fillBalls;
        if (nineBall)
        {
            int[][] rows =
            {
                new[] { 1 },
                new[] { 2, 3 },
                new[] { 4, 9, 5 }, // 9-ball in the center of the rack
                new[] { 6, 7, 8 },
            };
            int idx = 0;
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    slots.Add((r, c));
                    if (rows[r][c] == 1 || rows[r][c] == 9)
                        fixedBalls[idx] = rows[r][c];
                    idx++;
                }
            fillBalls = new List<int> { 2, 3, 4, 5, 6, 7, 8 };
        }
        else
        {
            int[][] rows =
            {
                new[] { 1 },
                new[] { 3, 4 },
                new[] { 5, 6, 7 },
                new[] { 10, 11, 12, 13 },
                new[] { 9, 14, 8, 15, 2 }, // 8 in center; corners solid(2) + stripe(9)
            };
            int idx = 0;
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < rows[r].Length; c++)
                {
                    slots.Add((r, c));
                    if (rows[r][c] == 1 || rows[r][c] == 8 || rows[r][c] == 9)
                        fixedBalls[idx] = rows[r][c];
                    idx++;
                }
            fillBalls = new List<int> { 2, 3, 4, 5, 6, 7, 10, 11, 12, 13, 14, 15 };
        }

        // Shuffle fill balls if enabled.
        if (shuffle)
        {
            // System.Random seeded from time + counter ensures different shuffle on
            // every call (even two rapid calls in the same millisecond).
            System.Random rng = new System.Random(System.Environment.TickCount + _shuffleCount++);
            for (int i = fillBalls.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (fillBalls[i], fillBalls[j]) = (fillBalls[j], fillBalls[i]);
            }
        }

        // Assemble final layout: fixed balls at their slots, fill balls in the rest.
        var layout = new List<(int row, int col, int ball)>();
        int fillIdx = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            int ballNum;
            if (fixedBalls.TryGetValue(i, out int fb))
                ballNum = fb;
            else
                ballNum = fillBalls[fillIdx++];
            layout.Add((slots[i].row, slots[i].col, ballNum));
        }

        // Group layout rows for lateral centering.
        var rowSizes = new Dictionary<int, int>();
        foreach ((int row, int col, int ball) slot in layout)
            rowSizes[slot.row] = Mathf.Max(rowSizes.TryGetValue(slot.row, out int s) ? s : 0, slot.col + 1);

        foreach ((int row, int col, int ball) slot in layout)
        {
            Transform ball = balls[slot.ball];
            int size = rowSizes[slot.row];
            float lateral = (slot.col - (size - 1) * 0.5f) * BallDiameter;
            float x = footX - slot.row * rowSpacing;
            ball.position = new Vector3(x, ballY, centerZ + lateral);
        }

        // Cue ball behind the head string.
        if (cueBall != null)
        {
            cueBall.position = new Vector3(headX, ballY, centerZ);
        }
        else
        {
            Debug.LogWarning("[BallRack] Cue ball (material 'ballCue') not found.", this);
        }

        Debug.Log($"[BallRack] Racked {(nineBall ? "9-ball" : "8-ball")} — {layout.Count} balls + cue at head spot, shuffle={shuffle}.", this);
    }
}
