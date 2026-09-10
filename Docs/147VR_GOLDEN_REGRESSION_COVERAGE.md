# 147 VR â€” Golden Regression Coverage

Status: IMPLEMENTED / NOT RUNTIME-VERIFIED

## Why
A regression runner is unsafe if it can report PASS for only the subset of cases that happened to be supplied.
The catalog is therefore the authority for expected coverage.

## Pattern
Golden Catalog = required case set.
Measurement list = observed samples.
Runner evaluates only catalog-owned cases.
Report exposes total, passed, failed, invalid, missing, pass rate, and coverage rate.

## Recording Rules
- Null cases are rejected.
- NaN and Infinity measurements are rejected.
- When a catalog is assigned, measurements from outside that catalog are rejected.
- Re-capturing the same case replaces its previous measurement.

## Coverage Meaning
coverageRate = evaluated catalog cases / catalog case count.
allCasesCovered is true only when every catalog case has an evaluated sample.

## Important Boundary
Coverage does not mean physics correctness.
A fully covered run can still fail cases. PASS/FAIL remains owned by the evaluator.

## Verification
Static source inspection completed after implementation. Brace balance verified for the modified Runner and Report.
Unity compile and runtime execution evidence was completed during M2.3 Draw certification; see the M2.3 section below.

## Next
Create a controlled shot execution adapter that produces real measurements for Straight, Cushion, and Pocket cases.

## M2.3 Draw — 2026-08-29

- **147VR-PHY-004 / Draw r1: PASS**
- Runtime evidence: `Assets/AAA/PhysicsCalibration/RuntimeMeasurements/draw_runtime_measurements.json`
- Input: `drawEnglish=-0.75`, shot speed `4.0 m/s`, `5` repetitions
- Draw-specific acceptance: post-contact cue speed positive magnitude, `drawDot=-1.0` on all 5 samples, reverse distance measured
- Golden provenance: generated from the runtime JSON; no reuse of PHY-001/002/003 samples
- Regression: **5/5 PASS**, tolerance `0.50%`
- Full evidence: `Docs/147VR_M2_3_DRAW_CERTIFICATION.md`

