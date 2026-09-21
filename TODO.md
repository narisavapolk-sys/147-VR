# 147 VR — TODO / Roadmap

## GATE 1 — Foundation

- [x] Git baseline and clean working tree
- [x] Branch/remote strategy
- [x] Project structure audit
- [x] Package/dependency audit
- [x] Architecture/module boundary audit
- [x] Identify blockers and technical debt
- [x] Record Gate acceptance state

## GATE 2 — Core Systems

- [x] Gameplay/core flow audit
- [x] State and event architecture
- [x] Service/system boundaries
- [x] Reusable core vs game-specific separation
- [x] Dependency direction
- [x] Integration contracts
- [x] Record Gate acceptance state

## GATE 3 — Production Readiness

- [x] Production folder/content structure audit
- [x] Asset workflow
- [x] Build/configuration workflow
- [x] Validation and test workflow
- [x] Integration points
- [x] Handoff documentation
- [x] Record Gate acceptance state

## Post-Gate Runtime Verification

- [ ] Successful Unity batch validation with exit code 0
- [ ] Open 8-ball scene in Editor and Play
- [ ] Open 9-ball scene in Editor and Play
- [ ] Verify XR device/runtime path
- [ ] Verify cue → physics → shot → score → turn → UI loop

## Future — Blender / Asset Pipeline

- [ ] MCP session stability and reconnect workflow
- [ ] Blender automation toolkit
- [ ] 147 VR asset specification
- [ ] FBX/GLB export pipeline
- [ ] Unity auto-import pipeline
- [ ] Asset validation + auto-fix
- [ ] LOD generation
- [ ] Batch asset processing
- [ ] Rig/animation automation


## AAA Physics — Active Work

- [x] Golden Case data contract (identity/revision/category/intent)
- [x] Golden Case catalog validation + duplicate protection
- [x] Golden evaluator/result boundary
- [x] Golden regression aggregation boundary
- [ ] Bind Golden Regression to repeatable real-table shot execution
- [ ] Create controlled Golden cases from measured real-table behavior
- [ ] Validate Cushion response against Golden cases
- [ ] Validate Pocket capture/rejection against Golden cases
- [ ] Integrate validated physics into Gameplay shot lifecycle
- [ ] Advance VR Interaction after physics/gameplay contracts stabilize

## 147VR — Golden Measurement Bridge Checkpoint
Status: IMPLEMENTED / NOT RUNTIME-VERIFIED
Pattern: ShotMeasurementTracker → PhysicsGoldenMeasurementBridge → PhysicsGoldenRegressionRunner → Evaluator → Result → Report.
Done: Golden runner now accepts validated runtime measurements and replaces duplicate captures for the same case.
Done: Bridge transfers the existing measurement truth without duplicating physics calculations.
Not done: Automated shot execution from a controlled physics scene.
Not done: Unity batch/runtime verification of the new Golden bridge path.
Next: Controlled shot execution adapter, then real Straight/Cushion/Pocket Golden Cases.


### Golden Physics — Controlled Shot Execution
- [x] Controlled Golden Shot executor implemented.
- [x] Measurement-to-regression bridge integrated.
- [ ] Create concrete Straight Golden Case assets.
- [ ] Bind controlled executor in a validation scene.
- [ ] Unity compile validation.
- [ ] Unity runtime shot/settle validation.
- [ ] First real Golden regression report.


## 2026-08-27 — YOLO Continuation Checkpoint

- [x] Re-audit current 147VR state against actual project files.
- [x] Correct calibration execution path so a batch run launches a validated physical shot before measurement.
- [x] Update persistent current-state handoff: `Docs/147VR_CURRENT_STATE.md`.
- [x] Update `Docs/147VR_PROJECT_MEMORY.md` with the correction and verification boundary.
- [ ] Unity compile/runtime verification after the calibration correction.
- [ ] Persist first real measured Straight-shot Truth samples.
- [ ] Author Golden assets only from measured Truth.
- [ ] Run first complete Golden regression and record PASS/FAIL report.
- [ ] Validate Cushion/Pocket with real measurements.
- [ ] Integrate validated physics into gameplay shot lifecycle.
- [ ] Complete VR interaction after physics/gameplay contracts stabilize.

Rule: source/contract completeness is never reported as runtime/production completeness. Every meaningful work block must update persistent `.md` handoff state for the next AI.
