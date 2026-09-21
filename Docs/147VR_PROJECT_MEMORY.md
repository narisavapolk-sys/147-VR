# 147 VR — Project Memory / Recovery Checkpoint

> Purpose: persistent handoff memory so future sessions can continue 147 VR without relying on chat memory.
> Verified against project files: 2026-08-26.

## Source of Truth
- Project root: `C:\Users\mongo\UnityProjects\147 VR`
- Active assignment: 147 VR only.
- Other Unity projects are read-only reference/component sources.
- Before any cross-project reuse: inspect source, map dependencies, adapt only what is needed, keep source untouched, verify 147 VR.

## Verified Documentation Set
- `147VR_WORKING_DIRECTIVE.md` — permanent working rules and recovery discipline.
- `ARCHITECTURE.md` — runtime spine and dependency contract.
- `PRODUCTION_READINESS.md` — Gate 3 production contract.
- `GATE_STATUS.md` — Gate 1–3 acceptance state.
- `TODO.md` — post-Gate runtime verification and future roadmap.
- `Docs/147VR_BONE_AND_BALL_PORT_STRATEGY.md` — persistent port/reference decision.

## Architecture Contract
`Cue Controller → Physics → Ball Tracker → Shot Tracker → Score Manager → Turn Manager → UI/View`

Target upgrade:
`CueInputAdapter → CueInteractionState → SnookerPhysics → BallTracker → ShotLifecycle → SnookerRules → ScoreManager → TurnManager → Presentation`

Rules: physics reports facts; rules interpret facts; UI presents state; XR/presentation adapters stay replaceable.

## Current Physics Work State
The current progress snapshot supplied by the active development session is:
- Measurement Lifecycle — 100%
- Measurement Truth Model — 100%
- Calibration Case — 100%
- Evaluator — 100%
- Result — 100%
- Statistics — 100%
- Scorecard — 100%
- Runner — 100%
- Golden Case Architecture — 80%
- Golden Regression Runner — 50%
- Cushion / Pocket — 60%
- Gameplay — 30%
- VR Interaction — 20%

## Next Work Order
1. Complete Golden Case Architecture: 80% → 100%.
2. Complete Golden Regression Runner: 50% → 100%.
3. Push Cushion / Pocket validation: 60% → 100%.
4. Integrate/validate Gameplay against the physics truth boundary.
5. Advance VR Interaction only after the physics/gameplay contracts are stable.

## Physics Design Guardrails
- Do not allow multiple authoritative ball-velocity/physics controllers to fight each other.
- Preferred ball boundary: `Rigidbody + Collider + BallIdentity + SnookerPhysics + BallVisual`.
- Prediction/trajectory assistance must never become authoritative physics.
- Keep physics, rules, turn management, and presentation separated.

## Port Strategy
- BONE & BALL: component/reference laboratory; strongest candidate is SnookerPhysics and related billiards gameplay modules.
- CueStrike: reference for deeper VR interaction, calibration, trajectory and AI patterns.
- 147 VR owns final architecture, UX, scenes and game identity.
- Never copy source-project scene/GUID structure or modify source projects.

## Reference Video
- The newly discussed video is a REF/benchmark only at this stage.
- It must not drive immediate settings or architecture changes.
- Analyze first; request additional REF material only when evidence is insufficient.

## Recovery Protocol
At the start of a new session:
1. Read this file.
2. Read `147VR_WORKING_DIRECTIVE.md`.
3. Read `GATE_STATUS.md`, `TODO.md`, and `ARCHITECTURE.md`.
4. Read `Docs/147VR_BONE_AND_BALL_PORT_STRATEGY.md` before porting components.
5. Inspect actual project state before changing code/settings.
6. Continue from the current checkpoint; do not recreate completed systems blindly.

## Important Verification Boundary
- The progress percentages above are the current working snapshot provided by the active session; this checkpoint records them as status, not as independent test evidence.
- Existing Gate 1–3 documentation is present in the project and must remain the historical baseline.
- `GATE_STATUS.md` explicitly notes that a successful Unity batch validation exit code still needs to be rerun before claiming runtime/build validation passed.

