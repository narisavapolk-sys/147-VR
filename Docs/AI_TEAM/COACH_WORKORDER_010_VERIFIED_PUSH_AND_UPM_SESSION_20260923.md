# COACH WORK ORDER #010 — Verification of the pack-v8 push, two evidence defects, and the UPM session

**Issued by:** Coach seat · **Date:** 2026-09-23
**Executor:** Luna · **Decision owner:** the project owner
**Base of record:** `integration/008-m53` @ `3b318cce6583114b5dd52f9aa951666cea6c2f3c`
**Predecessors:** WO#009 (provenance gap) · WO#008 · WO#007

---

## PART 0 — WHAT I VERIFIED MYSELF (read-only, against the live remote)

Everything in the provenance section of your report is **confirmed**:

| Check | Result |
|---|---|
| remote tip = `3b318cce6583114b5dd52f9aa951666cea6c2f3c` | ✅ matches |
| all 8 claimed SHAs are **commits** and **reachable** from the tip | ✅ 8/8 |
| commit chain `d209bb45 → 3b318cce` (11 commits) | ✅ WO#006/007/008 + A2 fix + A1.1 + A1.2 + A1.3 + A3 + cleanup ×2 + ignore guard |
| `cleanupbak` tracked paths at tip | ✅ **0** |
| `.gitignore` guard | ✅ lines 117 `*.cleanupbak`, 118 `*.cleanupbak.meta` |
| **A1.3 audit, re-run by me at the tip** | ✅ **rows=14 · MATCH=14 · MISMATCH=0 · MISSING=0 · DUPLICATES=0** |

**The falsifiable prediction from WO#009 held exactly** (14 rows / MATCH=14). The four rows I had relabelled `LOCAL ONLY / UNVERIFIED` are now **remote-verifiable** and the relabel is lifted. **A0.1, A1.1, A1.2, A1.3: CLOSED.**

**A3's source citations — I checked all six, verbatim, at the tip. All accurate:**

```
SnookerBallTracker.cs:185            points = 0,                       -----------------------+
M5ShotObservationBuilder.cs:68       if (b.points == 0) { if (map.ContainsKey(1)) throw ... }    | cue identity
M5ShotObservationBuilder.cs:94-101   case 0: return M5PottedBallType.CueBall;                    |
M5SnookerRulesEngine.cs:280          if (pot.AsColour() != M5Colour.None) result.Add(pot);        | iterators do not
M5SnookerRulesEngine.cs:288          if (pot.Type == type) count++                                | classify the cue
```

So the classification is **supported by source**, and the design statement WO#006 §A3 asked for — *the zero-point cue is not counted as a colour/red ball by the rules iterators* — is **answered: true**.

---

## PART 1 — TWO DEFECTS IN THE NEW EVIDENCE (both mechanical, both cheap)

### 1.1 `A3_I07_reconciliation_20260923.txt` uses an 8-character commit id

```
HEAD at inspection: 55539c16        <- NOT a resolvable identifier
```

This is the **third instance of the same class** in this project: `c974ad4f` mistaken for a commit · the 39-char `WHICH_COMMIT` in the D1 artifact · now 8 chars. Two of those three happened *after* we wrote the rule. The "be careful with SHAs" instruction has therefore **failed as a control**, so replace it with a mechanism:

**Mandatory evidence-artifact header (copy-paste; the command emits 40 chars, do not type the value):**

```powershell
$head = git rev-parse HEAD          # 40 chars, never hand-written
$unity = "6000.4.12f1 (3ca267ce8005)"   # or the literal text: Unity: not involved
@"
$ARTIFACT_NAME
Date: $(Get-Date -Format yyyy-MM-dd)
HEAD: $head
Unity: $unity
Method: <exact command(s) run>
"@ | Set-Content -NoNewline -Encoding utf8 "Artifacts/AI_TEAM/<name>.txt"
```

