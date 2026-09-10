# 147 VR — M5 Authority Chain Status

## Target chain

CuePhysicsAdapter
→ PhysX / Rigidbody
→ M5 Shot Lifecycle
→ Physics Settled Boundary
→ Event Contract
→ Rules Authority
→ Scoring Authority
→ Turn Authority

## Current implementation

- M4.2 CuePhysicsAdapter remains the sole cue-strike impulse authority.
- M5ShotLifecycle added as the shot state boundary.
- M5ShotEventContract added as the lifecycle event boundary.
- SnookerCueController opens M5 before applying the physics impulse.
- SnookerShotTracker now defers shot resolution to M5 PhysicsSettled when M5 exists.
- SnookerTurnManager disables its legacy timer when M5 exists, preventing an independent turn clock.
- Existing M3/M4.1/M4.2 code was not intentionally replaced.

## Important boundary

Rules, scoring and turn decisions must not execute from raw Rigidbody polling.
The intended downstream trigger is M5 PhysicsSettled.

## Verification

Unity 6000.4.4f1 batchmode project initialization/compile completed with exit code 0.
No `error CS` or `Compilation failed` lines were found in the compile log.
PhysX backend initialized successfully.
UPM connected successfully during this run.

## M5.2 Verification — PASS

M5.2 Event Contract is verified by a real Unity 6000.4.4f1 batch run through
`Docs/Tools/Unity_Batch_Safe.ps1`.

Evidence: `Docs/UnityBatchLogs/UnityBatch_20260908_074615.log`
- UPM IPC connected successfully.
- M5 lifecycle reached `Settled`, sequence `1`.
- `M5ShotEventContract.PhysicsSettled` delivered exactly once for sequence `1`.
- `SnookerShotTracker` consumed the settled boundary and left shot-in-progress false.
- No Physics Authority, Bed_Collider, Golden data, or V007 assets were touched by the verification.

A binding hardening was applied: `M5ShotEventContract.InitializeBindings()` is now
idempotent and `SnookerShotTracker` explicitly initializes the contract before subscribing.

## Not yet certified

M5.1, M5.3–M5.6 are not declared CERTIFIED by this file.
The remaining work is to make Rules → Scoring → Turn consume the settled event as
one deterministic authority chain, then run real vertical-slice and regression cases.

## Backup

Pre-refactor backups were created under:
`Docs/M5_PRE_REFACTOR_BACKUP_20260830_101648/`
