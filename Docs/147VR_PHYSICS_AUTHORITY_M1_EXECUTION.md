# 147VR PHYSICS AUTHORITY INTEGRATION — M1

Execution date: 2026-08-27
Scope: 147VR only

## Current state

- Phase A — authority integration: IMPLEMENTED in source. `SnookerCueController` now validates a shot then delegates the physics mutation to `CuePhysicsAdapter`.
- Phase A — dual impulse check: STATIC PASS for the cue path. `SnookerCueController` no longer writes cue-ball `linearVelocity`, `AddForce`, or `AddTorque`.
- Calibration/G​olden executors: migrated shot application to `CuePhysicsAdapter`; direct velocity writes remain only for reset/initialization, not shot execution.
- Calibration scene: serialized runtime rig added: `SnookerCueController` + `CuePhysicsAdapter` + `SnookerBallTracker` + `ShotMeasurementTracker` + `CalibrationShotController` + batch runner/bootstrap.
- Runtime measurement persistence: existing batch JSON persistence is wired to `Assets/AAA/PhysicsCalibration/RuntimeMeasurements/straight_runtime_measurements.json`.

## Hard truth gate

- Real Straight Shot: NOT YET VERIFIED.
- Real telemetry file: NOT YET GENERATED in this execution.
- Golden Truth: NOT CREATED from invented values. A pre-runtime calibration case exists only as a test harness target; it is NOT a Golden Truth.
- Golden Regression PASS: NOT CLAIMED.
- Unity runtime compile/import gate is currently blocked because the project has no `Library/ScriptAssemblies/Assembly-CSharp.dll`, and Unity 6000.4.4f1 exits during batch `-executeMethod` startup with return code 1 before the requested editor method runs. A normal Unity launch also became non-responsive during the import/compile phase and was terminated.

## Authority contract

1. Input/aim/stroke lives in `SnookerCueController`.
2. `CueShotValidator` produces immutable `CueShotData`.
3. `CuePhysicsAdapter` is the sole cue-strike physics writer. It converts target speed to mass-correct impulse and applies optional spin torque.
4. Measurement observes the resulting Rigidbody motion.
5. Golden values may only be generated after real runtime measurements are persisted.

## Next executable gate

1. Restore/complete Unity script compilation/import without deleting authored 147VR work.
2. Open `Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity`.
3. Run the five-shot straight calibration batch.
4. Verify persisted JSON contains real measurements.
5. Derive Golden Truth only from those measured samples.
6. Run Golden Regression against the same authority path and record PASS/FAIL.
7. Update this document and the Reality Map with measured evidence.

## Evidence rule

No numeric Golden baseline is considered authoritative until it comes from a completed Unity physics run.