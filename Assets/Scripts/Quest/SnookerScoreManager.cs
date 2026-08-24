using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Full snooker scoring state machine.
///
/// Rules implemented:
///   - Ball values: red = 1, yellow = 2, green = 3, brown = 4, blue = 5, pink = 6, black = 7.
///   - Ball-on sequence: red → colour → red → colour … while reds remain.
///   - A colour potted during the red/colour phase is RE-SPOTTED (placed back on its spot),
///     including the colour potted right after the last red.
///   - When no reds remain, colours are potted in order (yellow → green → brown → blue →
///     pink → black) and stay off the table. Potting the black ends the frame.
///   - Cue ball potted = foul: opponent gets max(4, value of ball on), turn passes, cue
///     ball is re-spotted (in-hand, back at its home position).
///   - Wrong ball potted = foul: opponent gets max(4, value of ball on, value of ball
///     potted), the potted ball is re-spotted, turn passes.
///   - Legal pot: the striker keeps the table (turn manager is told via StrikerContinues).
///
/// Fouls that depend on shot execution (hitting the wrong ball first, missing everything)
/// are outside the scope of this scoreboard — the turn manager's shot-end detection
/// handles the "no pot, turn passes" case.
/// </summary>
public sealed class SnookerScoreManager : MonoBehaviour
{
    public enum BallOn { Red, Colour, ColoursInOrder }

    /// <summary>Fired whenever either score changes (also on frame reset).</summary>
    public event System.Action ScoresChanged;

    /// <summary>Fired after a pot is resolved: (ballName, pocketName, points, player).</summary>
    public event System.Action<string, string, int, int> PotRecorded;

    private static readonly string[] ColourOrder = { "Yellow", "Green", "Brown", "Blue", "Pink", "Black" };

    [Header("Dependencies")]
    public SnookerBallTracker ballTracker;
    public SnookerTurnManager turnManager;

    [Header("Scores")]
    public int player1Score;
    public int player2Score;
    [Tooltip("Minimum foul penalty (points).")]
    public int foulPenalty = 4;

