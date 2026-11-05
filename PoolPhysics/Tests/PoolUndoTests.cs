using System;
using System.Collections.Generic;
using System.Numerics;

namespace PoolPhysics.Sim
{
    // ---------------------------------------------------------------------------
    // PoolUndoTests — headless UNDO QA (mode "undo [fuzzN]").
    // Covers the PoolUndoStack contract (S2 #2c) plus the gameplay claim it exists
    // for — "undo the last shot, then replay the identical strike":
    //   U1 push→play→undo returns the EXACT pre-shot state (field-wise)
    //   U2 empty stack: TryUndo false + cause, never throws, nothing mutated
    //   U3 corrupt top entry: false, entry KEPT (retry-able), counted, no throw
    //   U4 capacity ring: pushing past capacity discards the OLDEST, pops stay LIFO
    //   U5 Push of an invalid state throws at the call site; stack unchanged
    //   U6 PeekTop is the canonical JSON of what TryUndo will restore (no aliasing)
    //   U7 undo→replay determinism: same pre-state + same strike = byte-identical
    //      outcome as the original shot (the reason ShotNumber rides the snapshot)
    //   U8 seeded fuzz: random push/undo interleavings never throw and every undo
    //      returns a state byte-identical to one that was pushed
    //   U9 Clear removes both history and corruption diagnostics
    //   U10 SoloPractice gate matrix blocks Match, moving, open-shot, invalid-map, and no-authority cases
    //   U11 canonical sorting returns a new-index→old-index permutation and remaps CueBallIndex by identity
    // CONTRACT mirrors PoolSaveTests plumbing (AddCue before first BeginShot,
    // RunShot snapshot mapping, SameState field-wise compare).
    // ---------------------------------------------------------------------------
    public static class PoolUndoTests
    {
        static float R => 0.028575f;
        const float LEN = 2.90f, WID = 1.62f, PR = 0.0962f;

        static PhysicsProfile RuleP() => new PhysicsProfile
        {
            SlidingFriction = 0.2f, RollingFriction = 0.01f, BallBallRestitution = 0.95f,
            BallBallFriction = 0.05f, CushionRestitution = 0.6f, SpinDecay = 2f, NapAnisotropy = 0f,
            Gravity = 9.81f
        };

        static EventSimulator Table(PhysicsProfile p)
        {
            var t = new TableSpecData { PlayfieldSize = new Vector2(LEN, WID) };
            t.BuildSnookerRails(PR);
            return new EventSimulator(t, p) { RecordEvents = true };
        }

        public static void RunAll(int fuzzN)
        {
            U1_UndoRestoresPreShotState();
            U2_EmptyStack();
            U3_CorruptTopEntry();
            U4_CapacityRing();
            U5_PushInvalidThrowsNoMutation();
            U6_PeekTopCanonical();
            U7_UndoReplayDeterminism();
            U8_Fuzz(fuzzN);
            U9_ClearResetsHistoryAndDiagnostics();
            U10_SoloPracticeGateMatrix();
            U11_BeginShotPermutationMapsCueByIdentity();
        }

        // -----------------------------------------------------------------------
        // state builders (compact 9-ball layout, unique numbers, cue present)
        // -----------------------------------------------------------------------
        static PoolSaveState RackState(int shotNumber)
        {
            var balls = new List<PoolSaveBall> { new PoolSaveBall { Number = 0, X = LEN * 0.75f, Z = WID * 0.5f } };
            int n = 1;
            for (int rr = 0; rr < 3; rr++)
                for (int c = 0; c < 3 && n <= 9; c++, n++)
                    balls.Add(new PoolSaveBall { Number = n, X = LEN * 0.25f + rr * 3 * R, Z = WID * 0.5f + (c - 1) * 3 * R });
            return new PoolSaveState
            {
                RulesMode = "NineBall", CurrentPlayer = 0, RacksWon = new[] { 0, 0 },
                ShotNumber = shotNumber, BallInHand = false, IsBreakShot = shotNumber == 0,
                Balls = balls.ToArray()
            };
        }

        // -----------------------------------------------------------------------
        // shared sim plumbing (mirrors PoolSaveTests; cue added last)
        // -----------------------------------------------------------------------
        static void AddBall(EventSimulator s, Vector2 pos)
            => s.Balls.Add(new Ball { Position = new Vector3(pos.X, R, pos.Y), Radius = R, Mass = 0.16f, Active = true });

