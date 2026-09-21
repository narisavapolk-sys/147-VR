using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace M5Rules
{
    public enum M5BallOn { Red, NominatedColour, ColoursInOrder }

    public enum M5Colour
    {
        None = 0,
        Yellow = 2,
        Green = 3,
        Brown = 4,
        Blue = 5,
        Pink = 6,
        Black = 7
    }

    public enum M5PottedBallType
    {
        Red,
        Yellow,
        Green,
        Brown,
        Blue,
        Pink,
        Black,
        CueBall
    }

    public enum M5FoulReason
    {
        Miss,
        WrongFirstContact,
        CueBallPotted,
        CueBallOffTable,
        MissingNomination,
        WrongNominatedColour,
        ColourPottedWhenRedOn,
        RedPottedWhenColourOn,
        InvalidPotObservation
    }
    public readonly struct M5PottedBall
    {
        public int BallId { get; }
        public M5PottedBallType Type { get; }
        public int Value { get; }

        public M5PottedBall(int ballId, M5PottedBallType type)
        {
            if (ballId <= 0) throw new ArgumentOutOfRangeException(nameof(ballId));
            ValidateType(type);
            BallId = ballId;
            Type = type;
            Value = GetValue(type);
        }

        public M5PottedBall(int ballId, M5PottedBallType type, int value)
            : this(ballId, type)
        {
            if (value != Value) throw new ArgumentException(nameof(value));
        }

        public static int GetValue(M5PottedBallType type)
        {
            switch (type)
            {
                case M5PottedBallType.Red: return 1;
                case M5PottedBallType.Yellow: return 2;
                case M5PottedBallType.Green: return 3;
                case M5PottedBallType.Brown: return 4;
                case M5PottedBallType.Blue: return 5;
                case M5PottedBallType.Pink: return 6;
                case M5PottedBallType.Black: return 7;
                case M5PottedBallType.CueBall: return 4;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
        public M5Colour AsColour()
        {
            switch (Type)
            {
                case M5PottedBallType.Yellow: return M5Colour.Yellow;
                case M5PottedBallType.Green: return M5Colour.Green;
                case M5PottedBallType.Brown: return M5Colour.Brown;
                case M5PottedBallType.Blue: return M5Colour.Blue;
                case M5PottedBallType.Pink: return M5Colour.Pink;
                case M5PottedBallType.Black: return M5Colour.Black;
                default: return M5Colour.None;
            }
        }

        private static void ValidateType(M5PottedBallType type)
        {
            if ((int)type < (int)M5PottedBallType.Red ||
                (int)type > (int)M5PottedBallType.CueBall)
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public sealed class ShotObservation
    {
        public int ShotSequence { get; }
        public int Striker { get; }
        public M5BallOn BallOnAtStart { get; }
        public int RedsRemainingAtStart { get; }
        public M5Colour ColourIndexAtStart { get; }
        public M5Colour NominatedColour { get; }
        public M5PottedBallType FirstContact { get; }
        public bool HitAnyObjectBall { get; }
        public bool CueBallPotted { get; }
        public bool CueBallOffTable { get; }
        public ReadOnlyCollection<M5PottedBall> PottedBalls { get; }
        // Reserved for Shoot Out rules; standard snooker Phase A does not use cushion requirements.
        public bool AnyBallHitCushionAfterFirstContact { get; }

        public ShotObservation(
            int shotSequence,
            int striker,
            M5BallOn ballOnAtStart,
            int redsRemainingAtStart,
            M5Colour colourIndexAtStart,
            M5Colour nominatedColour,
            M5PottedBallType firstContact,
            bool hitAnyObjectBall,
            bool cueBallPotted,
            bool cueBallOffTable,
            IEnumerable<M5PottedBall> pottedBalls,
            bool anyBallHitCushionAfterFirstContact)
        {
            ValidateState(
                shotSequence, striker, ballOnAtStart, redsRemainingAtStart,
                colourIndexAtStart, nominatedColour, firstContact,
                hitAnyObjectBall, cueBallPotted, cueBallOffTable);

            var copied = new List<M5PottedBall>(
                pottedBalls ?? Array.Empty<M5PottedBall>());
            ValidatePots(copied, cueBallPotted);

            ShotSequence = shotSequence;
            Striker = striker;
            BallOnAtStart = ballOnAtStart;
            RedsRemainingAtStart = redsRemainingAtStart;
            ColourIndexAtStart = colourIndexAtStart;
            NominatedColour = nominatedColour;
            FirstContact = firstContact;
            HitAnyObjectBall = hitAnyObjectBall;
            CueBallPotted = cueBallPotted;
            CueBallOffTable = cueBallOffTable;
            PottedBalls = new ReadOnlyCollection<M5PottedBall>(copied);
            AnyBallHitCushionAfterFirstContact = anyBallHitCushionAfterFirstContact;
        }

        private static void ValidateState(
            int shotSequence,
            int striker,
            M5BallOn ballOn,
            int reds,
            M5Colour ordered,
            M5Colour nominated,
            M5PottedBallType firstContact,
            bool hitAnyObjectBall,
            bool cueBallPotted,
            bool cueBallOffTable)
        {
            if (shotSequence < 1) throw new ArgumentOutOfRangeException(nameof(shotSequence));
            if (striker != 1 && striker != 2)
                throw new ArgumentOutOfRangeException(nameof(striker));
            if (reds < 0 || reds > 15)
                throw new ArgumentOutOfRangeException(nameof(reds));
            if (!Enum.IsDefined(typeof(M5BallOn), ballOn))
                throw new ArgumentOutOfRangeException(nameof(ballOn));
            if (!Enum.IsDefined(typeof(M5PottedBallType), firstContact))
                throw new ArgumentOutOfRangeException(nameof(firstContact));
            if (cueBallPotted && cueBallOffTable)
                throw new ArgumentException("Cue-ball cannot be both potted and off-table.");
            if (hitAnyObjectBall && firstContact == M5PottedBallType.CueBall)
                throw new ArgumentException("FirstContact cannot be CueBall when an object ball was hit.");
            if (!hitAnyObjectBall && firstContact != M5PottedBallType.CueBall)
                throw new ArgumentException("FirstContact must be CueBall when no object ball was hit.");

            if (ballOn == M5BallOn.Red)
            {
                if (reds == 0) throw new ArgumentException("Red cannot be on when no reds remain.");
                if (ordered != M5Colour.None)
                    throw new ArgumentException("Ordered colour must be None while Red is on.");
                if (nominated != M5Colour.None)
                    throw new ArgumentException("Nomination must be None while Red is on.");
            }
            else if (ballOn == M5BallOn.NominatedColour)
            {
                if (ordered != M5Colour.None) throw new ArgumentException("Ordered colour must be None during a nominated colour phase.");
                if (!IsColour(nominated)) throw new ArgumentException("A valid colour nomination is required during a nominated colour phase.");
            }
            else
            {
                if (reds != 0) throw new ArgumentException("Ordered colours require zero reds.");
                if (!IsColour(ordered)) throw new ArgumentException("A valid ordered colour is required after all reds are gone.");
                if (nominated != M5Colour.None) throw new ArgumentException("Nomination must be None in the ordered-colour phase.");
            }
        }
        private static void ValidatePots(List<M5PottedBall> pots, bool cueBallPotted)
        {
            var ids = new HashSet<int>();
            var nonRedTypes = new HashSet<M5PottedBallType>();
            bool listHasCue = false;

            foreach (var pot in pots)
            {
                if (pot.BallId <= 0 || !Enum.IsDefined(typeof(M5PottedBallType), pot.Type) ||
                    pot.Value != M5PottedBall.GetValue(pot.Type))
                    throw new ArgumentException("Invalid/default potted-ball observation.");
                if (!ids.Add(pot.BallId))
                    throw new ArgumentException("Duplicate BallId in one shot.");
                if (pot.Type != M5PottedBallType.Red && !nonRedTypes.Add(pot.Type))
                    throw new ArgumentException("A non-red ball cannot be potted twice in one stroke.");
                if (pot.Type == M5PottedBallType.CueBall)
                    listHasCue = true;
            }

            if (listHasCue != cueBallPotted)
                throw new ArgumentException("CueBallPotted must agree with the CueBall entry in PottedBalls.");
        }

        internal static bool IsColour(M5Colour colour)
        {
            return colour == M5Colour.Yellow || colour == M5Colour.Green ||
                   colour == M5Colour.Brown || colour == M5Colour.Blue ||
                   colour == M5Colour.Pink || colour == M5Colour.Black;
        }
    }

    public sealed class ShotDecision
    {
        public int ShotSequence { get; }
        public bool IsLegal { get; }
        public ReadOnlyCollection<M5FoulReason> FoulReasons { get; }
        public int SingleFoulPenalty { get; }
        public int StrikerPoints { get; }
        public int OpponentPoints { get; }
        public bool StrikerContinues { get; }
        public M5BallOn? NextBallOn { get; }
        public int NextRedsRemaining { get; }
        public M5Colour NextColourIndex { get; }
        public ReadOnlyCollection<M5PottedBall> BallsToRespot { get; }
        public ReadOnlyCollection<M5PottedBall> BallsToRemove { get; }
        public bool CueBallInHandInD { get; }
        public bool FrameEndCandidate { get; }
        public bool FinalBlackRecoveryCandidate { get; }
        public bool FinalBlackWasPotted { get; }
        public M5PottedBall? FinalBlackRecoveryBall { get; }
        public int FinalBlackRecoveryPenalty { get; }

        internal ShotDecision(
            int shotSequence, bool isLegal, IEnumerable<M5FoulReason> foulReasons,
            int singleFoulPenalty, int strikerPoints, int opponentPoints,
            bool strikerContinues, M5BallOn? nextBallOn, int nextRedsRemaining,
            M5Colour nextColourIndex, IEnumerable<M5PottedBall> ballsToRespot,
            IEnumerable<M5PottedBall> ballsToRemove, bool cueBallInHandInD,
            bool frameEndCandidate, bool finalBlackRecoveryCandidate,
            bool finalBlackWasPotted, M5PottedBall? finalBlackRecoveryBall,
            int finalBlackRecoveryPenalty)
        {
            ShotSequence = shotSequence;
            IsLegal = isLegal;
            FoulReasons = ReadOnly(foulReasons);
            SingleFoulPenalty = singleFoulPenalty;
            StrikerPoints = strikerPoints;
            OpponentPoints = opponentPoints;
            StrikerContinues = strikerContinues;
            NextBallOn = nextBallOn;
            NextRedsRemaining = nextRedsRemaining;
            NextColourIndex = nextColourIndex;
            BallsToRespot = ReadOnly(ballsToRespot);
            BallsToRemove = ReadOnly(ballsToRemove);
            CueBallInHandInD = cueBallInHandInD;
            FrameEndCandidate = frameEndCandidate;
            FinalBlackRecoveryCandidate = finalBlackRecoveryCandidate;
            FinalBlackWasPotted = finalBlackWasPotted;
            FinalBlackRecoveryBall = finalBlackRecoveryBall;
            FinalBlackRecoveryPenalty = finalBlackRecoveryPenalty;
        }

        private static ReadOnlyCollection<T> ReadOnly<T>(IEnumerable<T> values)
        {
            return new ReadOnlyCollection<T>(new List<T>(values ?? Array.Empty<T>()));
        }
    }
}