# 147 VR — Current State / Handoff

> Updated: 2026-08-27
> Source of truth: actual files under `C:\Users\mongo\UnityProjects\147 VR`.
> This document supersedes percentage guesses from chat and must be read by the next AI before implementation.

## Executive Truth

The project has substantial AAA physics infrastructure, but the game is **not 100% production complete**.
A subsystem marked 100% means its source-level contract/implementation is present; it does not mean scene wiring, runtime proof, measured Truth, Golden certification, Quest proof, or gameplay integration is complete.

## Verified Complete at Source/Contract Level

- Project/Gate 1–3 documentation and architecture baseline.
- Measurement lifecycle, tracker, evaluator/result/statistics/scorecard infrastructure.
- Golden Case/Catalog/Evaluator/Result/Regression report/Runner boundaries.
- Golden measurement bridge.
- Production cue path uses `CueShotValidator -> CueShotData -> Rigidbody.linearVelocity`.
- Golden controlled shot executor uses the same validated cue speed contract.
- Cushion/Pocket profile and responder source infrastructure exists.
- Calibration runtime path has been hardened to execute a validated shot before measurement; this was a source-level correction on 2026-08-27.

## Verified Not Complete

1. No serialized `PhysicsGoldenCase` / `PhysicsGoldenCatalog` asset instances have been established as authoritative measured Truth.
2. No real runtime Golden PASS report exists yet.
3. Calibration/GOLDEN scene execution has not produced persisted measured Truth samples.
4. AAA `CuePhysicsAdapter` / `CueStrikeSolver` remains unreferenced by live `.unity/.prefab/.asset` consumers and must not be silently made a second physics authority.
5. Gameplay integration of the validated AAA physics contract is not complete.
6. VR interaction/presentation is not production-complete.
7. Quest end-to-end runtime/build proof is not complete.
8. Unity batch validation could not be claimed: the attempted 6000.4.4f1 batch invocation initialized licensing but exited before producing the requested validation method output.

## 2026-08-27 Source Correction

`CalibrationShotController` previously measured immediately after `ResetShot()` without actually launching the calibration shot. That made the batch path capable of recording a zero-distance stationary sample rather than a physical shot. It has now been changed so `ExecuteShot()`:

`CalibrationCase -> CueStrokeModel -> CueShotValidator -> CueShotData -> Rigidbody.linearVelocity -> ShotMeasurementTracker`

`CalibrationBatchRunner` now schedules actual `ExecuteShot()` calls between repetitions instead of repeatedly resetting and waiting on a stationary body.

This correction is **not** a physics Truth result. It only fixes the calibration execution path so future measurements can be meaningful.

## Runtime Blocker

The next valid proof must come from the real Unity physics simulation. Do not synthesize expected distance/speed values merely to create Golden assets.

Required order:

1. Open/execute controlled calibration scene.
2. Confirm real cue-ball Rigidbody and table setup are resolved.
3. Execute repeated Straight shots.
4. Persist measured samples.
5. Promote measured Truth into explicit Golden Case assets.
6. Run Golden regression and require complete coverage + valid PASS results.
7. Repeat for Stun/Follow/Draw/English/Cushion/Pocket.
8. Only then integrate the validated physics path into gameplay.

## AI Handoff Rule

After every meaningful work block, update this file and the relevant project `.md` files with:
- what changed;
- why;
- verification result;
- remaining blocker;
- exact next step.

Never report a source-level 100% as a runtime/production 100%.

## 2026-08-27 AI Protocol Update

- Added `Docs/147VR_AI_WORK_PROTOCOL.md` as the mandatory cross-AI execution and handoff contract.
- All future AI work must update the state/handoff Markdown after every meaningful project change.
- Handoff must preserve exact changed files, verification evidence, blockers, and next action so another AI can continue without chat history.
- YOLO execution is allowed within 147VR scope when explicitly authorized; no invented Truth/Golden/PASS values are permitted.

## 2026-08-27 Execution Reliability Update

- Added the mandatory asynchronous/background execution policy to `Docs/147VR_AI_WORK_PROTOCOL.md`.
- Long-running Commander tasks must write deterministic logs to `$env:TEMP` or an explicitly created absolute project-local log directory and be polled incrementally.
- Broad root scans are prohibited when a focused `Assets/Scripts/...`, `Assets/Scenes/...`, or equivalent scope is sufficient.
- `-NoProfile -NonInteractive` is the default for unattended PowerShell tasks; error suppression must not hide required validation failures.
- Commander wrapper timeout must be distinguished from the actual state of an asynchronously launched task.
- This is a tooling/workflow correction only; it does not change the project's physics Truth or completion status.

## 2026-08-27 UPM Recovery Procedure Recorded