        sealed class RunShotResult { public StepResult Res; public List<EventSimulator.PoolEventSim> Events; public int[] Numbers; }

        static RunShotResult RunShotC(EventSimulator s, List<(Vector2 pos, int num)> placed, float maxTime)
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
            return new RunShotResult { Res = res, Events = ev, Numbers = map };
        }

        // play one scripted shot from a state (cue strikes ball 1); folds sim back
        // into a NEW save state exactly like the Unity bridge will
        static PoolSaveState PlayShot(PoolSaveState st)
        {
            var sim = Table(RuleP());
            var placed = new List<(Vector2, int)>();
            for (int i = 0; i < st.Balls.Length; i++)
                if (!st.Balls[i].Pocketed)
                {
                    var p = new Vector2((float)st.Balls[i].X, (float)st.Balls[i].Z);
                    AddBall(sim, p); placed.Add((p, st.Balls[i].Number));
                }
            // cue must be re-indexed AFTER adding (BeginShot remaps)
            for (int i = 0; i < sim.Balls.Count; i++)
                if (placed[i].Item2 == 0) { sim.CueBallIndex = i; break; }

            Vector2 cue = new Vector2((float)CueBallOf(st).X, (float)CueBallOf(st).Z);
            Vector2 one = new Vector2((float)NumBallOf(st, 1).X, (float)NumBallOf(st, 1).Z);
            var dir = one - cue;
            sim.BeginShot();
            for (int i = 0; i < sim.Balls.Count; i++)
            { var b = sim.Balls[i]; b.Radius = R; b.Mass = 0.16f; b.Active = true; sim.Balls[i] = b; }
            var c = sim.Balls[sim.CueBallIndex];
            float L2 = dir.X * dir.X + dir.Y * dir.Y;
            var d = L2 > 1e-12f ? dir / MathF.Sqrt(L2) : new Vector2(1f, 0f);
            c.Velocity = new Vector3(d.X * 4.5f, 0f, d.Y * 4.5f);
            sim.Balls[sim.CueBallIndex] = c;
            var shot = RunShotC(sim, placed, 12f);

            var outState = new PoolSaveState
            {
                SchemaVersion = st.SchemaVersion, RulesMode = st.RulesMode,
                CurrentPlayer = st.CurrentPlayer, RacksWon = new[] { st.RacksWon[0], st.RacksWon[1] },
                ShotNumber = st.ShotNumber + 1, BallInHand = false, IsBreakShot = false,
                Balls = new PoolSaveBall[sim.Balls.Count]
            };
            for (int i = 0; i < sim.Balls.Count; i++)
            {
                var b = sim.Balls[i];
                outState.Balls[i] = new PoolSaveBall
                {
                    Number = shot.Numbers[i], X = b.Position.X, Z = b.Position.Z, Pocketed = b.Pocketed
                };
            }
            return outState;
        }

        static PoolSaveBall CueBallOf(PoolSaveState st) => NumBallOf(st, 0);
        static PoolSaveBall NumBallOf(PoolSaveState st, int number)
        {
            for (int i = 0; i < st.Balls.Length; i++) if (st.Balls[i].Number == number) return st.Balls[i];
            return null;
        }

        static bool SameState(PoolSaveState a, PoolSaveState b)
        {
            if (a.SchemaVersion != b.SchemaVersion || a.RulesMode != b.RulesMode
                || a.CurrentPlayer != b.CurrentPlayer || a.ShotNumber != b.ShotNumber
                || a.BallInHand != b.BallInHand || a.IsBreakShot != b.IsBreakShot) return false;
            if (a.Balls.Length != b.Balls.Length) return false;
            for (int i = 0; i < a.Balls.Length; i++)
            {
                var x = a.Balls[i]; var y = b.Balls[i];
                if (x.Number != y.Number || x.Pocketed != y.Pocketed || x.X != y.X || x.Z != y.Z) return false;
            }
            return true;
        }