    [Header("Frame state")]
    public BallOn ballOn = BallOn.Red;
    [Tooltip("Reds still on the table (set automatically from the scene).")]
    public int redsRemaining = 15;
    [Tooltip("Index into ColourOrder while ballOn == ColoursInOrder.")]
    public int colourIndex;
    public bool frameOver;

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (ballTracker != null)
            ballTracker.BallPotted -= OnBallPotted;
    }

    /// <summary>Resolves dependencies and subscribes to the ball tracker. Idempotent.</summary>
    public void Initialize()
    {
        if (ballTracker == null)
            ballTracker = GetComponent<SnookerBallTracker>();
        if (turnManager == null)
            turnManager = GetComponent<SnookerTurnManager>();

        if (ballTracker != null)
        {
            ballTracker.BallPotted -= OnBallPotted; // avoid double-subscribe
            ballTracker.BallPotted += OnBallPotted;
        }
        else
        {
            Debug.LogWarning("[SnookerScoreManager] No SnookerBallTracker assigned — scores won't update.");
        }
    }

    private void OnBallPotted(SnookerBallTracker.BallInfo ball, SnookerBallTracker.PocketInfo pocket)
    {
        if (frameOver)
        {
            Debug.Log($"[Score] Frame over ({Player1Score}:{Player2Score}) — press Reset Frame to play again");
            return;
        }
        if (ball == null || ball.transform == null)
            return;

        int striker = turnManager != null ? turnManager.currentPlayer : 1;
        int opponent = striker == 1 ? 2 : 1;

        // Cue ball potted → foul.
        if (ball.points == 0)
        {
            int penalty = Mathf.Max(foulPenalty, BallOnValue());
            AddScore(opponent, penalty);
            Respot(ball);
            Debug.Log($"[Score] Foul! Cue ball potted in {pocket.name} — Player {opponent} awarded {penalty} points (turn passes)");
            PotRecorded?.Invoke(ball.name, pocket.name, penalty, opponent);
            turnManager?.NextTurn();
            return;
        }

        bool legal = IsLegalPot(ball);
        if (legal)
        {
            switch (ballOn)
            {
                case BallOn.Red:
                    redsRemaining--;
                    ballOn = BallOn.Colour;
                    break;

                case BallOn.Colour:
                    // Colours are re-spotted while this phase is active (incl. after the last red).
                    Respot(ball);
                    ballOn = redsRemaining > 0 ? BallOn.Red : BallOn.ColoursInOrder;
                    break;

                case BallOn.ColoursInOrder:
                    colourIndex++; // colour stays potted
                    if (colourIndex >= ColourOrder.Length)
                    {
                        frameOver = true;
                        Debug.Log($"[Score] Frame over! Player {striker} wins {Player1Score} : {Player2Score}");
                    }
                    break;
            }

            AddScore(striker, ball.points);
            Debug.Log($"[Score] {ball.name} potted in {pocket.name} — Player {striker} awarded {ball.points} points (continues)");
            PotRecorded?.Invoke(ball.name, pocket.name, ball.points, striker);
            turnManager?.StrikerContinues();
        }
        else
        {
            int penalty = Mathf.Max(foulPenalty, BallOnValue(), ball.points);
            AddScore(opponent, penalty);
            Respot(ball); // wrongly potted ball is re-spotted
            Debug.Log($"[Score] Foul! {ball.name} potted in {pocket.name} but ball on was {BallOnName()} — Player {opponent} awarded {penalty} points (turn passes)");
            PotRecorded?.Invoke(ball.name, pocket.name, penalty, opponent);
            turnManager?.NextTurn();
        }
    }

    private bool IsLegalPot(SnookerBallTracker.BallInfo ball)
    {
        switch (ballOn)
        {
            case BallOn.Red:
                return ball.points == 1;
            case BallOn.Colour:
                return ball.points >= 2; // any colour is on after a red
            case BallOn.ColoursInOrder:
                return ball.name.StartsWith(ColourOrder[colourIndex], System.StringComparison.Ordinal);
            default:
                return false;
        }
    }

    /// <summary>Value of the ball that is currently "on" (red = 1, nominated colour = 4 minimum).</summary>
    public int BallOnValue()
    {
        switch (ballOn)
        {
            case BallOn.Red:
                return 1;
            case BallOn.Colour:
                return 4;
            case BallOn.ColoursInOrder:
                return ColourValue(ColourOrder[colourIndex]);
            default:
                return 1;
        }
    }

    public string BallOnName()
    {
        switch (ballOn)
        {
            case BallOn.Red:
                return "Red";
            case BallOn.Colour:
                return "Colour";
            case BallOn.ColoursInOrder:
                return ColourOrder[colourIndex];
            default:
                return "?";
        }
    }

    public int Player1Score => player1Score;
    public int Player2Score => player2Score;

    /// <summary>Public method to add score (used by SnookerShotTracker).</summary>
    public void AddScorePublic(int player, int points)
    {
        AddScore(player, points);
    }

    /// <summary>Check if the first ball hit is legal for the current ball-on.</summary>
    public bool IsLegalFirstHit(SnookerBallTracker.BallInfo ball)
    {
        if (ball == null)
            return false;

        switch (ballOn)
        {
            case BallOn.Red:
                return ball.points == 1; // must hit red first
            case BallOn.Colour:
                return ball.points >= 2; // must hit a colour first
            case BallOn.ColoursInOrder:
                // Must hit the next colour in sequence first
                return ball.name.StartsWith(ColourOrder[colourIndex], System.StringComparison.Ordinal);
            default:
                return false;
        }
    }

    private void AddScore(int player, int points)
    {
        if (player == 1)
            player1Score += points;
        else
            player2Score += points;
        ScoresChanged?.Invoke();
    }

    // ---- Re-spotting ----

    private void Respot(SnookerBallTracker.BallInfo ball)
    {
        if (ball == null || ball.transform == null)
            return;

        Vector3 target = FindFreeSpot(ball);
        ball.transform.position = target;
        ballTracker?.Unpot(ball); // the ball is back on the table and can be potted again
        Debug.Log($"[Score] Re-spot {ball.name} → ({target.x:F2}, {target.z:F2})");
    }

    private Vector3 FindFreeSpot(SnookerBallTracker.BallInfo ball)
    {
        Vector3 home = ball.hasHomePosition ? ball.homePosition : ball.transform.position;

        if (!Occupied(home))
            return home;

        // Home spot is blocked — nearest free known home spot.
        Vector3 best = home;
        float bestDist = float.MaxValue;
        foreach (SnookerBallTracker.BallInfo other in ballTracker.Balls)
        {
            if (other == null || !other.hasHomePosition)
                continue;
            if (Occupied(other.homePosition))
                continue;
            float d = Vector3.Distance(home, other.homePosition);
            if (d < bestDist)
            {
                bestDist = d;
                best = other.homePosition;
            }
        }
        return best;
    }

    private bool Occupied(Vector3 spot)
    {
        foreach (SnookerBallTracker.BallInfo ball in ballTracker.Balls)
        {
            if (ball?.transform == null)
                continue;
            if (ball.potted)
                continue; // potted balls are off the table
            if (Vector3.Distance(ball.transform.position, spot) < 0.05f)
                return true;
        }
        return false;
    }

    private static int ColourValue(string name)
    {
        for (int i = 0; i < ColourOrder.Length; i++)
            if (name == ColourOrder[i])
                return i + 2; // yellow=2 … black=7
        return 1;
    }

    // ---- Manual controls ----

    [ContextMenu("Reset Frame")]
    public void ResetFrame()
    {
        player1Score = 0;
        player2Score = 0;
        ballOn = BallOn.Red;
        colourIndex = 0;
        frameOver = false;
        redsRemaining = 15;

        if (ballTracker != null)
        {
            // Put every ball back on its captured home spot, then re-scan.
            foreach (SnookerBallTracker.BallInfo ball in ballTracker.Balls)
            {
                if (ball?.transform != null && ball.hasHomePosition)
                    ball.transform.position = ball.homePosition;
            }
            ballTracker.ResetPotted();
            ballTracker.Refresh();
        }

        Debug.Log("[Score] Frame reset: Score 0 : 0, Reds 15, Ball on: Red");
        ScoresChanged?.Invoke();
    }

    [ContextMenu("Log Scores")]
    public void LogScores()
    {
        Debug.Log($"[Score] Player 1: {player1Score}  |  Player 2: {player2Score}  |  Ball on: {BallOnName()}  |  Reds left: {redsRemaining}");
    }
}