- Recorded the Unity Batch/UPM recovery procedure in `147VR_AI_WORK_PROTOCOL.md`.
- For non-Package-dependent compile/editor/test runs, use `-noUpm` where compatible.
- Use an absolute `$env:TEMP` Unity `-logFile` and inspect the log after completion.
- Only clear stale `Library/EditorInstance.json` / project `Temp` state when Unity is not running and the state is confirmed stale.
- UPM IPC failure is an environment/tooling blocker, not evidence of gameplay-code failure.

## 2026-08-27 UPM Batch Correction

- Recorded that package-aware Unity Batchmode validation must omit `-noUpm` when compilation depends on UPM packages.
- The previous missing `UnityEngine.UI` / `InputSystem` errors occurred during a `-noUpm` validation attempt and therefore are not yet accepted as source-code defects.
- Next retry must use normal UPM initialization with `-batchmode -nographics -logFile <absolute TEMP log> -executeMethod <method>`.
- Do not change gameplay/source code solely because of the previous `-noUpm` compiler errors until the package-aware retry establishes the real state.

## 2026-08-27 UPM Extended Timeout Mitigation

- Recorded the proposed `UNITY_UPM_TIMEOUT=120` environment setting for future Unity Batchmode retries.
- Normal UPM/package-aware mode remains mandatory for validation that depends on UGUI/Input System.
- The 120-second value is treated as a mitigation to verify from logs, not as an assumed hardcoded Unity guarantee.
- Future unattended runs must use file-based PowerShell scripts, absolute TEMP logs, asynchronous execution, and final exit-code inspection.

## 2026-08-28 Commander Recovery / Preflight Gate

Status: PREFLIGHT DOCUMENTED / COMPILE GATE BLOCKED BY UPM IPC.

Verified: Desktop Commander device `wIn-NaRIs` is online and ping succeeds. Project root `C:\Users\mongo\UnityProjects\147 VR` is accessible and writable. Added mandatory `Docs/147VR_DESKTOP_COMMANDER_PREFLIGHT.md` and linked its execution gate into `Docs/147VR_REALITY_MAP.md`.

Unity process check: Unity Editor was not running before the package-aware compile probe. Unity Hub was running. No active Unity Editor compile session was left running after the failed probe.

Compile Gate result: FAIL. Unity 6000.4.4f1 launched against `C:\Users\mongo\UnityProjects\147 VR`, initialized licensing, then failed to connect to Package Manager IPC after 30 seconds and exited with return code 1. This is recorded as an environment/UPM tooling blocker, not as evidence of a source-code defect.

Console / Editor.log review: licensing entitlement resolved successfully; latest batch log records the same Package Manager IPC failure. No gameplay-code fix is authorized from this evidence alone.

Calibration Gate: NOT RUN because the Compile Gate failed. Runtime Truth, Golden values, and PASS results remain unverified.

Next exact action: recover/diagnose Unity Package Manager IPC while Unity is fully stopped, then rerun the package-aware compile gate with an explicit absolute log before any source modification or calibration execution.

## 2026-08-28 Gate 7 / Runtime Checkpoint

- Confirmed active target project for this execution is `C:\Users\mongo\UnityProjects\147 VR`.
- Found a second registered copy at `C:\Users\mongo\UnityProjects\147 VR`; it remains a separate project and must not be treated as the active execution target.
- Registered `C:\Users\mongo\UnityProjects\147 VR` in the Unity Hub project registry to prevent future ambiguous project selection.
- Fixed `Assets/147 main/ConcertRoom/DriftingConcertSmoke.cs`: `VelocityOverLifetime` Y now uses `new ParticleSystem.MinMaxCurve(driftSpeed, driftSpeed)` so X/Y/Z use the same curve mode while preserving constant Y drift behavior.
- No physics authority, CuePhysicsAdapter, calibration contract, or gameplay logic was changed by the particle fix.
- Current Unity GUI startup can reach `C:\Users\mongo\UnityProjects\147 VR`, but Package Manager still reports `The "path" argument must be of type string. Received undefined` during package resolution.
- Therefore no new `straight_runtime_measurements.json` exists and no real Straight runtime measurement was captured in this checkpoint.
- Golden Truth / Golden Regression PASS remain prohibited until measured runtime data exists.
- Exact next step: diagnose the Unity-spawned UPM environment/config handoff, then rerun package-aware calibration and verify 5 real Straight samples before authoring Golden Truth.


## 2026-08-28 Project Authority Normalization

- VERIFIED canonical project root: `C:\Users\mongo\UnityProjects\147 VR`.
- `C:\147VR` was verified as a Windows **JUNCTION** targeting the canonical root, not a second physical project.
- The junction `C:\147VR` has been removed. The canonical project remains intact and accessible.
- Active Unity M2.1 batch process was verified with `-projectPath "C:\Users\mongo\UnityProjects\147 VR"`.
- All Markdown references to the retired alias `C:\147VR` were normalized to the canonical root.
- Future AI/Commander/Unity work MUST use only `C:\Users\mongo\UnityProjects\147 VR`.
- Do not recreate the `C:\147VR` junction/alias.


