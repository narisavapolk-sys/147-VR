using System;
using System.Collections.Generic;

namespace M5Rules
{
    // Rule authority: WPBSA Official Rulebook 2024-25, Section 3.
    // Phase A excludes free-ball, foul-and-miss replacement, and referee-discretion cases.
    public static class M5SnookerRulesEngine
    {
        private static readonly M5Colour[] OrderedColours =
        {
            M5Colour.Yellow, M5Colour.Green, M5Colour.Brown,
            M5Colour.Blue, M5Colour.Pink, M5Colour.Black
        };

        public static ShotDecision Evaluate(ShotObservation observation)
        {
            if (observation == null) throw new ArgumentNullException(nameof(observation));

            int redPots = Count(observation, M5PottedBallType.Red);
            if (redPots > observation.RedsRemainingAtStart)
                throw new ArgumentException("More reds were observed potted than remained at shot start.");

            var fouls = new List<M5FoulReason>();
            var respot = new List<M5PottedBall>();
            var remove = new List<M5PottedBall>();
            var colourPots = GetColourPots(observation);
            int nextReds = observation.RedsRemainingAtStart - redPots;
            int penalty = BasePenalty(observation);
            bool inHand = observation.CueBallPotted || observation.CueBallOffTable;

            if (observation.CueBallPotted)
                AddFoul(fouls, M5FoulReason.CueBallPotted);
            if (observation.CueBallOffTable)
                AddFoul(fouls, M5FoulReason.CueBallOffTable);
            if (!observation.HitAnyObjectBall)
                AddFoul(fouls, M5FoulReason.Miss);
            else if (observation.FirstContact != ExpectedFirstContact(observation))
                AddFoul(fouls, M5FoulReason.WrongFirstContact);

            int strikerPoints = 0;
            bool continues = false;
            M5BallOn? nextBallOn = observation.BallOnAtStart;
            M5Colour nextColour = observation.ColourIndexAtStart;

            if (observation.BallOnAtStart == M5BallOn.Red)
            {
                EvaluateRedPhase(observation, colourPots, redPots, fouls,
                    remove, respot, ref strikerPoints, ref continues, ref nextBallOn);
            }
            else if (observation.BallOnAtStart == M5BallOn.NominatedColour)
            {
                EvaluateNominatedColourPhase(observation, colourPots, redPots, fouls,
                    remove, respot, ref strikerPoints, ref continues,
                    ref nextBallOn, ref nextColour);
            }
            else
            {
                EvaluateOrderedColourPhase(observation, colourPots, fouls,
                    remove, respot, ref strikerPoints, ref continues,
                    ref nextBallOn, ref nextColour);
            }

            bool legal = fouls.Count == 0;
            if (!legal)
            {
                strikerPoints = 0;
                continues = false;
                penalty = Math.Min(7, Math.Max(penalty, MinimumPenalty(observation)));
            }
            else
            {
                penalty = 0;
            }

            bool finalBlackState = observation.RedsRemainingAtStart == 0 &&
                                   observation.ColourIndexAtStart == M5Colour.Black;
            bool finalBlackWasPotted = Contains(colourPots, M5Colour.Black);
            M5PottedBall? finalBlackRecoveryBall = null;
            if (finalBlackWasPotted)
            {
                foreach (var pot in colourPots)
                    if (pot.AsColour() == M5Colour.Black) { finalBlackRecoveryBall = pot; break; }
            }
            bool frameEnd = finalBlackState &&
                            (finalBlackWasPotted || fouls.Count > 0);
            if (frameEnd)
            {
                continues = false;
                nextBallOn = null;
                if (!legal)
                {
                    respot.Clear();
                    remove.Clear();
                }
            }

            SortByBallId(respot);
            SortByBallId(remove);

            return new ShotDecision(
                observation.ShotSequence, legal, fouls, penalty,
                strikerPoints, penalty, continues, nextBallOn, nextReds,
                nextColour, respot, remove, inHand, frameEnd,
                frameEnd, finalBlackWasPotted, finalBlackRecoveryBall,
                frameEnd && !legal ? penalty : 0);
        }
        private static void EvaluateRedPhase(
            ShotObservation o,
            List<M5PottedBall> colourPots,
            int redPots,
            List<M5FoulReason> fouls,
            List<M5PottedBall> remove,
            List<M5PottedBall> respot,
            ref int points,
            ref bool continues,
            ref M5BallOn? nextBallOn)
        {
            foreach (var pot in colourPots)
            {
                AddFoul(fouls, M5FoulReason.ColourPottedWhenRedOn);
                respot.Add(pot);
            }

            foreach (var pot in o.PottedBalls)
                if (pot.Type == M5PottedBallType.Red)
                    remove.Add(pot);

            if (fouls.Count == 0 && redPots > 0)
            {
                points = redPots;
                continues = true;
                nextBallOn = M5BallOn.NominatedColour;
            }
            else if (fouls.Count == 0)
            {
                continues = false;
                nextBallOn = M5BallOn.Red;
            }
            else if (redPots > 0 && o.RedsRemainingAtStart - redPots == 0)
            {
                nextBallOn = M5BallOn.NominatedColour;
            }
        }