        // -----------------------------------------------------------------------
        // U1 — the core gameplay contract
        // -----------------------------------------------------------------------
        static void U1_UndoRestoresPreShotState()
        {
            var pre = RackState(0);
            var undo = new PoolUndoStack(8);
            undo.Push(pre);
            var post = PlayShot(pre);
            Check(!SameState(pre, post), "undo U1: the shot actually changed the state");
            bool ok = undo.TryUndo(out var restored, out string err);
            Check(ok && SameState(pre, restored),
                  $"undo U1: undo restores the exact pre-shot state ({(ok ? "ok" : err)})");
        }

        static void U2_EmptyStack()
        {
            var undo = new PoolUndoStack(8);
            bool ok = undo.TryUndo(out var restored, out string err);
            Check(!ok && restored == null && err == "undo stack empty" && undo.Count == 0 && undo.CorruptCount == 0,
                  $"undo U2: empty stack → false + cause, nothing mutated (err={err})");
        }

        static void U3_CorruptTopEntry()
        {
            var undo = new PoolUndoStack(8);
            undo.Push(RackState(0));
            // inject an unreadable schema version into the stored canonical JSON
            var fld = typeof(PoolUndoStack).GetField("_serialized",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (List<string>)fld.GetValue(undo);
            list[0] = list[0].Replace("\"SchemaVersion\":1", "\"SchemaVersion\":9");
            int countBefore = undo.Count;
            bool ok = undo.TryUndo(out var restored, out string err);
            Check(!ok && restored == null && err != null && undo.Count == countBefore && undo.CorruptCount == 1,
                  $"undo U3: corrupt top → false, entry KEPT, counted (err={err?.Length > 0})");
        }

        static void U4_CapacityRing()
        {
            var undo = new PoolUndoStack(3);
            for (int k = 0; k < 5; k++) undo.Push(RackState(k));   // ring keeps 2,3,4
            Check(undo.Count == 3, "undo U4: capacity ring trims the OLDEST push");
            bool ordered = true;
            for (int expect = 4; expect >= 2; expect--)
            {
                if (!undo.TryUndo(out var st, out _) || !SameState(st, RackState(expect))) { ordered = false; break; }
            }
            Check(ordered && undo.Count == 0, "undo U4: pops stay LIFO after ring trimming (4→3→2)");
        }

        static void U5_PushInvalidThrowsNoMutation()
        {
            var undo = new PoolUndoStack(8);
            undo.Push(RackState(0));
            var bad = RackState(1);
            bad.Balls[3].X = double.NaN;
            bool threw = false;
            try { undo.Push(bad); } catch (ArgumentException) { threw = true; }
            Check(threw && undo.Count == 1, "undo U5: invalid Push throws at the call site; stack unchanged");
            bool stillWorks = undo.TryUndo(out var restored, out _) && SameState(restored, RackState(0));
            Check(stillWorks, "undo U5: stack still fully functional after the rejected push");
        }

        static void U6_PeekTopCanonical()
        {
            var undo = new PoolUndoStack(8);
            var st = RackState(0);
            undo.Push(st);
            string top = undo.PeekTop();
            bool ok = undo.TryUndo(out var restored, out _);
            Check(ok && top == PoolSaveCodec.ToJson(restored),
                  "undo U6: PeekTop is the canonical JSON of the restored state (no aliasing)");
        }

        // -----------------------------------------------------------------------
        // U7 — undo then replay the same strike: outcome must equal the original
        // -----------------------------------------------------------------------
        static void U7_UndoReplayDeterminism()
        {
            string first, second;
            PoolSaveState post, post2;

            var pre = RackState(0);
            var undo = new PoolUndoStack(8);
            undo.Push(pre);
            post = PlayShot(pre);
            undo.TryUndo(out var restored, out _);
            post2 = PlayShot(restored);
            first = PoolSaveCodec.ToJson(post);
            second = PoolSaveCodec.ToJson(post2);

            Check(first == second && SameState(post, post2),
                  "undo U7: undo→replay(shot) == original shot outcome (byte-identical)");
            Check(SameState(pre, restored), "undo U7: restored snapshot still byte-equal to the pre-shot state");

            // whole chain twice (stack + codec + engine determinism through the undo path)
            string Chain()
            {
                var u = new PoolUndoStack(8);
                u.Push(RackState(0));
                var after = PlayShot(RackState(0));
                u.TryUndo(out var r, out _);
                return PoolSaveCodec.ToJson(PlayShot(r));
            }
            Check(Chain() == Chain(), "undo U7: full undo chain deterministic across runs");
        }

        // -----------------------------------------------------------------------
        // U8 — seeded fuzz over random push/undo interleavings
        // -----------------------------------------------------------------------
        static void U8_Fuzz(int fuzzN)
        {
            var rng = new Random(20261104);
            var undo = new PoolUndoStack(8);
            var pushed = new HashSet<string>();   // canonical JSON of every Push accepted
            int undos = 0, unknownRestores = 0, threw = 0;
            for (int i = 0; i < fuzzN; i++)
            {
                try
                {
                    if (rng.NextDouble() < 0.55)
                    {
                        var s = RandomState(rng);
                        undo.Push(s);
                        pushed.Add(PoolSaveCodec.ToJson(s));
                    }
                    else if (undo.TryUndo(out var restored, out _))
                    {
                        undos++;
                        if (!pushed.Contains(PoolSaveCodec.ToJson(restored))) unknownRestores++;
                    }
                }
                catch (Exception) { threw++; }
            }
            Check(threw == 0 && unknownRestores == 0 && undos > 0,
                  $"undo fuzz {fuzzN}: never-threw={threw == 0}, every restore was a pushed state " +
                  $"(unknown={unknownRestores}, undos={undos})");
        }

        static PoolSaveState RandomState(Random rng)
        {
            int n = 2 + rng.Next(8);               // cue + 1..8 objects
            var balls = new List<PoolSaveBall>();
            var taken = new List<(double x, double z)>();
            double NX() { double v; int guard = 0; do { v = 0.2 + rng.NextDouble() * (LEN - 0.4); } while (++guard < 100 && TooClose(v, -1, taken)); return v; }
            double NZ() { double v; int guard = 0; do { v = 0.2 + rng.NextDouble() * (WID - 0.4); } while (++guard < 100 && TooClose(-1, v, taken)); return v; }
            bool TooClose(double x, double z, List<(double X, double Z)> t)
            {
                for (int i = 0; i < t.Count; i++)
                {
                    double dx = x < 0 ? 0 : x - t[i].X, dz = z < 0 ? 0 : z - t[i].Z;
                    if (dx * dx + dz * dz < (4 * R) * (4 * R)) return true;
                }
                return false;
            }
            for (int k = 0; k <= n; k++)
            {
                double x = NX(), z = NZ();
                taken.Add((x, z));
                balls.Add(new PoolSaveBall { Number = k, X = x, Z = z, Pocketed = k == n });
            }
            return new PoolSaveState
            {
                RulesMode = "NineBall", CurrentPlayer = rng.Next(2), RacksWon = new[] { rng.Next(3), rng.Next(3) },
                ShotNumber = rng.Next(100), BallInHand = rng.NextDouble() < 0.3, IsBreakShot = false,
                Balls = balls.ToArray()
            };
        }

        // -----------------------------------------------------------------------
        // U9 — mode changes must not leave stale snapshots or stale diagnostics
        // -----------------------------------------------------------------------
        static void U9_ClearResetsHistoryAndDiagnostics()
        {
            var undo = new PoolUndoStack(2);
            undo.Push(RackState(0));

            var field = typeof(PoolUndoStack).GetField("_serialized",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var entries = (List<string>)field.GetValue(undo);
            entries[0] = entries[0].Replace("\"SchemaVersion\":1", "\"SchemaVersion\":9");
            bool rejected = !undo.TryUndo(out _, out _) && undo.CorruptCount == 1;

            undo.Clear();
            bool empty = !undo.TryUndo(out var restored, out string error)
                         && restored == null && error == "undo stack empty";
            Check(rejected && empty && undo.Count == 0 && undo.CorruptCount == 0,
                  "undo U9: Clear removes snapshots and corruption diagnostics");
        }

        // -----------------------------------------------------------------------
        // U10 — every gameplay gate is explicit and headless-testable
        // -----------------------------------------------------------------------
        static void U10_SoloPracticeGateMatrix()
        {
            bool matchCannotCapture = !PoolUndoPolicy.CanCaptureSnapshot(PoolGameMode.Match, true, true, true);
            bool matchCannotUndo = !PoolUndoPolicy.CanUndo(PoolGameMode.Match, true, true, true, true, false);
            bool practiceCanCapture = PoolUndoPolicy.CanCaptureSnapshot(PoolGameMode.SoloPractice, true, true, true);
            bool practiceCanUndo = PoolUndoPolicy.CanUndo(PoolGameMode.SoloPractice, true, true, true, true, false);
            bool invalidMapBlocked = !PoolUndoPolicy.CanUndo(PoolGameMode.SoloPractice, false, true, true, true, false);
            bool emptyHistoryBlocked = !PoolUndoPolicy.CanUndo(PoolGameMode.SoloPractice, true, false, true, true, false);
            bool noAuthorityCaptureBlocked = !PoolUndoPolicy.CanCaptureSnapshot(PoolGameMode.SoloPractice, true, false, true);
            bool movingCaptureBlocked = !PoolUndoPolicy.CanCaptureSnapshot(PoolGameMode.SoloPractice, true, true, false);
            bool noAuthorityBlocked = !PoolUndoPolicy.CanUndo(PoolGameMode.SoloPractice, true, true, false, true, false);
            bool movingBlocked = !PoolUndoPolicy.CanUndo(PoolGameMode.SoloPractice, true, true, true, false, false);
            bool shotOpenBlocked = !PoolUndoPolicy.CanUndo(PoolGameMode.SoloPractice, true, true, true, true, true);
            bool transitionGate = PoolUndoPolicy.CanChangeGameMode(false, true)
                                  && !PoolUndoPolicy.CanChangeGameMode(true, true)
                                  && !PoolUndoPolicy.CanChangeGameMode(false, false);

            Check(matchCannotCapture && matchCannotUndo && practiceCanCapture && practiceCanUndo,
                  "undo U10: only SoloPractice can capture snapshots and undo");
            Check(invalidMapBlocked && emptyHistoryBlocked && noAuthorityBlocked && movingBlocked && shotOpenBlocked,
                  "undo U10: invalid map, empty history, no authority, moving table, or open shot blocks undo");
            Check(noAuthorityCaptureBlocked && movingCaptureBlocked,
                  "undo U10: snapshots are captured only while the sim owns a resting table");
            Check(transitionGate, "undo U10: game mode transitions require a settled shot boundary");
        }

        // -----------------------------------------------------------------------
        // U11 — canonical sorting returns the exact new-index -> old-index map
        // -----------------------------------------------------------------------
        static void U11_BeginShotPermutationMapsCueByIdentity()
        {
            var sim = Table(RuleP());
            AddBall(sim, new Vector2(1.7f, 0.9f)); // old 0
            AddBall(sim, new Vector2(0.2f, 0.3f)); // old 1
            AddBall(sim, new Vector2(1.0f, 0.6f)); // old 2 (cue)
            AddBall(sim, new Vector2(2.2f, 1.1f)); // old 3
            sim.CueBallIndex = 2;

            var oldPositions = new Vector3[sim.Balls.Count];
            for (int i = 0; i < oldPositions.Length; i++) oldPositions[i] = sim.Balls[i].Position;

            int[] newToOld = sim.BeginShotWithPermutation();
            bool permutationMatches = newToOld.Length == oldPositions.Length;
            bool sorted = true;
            for (int i = 0; i < newToOld.Length; i++)
            {
                permutationMatches &= sim.Balls[i].Position == oldPositions[newToOld[i]];
                if (i > 0)
                {
                    var previous = sim.Balls[i - 1].Position;
                    var current = sim.Balls[i].Position;
                    if (previous.X > current.X || (previous.X == current.X && previous.Z > current.Z))
                        sorted = false;
                }
            }

            int expectedCueIndex = Array.IndexOf(newToOld, 2);
            Check(permutationMatches && sorted, "undo U11: canonical sort returns a valid parallel-array permutation");
            Check(sim.CueBallIndex == expectedCueIndex, "undo U11: cue index follows the same ball identity through sorting");
        }

        static int FAILS = 0;
        static void Check(bool cond, string msg) { Console.WriteLine($"  {(cond ? "PASS" : "FAIL")}  {msg}"); if (!cond) FAILS++; }
        public static int Fails => FAILS;
    }
}