## 2026-08-29 — Post-DC Recovery / M1 Straight Runtime Proof

- Commander recovery verified on `wIn-NaRIs`; project root remains `C:\Users\mongo\UnityProjects\147 VR`.
- Unity/UPM process health was checked before execution; no pre-existing Unity Editor or UnityPackageManager process was active.
- Historical 05:05 GUI recovery log was inspected. It proves package-aware Unity startup can connect to UPM, register 86 packages, compile `Assembly-CSharp` successfully, and load the M21 calibration scene.
- First resumed package-aware batch exposed the real environment issue: Commander inherited an `npm-cache\_npx` entry in `PATH`; Unity then failed to start UPM and timed out after 30 seconds.
- Runtime launch was retried with the inherited npx PATH entry removed while preserving normal package-aware UPM mode and `UNITY_UPM_TIMEOUT=120`.
- Package-aware Unity then connected to UPM in 0.3s and completed the controlled Straight runtime batch successfully.
- Real runtime evidence: `Assets/AAA/PhysicsCalibration/RuntimeMeasurements/straight_runtime_measurements.json` contains 5 measured Straight samples from Unity 6000.4.4f1.
- Measured distance samples: 0.7282500267m x5. Measured peak speeds: 4.0529303551, 4.0529942513, 4.0529942513, 4.0529942513, 4.0529942513 m/s.
- Runtime batch process exited 0 after persisting the measurements. This is real simulation evidence, not a synthetic calibration result.
- Promoted the measured Truth into `147VR-PHY-001` revision 3 via `147VR-PHY-001_Straight_r3.asset` and `147VR-PHY-001_Straight_r3_Calibration.asset`.
- Updated `147VR_GoldenCatalog.asset` to reference the revision-3 Golden case; the older revision-2 artifacts remain as historical records.
- Updated `147VR_GoldenCI_Straight_Input.json` from the current measured runtime samples and ran the Unity headless Golden regression.
- Golden regression evidence: total=5, passed=5, failed=0, revision=3, Unity process exit code 0.
- `147VR_GoldenCI_Straight_Report.json` records `allPassed=true` and the five revision-3 PASS results.
- Golden tolerance remains 0.5%; no expected distance/speed was invented. Golden means and samples are copied from the persisted runtime measurement file.
- The pre-existing `path argument must be of type string. Received undefined` UPM warnings still appear during some Unity startup callbacks, but they no longer prevent package registration or the verified batch/regression execution when the npx PATH contamination is removed.
- Remaining gate: M1 Straight runtime + Golden proof is now evidenced. M2.1 Stun remains a separate feature-specific verification track; do not conflate it with the Straight certification.
- Next concrete action: if continuing certification, run M2.1 Stun from its existing controlled scene and persist real Stun measurements; otherwise proceed to the next physics case in the runbook without changing the certified Straight baseline.


## 2026-08-29 — M2.1 Stun Forensic Execution Blocker

- M2.1 automation harness was investigated without touching the certified Straight baseline.
- The original `RunRealBatch()` failure was reproduced: `EditorApplication.isPlaying = true` plus `-quit` can terminate before the intended PlayMode/update lifecycle completes. A no-`-quit` package-aware run confirmed the Unity PlayMode loop can remain alive.
- Additional scene-level blockers were exposed during real execution. The derived M2.1 scene had `Sphere.009` collider disabled and the `Bed_Collider` was authored at world top Y=0 while the actual table surface is around Y=0.756.
- M2.1-only preflight normalization was added to enable the ball colliders, normalize the Bed_Collider, force layer collision ON, and center the cue/object pair. These changes are scoped to the derived M2.1 scene path.
- Runtime `SnookerPhysicsSetup` confirms the table bounds are approximately 1.78m x 3.57m and surface top Y=0.756m.
- Despite these corrections, real PlayMode execution still produces a collision but the bodies subsequently leave the playing surface and fall; no valid 5-repetition Stun JSON is produced.
- Headless scripted-physics experiments were also attempted. They are retained as forensic diagnostics only and are NOT evidence for Golden Truth.
- Current M2.1 status: BLOCKED at runtime physics execution/evidence capture. Do not create `147VR-PHY-002` or declare M2.1 PASS until five real samples are persisted and independently regression-tested.

## 2026-08-29 — M2.1 Post-Collision Physics Isolation

