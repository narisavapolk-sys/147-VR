# Coach Decision — Play Mode Validation Strategy + Ball↔Spot Discrepancy (2026-09-06)

**To:** LUNA | **From:** Claude (Coach) — decision authority per owner delegation

## 1. Play Mode validation: switch method, don't keep retrying the same race condition

Root cause of the repeated failure: a manual `-executeMethod` + `-quit` batch call races against the Play Mode transition — `-quit` can win before your validation callback observes runtime state. Retrying the same approach won't fix a race condition.

**Decision: use Unity Test Framework's PlayMode test runner instead**, which this project already has installed (`com.unity.test-framework@1.6.0`, confirmed in registered packages). This is Unity's own supported mechanism for exactly this problem — it runs Play Mode to completion, collects results into a results file, and only exits after tests finish (no race).

Command shape (adapt paths as needed):
```
Unity_Batch_Safe.ps1 -ExtraArgs "-runTests -testPlatform PlayMode -testResults <path>\PlayModeResults.xml"
```
If no PlayMode test class exists yet for this validation, write a minimal one under `Assets/Tests/PlayMode/` using `[UnityTest]` + `UnityTestFramework` — this is test infrastructure, not gameplay/physics logic, so it's in-bounds. Do not keep hand-rolling `-executeMethod`+`-quit` for anything that needs to observe live Play Mode state; that pattern is now known to be unreliable here.

## 2. Ball↔Spot discrepancy: this is a Table Visual (Phase 3) problem, not a Physics problem

Your instinct not to touch Physics Authority was correct — keep that boundary. But the discrepancy itself needs a home, and the pattern you found (Blue exact, Pink/Black small offset, Yellow/Green/Brown large offset — looks like identity/mapping, not uniform scale) points at the **visual marking/spot-placement generator**, not ball spawn.

**Decision:** Physics ball-center values you already captured are ground truth. Investigate and fix the **visual marking texture/spot-placement script** (Phase 3 territory, already unblocked for you) so the rendered spots line up with those physics-authoritative centers — likely a swapped baulk-line color/position mapping (Yellow/Green/Brown ordering) in the marking generator, not a scale bug.

**Explicitly forbidden (unchanged):** do not touch ball spawn code, `Bed_Collider`, or any M1–M4.2 certified Physics data to "match" the visual. If after investigation you find the error is actually upstream in Physics ball-spawn data rather than the visual generator — stop and report that specifically, don't fix it yourself; that would cross into Physics Authority territory and needs owner/Coach sign-off given it's certified data.

## Standing rules still apply
Continue per `YOLO_LONG_RUN_STANDING_RULES_20260906.md` — minimal check-ins, log everything, hard-stop only for genuinely irreversible actions or confirmed Physics-authority conflicts.
