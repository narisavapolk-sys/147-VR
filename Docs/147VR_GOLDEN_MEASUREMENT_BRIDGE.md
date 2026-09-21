# 147 VR — Golden Measurement Bridge

Status: IMPLEMENTED / NOT YET RUNTIME-VERIFIED

## Purpose
Connect the existing physics measurement truth layer to the Golden Regression layer.

## Pattern
Physics scene produces a measured sample.
ShotMeasurementTracker owns measurement truth.
PhysicsGoldenMeasurementBridge captures that sample for one Golden Case.
PhysicsGoldenRegressionRunner stores and evaluates the sample.

## Contract
- No physics calculation is duplicated in the Golden layer.
- No expected value is inferred from the measured value.
- Invalid or missing measurements are rejected.
- One case can be recorded repeatedly; the latest capture replaces the previous sample.
- Regression remains an explicit evaluation step.

## Runtime Flow
Shot → MeasurementTracker → MeasurementBridge → GoldenRunner → Evaluator → Result → Report

## Current Limitation
The bridge does not fire or simulate a shot. It only transfers an already-valid measurement.
A controlled scene adapter is still required for automated physical shot execution.

## Verification
Code structure reviewed against the existing ShotCalibrationCase, ShotCalibrationEvaluator,
and ShotMeasurementTracker contracts. Unity compilation/runtime verification remains pending.

## Next
Build the controlled shot execution adapter without changing the measurement truth model.