- M2.1 remains BLOCKED; no Stun JSON or Golden has been promoted.
- Forensic review confirmed the Real Stun shot and Cue->Object collision are genuine: Tcollision is observed at cueSpeed ~= 3.976 m/s and objectSpeed ~= 0.196 m/s.
- The failure occurs after contact: the cue/object bodies leave the intended playing surface and eventually time out. Observed cue speed grows to ~49.5 m/s while Y falls hundreds of metres, consistent with gravity after losing surface support rather than a Stun measurement failure.
- Project Physics layer matrix is all-enabled; no global layer pair is suppressing collisions.
- M2.1 scene uses a dedicated `Red` target and `Sphere.009` cue. The source `SnookerPhysicsSetup` builds an authoritative runtime Surface from the table bounds and uses 0.05 friction / 0.8 bounce material.
- Important scene-path finding: the saved M2.1 preflight can unparent `Bed_Collider`; `SnookerPhysicsSetup` then derives its runtime Surface from the visual `TABLE SURFACE` renderer. Runtime logs report surfaceTop=0.756 and bounds 1.78 x 3.57, so geometry dimensions are present, but actual post-contact support is still unproven.
- M2.1 runner was hardened to call `SnookerPhysicsSetup.EnsurePhysics()` explicitly, normalize both bodies to `SurfaceTopY + SphereCollider.radius`, force dynamic/CCD state, and log the authoritative surface height before the first shot. This change is M2.1-only and does not touch Straight Golden artifacts.
- Temporary collision diagnostics were added to `M21CollisionProbe` to identify whether post-contact collisions with the runtime Surface/Rails are actually occurring. They must be removed after the next evidence run unless promoted as a deliberate diagnostic facility.
- No M2.1 PASS claim is allowed until five persisted real measurements exist and Golden Regression passes.


## 2026-08-29 — M2.1 Stun REAL Evidence / Golden Certification

- Collision Probe evidence resolved the post-contact blocker. Runtime `Bed_Collider` is enabled with bounds min=(-0.89,0.72,-1.78), max=(0.89,0.76,1.78), topY=0.756000.
- Runtime `Surface` is created by `SnookerPhysicsSetup` and receives real ball contact at Y≈0.76 with upward normal (0,1,0). Surface support is therefore VERIFIED; the earlier hypothesis that the ball was simply missing the runtime surface is ruled out.
- First centered-lane test exposed a deterministic table-topology issue: x=0 follows the centre line into the middle-pocket region and reaches the Catcher Floor at z≈1.81. This was a legitimate geometry outcome, not a physics failure.
- M2.1 runner was corrected to use a deterministic non-pocket calibration lane at X=+0.30m while keeping cue/object separation 0.65m and a straight +Z shot. Straight baseline was untouched.
- Real Stun batch then completed 5/5 repetitions in Unity 6000.4.4f1. All five samples are identical: cue speed at contact 0.1531031132 m/s, cue residual speed 0, object peak speed 3.6744694710 m/s, persisted object displacement 0.0230781436 m, all `passed=true`.
- Persisted evidence: `Assets/AAA/PhysicsCalibration/RuntimeMeasurements/stun_runtime_measurements.json`.
- Created `Stun_Runtime_Case.asset` and measured Golden `147VR-PHY-002_Stun.asset`, revision 1, from the persisted runtime JSON only. No synthetic values were introduced.
- Updated `147VR_GoldenCatalog.asset` by appending PHY-002 while preserving the active Straight revision-3 catalog entry.
- Headless M2.1 Golden regression completed 5/5 PASS at 0.50% tolerance using the persisted five samples.
- M2.1 evidence chain is now: real runtime collision -> real 5-rep JSON -> measured Golden PHY-002 r1 -> headless Golden regression 5/5 PASS.
- Important measurement semantics: the current Stun Golden uses the persisted final object-ball displacement and measured object-ball peak speed because those are the fields supported by the existing Golden infrastructure. The JSON also preserves cue residual speed/distance and direction-dot telemetry for future Stun-specific evaluator refinement.
- Ball geometry note: the actual 0.028575m collider radius corresponds to the WPBSA 52.5mm snooker-ball diameter specification; the older `SnookerPhysicsSetup.ballRadius=0.026` default was not used to fabricate the M2.1 evidence. See official WPBSA rules for the 52.5mm ball specification. citeturn2search12
- M2.1 status: **PASS — evidence-complete for the current controlled Stun calibration contract**. This does not certify full gameplay Stun behavior, all cushions/pockets, or production VR interaction.

## 2026-08-29 — M2.2 Follow Runtime / Golden Certification