## Work Pattern Rule
Every completed large work block must leave a concise persistent Pattern in project `.md` documentation: what was done, why, result, verification state, and next step. Chat memory is not the source of truth.

## 2026-08-26 Golden Case Work
Status: IN PROGRESS — architecture implementation completed; runtime regression binding remains.

Done:
- Added `Assets/Scripts/AAA/Physics/Golden/PhysicsGoldenCase.cs` for persistent case identity, revision, category, intent and calibration contract.
- Added `PhysicsGoldenCatalog.cs` for validation and duplicate ID protection.
- Added `PhysicsGoldenEvaluator.cs` and immutable `PhysicsGoldenResult.cs`.
- Reused `ShotCalibrationEvaluator` instead of duplicating distance/peak-speed math.
- Added `Docs/147VR_GOLDEN_CASE_ARCHITECTURE.md` as the detailed persistent pattern.

Pattern:
`GoldenCase → CalibrationCase → Evaluator → GoldenResult → Regression`

Verification:
- Files created successfully in the real 147 VR project.
- Unity batch validation was attempted, but the current Commander invocation did not produce a usable validation result/log; do not mark runtime validation passed from that attempt.

Next:
- Bind Golden Catalog to repeatable real-table execution.
- Build regression aggregation/reporting.
- Then validate Cushion/Pocket against controlled Golden Cases.

## 2026-08-26 Golden Regression Layer
Status: IMPLEMENTED — deterministic regression boundary is now present; live scene execution is intentionally separate.

## 2026-08-26 Production Shot Execution Alignment
Status: IMPLEMENTED — Golden executor now follows the production CueStrokeModel speed contract.

Additional hardening:
- Golden execution now creates a `CueShotData` through `CueShotValidator.TryCreate(...)` before applying velocity.
- This reuses the production validation/data boundary instead of constructing a parallel test-only shot representation.
- Golden direction/speed are taken from the validated `CueShotData`.
- If validation rejects the shot, execution aborts cleanly without recording a physics sample.

Pattern:
`GoldenCase → CueShotValidator → CueShotData → Rigidbody.linearVelocity → MeasurementTracker → RegressionRunner`

Done:
- Golden executor depends on `CueStrokeModel` rather than duplicating shot-speed math.
- Golden shot resets linear/angular velocity before execution for deterministic setup.
- Golden shot maps normalized CalibrationCase power through `CueStrokeModel.EvaluateSpeed(power)` and applies the resulting speed via Rigidbody linear velocity.
- Removed the previous impulse-based execution path and duplicated speed evaluator.

Pattern:
`GoldenCase.power → CueStrokeModel.EvaluateSpeed → Rigidbody.linearVelocity → MeasurementTracker → RegressionRunner`

Why:
The test harness must execute the same shot-speed contract as production; otherwise regression can validate a different physics input than gameplay.

Verification:
- Source re-read after edit confirms production `CueStrokeModel.EvaluateSpeed(power)` is the execution path.
- Unity compile/runtime validation remains pending.
- Production `SnookerCueController.Shoot()` now also routes shot creation through `CueShotValidator` and consumes validated `CueShotData`; the previous duplicated speed/fallback execution path is removed.

Pattern:
`VR/Desktop input → SnookerCueController → CueShotValidator → CueShotData → Rigidbody.linearVelocity`

Next:
- Bind deterministic executor in a controlled validation scene.
- Create Straight Golden Cases and execute repeated runtime samples.
- Investigate `CueImpactProfile` separately before adding spin to `CueShotData`; do not invent or duplicate an impact contract prematurely.

## 2026-08-26 Cue Impact / Spin Audit
Status: AUDITED — spin infrastructure exists, but committed-shot data currently carries direction/contact/power/speed only.

Observed:
- `CueImpactProfile` already exposes local impact impulse tuning for side/vertical english.
- `BallCollisionResponder` handles collision response separately.
- `BallMotionController` evaluates angular velocity and rolling/sliding state.
- `CushionResponder` currently resolves linear velocity through `CushionResponse`.

