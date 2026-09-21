# 147VR AAA BUILD LOG

## Purpose
Single source of truth for the AAA work being built directly in the 147VR project.
Never modify source projects just to support this log.

## Current status
Phase 1 - Cue foundation: COMPLETE
Phase 2 - Ball motion foundation: COMPLETE
Phase 3 - Collision/cushion foundation: COMPLETE
Phase 4 - Pocket/table integration: IN PROGRESS
Phase 5 - Calibration/QA: NOT STARTED
Phase 6 - AAA presentation/assets: NOT STARTED

## Implemented - Cue
- CueAimProfile
- CueStrokeModel
- CueShotData
- CueShotValidator
- CueImpactProfile
- CueImpactSolver
- CueStrikeResult
- CueStrikeSolver
- CueStrikeDiagnostics
- CueStrikeTestCase
- CueStrikeValidation
- CueStrikePresetFactory
- CuePhysicsAdapter

## Implemented - Ball Physics
- BallMotionProfile
- BallRollingState
- BallMotionState
- BallMotionEvaluator
- BallMotionController
- BallCollisionProfile
- BallCollisionResponse
- BallCollisionResponder
- CushionProfile
- CushionResponse
- CushionResponder
- PocketProfile
- PocketCaptureResult

## Important architecture rule
Keep Profile -> Solver/Evaluator -> Runtime Adapter/Responder separation.
Do not replace existing gameplay physics wholesale until calibration proves the new path.
Use the AAA layer as an isolated migration path with rollback safety.

## Next execution order
1. Verify existing Pocket/Table assets and scene references.
2. Build PocketSolver and PocketResponder.
3. Build TableSurface controller and cloth calibration.
4. Integrate cue strike into the real cue-ball path.
5. Build controlled shot matrix: straight, stun, follow, draw, side English.
6. Calibrate ball-ball and cushion response against measured targets.
7. Only then enable AAA physics by default.
8. Build AAA table/cue visual asset pass.
9. VR interaction, haptics, audio and presentation pass.
10. Performance/QA regression pass.

## Safety
If MCP times out, verify before rewriting. Never claim a file exists unless directory/read verification confirms it.

## Physics Calibration Harness
- ShotCalibrationCase.cs: CREATED
- ShotCalibrationResult.cs: CREATED
- ShotCalibrationEvaluator.cs: CREATED
- ShotCalibrationRunner.cs: CREATED
- Calibration types: Straight, Stun, Follow, Draw, LeftEnglish, RightEnglish, Cushion, Pocket.
- Next: connect runner to controlled shot scene/test balls, measure actual travel, then tune profiles from measurements.

## 2026-08-26 Calibration Harness pass 2
- ShotCalibrationRunner.cs: upgraded measurement origin + planar distance calculation.
- ShotCalibrationHarness.cs: CREATED for repeatable case execution and PASS/FAIL logging.
- Next: create controlled test scene only after verifying existing 147VR scenes/assets, then run baseline shots.
