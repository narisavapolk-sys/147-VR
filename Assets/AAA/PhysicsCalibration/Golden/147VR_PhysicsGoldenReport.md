# 147 VR Physics Golden Report

Generated from the validated Golden catalog state.

## Infrastructure

- Catalog validation: PASS
- Golden cases: 1
- Provenance fields: required
- Headless entry point: `VR147.AAA.Editor.PhysicsGoldenInfrastructureAutomation.RunHeadlessRegression`

## Golden Cases

| Case | Revision | Type | Reps | Distance Mean | Distance SD | Peak Speed Mean | Peak Speed SD | Tolerance |
|---|---:|---|---:|---:|---:|---:|---:|---:|
| 147VR-PHY-001 | 2 | Straight | 5 | 0.728250027 m | 0.000000000 m | 4.052930403 m/s | 0.000127411 m/s | 0.500% |

## Provenance

### 147VR-PHY-001 r2

- Source JSON: `Assets/AAA/PhysicsCalibration/RuntimeMeasurements/straight_runtime_measurements.json`
- Source scene: `Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity`
- Unity version: `6000.4.4f1`
- Shot type: `Straight`
- Source repetitions: `5`

## Independent Headless Regression

- Input: `147VR_GoldenCI_Straight_Input.json`
- Repetitions: 5
- Passed: 5
- Failed: 0
- Result: **PASS**
- Tolerance: 0.500%

## Gate

M1.5 Golden infrastructure is ready for M2 feature-specific Golden cases.