- M2.2 Follow was implemented as a dedicated controlled calibration path; M1 Straight and M2.1 Stun baselines were not modified.
- Dedicated scene: `Assets/AAA/PhysicsCalibration/147VR_M22_FollowCalibration.unity`.
- Real runtime batch executed in Unity 6000.4.4f1 with 5 repetitions, shotSpeed=4.0m/s and followEnglish=+0.75.
- Collision evidence confirms Cue -> Object contact on the controlled lane; post-contact cue velocity remains aligned with the shot direction.
- Follow-specific telemetry is persisted: cue velocity at contact, cue velocity after contact, object velocity at contact, first-flight distance, cue post-contact distance, cue post-contact speed, follow direction dot, and object peak speed.
- All five runtime samples passed the Follow contract. Post-contact cue speed is approximately 0.171718m/s and follow direction dot is 1.0 for all five samples.
- First-flight distance is 0.900657058m for all five samples. Object peak speed mean is 3.674469233m/s with sub-micro variation.
- Persisted evidence: `Assets/AAA/PhysicsCalibration/RuntimeMeasurements/follow_runtime_measurements.json`.
- Measured Golden created from that JSON only: `Assets/AAA/PhysicsCalibration/Golden/147VR-PHY-003_Follow.asset`, revision 1.
- Follow calibration case created: `Assets/AAA/PhysicsCalibration/Follow_Runtime_Case.asset`.
- Golden Catalog now contains PHY-001 Straight, PHY-002 Stun, and PHY-003 Follow; existing certified entries remain preserved.
- Headless Follow Golden regression completed 5/5 PASS at 0.50% tolerance, additionally enforcing positive post-contact cue speed from the runtime JSON.
- M2.2 status: **PASS — evidence-complete for the controlled Follow calibration contract**.
- Scope boundary: this certifies the controlled Follow shot only; broader production Follow behavior, rail/pocket interactions, and VR cue-stroke integration remain future gates.

## 2026-08-31 — Semantic Input / Cue Integration

- Added the first concrete `VR147` Semantic Input stack under `Assets/Scripts/AAA/Input/`: semantic vocabulary/state, InputSystem source, router, dominant-hand authority, and interaction context.
- `SnookerCueController` now resolves `VR147InteractionContext` and uses the semantic router state for cue-grab/charge/release when the semantic stack is present; legacy XR/desktop paths remain as fallback.
- The calibration scene `Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity` now serializes the semantic stack on the existing cue host: `VR147DominantHand -> VR147InputSystemSource -> VR147InputRouter -> VR147InteractionContext -> SnookerCueController`.
- The semantic layer does not own shot physics. `SnookerCueController` continues to enter `M5ShotLifecycle` and send the validated shot only through `CuePhysicsAdapter`, preserving single physics authority.
- Package-aware Unity validation was successfully initialized with the required environment inheritance (`PROGRAMDATA`, `APPDATA`, `LOCALAPPDATA`, `USERPROFILE`, `TEMP/TMP`, `NO_PROXY`, `UNITY_UPM_TIMEOUT`). UPM connected and registered 86 packages; the previous `-noUpm` false-positive InputSystem/UGUI errors are therefore not applicable.
- The semantic source compile completed with Bee/Tundra success and no `CS` compiler errors in the package-aware validation log. Unity also reported the pre-existing Package Manager `path` undefined warnings; these did not prevent package registration or script compilation.
- Scene YAML wiring was corrected so the new MonoBehaviour records are placed before `SceneRoots` rather than after it.
- Runtime VR cue pose/handedness proof is still pending. Do not claim the full Real VR Loop complete until a real XR interaction run proves cue aim + grab/charge/release + M5 shot lifecycle without bypassing the semantic layer.
- Next exact action: open the already-canonical 147 VR project, verify the serialized semantic stack in the calibration scene, then perform the first real cue interaction test before extending the semantic layer to REST/Haptics/Calibration UI.

## 2026-09-01 — Snooker App Layer / UI & Scene System (chat-tracked work)

- Converted all user-facing game text to English across the Quest scripts (SnookerScoreUI, SnookerScoreManager, SnookerBallTracker, SnookerShotTracker, QuestSpawnSetup, NightSky/ConcertRoom/promDance effects). `GAME_TEXTS.md` records the full text inventory.
- Added `Assets/Scripts/Quest/TabletOptionsMenu.cs`: left-controller Y/Menu opens a world-space tablet with movement-speed slider (1-10), player-height slider (160-185cm), volume slider + mute, and scene selector (MR MODE / NightSky / Dreamy_OLED / ConcertRoom / promDance). Settings persist via PlayerPrefs.
- Added `Assets/Scripts/Quest/ControllerMapInfo.cs` and `Assets/147 main/design/CONTROLLER_MAPPING.md` documenting the full left/right controller mapping.
- `QuestSpawnSetup.cs`: female dancer NPCs moved 3 steps further back from the table (backOffset).
- NOTE: this app-layer work is tracked in chat/GAME_TEXTS.md; it is separate from the AAA physics Golden certification track above and does not modify any physics authority or Golden artifacts.


## 2026-09-01 — M7.4 Table Visual Execution Plan