Add one assertion to the workflow: `git rev-parse HEAD` output must be **40** characters, and the resulting value must `git cat-file -t` as a `commit`. Two commands, no judgement.

### 1.2 The A1.3 artifact's HEAD is stale, and its scope makes it look fine

```
Artifacts/AI_TEAM/index_sha256_audit_20260923.txt:  HEAD: 1f343f0e06b9302bfcebc338cd3e75e8ebc5d21d
                                                    (40 chars ✅ — but 4 commits before the tip)
```

Four commits landed after it (`4ad2cfbc`, `94c6e9b6`, `736080b8`, `3b318cce`). None touches an **indexed** file, and I reproduced **14/14 at `3b318cce`** — so the conclusion stands. But the artifact as written asserts a fact about a tree that is no longer HEAD, and a reader cannot tell whether re-verification was done.

**Required:** amend the artifact to add two lines — `Re-verified at HEAD: 3b318cce6583114b5dd52f9aa951666cea6c2f3c (Coach, 2026-09-23): rows=14 MATCH=14 MISMATCH=0`. Note the scope boundary while you are there:

> The amendment rule added in A1.1 scopes **Coach artifacts under `Docs/AI_TEAM/`**. Executor evidence under `Artifacts/` is not covered by it. Extend the rule's wording to say which class it governs, and add the one-line rule that **executor evidence artifacts are re-pointed, not rewritten**, when they are re-verified against a newer HEAD.

Also: both new artifacts record **no Unity build**. For git-only work that is correct — but make the absence explicit (`Unity: not involved`), otherwise it is indistinguishable from an omission.

---

## PART 2 — A3 / I07: ACCEPTED AS INVALID, WITH THREE ADDENDA

**Accepted.** I07 = **INVALID / STALE TEST EXPECTATION for the null assertion**, and I confirm the reasoning: the test's own name says *Resolves … ByPointsZero*, its next statement (`M5RuntimeTransactionTests.cs:93 BallInfoByPointsZeroMustExist(tracker)`) asserts the existence the null assertion denies, and lines 95–97 assert the resolved identity. Line 92 is the odd one out. No frozen source touched. **A3: CLOSED.**

Three things must be recorded with it, because "stale test expectation" describes the **test**, not the **property**:

1. **The exclusion property is now source-verified but test-unverified.** Line 92 was the **only** assertion of "the calibration cue is not returned as a normal ball". My project-wide scan found no other test asserting it — `M5PostCueReliabilityTests.cs:9-10` asserts a *different* property (rejection when `Sphere.009` exists without cue identity). So after this reclassification, that property rests **only** on the source citations above. Record it as: *property upheld by source (`M5SnookerRulesEngine.cs:280/288`), not covered by any test*. Add a case to the **next** register version — do **not** touch the frozen register.

2. **Open question — `FindBall` prefix semantics.** The declaration is `public BallInfo FindBall(string namePrefix)`; if it is a prefix match, then a future `FindBall("Sphere")` would silently return the calibration cue. **INFERENCE from the parameter name — I did not read the body.** Today no production caller does this (all callers use `"White_CueBall"` / `"Red"` / `"Blue"`, and `M5FinalCertification.cs:41` treats `Sphere.009` as the cue ball deliberately). So this is a **latent hazard, not a defect**: post-RC hardening note, one comment or guard — **do not act on it now**.

