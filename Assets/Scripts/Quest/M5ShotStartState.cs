using M5Rules;

/// <summary>Immutable facts captured immediately after BeginShot.</summary>
public sealed class M5ShotStartState
{
    public int ShotSequence { get; }
    public int Striker { get; }
    public M5BallOn BallOn { get; }
    public int RedsRemaining { get; }
    public M5Colour OrderedColour { get; }
    public M5Colour NominatedColour { get; }

    public M5ShotStartState(int shotSequence, int striker, M5BallOn ballOn,
        int redsRemaining, M5Colour orderedColour, M5Colour nominatedColour)
    {
        if (shotSequence < 1) throw new System.ArgumentOutOfRangeException(nameof(shotSequence));
        if (striker != 1 && striker != 2) throw new System.ArgumentOutOfRangeException(nameof(striker));
        ShotSequence = shotSequence;
        Striker = striker;
        BallOn = ballOn;
        RedsRemaining = redsRemaining;
        OrderedColour = orderedColour;
        NominatedColour = nominatedColour;
    }
}