Decision:
Do not merge `CueImpactProfile` into `CueShotData` yet. The current codebase already contains a separate `CueStrikeSolver` → `CueStrikeResult` → `CuePhysicsAdapter` impact path that applies linear and angular impulse. Treat this as an existing physics subsystem requiring production-consumer verification, not as permission to invent a new spin contract.

Important distinction:
- `SnookerCueController.Shoot()` currently commits a validated `CueShotData` and applies linear velocity.
- `CuePhysicsAdapter` separately applies `CueStrikeResult.linearImpulse` and `angularImpulse`.
- These are potentially two execution authorities. Before claiming Physics Certification, trace whether `CuePhysicsAdapter` is actually instantiated/used by live prefabs/scenes. If unused, keep it isolated until intentionally integrated; if used, reconcile the authority rather than silently running both.

Next:
- Verified no textual `CuePhysicsAdapter` references in `*.prefab`, `*.unity`, or `*.asset` files under `Assets`.
- Verified the only C# occurrence of `CuePhysicsAdapter` is its own class file; no other C# consumer was found in the AAA scope search.
- Treat `CuePhysicsAdapter` / `CueStrikeSolver` as currently unreferenced infrastructure, not live gameplay authority.
- Golden infrastructure exists as ScriptableObject types, but no `PhysicsGoldenCase` `.asset` instances were found under `Assets`; runtime/catalog asset authoring is therefore still pending.
- `ShotCalibrationCase` already exposes the full calibration dimensions: Straight/Stun/Follow/Draw/LeftEnglish/RightEnglish/Cushion/Pocket, power, side, vertical, expected distance, distance tolerance, optional peak-speed target/tolerance.
- Do not invent GoldenCase asset values from assumptions. Asset authoring requires measured Truth values or an explicit production decision.
- Hardened `PhysicsGoldenRegressionReport.allCasesCovered`: coverage is only complete when catalog count equals measured-result count AND no result is invalid.
- Golden reporting therefore cannot claim full coverage merely because the number of result entries matches the catalog; invalid samples keep certification incomplete.
- Current blocker for actual Golden PASS remains measured Truth/runtime samples, not missing evaluator/reporting infrastructure.
- Hardened `ShotMeasurementTracker`: non-finite Rigidbody speed or measured distance now aborts capture instead of producing a poisoned measurement.
- Hardened `PhysicsGoldenRegressionRunner`: exposes `LastCatalogValid` and `LastCatalogError` so a failed catalog cannot be mistaken for an empty-but-valid regression run.
- These are certification guardrails only; no Physics Core behavior or gameplay tuning was changed.
- Do not delete yet; preserve the subsystem until a deliberate cleanup pass, but do not wire it into Golden tests implicitly.
- Keep Golden tests aligned to the confirmed live production authority: `CueShotValidator → CueShotData → SnookerCueController → Rigidbody.linearVelocity`.

Pattern:
`Cue input → committed shot → impact adapter → Rigidbody linear/angular state → ball motion → cushion/pocket`


Done:
- Added `PhysicsGoldenRegressionReport.cs` with total/pass/fail/invalid/pass-rate aggregation.
- Added `PhysicsGoldenRegressionRunner.cs` to evaluate measured samples against the Golden Case catalog.
- Kept measurement acquisition outside the evaluator so recorded/live data can share the same acceptance logic.

Pattern:
`Scene/Measurement Adapter → PhysicsGoldenMeasurement → RegressionRunner → Report`

Guardrail:
Do not claim this is a live physics regression run until a scene adapter actually produces repeatable measurements from the real table/balls.

Next:
Build the controlled-scene adapter and then create the first real Golden cases for Straight, Cushion and Pocket behavior.


## 2026-08-26 — Golden Measurement Bridge Checkpoint
Status: IMPLEMENTED / NOT RUNTIME-VERIFIED
Pattern: Existing Measurement Truth remains authoritative; Golden layer only captures, evaluates, and reports.
Implemented: PhysicsGoldenRegressionRunner.RecordMeasurement and HasMeasurementFor.
Implemented: PhysicsGoldenMeasurementBridge for transferring ShotMeasurementTracker samples into Golden Regression.
Guardrail: No duplicated physics calculation and no expected-value inference from measured data.
Next execution boundary: Controlled shot execution adapter → real scene measurement → Golden regression.


