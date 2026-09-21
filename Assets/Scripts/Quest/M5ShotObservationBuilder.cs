using System;
using System.Collections.Generic;
using M5Rules;
using UnityEngine;

/// <summary>Builds exactly one immutable observation from shot-start state and settled facts.</summary>
public sealed class M5ShotObservationBuilder
{
    private readonly SnookerBallTracker _balls;
    private readonly Dictionary<int, SnookerBallTracker.BallInfo> _identity;

    public M5ShotObservationBuilder(SnookerBallTracker balls)
    {
        _balls = balls ?? throw new ArgumentNullException(nameof(balls));
        _balls.RefreshIfNeeded();
        _identity = BuildIdentityMap(_balls);
    }

    public M5ShotStartState CaptureStartState(int sequence, SnookerScoreManager score,
        SnookerTurnManager turns)
    {
        if (score == null || turns == null) throw new ArgumentNullException();
        M5BallOn on = score.ballOn == SnookerScoreManager.BallOn.Red ? M5BallOn.Red :
            score.ballOn == SnookerScoreManager.BallOn.Colour ? M5BallOn.NominatedColour : M5BallOn.ColoursInOrder;
        M5Colour ordered = on == M5BallOn.ColoursInOrder ? ToColour(score.BallOnName()) : M5Colour.None;
        M5Colour nominated = on == M5BallOn.NominatedColour ? score.NominatedColour : M5Colour.None;
        return new M5ShotStartState(sequence, turns.currentPlayer, on, score.redsRemaining, ordered, nominated);
    }
    public ShotObservation Build(M5ShotStartState start,
        SnookerBallTracker.BallInfo firstBall, bool hitAnyObjectBall,
        bool cueOnTable, IEnumerable<SnookerBallTracker.BallInfo> observedPots)
    {
        if (start == null) throw new ArgumentNullException(nameof(start));
        var pots = new List<M5PottedBall>();
        var seen = new HashSet<int>();
        bool cuePotted = false;
        foreach (var ball in observedPots ?? Array.Empty<SnookerBallTracker.BallInfo>())
        {
            if (ball == null || ball.transform == null) continue;
            int id = StableBallId(ball);
            if (!seen.Add(id)) continue;
            M5PottedBallType type = ToType(ball);
            pots.Add(new M5PottedBall(id, type));
            cuePotted |= type == M5PottedBallType.CueBall;
        }
        // A cue-ball pot is not also an off-table observation.
        bool cueOffTable = !cueOnTable && !cuePotted;
        M5PottedBallType first = firstBall == null ? M5PottedBallType.CueBall : ToType(firstBall);
        return new ShotObservation(start.ShotSequence, start.Striker, start.BallOn,
            start.RedsRemaining, start.OrderedColour, start.NominatedColour, first,
            hitAnyObjectBall, cuePotted, cueOffTable, pots, false);
    }

    public int StableBallId(SnookerBallTracker.BallInfo ball)
    {
        if (ball == null || ball.transform == null) throw new ArgumentNullException(nameof(ball));
        foreach (var pair in _identity)
            if (pair.Value == ball) return pair.Key;
        throw new InvalidOperationException("Ball identity is not in the frozen scene identity map.");
    }
    private static Dictionary<int, SnookerBallTracker.BallInfo> BuildIdentityMap(SnookerBallTracker tracker)
    {
        var map = new Dictionary<int, SnookerBallTracker.BallInfo>();
        var reds = new List<SnookerBallTracker.BallInfo>();
        foreach (var b in tracker.Balls)
        {
            if (b == null || b.transform == null) continue;
            if (b.points == 0)
            {
                if (map.ContainsKey(1)) throw new InvalidOperationException("Duplicate cue-ball identity.");
                map[1] = b;
            }
            else if (b.points == 1) reds.Add(b);
            else
            {
                int id = 100 + b.points;
                if (map.ContainsKey(id)) throw new InvalidOperationException("Duplicate colour identity: " + id);
                map[id] = b;
            }
        }
        if (!map.ContainsKey(1)) throw new InvalidOperationException("Cue-ball identity missing.");
        if (reds.Count > 15) throw new InvalidOperationException("More than 15 red identities found.");
        reds.Sort((a,b) => string.CompareOrdinal(Path(a.transform), Path(b.transform)));
        for (int i=0; i<reds.Count; i++) map[10+i] = reds[i];
        return map;
    }

    private static string Path(Transform t)
    {
        var parts = new Stack<string>();
        while (t != null) { parts.Push(t.GetSiblingIndex() + ":" + t.name); t=t.parent; }
        return string.Join("/", parts.ToArray());
    }
    private static M5PottedBallType ToType(SnookerBallTracker.BallInfo b)
    {
        switch (b.points)
        {
            case 0: return M5PottedBallType.CueBall; case 1: return M5PottedBallType.Red;
            case 2: return M5PottedBallType.Yellow; case 3: return M5PottedBallType.Green;
            case 4: return M5PottedBallType.Brown; case 5: return M5PottedBallType.Blue;
            case 6: return M5PottedBallType.Pink; case 7: return M5PottedBallType.Black;
            default: throw new ArgumentOutOfRangeException(nameof(b.points));
        }
    }

    private static M5Colour ToColour(string name)
    {
        if (name == "Yellow") return M5Colour.Yellow; if (name == "Green") return M5Colour.Green;
        if (name == "Brown") return M5Colour.Brown; if (name == "Blue") return M5Colour.Blue;
        if (name == "Pink") return M5Colour.Pink; if (name == "Black") return M5Colour.Black;
        return M5Colour.None;
    }
}
