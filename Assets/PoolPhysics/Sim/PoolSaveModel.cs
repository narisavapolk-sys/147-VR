using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PoolPhysics.Sim
{
    // ---------------------------------------------------------------------------
    // PoolSaveModel — pure-C# match-state save system (S2 #2, headless-first).
    //
    // CONTRACT (mirrors NineBallRules/NineBallTests QA discipline):
    //   * The model is plain data — no UnityEngine, no engine types. It lives in
    //     Assets/PoolPhysics/Sim so the Unity compiler AND the headless project
    //     (PoolPhysics/Tests, glob Assets/PoolPhysics/Sim/*.cs) build the SAME file.
    //   * PoolSaveCodec.ToJson is DETERMINISTIC: fixed field order, invariant
    //     culture, no whitespace — same state on any machine => byte-identical.
    //     It also refuses (throws ArgumentException) to serialise an invalid state
    //     so authoring bugs surface at the call site, not in the file.
    //   * TryFromJson is the ONLY entry point for untrusted input (disk, network
    //     replay). It never throws on any input shape; malformed / out-of-schema
    //     saves return false + a short cause. Accepts known schema only — no
    //     silent upgrades, unknown fields are rejected (strict forward-compat:
    //     bump SchemaVersion when the schema changes).
    //   * Save semantics: everything is recorded AT REST. Pocketed balls stay in
    //     the save (with Pocketed=true) so rules evaluators can rebuild
    //     remaining-ball sets; a scratched cue is Pocketed=true + BallInHand=true
    //     and is re-spawned by the Unity layer on the next placement.
    //   * Simplification (documented, deliberate): a completed break shot always
    //     clears IsBreakShot; re-break / push-out policies stay a bridge-layer
    //     decision (NineBallRules already treats them as out of scope).
    // ---------------------------------------------------------------------------
    public sealed class PoolSaveBall
    {
        public int Number;      // 0 == cue ball; NineBall objects 1..9, Snooker 1..21
        public double X, Z;     // table-plane rest position, playfield coordinates (metres)
        public bool Pocketed;   // true == currently in a pocket (cue: scratch, ball-in-hand next)
    }

    public sealed class PoolSaveState
    {
        public const int CurrentSchema = 1;

        public int SchemaVersion = CurrentSchema;
        public string RulesMode = "NineBall"; // "NineBall" | "Snooker"
        public int CurrentPlayer;             // 0 or 1
        public int[] RacksWon = new int[2];   // frames (snooker) / racks (9-ball) per player
        public int ShotNumber;                // 0-based shot counter; break shot == 0
        public bool BallInHand;               // current shooter may place the cue anywhere legal
        public bool IsBreakShot;              // next shot is the rack break
        public PoolSaveBall[] Balls;          // cue (0) always present; every rack ball listed once
    }

    public static class PoolSaveValidator
    {
        /// <summary>Semantic validation. Returns null when the state is loadable,
        /// otherwise a short cause string (checked exhaustively by PoolSaveTests).</summary>
        public static string Validate(PoolSaveState s)
        {
            if (s == null) return "state is null";
            if (s.SchemaVersion != PoolSaveState.CurrentSchema)
                return $"SchemaVersion {s.SchemaVersion} not supported (current {PoolSaveState.CurrentSchema})";
            if (s.RulesMode != "NineBall" && s.RulesMode != "Snooker")
                return $"unknown RulesMode '{s.RulesMode ?? "<null>"}'";
            if (s.CurrentPlayer != 0 && s.CurrentPlayer != 1) return $"CurrentPlayer {s.CurrentPlayer} not in [0,1]";
            if (s.RacksWon == null || s.RacksWon.Length != 2) return "RacksWon must contain exactly 2 entries";
            if (s.RacksWon[0] < 0 || s.RacksWon[1] < 0 || s.RacksWon[0] > 100000 || s.RacksWon[1] > 100000)
                return "RacksWon out of range";
            if (s.ShotNumber < 0) return "ShotNumber is negative";
            if (s.Balls == null || s.Balls.Length == 0) return "Balls is empty";
            int maxBalls = s.RulesMode == "NineBall" ? 10 : 22;
            if (s.Balls.Length > maxBalls)
                return $"too many balls ({s.Balls.Length}) for {s.RulesMode} (max {maxBalls})";
            int cueCount = 0;
            var seenObjects = new HashSet<int>();
            for (int i = 0; i < s.Balls.Length; i++)
            {
                var b = s.Balls[i];
                if (b == null) return $"Balls[{i}] is null";
                if (!double.IsFinite(b.X) || !double.IsFinite(b.Z))
                    return $"Balls[{i}] (#{b.Number}) has a non-finite position";
                if (Math.Abs(b.X) > PoolSaveCodec.MaxAbsPosition || Math.Abs(b.Z) > PoolSaveCodec.MaxAbsPosition)
                    return $"Balls[{i}] (#{b.Number}) position outside sanity window (+-{PoolSaveCodec.MaxAbsPosition} m)";
                if (b.Number == 0) { cueCount++; continue; }
                int maxNumber = s.RulesMode == "NineBall" ? 9 : 21;
                if (b.Number < 1 || b.Number > maxNumber)
                    return $"Balls[{i}] number {b.Number} out of range for {s.RulesMode} (1..{maxNumber})";
                if (!seenObjects.Add(b.Number))
                    return $"duplicate ball number {b.Number}";
            }
            if (cueCount != 1) return $"cue ball (0) appears {cueCount} times (must be exactly 1)";
            return null;
        }
    }

    public static class PoolSaveCodec
    {
        public const int MaxInputLength = 1_000_000; // 1 MB cap: a valid save is < 8 KB
        public const int MaxBalls = 64;
        public const double MaxAbsPosition = 100.0;  // table-plane sanity window (not table-specific)

        // -----------------------------------------------------------------------
        // Deterministic writer. Fixed layout:
        //   {"SchemaVersion":1,"RulesMode":"NineBall","CurrentPlayer":0,
        //    "RacksWon":[0,0],"ShotNumber":0,"BallInHand":false,"IsBreakShot":true,
        //    "Balls":[{"Number":0,"X":2.175,"Z":0.81,"Pocketed":false}, ...]}
        // -----------------------------------------------------------------------
        public static string ToJson(PoolSaveState s)
        {
            string why = PoolSaveValidator.Validate(s);
            if (why != null) throw new ArgumentException("refusing to serialise an invalid save state: " + why);
            var sb = new StringBuilder(256 + s.Balls.Length * 48);
            sb.Append('{');
            sb.Append("\"SchemaVersion\":").Append(s.SchemaVersion.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"RulesMode\":");
            Quote(sb, s.RulesMode);
            sb.Append(",\"CurrentPlayer\":").Append(s.CurrentPlayer.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"RacksWon\":[");
            for (int i = 0; i < s.RacksWon.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(s.RacksWon[i].ToString(CultureInfo.InvariantCulture));
            }
            sb.Append(']');
            sb.Append(",\"ShotNumber\":").Append(s.ShotNumber.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"BallInHand\":").Append(s.BallInHand ? "true" : "false");
            sb.Append(",\"IsBreakShot\":").Append(s.IsBreakShot ? "true" : "false");
            sb.Append(",\"Balls\":[");
            for (int i = 0; i < s.Balls.Length; i++)
            {
                if (i > 0) sb.Append(',');
                var b = s.Balls[i];
                sb.Append("{\"Number\":").Append(b.Number.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"X\":").Append(b.X.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"Z\":").Append(b.Z.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"Pocketed\":").Append(b.Pocketed ? "true" : "false");
                sb.Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// Strict reader for untrusted input. Never throws; malformed saves return
        /// false with a cause. Accepted states always satisfy
        /// <see cref="PoolSaveValidator.Validate"/> and round-trip byte-identically
        /// through <see cref="ToJson"/>.
        /// </summary>
        public static bool TryFromJson(string text, out PoolSaveState state, out string error)
        {
            state = null; error = null;
            try
            {
                if (text == null || text.Length == 0) { error = "empty input"; return false; }
                if (text.Length > MaxInputLength) { error = $"input too large ({text.Length} > {MaxInputLength})"; return false; }
                var r = new Reader(text);
                var s = r.ReadState();
                r.Ws();
                if (!r.Eof) r.Err("trailing content after save object");
                error = PoolSaveValidator.Validate(s);
                if (error != null) { return false; }
                state = s;
                return true;
            }
            catch (Exception ex)
            {
                state = null;
                error = "parse error: " + ex.Message;
                return false;
            }
        }

        static void Quote(StringBuilder sb, string s)
        {
            if (s == null) { sb.Append("null"); return; }
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // -----------------------------------------------------------------------
        // Hand-rolled scanner (strict JSON subset, no external parser needed:
        // keeps the Sim folder dependency-free for both Unity and headless builds).
        // Internal ParseErr(FormatException) calls are converted to `error` by
        // TryFromJson's catch — the public API never throws.
        // -----------------------------------------------------------------------
        sealed class Reader
        {
            readonly string S;
            public int I;
            public Reader(string s) { S = s ?? string.Empty; }
            public bool Eof => I >= S.Length;

            public void Err(string why)
            {
                string near = "";
                if (I < S.Length) near = $" near '{S[I]}' at {I}";
                throw new FormatException($"{why}{near}");
            }

            public void Ws()
            {
                while (I < S.Length && (S[I] == ' ' || S[I] == '\t' || S[I] == '\r' || S[I] == '\n')) I++;
            }

            double ReadNumber()
            {
                Ws();
                int start = I;
                bool leadZero = false;
                if (I < S.Length && S[I] == '-') I++;
                if (I >= S.Length || S[I] < '0' || S[I] > '9') Err("expected a digit");
                if (S[I] == '0') { I++; leadZero = true; }
                else while (I < S.Length && S[I] >= '0' && S[I] <= '9') I++;
                if (leadZero && I < S.Length && S[I] >= '0' && S[I] <= '9') Err("leading zero in number");
                if (I < S.Length && S[I] == '.')
                {
                    I++;
                    if (I >= S.Length || S[I] < '0' || S[I] > '9') Err("expected fractional digit");
                    while (I < S.Length && S[I] >= '0' && S[I] <= '9') I++;
                }
                if (I < S.Length && (S[I] == 'e' || S[I] == 'E'))
                {
                    I++;
                    if (I < S.Length && (S[I] == '+' || S[I] == '-')) I++;
                    if (I >= S.Length || S[I] < '0' || S[I] > '9') Err("expected exponent digit");
                    while (I < S.Length && S[I] >= '0' && S[I] <= '9') I++;
                }
                string tok = S.Substring(start, I - start);
                if (!double.TryParse(tok, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                    Err($"number '{tok}' does not parse");
                if (!double.IsFinite(v)) Err($"number '{tok}' is not finite");
                return v;
            }

            public int ReadInt()
            {
                double d = ReadNumber();
                if (d != Math.Truncate(d) || d < int.MinValue || d > int.MaxValue) Err("expected an integer");
                return (int)d;
            }

            public bool ReadBool()
            {
                Ws();
                if (I + 4 <= S.Length && S.Substring(I, 4) == "true") { I += 4; return true; }
                if (I + 5 <= S.Length && S.Substring(I, 5) == "false") { I += 5; return false; }
                Err("expected true/false");
                return false; // unreachable
            }

            public string ReadString()
            {
                Ws();
                if (I >= S.Length || S[I] != '"') Err("expected a string");
                I++;
                var sb = new StringBuilder(24);
                while (true)
                {
                    if (I >= S.Length) Err("unterminated string");
                    char c = S[I];
                    if (c == '"') { I++; return sb.ToString(); }
                    if (c == '\\')
                    {
                        I++;
                        if (I >= S.Length) Err("unterminated escape");
                        char e = S[I++];
                        switch (e)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (I + 4 > S.Length) Err("truncated \\u escape");
                                if (!int.TryParse(S.Substring(I, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int u4))
                                    Err("bad \\u escape digits");
                                I += 4;
                                sb.Append((char)u4);
                                break;
                            default: Err($"bad escape '\\{e}'"); break;
                        }
                        continue;
                    }
                    if (c < 0x20) Err($"raw control char 0x{((int)c):x2} inside string");
                    sb.Append(c);
                    I++;
                }
            }

            // ---- schema-specific array readers (fixed shapes, depth-capped) ----

            int[] ReadRacksWon()
            {
                Ws();
                if (I >= S.Length || S[I] != '[') Err("expected array for RacksWon");
                I++;
                var vals = new List<int>(2);
                Ws();
                if (I < S.Length && S[I] == ']') { I++; }
                else
                {
                    while (true)
                    {
                        vals.Add(ReadInt());
                        Ws();
                        if (I < S.Length && S[I] == ',') { I++; continue; }
                        if (I < S.Length && S[I] == ']') { I++; break; }
                        Err("expected ',' or ']' in RacksWon");
                    }
                }
                if (vals.Count != 2) Err($"RacksWon must have exactly 2 entries (got {vals.Count})");
                return vals.ToArray();
            }

            PoolSaveBall[] ReadBallArray()
            {
                Ws();
                if (I >= S.Length || S[I] != '[') Err("expected array for Balls");
                I++;
                var balls = new List<PoolSaveBall>(4);
                Ws();
                if (I < S.Length && S[I] == ']') { I++; return balls.ToArray(); }
                while (true)
                {
                    if (balls.Count >= MaxBalls) Err("too many ball objects");
                    balls.Add(ReadBall());
                    Ws();
                    if (I < S.Length && S[I] == ',') { I++; continue; }
                    if (I < S.Length && S[I] == ']') { I++; break; }
                    Err("expected ',' or ']' in Balls");
                }
                return balls.ToArray();
            }

            PoolSaveBall ReadBall()
            {
                Ws();
                if (I >= S.Length || S[I] != '{') Err("expected ball object");
                I++;
                var b = new PoolSaveBall();
                bool hasN = false, hasX = false, hasZ = false, hasP = false;
                Ws();
                if (I < S.Length && S[I] == '}') { I++; }
                else
                {
                    while (true)
                    {
                        Ws();
                        if (I >= S.Length || S[I] != '"') Err("expected ball field name");
                        string key = ReadString();
                        Ws();
                        if (I >= S.Length || S[I] != ':') Err("expected ':'");
                        I++;
                        switch (key)
                        {
                            case "Number": b.Number = ReadInt(); hasN = true; break;
                            case "X": b.X = ReadNumber(); hasX = true; break;
                            case "Z": b.Z = ReadNumber(); hasZ = true; break;
                            case "Pocketed": b.Pocketed = ReadBool(); hasP = true; break;
                            default: Err($"unknown ball field '{key}'"); break;
                        }
                        Ws();
                        if (I < S.Length && S[I] == ',') { I++; continue; }
                        if (I < S.Length && S[I] == '}') { I++; break; }
                        Err("expected ',' or '}' in ball object");
                    }
                }
                if (!hasN) Err("ball missing Number");
                if (!hasX) Err("ball missing X");
                if (!hasZ) Err("ball missing Z");
                if (!hasP) Err("ball missing Pocketed");
                return b;
            }

            public PoolSaveState ReadState()
            {
                Ws();
                if (I >= S.Length || S[I] != '{') Err("expected save object");
                I++;
                var st = new PoolSaveState();
                bool hasVer = false, hasMode = false, hasPlayer = false, hasRacks = false,
                     hasShot = false, hasHand = false, hasBreak = false, hasBalls = false;
                int[] racks = new[] { 0, 0 };
                PoolSaveBall[] balls = new PoolSaveBall[0];
                Ws();
                if (I < S.Length && S[I] == '}') { I++; }
                else
                {
                    while (true)
                    {
                        Ws();
                        if (I >= S.Length || S[I] != '"') Err("expected field name");
                        string key = ReadString();
                        Ws();
                        if (I >= S.Length || S[I] != ':') Err("expected ':'");
                        I++;
                        switch (key)
                        {
                            case "SchemaVersion": st.SchemaVersion = ReadInt(); hasVer = true; break;
                            case "RulesMode": st.RulesMode = ReadString(); hasMode = true; break;
                            case "CurrentPlayer": st.CurrentPlayer = ReadInt(); hasPlayer = true; break;
                            case "RacksWon": racks = ReadRacksWon(); hasRacks = true; break;
                            case "ShotNumber": st.ShotNumber = ReadInt(); hasShot = true; break;
                            case "BallInHand": st.BallInHand = ReadBool(); hasHand = true; break;
                            case "IsBreakShot": st.IsBreakShot = ReadBool(); hasBreak = true; break;
                            case "Balls": balls = ReadBallArray(); hasBalls = true; break;
                            default: Err($"unknown save field '{key}'"); break;
                        }
                        Ws();
                        if (I < S.Length && S[I] == ',') { I++; continue; }
                        if (I < S.Length && S[I] == '}') { I++; break; }
                        Err("expected ',' or '}' in save object");
                    }
                }
                if (!hasVer) Err("missing SchemaVersion");
                if (!hasMode) Err("missing RulesMode");
                if (!hasPlayer) Err("missing CurrentPlayer");
                if (!hasRacks) Err("missing RacksWon");
                if (!hasShot) Err("missing ShotNumber");
                if (!hasHand) Err("missing BallInHand");
                if (!hasBreak) Err("missing IsBreakShot");
                if (!hasBalls) Err("missing Balls");
                st.RacksWon = racks;
                st.Balls = balls;
                return st;
            }
        }
    }
}