## 2026-08-26 — Controlled Shot Execution Pattern

Status: IMPLEMENTED / RUNTIME VALIDATION PENDING

ทำแล้ว:
- Added PhysicsGoldenShotExecutor as a controlled validation-only shot path.
- Connects Golden Case → ShotMeasurementTracker → PhysicsGoldenRegressionRunner.
- Uses Unity Rigidbody impulse; does not invent a second physics model.
- Added re-entry guard, impulse clamp, cancellation, and TryRun entry point.

Pattern:
Golden Case → Begin measurement → Controlled impulse → Physics settles → Measurement captured → Regression sample recorded.

Boundary:
This executor is a test/validation harness. VR gameplay cue input must remain a separate adapter and eventually feed the same measurement contract.

Next:
Create concrete Straight Golden Cases and a controlled scene adapter, then perform Unity compile/runtime verification before marking the milestone COMPLETE.

- Hardened `PhysicsGoldenCatalog.ValidateCatalog`: an empty/null case list is now invalid, preventing an empty catalog from being certified as valid coverage.
- Verified the catalog source remains brace-balanced after the guard insertion.
- Golden asset authoring remains intentionally blocked on measured Truth; no synthetic calibration values were introduced.

## Golden Certification � 2026-08-27
- Golden runtime infrastructure is code-complete for case validation, measurement capture, evaluation, reporting, and regression execution.
- `PhysicsGoldenMeasurementBridge` is the explicit runtime handoff from `ShotMeasurementTracker` into `PhysicsGoldenRegressionRunner`; it refuses capture without a valid assigned runner/case/tracker or measurement.
- `PhysicsGoldenRegressionRunner` rejects NaN/Infinity samples and rejects cases outside an assigned catalog.
- Actual Golden PASS remains dependent on authored calibration assets and real runtime measurements; no synthetic Truth values are permitted.
- Pattern: `Runtime Shot ? ShotMeasurementTracker ? GoldenMeasurementBridge ? RegressionRunner ? Evaluator ? Report`.

## Calibration Runtime Audit � 2026-08-27
- `CalibrationShotController`, `ShotCalibrationRunner`, and `ShotMeasurementTracker` form a complete calibration-side measurement path in code.
- No textual references to these three components were found in `.prefab`, `.unity`, or `.asset` files under `Assets`; they are currently tool/infrastructure code rather than confirmed scene-wired runtime objects.
- `CalibrationShotController` correctly resets both linear and angular velocity and begins measurement from a known origin.
- `ShotMeasurementTracker` waits for the configured settle duration before committing distance, and rejects non-finite speed/distance.
- Do not manufacture calibration assets or wire these tools into gameplay automatically; first establish the intended calibration scene/workflow.
- Pattern: `Calibration Controller ? Measurement Tracker ? Calibration Evaluator`; Golden bridge remains a separate explicit handoff.

## Golden Certification Asset Gate � 2026-08-27
Status: INFRASTRUCTURE VERIFIED / DATA PENDING
- Golden Case, Catalog, Evaluator, and Regression Runner source files are structurally balanced.
- No serialized PhysicsGoldenCase/PhysicsGoldenCatalog asset instances were found under Assets during this audit.
- Existing ShotCalibrationCase infrastructure already defines calibration dimensions; Truth values must come from measured runtime data.
- Certification rule: never synthesize expected distance/speed values merely to populate Golden assets.
- Next executable gate: create/import authoritative measured Truth data, then author Golden Case assets from that data.

## Golden Catalog Source Hygiene � 2026-08-27
Status: COMPLETE
- Audited `PhysicsGoldenCatalog.cs` and found literal escaped CR/LF text embedded in the source around `ValidateCatalog`.
- Normalized those literals to real line breaks without changing catalog behavior.
- Structural brace check passes after normalization (11/11).
- No Golden data values were fabricated or changed.

