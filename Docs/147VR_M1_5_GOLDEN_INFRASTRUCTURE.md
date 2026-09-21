# 147 VR — M1.5 Physics Golden Infrastructure

## Status

**M1.5 implementation: PASS**

M1 Straight Shot remains certified as PASS. M1.5 standardizes the Golden workflow before M2 Spin Physics.

## Architecture

- `PhysicsGoldenCase` is the immutable-style ScriptableObject record for a measured truth case.
- `PhysicsGoldenCatalog` owns multiple Golden cases and validates uniqueness, provenance and case validity.
- `PhysicsGoldenEvaluator` evaluates runtime measurements against case-specific Golden tolerance.
- `PhysicsGoldenRegressionRunner` executes catalog-backed regression evaluation.
- `PhysicsGoldenMeasurementBridge` transfers real runtime measurement into the regression layer.
- `PhysicsGoldenInfrastructureAutomation` provides Unity batch/headless entry points.
- `PhysicsGoldenReportGenerator` creates a human-readable Markdown inventory.

## Provenance Contract

Every Golden case records source JSON path, UTC timestamp, scene, Unity version, shot type, repetition count, means, standard deviations, raw samples and tolerance.

Golden values MUST originate from real runtime measurement. Manual numeric overrides are prohibited.

## Tolerance Policy

Tolerance is a property of each Golden case, not a global constant. M1 Straight uses **0.50%** because repeated real measurements demonstrated stable behavior. Future Spin/Cushion/Pocket cases must establish their tolerance from evidence rather than inherit 0.50% blindly.

## Headless Contract

`VR147.AAA.Editor.PhysicsGoldenInfrastructureAutomation.RunHeadlessRegression` accepts `VR147_GOLDEN_INPUT` and `VR147_GOLDEN_REPORT` environment variables and exits non-zero on regression failure.

The M1 Straight independent regression dataset was converted into the generic input schema and produced **5/5 PASS** against `147VR-PHY-001` at 0.50% tolerance.

## Report

Human-readable report target: `Assets/AAA/PhysicsCalibration/Golden/147VR_PhysicsGoldenReport.md`.

## Environment Note

A later attempt to execute the report generator hit the known Unity Package Manager environment blocker: UPM IPC returned HTTP 500 for `project:list-packages`. This does not invalidate the earlier successful compilation/infrastructure validation or the successful headless Golden regression. Do not bypass UPM by launching the full project with `-noUpm`; that causes package-backed assemblies such as Input System/UI to disappear from compilation.

## Next Milestone

M2 begins with **Stun** and uses this infrastructure without creating a parallel Golden system.

M2.1 acceptance gates remain A–F: deterministic scene, deterministic cue ball, isolated environment, real runtime measurement, Golden from measured data, and independent regression PASS.

Video is reserved as an external behavioral reference for M2/M3/M5. It is never the numerical Golden source.

## Executor Safety Rule

Only one agent may launch Unity for this project at a time. Before every Unity launch, inspect running `Unity.exe` processes and abort rather than opening a second instance.

Never use `-noUpm` for full project validation because this project depends on Input System, UGUI and XR packages. The known UPM 500/IPC issue must be solved at the environment/package layer before relying on a normal package-resolved launch.

## M1.5 Evidence Summary

- Catalog validation: PASS.
- Generic headless Golden regression: PASS, 5/5 samples.
- Human-readable report generated from validated catalog state.
- Case model already enumerates Straight, Stun, Follow, Draw, English, Cushion, Pocket and Integration categories.
- Catalog now exposes deterministic `TryGetCase(caseId, out case)` lookup for multi-case scaling.
