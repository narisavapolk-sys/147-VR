# 147 VR — Controlled Shot Execution

## Status
INTEGRATED — implementation boundary complete; runtime validation pending.

## Pattern
A Golden Case is executed through one controlled shot path, then measured by the existing truth tracker.
The executor does not calculate physics outcomes; Unity physics remains the authority.

## Flow
Golden Case → Begin measurement → Evaluate production shot speed → Apply linear velocity → Wait for settle → Capture measurement → Record regression sample.

## Guardrails
- Requires valid Golden Case.
- Requires Rigidbody, MeasurementTracker, RegressionRunner, and CueStrokeModel.
- Resets linear/angular velocity before each case.
- Uses the production `CueShotValidator` → `CueShotData` boundary; no duplicated speed curve exists in the executor.
- Production `SnookerCueController.Shoot()` uses the same validator/data boundary as Golden execution.
- NaN/Infinity rejection exists at both measurement capture and regression recording boundaries.
- Invalid Rigidbody speed/distance aborts measurement and cannot produce a valid capture.
- Regression runner exposes catalog validation state/error for diagnostics.
- Active execution is single-shot and non-reentrant.

## Boundary
This is a validation harness, not gameplay cue logic. Production VR cue input must later feed the same measurement contract without making this executor a gameplay dependency.

## Verification
Static source inspection completed. Unity compile/runtime validation is still pending.

## Certification Guardrails
- Empty Golden catalogs are invalid and cannot produce a certification-ready report.
