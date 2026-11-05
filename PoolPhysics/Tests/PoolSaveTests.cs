using System;
using System.Collections.Generic;
using System.Numerics;

namespace PoolPhysics.Sim
{
    // ---------------------------------------------------------------------------
    // PoolSaveTests — headless save-system QA (mode "save [fuzzN]").
    // Covers the full save contract shipped in PoolSaveModel:
    //   T1 pristine-rack roundtrip + canonical re-encode byte-identity
    //   T2 foul/scratch + snooker-mode state roundtrip
    //   T3 reject matrix (malformed inputs must be REJECTED, never thrown)
    //   T4 seeded malformed-input fuzz (never throw; accepted states stay stable)
    //   T5 save→shot→save determinism (same script => byte-identical save JSON)
    //   T6 file-load path == code-build path in the real simulator (BeginShot
    //      ordering must make the save's ball order irrelevant)
    // CONTRACT mirrors NineBallTests (AddCue before first BeginShot, RunShot
    // snapshot mapping, JudgeSim-style paging) — see NineBallTests header.
    // ---------------------------------------------------------------------------
    public static class PoolSaveTests
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
            T1_RackRoundTrip();
            T2_FoulAndSnookerRoundTrip();
            T3_RejectMatrix();
            T4_FuzzMalformed(fuzzN);
            T5_SaveShotSaveDeterminism();
            T6_LoadShotMatchesDirect();
        }

        // -----------------------------------------------------------------------
        // state builders (positions mirror NineBallTests S1 rack geometry exactly)
        // -----------------------------------------------------------------------
        static PoolSaveState NineBallPristineState()
        {
            Vector2 foot = new Vector2(LEN * 0.25f, WID * 0.5f), head = new Vector2(LEN * 0.75f, WID * 0.5f);
            float rowSpacing = R * 2f * MathF.Sqrt(3f) * 0.5f + 2e-4f;
            int[][] rows = { new[]{1}, new[]{2,3}, new[]{4,9,5}, new[]{6,7,8} };
            var balls = new List<PoolSaveBall> { new PoolSaveBall { Number = 0, X = head.X, Z = head.Y } };
            for (int rr = 0; rr < rows.Length; rr++)
                for (int c = 0; c < rows[rr].Length; c++)
                {
                    float lateral = (c - (rows[rr].Length - 1) * 0.5f) * (R * 2f + 4e-4f);
                    balls.Add(new PoolSaveBall { Number = rows[rr][c], X = foot.X - rr * rowSpacing, Z = WID * 0.5f + lateral });
                }
            return new PoolSaveState
            {
                SchemaVersion = PoolSaveState.CurrentSchema,
                RulesMode = "NineBall",
                CurrentPlayer = 0,
                RacksWon = new[] { 0, 0 },
                ShotNumber = 0,
                BallInHand = false,
                IsBreakShot = true,
                Balls = balls.ToArray()
            };
        }

        static PoolSaveState AfterScratchState()
        {
            var s = NineBallPristineState();
            s.CurrentPlayer = 1;
            s.RacksWon = new[] { 1, 0 };
            s.ShotNumber = 9;
            s.BallInHand = true;
            s.IsBreakShot = false;
            var cue = BallOf(s, 0);
            cue.Pocketed = true;                 // scratch: cue is out, ball-in-hand next
            BallOf(s, 7).Pocketed = true;
            return s;
        }

        static PoolSaveState SnookerFrameState()
        {
            var balls = new List<PoolSaveBall> { new PoolSaveBall { Number = 0, X = 1.45, Z = 0.81 } };
            for (int i = 1; i <= 15; i++)
                balls.Add(new PoolSaveBall { Number = i, X = 2.20 + 0.01 * i, Z = 0.81, Pocketed = true });
            for (int n = 16; n <= 21; n++)
                balls.Add(new PoolSaveBall { Number = n, X = 1.75, Z = 0.30 + 0.10 * (n - 16) });
            return new PoolSaveState
            {
                SchemaVersion = PoolSaveState.CurrentSchema,
                RulesMode = "Snooker",
                CurrentPlayer = 1,
                RacksWon = new[] { 3, 2 },
                ShotNumber = 41,
                BallInHand = false,
                IsBreakShot = false,
                Balls = balls.ToArray()
            };
        }

        // -----------------------------------------------------------------------
        // T1 — pristine rack roundtrip
        // -----------------------------------------------------------------------
        static void T1_RackRoundTrip()
        {
            var st = NineBallPristineState();
            string j = PoolSaveCodec.ToJson(st);
            bool ok = PoolSaveCodec.TryFromJson(j, out var back, out var err);
            Check(ok && err == null, err != null ? $"save T1: roundtrip rejected: {err}" : "save T1: pristine 9-ball rack roundtrip accepted");
            if (ok)
            {
                Check(PoolSaveCodec.ToJson(back) == j, "save T1: canonical re-encode byte-identical");
                Check(SameState(st, back), "save T1: field-wise identical");
            }
        }

        // -----------------------------------------------------------------------
        // T2 — foul state (scratch/ball-in-hand) + snooker-mode frame roundtrip
        // -----------------------------------------------------------------------
        static void T2_FoulAndSnookerRoundTrip()
        {
            var st = AfterScratchState();
            string j = PoolSaveCodec.ToJson(st);
            bool ok = PoolSaveCodec.TryFromJson(j, out var back, out var err);
            Check(ok && SameState(st, back),
                  err != null ? $"save T2: scratch state rejected: {err}" : "save T2: scratch/ball-in-hand roundtrip identical");

            var sn = SnookerFrameState();
            string sj = PoolSaveCodec.ToJson(sn);
            bool ok2 = PoolSaveCodec.TryFromJson(sj, out var snback, out var err2);
            Check(ok2 && SameState(sn, snback),
                  err2 != null ? $"save T2: snooker state rejected: {err2}" : "save T2: snooker 22-ball frame roundtrip identical");

            // duplicate RED numbering is not (reader-side: write the invalid state by hand)
            var dup = SnookerFrameState();
            dup.Balls[2].Number = dup.Balls[1].Number;
            Check(!PoolSaveCodec.TryFromJson(JsonForce(dup), out _, out var err3) && err3 != null,
                  $"save T2: duplicate snooker ball numbers rejected ({err3})");
        }

        // -----------------------------------------------------------------------
        // T3 — reject matrix: every row must be rejected with a cause, no throw.
        // Float anchors are taken from the ACTUAL writer output (write "R" emits
        // the real double like 2.175000071526, never the neat 2.175).
        // -----------------------------------------------------------------------
        static void T3_RejectMatrix()
        {
            string good = PoolSaveCodec.ToJson(NineBallPristineState());
            int len = good.Length;

            // surgical mutations of the canonical document (integer anchors are exact)
            ExpectReject(good.Replace("\"SchemaVersion\":1", "\"SchemaVersion\":2"), "bad version");
            ExpectReject(good.Replace("\"SchemaVersion\":1", "\"SchemaVersion\":\"1\""), "version as string");
            ExpectReject(good.Replace("\"RulesMode\":\"NineBall\"", "\"RulesMode\":\"EightBall\""), "bad rules mode");
            ExpectReject(good.Replace("\"CurrentPlayer\":0", "\"CurrentPlayer\":7"), "bad player");
            ExpectReject(good.Replace("\"ShotNumber\":0", "\"ShotNumber\":-1"), "negative shot number");
            ExpectReject(good.Replace("\"RacksWon\":[0,0]", "\"RacksWon\":[1]"), "short RacksWon");
            ExpectReject(good.Replace("\"RacksWon\":[0,0]", "\"RacksWon\":[1,2,3]"), "long RacksWon");
            ExpectReject(good.Replace("\"RacksWon\":[0,0]", "\"RacksWon\":[-9,7]"), "negative racks");
            ExpectReject(good.Replace("\"BallInHand\":false", "\"BallInHand\":1"), "int for bool");
            ExpectReject(good.Replace("\"IsBreakShot\":true", "\"IsBreakShot\":TRUE"), "uppercase true");
            ExpectReject(good.Replace("\"Balls\":[{", "\"Balls\":[],{"), "malformed balls array");
            ExpectReject(good.Substring(0, len / 2), "truncated");
            ExpectReject("{\"junk\":1,\"SchemaVersion\":1,\"RulesMode\":\"NineBall\",\"CurrentPlayer\":0,\"RacksWon\":[0,0],\"ShotNumber\":0,\"BallInHand\":false,\"IsBreakShot\":true,\"Balls\":[{\"Number\":0,\"X\":0,\"Z\":0,\"Pocketed\":false}]}", "unknown field");

            // number-token mutations, anchored to the FIRST ball's "X": value
            ExpectReject(ReplaceFirstNumberValue(good, "NaN"), "NaN position");
            ExpectReject(ReplaceFirstNumberValue(good, "Infinity"), "Infinity position");
            ExpectReject(ReplaceFirstNumberValue(good, "1e400"), "overflow exponent");
            ExpectReject(ReplaceFirstNumberValue(good, "\"2.175\""), "number as string");
            ExpectReject(ReplaceFirstNumberValue(good, "0175"), "leading-zero number");
            ExpectReject(ReplaceFirstNumberValue(good, "2. 175"), "space inside number");

            // unknown field injected INSIDE the first ball object
            int firstClose = good.IndexOf('}', good.IndexOf("\"Balls\"", StringComparison.Ordinal));
            ExpectReject(good.Insert(firstClose, ",\"pwn\":1"), "unknown field injected in ball");

            // semantic rejects via ball-row surgery
            string cue = BallRow(good, 0);
            ExpectReject(good.Replace(cue, cue + "," + cue), "two cue balls");
            string one = BallRow(good, 1);
            ExpectReject(good.Replace(one, one.Replace("\"Number\":1,", "\"Number\":9,")), "duplicate object number");

            // non-object root / other shapes
            ExpectReject("[]", "array root");
            ExpectReject("null", "null root");
            ExpectReject("\"save\"", "string root");
            ExpectReject("", "empty input");
            ExpectReject(null, "null input");

            // benign control char inside a string constant
            ExpectReject(good.Replace("NineBall", "Nine\tBall"), "raw tab inside string");

            // accepted-but-equivalent whitespace (JSON standard) MUST still load:
            bool okWs = PoolSaveCodec.TryFromJson("{ \"SchemaVersion\" : 1 , \"RulesMode\" : \"NineBall\" , \"CurrentPlayer\" : 0 , \"RacksWon\" : [ 0 , 0 ] , \"ShotNumber\" : 0 , \"BallInHand\" : false , \"IsBreakShot\" : true , \"Balls\" : [ { \"Number\" : 0 , \"X\" : 2.175 , \"Z\" : 0.81 , \"Pocketed\" : false } ] }", out var wsState, out var wsErr);
            Check(okWs, $"save T3: whitespace-widened single-cue save accepted ({wsErr})");
        }

        static string ReplaceFirstNumberValue(string good, string rawValue)
        {
            int i = good.IndexOf("\"X\":", StringComparison.Ordinal);          // first ball row
            int comma = good.IndexOf(',', i);
            return good.Substring(0, i + 4) + rawValue + good.Substring(comma);
        }

        static string BallRow(string good, int number)
        {
            string key = $"{{\"Number\":{number},";
            int i = good.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return null;
            int j = good.IndexOf('}', i);
            return good.Substring(i, j - i + 1);
        }

        static void ExpectReject(string text, string label)
        {
            try
            {
                bool ok = PoolSaveCodec.TryFromJson(text, out _, out var err);
                Check(!ok && err != null && err.Length > 0, $"save T3 reject: {label} ({(ok ? "WRONGLY ACCEPTED" : err)})");
            }
            catch (Exception ex)
            {
                Check(false, $"save T3 reject: {label} THREW {ex.GetType().Name}: {ex.Message}");
            }
        }

        // ToJson refuses invalid states; T2 needs one purely as reader food, so
        // this writer deliberately skips validation (input to the strict reader).
        static string JsonForce(PoolSaveState s)
        {
            // hand-rolled minimal writer for states that MAY be invalid (reader-side fuzz food)
            var sb = new global::System.Text.StringBuilder();
            sb.Append("{\"SchemaVersion\":").Append(s.SchemaVersion);
            sb.Append(",\"RulesMode\":\"").Append(s.RulesMode).Append('"');
            sb.Append(",\"CurrentPlayer\":").Append(s.CurrentPlayer);
            sb.Append(",\"RacksWon\":[").Append(s.RacksWon[0]).Append(',').Append(s.RacksWon[1]).Append(']');
            sb.Append(",\"ShotNumber\":").Append(s.ShotNumber);
            sb.Append(",\"BallInHand\":").Append(s.BallInHand ? "true" : "false");
            sb.Append(",\"IsBreakShot\":").Append(s.IsBreakShot ? "true" : "false");
            sb.Append(",\"Balls\":[");
            for (int i = 0; i < s.Balls.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"Number\":").Append(s.Balls[i].Number)
                  .Append(",\"X\":").Append(s.Balls[i].X.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(",\"Z\":").Append(s.Balls[i].Z.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(",\"Pocketed\":").Append(s.Balls[i].Pocketed ? "true" : "false").Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        // -----------------------------------------------------------------------
        // T4 — seeded malformed-input fuzz: the reader must never throw, accepted
        // states must validate and re-encode stably (deterministic codec).
        // -----------------------------------------------------------------------
        static void T4_FuzzMalformed(int n)
        {
            if (n <= 0) { Check(true, "save fuzz: skipped (n<=0)"); return; }
            var rng = new Random(20261104);
            string good = PoolSaveCodec.ToJson(NineBallPristineState());
            string junkSet = "  {}[]\",:;0123456789.eE-+\\/\t\nAaXx";
            int accepted = 0, badNoErr = 0, badUnstable = 0, threw = 0;
            for (int it = 0; it < n; it++)
            {
                var m = new List<char>(good);
                int nMut = 1 + rng.Next(3);
                for (int k = 0; k < nMut && m.Count > 0; k++)
                {
                    int op = rng.Next(4), at = rng.Next(m.Count);
                    char c = junkSet[rng.Next(junkSet.Length)];
                    if (op == 0) m[at] = c;                                          // replace
                    else if (op == 1) m.RemoveAt(at);                                // delete
                    else if (op == 2) m.Insert(at, c);                               // insert
                    else { if (at < m.Count) m.RemoveRange(at, m.Count - at); } // truncate tail at random point
                }
                string t = new string(m.ToArray());
                try
                {
                    if (PoolSaveCodec.TryFromJson(t, out var st, out var err))
                    {
                        accepted++;
                        string verr = PoolSaveValidator.Validate(st);
                        bool stable = verr == null
                                      && PoolSaveCodec.TryFromJson(PoolSaveCodec.ToJson(st), out var st2, out var err2)
                                      && err2 == null
                                      && PoolSaveCodec.ToJson(st2).Equals(PoolSaveCodec.ToJson(st), StringComparison.Ordinal);
                        if (!stable) badUnstable++;
                    }
                    else if (err == null || err.Length == 0) badNoErr++;
                }
                catch (Exception ex)
                {
                    threw++;
                    if (threw <= 2) Console.WriteLine($"  save fuzz [#it={it}] THREW {ex.GetType().Name}: {ex.Message}");
                }
            }
            Check(threw == 0 && badNoErr == 0 && badUnstable == 0,
                  $"save fuzz {n}: never-throws + accepted-stable (accepted={accepted}, threw={threw}, noErr={badNoErr}, unstable={badUnstable})");
        }

        // -----------------------------------------------------------------------
        // shared sim plumbing (mirrors NineBallTests helpers; cue = AddCue LAST in
        // the code-build path, and the save path reproduces the same geometry)
        // -----------------------------------------------------------------------
        sealed class RunShotResult { public StepResult Res; public List<EventSimulator.PoolEventSim> Events; public int[] Numbers; }

        static void AddBall(EventSimulator s, Vector2 pos)
            => s.Balls.Add(new Ball { Position = new Vector3(pos.X, R, pos.Y), Radius = R, Mass = 0.16f, Active = true });

        static void AddCue(EventSimulator s, Vector2 pos)
        {
            AddBall(s, pos);
            s.CueBallIndex = s.Balls.Count - 1;   // before the first BeginShot (contract)
        }

        static void StrikeCue(EventSimulator s, Vector2 cuePos, Vector2 target, float speed)
        {
            var dir = target - cuePos;
            s.BeginShot();
            for (int i = 0; i < s.Balls.Count; i++)
            { var b = s.Balls[i]; b.Radius = R; b.Mass = 0.16f; b.Active = true; s.Balls[i] = b; }
            var c = s.Balls[s.CueBallIndex];
            float L2 = dir.X * dir.X + dir.Y * dir.Y;
            var d = L2 > 1e-12f ? dir / MathF.Sqrt(L2) : new Vector2(1f, 0f);
            c.Velocity = new Vector3(d.X * speed, 0f, d.Y * speed);
            s.Balls[s.CueBallIndex] = c;
        }

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

        static bool CueEscaped(EventSimulator s)
        {
            foreach (var b in s.Balls)
                if (b.Active && !b.Pocketed && (b.Position.X < -1.6f * R || b.Position.X > s.Table.PlayfieldSize.X + 1.6f * R
                    || b.Position.Z < -1.6f * R || b.Position.Z > s.Table.PlayfieldSize.Y + 1.6f * R)) return true;
            return false;
        }

        static List<(Vector2 pos, int num)> PlacedFromState(PoolSaveState st)
        {
            var placed = new List<(Vector2, int)>();
            for (int i = 0; i < st.Balls.Length; i++)
                if (!st.Balls[i].Pocketed)
                    placed.Add((new Vector2((float)st.Balls[i].X, (float)st.Balls[i].Z), st.Balls[i].Number));
            return placed;
        }

        // save → simulator (skips potted rows; cue must be on-table for a fresh shot)
        static EventSimulator SimFromState(PoolSaveState st, PhysicsProfile p)
        {
            var s = Table(p);
            for (int i = 0; i < st.Balls.Length; i++)
            {
                var b = st.Balls[i];
                if (b.Pocketed) continue;
                AddBall(s, new Vector2((float)b.X, (float)b.Z));
            }
            // index the cue AFTER adding (any list position works — BeginShot remaps).
            for (int i = 0; i < st.Balls.Length; i++)
                if (!st.Balls[i].Pocketed && st.Balls[i].Number == 0) { s.CueBallIndex = FindNearestAddedCueIndex(s, st.Balls[i]); break; }
            return s;
        }

        static int FindNearestAddedCueIndex(EventSimulator s, PoolSaveBall cue)
        {
            for (int i = 0; i < s.Balls.Count; i++)
                if (Math.Abs(s.Balls[i].Position.X - cue.X) < 1e-4f && Math.Abs(s.Balls[i].Position.Z - cue.Z) < 1e-4f) return i;
            return -1;
        }

        static PoolSaveBall BallOf(PoolSaveState st, int number)
        {
            for (int i = 0; i < st.Balls.Length; i++) if (st.Balls[i].Number == number) return st.Balls[i];
            return null;
        }

        // run the canonical opening break from a loaded state, then fold the
        // verdict back into a NEW save state (the Unity layer will do the same)
        static PoolSaveState PlayOneShot(PoolSaveState st, out NineBallVerdict verdict)
        {
            var sim = SimFromState(st, RuleP());
            var placed = PlacedFromState(st);
            Vector2 cue = new Vector2((float)BallOf(st, 0).X, (float)BallOf(st, 0).Z);
            Vector2 one = new Vector2((float)BallOf(st, 1).X, (float)BallOf(st, 1).Z);
            StrikeCue(sim, cue, one, 4.5f);
            var shot = RunShotC(sim, placed, 12f);
            var remaining = new HashSet<int>(); foreach (var b in st.Balls) if (b.Number != 0 && !b.Pocketed) remaining.Add(b.Number);
            verdict = NineBallRules.EvaluateShot(shot.Events, sim.CueBallIndex, shot.Numbers, remaining, CueEscaped(sim), st.IsBreakShot);

            var outState = new PoolSaveState
            {
                SchemaVersion = st.SchemaVersion,
                RulesMode = st.RulesMode,
                CurrentPlayer = verdict.ShooterContinues ? st.CurrentPlayer : (st.CurrentPlayer + 1) % 2,
                RacksWon = new[] { st.RacksWon[0], st.RacksWon[1] },
                ShotNumber = st.ShotNumber + 1,
                BallInHand = verdict.BallInHandForOpponent,
                IsBreakShot = false, // simplicity contract (see PoolSaveModel header)
                Balls = new PoolSaveBall[sim.Balls.Count]
            };
            if (verdict.LegalNinePot) outState.RacksWon[st.CurrentPlayer]++;
            for (int i = 0; i < sim.Balls.Count; i++)
            {
                var b = sim.Balls[i];
                outState.Balls[i] = new PoolSaveBall
                {
                    Number = shot.Numbers[i],
                    X = b.Position.X, Z = b.Position.Z,
                    // cue-escaped-off-table never fires a Pocket event — stamp it explicitly
                    Pocketed = b.Pocketed || (shot.Numbers[i] == 0 && verdict.CuePocketed)
                };
            }
            return outState;
        }

        // -----------------------------------------------------------------------
        // T5 — save → one scripted shot → save; the whole chain twice must be
        // byte-identical (codec + engine + rules determinism through the save path).
        // -----------------------------------------------------------------------
        static void T5_SaveShotSaveDeterminism()
        {
            string a = PlayScriptAndSaveJson();
            string b = PlayScriptAndSaveJson();
            Check(a == b, $"save T5: script→save byte-identical twice (len={a.Length}{(a == b ? "" : ", JSON DIFFERS")})");
            if (a != b)
            {
                for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
                {
                    char ca = i < a.Length ? a[i] : '?', cb = i < b.Length ? b[i] : '?';
                    if (ca != cb) { Console.WriteLine($"  first diff at {i}: '{ca}' vs '{cb}'"); break; }
                }
            }
        }

        static string PlayScriptAndSaveJson()
        {
            var st = PlayOneShot(NineBallPristineState(), out var v);
            return PoolSaveCodec.ToJson(st);
        }

        // -----------------------------------------------------------------------
        // T6 — the FILE LOAD path must produce the same physics as the code-build
        // path (ball ORDER in the save is irrelevant: BeginShot canonicalises).
        // -----------------------------------------------------------------------
        static void T6_LoadShotMatchesDirect()
        {
            // Path A: pristine state (cue FIRST) → json → load → play
            var pristine = NineBallPristineState();
            bool okA = PoolSaveCodec.TryFromJson(PoolSaveCodec.ToJson(pristine), out var loaded, out var errA);
            Check(okA, $"save T6: pristine save loads ({errA})");
            var endA = PlayOneShot(loaded, out var vA);

            // Path B: code-build like NineBallTests (objects first, cue added LAST)
            var simB = Table(RuleP());
            var placedB = new List<(Vector2, int)>();
            Vector2 foot = new Vector2(LEN * 0.25f, WID * 0.5f), head = new Vector2(LEN * 0.75f, WID * 0.5f);
            float rowSpacing = R * 2f * MathF.Sqrt(3f) * 0.5f + 2e-4f;
            int[][] rows = { new[]{1}, new[]{2,3}, new[]{4,9,5}, new[]{6,7,8} };
            for (int rr = 0; rr < rows.Length; rr++)
                for (int c = 0; c < rows[rr].Length; c++)
                {
                    float lateral = (c - (rows[rr].Length - 1) * 0.5f) * (R * 2f + 4e-4f);
                    var p = new Vector2(foot.X - rr * rowSpacing, WID * 0.5f + lateral);
                    AddBall(simB, p); placedB.Add((p, rows[rr][c]));
                }
            AddCue(simB, head); placedB.Add((head, 0));
            var prune = pristine;
            Vector2 cuep = new Vector2((float)BallOf(prune, 0).X, (float)BallOf(prune, 0).Z);
            Vector2 onep = new Vector2((float)BallOf(prune, 1).X, (float)BallOf(prune, 1).Z);
            StrikeCue(simB, cuep, onep, 4.5f);
            var shotB = RunShotC(simB, placedB, 12f);
            var vB = NineBallRules.EvaluateShot(shotB.Events, simB.CueBallIndex, shotB.Numbers,
                                                new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, CueEscaped(simB), true);

            Check(vA.Foul == vB.Foul
                  && vA.FirstContactNumber == vB.FirstContactNumber
                  && vA.CuePocketed == vB.CuePocketed
                  && vA.Potted.Count == vB.Potted.Count,
                  $"save T6: verdict match A(save-file) vs B(code-rack): foul {vA.Foul}=={vB.Foul}, first {vA.FirstContactNumber}=={vB.FirstContactNumber}");

            double maxDelta = 0; bool pocketsMatch = true;
            var endByNumB = new Dictionary<int, (Vector3 pos, bool potted)>();
            for (int i = 0; i < simB.Balls.Count; i++)
                if (shotB.Numbers[i] >= 0) endByNumB[shotB.Numbers[i]] = (simB.Balls[i].Position, simB.Balls[i].Pocketed);
            for (int i = 0; i < endA.Balls.Length; i++)
            {
                int n = endA.Balls[i].Number;
                if (!endByNumB.TryGetValue(n, out var eB)) { pocketsMatch = false; continue; }
                maxDelta = Math.Max(maxDelta,
                    (new Vector3((float)endA.Balls[i].X, R, (float)endA.Balls[i].Z) - eB.pos).Length());
                pocketsMatch &= (eB.potted == endA.Balls[i].Pocketed);
            }
            Check(maxDelta <= 1e-6 && pocketsMatch,
                  $"save T6: end positions identical through the file path (maxDelta={maxDelta:E2}, pockets={pocketsMatch})");
        }

        static bool SameState(PoolSaveState a, PoolSaveState b)
        {
            if (a.SchemaVersion != b.SchemaVersion || a.RulesMode != b.RulesMode
                || a.CurrentPlayer != b.CurrentPlayer || a.ShotNumber != b.ShotNumber
                || a.BallInHand != b.BallInHand || a.IsBreakShot != b.IsBreakShot) return false;
            if ((a.RacksWon?.Length ?? -1) != (b.RacksWon?.Length ?? -1)) return false;
            if (a.RacksWon != null)
                for (int i = 0; i < a.RacksWon.Length; i++) if (a.RacksWon[i] != b.RacksWon[i]) return false;
            if ((a.Balls?.Length ?? -1) != (b.Balls?.Length ?? -1)) return false;
            if (a.Balls != null)
                for (int i = 0; i < a.Balls.Length; i++)
                {
                    var x = a.Balls[i]; var y = b.Balls[i];
                    if (x.Number != y.Number || x.Pocketed != y.Pocketed || x.X != y.X || x.Z != y.Z) return false;
                }
            return true;
        }

        static int FAILS = 0;
        static void Check(bool cond, string msg) { Console.WriteLine($"  {(cond ? "PASS" : "FAIL")}  {msg}"); if (!cond) FAILS++; }
        public static int Fails => FAILS;
    }
}
