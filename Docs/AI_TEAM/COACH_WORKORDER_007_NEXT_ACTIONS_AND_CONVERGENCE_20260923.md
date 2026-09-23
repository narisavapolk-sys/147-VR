# COACH WORK ORDER #007 — Next actions, and a convergence plan so this finishes

**Issued by:** Coach seat · **Date:** 2026-09-23
**Executor:** Luna · **Decision owner:** the project owner
**Base of record:** `integration/008-m53` @ `d209bb4535a33d4f00cff22c9da47689649ac31a`
**Predecessors:** WO#006 (S2a disposition) · WO#005 (S1b/S2) · WO#004 (integration line)

---

## PART A — DO THIS NEXT, IN THIS ORDER

### A0 — Land the pending documentation (10 minutes)

Apply the Coach pack, verify the hash first, then push. Nothing else in this step.

### A1 — Integrity trio (3 small commits, ~30 minutes total)

1. **Amendment rule** — add the rule from WO#006 §4.2 to `Docs/147VR_AI_WORK_PROTOCOL.md` (or wherever the working protocol lives) so it is binding rather than advisory. One paragraph.
2. **Remove the tracked debris** `Assets/Scripts/Quest/M5ShotLifecycle.cs.cleanupbak` — its own commit. It sits in the folder that holds the frozen M5 lifecycle and is a "which file is real?" hazard.
3. **Confirm** `git status` clean and the index consistent (every artifact row matches `git show HEAD:<path>` bytes).

### A2 — Fix the two assertions, then re-run only those two cases (~1 hour)

**Change:** `Assets/Editor/M5_3_RulesUnitTests.cs` lines **161** and **408** — drop the spurious `.ToString()` so they compare the enum member like every sibling assertion in the same file (lines 64 / 77 / 88). The engine side is already correct (`M5SnookerRulesEngine.cs:163`).

**Declare in the commit message, in these words:** *assertion-form fix; no expectation changed.* That distinction is what makes this safe — the fix cannot launder a failure.

**Then:** re-run **only** R11 and R36. Expected: both PASS. Produce a new S2a evidence artifact (the frozen register stays frozen; append a run record, do not edit the register). If either still fails, that is a real finding — stop and report.

### A3 — Reconcile I07 before classifying it (~30 minutes)

1. Locate the assertion that produced `expected FindBall("Sphere.009") == null` — **file and line**. The register says the case expects the opposite (`Resolve Sphere.009 as cue identity`).
2. If that assertion is not the registered case → **I07 = INVALID for this run**, correct the table, re-run the registered case.
3. If the register row itself is stale and a design change made null correct → issue a **register version 2** citing the decision record that authorised the calibration-cue registration. Never edit the register in place.
4. **Targeted design check (same session, one question):** confirm the zero-point calibration cue is excluded from the **rules iterators** — ball-on determination, foul detection, frame-end ball counting — not merely from scoring. A zero-point ball that is still iterated as a ball is how wrong foul calls are born.

### A4 — Make the I19/I20 negative reproducible, and ask the owner one question (~30 minutes)

Record the exact search terms, the directories scanned (`Assets/Scripts/Quest`, `Assets/Scripts/AAA`) and the command used, so `NOT_IMPLEMENTED` is a repeatable negative rather than an assertion. Then put **one question** to the owner — *is multi-frame match progression in scope for this release?* — with both branches costed:

- **in scope** → it is a **new authority** (contract §6: one owner, no competing state machine, no hidden fallback) sitting adjacent to the frozen M5.3 rules core. It needs explicit authorisation *before* implementation, and it is a design task, not a patch.
- **out of scope** → record it as `OUT OF SCOPE` in the state doc and in the release definition, so it stops being re-read as debt every session.

### A5 — Then run the physics campaign: S3 and S5 (~half a day)

These are **not blocked** by the rules contract items. Run them as their own evidence campaign on this line, in this order, and do not mix their artifacts with S2 evidence.

