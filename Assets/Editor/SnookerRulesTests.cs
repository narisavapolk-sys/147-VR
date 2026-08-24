using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Automated snooker-rule tests (run from the Tools menu or batch mode):
///   Tools > 147 > Run Snooker Rule Tests
///   Unity -batchmode -executeMethod SnookerRulesTests.Run
///
/// Covers:
///   - Cue ball fouls (red on, colour on, black on, turn passes, re-spot)
///   - All six pocket positions detect a pot correctly
///   - Turn continuation: legal pot keeps the table, fouls pass the turn
///   - Wrong-ball fouls and re-spotting
/// </summary>
public static class SnookerRulesTests
{
    private static int _pass;
    private static int _fail;

    [MenuItem("Tools/147/Run Snooker Rule Tests")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

        var tracker = Object.FindObjectOfType<SnookerBallTracker>();
        var score = Object.FindObjectOfType<SnookerScoreManager>();
        var turns = Object.FindObjectOfType<SnookerTurnManager>();

        if (tracker == null || score == null || turns == null)
        {
            Debug.LogError("[RuleTests] Missing tracker/score/turn manager in scene.");
            return;
        }

        _pass = 0;
        _fail = 0;

        tracker.Refresh();
        score.Initialize(); // batch mode does not run Awake on scene objects
        score.ResetFrame();
        turns.SetTurn(1);

        TestCueBallFouls(tracker, score, turns);
        TestAllPockets(tracker, score, turns);
        TestTurnContinuation(tracker, score, turns);
        TestWrongBallFoul(tracker, score, turns);
        TestRespotting(tracker, score, turns);
        TestShotTrackerFouls(tracker, score, turns);

        Debug.Log($"[RuleTests] RESULT: {_pass} passed, {_fail} failed.");
        if (_fail > 0)
            Debug.LogError("[RuleTests] Some tests FAILED — see log above.");
    }

    // ---- Cue ball fouls ----

    private static void TestCueBallFouls(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns)
    {
        // Cue potted while red is on → opponent +4, turn passes, ball-on stays red, cue re-spotted.
        ResetFor(tracker, score, turns, 1);
        tracker.SimulatePot("White_CueBall", 0);
        tracker.ManualUpdate();
        Check(score.player2Score == 4, "cue potted (red on) → P2 +4");
        Check(turns.currentPlayer == 2, "cue potted → turn passes to P2");
        Check(score.ballOn == SnookerScoreManager.BallOn.Red, "cue foul keeps ball-on = red");
        Check(tracker.FindBall("White_CueBall") != null, "cue ball re-spotted (back on table)");

        // Cue potted while a colour is on → opponent +4 (min), turn passes.
        ResetFor(tracker, score, turns, 1);
        tracker.SimulatePot("Red", 1);
        tracker.ManualUpdate(); // red pot → ball-on colour, P1 keeps table
        Check(turns.currentPlayer == 1, "after legal red, P1 keeps table");
        tracker.SimulatePot("White_CueBall", 1);
        tracker.ManualUpdate();
        Check(score.player2Score == 4, "cue potted (colour on) → P2 +4");
        Check(turns.currentPlayer == 2, "cue foul passes turn");

        // Cue potted while black is on (colours in order) → opponent +7 (max(4,7)).
        ResetFor(tracker, score, turns, 1);
        score.ballOn = SnookerScoreManager.BallOn.ColoursInOrder;
        score.colourIndex = 5; // black
        tracker.SimulatePot("White_CueBall", 2);
        tracker.ManualUpdate();
        Check(score.player2Score == 7, "cue potted (black on) → P2 +7");
    }

    // ---- All six pockets ----

    private static void TestAllPockets(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns)
    {
        int pockets = tracker.PocketsCount();
        Check(pockets == 6, $"six pockets detected ({pockets})");

        for (int i = 0; i < 6 && i < tracker.Pockets.Count; i++)
        {
            ResetFor(tracker, score, turns, 1);
            string expected = tracker.Pockets[i].name;

            string recordedPocket = null;
            int recordedPoints = 0;
            void OnPot(string ball, string pocket, int points, int player) { recordedPocket = pocket; recordedPoints = points; }
            score.PotRecorded += OnPot;

            tracker.SimulatePot("Red", i);
            tracker.ManualUpdate();

            score.PotRecorded -= OnPot;

            Check(score.player1Score == 1, $"pocket {i} ({expected}): red potted → P1 +1");
            Check(recordedPocket == expected, $"pocket {i}: reported as '{expected}' (got '{recordedPocket}')");
            Check(recordedPoints == 1, $"pocket {i}: pot recorded +1");
            Check(score.ballOn == SnookerScoreManager.BallOn.Colour, $"pocket {i}: ball-on becomes colour");
        }
    }

