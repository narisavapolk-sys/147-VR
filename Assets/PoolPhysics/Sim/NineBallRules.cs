using System;
using System.Collections.Generic;
using System.Numerics;

namespace PoolPhysics.Sim
{
    /// <summary>Config for the 9-ball rules evaluator. All coordinates are on the table
    /// plane (XZ) in TableSpecData playfield coordinates. Geometry mirrors the Unity
    /// BallRack component: foot spot at centre−0.25·L, rack rows extend toward the FOOT
    /// rail along −X (RackDirX=−1), apex/1-ball on the foot spot, cue behind the head string.</summary>
    public sealed class NineBallConfig
    {
        public Vector2 FootSpot;                       // apex spot (1-ball / respot anchor)
        public Vector2 HeadSpot;                       // default cue spot behind the head string
        public float  RackDirX = -1f;                  // rack row direction toward the foot rail
        public float  PlayMinX, PlayMaxX;              // playfield X bounds for respot sliding
        public const int   BreakRailRequirement = 4;   // WPA: >=4 object balls to cushions (no pot)
        public const float RespotStep = 0.001f;        // 1 mm steps when sliding the spot line
        public const float RespotEps = 1.002f;         // overlap factor for "place is free"
    }

    public enum NineBallFoul { None, NoContact, WrongFirstContact, NoRailAfterContact, CueScratch, CueOutOfBounds, IllegalBreak }

    /// <summary>Verdict for ONE completed shot (fresh simulator run + drained EventLog).</summary>
    public sealed class NineBallVerdict
    {
        public NineBallFoul Foul = NineBallFoul.None;
        public int  FirstContactNumber;                // number of the first cue-contact ball (0 = none)
        public readonly List<int> Potted = new List<int>();   // object-ball numbers pocketed this shot
        public bool CuePocketed;                       // cue ball fell into a pocket (scratch)
        public bool NinePotted;
        public bool LegalNinePot;                      // 9 legally potted == instant WIN
        public bool NineNeedsRespot;                   // 9 fell during a FOUL -> spot it back
        public bool ShooterContinues;                  // legal shot with >=1 legal pot (incl. break pot)
        public bool BallInHandForOpponent;             // any foul -> opponent ball-in-hand anywhere
        public Vector2 NineRespotPosition;
        public int  ObjectBallsCushioned;              // distinct object balls that reached rails/jaws/pockets
    }

    /// <summary>
    /// WPA 9-ball rules, engine-facing core subset, evaluated PURELY from the
    /// EventSimulator event log (kind BallBall/Cushion/Jaw/Pocket) + the ball-number
    /// mapping of that run. Deliberately NOT modelled (documented): push-out, jump
    /// cues, three-foul-match rules, interference/"shooting while balls are moving".
    /// Scratch precedence: cue-off-table holds over cue-potted changes nothing mechanically
    /// (both are ball-in-hand fouls).
    /// </summary>
    public static class NineBallRules
    {
        public static int LowestRemaining(HashSet<int> numbersRemaining)
        {
            int lo = int.MaxValue;
            foreach (int n in numbersRemaining) if (n < lo) lo = n;
            return lo == int.MaxValue ? 0 : lo;
        }

