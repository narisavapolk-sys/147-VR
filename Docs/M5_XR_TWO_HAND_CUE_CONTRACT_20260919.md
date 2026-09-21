# 147 VR — XR Two-Hand Cue Contract
Date: 2026-09-19 / continued 2026-09-20

## Existing state audited
- VR147CueHandSource was single-hand dominant-controller pose only.
- VR147DominantHand is the source of truth for left/right dominant hand.
- Main Scene contains XR hand/controller anchor objects.
- CuePhysicsAdapter remains the single gameplay physics authority.

## Implemented
Asset: Assets/Scripts/AAA/Input/VR147TwoHandCuePoseSource.cs
GUID: 0b73c0a7977dbee458faaa4becdd36c6
Current SHA256: 0BD7606F04193241F275DAD8D6D4D7760F17427DD8368F831E5003B760659C84

Semantics:
- Stroke hand = dominant hand.
- Bridge hand = non-dominant hand.
- Exposes positions, rotations, smoothed velocities and separation.
- CueAxis is BridgePosition - StrokePosition, normalized.
- Supports optional per-hand fallback anchors.
- Does not own scoring, lifecycle or physics.

## Main Scene integration
- SnookerCueController has explicit opt-in: useTwoHandCuePose.
- Main Scene persists `useTwoHandCuePose: 1`.
- Added `AAA Two-Hand Cue Rig` with VR147DominantHand + VR147TwoHandCuePoseSource.
- Rig default dominant hand is Right.
- SnookerCueController references the rig's dominant hand and two-hand source.
- Existing single-hand path remains available as fallback.

Main Scene SHA after integration:
82F053BEF663921C20EB7A0003AF1528348E2DB80A653DFCA831CD9FADF7B4B7
Pre-integration / backup SHA:
B1A3416B7B47314DBA34A9031A70792FBE12D771163275B3BA34285AA36D4197

## Verification
### Two-Hand Contract — PASS
Run date: 2026-09-20
Evidence log: Docs/UnityBatchLogs/UnityBatch_20260920_085910.log
Validated both deterministic dominant-hand modes using fallback anchors without modifying scene state:
- Right dominant -> Bridge=Left, Stroke=Right, separation=1.0m, CueAxis=(-1,0,0).
- Left dominant -> Bridge=Right, Stroke=Left, separation=1.0m, CueAxis=(1,0,0).

### Post-reset REAL10 — PASS
Run date: 2026-09-20
Evidence: Docs/M5_REAL_10SHOT_CERT_20260919.json
Result: fired=10, resolved=10, settled=10, errors=0.
Runtime path included actual pots/respots/turn changes and CuePhysicsAdapter authority.
The PC had a hard reset before this run; Unity Package Manager IPC was restored by the repaired launcher environment.

## Safety / limitations
- Quest 2/3 physical hardware validation is NOT claimed by this record.
- Batch warning `PlayerViewManager: No rig and no main camera found.` occurred during headless runtime certification; it did not prevent the M5 transaction result.
- XR shutdown emitted `StopSubsystems without an initialized manager`; it did not prevent the certification result.
- No destructive git reset/clean/stash operation was used.

## Next acceptance gate
1. Physical Quest 2/3 two-hand pose validation.
2. Verify cue visual alignment and physical bridge/stroke placement in headset.
3. Then calibrate table/cue feel using the existing certified calibration stack; do not duplicate Golden physics systems.