## Controlled Calibration Scene — Implementation Checkpoint
- Created dedicated scene: `Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity` from the existing PoolTable_8Ball scene; source scene remains untouched.
- Added `PhysicsCalibrationHarness` as a dedicated runtime measurement component; it resolves the live cue ball and `SnookerCueController`, executes a controlled shot, waits for settle, rejects invalid values, and stores distance/peak-speed samples.
- Harness is isolated from gameplay and defaults to `autoRun = false`, so opening the calibration scene cannot fire shots unexpectedly.
- Existing `PoolTable_8Ball` remains the reference source; no gameplay scene was modified.
- Runtime Truth capture still requires Play Mode execution in the Unity Editor because the authoritative sample must come from the real Rigidbody simulation.


## Certification Runbook Gate — 2026-08-27
- Added `Docs/147VR_PHYSICS_CERTIFICATION_RUNBOOK.md` as operational source of truth for controlled measurement -> Golden promotion -> regression.
- Case order: Straight, Stun, Follow, Draw, Left English, Right English, Cushion, Pocket.
- Runtime calibration harness uses Unity 6 `FindFirstObjectByType` discovery.
- Calibration batch runner exposes explicit `StopBatch()` control.
- No Golden expected values are synthesized.


## 2026-08-27 — Corrected Current-State Audit

Status: SOURCE AUDIT CORRECTED / RUNTIME PROOF STILL PENDING.

Important: previous percentage snapshots are not production-completion percentages. They describe source/contract maturity only. The authoritative current-state document is `Docs/147VR_CURRENT_STATE.md`.

Correction made today:
- `CalibrationShotController` previously began measurement on reset without launching a shot; this could record stationary/zero-distance samples.
- Added `ExecuteShot()` using the existing production cue contract: `CueStrokeModel -> CueShotValidator -> CueShotData -> Rigidbody.linearVelocity`.
- `CalibrationBatchRunner` now schedules actual executed shots between repetitions.

Verification boundary:
- Source files were re-read from the real project path.
- Unity 6000.4.4f1 batch validation was attempted after the change. The process initialized the project/licensing but exited before emitting the requested validation method result; therefore compile/runtime PASS is NOT claimed.
- No Golden expected values were fabricated.
- No source project other than 147 VR was modified.

Remaining true blockers:
- Real Unity runtime calibration samples.
- Persisted measured Truth.
- Concrete Golden Case/Catalog assets authored from measured Truth.
- Golden regression PASS with complete valid coverage.
- Cushion/Pocket measured validation.
- Gameplay physics integration.
- VR interaction completion.
- Quest end-to-end build/runtime proof.

Next exact step:
Run the controlled calibration scene in Unity, verify the actual cue-ball Rigidbody/table binding, execute repeated Straight shots, persist measured samples, then promote only measured Truth into Golden assets.


## 2026-08-27 Mandatory AI Work Protocol

`Docs/147VR_AI_WORK_PROTOCOL.md` is now the mandatory execution/handoff contract for every AI working on 147VR. Every meaningful work block must update the project Markdown state so another AI can continue without chat history. Source files and runtime evidence remain authoritative; Markdown records status and handoff. Source-level 100% must never be represented as runtime/production 100%.

## 2026-08-28 Commander Recovery / Preflight Gate

Commander recovery verified on the active Windows device. Added `Docs/147VR_DESKTOP_COMMANDER_PREFLIGHT.md` as the locked startup gate and linked it from `Docs/147VR_REALITY_MAP.md`.

Package-aware Unity compile probe was run directly against `C:\Users\mongo\UnityProjects\147 VR` using Unity 6000.4.4f1. Licensing resolved, but Unity could not connect to the Package Manager IPC stream after 30 seconds and exited with return code 1. Treat this strictly as an environment/UPM blocker.

No gameplay/source changes were made from this failure. Calibration was intentionally not executed because the Compile Gate failed.

Next: recover/diagnose UPM IPC with Unity fully stopped, then rerun package-aware compile validation and inspect the resulting log before touching source.


## 2026-08-29 — Recovery Pattern / Real Straight Truth

