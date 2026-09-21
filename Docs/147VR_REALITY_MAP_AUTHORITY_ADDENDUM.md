# 147VR REALITY MAP - VERIFIED AUTHORITY ADDENDUM

Generated 2026-08-27

## Verified runtime authority

- Cue execution: `Assets/Scripts/Quest/SnookerCueController.cs` now validates through `CueShotValidator` and delegates the actual cue-ball physics mutation to `Assets/Scripts/AAA/Cue/CuePhysicsAdapter.cs`.
- Runtime physics setup: `Assets/Scripts/Quest/SnookerPhysicsSetup.cs` creates the runtime surface, rails, pocket catchers and ball rigidbodies.
- `CuePhysicsAdapter` applies mass-correct impulse/torque and is the intended single cue-strike physics authority.

## M1 integration evidence

- `SnookerCueController` no longer writes cue-ball `linearVelocity`, `AddForce`, or `AddTorque` during shot execution.
- `CalibrationShotController` and `PhysicsGoldenShotExecutor` delegate shot execution to `CuePhysicsAdapter`.
- `147VR_PhysicsCalibration.unity` now contains the serialized M1 calibration chain.
- `ShotMeasurementTracker` supports runtime target configuration for the discovered cue ball.

## Architecture risk

The project still contains a legacy/runtime Quest path and a newer AAA Cue/Physics path. M1 establishes the cue-strike migration boundary without creating a third authority. Reset/initialization velocity writes in calibration/golden tooling are not shot authority; shot application must go through the adapter.

## Required runtime verification

1. Prove the serialized calibration scene runs after Unity completes compilation/import.
2. Execute real straight shots and persist telemetry.
3. Generate Golden Truth only from those measured samples.
4. Run Golden Regression using the same authority path.
5. Record PASS/FAIL evidence and measured values.

## Audit integrity

Static inventory is evidence, not deletion authority. Orphan candidates require scene/prefab/reflection/runtime verification. Never synthesize Golden measurements.