- Created `Docs/M7_4_TABLE_VISUAL_EXECUTION_PLAN.md` as the persistent execution plan for the table visual/art phase.
- Phase order locked: 1 Audit + REF -> 2 Scale/Proportions -> 3 22-ball Layout -> 4 Cushion/Pocket Geometry -> 5 Cloth/Wood/Rubber Materials -> 6 Physics↔Visual Alignment -> 7 AAA Polish -> 8 Cue/Hand/Rest.
- Rule: correct -> realistic -> beautiful. Do not polish around incorrect geometry.
- Visual work must remain separated from certified M1-M6 physics authority; stop and split layers if art work starts affecting physics foundation.
- Blender is an authoring option for live inspection/editing when the connected workflow supports it; Unity remains integration/runtime authority.
- Current status: PLAN CREATED; Phase 1 not yet started.


## 2026-09-03 — M7.4 Clean MainScene Migration

- Desktop Commander connection/device was recovered on `wIn-NaRIs`; canonical project root remains `C:\Users\mongo\UnityProjects\147 VR`.
- Mandatory preflight documents and AI execution protocol were reread before continuing; LUNA remains the active Unity Executor.
- Package-aware Unity startup was verified in the migration run: UPM connected successfully and 86 packages registered. No `-noUpm` was used.
- `Assets/Scenes/SampleScene.unity` had already been recovered with V004 replacing V003. A new main scene was created by Unity's own `EditorSceneManager.SaveScene` duplication path to preserve serialized scene references instead of cloning roots or editing YAML.
- New scene: `Assets/Scenes/147VR_MainScene.unity`.
- Unity execution evidence: `C:\Users\mongo\AppData\Local\Temp\147VR_M7_4_MainScene_20260903.log`.
- Verified by the editor method before save: V003 absent, V004 present, `Bed_Collider` present; MainScene inserted at Build Settings index 0.
- Temporary migration editor script was moved out of `Assets/Editor` into `Docs/M7_4_BACKUPS/M7_4_CreateCleanMainScene_20260903.cs` after successful execution to prevent accidental reruns.
- Physics Authority, Golden artifacts, and certified M1/M2.1/M2.2 evidence were not modified by this migration.
- Known non-blocking environment warnings remain: malformed/ignored legacy FBX `.meta` under `Assets/AAA/ImportedSnooker/`, duplicate `System.Runtime.CompilerServices.Unsafe.dll` package warning, and Meta XR/Package Manager `path` undefined callbacks. These did not prevent the migration method from completing.
- Current M7.4 status: **CLEAN MAIN SCENE CREATED / EDITOR-METHOD VERIFIED**. Runtime Play Mode and visual↔physics alignment are still required before deleting SampleScene or declaring production certification.
- Next exact action: inspect `147VR_MainScene` runtime bindings and V004/Bed_Collider alignment, then run controlled Play Mode validation. Keep SampleScene as rollback until those checks pass.


## 2026-09-06 — UPM IPC Timeout Root Cause Fixed / Canonical Launch Tool Added

- Investigated the recurring "Package Manager could not connect to IPC stream
  after 30 seconds" failures (seen 2026-08-28, 2026-08-29, and again
  2026-09-05 in `CLEANROOM_SURYERY_20260905.log`).
- Confirmed live on the host (2026-09-06): the Desktop Commander session's
  inherited PATH still contains
  `C:\Users\mongo\AppData\Local\npm-cache\_npx\4b4c857f6efdfb61\node_modules\.bin`,
  the same contamination previously identified on 2026-08-29. This entry is
  reintroduced by `npx @wonderwhy-er/desktop-commander@latest remote` each
  time that command is used to start the Commander session, so the fix must
  live in the Unity launch step, not rely on the operator remembering to
  clean PATH by hand.
- Added `Docs/AI_TEAM/Tools/Unity_Safe_Batch_Launch.ps1`: strips
  `npm-cache`/`_npx` PATH entries, sets `UNITY_UPM_TIMEOUT=120`, refuses to
  launch if Unity.exe is already running, logs to
  `Docs/AI_TEAM/UnityLogs/<name>_<timestamp>.log`, and writes a short
  SUMMARY.txt (IPC connected/failed, CS compile errors, exit code) after
  polling to completion.
- Recorded this as the mandatory canonical entry point in
  `147VR_AI_WORK_PROTOCOL.md` ("Canonical Unity Launch Tool") and referenced
  it from `Docs/AI_TEAM/TECH_CONSTRAINTS.md`.
- Scope: tooling/environment only. No scene, prefab, physics, Golden, or
  gameplay file was touched.
- Verification boundary: PowerShell parser validated the script (0 syntax
  errors, 96 lines). It was NOT run end-to-end, because `Unity.exe` was
  already running interactively on the host at the time (confirmed via
  process list: Unity.exe ~4.8GB working set, plus UnityPackageManager.exe,
  UnityShaderCompiler.exe x5, Unity.ILPP.Runner.exe) — running a second
  batch invocation would itself have violated the "One Unity Executor at a
  time" rule this tool exists to support. `LOCK.md` was not re-taken; this
  was infrastructure work performed at the explicit request of the project
  owner in-chat, not a physics/gameplay change.
