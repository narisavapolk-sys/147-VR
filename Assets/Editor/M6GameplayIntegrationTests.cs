using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class M6GameplayIntegrationTests
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private static int _pass;
    private static int _fail;

    [MenuItem("Tools/147/M6/Run Gameplay Integration")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var tracker = Object.FindObjectOfType<SnookerBallTracker>();
        var score = Object.FindObjectOfType<SnookerScoreManager>();
        var turns = Object.FindObjectOfType<SnookerTurnManager>();
        if (tracker == null || score == null || turns == null)
        {
            Fail("required M5 gameplay managers missing");
            Finish();
            return;
        }

        var shot = Object.FindObjectOfType<SnookerShotTracker>();
        if (shot == null)
        {
            shot = score.gameObject.AddComponent<SnookerShotTracker>();
            Debug.Log("[M6] Added missing SnookerShotTracker to ScoreManager host.");
        }

        shot.ballTracker = tracker;
        shot.scoreManager = score;
        shot.turnManager = turns;
        score.ballTracker = tracker;
        score.turnManager = turns;
        shot.InitializeBindings();
        score.Initialize();
        tracker.Refresh();
        score.ResetFrame();
        turns.SetTurn(1);

        TestLegalRed(tracker, score, turns, shot);
        TestLegalColour(tracker, score, turns, shot);
        TestWrongBallFirst(tracker, score, turns, shot);
        TestMiss(tracker, score, turns, shot);
        TestCueOffTable(tracker, score, turns, shot);
        TestRespot(tracker, score, turns, shot);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Finish();
    }

    private static void TestLegalRed(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns, SnookerShotTracker shot)
    {
        Reset(tracker, score, turns, 1);
        shot.OnShotFired();
        Check(shot.ShotInProgress, "legal red shot opened");
        var red = tracker.FindBall("Red");
        shot.OnCueBallHitBall(red);
        tracker.SimulatePot("Red", 0);
        tracker.ManualUpdate();
        shot.ForceResolveForTest();
        Check(score.player1Score == 1, "legal red â†’ P1 +1");
        Check(turns.currentPlayer == 1, "legal red â†’ striker keeps turn");
        Check(score.ballOn == SnookerScoreManager.BallOn.Colour, "legal red â†’ colour on");
    }

    private static void TestLegalColour(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns, SnookerShotTracker shot)
    {
        shot.OnShotFired();
        var colour = tracker.FindBall("Blue");
        shot.OnCueBallHitBall(colour);
        tracker.SimulatePot("Blue", 0);
        tracker.ManualUpdate();
        shot.ForceResolveForTest();
        Check(score.player1Score == 6, "legal colour â†’ cumulative P1 = 6");
        Check(turns.currentPlayer == 1, "legal colour â†’ striker keeps turn");
        Check(score.ballOn == SnookerScoreManager.BallOn.Red, "legal colour â†’ red on");
    }

    private static void TestWrongBallFirst(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns, SnookerShotTracker shot)
    {
        Reset(tracker, score, turns, 1);
        shot.OnShotFired();
        var blue = tracker.FindBall("Blue");
        shot.OnCueBallHitBall(blue);
        shot.ForceResolveForTest();
        Check(score.player2Score == 5, "wrong blue first on red â†’ P2 +5");
        Check(turns.currentPlayer == 2, "wrong blue first â†’ turn passes");
    }

    private static void TestMiss(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns, SnookerShotTracker shot)
    {
        Reset(tracker, score, turns, 1);
        shot.OnShotFired();
        shot.ForceResolveForTest();
        Check(score.player2Score == 4, "miss â†’ P2 +4");
        Check(turns.currentPlayer == 2, "miss â†’ turn passes");
    }

    private static void TestCueOffTable(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns, SnookerShotTracker shot)
    {
        Reset(tracker, score, turns, 1);
        shot.OnShotFired();
        shot.ForceResolveForTest(false);
        var cue = tracker.FindBall("White_CueBall");
        Check(score.player2Score == 4, "cue off table â†’ P2 +4");
        Check(turns.currentPlayer == 2, "cue off table â†’ turn passes");
        Check(cue != null && cue.transform.position.y >= tracker.pocketMouthY, "cue off table â†’ cue restored on table");
    }

    private static void TestRespot(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns, SnookerShotTracker shot)
    {
        Reset(tracker, score, turns, 1);
        var blue = tracker.FindBall("Blue");
        Vector3 home = blue != null ? blue.homePosition : Vector3.zero;
        shot.OnShotFired();
        shot.OnCueBallHitBall(tracker.FindBall("Red"));
        tracker.SimulatePot("Red", 0);
        tracker.ManualUpdate();
        shot.ForceResolveForTest();
        shot.OnShotFired();
        shot.OnCueBallHitBall(tracker.FindBall("Blue"));
        tracker.SimulatePot("Blue", 0);
        tracker.ManualUpdate();
        shot.ForceResolveForTest();
        blue = tracker.FindBall("Blue");
        Check(blue != null && !blue.potted, "colour pot â†’ colour remains playable");
        Check(blue != null && Vector3.Distance(blue.transform.position, home) < 0.1f, "colour pot â†’ colour re-spotted");
    }

    private static void Reset(SnookerBallTracker tracker, SnookerScoreManager score, SnookerTurnManager turns, int player)
    {
        score.ResetFrame();
        turns.SetTurn(player);
        tracker.Refresh();
    }

    private static void Check(bool condition, string label)
    {
        if (condition) { _pass++; Debug.Log("[M6] PASS: " + label); }
        else { _fail++; Debug.LogError("[M6] FAIL: " + label); }
    }

    private static void Fail(string label)
    {
        _fail++;
        Debug.LogError("[M6] FATAL: " + label);
    }

    private static void Finish()
    {
        Debug.Log($"[M6] RESULT: {_pass} passed, {_fail} failed.");
        if (_fail == 0) Debug.Log("[M6 CERTIFIED] Gameplay authority integration PASS.");
        else Debug.LogError("[M6] Gameplay integration FAILED.");
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }
}
