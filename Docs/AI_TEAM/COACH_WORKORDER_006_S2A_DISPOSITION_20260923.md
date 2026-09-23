# COACH WORK ORDER #006 — S2a disposition: refine two classifications, fix the index, unblock correctly

**Issued by:** Coach seat · **Date:** 2026-09-23
**Executor:** Luna · **Decision owner:** the project owner
**Base of record:** `integration/008-m53` @ `d209bb4535a33d4f00cff22c9da47689649ac31a`
**Predecessors:** `COACH_WORKORDER_005_S1B_S2_20260923.md` · `COACH_DISPOSITION_R3_PACKAGES_LOCK_20260923.md`

---

## 0. VERDICT ON S2a

**The harness run is a PASS, the classifications are not all final.**

Independently verified from GitHub (not from the report):

| Check | Result |
|---|---|
| Branch tip | `d209bb4535a33d4f00cff22c9da47689649ac31a` ✅ |
| Commit order | `d209bb4` (S2a stop) ← `e3c6bc4` (**register**) ← `eeef233`/`4ae1961` (F9b) ← `135da3b` (H1/H2) — the register precedes every result by construction ✅ |
| Register | `Artifacts/M5_3/S2a_RULE_CASE_REGISTER_20260923.md` — **63 case rows**, each with `input`, `expected` and a `basis` naming the test method or the scan target. **No outcomes recorded** (the only PASS/FAIL words in the file are rules text, lines 76–77) ⇒ genuinely pre-registered ✅ |
| Evidence artifact | `Artifacts/M5_3/S2a_rules_validation_20260923.txt` canonical sha256 = `b465d8ec1607ea0dbd539c9ffb577ba0c83253c17862906aabd32690f4b5b177` = **the recorded value** ✅ |
| Counts | `SUMMARY: PASS=58 FAIL=3 INVALID=0 NOT_IMPLEMENTED=2 TOTAL=63` — internally consistent ✅ |
| Discipline statements | `MODE: log-driven reflection harness; NUnit runner not used` · `LOG_SHA256: f64036a7…` · `LOG_BYTES: 225590` · **`STOP. No M5.3 frozen rules source was changed. No test was altered to make S2a pass.`** ✅ |
| H1 (D1 artifact) | Fixed: `WHICH_COMMIT: 4e1b131d11b9b14df4a422b110039386ac09209d` — now 40 characters ✅ |
| H2 (D1 artifact) | Adopted and improved: *"no dependent declared minimum changed anywhere in the graph; the entire lock diff is only the two resolved version strings, with no dependency edge added/removed or requested version changed. Resolver elevation therefore originated from Unity 6000.4.12f1 bundled resolver/metadata, not from a package in this project graph."* ✅ |
| F9b | `Tools/D3A_L2_Probe/` = probe + README, **outside `Assets/`**, committed before its evidence; `Artifacts/M5_3/F9b_D3A_L2_repro_20260923.txt` present ✅ |

**⇒ S2a = PASS as a harness run. Phase 2 rules validation = NOT complete** (3 non-diagnostic cases + 2 unimplemented behaviours).

---

## 1. R11 / R36 — VERIFIED as a test-contract defect, located to the line

Coach fetched the source at the tip. The claim is correct, and the defect is narrower and safer than "the test checks with a string":

`Assets/Editor/M5_3_RulesUnitTests.cs`

```
 64:        Assert.That(d.FoulReasons, Does.Contain(M5FoulReason.ColourPottedWhenRedOn));
 77:        Assert.That(d.FoulReasons, Does.Contain(M5FoulReason.WrongFirstContact));
 88:        Assert.That(d.FoulReasons, Does.Contain(M5FoulReason.Miss));

161:        Assert.That(d.FoulReasons, Does.Contain(M5FoulReason.RedPottedWhenColourOn.ToString()));
408:        Assert.That(d.FoulReasons, Does.Contain(M5FoulReason.RedPottedWhenColourOn.ToString()));
```

Every sibling asserts against the **enum member**; only lines **161** and **408** append `.ToString()`. Since the sibling assertions pass, `d.FoulReasons` is enum-typed, so those two comparisons are type-mismatched and can never succeed. And the engine side is correct:

```
M5SnookerRulesEngine.cs:163:                AddFoul(fouls, M5FoulReason.RedPottedWhenColourOn);
```

**⇒ The expectation is right; only the comparison form is wrong.** This matters for the disposition: removing the two spurious `.ToString()` calls changes **no expectation**, so there is no way to launder a failure through this fix. It is the safe class of test change.

**Required framing:** declare this as a **harness/assertion defect fix**, distinct from an **expectation change**. The frozen register stays frozen; only the assertion *form* is corrected, and the fix commit must say so in those words.

---

## 2. I07 — **NOT yet a "stale test expectation". Reclassify as INVALID until reconciled**

There is a contradiction between the pre-registered case and the observed failure that has not been reconciled.

**Pre-registered (register row):**

```
| I07 | ball identity | Calibration Sphere.009 points=0 | Resolve Sphere.009 as cue identity | M5RuntimeTransactionTests::Tracker_Resolves_CalibrationSphere009_ByPointsZero |
```

**Observed failure (evidence artifact):**

```
FAIL: AssertionException expected FindBall("Sphere.009") == null.
```

Those two statements disagree about what the case expects: the register expects **resolution**, the failure message expects **null**. Meanwhile the cited source test expects resolution:

```
M5RuntimeTransactionTests.cs:85:    public void Tracker_Resolves_CalibrationSphere009_ByPointsZero()
M5RuntimeTransactionTests.cs:91:            var expected = tracker.FindBall("Sphere.009");
M5RuntimeTransactionTests.cs:97:            Assert.AreEqual("Sphere.009", resolved.name);
```

