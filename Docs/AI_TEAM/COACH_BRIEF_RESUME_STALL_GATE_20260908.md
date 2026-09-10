# Coach Brief — Resume with Stall Detector as Safety Gate, Not a Fix (2026-09-08 19:20)

**To:** LUNA | **From:** Claude (Coach), reflecting owner's explicit framing — agreed in full.

## Two categories, kept strictly separate
**Actually fixed (root cause confirmed, documented):**
1. npx PATH contamination → `Unity_Batch_Safe.ps1` + `Docs/TOOLING_KNOWN_ISSUES_UPM_LAUNCHER.md`
2. Competing launcher scripts → old one deprecated, one canonical launcher remains

**NOT fixed, still open:**
- Tonight's post-Licensing/Physics stall — root cause **Unknown**. Chrome/resource-contention is a hypothesis only, not to be written up as the cause anywhere until actual evidence confirms it.

## Standing procedure going forward — a safety gate, not a workaround pretending to be a fix
```
Launch → Observe log growth / CPU time → progressing? continue.
       → stalled? confirm target method hasn't started yet → terminate cleanly → preserve evidence → retry.
```
This is monitoring + safe recovery, explicitly not claimed as a root-cause fix. If it recurs, the recurrence pattern itself (load at the time, how far it got, timing) becomes the evidence for isolating the real cause — don't jump to a guessed fix before that.

## Important boundary
Tonight's stall must **not** be conflated with an M5.1 failure — the run never reached `M5_1_Install_TMP.Run()`, so there is no M5.1 result (pass or fail) to report from it. M5.1 remains exactly where it was: BLOCKED/pending, no code implication either way.

## Go-ahead
Resume the long run now under this framing. Standing rules otherwise unchanged: checkpoint-only reporting, no PASS without persisted evidence, canonical launcher only, Physics Authority/V007-WPBSA baseline frozen, hard-stop only for irreversible actions or genuine Authority conflicts.
