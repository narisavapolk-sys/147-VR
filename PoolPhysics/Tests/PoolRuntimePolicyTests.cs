using System;
using PoolPhysics.Sim;

static class PoolRuntimePolicyTests
{
    public static int Fails { get; private set; }

    static void Check(bool condition, string message)
    {
        Console.WriteLine((condition ? "  PASS  " : "  FAIL  ") + message);
        if (!condition)
            Fails++;
    }

    public static void RunAll()
    {
        Fails = 0;

        Check(PoolBallIdentity.SnookerPoints("SM_Red.012") == 1,
            "FBX dotted red name maps to snooker red");
        Check(PoolBallIdentity.SnookerPoints("SM_Black") == 7,
            "prefixed black name maps to snooker black");
        Check(PoolBallIdentity.SnookerPoints("SM_White_CueBall") == 0,
            "prefixed white cue name maps to cue ball");
        Check(PoolBallIdentity.SnookerPoints("SM_147VR_Bay1_Ball_Red_011") == 1,
            "production Bay1 red name maps to snooker red");
        Check(PoolBallIdentity.SnookerPoints("SM_147VR_Bay3_Ball_03") == -1,
            "numbered pool ball is not misclassified as a snooker colour");

        Check(PoolBallIdentity.TryParsePoolNumber("SM_147VR_Bay3_Ball_03", 15, out int eightBall) && eightBall == 3,
            "8-Ball FBX number parses correctly");
        Check(PoolBallIdentity.TryParsePoolNumber("SM_147VR_Bay4_Ball_09", 9, out int nineBall) && nineBall == 9,
            "9-Ball FBX number parses correctly");
        Check(!PoolBallIdentity.TryParsePoolNumber("SM_147VR_Bay4_Ball_10", 9, out _),
            "9-Ball rejects an out-of-range number");
        Check(!PoolBallIdentity.TryParsePoolNumber("SM_147VR_Bay4_Ball_Cue", 9, out _),
            "cue-ball name is not parsed as an object-ball number");

        Check(PoolTurnPolicy.ShouldKeepTurnAfterPot(false, 1),
            "legal object pot keeps the striker");
        Check(!PoolTurnPolicy.ShouldKeepTurnAfterPot(false, 0),
            "no object pot passes the turn");
        Check(!PoolTurnPolicy.ShouldKeepTurnAfterPot(true, 0),
            "scratch passes the turn");
        Check(!PoolTurnPolicy.ShouldKeepTurnAfterPot(true, 2),
            "scratch passes the turn even when object balls were potted");

        Console.WriteLine($"Pool runtime policy tests: {12 - Fails}/12 passed, {Fails} failed.");
    }
}