        private static void EvaluateNominatedColourPhase(
            ShotObservation o,
            List<M5PottedBall> colourPots,
            int redPots,
            List<M5FoulReason> fouls,
            List<M5PottedBall> remove,
            List<M5PottedBall> respot,
            ref int points,
            ref bool continues,
            ref M5BallOn? nextBallOn,
            ref M5Colour nextColour)
        {
            M5Colour target = o.NominatedColour;
            if (!ShotObservation.IsColour(target))
                AddFoul(fouls, M5FoulReason.MissingNomination);
            if (redPots > 0)
            {
                AddFoul(fouls, M5FoulReason.RedPottedWhenColourOn);
                foreach (var pot in o.PottedBalls)
                    if (pot.Type == M5PottedBallType.Red) remove.Add(pot);
            }
            foreach (var pot in colourPots)
                if (pot.AsColour() != target) AddFoul(fouls, M5FoulReason.WrongNominatedColour);
            M5PottedBall correctPot = default(M5PottedBall);
            bool hasCorrectPot = false;
            foreach (var pot in colourPots)
                if (pot.AsColour() == target) { correctPot = pot; hasCorrectPot = true; break; }
            if (fouls.Count == 0 && hasCorrectPot)
            {
                points = correctPot.Value;
                continues = true;
                respot.Add(correctPot);
                if (o.RedsRemainingAtStart - redPots > 0)
                    nextBallOn = M5BallOn.Red;
                else
                {
                    nextBallOn = M5BallOn.ColoursInOrder;
                    nextColour = M5Colour.Yellow;
                }
            }
            else if (fouls.Count == 0)
            {
                continues = false;
                nextBallOn = o.RedsRemainingAtStart - redPots > 0
                    ? M5BallOn.Red : M5BallOn.ColoursInOrder;
                if (nextBallOn == M5BallOn.ColoursInOrder) nextColour = M5Colour.Yellow;
            }
            else
            {
                foreach (var pot in colourPots) respot.Add(pot);
                nextBallOn = o.RedsRemainingAtStart - redPots > 0
                    ? M5BallOn.Red : M5BallOn.ColoursInOrder;
                if (nextBallOn == M5BallOn.ColoursInOrder) nextColour = M5Colour.Yellow;
            }
        }