Recovery result:
- The 05:05 GUI recovery evidence was preserved: Unity 6000.4.4f1 connected to UPM, registered packages, completed script compilation, and loaded the M21 calibration scene.
- A resumed package-aware batch initially failed because the Commander environment inherited `C:\Users\mongo\AppData\Local\npm-cache\_npx\...\node_modules\.bin` in `PATH`. This contaminated Unity/UPM child-process startup and reproduced the 30s UPM IPC timeout.
- Clean launch pattern: preserve normal UPM/package-aware mode, set the canonical Windows user/temp environment, set `UNITY_UPM_TIMEOUT=120`, and remove the inherited `npm-cache\_npx` entry from `PATH` before spawning Unity.
- With that environment, Unity connected to UPM in 0.3s and the controlled Straight runtime batch completed with exit code 0.

Real Truth checkpoint:
- `straight_runtime_measurements.json` now contains five actual PhysX samples captured by the controlled calibration scene.
- Distance: 0.7282500267m on all five repetitions.
- Peak speed: 4.0529303551m/s, then 4.0529942513m/s on the remaining four repetitions.
- These values are runtime evidence and are the only source used for the new Golden baseline.

Golden checkpoint:
- `147VR-PHY-001_Straight_r3.asset` is the current revision-3 Golden case.
- `147VR-PHY-001_Straight_r3_Calibration.asset` stores the measured Straight calibration contract.
- `147VR_GoldenCatalog.asset` now references revision 3; older revision-2 artifacts remain historical and are not the active catalog entry.
- `147VR_GoldenCI_Straight_Input.json` was refreshed from the measured runtime file, not hand-tuned.
- Unity headless Golden regression returned total=5, passed=5, failed=0, revision=3, exit code 0.

Boundary:
- This proves the Straight runtime/Golden path only. It does not certify Stun, Follow, Draw, English, Cushion, Pocket, Quest runtime, or full gameplay production readiness.
- UPM `path argument ... Received undefined` warnings may still appear during Unity startup callbacks, but clean PATH inheritance is sufficient to prevent the prior hard UPM startup timeout for the verified batch path.
- Do not silently delete old Golden artifacts; preserve them as historical evidence until a deliberate cleanup/audit pass.

Next:
- Continue from the existing M2.1 Stun controlled calibration path if feature certification is the next priority.
- Use the same clean Unity child-process environment for future unattended runs.
- Never fabricate Golden values or promote a case without persisted real runtime measurements.


## 2026-08-29 — M2.1 Stun Forensic Boundary

- M2.1 Stun execution was resumed after DC recovery using the clean package-aware Unity environment. Straight Golden revision 3 was not modified.
- Original automation behavior was reproduced: `RunRealBatch()` enters PlayMode but a `-quit` launch can terminate the editor lifecycle before the update-driven completion monitor. A no-`-quit` run proved the editor can remain alive for the runner lifecycle.
- Real M2.1 execution then exposed scene/physics issues: the derived cue SphereCollider was disabled; Bed_Collider top was Y=0 while the visual/runtime table surface is Y=0.756; the cue/object pair was also positioned near the table edge.
- M2.1-only preflight normalization now enables ball colliders, normalizes Bed_Collider world geometry, forces the relevant layer collision pair ON, and centers the cue/object setup.
- `SnookerPhysicsSetup.EnsurePhysics()` reports the expected table bounds of about 1.78m x 3.57m and surface top Y=0.756m.
- Even after these corrections, the real PlayMode batch observes cue/object collision but the bodies subsequently leave the playing surface and fall, so no valid five-repetition Stun measurement JSON has been produced.
- Several headless scripted-physics harnesses were tested to isolate lifecycle versus scene-physics behavior. Their outputs are forensic diagnostics only and must not be promoted to Golden evidence.
- M2.1 remains BLOCKED. The next investigation target is the runtime collision geometry/physics authority path causing post-contact fall-off. No Stun Golden asset or PASS declaration is permitted until real JSON + 5/5 regression evidence exists.

## 2026-08-29 — M2.1 Physics Forensic Checkpoint