- **S3** — fresh current-profile **M4.2** re-capture (the current M4.2 result is explicitly historical).
- **S5** — **REAL10 / M5** re-run on `integration/008-m53` under 12f1.
- Acceptance for both: F9 tracked artifact with `WHICH_COMMIT` (40 chars) · `UNITY_EDITOR=6000.4.12f1 (3ca267ce8005)` · `TARGET_TREE_HEAD/BRANCH` · log path + SHA + bytes · the decisive lines verbatim · `RESULT`.
- Reminder: judge by endpoint/co-occurrence evidence, **never** by launcher exit code alone (the documented stock-`-quit` quirk can produce exit 4 on a healthy run).

### A6 — Report

One short status block: what closed, what is still open, next action, and any STOP. No narrative.

---

## PART B — WHY THIS KEEPS FEELING UNFINISHED, AND HOW IT ENDS

*(Coach's read of the evidence. The owner decides; this is a recommendation.)*

The project has spent its recent sessions on **toolchain and gate hygiene** — and that was the right call, because 12f1 could not even open a project. But gate-clearing has no natural end: it expands to fill whatever time is available. Meanwhile the remaining work is not research, it is **content and device work**, and no amount of additional evidence will move it.

**The honest arithmetic from the map:** 3 of 14 phases are closed; 11 are open; two open items are genuinely large — **multiplayer (0 first-party artefacts; every network/NGO file in the tree is inside `Packages/com.meta.xr.sdk.core`)** and **audio (0 `.wav`/`.mp3`, no AudioMixer)** — and **three phases cannot start until a build runs on a headset, which has never happened**.

### B1 — Define the release, once, and write it down

Proposed **Release Candidate scope** for owner approval:

> **Single-player snooker.** `147VR_MainScene` with the 008 table integrated, rules validated end-to-end, the tablet options menu and HUD as they ship today, MR passthrough mode, minimum viable audio, and a signed build that survives a full session on Quest 2 and Quest 3.

**Explicitly deferred, in writing:** multiplayer (Phase 10) · additional venue scenes beyond those already shipping · any new rules variant. Deferring multiplayer is the single largest schedule lever available, and it costs nothing today: it cannot be built on top of an unvalidated rules layer anyway.

### B2 — Define the device gate so it can actually close

**GATE 5 (device)** — acceptance, all required:
install the APK on Quest 2 **and** Quest 3 via a documented route · open `147VR_MainScene` · complete **one full frame** end-to-end: cue → physics → rules → score → turn → frame end · no crash and no unexplained stall for 30 minutes of play · capture the log + FPS/frame-time baseline · record the exact build (commit, keystore, build config). When that passes, Phases 4 / 7 / 12 unblock together.

### B3 — The convergence rule

From now on, **every session must do one of exactly two things**:

1. **clear a named gate**, or
2. **add shipping content**.

Nothing else. No new evidence streams, no new probes, no new tooling, no re-litigating a closed gate — unless a *proven* new failure appears. This is the rule that ends the loop.

### B4 — Critical path (shortest route to "finished")

```
A1–A4 (rules contract closed)
   → S3 + S5 (physics evidence on this line)
      → 008 non-destructive integration          [Phase 3 closes]
         → device gate (GATE 5)                  [4 / 7 / 12 unblock]
            → audio (sourced, not coded) + UI flows
               → polish + RC validation          [Phase 13]
```

**Multiplayer is not on this path.** It is its own milestone after RC, if the owner still wants it.

---

## PART C — PROHIBITIONS AND STOP CONDITIONS

Unchanged: certified core (V007 / M5.1 / M5.2 / **M5.3 frozen rules** / D8 / W1) · `147VR_M53_VALIDATE` · the real project beyond what is authorised · system environment · firewall · the tracked launcher · any second authority.

**STOP** on: an expectation edited to match observed output · a register edited after results · a fix bundled with unrelated work · a case left classified as FAIL without a located defect · implementation started in an area that needs authorisation first · evidence that cannot be tied to a commit and an editor build.

**No claim of "rules validated"** until every one of the 63 cases is PASS or explicitly OUT OF SCOPE by owner decision.

## PART D — WHAT COACH DOES NOT DO

Coach does not authorise the release scope, the device gate definition, or the merge of the APK checkpoint into the integration line. Coach reviews, challenges and specifies acceptance; the owner decides and the shipping tree still needs its own verification at the RC gate.