        private static void EvaluateOrderedColourPhase(
            ShotObservation o,
            List<M5PottedBall> colourPots,
            List<M5FoulReason> fouls,
            List<M5PottedBall> remove,
            List<M5PottedBall> respot,
            ref int points,
            ref bool continues,
            ref M5BallOn? nextBallOn,
            ref M5Colour nextColour)
        {
            M5Colour target = o.ColourIndexAtStart;
            if (!ShotObservation.IsColour(target))
                AddFoul(fouls, M5FoulReason.WrongFirstContact);
            foreach (var pot in colourPots)
                if (pot.AsColour() != target) AddFoul(fouls, M5FoulReason.WrongNominatedColour);
            M5PottedBall correctPot = default(M5PottedBall);
            bool hasCorrectPot = false;
            foreach (var pot in colourPots)
                if (pot.AsColour() == target) { correctPot = pot; hasCorrectPot = true; break; }
            if (fouls.Count == 0 && hasCorrectPot)
            {
                points = correctPot.Value;
                continues = true;
                remove.Add(correctPot);
                nextColour = NextOrderedColour(target);
                nextBallOn = nextColour == M5Colour.None ? null : M5BallOn.ColoursInOrder;
            }
            else
            {
                continues = false;
                nextBallOn = M5BallOn.ColoursInOrder;
                nextColour = target;
                foreach (var pot in colourPots) respot.Add(pot);
            }
        }
        private static M5PottedBallType ExpectedFirstContact(ShotObservation o)
        {
            switch (o.BallOnAtStart)
            {
                case M5BallOn.Red:
                    return M5PottedBallType.Red;

                case M5BallOn.NominatedColour:
                    return ToPottedType(o.NominatedColour);

                case M5BallOn.ColoursInOrder:
                    return ToPottedType(o.ColourIndexAtStart);

                default:
                    throw new ArgumentOutOfRangeException(nameof(o.BallOnAtStart));
            }
        }

        private static int MinimumPenalty(ShotObservation o)
        {
            int value = o.BallOnAtStart == M5BallOn.Red
                ? 1
                : (o.BallOnAtStart == M5BallOn.NominatedColour
                    ? (int)o.NominatedColour
                    : (int)o.ColourIndexAtStart);
            return Math.Max(4, Math.Min(7, value));
        }

        private static int BasePenalty(ShotObservation o)
        {
            int max = MinimumPenalty(o);
            if (o.HitAnyObjectBall)
                max = Math.Max(max, M5PottedBall.GetValue(o.FirstContact));
            foreach (var pot in o.PottedBalls)
                max = Math.Max(max, pot.Value);
            return Math.Min(7, max);
        }

        private static List<M5PottedBall> GetColourPots(ShotObservation o)
        {
            var result = new List<M5PottedBall>();
            foreach (var pot in o.PottedBalls)
                if (pot.AsColour() != M5Colour.None)
                    result.Add(pot);
            return result;
        }
        private static int Count(ShotObservation o, M5PottedBallType type)
        {
            int count = 0;
            foreach (var pot in o.PottedBalls)
                if (pot.Type == type)
                    count++;
            return count;
        }

        private static bool Contains(List<M5PottedBall> pots, M5Colour colour)
        {
            foreach (var pot in pots)
                if (pot.AsColour() == colour)
                    return true;
            return false;
        }

        private static void AddFoul(List<M5FoulReason> fouls, M5FoulReason reason)
        {
            if (!fouls.Contains(reason))
                fouls.Add(reason);
        }

        private static void SortByBallId(List<M5PottedBall> balls)
        {
            balls.Sort((a, b) => a.BallId.CompareTo(b.BallId));
        }

        private static M5Colour NextOrderedColour(M5Colour current)
        {
            for (int i = 0; i < OrderedColours.Length; i++)
            {
                if (OrderedColours[i] == current)
                    return i + 1 < OrderedColours.Length
                        ? OrderedColours[i + 1]
                        : M5Colour.None;
            }
            return M5Colour.None;
        }

        private static M5PottedBallType ToPottedType(M5Colour colour)
        {
            switch (colour)
            {
                case M5Colour.Yellow: return M5PottedBallType.Yellow;
                case M5Colour.Green: return M5PottedBallType.Green;
                case M5Colour.Brown: return M5PottedBallType.Brown;
                case M5Colour.Blue: return M5PottedBallType.Blue;
                case M5Colour.Pink: return M5PottedBallType.Pink;
                case M5Colour.Black: return M5PottedBallType.Black;
                default: throw new ArgumentException("A real colour is required.", nameof(colour));
            }
        }
    }
}