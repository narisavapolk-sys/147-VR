using System;
using System.Collections.Generic;
using System.Numerics;

namespace PoolPhysics.Sim
{
    // ---------------------------------------------------------------------------
    // NineBallTests — headless WPA 9-ball rules QA (mode "nineball [fuzzN]").
    // CONTRACT: every scenario EXPLICITLY adds the cue ball via AddCue (the cue is a
    // real sim ball — StrikeCue only sets its velocity; it never teleports balls).
    // RunShot snapshots positions AFTER BeginShot's canonical sort (pre-run) so the
    // number mapping stays index-correct while balls move; it drains the FULL shot
    // event log and returns it with the mapping.
    // ---------------------------------------------------------------------------
    public static class NineBallTests
    {
        static float R => 0.028575f;

        static PhysicsProfile RuleP() => new PhysicsProfile
        {
            SlidingFriction = 0.2f, RollingFriction = 0.01f, BallBallRestitution = 0.95f,
            BallBallFriction = 0.05f, CushionRestitution = 0.6f, SpinDecay = 2f, NapAnisotropy = 0f,
            Gravity = 9.81f
        };

        static PhysicsProfile StunP()   // quick-stop profile: surfaces die fast, no rail travel
            => new PhysicsProfile { SlidingFriction = 1.2f, RollingFriction = 0.0f, BallBallRestitution = 0.6f,
                                    BallBallFriction = 0f, SpinDecay = 2f, NapAnisotropy = 0f, Gravity = 9.81f };

        const float LEN = 2.90f, WID = 1.62f, PR = 0.0962f;

        static EventSimulator Table(PhysicsProfile p)
        {
            var t = new TableSpecData { PlayfieldSize = new Vector2(LEN, WID) };
            t.BuildSnookerRails(PR);
            return new EventSimulator(t, p) { RecordEvents = true };
        }

        static void Add(EventSimulator s, Vector2 pos)
            => s.Balls.Add(new Ball { Position = new Vector3(pos.X, R, pos.Y), Radius = R, Mass = 0.16f, Active = true });

        static void AddCue(EventSimulator s, Vector2 pos, List<(Vector2, int)> placed)
        {
            Add(s, pos);
            // CRITICAL: point the sim at the cue BEFORE the first BeginShot — BeginShot
            // remaps the index by the position of the CURRENT pointer, so a stale default
            // (index 0 = first-added object) would hand the velocity to an object ball.
            s.CueBallIndex = s.Balls.Count - 1;
            placed.Add((pos, 0));
        }

        static void AddObj(EventSimulator s, int number, Vector2 pos, List<(Vector2, int)> placed)
        { Add(s, pos); placed.Add((pos, number)); }

        /// <summary>Aim + speed on the cue ball already present in the sim (begins its shot).</summary>
        static void StrikeCue(EventSimulator s, Vector2 from, Vector2 dir, float speed)
        {
            s.BeginShot(); // canonicalize ball order + remap cue index (must see the cue position)
            for (int i = 0; i < s.Balls.Count; i++)
            { var b = s.Balls[i]; b.Radius = R; b.Mass = 0.16f; b.Active = true; s.Balls[i] = b; }
            var c = s.Balls[s.CueBallIndex];
            float L2 = dir.X * dir.X + dir.Y * dir.Y;
            var d = L2 > 1e-12f ? dir / MathF.Sqrt(L2) : new Vector2(1f, 0f);
            c.Velocity = new Vector3(d.X * speed, 0f, d.Y * speed);
            s.Balls[s.CueBallIndex] = c;
        }

        sealed class Shot { public StepResult Res; public List<EventSimulator.PoolEventSim> Events; public int[] Numbers; }

        /// <summary>Snapshot AFTER canonical sort, run to rest, drain events, map sim-index→number.</summary>
        static Shot RunShot(EventSimulator s, List<(Vector2 pos, int num)> placed, float maxTime)
        {
            s.BeginShot();
            var snap = new Vector3[s.Balls.Count];
            for (int i = 0; i < s.Balls.Count; i++) snap[i] = s.Balls[i].Position;
            var res = s.Step(maxTime);
            var ev = new List<EventSimulator.PoolEventSim>(); s.DrainEvents(ev);
            var map = new int[s.Balls.Count]; for (int k = 0; k < map.Length; k++) map[k] = -1;
            for (int i = 0; i < s.Balls.Count; i++)
                for (int k = 0; k < placed.Count; k++)
                    if (Math.Abs(snap[i].X - placed[k].pos.X) < 1e-4f && Math.Abs(snap[i].Z - placed[k].pos.Y) < 1e-4f)
                    { map[i] = placed[k].num; break; }
            return new Shot { Res = res, Events = ev, Numbers = map };
        }

