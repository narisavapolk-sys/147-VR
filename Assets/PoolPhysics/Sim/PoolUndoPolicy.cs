namespace PoolPhysics.Sim
{
    /// <summary>Authoritative gameplay mode used by the rules bridge.</summary>
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
