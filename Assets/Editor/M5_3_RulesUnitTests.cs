using System;
using System.Collections.Generic;
using NUnit.Framework;
using M5Rules;

public sealed class M5_3_RulesUnitTests
{
    private static M5PottedBall P(int id, M5PottedBallType type)
    {
        return new M5PottedBall(id, type);
    }

    private static ShotObservation Shot(
        int seq,
        M5BallOn on,
        int reds,
        M5Colour ordered = M5Colour.None,
        M5Colour nominated = M5Colour.None,
        M5PottedBallType first = M5PottedBallType.Red,
        bool hit = true,
        bool cuePotted = false,
        bool cueOff = false,
        bool cushion = true,
        params M5PottedBall[] pots)
    {
        return new ShotObservation(seq, 1, on, reds, ordered, nominated,
            first, hit, cuePotted, cueOff, pots, cushion);
    }

    [Test]
    public void LegalRedSingleScoresOneAndMovesToColour()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(1, M5BallOn.Red, 15, pots: P(1, M5PottedBallType.Red)));
        Assert.That(d.IsLegal, Is.True);
        Assert.That(d.StrikerPoints, Is.EqualTo(1));
        Assert.That(d.StrikerContinues, Is.True);
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.NominatedColour));
        Assert.That(d.NextRedsRemaining, Is.EqualTo(14));
        Assert.That(d.BallsToRemove[0].BallId, Is.EqualTo(1));
    }

    [Test]
    public void LegalMultipleRedsScoreEachRed()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(2, M5BallOn.Red, 15,
                pots: new[] { P(1, M5PottedBallType.Red), P(2, M5PottedBallType.Red) }));
        Assert.That(d.IsLegal, Is.True);
        Assert.That(d.StrikerPoints, Is.EqualTo(2));
        Assert.That(d.NextRedsRemaining, Is.EqualTo(13));
        Assert.That(d.BallsToRemove[0].BallId, Is.EqualTo(1));
        Assert.That(d.BallsToRemove[1].BallId, Is.EqualTo(2));
    }

    [Test]
    public void RedAndColourSameStrokeIsOneFoulAndRedIsRemoved()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(3, M5BallOn.Red, 15,
                pots: new[] { P(1, M5PottedBallType.Red), P(107, M5PottedBallType.Black) }));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.OpponentPoints, Is.EqualTo(7));
        Assert.That(d.FoulReasons, Does.Contain(M5FoulReason.ColourPottedWhenRedOn));
        Assert.That(d.BallsToRespot[0].BallId, Is.EqualTo(107));
        Assert.That(d.BallsToRemove[0].BallId, Is.EqualTo(1));
        Assert.That(d.NextRedsRemaining, Is.EqualTo(14));
    }

    [Test]
    public void WrongFirstContactUsesHighestSinglePenalty()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(4, M5BallOn.Red, 15, first: M5PottedBallType.Black));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.OpponentPoints, Is.EqualTo(7));
        Assert.That(d.FoulReasons, Does.Contain(M5FoulReason.WrongFirstContact));
    }

    [Test]
    public void MissAllIsFoul()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(5, M5BallOn.Red, 15, first: M5PottedBallType.CueBall,
                hit: false, cushion: false));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.OpponentPoints, Is.EqualTo(4));
        Assert.That(d.FoulReasons, Does.Contain(M5FoulReason.Miss));
    }

    [Test]
    public void CueBallPottedRequestsInHandInD()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(6, M5BallOn.Red, 15, first: M5PottedBallType.CueBall,
                hit: false, cuePotted: true, pots: P(900, M5PottedBallType.CueBall)));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.CueBallInHandInD, Is.True);
        Assert.That(d.OpponentPoints, Is.EqualTo(4));
    }
    [Test]
    public void CueBallOffTableRequestsInHandInD()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(7, M5BallOn.Red, 15, first: M5PottedBallType.Red,
                cueOff: true));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.CueBallInHandInD, Is.True);
        Assert.That(d.OpponentPoints, Is.EqualTo(4));
    }

    [Test]
    public void NominatedBlackScoresSevenAndRespots()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(8, M5BallOn.NominatedColour, 10, nominated: M5Colour.Black,
                first: M5PottedBallType.Black,
                pots: P(107, M5PottedBallType.Black)));
        Assert.That(d.IsLegal, Is.True);
        Assert.That(d.StrikerPoints, Is.EqualTo(7));
        Assert.That(d.StrikerContinues, Is.True);
        Assert.That(d.BallsToRespot[0].BallId, Is.EqualTo(107));
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.Red));
    }

    [Test]
    public void NominatedBlackThenBlueIsFoulWithSevenPenalty()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(9, M5BallOn.NominatedColour, 10, nominated: M5Colour.Black,
                first: M5PottedBallType.Black,
                pots: P(105, M5PottedBallType.Blue)));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.OpponentPoints, Is.EqualTo(7));
        Assert.That(d.BallsToRespot[0].BallId, Is.EqualTo(105));
    }

    [Test]
    public void ColourOnRedPottedIsFoulAndRedRemainsRemoved()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(10, M5BallOn.NominatedColour, 10, nominated: M5Colour.Black,
                first: M5PottedBallType.Black,
                pots: P(4, M5PottedBallType.Red)));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.StrikerPoints, Is.EqualTo(0));
        Assert.That(d.OpponentPoints, Is.EqualTo(7));
        Assert.That(d.NextRedsRemaining, Is.EqualTo(9));
        Assert.That(d.BallsToRemove[0].BallId, Is.EqualTo(4));
    }

    [Test]
    public void ColourOnNominatedBlackAndRedIsOneFoulWithRedRemoved()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(11, M5BallOn.NominatedColour, 10, nominated: M5Colour.Black,
                first: M5PottedBallType.Black,
                pots: new[] { P(107, M5PottedBallType.Black), P(4, M5PottedBallType.Red) }));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.OpponentPoints, Is.EqualTo(7));
        Assert.That(d.FoulReasons, Does.Contain(M5FoulReason.RedPottedWhenColourOn.ToString()));
        Assert.That(d.BallsToRemove[0].BallId, Is.EqualTo(4));
        Assert.That(d.BallsToRespot[0].BallId, Is.EqualTo(107));
    }

    [Test]
    public void MultipleFoulReasonsStillAwardOneMaximumPenalty()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(12, M5BallOn.Red, 15, first: M5PottedBallType.Black,
                cuePotted: true,
                pots: new[] { P(107, M5PottedBallType.Black), P(900, M5PottedBallType.CueBall) }));
        Assert.That(d.FoulReasons.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(d.OpponentPoints, Is.EqualTo(7));
        Assert.That(d.SingleFoulPenalty, Is.EqualTo(7));
    }
    [Test]
    public void LegalNoPotWithCushionEndsTurn()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(13, M5BallOn.Red, 15, cushion: true));
        Assert.That(d.IsLegal, Is.True);
        Assert.That(d.StrikerContinues, Is.False);
        Assert.That(d.StrikerPoints, Is.EqualTo(0));
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.Red));
    }

    [Test]
    public void NoPotWithoutCushionIsStillLegalUnderStandardSnooker()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(14, M5BallOn.Red, 15, cushion: false));
        Assert.That(d.IsLegal, Is.True);
        Assert.That(d.StrikerContinues, Is.False);
        Assert.That(d.StrikerPoints, Is.EqualTo(0));
    }

    [Test]
    public void ColoursRunInAscendingOrderAfterRedsAreGone()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(15, M5BallOn.ColoursInOrder, 0, ordered: M5Colour.Yellow,
                first: M5PottedBallType.Yellow,
                pots: P(102, M5PottedBallType.Yellow)));
        Assert.That(d.IsLegal, Is.True);
        Assert.That(d.StrikerPoints, Is.EqualTo(2));
        Assert.That(d.StrikerContinues, Is.True);
        Assert.That(d.BallsToRemove[0].BallId, Is.EqualTo(102));
        Assert.That(d.NextColourIndex, Is.EqualTo(M5Colour.Green));
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.ColoursInOrder));
    }

    [Test]
    public void FinalBlackLegalEndsFrameAndDoesNotContinue()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(16, M5BallOn.ColoursInOrder, 0, ordered: M5Colour.Black,
                first: M5PottedBallType.Black,
                pots: P(107, M5PottedBallType.Black)));
        Assert.That(d.IsLegal, Is.True);
        Assert.That(d.StrikerPoints, Is.EqualTo(7));
        Assert.That(d.FrameEndCandidate, Is.True);
        Assert.That(d.StrikerContinues, Is.False);
        Assert.That(d.NextBallOn, Is.Null);
    }

    [Test]
    public void FinalBlackFoulEndsFrameAndDoesNotContinue()
    {
        var d = M5SnookerRulesEngine.Evaluate(
            Shot(17, M5BallOn.ColoursInOrder, 0, ordered: M5Colour.Black,
                first: M5PottedBallType.Yellow));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.FrameEndCandidate, Is.True);
        Assert.That(d.StrikerContinues, Is.False);
        Assert.That(d.NextBallOn, Is.Null);
        Assert.That(d.OpponentPoints, Is.EqualTo(7));
    }
    [Test]
    public void PendingPotPermutationProducesEquivalentFullDecision()
    {
        var a = M5SnookerRulesEngine.Evaluate(
            Shot(18, M5BallOn.Red, 15,
                pots: new[] { P(2, M5PottedBallType.Red), P(1, M5PottedBallType.Red) }));
        var b = M5SnookerRulesEngine.Evaluate(
            Shot(18, M5BallOn.Red, 15,
                pots: new[] { P(1, M5PottedBallType.Red), P(2, M5PottedBallType.Red) }));
        AssertDecisionEquivalent(a, b);
    }

    [Test]
    public void DuplicateSequencePreservesFullDecision()
    {
        var input = Shot(19, M5BallOn.Red, 15,
            pots: P(1, M5PottedBallType.Red));
        var first = M5SnookerRulesEngine.Evaluate(input);
        var second = M5SnookerRulesEngine.Evaluate(input);
        AssertDecisionEquivalent(first, second);
    }

    [Test]
    public void DuplicateBallIdIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Shot(
            20, M5BallOn.Red, 15,
            pots: new[] { P(1, M5PottedBallType.Red), P(1, M5PottedBallType.Red) }));
    }

    [Test]
    public void SameRedCallbackTwiceIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Shot(
            21, M5BallOn.Red, 15,
            pots: new[] { P(3, M5PottedBallType.Red), P(3, M5PottedBallType.Red) }));
    }

    [Test]
    public void ForgedBallValueIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new M5PottedBall(107, M5PottedBallType.Black, 1));
        Assert.That(P(107, M5PottedBallType.Black).Value, Is.EqualTo(7));
    }

    [Test]
    public void CueFlagAndPotListContradictionIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Shot(
            22, M5BallOn.Red, 15,
            pots: P(900, M5PottedBallType.CueBall)));
        Assert.Throws<ArgumentException>(() => Shot(
            23, M5BallOn.Red, 15, cuePotted: true));
    }

    [Test]
    public void InvalidStrikerIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShotObservation(24, 3, M5BallOn.Red, 15,
                M5Colour.None, M5Colour.None, M5PottedBallType.Red,
                true, false, false, new M5PottedBall[0], false));
    }

    [Test]
    public void RedsAboveFifteenAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShotObservation(25, 1, M5BallOn.Red, 16,
                M5Colour.None, M5Colour.None, M5PottedBallType.Red,
                true, false, false, new M5PottedBall[0], false));
    }
    [Test]
    public void RedOnWithZeroRedsIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Shot(26, M5BallOn.Red, 0));
    }

    [Test]
    public void OrderedColourMissingAfterRedsAreGoneIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Shot(27, M5BallOn.NominatedColour, 0));
    }

    [Test]
    public void MissingNominationIsRejectedAsInvalidTelemetry()
    {
        Assert.Throws<ArgumentException>(() => Shot(
            28, M5BallOn.NominatedColour, 10,
            first: M5PottedBallType.Black,
            pots: P(107, M5PottedBallType.Black)));
    }

    [Test]
    public void InvalidEnumStateIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new M5PottedBall(50, (M5PottedBallType)99));
        Assert.Throws<ArgumentException>(() => Shot(
            29, M5BallOn.NominatedColour, 10,
            nominated: (M5Colour)99,
            first: M5PottedBallType.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShotObservation(30, 1, M5BallOn.Red, 15,
                M5Colour.None, M5Colour.None, (M5PottedBallType)99,
                true, false, false, new M5PottedBall[0], false));
    }

    [Test]
    public void FirstContactCueBallWhileObjectHitIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Shot(
            31, M5BallOn.Red, 15,
            first: M5PottedBallType.CueBall, hit: true));
    }

    [Test]
    public void MoreRedsPottedThanWereAvailableIsRejectedByEngine()
    {
        var input = Shot(32, M5BallOn.Red, 1,
            pots: new[] { P(1, M5PottedBallType.Red), P(2, M5PottedBallType.Red) });
        Assert.Throws<ArgumentException>(() => M5SnookerRulesEngine.Evaluate(input));
    }


    [Test]
    public void DefaultPottedBallIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Shot(33, M5BallOn.Red, 15,
            pots: new M5PottedBall[] { default(M5PottedBall) }));
    }

    [Test]
    public void NominatedColourNoPotReturnsToRed()
    {
        var d = M5SnookerRulesEngine.Evaluate(Shot(34, M5BallOn.NominatedColour, 10,
            nominated: M5Colour.Black, first: M5PottedBallType.Black));
        Assert.That(d.IsLegal, Is.True);
        Assert.That(d.StrikerContinues, Is.False);
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.Red));
    }

    [Test]
    public void NominatedColourFoulReturnsToRedForIncomingPlayer()
    {
        var d = M5SnookerRulesEngine.Evaluate(Shot(35, M5BallOn.NominatedColour, 10,
            nominated: M5Colour.Black, first: M5PottedBallType.Black,
            pots: P(105, M5PottedBallType.Blue)));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.Red));
    }

    [Test]
    public void LastRedFoulMovesNextBallToColour()
    {
        var d = M5SnookerRulesEngine.Evaluate(Shot(36, M5BallOn.Red, 1,
            first: M5PottedBallType.Black, pots: P(1, M5PottedBallType.Red)));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.NextRedsRemaining, Is.EqualTo(0));
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.NominatedColour));
    }

    [Test]
    public void RedPottedWhenColourOnUsesDistinctFoulReason()
    {
        var d = M5SnookerRulesEngine.Evaluate(Shot(37, M5BallOn.NominatedColour, 10,
            nominated: M5Colour.Black, first: M5PottedBallType.Black,
            pots: P(4, M5PottedBallType.Red)));
        Assert.That(d.FoulReasons, Does.Contain(M5FoulReason.RedPottedWhenColourOn.ToString()));
        Assert.That(d.FoulReasons, Does.Not.Contain(M5FoulReason.ColourPottedWhenRedOn.ToString()));
    }

    [Test]
    public void FinalBlackPreservesRecoveryDataForTieResolution()
    {
        var d = M5SnookerRulesEngine.Evaluate(Shot(38, M5BallOn.ColoursInOrder, 0,
            ordered: M5Colour.Black, first: M5PottedBallType.Black,
            pots: P(107, M5PottedBallType.Black)));
        Assert.That(d.FinalBlackRecoveryCandidate, Is.True);
        Assert.That(d.FinalBlackWasPotted, Is.True);
        Assert.That(d.FinalBlackRecoveryBall.HasValue, Is.True);
        Assert.That(d.FinalBlackRecoveryBall.Value.BallId, Is.EqualTo(107));
    }

    [Test]
    public void FinalBlackFoulPreservesRecoveryPenaltyData()
    {
        var d = M5SnookerRulesEngine.Evaluate(Shot(39, M5BallOn.ColoursInOrder, 0,
            ordered: M5Colour.Black, first: M5PottedBallType.Yellow));
        Assert.That(d.FinalBlackRecoveryCandidate, Is.True);
        Assert.That(d.FinalBlackWasPotted, Is.False);
        Assert.That(d.FinalBlackRecoveryBall.HasValue, Is.False);
        Assert.That(d.FinalBlackRecoveryPenalty, Is.EqualTo(7));
    }

    [Test]
    public void MixedRedColourPermutationProducesEquivalentFullDecision()
    {
        var a = M5SnookerRulesEngine.Evaluate(Shot(40, M5BallOn.Red, 15,
            pots: new[] { P(107, M5PottedBallType.Black), P(1, M5PottedBallType.Red), P(2, M5PottedBallType.Red) }));
        var b = M5SnookerRulesEngine.Evaluate(Shot(40, M5BallOn.Red, 15,
            pots: new[] { P(2, M5PottedBallType.Red), P(1, M5PottedBallType.Red), P(107, M5PottedBallType.Black) }));
        AssertDecisionEquivalent(a, b);
    }

    [Test]
    public void LastRedTransitionsToNominatedColourPhase()
    {
        var d = M5SnookerRulesEngine.Evaluate(Shot(41, M5BallOn.Red, 1, pots: P(1, M5PottedBallType.Red), first: M5PottedBallType.Red));
        Assert.That(d.NextRedsRemaining, Is.EqualTo(0));
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.NominatedColour));
    }

    [Test]
    public void PostLastRedColourTransitionsToOrderedYellow()
    {
        var d = M5SnookerRulesEngine.Evaluate(Shot(42, M5BallOn.NominatedColour, 0, nominated: M5Colour.Black, pots: P(107, M5PottedBallType.Black), first: M5PottedBallType.Black));
        Assert.That(d.IsLegal, Is.True);
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.ColoursInOrder));
        Assert.That(d.NextColourIndex, Is.EqualTo(M5Colour.Yellow));
    }

    [Test]
    public void OrderedColourFoulKeepsSameColourOn()
    {
        var d = M5SnookerRulesEngine.Evaluate(Shot(43, M5BallOn.ColoursInOrder, 0, ordered: M5Colour.Blue, first: M5PottedBallType.Yellow));
        Assert.That(d.IsLegal, Is.False);
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.ColoursInOrder));
        Assert.That(d.NextColourIndex, Is.EqualTo(M5Colour.Blue));
    }

    [Test]
    public void FinalBlackLegalNoPotDoesNotCreateRecoveryCandidate()
    {
        var d = M5SnookerRulesEngine.Evaluate(Shot(44, M5BallOn.ColoursInOrder, 0, ordered: M5Colour.Black, first: M5PottedBallType.Black));
        Assert.That(d.IsLegal, Is.True);
        Assert.That(d.FrameEndCandidate, Is.False);
        Assert.That(d.FinalBlackRecoveryCandidate, Is.False);
        Assert.That(d.NextBallOn, Is.EqualTo(M5BallOn.ColoursInOrder));
        Assert.That(d.NextColourIndex, Is.EqualTo(M5Colour.Black));
        Assert.That(d.StrikerContinues, Is.False);
    }

    private static void AssertDecisionEquivalent(ShotDecision a, ShotDecision b)
    {
        Assert.That(b.ShotSequence, Is.EqualTo(a.ShotSequence));
        Assert.That(b.IsLegal, Is.EqualTo(a.IsLegal));
        Assert.That(b.SingleFoulPenalty, Is.EqualTo(a.SingleFoulPenalty));
        Assert.That(b.StrikerPoints, Is.EqualTo(a.StrikerPoints));
        Assert.That(b.OpponentPoints, Is.EqualTo(a.OpponentPoints));
        Assert.That(b.StrikerContinues, Is.EqualTo(a.StrikerContinues));
        Assert.That(b.NextBallOn, Is.EqualTo(a.NextBallOn));
        Assert.That(b.NextRedsRemaining, Is.EqualTo(a.NextRedsRemaining));
        Assert.That(b.NextColourIndex, Is.EqualTo(a.NextColourIndex));
        Assert.That(b.CueBallInHandInD, Is.EqualTo(a.CueBallInHandInD));
        Assert.That(b.FrameEndCandidate, Is.EqualTo(a.FrameEndCandidate));
        Assert.That(b.FinalBlackRecoveryCandidate, Is.EqualTo(a.FinalBlackRecoveryCandidate));
        Assert.That(b.FinalBlackWasPotted, Is.EqualTo(a.FinalBlackWasPotted));
        Assert.That(b.FinalBlackRecoveryPenalty, Is.EqualTo(a.FinalBlackRecoveryPenalty));
        Assert.That(b.FinalBlackRecoveryBall.HasValue, Is.EqualTo(a.FinalBlackRecoveryBall.HasValue));
        if (a.FinalBlackRecoveryBall.HasValue)
        {
            Assert.That(b.FinalBlackRecoveryBall.Value.BallId, Is.EqualTo(a.FinalBlackRecoveryBall.Value.BallId));
            Assert.That(b.FinalBlackRecoveryBall.Value.Type, Is.EqualTo(a.FinalBlackRecoveryBall.Value.Type));
            Assert.That(b.FinalBlackRecoveryBall.Value.Value, Is.EqualTo(a.FinalBlackRecoveryBall.Value.Value));
        }

        var af = new List<M5FoulReason>(a.FoulReasons);
        var bf = new List<M5FoulReason>(b.FoulReasons);
        af.Sort();
        bf.Sort();
        CollectionAssert.AreEqual(af, bf);

        AssertBallListsEquivalent(a.BallsToRespot, b.BallsToRespot);
        AssertBallListsEquivalent(a.BallsToRemove, b.BallsToRemove);
    }

    private static void AssertBallListsEquivalent(
        IReadOnlyList<M5PottedBall> a, IReadOnlyList<M5PottedBall> b)
    {
        Assert.That(b.Count, Is.EqualTo(a.Count));
        for (int i = 0; i < a.Count; i++)
        {
            Assert.That(b[i].BallId, Is.EqualTo(a[i].BallId));
            Assert.That(b[i].Type, Is.EqualTo(a[i].Type));
            Assert.That(b[i].Value, Is.EqualTo(a[i].Value));
        }
    }
}