- Real Stun contact is verified, but post-contact support on the playing surface is not.
- Failure signature: after target collision, cue/object descend out of the table; cue speed reaches ~49.5 m/s by timeout, matching gravity-driven fall rather than a valid settled Stun shot.
- Physics layer collision matrix was checked and is fully enabled.
- M2.1 runner now explicitly initializes `SnookerPhysicsSetup`, places both balls at authoritative `SurfaceTopY + radius`, and enables ContinuousDynamic CCD before launching the real shot.
- Temporary collision logging records every Cue/Object collision partner, contact normal, Y, and relative velocity for the next isolation run.
- Do not modify or regenerate the certified Straight baseline while debugging M2.1.


## 2026-08-29 — M2.1 Stun Certification COMPLETE

- Collision Probe evidence proved the runtime physics surface is real and supported the balls: `Bed_Collider` enabled, topY=0.756000; runtime `Surface` contacts occurred at Y≈0.76 with normal (0,1,0).
- The initial post-contact fall was traced to an inherited source-scene lane near the table edge, then a centered lane exposed the legitimate middle-pocket trajectory. M2.1 now uses a deterministic +0.30m X calibration lane to avoid pocket topology while preserving a straight real shot.
- `M21StunBatchRunner` was hardened to place cue/object from `physicsSetup.TableBounds.center`, Y=`SurfaceTopY + actual collider radius`, with ContinuousDynamic CCD.
- Real Stun batch evidence: 5/5 repetitions, shotSpeed=4.0m/s, cueSpeedAtContact=0.1531031132m/s, cueResidualSpeed=0, objectPeakSpeed=3.6744694710m/s, object final displacement=0.0230781436m, all pass.
- Runtime JSON: `Assets/AAA/PhysicsCalibration/RuntimeMeasurements/stun_runtime_measurements.json`.
- Measured Golden created: `Assets/AAA/PhysicsCalibration/Golden/147VR-PHY-002_Stun.asset`, revision 1, category Stun, tolerance 0.5%.
- `Stun_Runtime_Case.asset` was created from the measured runtime data; `147VR_GoldenCatalog.asset` now contains both the active Straight Golden and PHY-002 Stun.
- Headless Stun Golden regression: 5/5 PASS.
- Golden provenance is persisted to the runtime JSON timestamp/scene/Unity version. No fabricated measurements were used.
- Temporary collision diagnostics remain useful for forensic work, but they are not themselves the Golden source of truth.
- Straight Golden revision 3 remains untouched and independently certified.
- Scope boundary: M2.1 certification proves the controlled Stun calibration contract; it does not yet prove production gameplay Stun, all pocket/cushion edge cases, or VR hand/cue interaction.

## 2026-08-29 — M2.2 Follow Certification COMPLETE

- M2.2 was built without modifying PHY-001 Straight r3 or PHY-002 Stun r1.
- Follow uses a dedicated derived calibration scene and the existing single CuePhysicsAdapter authority.
- Runtime batch used shotSpeed=4.0m/s and vertical follow input +0.75. Five genuine PhysX repetitions completed.
- Collision probe captured actual Cue -> Object contact and showed post-contact cue velocity aligned with shot direction.
- Follow telemetry now records contact vectors, post-contact cue vector/speed, first-flight distance, cue post-contact travel, object peak speed, and follow direction dot.
- Real values: post-contact cue speed ~0.171718m/s, followDot=1.0, firstFlight=0.900657058m, objectPeakSpeed mean=3.674469233m/s.
- Persisted JSON: `Assets/AAA/PhysicsCalibration/RuntimeMeasurements/follow_runtime_measurements.json`.
- Measured Golden: `Assets/AAA/PhysicsCalibration/Golden/147VR-PHY-003_Follow.asset`, revision 1.
- Calibration contract: `Assets/AAA/PhysicsCalibration/Follow_Runtime_Case.asset`.
- Golden catalog registration succeeded without replacing earlier certified cases.
- Headless Follow Golden regression returned 5/5 PASS at 0.50% tolerance and also checked positive post-contact cue speed.
- M2.2 is PASS for the controlled Follow contract. Full production Follow, cushion/pocket interaction, and hand-driven cue validation remain separate scope.


## 2026-09-03 — M7.4 Clean MainScene

