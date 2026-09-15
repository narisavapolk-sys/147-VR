# M5 Provenance & Next Steps — 2026-09-15

## Purpose
Durable project record for the M5.1/M5.2 certified state, the M5ShotLifecycle provenance investigation, and the approved path into M5.3 Rules.

## Evidence searched
- Existing M5 decision/certification documents under `Docs/AI_TEAM/` were reviewed by filename/content search.
- `COACH_M5_DECISION_BRIEF_20260906.md` records the M5 authority-chain plan.
- `COACH_CERTIFICATION_M5_1_20260908.md` records M5.1 PASS/VERIFIED and the pending M5.3–M5.6 chain.
- `COACH_CERTIFICATION_M5_2_20260908.md` records M5.2 PASS/VERIFIED.
- Existing `Docs/` content was searched for M5.3, provenance, isKinematic, and Rules references.

## Current certified baseline
- M5.1 REAL Physics: CERTIFIED on the local runtime variant used for the 2026-09-14 10-shot certification.
- M5.2 Event Contract: CERTIFIED.
- Certified local `M5ShotLifecycle.cs`: 3857 bytes, SHA-256 `0e2b0f47444a453bef173ac98eac851f02fb1304059a4a6798a6cfeb59f24d38`.
- Immutable evidence directory: `C:\Temp\147VR_M5_CERTIFIED_BASELINE\`.
- Preservation manifest SHA-256: `ac31b6deb8f6acb0a8577b7c5be4f0f9026f07557c6faedde3dcc0450a0cbaa4`.

## Provenance finding
The source-integrity gate against Git blob `4f1ef063fbe340c77177a1283dfc4d282305e238` found a real semantic difference in `GetMaxBallSpeed()`:

```diff
- if (rb == null)
+ if (rb == null || rb.isKinematic)
```

A separate removed comment and BOM/encoding differences were also found.

A pre-existing local backup was found:
- `C:\Temp\147VR_M5_RUNTIME\M5ShotLifecycle.baseline.cs`
- 3945 bytes
- same content/hash as the 2026-08-30 local backup lineage.

That backup already used `rb.isKinematic` inside `AreAllDynamicsSleeping()`, but did NOT yet use it in `GetMaxBallSpeed()`.

Therefore:
- The `GetMaxBallSpeed()` `isKinematic` addition is a later semantic change than that backup.
- Its exact actor/command/time has NOT been proven.
- PowerShell history and obvious patch-artifact searches did not identify the exact command/script responsible.
- REAL certification logs independently show kinematic-body velocity warnings in the calibration environment, supporting the runtime rationale for excluding kinematic bodies from speed-based settling.

## Decision
1. Do NOT remove `rb.isKinematic` from the certified local variant.
2. Do NOT restore `M5ShotLifecycle.cs` from PR #3/Git blob.
3. Treat the 3857-byte local variant as the certified runtime baseline for M5.1.
4. Keep production source unchanged while provenance remains unresolved.
5. Do not start M5.3 integration against `SnookerShotTracker`/other protected production files yet.

## Approved next path
### Gate A — Provenance
Read-only investigation only. If stronger provenance evidence appears, record it here. No source patch, checkout, restore, or commit.

### Phase B — M5.3 Pure Rules Core
After the preservation/provenance decision is formally closed, create new files only:
- `Assets/Scripts/Quest/Rules/M5RulesTypes.cs`
- `Assets/Scripts/Quest/Rules/M5SnookerRulesEngine.cs`
- `Assets/Editor/M5_3_RulesUnitTests.cs`

Rules Core must be deterministic and must not mutate GameObjects, score managers, turn managers, transforms, or physical respots.

### Required M5.3 coverage
- red on / red pot
- multiple reds in one shot
- red + colour atomic handling
- colour nomination
- wrong nominated colour
- multiple foul reasons with one penalty
- cue ball potted/off-table and D-in-hand
- miss all
- correct first contact with/without cushion
- colours in order
- final black / frame-end candidate
- pending-pot permutation invariance

### Integration order after Pure Rules
`M5.3 Rules → compile/unit gate → inspect diff → Phase B integration → REAL PhysicsSettled rules evidence → M5.3 certification → M5.4 Scoring → M5.5 Turn → M5.6 Integrated Transaction → regression/final certification`.

## Safety boundary
Protected until provenance/source authority is resolved:
- `M5ShotLifecycle.cs`
- `M5ShotEventContract.cs`
- `SnookerShotTracker.cs`
- `SnookerScoreManager.cs`
- `SnookerTurnManager.cs`
- `CueBallCollision.cs`
- `147VR_MainScene.unity`

No Unity run was performed during this documentation/provenance pass.

## Certified Baseline Adoption Gate — 2026-09-15
### Decision-record correction
The canonical shot-state transition is:
- Red on -> legal red pot -> score +1 per red potted -> Next Ball On = Colour -> require Nominated Colour.
- Colour on -> legal nominated colour pot -> score colour value -> respot colour -> if reds remain, Next Ball On = Red.
The prior shorthand "red pot -> still Red on" is incorrect and is not authoritative.

### Citation hygiene
A project-wide search found no `Symbolzzz/billiard-unity` citation under `Docs/`; no external repository is used as provenance for 147 VR rules or source.

### Operational provenance conclusion
- Source `M5ShotLifecycle.cs` SHA-256 `0e2b0f47444a453bef173ac98eac851f02fb1304059a4a6798a6cfeb59f24d38` modified `2026-09-15T09:34:19.136Z`.
- Certification log starts at `2026-09-15T12:05:46Z`; Unity compiled `Assembly-CSharp.dll`, modified `2026-09-15T12:06:03.469Z`.
- Runtime JSON baseline/regression were persisted at `2026-09-15T12:11:57.866Z`.
- Certification log ends after `[M5 CERTIFIED] REAL baseline + independent regression PASS.` and shutdown.
- Current source LastWriteTime is earlier than compilation and certification timestamps; this supports operational provenance for the certified run, but does not prove historical actor/command provenance.
- Decision: ADOPT the 3857-byte local lifecycle variant as the M5.1 Certified Baseline; retain `rb.isKinematic` exclusion.

### Durability / Git gate
- M5.2 Runtime behaviour: CERTIFIED.
- M5.2 Local source preserved: YES.
- M5.2 Git durability: NOT YET; no staging or commit performed.
- `.m5_straight_ready`: present; do not delete yet.
- Proposed production stage set only: `M5ShotLifecycle.cs`, `M5ShotEventContract.cs`, `SnookerShotTracker.cs`, `SnookerScoreManager.cs`, `SnookerTurnManager.cs`, `147VR_MainScene.unity`, this provenance record.
- Exclude from proposed production commit: `.m5_straight_ready`, certification logs, generated JSON, `M6GameplayIntegrationTests.cs`, modified `SnookerRulesTests.cs`, temporary scripts/bootstrap files.
- HARD STOP: no git add/commit/checkout/restore, source patch, or Unity launch in this gate.