        static bool CueEscaped(EventSimulator s)
        {
            foreach (var b in s.Balls)
                if (b.Active && !b.Pocketed && (b.Position.X < -1.6f * R || b.Position.X > s.Table.PlayfieldSize.X + 1.6f * R
                    || b.Position.Z < -1.6f * R || b.Position.Z > s.Table.PlayfieldSize.Y + 1.6f * R)) return true;
            return false;
        }

        public static void RunAll(int fuzzN)
        {
            S1_LegalBreak();
            S2_WrongFirstContact();
            S3_NoContact();
            S4_NoRailAfterContact();
            S5_Scratch();
            S6_LegalNineWin();
            S7_RespotAfterFoul();
            Fuzz(fuzzN);
            ReplayDeterminism();
        }

        // Full standard rack; break straight into the apex: contact=1, rack must legally spread.
        static void S1_LegalBreak()
        {
            var s = Table(RuleP());
            Vector2 foot = new Vector2(LEN * 0.25f, WID * 0.5f), head = new Vector2(LEN * 0.75f, WID * 0.5f);
            float rowSpacing = R * 2f * MathF.Sqrt(3f) * 0.5f + 2e-4f;
            var placed = new List<(Vector2, int)>();
            int[][] rows = { new[]{1}, new[]{2,3}, new[]{4,9,5}, new[]{6,7,8} };
            for (int rr = 0; rr < rows.Length; rr++)
                for (int c = 0; c < rows[rr].Length; c++)
                {
                    float lateral = (c - (rows[rr].Length - 1) * 0.5f) * (R * 2f + 4e-4f);
                    var p = new Vector2(foot.X - rr * rowSpacing, WID * 0.5f + lateral);
                    AddObj(s, rows[rr][c], p, placed);
                }
            AddCue(s, head, placed);
            StrikeCue(s, head, new Vector2(-1f, 0f), 4.5f);
            var shot = RunShot(s, placed, 12f);
            var remaining = new HashSet<int> { 1,2,3,4,5,6,7,8,9 };
            var v = NineBallRules.EvaluateShot(shot.Events, s.CueBallIndex, shot.Numbers, remaining, CueEscaped(s), true);

            Check(v.FirstContactNumber == 1, $"nine S1: first contact == 1 (got {v.FirstContactNumber})");
            Check(shot.Res.Reason == TerminationReason.Rest && v.Foul == NineBallFoul.None,
                  $"nine S1: legal break (foul={v.Foul}, potted={v.Potted.Count}, objectRails={v.ObjectBallsCushioned})");
            Check(!v.CuePocketed && !v.NinePotted,
                  $"nine S1: no scratch (cue={v.CuePocketed}) no premature 9 (={v.NinePotted})");
            if (v.Potted.Count > 0)
                Check(v.ShooterContinues, "nine S1: pot on legal break -> shooter continues");
        }

        // 5-ball sits in front of the 1-ball on the line: first contact=5 => wrong-contact foul.
        static void S2_WrongFirstContact()
        {
            var s = Table(StunP());
            var placed = new List<(Vector2, int)>();
            var pCue = new Vector2(1.30f, 0.81f); var p5 = new Vector2(1.45f, 0.81f); var p1 = new Vector2(1.60f, 0.81f);
            AddObj(s, 1, p1, placed); AddObj(s, 5, p5, placed); AddCue(s, pCue, placed);
            StrikeCue(s, pCue, new Vector2(1f, 0f), 0.8f);
            var shot = RunShot(s, placed, 8f);
            var v = NineBallRules.EvaluateShot(shot.Events, s.CueBallIndex, shot.Numbers, new HashSet<int>{1,5,9}, false, false);
            Check(v.FirstContactNumber == 5 && v.Foul == NineBallFoul.WrongFirstContact && v.BallInHandForOpponent,
                  $"nine S2: wrong first contact (first={v.FirstContactNumber} foul={v.Foul} inhand={v.BallInHandForOpponent})");
        }

        // Aim away from every ball => no contact at all.
        static void S3_NoContact()
        {
            var s = Table(RuleP());
            var placed = new List<(Vector2, int)>();
            var pCue = new Vector2(1.30f, 1.10f); var p1 = new Vector2(1.30f, 0.60f);
            AddObj(s, 1, p1, placed); AddCue(s, pCue, placed);
            StrikeCue(s, pCue, new Vector2(0f, 1f), 1.2f);   // straight up toward +Z cushion
            var shot = RunShot(s, placed, 8f);
            var v = NineBallRules.EvaluateShot(shot.Events, s.CueBallIndex, shot.Numbers, new HashSet<int>{1,9}, false, false);
            Check(v.FirstContactNumber == 0 && v.Foul == NineBallFoul.NoContact && v.BallInHandForOpponent,
                  $"nine S3: no contact foul (first={v.FirstContactNumber} foul={v.Foul})");
        }