        /// <summary>
        /// Evaluate one shot. NOTES:
        ///  - events: FULL chronological event list of the shot (DrainEvents after RunToRest).
        ///  - cueIndex: sim.CueBallIndex AFTER BeginShot (indices are post-sort).
        ///  - numberBySimIndex: number per sim index, rebuilt AFTER BeginShot by position match.
        ///  - cueEscapedTable: caller's out-of-bounds check (captures sim-level escapes that
        ///    never produced a Pocket event).
        ///  - isBreakShot: true for the very first shot of the match.
        /// </summary>
        public static NineBallVerdict EvaluateShot(IReadOnlyList<EventSimulator.PoolEventSim> events,
                                                   int cueIndex, IReadOnlyList<int> numberBySimIndex,
                                                   HashSet<int> numbersRemaining,
                                                   bool cueEscapedTable, bool isBreakShot)
        {
            var v = new NineBallVerdict();
            int nineIndex = -1;
            for (int i = 0; i < numberBySimIndex.Count; i++) if (numberBySimIndex[i] == 9) nineIndex = i;
            int lowest = LowestRemaining(numbersRemaining);

            bool railAfterContact = false;
            var cushioned = new HashSet<int>();
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                switch (e.Kind)
                {
                    case EventSimulator.PoolEventKind.BallBall:
                        if (v.FirstContactNumber == 0 && (e.BallIndex == cueIndex || e.OtherIndex == cueIndex))
                        {
                            int other = e.BallIndex == cueIndex ? e.OtherIndex : e.BallIndex;
                            v.FirstContactNumber = numberBySimIndex[other];
                        }
                        break;
                    case EventSimulator.PoolEventKind.Cushion:
                    case EventSimulator.PoolEventKind.Jaw:
                        if (v.FirstContactNumber != 0) railAfterContact = true;
                        if (e.BallIndex != cueIndex) cushioned.Add(numberBySimIndex[e.BallIndex]);
                        break;
                    case EventSimulator.PoolEventKind.Pocket:
                        if (v.FirstContactNumber != 0) railAfterContact = true;
                        if (e.BallIndex == cueIndex) v.CuePocketed = true;
                        else
                        {
                            if (!v.Potted.Contains(numberBySimIndex[e.BallIndex])) v.Potted.Add(numberBySimIndex[e.BallIndex]);
                            if (e.BallIndex == nineIndex) v.NinePotted = true;
                        }
                        break;
                }
            }
            v.ObjectBallsCushioned = cushioned.Count;

            // ---- foul chain (highest precedence first) ----
            if (cueEscapedTable)                        v.Foul = NineBallFoul.CueOutOfBounds;
            else if (v.CuePocketed)                     v.Foul = NineBallFoul.CueScratch;
            else if (v.FirstContactNumber == 0)         v.Foul = NineBallFoul.NoContact;
            else if (v.FirstContactNumber != lowest)    v.Foul = NineBallFoul.WrongFirstContact;
            else if (isBreakShot && v.Potted.Count == 0 && v.ObjectBallsCushioned < NineBallConfig.BreakRailRequirement)
                                                        v.Foul = NineBallFoul.IllegalBreak;
            else if (!railAfterContact && !isBreakShot) v.Foul = NineBallFoul.NoRailAfterContact;
            // break: pot OR >=4 rails satisfy legality; the rail-after-contact check is
            // intentionally skipped on the break (the 4-rail rule subsumes it).

            bool legal = v.Foul == NineBallFoul.None;
            v.LegalNinePot        = legal && v.NinePotted;
            v.NineNeedsRespot     = !legal && v.NinePotted;
            v.ShooterContinues    = legal && v.Potted.Count > 0 && !v.NinePotted; // 9-pot = win, not continue
            v.BallInHandForOpponent = !legal;
            return v;
        }

        /// <summary>
        /// Respot the 9: start at the foot spot; while the position overlaps any active ball
        /// (<see cref="NineBallConfig.RespotEps"/> diameter clearance) slide toward the FOOT
        /// rail in 1 mm steps until the playfield edge; if never free, slide the other way
        /// toward the head. Deterministic, no randomness (WPA "as close as possible",
        /// long-string simplification).
        /// </summary>
        public static Vector2 SpotNine(Vector2 footSpot, IReadOnlyList<Vector2> activePositions,
                                       float ballRadius, NineBallConfig cfg)
        {
            float clearance = ballRadius * 2f * NineBallConfig.RespotEps;
            foreach (int dir in new[] { cfg.RackDirX, -cfg.RackDirX })
            {
                var p = footSpot;
                while (p.X >= cfg.PlayMinX && p.X <= cfg.PlayMaxX)
                {
                    bool blocked = false;
                    for (int i = 0; i < activePositions.Count; i++)
                        if ((activePositions[i] - p).LengthSquared() < clearance * clearance) { blocked = true; break; }
                    if (!blocked) return p;
                    p.X += dir * NineBallConfig.RespotStep;
                }
            }
            return footSpot; // pathological: playfield full — caller decides (documented)
        }
    }
}