    // ---- Turn continuation ----

    private static void TestTurnContinuation(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns)
    {
        // Legal red pot → striker keeps the table.
        ResetFor(tracker, score, turns, 1);
        tracker.SimulatePot("Red", 0);
        tracker.ManualUpdate();
        Check(turns.currentPlayer == 1, "legal red pot → P1 keeps the table");

        // Legal colour after the red → striker keeps the table, ball-on returns to red.
        tracker.SimulatePot("Blue", 0);
        tracker.ManualUpdate();
        Check(turns.currentPlayer == 1, "legal colour pot → P1 keeps the table");
        Check(score.player1Score == 6, "red + blue = 6 points to P1");
        Check(score.ballOn == SnookerScoreManager.BallOn.Red, "after colour, ball-on = red");

        // Foul → turn passes.
        tracker.SimulatePot("White_CueBall", 0);
        tracker.ManualUpdate();
        Check(turns.currentPlayer == 2, "foul → turn passes to P2");

        // A fresh shot with a foul by P2 passes back to P1.
        tracker.SimulatePot("Pink", 0); // P2 pots pink while red is on → foul
        tracker.ManualUpdate();
        Check(turns.currentPlayer == 1, "foul by P2 → turn returns to P1");
    }

    // ---- Wrong-ball fouls ----

    private static void TestWrongBallFoul(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns)
    {
        // Pink potted while red is on → opponent gets max(4, 1, 6) = 6.
        ResetFor(tracker, score, turns, 1);
        tracker.SimulatePot("Pink", 0);
        tracker.ManualUpdate();
        Check(score.player2Score == 6, "pink potted while red on → foul 6 to P2");
        Check(turns.currentPlayer == 2, "wrong-ball foul → turn passes");
        Check(score.ballOn == SnookerScoreManager.BallOn.Red, "wrong-ball foul keeps ball-on red");

        // Red potted while a colour is on → opponent gets max(4, 4, 1) = 4.
        ResetFor(tracker, score, turns, 1);
        tracker.SimulatePot("Red", 0);
        tracker.ManualUpdate(); // ball-on → colour
        tracker.SimulatePot("Red", 1); // second red while colour is on → foul
        tracker.ManualUpdate();
        Check(score.player2Score == 4, "red potted while colour on → foul 4 to P2");
    }

    // ---- Re-spotting ----

    private static void TestRespotting(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns)
    {
        // Colours potted during the red/colour phase come back to the table.
        ResetFor(tracker, score, turns, 1);
        var blue = tracker.FindBall("Blue");
        Vector3 home = blue.transform.position;

        tracker.SimulatePot("Red", 0);
        tracker.ManualUpdate();
        tracker.SimulatePot("Blue", 0);
        tracker.ManualUpdate();

        bool backOnTable = blue != null && blue.transform != null && blue.transform.position.y > 0.7f && !blue.potted;
        Check(backOnTable, "blue re-spotted after pot (still on table, playable)");

        bool nearHome = blue != null && Vector3.Distance(blue.transform.position, home) < 0.1f;
        Check(nearHome, "blue re-spotted near its home spot");

        // Cue ball re-spotted after a foul.
        ResetFor(tracker, score, turns, 1);
        tracker.SimulatePot("White_CueBall", 0);
        tracker.ManualUpdate();
        var cue = tracker.FindBall("White_CueBall");
        Check(cue != null && cue.transform.position.y > 0.7f, "cue ball re-spotted after foul");
    }

    // ---- Shot tracker fouls (wrong ball first, miss all, cue off table) ----

