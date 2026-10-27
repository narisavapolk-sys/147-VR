# Real GOLDEN cases extracted from 147VR Unity project

Source: `Assets/AAA/PhysicsCalibration/*_Runtime_Case.asset` (ShotCalibrationCase SOs)
+ `Assets/AAA/PhysicsCalibration/Golden/147VR-PHY-*.asset` (PhysicsGoldenCase SOs).
Harness: `Assets/Scripts/AAA/Physics/Golden/` — pass = re-sim `distance` AND
`peakSpeed` within `regressionTolerancePercent` (0.5%).

## Single-ball calibration cases (power/side/vertical → targets)
| case | type | power | side | vertical | expectedDistance (m) | expectedPeakSpeed (m/s) |
|---|---|---|---|---|---|---|
| Straight     | 0 | 0.5 | 0     | 0     | 0.72825 | 4.05298 |
| Stun         | 1 | 1.0 | 0     | 0     | 0.02308 | 3.67447 |
| Follow       | 2 | 1.0 | 0     | +0.75 | 0.90066 | 3.67447 |
| Draw         | 3 | 1.0 | 0     | -0.75 | 0.49386 | 2.54708 |
| LeftEnglish  | 4 | 1.0 | -0.75 | 0     | 0.80090 | 3.67447 |
| RightEnglish | 5 | 1.0 | +0.75 | 0     | 0.80090 | 3.67447 |

## Current game cloth/cushion calibration (PhysX+correction, for reference)
`TableSurfaceProfile_RuntimeBaseline.asset` (measuredTruthCertified=1):
rollingFriction 0.452 · rollingDamping 0.21 · slidingFriction 0.6808 ·
spinFriction 0.2004 · settleSpeed 0.012 · settleSpin 0.035 · rollSlipTolerance 0.03
`M3_CushionProfile.asset` — cushion params (read separately).

## Open question before real fitting
`expectedDistance` is NOT the natural stop distance (0.728 m @ 4 m/s implies
μ≈1.1, unphysical). The harness `ShotMeasurementTracker`/`PhysicsGoldenEvaluator`
must define distance as (path length to first contact? | over a fixed time? |
displacement after an object-ball collision for stun/follow/draw?). MUST read
`ShotMeasurementTracker.cs` + `PhysicsGoldenEvaluator.cs` + `CueStrokeModel.cs`
(power→v0 curve) to reproduce the metric exactly.

## Integration seam (from discovery)
- Cue strike entry: `Assets/Scripts/AAA/Cue/CuePhysicsAdapter.Apply(CueShotData, english)`
  (+ `CueStrokeModel.cs`, `CueImpactProfile.cs`).
- Per-ball motion: `Assets/Scripts/AAA/Physics/BallMotionController.cs` (Rigidbody+PhysX).
- Golden plug: PhysicsGoldenShotExecutor → CuePhysicsAdapter.Apply →
  ShotMeasurementTracker → PhysicsGoldenRegressionRunner → PhysicsGoldenEvaluator.
- Tables/modes: snooker scene + `Quest/Rules/M5*`; pool 8/9-ball scenes + `Scripts/PoolTable/BallRack.cs`.
  No TableType enum; dims in prefab colliders + `SnookerPhysicsSetup` fields.
