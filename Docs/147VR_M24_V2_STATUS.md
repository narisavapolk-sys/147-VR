# 147VR M2.4 V2 Status

## Verified 2026-08-29
- M24EnglishAutomationV2 source exists and is compiling into the editor assembly.
- Assembly-CSharp.dll timestamp: 2026-08-29 17:39:03.
- M24EnglishAutomationV2.Load/Reg was successfully invoked by the editor regression hook.
- No new CS compile error was observed in the relevant log evidence.
- M2.4 Existing Editor Regression is currently BLOCKED only because the runtime measurement JSON is missing.
- english_left_runtime_measurements.json is not present in the project.

## Decision
- Compile Gate: PASS for V2 execution evidence.
- Do NOT rebuild/delete Library based on the timestamp check.
- Do NOT create fake measurement data.
- Proceed to REAL Runtime Shot phase.

## REAL Shot Phase
1. Fire REAL Left x5 in the V2 English calibration scene.
2. Persist runtime measurements to english_left_runtime_measurements.json.
3. Fire REAL Right x5 and persist english_right_runtime_measurements.json.
4. Use only measured runtime data for PHY-005/PHY-006 Golden Truth.
5. Run M2.4 regression after both datasets exist.

## M2.4 YOLO Run — PASS
- REAL LEFT x5 completed with all five runtime acceptance samples PASS.
- REAL RIGHT x5 completed with all five runtime acceptance samples PASS.
- Runtime JSON persisted from the physics simulation; no fake measurements were introduced.
- Measured first-flight distance is captured from the post-contact runtime object position.
- Headless/manual physics execution required a contact-probe fallback using actual Rigidbody distance/contact state.
- PHY-005 and PHY-006 Golden assets were built from the measured JSON datasets.
- Golden calibrationCase serialization was corrected to persist the ScriptableObject reference.
- Final Golden Regression: LEFT 5/5 PASS, RIGHT 5/5 PASS, tolerance=0.50%.
- Temporary YOLO orchestration was removed after completion; normal M2.4 entry points restored.