- Created `Assets/Scenes/147VR_MainScene.unity` from the recovered/current SampleScene using Unity's scene duplication API, preserving serialized references.
- Verified migration state through the Unity editor method: `V004=1`, `V003=0`, `Bed_Collider=1`, Build Settings index 0.
- The migration method was archived at `Docs/M7_4_BACKUPS/M7_4_CreateCleanMainScene_20260903.cs` and removed from `Assets/Editor` after successful execution.
- Do not delete `SampleScene.unity` yet. It remains the rollback source until MainScene passes runtime and visual/physics alignment validation.
- Physics Authority and Golden Truth remain locked/unchanged.


## 2026-09-20 — YOLO Calibration Checkpoint
- REAL Straight calibration is now runtime-verified on Unity 6000.4.4f1 with 5 persisted repetitions.
- Fresh runtime JSON timestamp: 2026-09-20T08:22:56.4497414Z. Distance mean 0.7282500267m; peak speed mean ~4.052994m/s.
- PHY-001 Straight Golden revision 2 was regenerated only from the fresh runtime JSON; provenance and sample equality verified PASS.
- Golden regression returned 5/5 PASS at 0.50% tolerance.
- TableSurfaceProfile runtime bridge is implemented as a certified-gated adapter. RuntimeBaseline profile remains `measuredTruthCertified=false`; its 0.18/0.32/0.08 values are seeds only and are NOT production tuning.
- Runtime construction smoke PASS verified that SnookerPhysicsSetup creates the runtime Surface and attaches TableSurfaceController with the expected profile reference while the profile is inactive.
- Do not flip measuredTruthCertified=true until rolling/sliding/spin coefficients are derived from measured truth and independently re-regressed.
- Do not interpret the Straight Golden regression as independent repeatability of cloth friction coefficients; it validates the Golden evaluator/regression plumbing against the measured baseline.
- UPM environment workaround is currently the known-good unattended launch route for this machine/project; keep one Unity executor active at a time.
- Next engineering target: measured cloth speed/friction matrix, coefficient fit, then enable certified profile and rerun Straight/Cushion/Pocket regression before Quest hardware.

## 2026-09-20 — YOLO Physics Gate Closure Checkpoint

- Runtime-equivalent cloth calibration completed and PROMOTED from fresh Unity PhysX evidence. Profile asset: `Assets/AAA/PhysicsCalibration/TableSurfaceProfile_RuntimeBaseline.asset`.
- Persisted certified coefficients: rollingFriction=0.45200002, rollingDamping=0.21000001, slidingFriction=0.6807868, spinFriction=0.20040171, measuredTruthCertified=1.
- Acceptance evidence: 5 repetitions; max replay error rolling=0.88%, sliding=1.50%, spin=0.02%; acceptance gate <=2% on all channels.
- Semantic physics fixes made in active project: slip-based rolling/sliding classification; world-space ball radius; correct bottom-contact velocity sign; rolling angular-velocity cap for 4 m/s true-roll probes; vertical-only spin damping; rolling resistance torque coupling; rolling viscous damping fit. `0.18/0.32/0.08` are no longer used as production coefficients and remain historical seed values only.
- Straight Golden regression after certified cloth bridge: 5/5 PASS at 0.50% tolerance. Historical regression fixture was restored after test execution.
- Fresh current-profile M3 Cushion runtime batch completed 7 cases x 5 repetitions. Current measurements regressed against the existing M3 Goldens at 35/35 PASS; the fresh measurements were then promoted so PHY-007..013 provenance now points to the 2026-09-20 runtime JSON timestamps.
- M4.2 Pocket/Jaw/Rattle Golden certification: 6/6 cases, 30/30 REAL samples, Left/Right Jaw symmetry PASS. Current M4.2 runtime regeneration was not available because the repository contains the Golden certification path but no dedicated fresh M4.2 runtime runner; therefore M4.2 remains a REAL historical runtime Golden certification, not a post-cloth fresh-runtime re-capture.
- Quest hardware gate: no authoritative Quest 2/3 device evidence was obtained from the PC during this run. ADB startup was unreliable/hanging and all stray ADB/shell processes were cleaned up. Do not claim Quest 2/3 PASS yet.
- Current remaining gate: Quest 2/3 hardware build/runtime proof. No change to active gameplay M5 source was required by this physics certification block.

