using NUnit.Framework;
using System.Reflection;
using M5Rules;
using UnityEngine;

public sealed class M5RuntimeTransactionTests
{
    private static GameObject MakeRig(out SnookerBallTracker tracker, out SnookerScoreManager score,
        out SnookerTurnManager turns, out SnookerShotTracker shots)
    {
        var root = new GameObject("M5TestTable");
        tracker = root.AddComponent<SnookerBallTracker>();
        tracker.tableRoot = root.transform;
        NewBall(root.transform, "White_CueBall");
        NewBall(root.transform, "Red_01");
        NewBall(root.transform, "Red_02");
        NewBall(root.transform, "Black");
        score = root.AddComponent<SnookerScoreManager>();
        turns = root.AddComponent<SnookerTurnManager>();
        shots = root.AddComponent<SnookerShotTracker>();
        shots.ballTracker = tracker;
        shots.scoreManager = score;
        shots.turnManager = turns;
        tracker.Refresh();
        score.Initialize();
        return root;
    }

    private static void NewBall(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
    }

    private static GameObject MakeCueRig(string cueName, bool includeProductionCue, out SnookerBallTracker tracker,
        out SnookerScoreManager score, out SnookerTurnManager turns, out SnookerShotTracker shots)
    {
        var root = new GameObject("M5CueIdentityTable");
        tracker = root.AddComponent<SnookerBallTracker>();
        tracker.tableRoot = root.transform;
        if (includeProductionCue) NewBall(root.transform, "White_CueBall");
        NewBall(root.transform, cueName);
        if (cueName == "Sphere.009")
            root.transform.Find(cueName).gameObject.AddComponent<Rigidbody>();
        NewBall(root.transform, "Red_01");
        NewBall(root.transform, "Black");
        score = root.AddComponent<SnookerScoreManager>();
        turns = root.AddComponent<SnookerTurnManager>();
        shots = root.AddComponent<SnookerShotTracker>();
        shots.ballTracker = tracker;
        shots.scoreManager = score;
        shots.turnManager = turns;
        tracker.Refresh();
        score.Initialize();
        return root;
    }