and the tracker deliberately registers it:

```
SnookerBallTracker.cs:168:        // Calibration scenes use Sphere.009 as the authoritative cue ball while
SnookerBallTracker.cs:171:        GameObject calibrationCue = GameObject.Find("Sphere.009");
```

**Therefore one of these is true, and we must know which before the case is classified:**

- **(a)** the harness executed an assertion that is **not** the registered case (the registered expectation was never exercised) ⇒ **I07 is INVALID / non-diagnostic for this run**, and the pass/fail table must be corrected;
- **(b)** the register row is wrong and a second, unregistered expectation (`FindBall(...) == null`) exists in the harness ⇒ **the register and the harness are out of sync**, which is a pre-registration integrity defect.

**Do not resolve this by picking the convenient label.** Locate the assertion that produced `expected FindBall("Sphere.009") == null`, name file and line, and then reclassify. If the resolution behaviour is confirmed as intended and authorised, the register gets a **new version** (not an in-place edit) citing the decision record.

**Targeted check to run at the same time (design-level, one question):** the calibration cue is registered in the tracker with `points == 0`. Confirm it is excluded from the **rules iterators** (ball-on determination, foul detection, frame-end counting), not merely from scoring. A zero-point ball that is still iterated as a ball is exactly the sort of thing that produces a wrong foul call later.

---

## 3. I19 / I20 — NOT_IMPLEMENTED accepted, with a stronger negative

Accepted as `NOT_IMPLEMENTED`. Two requirements:

1. **Make the negative reproducible.** The register's basis is "source scan". Record instead: the exact search terms, the directories scanned (`Assets/Scripts/Quest`, `Assets/Scripts/AAA`), and the command used — so the negative is repeatable rather than asserted.
2. **The scope decision is the owner's.** Snooker implies frames; a match progression is therefore either in scope or explicitly out. If **in scope**, this is not a patch task: it is a **new authority** (contract §6 — one owner, no competing state machine, no hidden fallback) sitting directly adjacent to the frozen M5.3 rules core, so it requires explicit authorisation **before** implementation. If **out of scope**, say so in the state doc and in the release definition, and keep the 2 cases as `OUT OF SCOPE` rather than `NOT_IMPLEMENTED` so the signal is not re-read as debt each session.

---

## 4. TWO INTEGRITY FIXES (do these in the same commit as this work order)

**4.1 The artifact index is stale for WO#005.** When `135da3b` edited the D1 artifact, it also updated the hash cross-reference *inside* `COACH_WORKORDER_005_S1B_S2_20260923.md` — correct, and Coach accepts that amendment. But the index was not updated with it:

```
COACH_ARTIFACT_INDEX_20260923.md:19:  | `COACH_WORKORDER_005_S1B_S2_20260923.md` | `7eb45fbd8563b5bfe51914d9a8b62c549085c487cffe8c6413bd734551c137ef` |
actual pushed content:                bf1041bea95e1a8f65c0d9c3b038c796d842755b04a09a957b9097c1e3a4ad69
```

A reader verifying WO#005 against the index gets a mismatch. Fix the row.

**4.2 Declare an amendment rule** (so this class stops recurring). Proposed, for the owner to adopt or replace:

> A Coach artifact may be amended by the executor only when the change is a **factual correction of a cross-reference** (hash, path, commit id) and (a) the amendment commit names the artifact and the field changed, (b) the index row is updated in the same commit, (c) the artifact's own header gains one line recording the amendment and its commit. Any change to a Coach artifact's **verdict, criteria or recommendation** comes back to Coach as a new work order instead.

**4.3 Hygiene.** `Assets/Scripts/Quest/M5ShotLifecycle.cs.cleanupbak` is **tracked** at the tip, sitting in the folder that holds the frozen M5 lifecycle. It is debris in a protected area and a source of "which file is real?" confusion. Remove it (or move it out of `Assets/`), as its own tiny commit.

---

## 5. ORDER OF WORK

1. §4 integrity fixes (index row; amendment rule; the `.cleanupbak`) — small, first.
2. §1 fix the two `.ToString()` assertions → re-run **only** those 2 cases → both must PASS; new evidence artifact, register untouched, fix declared as an assertion-form fix.
3. §2 reconcile I07 → reclassify honestly (INVALID or register-v2) → then run the intended case → plus the rules-iterator check for the zero-point cue.
4. §3 make the I19/I20 negative reproducible; owner decides scope.
5. Then continue **S3** (fresh current-profile M4.2 re-capture) and **S5** (REAL10 / M5 re-run) — these are physics-side and are **not blocked** by the rules contract defects. Run them as their own evidence, on this line, and do not mix them with S2 evidence.
6. **No claim may be made that "Phase 2 rules are validated"** until 63/63 cases are either PASS or explicitly OUT OF SCOPE with an owner decision on record.

---

## 6. PROHIBITIONS AND STOP CONDITIONS

Unchanged: certified core (V007 / M5.1 / M5.2 / **M5.3 frozen rules** / D8 / W1) · `147VR_M53_VALIDATE` · the real project beyond what is authorized · system environment · firewall · the tracked launcher · a second authority of any kind.

**STOP** on: an expectation edited to match observed output · a register edited in place after results · a fix bundled with anything else · an unreconciled case left classified as FAIL · a test environment that is not the declared authority · evidence whose commit/editor cannot be resolved.

## 7. WHAT COACH DOES NOT DO

Coach does not re-open or re-certify M5.3, does not authorise implementation of the match-state authority, and does not sign off the shipping tree. The refusal to edit tests toward green is the correct behaviour and is recorded as such.
