namespace PoolPhysics.Sim
{
    /// <summary>
    /// Conservative legacy pot-continuation policy used until a variant-specific
    /// rules adapter supplies the authoritative verdict for 8-Ball / 9-Ball.
    /// A scratch always passes the turn; otherwise an object-ball pot keeps it.
    /// </summary>
    public static class PoolTurnPolicy
    {
        public static bool ShouldKeepTurnAfterPot(bool cueBallPotted, int objectBallPots)
            => !cueBallPotted && objectBallPots > 0;
    }
}
