# Coach Evidence Audit — 2026-09-22

## Purpose
This document records the independent source-level audit reported by Coach against commit `3f7cf73af45c...` on branch `checkpoint/m5-certified-baseline-20260915`.

## Boundary
Coach does not run Unity Editor/PlayMode, build/install Quest APKs, or perform physical Quest hardware tests. Therefore VERIFIED below means the repository contains the claimed source/artifact and that the artifact text matches the stated claim; it does not independently prove runtime behavior.

## Audit verdicts

| Claim | Evidence inspected | Verdict |
|---|---|---|
| `3f7cf73a` was pushed | `git ls-remote` | VERIFIED |
| 008 handoff is in Git | `Docs/M5_3_008_INTEGRATION_HANDOFF_20260922.md` | VERIFIED |
| Rules 43/43 | `M5_3_RulesUnitTests.cs` contains 43 `[Test]` methods | VERIFIED (source inventory) |
| Runtime 20/20 | `M5RuntimeTransactionTests.cs` contains 20 `[Test]` methods | VERIFIED (source inventory) |
| Post-Cue 3/3 | `M5PostCueReliabilityTests.cs` contains 3 `[Test]` methods | VERIFIED (source inventory) |
| REAL10 PASS | `Docs/M5_REAL_10SHOT_CERT_20260919.json`: fired 10, resolved 10, settled 10, errors 0 | VERIFIED (artifact content) |
| Post-reset XR regression | `Docs/M5_XR_POST_RESET_CERT_20260920.json` | VERIFIED (artifact content) |
| 43/43 and 20/20 were actually executed | No committed NUnit/runtime result artifact found by audit | NOT VERIFIED |
| MissingScripts=0 | Validator exposes the field, but no committed decisive log line was found | NOT VERIFIED |
| Quest 2/3 hardware | Certificate explicitly says `NOT_RUN` | NOT RUN |

## High-priority evidence issues

### F1 — Evidence chain
The repository ignores `*.log`, while several certification JSON files reference paths under `Docs/UnityBatchLogs/`. Those log files are therefore not available as Git evidence. Until decisive evidence is embedded in committed certification artifacts or committed as non-log evidence, those claims remain artifact-backed rather than independently replayable.

### F2 — Conflicting REAL10 final artifact
`Docs/M5_REAL_10SHOT_FINAL_20260920.json` was found with status RUNNING and zero shots. This conflicts with the authoritative REAL10 certificate and must be explicitly marked superseded rather than left ambiguous.

### F3 — Missing executable test output
The source inventories establish the number of test methods, but no committed NUnit XML or equivalent decisive runtime output was found for the claimed 43/43 and 20/20 execution results. This is a provenance gap, not evidence of test failure.

### F4 — Hardware
Quest 2/3 hardware certification remains NOT RUN.

### F5 — Repeated abort pattern
Two prior runs reportedly aborted around ~3 GB during calibration/reset-frame work. This recurring failure mode should be diagnosed rather than masked by repeated retries.

### F6 — Empty integration test classes
`M6GameplayIntegrationTests.cs` and `SnookerRulesTests.cs` contain zero test methods in the audited source inventory.

## Current engineering boundary
- M5 gameplay core remains locked.
- Historical V007 Golden evidence is preserved.
- Production integration target is Blender 008.
- Do not replace the Main Scene table prefab with raw Blender 008 until a Unity-side provenance + geometry/collider/calibration contract is written and checked.
- REAL10 should be rerun only after the 008 integration gate is satisfied.

## Audit provenance
This file is a repository record of Coach's 2026-09-22 audit report as supplied to LUNA. It is not a substitute for Unity runtime execution or hardware testing.
