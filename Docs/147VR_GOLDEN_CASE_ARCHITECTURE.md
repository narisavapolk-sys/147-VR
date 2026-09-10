# 147 VR — Golden Case Architecture

## Purpose
Golden Cases are persistent physics contracts. They define what a controlled
physics scenario is, what outcome is expected, and how that outcome is evaluated.

## Pattern
`GoldenCase → CalibrationCase → Evaluator → GoldenResult → Regression`

The Golden layer owns identity, revision, category and intent. Existing
ShotCalibrationCase remains the measurement contract and evaluator authority.

## Components
- `PhysicsGoldenCase` — ScriptableObject contract and stable case identity.
- `PhysicsGoldenCatalog` — ordered case set with duplicate/validity checks.
- `PhysicsGoldenEvaluator` — evaluates measured values through the existing evaluator.
- `PhysicsGoldenResult` — immutable result snapshot containing identity, revision,
  pass state and measurement errors.

## Rules
1. Golden Case IDs must be unique inside a catalog.
2. A case must reference a valid `ShotCalibrationCase`.
3. Case revision changes whenever its expected contract changes materially.
4. Golden evaluation must not mutate physics state.
5. Physics remains the source of measured facts; Golden Cases define acceptance.
6. A failed or invalid case must be visible to regression reporting, never silently skipped.

## Current Scope
Initial architecture covers distance and optional peak-speed validation through
the existing calibration evaluator. Cushion, pocket and multi-event contracts
will extend the same pattern after controlled scene binding.

## Completion Pattern
Status: IN PROGRESS → architecture implemented; runtime suite still pending.

Done:
- Persistent case identity/revision/category/intent.
- Catalog validation and duplicate detection.
- Immutable golden result.
- Reuse of existing calibration evaluator without duplicating physics math.

Next:
- Bind catalog to repeatable scene execution.
- Add regression aggregation/reporting.
- Add dedicated cushion/pocket event assertions.

## Regression Layer
The first regression layer is now implemented as a deterministic evaluator/report boundary.
`PhysicsGoldenRegressionRunner` accepts measured samples and produces a
`PhysicsGoldenRegressionReport` with total/pass/fail/invalid counts and pass rate.

Important boundary: this runner does not pretend to execute physical shots yet.
Scene execution remains a separate adapter step so measurement acquisition cannot
be confused with evaluation truth.

## Implementation Pattern
`Scene/Measurement Adapter → PhysicsGoldenMeasurement → RegressionRunner → Report`

This keeps the evaluator deterministic and makes it possible to feed recorded
measurements, live controlled-scene measurements, or future automated runs without
changing acceptance logic.