    private static void TestShotTrackerFouls(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns)
    {
        var shotTracker = Object.FindObjectOfType<SnookerShotTracker>();
        if (shotTracker == null)
        {
            Debug.LogWarning("[RuleTests] SnookerShotTracker not found — skipping shot tracker tests.");
            return;
        }

        // Test 1: Wrong ball hit first (hit colour when red is on)
        ResetFor(tracker, score, turns, 1);
        shotTracker.OnShotFired();
        var blue = tracker.FindBall("Blue");
        if (blue != null)
        {
            shotTracker.OnCueBallHitBall(blue); // blue=5, but red is on → foul
            SimulateRest(shotTracker, tracker);
            Check(score.player2Score == 5, "wrong ball first (blue when red on) → P2 +5");
            Check(turns.currentPlayer == 2, "wrong ball first → turn passes");
        }

        // Test 2: Wrong ball hit first (hit red when colour is on)
        ResetFor(tracker, score, turns, 1);
        tracker.SimulatePot("Red", 0); // pot red → ball-on = colour
        tracker.ManualUpdate();
        shotTracker.OnShotFired();
        var red = tracker.FindBall("Red");
        if (red != null)
        {
            shotTracker.OnCueBallHitBall(red); // red=1, but colour is on → foul
            SimulateRest(shotTracker, tracker);
            Check(score.player2Score == 4, "wrong ball first (red when colour on) → P2 +4 (min foul)");
        }

        // Test 3: Miss all balls (cue ball doesn't hit anything)
        ResetFor(tracker, score, turns, 1);
        shotTracker.OnShotFired();
        // Don't hit any ball
        SimulateRest(shotTracker, tracker);
        Check(score.player2Score == 4, "miss all balls → P2 +4 (min foul)");
        Check(turns.currentPlayer == 2, "miss all → turn passes");

        // Test 4: Cue ball off table
        ResetFor(tracker, score, turns, 1);
        shotTracker.OnShotFired();
        shotTracker.ForceResolveForTest(cueOnTable: false);
        Check(score.player2Score == 4, "cue ball off table → P2 +4 (min foul)");
        Check(turns.currentPlayer == 2, "cue off table → turn passes");

        // Test 5: Legal first hit (hit red when red is on) — no foul
        ResetFor(tracker, score, turns, 1);
        shotTracker.OnShotFired();
        red = tracker.FindBall("Red");
        if (red != null)
        {
            shotTracker.OnCueBallHitBall(red); // red=1, red is on → legal
            SimulateRest(shotTracker, tracker);
            Check(score.player2Score == 0, "legal first hit (red when red on) → no foul");
        }

        // Test 6: Legal first hit (hit colour when colour is on) — no foul
        ResetFor(tracker, score, turns, 1);
        tracker.SimulatePot("Red", 0);
        tracker.ManualUpdate(); // ball-on = colour
        shotTracker.OnShotFired();
        blue = tracker.FindBall("Blue");
        if (blue != null)
        {
            shotTracker.OnCueBallHitBall(blue); // blue=5, colour is on → legal
            SimulateRest(shotTracker, tracker);
            Check(score.player2Score == 0, "legal first hit (colour when colour on) → no foul");
        }

        // Test 7: Wrong ball first during colours-in-order phase
        ResetFor(tracker, score, turns, 1);
        score.ballOn = SnookerScoreManager.BallOn.ColoursInOrder;
        score.colourIndex = 0; // yellow is on
        shotTracker.OnShotFired();
        blue = tracker.FindBall("Blue");
        if (blue != null)
        {
            shotTracker.OnCueBallHitBall(blue); // blue=5, but yellow=2 is on → foul
            SimulateRest(shotTracker, tracker);
            Check(score.player2Score == 5, "wrong ball first (blue when yellow on) → P2 +5");
        }
    }

    private static void SimulateRest(SnookerShotTracker shotTracker, SnookerBallTracker tracker)
    {
        // In editor tests Time.deltaTime is 0, so we force-resolve immediately.
        // This simulates the ball having come to rest after the shot.
        shotTracker.ForceResolveForTest();
    }

    // ---- Helpers ----

    private static void ResetFor(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns, int player)
    {
        score.ResetFrame();
        turns.SetTurn(player);
        tracker.Refresh();
    }

    private static void Check(bool ok, string label)
    {
        Debug.Log($"[RuleTests] {(ok ? "PASS" : "FAIL")}: {label}");
        if (ok) _pass++; else _fail++;
    }
}