        // Stun profile: object ball never reaches a rail after contact => no-rail-after-contact foul.
        static void S4_NoRailAfterContact()
        {
            var s = Table(StunP());
            var placed = new List<(Vector2, int)>();
            var pCue = new Vector2(1.30f, 0.81f); var p1 = new Vector2(1.75f, 0.81f);
            AddObj(s, 1, p1, placed); AddCue(s, pCue, placed);
            StrikeCue(s, pCue, new Vector2(1f, 0f), 0.5f);
            var shot = RunShot(s, placed, 6f);
            var v = NineBallRules.EvaluateShot(shot.Events, s.CueBallIndex, shot.Numbers, new HashSet<int>{1,9}, false, false);
            Check(v.FirstContactNumber == 1 && v.Potted.Count == 0 && v.ObjectBallsCushioned == 0 &&
                  v.Foul == NineBallFoul.NoRailAfterContact,
                  $"nine S4: no-rail foul (foul={v.Foul}, rails={v.ObjectBallsCushioned}, potted={v.Potted.Count})");
        }

        // Cue runs straight into the side-pocket mouth (nothing else on the line): scratch.
        // Also proves the scratch takes PRIORITY over the NoContact foul (1 stands untouched).
        static void S5_Scratch()
        {
            var s = Table(RuleP());
            var placed = new List<(Vector2, int)>();
            AddObj(s, 1, new Vector2(1.90f, 1.20f), placed);                  // parked, untouched
            var pCue = new Vector2(LEN * 0.5f, 0.50f);                        // on the side-mouth line (mx)
            AddCue(s, pCue, placed);
            StrikeCue(s, pCue, new Vector2(0f, -1f), 0.9f);
            var shot = RunShot(s, placed, 8f);
            var v = NineBallRules.EvaluateShot(shot.Events, s.CueBallIndex, shot.Numbers, new HashSet<int>{1,9}, false, false);
            Check(v.CuePocketed && v.Foul == NineBallFoul.CueScratch && v.BallInHandForOpponent && !v.Potted.Contains(1),
                  $"nine S5: scratch (foul={v.Foul} cueIn={v.CuePocketed} potted=[{string.Join(",", v.Potted)}])");
        }

        // Cut-shot: 9 aimed into the bottom-right corner mouth while the cue breaks off at
        // an angle (post-impact tangent diverges from the pot line), so no chased-in cue:
        // legal 9 == instant win.
        static void S6_LegalNineWin()
        {
            var s = Table(RuleP());
            var placed = new List<(Vector2, int)>();
            var pCue = new Vector2(2.37f, 0.445f); var p9 = new Vector2(2.60f, 0.25f);
            AddObj(s, 9, p9, placed); AddCue(s, pCue, placed);
            StrikeCue(s, pCue, new Vector2(0.76f, -0.65f), 2.0f);
            var shot = RunShot(s, placed, 8f);
            var v = NineBallRules.EvaluateShot(shot.Events, s.CueBallIndex, shot.Numbers, new HashSet<int>{9}, false, false);
            Check(v.Foul == NineBallFoul.None && v.NinePotted && v.LegalNinePot
                  && v.ShooterContinues == false && !v.NineNeedsRespot,
                  $"nine S6: legal 9 = win (foul={v.Foul}, nine={v.NinePotted}, legalPot={v.LegalNinePot})");
        }

        // First contact = 9 while 1 remains => wrong contact; then SpotNine must slide an
        // occupied foot spot off into clear space (geometry, deterministic).
        static void S7_RespotAfterFoul()
        {
            var s = Table(StunP());
            var placed = new List<(Vector2, int)>();
            Vector2 foot = new Vector2(LEN * 0.25f, WID * 0.5f);
            var pCue = new Vector2(1.30f, 0.81f); var p9 = new Vector2(1.42f, 0.81f); var p1 = new Vector2(1.60f, 0.81f);
            AddObj(s, 1, p1, placed); AddObj(s, 9, p9, placed); AddCue(s, pCue, placed);
            StrikeCue(s, pCue, new Vector2(1f, 0f), 1.2f);
            var shot = RunShot(s, placed, 6f);
            var v = NineBallRules.EvaluateShot(shot.Events, s.CueBallIndex, shot.Numbers, new HashSet<int>{1,9}, false, false);
            Check(v.Foul == NineBallFoul.WrongFirstContact && !v.LegalNinePot,
                  $"nine S7a: wrong contact evaluated (foul={v.Foul})");
            var spot = NineBallRules.SpotNine(foot, new List<Vector2> { foot }, R,
                new NineBallConfig { RackDirX = -1f, PlayMinX = 0f, PlayMaxX = LEN });
            Check(Math.Abs(spot.X - foot.X) > 1e-3f || Math.Abs(spot.Y - foot.Y) > 1e-3f,
                  $"nine S7b: respot slides off an occupied foot spot (spot=({spot.X:F4},{spot.Y:F4}))");
            bool free = (spot - foot).LengthSquared() > MathF.Pow(R * 2f * NineBallConfig.RespotEps, 2f);
            Check(free, "nine S7c: respot position does not overlap the blocker");
        }

