# 147 VR — YOLO Handoff / Continuation Record
Date: 2026-09-19 (Europe/Rome)
Active project: C:\Users\mongo\UnityProjects\147 VR

## Completed this run
- Fixed REAL 10-shot runner race: completion now waits for fired=10, resolved=10, settled=10.
- REAL 10-shot certification: PASS (Main Scene, runtime chain, no runner errors).
- Evidence: Docs\M5_REAL_10SHOT_CERT_20260919.json
- Evidence log: Docs\UnityBatchLogs\M5_REAL10_FIXED_20260919.log
- Fixed SnookerCueController compile issue: added System.Collections.Generic.
- Fixed Desktop test key types: UnityEngine.InputSystem.Key instead of UnityEngine.KeyCode for Input System indexer.
- Recompiled through the REAL runner; no CS compile errors blocked the run.
- Baseline manifest created before these fixes: Docs\M5_YOLO_BASELINE_MANIFEST_20260919.txt
- Main gameplay remains protected from destructive git reset/clean/stash operations.

## Current verified state
- 22 active runtime balls detected; inactive legacy duplicate set ignored.
- Table physics frame and CuePhysicsAdapter authority are runtime-active.
- M5 lifecycle + Rules/Scoring/Turn transaction chain executes in Main Scene.
- REAL 10/10: FIRE, RESOLVED, SETTLED all 10/10; errors 0.
- Formal result is REAL 10-shot PASS, not a claim that every production XR/game-mode feature is complete.

## Next work order
1. Freeze/record current dirty baseline without destructive cleanup.
2. Local Snooker Vertical Slice: Desktop mouse aim + LMB charge/release, reset R, undo U; acceptance in PlayMode still needs manual verification.
3. Harden local Undo/Reset semantics against every M5 transaction state.
4. Then XR two-hand cue/calibration/haptics.
5. Then unified game-mode/rules edge cases, presentation, multiplayer last.

## Safety
- Do not use git clean/reset/stash wholesale.
- Do not modify Golden/V007 or M5 lifecycle contract without direct evidence.
- Avoid parallel Unity instances; this PC has prior freeze history.

## Additional verified result
- Local Desktop vertical slice acceptance: PASS — FIRE 1, RESOLVED 1, SETTLED 1, Undo verified restored score/turn/ball state.
- Evidence: Docs\M5_DESKTOP_SLICE_CERT_20260919.json
- Runtime log: Docs\UnityBatchLogs\M5_DESKTOP_SLICE_20260919_R2.log
- Temporary acceptance runner archived to C:\Temp\M5DesktopVerticalSliceAcceptance_TMP.cs.done_20260919 (and .meta).

## XR groundwork completed
- Audited current XR input/hand stack; existing cue pose was single dominant hand.
- Added read-only two-hand contract: Assets\Scripts\AAA\Input\VR147TwoHandCuePoseSource.cs
- Bridge=non-dominant, Stroke=dominant; exposes pose/velocity/separation/cue axis and optional fallback anchors.
- Unity import/compile run completed with no CS compiler errors after adding it.
- Deliberately NOT wired into Main Scene yet; integration is gated to preserve the verified single-hand and Desktop paths.
- Design record: Docs\M5_XR_TWO_HAND_CUE_CONTRACT_20260919.md

## Reset-frame harness result
- ResetFrame runtime harness was intentionally aborted after a real shot resolved because Unity process memory rose above ~3 GB and the completion callback stalled.
- Unity was force-terminated to protect PC stability.
- Evidence is marked INCONCLUSIVE_ABORTED_SAFE in Docs\M5_RESET_FRAME_CERT_20260919.json. Do NOT treat this as PASS.
- Temporary runner archived to C:\Temp\M5ResetFrameAcceptance_TMP.cs.inconclusive_20260919.

## XR two-hand integration update
- Two-hand cue source is now persisted in Main Scene behind explicit opt-in.
- Main Scene SHA after integration: 82F053BEF663921C20EB7A0003AF1528348E2DB80A653DFCA831CD9FADF7B4B7.
- Pre-integration SHA: B1A3416B7B47314DBA34A9031A70792FBE12D771163275B3BA34285AA36D4197.
- Added `AAA Two-Hand Cue Rig` containing `VR147DominantHand` (Right default) and `VR147TwoHandCuePoseSource`.
- SnookerCueController references both and sets `useTwoHandCuePose=1`; existing single-hand path remains available as fallback.
- Unity generated and serialized the scene successfully; the final Windows temp-file move failed, so the Unity-generated temp scene was promoted after marker/hash verification. Backup is `Assets\Scenes\147VR_MainScene.unity.precert_20260919_twohand_retry3.bak`.
- Temporary integration runner archived to `C:\Temp\M5PersistTwoHandCue_TMP.cs.done_20260919` (and .meta).
- This is integration/persistence evidence, NOT Quest 2/3 hardware certification.
- Next gate: physical Quest 2/3 two-hand pose validation, then post-XR REAL 10-shot regression if required.

## 2026-09-20 post-reset continuation
- PC hard reset occurred before runtime continuation; persisted Main Scene integration survived intact.
- Repaired Unity launcher environment restored Package Manager IPC.
- Two-Hand Contract: PASS. Evidence log: `Docs\UnityBatchLogs\UnityBatch_20260920_085910.log`.
- Post-XR REAL10 regression: PASS — fired=10, resolved=10, settled=10, errors=0. Existing cert file remains valid: `Docs\M5_REAL_10SHOT_CERT_20260919.json`.
- New consolidated evidence: `Docs\M5_XR_POST_RESET_CERT_20260920.json`.
- Unity processes = 0 after the run.
- Quest 2/3 hardware validation is still NOT RUN.

## Next gate: Calibration / Feel
The repository already contains an M5-certified calibration stack (`Assets\AAA\PhysicsCalibration\.m5_certified`, Golden assets and RuntimeMeasurements). Main Scene runtime currently hardcodes ball/table PhysicsMaterial values in `SnookerPhysicsSetup`; `TableSurfaceProfile` / `TableSurfaceController` are not serialized into Main Scene. This is the next architecture gate: unify runtime table feel with the existing calibration profile without introducing a second conflicting physics writer or touching Golden/V007.

## 2026-09-20 Calibration / Feel gate result
- Baseline policy explicitly says measured gameplay data must drive tuning; guessed values must not be promoted to production.
- `TableSurfaceProfile` exists as a candidate-data class, but there is currently no profile asset/reference in Main Scene. `SnookerPhysicsSetup` still owns hard-coded runtime PhysicsMaterial values (`bounciness=0.8`, friction=0.05).
- A controlled 5-repetition Straight calibration attempt was started against the existing `147VR_PhysicsCalibration.unity` infrastructure using a distinct output file. Runtime armed, but no measurement file was produced and no progress callback arrived after the project entered Play Mode.
- The calibration scene also reported `Assets/InputSystem_Actions.inputactions` failed to load and missing MonoBehaviour references on `147VR_RuntimeCalibration`; Unity emitted automatic repair messages. No scene was saved.
- Unity reached ~3.2 GB without recovery progress, so the calibration process was terminated to protect PC stability.
- Evidence: `Docs\M5_CALIBRATION_GATE_20260920.json` status `INCONCLUSIVE_ABORTED_SAFE`.
- Temporary runner archived to `C:\Temp\M5RunStraightCalibration_TMP.cs.inconclusive_20260920` (and .meta).
- Do not modify Golden/V007 or promote TableSurfaceProfile candidates until calibration infrastructure/inputactions blocker is repaired and a measured run completes.