3. **The repair is not authorised yet** — and when it comes, it must be declared exactly like A2: *assertion-form fix; no expectation changed* (or, if `FindBall`'s contract is the thing that is wrong, that is a design change and returns to Coach first). Your artifact already says this. Keep it.

**Status wording to use:** `I07 = INVALID (stale test expectation, source-verified); test not repaired; property test-coverage gap recorded.`

---

## PART 3 — THE UPM SESSION (one session, then stop)

Correct not to have opened Unity. When you do, in this order, and **nothing else in the session**:

**Pre-flight (before launching anything):**
1. `Unity: not involved` is no longer true — this is a **run**, so the full F9 header applies (40-char HEAD, `UNITY_EDITOR=6000.4.12f1 (3ca267ce8005)`, exact argv, CWD, timeout used).
2. Enumerate Unity / UnityPackageManager / UnityHub processes. **If any orphan exists, kill it first** — starting on top of a live orphan wastes the session.
3. Check `-logFile`: if it was used, `Editor.log` is *by design* empty. Record which log path was actually written, or you will "find" a nonexistent anomaly.

**H-UPM-1 (highest prior, from our own D-3.0 result):** `Upm-7732` names a PID; `-s <pid>` is a **liveness token**. If PID 7732 was already dead, the server self-terminates in ~1.01 s and a 30 s handshake timeout is the **expected** result. One command: does that PID exist, and did the launcher pass it? If dead → **explained**; this is a *launcher invocation* finding, not a UPM finding.

**H-UPM-2:** launcher exit **3** = timeout **and leaves the process running**. Record the exit code of the failed attempt; an orphan holding the IPC name is a sufficient cause for the next run's timeout.

**Then exactly one retry:** fresh IPC name · liveness PID that outlives the session (never `explorer.exe`) · CWD = the PackageManager `Server` folder · **timeout > 30 s** — the 2,668 ms `list-packages` figure was measured *warm*; a cold UPM on a version-changed editor is a different cost.

**Cap: one session.** If H-UPM-1/H-UPM-2 do not explain it: capture the evidence artifact and stop. **Do not reopen the UPM architecture** — that question is settled and is not on the critical path.

**Then, still in that same session if it comes up: R11 and R36 only.** Two cautions:

- Select them by name filter from `Assets/Editor/M5_3_RulesUnitTests.cs`. Everything else in that assembly is out of scope for this run.
- **`Tracker_Resolves_CalibrationSphere009_ByPointsZero` will still be red** — line 92 is unchanged at the tip. That is the *already-classified* I07, **not a regression**. Report it as `I07 — INVALID, known, unchanged`, so the run cannot be misread as a new failure.
- Judge by endpoint/co-occurrence evidence, never by launcher exit code alone (the documented stock-`-quit` quirk can produce exit 4 on a healthy run).

**A2 stays OPEN until R11 and R36 both PASS with the artifact committed.** Do not let "A2 source fix ✅" drift into "A2 done".

---

## PART 4 — AFTER THAT

`A4` (commit the owner decision wording) → **S5** (REAL10 / M5 re-run) → **S3** (M4.2 fresh re-capture) → then the convergence path from WO#007 Part B (008 integration → GATE 5 → audio/UI → RC).

S3/S5 remain correctly gated on a working Unity — that is a resource gate, not a rules-contract gate.

**Index arithmetic for the next pack:** the index currently holds **14** rows; this work order and WO#009 add 2 → after pack v9 the audit must read **16 rows · MATCH=16 · MISMATCH=0 · MISSING=0 · DUPLICATES=0**. Anything else means the pack did not land as intended.

---

## PART 5 — PROHIBITIONS AND STOP CONDITIONS

Unchanged: certified core (V007 / M5.1 / M5.2 / **M5.3 frozen rules** / D8 / W1) · `147VR_M53_VALIDATE` · the real project beyond what is authorised · system environment · firewall · the tracked launcher · any second authority.

**STOP** on: a PASS declared on an unreachable commit · any commit id recorded in fewer than 40 chars · evidence re-verified against a newer HEAD but still labelled with the old one · a test repaired without the "assertion-form fix; no expectation changed" declaration · the frozen register edited · a `.log` artifact · UPM work past the one-session cap · a Unity session started with an orphan alive · a known-red test reported as a regression.

## PART 6 — WHAT COACH DOES NOT DO

Coach does not push, does not authorise the I07 test repair, does not decide the RC scope, and does not sign off the release. Here Coach re-ran the audit, checked six source citations, and replaced a failed rule with a mechanism.