        // ---- fuzz: random legal-ish frames; verdict-shape invariants only ----
        static void Fuzz(int n)
        {
            if (n <= 0) { Check(true, "nine fuzz: skipped (n<=0)"); return; }
            int badInvariant = 0; var rng = new Random(20261104);
            for (int it = 0; it < n; it++)
            {
                var s = Table(RuleP());
                var placed = new List<(Vector2, int)>();
                // random clutter: numbered objects at random in-bounds, non-overlapping positions
                int m = rng.Next(2, 9);
                var used = new List<Vector2>();
                for (int k = 0; k < m; k++)
                {
                    Vector2 p;
                    bool ok = false;
                    for (int tries = 0; tries < 30 && !ok; tries++)
                    {
                        p = new Vector2(0.35f + (float)rng.NextDouble() * 2.2f, 0.30f + (float)rng.NextDouble() * 1.02f);
                        ok = true;
                        foreach (var u in used)
                            if ((u - p).LengthSquared() < MathF.Pow(R * 2.4f, 2f)) { ok = false; break; }
                        if (ok) { used.Add(p); AddObj(s, rng.Next(1, 10), p, placed); }
                    }
                }
                var pCue = new Vector2(1.10f + (float)rng.NextDouble() * 0.5f, 0.40f + (float)rng.NextDouble() * 0.8f);
                AddCue(s, pCue, placed);
                float ang = (float)rng.NextDouble() * MathF.PI * 2f;
                StrikeCue(s, pCue, new Vector2(MathF.Cos(ang), MathF.Sin(ang)), 0.6f + (float)rng.NextDouble() * 1.6f);
                var shot = RunShot(s, placed, 10f);
                var v = NineBallRules.EvaluateShot(shot.Events, s.CueBallIndex, shot.Numbers,
                                                   new HashSet<int> { 1,2,3,4,5,6,7,8,9 }, CueEscaped(s), false);
                bool ok2 = shot.Res.Reason != TerminationReason.InvalidState
                       && (v.BallInHandForOpponent == (v.Foul != NineBallFoul.None))
                       && (v.ShooterContinues == (v.Foul == NineBallFoul.None && v.Potted.Count > 0 && !v.NinePotted))
                       && (!v.NinePotted || v.Potted.Contains(9));
                if (!ok2) badInvariant++;
            }
            Check(badInvariant == 0, $"nine fuzz {n}: verdict-shape invariants (bad={badInvariant})");
        }

        // ---- determinism: identical script => identical verdicts twice ----
        sealed class ReplayVerdict { public NineBallFoul Foul; public int FC; public string Pots; public int Cush; }

        static ReplayVerdict RunScript()
        {
            var s = Table(StunP());
            var placed = new List<(Vector2, int)>();
            var pCue = new Vector2(1.30f, 0.81f); var p5 = new Vector2(1.45f, 0.81f); var p1 = new Vector2(1.60f, 0.81f);
            AddObj(s, 1, p1, placed); AddObj(s, 5, p5, placed); AddCue(s, pCue, placed);
            StrikeCue(s, pCue, new Vector2(1f, 0f), 0.8f);
            var shot = RunShot(s, placed, 6f);
            var v = NineBallRules.EvaluateShot(shot.Events, s.CueBallIndex, shot.Numbers, new HashSet<int>{1,5,9}, false, false);
            return new ReplayVerdict { Foul = v.Foul, FC = v.FirstContactNumber, Pots = string.Join(",", v.Potted), Cush = v.ObjectBallsCushioned };
        }

        static void ReplayDeterminism()
        {
            var a = RunScript(); var b = RunScript();
            Check(a.Foul == b.Foul && a.FC == b.FC && a.Pots == b.Pots && a.Cush == b.Cush,
                  $"nine determinism: replay identical ({a.Foul}|{a.FC}|{a.Pots}|{a.Cush}) == ({b.Foul}|{b.FC}|{b.Pots}|{b.Cush})");
        }

        static int FAILS = 0;
        static void Check(bool cond, string msg) { Console.WriteLine($"  {(cond ? "PASS" : "FAIL")}  {msg}"); if (!cond) FAILS++; }
        public static int Fails => FAILS;
    }
}
