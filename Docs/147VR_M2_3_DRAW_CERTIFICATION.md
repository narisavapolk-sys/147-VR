# 147VR M2.3 Draw Certification

**Status: PASS — LOCK CANDIDATE**

Date: 2026-08-29  
Unity: `6000.4.4f1`  
Case: `147VR-PHY-004` / Draw r1  
Input: `drawEnglish = -0.75`  
Shot speed: `4.0 m/s`  
Repetitions: `5`

## Acceptance Evidence

| Metric | Measured result | Status |
|---|---:|---|
| Draw input | -0.75 | PASS |
| Cue speed at contact | 0.10685625 m/s mean | PASS |
| Post-contact cue speed | 0.20018928 m/s mean | PASS |
| Draw direction dot | -1.000000 mean | PASS |
| Cue reverse distance | 0.03168414 m mean | PASS |
| Object-ball peak speed | 2.54708133 m/s mean | PASS |
| First-flight distance* | 0.49386123 m mean | PASS |
| Repetitions | 5/5 | PASS |
| Regression tolerance | 0.50% | PASS |
| Golden regression | 5/5 PASS | PASS |

\* `firstFlightDistance` is measured as object-ball displacement after a deterministic 10-step post-contact observation window. This avoids reporting a zero displacement caused by discrete solver contact-frame timing.

## Runtime Samples

- Cue speed at contact range: `0.10685587 .. 0.10685682 m/s`
- Post-contact cue speed range: `0.20018889 .. 0.20018984 m/s`
- Draw dot: `-1.0` on all 5 samples
- Reverse distance range: `0.03168404 .. 0.03168425 m`
- Object peak range: `2.54708076 .. 2.54708171 m/s`
- First-flight distance range: `0.49386120 .. 0.49386126 m`

## Provenance

Runtime source:
`Assets/AAA/PhysicsCalibration/RuntimeMeasurements/draw_runtime_measurements.json`

Calibration scene:
`Assets/AAA/PhysicsCalibration/147VR_M23_DrawCalibration.unity`

Golden:
`Assets/AAA/PhysicsCalibration/Golden/147VR-PHY-004_Draw.asset`

Calibration case:
`Assets/AAA/PhysicsCalibration/Draw_Runtime_Case.asset`

Golden catalog:
`Assets/AAA/PhysicsCalibration/Golden/147VR_GoldenCatalog.asset`

The Golden was generated from the runtime JSON above. No measured sample was copied from PHY-001, PHY-002, or PHY-003.

## Regression Evidence

```text
[147VR M2.3 Golden Regression] rep=1/5 PASS | firstFlight=0.493861200 | peak=2.547081000 | postCue=0.200189800 | drawDot=-1.000000000
[147VR M2.3 Golden Regression] rep=2/5 PASS | firstFlight=0.493861300 | peak=2.547082000 | postCue=0.200188900 | drawDot=-1.000000000
[147VR M2.3 Golden Regression] rep=3/5 PASS | firstFlight=0.493861200 | peak=2.547081000 | postCue=0.200189800 | drawDot=-1.000000000
[147VR M2.3 Golden Regression] rep=4/5 PASS | firstFlight=0.493861300 | peak=2.547082000 | postCue=0.200188900 | drawDot=-1.000000000
[147VR M2.3 Golden Regression] rep=5/5 PASS | firstFlight=0.493861300 | peak=2.547082000 | postCue=0.200188900 | drawDot=-1.000000000
[147VR M2.3 Golden Regression] PASS | 5/5 | tolerance=0.50%
```

## Architecture Notes

- Draw uses the same `CuePhysicsAdapter` authority as Straight/Stun/Follow.
- English stimulus is inverted from Follow: `+0.75 -> -0.75`.
- No second physics authority was introduced.
- Existing PHY-001 / PHY-002 / PHY-003 Golden assets were not rewritten.
- A headless physics path was used for deterministic batch execution because the normal Play Mode batch callback path does not advance reliably under the current Unity batch invocation.
- The headless path still applies the real `CuePhysicsAdapter` impulse and advances Unity PhysX via `Physics.Simulate`; it does not synthesize Draw values.

## Decision

`M2.3 Draw = PASS`.

Next gate: **M2.4 English**.