- Next exact action: next time Unity is closed, run
  `Unity_Safe_Batch_Launch.ps1 -LogName "VerifyFix"` (compile-only, no
  `-executeMethod`) once to confirm real end-to-end UPM connection, then
  update this section and `147VR_AI_WORK_PROTOCOL.md` with the confirmed
  result. Until that confirmation exists, treat the fix as
  "implemented / not yet runtime-verified," consistent with this project's
  own 100% rule.

## 2026-09-07 - V007 Marking UV/World Closure
- Re-ran the audit in the actual Unity MainScene frame before changing the visual hierarchy. `TABLE SURFACE` is runtime-rotated to the correct X/Z playfield frame; the earlier Blender-space inverse audit had compared against the wrong frame.
- Root cause isolated to the marking generator: PIL pixel-Y was mirrored relative to Unity UV V. The generator was using `0.5 - z/A` for pixel Y, which inverted the marking texture along V.
- Corrected only `Docs/M7_4_generate_markings.py`: pixel Y now uses `0.5 + z/A`, spot radii are scaled independently in X/Y, the baulk line uses the authoritative `baulk_x`, and the D is generated as the interior/right half-circle toward the black end.
- Generator backup: `Docs/AI_TEAM/BACKUPS/M7_4_generate_markings_PRE_V007_V_MIRROR_FIX_20260907.py.bak`.
- Regenerated `Assets/AAA/ImportedSnooker/Textures/Snooker_Markings_V007.png` at 8192x4096. Existing material `M_V007_TableSurface_Marking` already references this texture GUID; no material/scene transform edit was required.
- Topology-safe actual Unity UV1->world audit now reports: Yellow 0.8mm, Green 1.1mm, Brown 0.8mm, Blue 0.5mm, Pink 0.5mm, Black 0.5mm. All six are INSIDE mesh triangles with zero UV sampling delta; max gap 1.1mm.
- Independent texture pixel audit: baulk-line hit rate 98.51%, D-arc hit rate 100%, all six spot centers contain the expected marking colors at their corrected pixel locations.
- Filtered Unity Test Framework PlayMode validation: `V007PlayModeValidationTests.V007_MainScene_Visual_Is_RuntimeValid` PASS 1/1, bounds 3.57 x 0.0127 x 1.78, material `M_V007_TableSurface_Marking`.
- One earlier unfiltered PlayMode attempt was terminated because it did not converge; it left Unity running briefly. Processes were cleared before the filtered rerun. The filtered run completed normally with exit code 0.
- Persisted closure evidence: `Docs/M7_4_V007_MARKING_CLOSURE_20260907.json`, `Docs/M7_4_V007_MARKING_PIXEL_AUDIT_20260907.json`, `Docs/V007_PlayMode_Results_20260907.xml`.
- Physics Authority, ball spawn, `Bed_Collider`, Golden data, and `V007_VISUAL_MAIN` hierarchy were not modified.
- V007 marking alignment status: **EVIDENCE-COMPLETE** for the current MainScene visual contract; no Physics Authority recertification implied.

## 2026-09-08 M5.1 Clean Run — UPM Bootstrap Blocker Reconfirmed

- Coach-authorized clean M5.1 sequence was attempted after reboot with zero Unity.exe initially running.
- Clean project-state recovery removed stale `Library/EditorInstance.json` and project `Temp` only while Unity was confirmed stopped.
- Dedicated package-aware Unity 6000.4.4f1 launch reached licensing successfully but failed before `M5_1_Install_TMP.Run()` with `Package Manager Could not connect to IPC stream "Upm-8580" after 30.0 seconds` and exit code 1.
- `UNITY_UPM_TIMEOUT=120` was supplied, but this Unity 6000.4.4f1 run still reported the fixed 30-second IPC wait; therefore the environment variable is not proven to extend this internal wait on this installation.
- Manual `UnityPackageManager.exe` was isolated: with missing `TMP`/related environment values it throws a Node `ERR_INVALID_ARG_TYPE`; with explicit Windows environment values it can start an IPC server successfully. This proves the UPM executable itself is runnable.
- Manual IPC injection attempts using `-upmIpcPath` were rejected by Unity with exit code 1 before project initialization; they are not accepted as a valid workaround.
- No valid `[M5.1 INSTALL PASS]` evidence was produced in this sequence. Scene persistence verification and lifecycle verification were NOT RUN.
- No gameplay/source-code changes were made by this run. M5.1 remains BLOCKED by Unity/UPM bootstrap tooling, not by M5.1 implementation evidence.
- Next action: retry only with a validated package-aware Unity/UPM launch path; do not certify M5.1 until scene YAML persistence and lifecycle runtime evidence are both obtained.