    private static SnookerBallTracker.BallInfo ResolveCueForTest(SnookerShotTracker shots)
    {
        var method = typeof(SnookerShotTracker).GetMethod("ResolveCueBall", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        var field = typeof(SnookerShotTracker).GetField("_cueBall", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        method.Invoke(shots, null);
        return field.GetValue(shots) as SnookerBallTracker.BallInfo;
    }

    [Test]
    public void Tracker_Resolves_ProductionCue_ByPointsZero()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeRig(out tracker, out score, out turns, out shots);
        try
        {
            var expected = tracker.FindBall("White_CueBall");
            Assert.IsNotNull(expected);
            Assert.AreEqual(0, expected.points);
            var resolved = ResolveCueForTest(shots);
            Assert.AreSame(expected, resolved);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Tracker_Resolves_CalibrationSphere009_ByPointsZero()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeCueRig("Sphere.009", false, out tracker, out score, out turns, out shots);
        try
        {
            var expected = tracker.FindBall("Sphere.009");
            Assert.IsNull(expected);
            BallInfoByPointsZeroMustExist(tracker);
            var resolved = ResolveCueForTest(shots);
            Assert.IsNotNull(resolved);
            Assert.AreEqual(0, resolved.points);
            Assert.AreEqual("Sphere.009", resolved.name);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void BallInfoByPointsZeroMustExist(SnookerBallTracker tracker)
    {
        int count = 0;
        foreach (var ball in tracker.Balls)
            if (ball != null && ball.transform != null && ball.points == 0) count++;
        Assert.AreEqual(1, count);
    }

    [Test]
    public void Tracker_Rejects_DuplicateCueIdentity()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeCueRig("Sphere.009", true, out tracker, out score, out turns, out shots);
        try
        {
            var method = typeof(SnookerShotTracker).GetMethod("ResolveCueBall", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(shots, null));
            Assert.IsNotNull(ex.InnerException);
            Assert.That(ex.InnerException, Is.TypeOf<System.InvalidOperationException>());
            StringAssert.Contains("Duplicate cue identity", ex.InnerException.Message);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Tracker_CalibrationCueOffTable_EmitsFalseCueOnTable()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeCueRig("Sphere.009", false, out tracker, out score, out turns, out shots);
        try
        {
            var cue = ResolveCueForTest(shots);
            Assert.IsNotNull(cue);
            cue.transform.position = new Vector3(0f, 0f, 0f);
            var lifecycle = root.AddComponent<M5ShotLifecycle>();
            lifecycle.ballTracker = tracker;
            shots.shotLifecycle = lifecycle;
            Assert.IsTrue(lifecycle.BeginShot());
            shots.OnShotFired();
            bool? received = null;
            shots.ShotResolved += (_, __, cueOnTable) => received = cueOnTable;
            shots.ForceResolveForTest();
            Assert.IsTrue(received.HasValue);
            Assert.IsFalse(received.Value);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Builder_Captures_StartState_And_StableId()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeRig(out tracker, out score, out turns, out shots);
        try
        {
            var builder = new M5ShotObservationBuilder(tracker);
            var start = builder.CaptureStartState(1, score, turns);
            Assert.AreEqual(1, start.ShotSequence);
            Assert.AreEqual(1, start.Striker);
            Assert.AreEqual(10, builder.StableBallId(tracker.FindBall("Red")));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Builder_Deduplicates_BallPotted_Events()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeRig(out tracker, out score, out turns, out shots);
        try
        {
            var builder = new M5ShotObservationBuilder(tracker);
            var start = builder.CaptureStartState(1, score, turns);
            var red = tracker.FindBall("Red");
            var observation = builder.Build(start, red, true, true, new[] { red, red });
            Assert.AreEqual(1, observation.PottedBalls.Count);
            Assert.AreEqual(10, observation.PottedBalls[0].BallId);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Applier_Commits_One_Score_And_One_Turn_Notification()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeRig(out tracker, out score, out turns, out shots);
        try
        {
            int scoreEvents = 0, turnEvents = 0;
            score.ScoresChanged += () => scoreEvents++;
            turns.TurnChanged += _ => turnEvents++;
            var builder = new M5ShotObservationBuilder(tracker);
            var start = builder.CaptureStartState(1, score, turns);
            var red = tracker.FindBall("Red");
            var observation = builder.Build(start, red, true, true, new[] { red });
            var decision = M5SnookerRulesEngine.Evaluate(observation);
            var applier = new M5ShotTransactionApplier(score, tracker, turns);
            Assert.IsTrue(applier.Apply(decision, start));
            Assert.AreEqual(1, score.Player1Score);
            Assert.AreEqual(1, scoreEvents);
            Assert.AreEqual(1, turnEvents);
            Assert.AreEqual(SnookerScoreManager.BallOn.Colour, score.ballOn);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Applier_CueFoul_Uses_StableCueId_And_PassesTurn()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeRig(out tracker, out score, out turns, out shots);
        try
        {
            var builder = new M5ShotObservationBuilder(tracker);
            var start = builder.CaptureStartState(1, score, turns);
            var cue = tracker.FindBall("White_CueBall");
            var observation = builder.Build(start, cue, false, false, new[] { cue });
            var decision = M5SnookerRulesEngine.Evaluate(observation);
            var applier = new M5ShotTransactionApplier(score, tracker, turns);
            Assert.IsTrue(applier.Apply(decision, start));
            Assert.AreEqual(4, score.Player2Score);
            Assert.AreEqual(2, turns.currentPlayer);
            Assert.IsFalse(cue.potted);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Applier_Rejects_MismatchedSequence_WithoutMutation_Then_Retries()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeRig(out tracker, out score, out turns, out shots);
        try
        {
            var builder = new M5ShotObservationBuilder(tracker);
            var start = builder.CaptureStartState(1, score, turns);
            var red = tracker.FindBall("Red");
            var decision = M5SnookerRulesEngine.Evaluate(builder.Build(start, red, true, true, new[] { red }));
            var applier = new M5ShotTransactionApplier(score, tracker, turns);
            var wrong = new M5ShotStartState(2, 1, start.BallOn, start.RedsRemaining, start.OrderedColour, start.NominatedColour);
            Assert.Throws<System.InvalidOperationException>(() => applier.Apply(decision, wrong));
            Assert.AreEqual(0, score.Player1Score);
            Assert.IsTrue(applier.Apply(decision, start));
            Assert.AreEqual(1, score.Player1Score);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Tracker_SequenceGuard_Ignores_Stale_Settle()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeRig(out tracker, out score, out turns, out shots);
        try
        {
            var method = typeof(SnookerShotTracker).GetMethod("OnPhysicsSettledFromContract",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(shots, new object[] { 0 });
            Assert.AreEqual(0, shots.LastResolvedSequence);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Tracker_ForceResolve_Uses_Explicit_CueObservation()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeRig(out tracker, out score, out turns, out shots);
        try
        {
            var lifecycle = root.AddComponent<M5ShotLifecycle>();
            lifecycle.ballTracker = tracker;
            shots.shotLifecycle = lifecycle;
            Assert.IsTrue(lifecycle.BeginShot());
            shots.OnShotFired();
            bool? received = null;
            shots.ShotResolved += (_, __, cueOnTable) => received = cueOnTable;
            shots.ForceResolveForTest(false);
            Assert.IsFalse(received.GetValueOrDefault(true));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Observation_Rejects_Duplicate_BallId()
    {
        Assert.Throws<System.ArgumentException>(() => new ShotObservation(1, 1, M5BallOn.Red, 15,
            M5Colour.None, M5Colour.None, M5PottedBallType.Red, true, false, false,
            new[] { new M5PottedBall(10, M5PottedBallType.Red), new M5PottedBall(10, M5PottedBallType.Red) }, false));
    }

    [Test]
    public void Rules_Produces_One_FoulPenalty_Value()
    {
        var observation = new ShotObservation(1, 1, M5BallOn.Red, 15, M5Colour.None, M5Colour.None,
            M5PottedBallType.CueBall, false, true, false,
            new[] { new M5PottedBall(1, M5PottedBallType.CueBall) }, false);
        var decision = M5SnookerRulesEngine.Evaluate(observation);
        Assert.AreEqual(4, decision.SingleFoulPenalty);
        Assert.AreEqual(2, decision.FoulReasons.Count);
    }

    [Test]
    public void FinalBlack_Recovery_Is_Reported_By_Rules()
    {
        var observation = new ShotObservation(1, 1, M5BallOn.ColoursInOrder, 0, M5Colour.Black, M5Colour.None,
            M5PottedBallType.Black, true, false, false,
            new[] { new M5PottedBall(107, M5PottedBallType.Black) }, false);
        var decision = M5SnookerRulesEngine.Evaluate(observation);
        Assert.IsTrue(decision.FinalBlackRecoveryCandidate);
        Assert.IsTrue(decision.FinalBlackRecoveryBall.HasValue);
    }

    [Test]
    public void ScoreSnapshot_Restores_All_M5_State()
    {
        var go = new GameObject("M5ScoreSnapshot");
        try
        {
            var score = go.AddComponent<SnookerScoreManager>();
            score.player1Score = 12; score.player2Score = 9; score.redsRemaining = 8;
            score.ballOn = SnookerScoreManager.BallOn.Colour; score.colourIndex = 3; score.frameOver = true;
            var snapshot = score.CaptureM5TransactionState();
            score.player1Score = 99; score.redsRemaining = 0; score.frameOver = false;
            score.RestoreM5TransactionState(snapshot);
            Assert.AreEqual(12, score.player1Score);
            Assert.AreEqual(8, score.redsRemaining);
            Assert.AreEqual(3, score.colourIndex);
            Assert.IsTrue(score.frameOver);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void TurnSnapshot_Restores_Transaction_State()
    {
        var go = new GameObject("M5TurnSnapshot");
        try
        {
            var turns = go.AddComponent<SnookerTurnManager>();
            turns.currentPlayer = 2;
            var snapshot = turns.CaptureM5TransactionState();
            turns.currentPlayer = 1;
            turns.RestoreM5TransactionState(snapshot);
            Assert.AreEqual(2, turns.currentPlayer);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void RulesTypes_Keep_Striker_In_Observation_Not_Decision()
    {
        Assert.IsNotNull(typeof(ShotObservation).GetProperty("Striker"));
        Assert.IsNull(typeof(ShotDecision).GetProperty("Striker"));
    }

    [Test]
    public void OrderedColour_Uses_ShotStart_Colour()
    {
        var observation = new ShotObservation(1, 2, M5BallOn.ColoursInOrder, 0, M5Colour.Blue, M5Colour.None,
            M5PottedBallType.Blue, true, false, false,
            new[] { new M5PottedBall(105, M5PottedBallType.Blue) }, false);
        var decision = M5SnookerRulesEngine.Evaluate(observation);
        Assert.IsTrue(decision.IsLegal);
        Assert.AreEqual(5, decision.StrikerPoints);
    }
    [Test]
    public void Applier_InvalidPlan_StopsBeforeMutation()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeRig(out tracker, out score, out turns, out shots);
        try
        {
            var builder = new M5ShotObservationBuilder(tracker);
            var start = builder.CaptureStartState(1, score, turns);
            var red1 = tracker.FindBall("Red");
            var red2 = tracker.Balls[2];
            var decision = M5SnookerRulesEngine.Evaluate(builder.Build(start, red1, true, true, new[] { red1, red2 }));
            var applier = new M5ShotTransactionApplier(score, tracker, turns);
            var saved = red2.transform;
            red2.transform = null;
            Assert.Throws<System.InvalidOperationException>(() => applier.Apply(decision, start));
            Assert.IsFalse(red1.potted);
            Assert.IsFalse(red2.potted);
            Assert.AreEqual(0, score.Player1Score);
            Assert.AreEqual(1, turns.currentPlayer);
            red2.transform = saved;
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Applier_RollsBack_AfterInjectedMutationFailure_And_AllowsRetry()
    {
        SnookerBallTracker tracker; SnookerScoreManager score; SnookerTurnManager turns; SnookerShotTracker shots;
        var root = MakeRig(out tracker, out score, out turns, out shots);
        try
        {
            int scoreEvents = 0, turnEvents = 0;
            score.ScoresChanged += () => scoreEvents++;
            turns.TurnChanged += _ => turnEvents++;
            var builder = new M5ShotObservationBuilder(tracker);
            var start = builder.CaptureStartState(1, score, turns);
            var red1 = tracker.FindBall("Red");
            var red2 = tracker.Balls[2];
            var p1 = red1.transform.position;
            var p2 = red2.transform.position;
            var decision = M5SnookerRulesEngine.Evaluate(builder.Build(start, red1, true, true, new[] { red1, red2 }));
            bool failOnce = true;
            var applier = new M5ShotTransactionApplier(score, tracker, turns, () =>
            {
                if (failOnce) { failOnce = false; throw new System.InvalidOperationException("Injected post-ball-mutation failure."); }
            });

            Assert.Throws<System.InvalidOperationException>(() => applier.Apply(decision, start));
            Assert.IsFalse(red1.potted);
            Assert.IsFalse(red2.potted);
            Assert.AreEqual(p1, red1.transform.position);
            Assert.AreEqual(p2, red2.transform.position);
            Assert.AreEqual(0, score.Player1Score);
            Assert.AreEqual(1, turns.currentPlayer);
            Assert.AreEqual(0, scoreEvents);
            Assert.AreEqual(0, turnEvents);

            Assert.IsTrue(applier.Apply(decision, start));
            Assert.AreEqual(2, score.Player1Score);
            Assert.AreEqual(1, scoreEvents);
            Assert.AreEqual(1, turnEvents);
            Assert.IsTrue(red1.potted);
            Assert.IsTrue(red2.potted);
        }
        finally { Object.DestroyImmediate(root); }
    }

}
