namespace PoolPhysics.Sim
{
    /// <summary>Ruleset assigned to a physical bay. This is independent of session mode.</summary>
    public enum GameVariant
    {
        Snooker = 0,
        EightBall = 1,
        NineBall = 2
    }

    /// <summary>Session policy. This is independent of the selected game variant.</summary>
    public enum PoolGameMode
    {
        Match = 0,
        SoloPractice = 1
    }

    /// <summary>
    /// Pure, headless-testable policy for the Solo Practice UNDO feature.
    /// The bridge supplies live physics/lifecycle facts; this class owns the gate.
    /// </summary>
    public static class PoolUndoPolicy
    {
        public static bool CanCaptureSnapshot(
            PoolGameMode mode,
            bool undoMapValid,
            bool ownsPhysics,
            bool atRest)
            => mode == PoolGameMode.SoloPractice
               && undoMapValid
               && ownsPhysics
               && atRest;

        public static bool CanChangeGameMode(bool shotOpen, bool atRest)
            => !shotOpen && atRest;

        public static bool CanUndo(
            PoolGameMode mode,
            bool undoMapValid,
            bool hasSnapshot,
            bool ownsPhysics,
            bool atRest,
            bool shotOpen)
            => CanCaptureSnapshot(mode, undoMapValid, ownsPhysics, atRest)
               && hasSnapshot
               && !shotOpen;
    }
}