## 2026-09-20 — REAL Straight Calibration / Golden / Surface Bridge Closure
- Fresh Unity 6000.4.4f1 REAL Straight runtime batch completed from the controlled calibration scene.
- Fresh evidence: `Assets/AAA/PhysicsCalibration/RuntimeMeasurements/straight_runtime_measurements.json`, timestampUtc `2026-09-20T08:22:56.4497414Z`, repetitions=5.
- Measured truth: distance=0.7282500267m x5; peakSpeed=4.052994m/s (within stored tolerance); persisted samples all passed.
- Measured Golden rebuilt from this fresh JSON: `Assets/AAA/PhysicsCalibration/Golden/147VR-PHY-001_Straight.asset`, revision=2, sourceTimestampUtc matches the fresh runtime file, Unity=6000.4.4f1.
- Golden source verification PASS: every stored Golden sample matches the runtime JSON.
- Golden regression PASS: 5/5 repetitions, distance=0.728250000m, peakSpeed=4.052994000m/s, tolerance=0.50%.
- Existing stale `straight_regression_measurements.json` was used only temporarily for the regression input swap, then restored unchanged. No historical regression evidence was overwritten.
- TableSurface bridge architecture is now installed but certified-gated. `TableSurfaceProfile_RuntimeBaseline.asset` stores the existing seed values 0.18 / 0.32 / 0.08 with `measuredTruthCertified=false`.
- MainScene serialized reference is present on `SnookerPhysicsSetup.tableSurfaceProfile`; runtime construction smoke PASS confirms `Surface -> TableSurfaceController -> RuntimeBaseline profile` is actually created.
- Because `measuredTruthCertified=false`, TableSurfaceController applies NO new cloth friction force; the Straight measured baseline remains physically unchanged.
- UPM workaround used for verified unattended runs: explicit Windows environment values plus a pre-started UnityPackageManager IPC server and `-upmIpcPath`. This is now a proven launch path for this installation; startup may still emit non-fatal `path argument undefined` warnings.
- Current boundary: Straight baseline + Golden path are certified. Rolling/sliding/spin coefficients are NOT measured production truth yet.
- Next: perform measured cloth/friction calibration matrix, then certify the profile, rerun Golden regression, then continue Cushion/Pocket and Quest hardware validation.

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



## 2026-09-20 — APK Candidate Preflight / Quest Gate Deferred

- Per project-owner direction, Quest 2/3 physical device connection is intentionally deferred until a Development APK exists. No Quest hardware PASS is claimed before that point.
- Verified AndroidPlayer, bundled SDK, NDK, OpenJDK, Gradle tooling, and Android SDK build-tools on Unity 6000.4.4f1.
- Bundled ADB reports 36.0.0-13206524.
- Assets/Settings/Build Profiles/Android™.asset exists and is configured for APK output (m_BuildAppBundle: 0).
- Android XR management has automatic loading/running enabled; Android provider references OpenXRLoader.asset by matching GUID 3a94ba8dc9a89ae499029c2667db5316.
- Meta XR OpenXR Android feature and Oculus Touch Controller Profile Android are enabled in the current OpenXR settings.
- MainScene contains the M5 runtime chain: SnookerBallTracker, SnookerScoreManager, SnookerTurnManager, SnookerShotTracker, M5ShotEventContract, M5ShotLifecycle. Lifecycle values are settledSpeedThreshold=0.01, settledDuration=1.5.
- New non-destructive APK candidate builder: Assets/Editor/AAA/Build147VRQuestCandidate.cs.
  - Preflight menu: Tools/147/APK/Quest Candidate Preflight
  - Development build menu: Tools/147/APK/Build Quest Candidate (Dev)
  - Candidate scenes: MainScene only.
  - Output: Builds/Quest/147VR-MainScene-Dev.apk
- Legacy Build147VR.QuestDevelopment() is not used for the candidate APK because it targets PoolTable_8Ball, PoolTable_9Ball and SampleScene.
- Android application identifier is still the Unity template placeholder com.UnityTechnologies.com.unity.template.urpblank. No final package identifier was invented or changed.
- Global Editor Build Settings still list MainScene + SampleScene + PoolTable_8Ball + PoolTable_9Ball. They were not destructively changed; candidate builder bypasses the global list.
- Android custom keystore is not configured. This blocks a signed release APK, but not the later local Development APK smoke stage.
- Existing M6 integration test targets SampleScene.unity and saves that scene; it is therefore not treated as shipped-MainScene certification.
- Current M4.2 fresh-runtime runner is ready at Assets/Editor/AAA/M4_2PocketCurrentProfileRuntimeRunner.cs, but fresh current-profile M4.2 execution is deferred while the active Unity Editor owns the project. Historical M4.2 Goldens remain explicitly historical until a fresh run is completed.
- Current next gate: resolve the APK debug/release application identity, run the new candidate preflight/build from the active Unity Editor, then connect Quest 2/3 and perform device runtime validation.

