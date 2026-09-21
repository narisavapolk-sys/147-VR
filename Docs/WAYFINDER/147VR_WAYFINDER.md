# 147 VR Wayfinder

## Mission
Durable architecture memory for 147 VR. Other Unity projects are reference material only.

## Hard Safety Rules
- Modify 147 VR only.
- CueWarpVrRebornEdition is read-only during this audit.
- Never move, rename, delete, or edit CueWarp files.
- Never add CueWarp namespaces or project dependencies to 147 VR merely for reuse.
- Reimplement/adapt patterns inside 147 VR.
- M3, M4.1, M4.2 and M5 remain locked/certified foundations.
- M6 Gameplay Authority remains the gameplay decision boundary.
- Preserve one physics authority through the existing CuePhysicsAdapter/M5 chain.

## Workflow
1. Inventory reference capabilities.
2. Inventory 147 VR capabilities.
3. Classify every capability KEEP / ADAPT / BUILD / REJECT.
4. Define ownership and event boundaries.
5. Implement only in 147 VR.
6. Compile and regression-test.
7. Record architectural decisions.

## Current Direction
M5 = certified physics/runtime truth foundation.
M6 = certified gameplay authority integration.
Next = environment cleanup, controller architecture, real VR gameplay loop, then vertical slice.

## Controller Principle
Raw XR hardware input should terminate at semantic 147 VR actions such as Move, Aim, GrabCue, OpenTablet, ResetFrame, CancelShot, Recenter, and Calibrate. Gameplay systems should consume semantic actions/events rather than read hardware directly.

## Reference Findings
CueWarp contains mature patterns for cue alignment, XR player input, comfort locomotion, eye dominance, player-height calibration, mechanical rest, haptics, aim visualization, tablet UI, player positioning, and gameplay presentation.

CueWarp's CueStickController is highly coupled to its own RCA physics, special abilities, inventory, audio, and UI. It is a design reference, not a drop-in implementation.

## Decision Log
- 2026-08-31: CueWarp audit approved as read-only reference.
- 2026-08-31: Pattern-level reuse approved; direct project mixing prohibited.
- 2026-08-31: Controller implementation deferred until capability gap analysis is documented.
## HANDOFF CHECKPOINT — 2026-08-31 11:52 CEST

### Certified / Locked — DO NOT REBUILD
- M1 Straight Physics: CERTIFIED
- M1.5 Infrastructure: CERTIFIED
- M2.1 Stun: CERTIFIED
- M2.2 Follow: CERTIFIED
- M2.3 Draw: CERTIFIED
- M2.4 English: CERTIFIED
- M3 Cushion: LOCKED
- M4.1 Ball-Ball: LOCKED
- M4.2 Pocket: CERTIFIED
- M5 Gameplay: CERTIFIED — real baseline + independent regression PASS
- M6 Gameplay Integration: CERTIFIED — 16/16 PASS

### M6 fixes already completed — DO NOT REPEAT
- SnookerShotTracker event binding to BallTracker via InitializeBindings()
- Cue off-table foul transaction/restore
- BallTracker.Refresh() original homePosition preservation
- Pocket crossing detection replacing simple Y-threshold pot detection
- M6 integration harness: shot ? pot ? rule ? score ? turn ? respot
- SnookerShotTracker added to gameplay scene with dependency bindings

### Current Workstream — CONTINUE HERE
Wayfinder / VR controller architecture.
Existing AAA/Input components:
- VR147SemanticInput.cs
- VR147InputRouter.cs
- VR147InputSystemSource.cs
- VR147DominantHand.cs
- VR147InteractionContext.cs

Current objective: connect semantic input to real cue interaction without bypassing M5/M6 authority.

### Next implementation frontier
1. Validate semantic input architecture/compile boundary.
2. Connect dominant-hand policy to cue interaction.
3. Build cue pose/alignment adapter inspired by CueWarp patterns, reimplemented locally.
4. Add calibration boundary.
5. Add Mechanical REST.
6. Add haptics.
7. Add comfort locomotion.
8. Add aim/contact visualization.
9. Exercise real VR gameplay loop.
10. Vertical Slice.

### Anti-regression rule
If a future agent sees a task involving M3/M4/M5/M6 foundations, assume it is already complete unless new evidence proves a regression. Do not redo certified work. First inspect this checkpoint and current git diff.

### CueWarp protection
CueWarpVrRebornEdition remains READ-ONLY reference. Copy patterns only; never modify/move/rename its files and never introduce its project dependencies into 147 VR.


## IRON RULE â€” HANDOFF / ANTI-REGRESSION

Every work session MUST finish by updating this file before continuing or handing work to another AI.

The update MUST record:
- what was already completed;
- what is locked/certified and MUST NOT be rebuilt;
- what changed in the current session;
- current verified state;
- exact next implementation frontier;
- blockers, if any;
- files touched in the current session.

A new AI must read this checkpoint FIRST. It must not restart old milestones from memory, prompts, or assumptions.

### Current handoff state
- M3 Cushion: LOCKED.
- M4.1 Ball-Ball: LOCKED.
- M4.2 Pocket: CERTIFIED.
- M5 Gameplay: CERTIFIED.
- M6 Gameplay Integration: CERTIFIED, 16/16 PASS.
- Current active workstream: Wayfinder / VR Controller Architecture.
- Current completed layer: semantic input foundation + router/source/context components.
- Current next frontier: validate/compile the input boundary, then connect dominant-hand policy to cue interaction.
- Do NOT return to M3/M4/M5/M6 implementation unless regression evidence exists.


## CHECKPOINT â€” 2026-08-31 / CONTROLLER WORKSTREAM

Verified current semantic input foundation files:
- VR147SemanticInput.cs â€” semantic action vocabulary + state contract.
- VR147InputRouter.cs â€” hardware-independent action publication.
- VR147InputSystemSource.cs â€” existing Input System bridge.
- VR147DominantHand.cs â€” single dominant-hand policy.
- VR147InteractionContext.cs â€” interaction dependency boundary.

Verified current 147 VR Player action asset includes Move, Look, Attack, Interact, Crouch, Jump, Previous, Next, Sprint; XR bindings include Primary2DAxis, PrimaryAction, and trigger. This means the semantic layer can reuse the existing Input System without replacing the project input asset.

Important current limitation: several semantic actions (CancelShot, OpenTablet, ResetFrame, Recenter, CalibrateHeight, CycleRest) do not yet have dedicated Player bindings in the inspected input asset. They remain semantic capabilities, not falsely claimed as implemented controller bindings.

SnookerCueController already consumes VR147InteractionContext/VR147InputRouter for semantic grab/strike path while retaining CuePhysicsAdapter as shot authority. Its legacy XR path still contains a right-hand controller fallback; this is the next refactor boundary for true handedness support.

### Next frontier
Replace the cue's hard-coded right-hand XR fallback with the 147 VR dominant-hand policy, without changing shot physics or M5/M6 contracts. Then add dedicated semantic bindings only where product requirements justify them.

## CHECKPOINT — 2026-08-31 / DOMINANT-HAND INTEGRATION

Completed this step:
- SnookerCueController XR device lookup now resolves through VR147DominantHand.
- Default remains Right for backward compatibility.
- Left/Right hand selection changes the XRNode used by cue pose/trigger input.
- CuePhysicsAdapter, M5ShotLifecycle, shot validation, and M6 gameplay authority were not changed.
- No CueWarp files were modified.

Verification:
- No literal replacement artifacts remain in SnookerCueController.cs.
- Existing semantic input foundation remains present.

Next frontier:
- Compile 147 VR and inspect any real compiler/runtime evidence.
- If clean, wire handedness changes to interaction bindings and then proceed to cue pose/alignment adapter.
- Do not rebuild certified M3/M4/M5/M6 systems.


## BLOCKER CHECKPOINT â€” 2026-08-31

### Current verified change
- Dominant-hand integration was applied to SnookerCueController XR node selection.
- Right hand remains the default for backward compatibility.
- CuePhysicsAdapter/M5/M6 authority was not changed.

### Validation blocker
- Unity 6000.4.4f1 batch validation reached Package Manager startup but then failed because UnityPackageManager IPC did not connect within the configured window on the first run.
- Retry with UNITY_UPM_TIMEOUT=120 successfully registered packages and reached ScriptCompilation.
- The retry log currently shows a duplicate System.Runtime.CompilerServices.Unsafe.dll warning from Unity packages, but no confirmed C# compiler error in the inspected output.
- Therefore: code compilation is NOT YET CERTIFIED; do not claim compile PASS until a complete Unity batch run exits cleanly.

### Do not redo
- Do not rebuild M3/M4/M5/M6.
- Do not revert the dominant-hand integration solely because compile validation is pending.
- Do not modify CueWarp.

### Next action
Resolve/observe the Unity batch validation environment, then obtain a definitive compile result. If compile is clean, continue to cue pose/alignment adapter.


## BLOCKER UPDATE â€” 2026-08-31

### Environment validation blocker
Unity 6000.4.4f1 batch validation currently exits with code 1 before project initialization completes.
- Normal UPM path: Package Manager IPC timeout at 30s.
- `-noUpm` retry: project path resolves correctly but Unity still exits immediately with code 1 and no compile diagnostics.
- No active Unity process was found during inspection.
- `Library/ArtifactDB-lock` and `Library/SourceAssetDB-lock` exist with timestamps from 2026-08-29; they may be stale, but automated deletion was blocked by safety controls and MUST NOT be forced through an unsafe workaround.

### Interpretation
This is an environment/Editor launch blocker, not evidence of a Wayfinder/Input code regression.
Do not rewrite or roll back the semantic input layer because of this blocker.
Next: resolve stale Unity lock / Editor launch state safely, then run compile validation before deeper integration.

## BLOCKER UPDATE — 2026-08-31

Unity 6000.4.4f1 batch validation was retried with UNITY_UPM_TIMEOUT=120. Unity still exits with code 1 after ~35s because Package Manager cannot connect to its IPC stream and reports failure to start UnityPackageManager.exe. This confirms the blocker is environment/UPM startup, not a newly observed C# compiler error.

Evidence:
- UnityPackageManager.exe exists at the expected Unity 6000.4.4f1 path.
- UPM diagnostic log shows previous UPM instances successfully started IPC, then shut down when their parent Unity process exited.
- No Defender threat detection was returned by the available query.
- No active Unity/UPM process was present during the inspection.

Decision:
- Do not delete Library, package caches, lock files, or reset git yet.
- Do not modify gameplay foundations to compensate for an environment blocker.
- Next environment action: controlled standalone UPM launch/IPC probe, then inspect its exit/log behavior before any destructive cleanup.
- Active product work remains Wayfinder controller architecture and is paused only at the validation gate.

## CONTROLLED UPM PROBE RESULT — 2026-08-31

Standalone UnityPackageManager.exe probe reproduced the failure independently of Unity project code.

Observed:
- UnityPackageManager.exe starts, then exits.
- Node runtime throws ERR_INVALID_ARG_TYPE: "The path argument must be of type string. Received undefined".
- Stack points to Package Manager `getLocalConfigFolder()` / `readConfig()` inside Unity 6000.4.4f1 PackageManager server.
- Therefore the earlier IPC timeout is a downstream symptom of UPM server startup failure.

This is now classified as an ENVIRONMENT / UPM CONFIGURATION BLOCKER, not a 147 VR C# blocker.

Safety decision:
- Do not alter M5/M6.
- Do not modify CueWarp.
- Do not delete Library or package cache blindly.
- Next diagnostic target: identify which expected environment/config path is undefined for the UPM server and restore it using the least-invasive project-independent fix.

## ENVIRONMENT PROGRESS — 2026-08-31

UPM investigation advanced:
- Root cause of the earlier standalone `ERR_INVALID_ARG_TYPE` was confirmed: `UnityPackageManager.exe` expects `PROGRAMDATA` on Windows.
- With the required Windows environment variables present, standalone UPM successfully started its IPC server (`Unity-Upm-Probe`). This clears the original UPM startup hypothesis.
- Unity batch validation was retried with the same environment. UPM is no longer the observed terminal failure.
- New terminal blocker: Unity exits with code 1 because `LicenseClient-mongo` IPC channel does not exist.

Classification:
UPM/IPC = DIAGNOSED / WORKING under corrected environment.
Unity licensing IPC = CURRENT ENVIRONMENT BLOCKER.

Safety:
No project source rollback, Library deletion, package-cache deletion, M5/M6 modification, or CueWarp modification performed.

Next:
Diagnose Unity Licensing Client availability/state. Only after Unity can start successfully will C# compilation and Wayfinder integration validation resume.

## IMPLEMENTATION UPDATE — 2026-08-31

### Completed this step
- Added local `VR147CueHandSource` under `Assets/Scripts/AAA/Input/`.
- Provides dominant-hand XR pose and smoothed linear velocity.
- Supports a local Transform fallback for editor/test environments.
- Contains no gameplay rules and no Rigidbody/physics authority.

### Reference-derived design decision
CueWarp inspection confirmed useful concepts in `VRCueBridge` and `CueAlignmentSystem`: controller pose drives cue presentation and hand motion can expose velocity. These patterns are being reimplemented locally, not copied as project dependencies.

### Safety
- CueWarp files remain untouched.
- M5/M6 foundations untouched.
- No physics authority moved into input layer.

### Next
Wire `VR147CueHandSource` into the cue pose/aim adapter, then validate compilation and inspect runtime bindings.


## LONG-RUN CHECKPOINT â€” 2026-08-31

### Validation finding
- Unity 6000.4.4f1 successfully reached script compilation/domain reload for 147 VR.
- No new `error CS` compiler output was observed in the validation log.
- Existing environment noise remains: many `Library/ScriptAssemblies/*.dll not valid` test/editor assemblies and duplicate `System.Runtime.CompilerServices.Unsafe.dll` versions.
- Package Manager continues to emit `path argument must be of type string. Received undefined`.
- A previous batch process remained alive because Meta XR editor async work continued after the requested quit; it must not be mistaken for a compile blocker.

### Wayfinder implementation
- Added `VR147CueHandSource.cs` as a hardware-facing, gameplay-agnostic XR pose/velocity source.
- CueWarp reference inspection: `VRCueBridge` and `CueAlignmentSystem` confirm useful patterns for pose following, grip alignment, and controller velocity. These are being reimplemented locally.

### Current boundary
`VR147CueHandSource` -> future cue pose/aim adapter -> `SnookerCueController` -> existing `CuePhysicsAdapter` -> M5/M6.

### Do not regress
M3/M4/M5/M6 remain locked/certified. CueWarp remains read-only reference.

## SESSION CHECKPOINT — 2026-08-31
- User authorized YOLO long run.
- Active task: UPM environment diagnosis -> validation -> resume Wayfinder.
- Time-budget rule: >5 min without new evidence = strategy change; >10 min = escalate.
- Do not rebuild certified M3/M4/M5/M6.


## UPM INVESTIGATION UPDATE — 2026-08-31

New evidence: UPM IPC server itself starts successfully. The failure occurs when project package requests are handled: `project:list-packages` and `packages:get-all-packageinfo` return HTTP 500 repeatedly. This narrows the blocker from IPC startup to project/package configuration processing.

Important: no destructive repair performed. M3/M4/M5/M6 remain protected. Wayfinder implementation remains intact.

Next diagnostic target: inspect Packages/manifest.json, packages-lock.json, and UPM project metadata for malformed/missing package configuration, without deleting or regenerating files blindly.

- 2026-08-31 validation: fixed VR147CueHandSource XRDevice/InputDevice type mismatch; remaining definite compile issue was out-variable definite-assignment (position/rotation), now initialized explicitly. No certified gameplay files touched.

- 2026-08-31 validation: compile issue cleared past source edits; current blocker is again UPM IPC stream "Upm-12064" timeout at exactly 30.0s. Unity license IPC connects successfully. UPM server executable exists. Defender exclusion query from current DC shell returned N/A (admin visibility issue), so no assumption made. Next: controlled Defender/process-level probe; no destructive changes.

- UPM probe: manifest.json and packages-lock.json are valid JSON; all 53 direct dependencies exist in 86 lock entries; embedded Meta XR package manifest valid. project:list-packages returns HTTP 500 in 4ms, so not a network timeout at request layer. Tested creating user .upmconfig.toml; it did not improve and was removed. No project package files modified.
- 2026-08-31 guard validation found literal PowerShell escape text accidentally persisted in VR147CueHandSource.cs line 37; corrected immediately. This was introduced by automation, not a gameplay design change. Revalidation required.
- 2026-08-31 stepwise recovery: UPM environment guard applied to process launch; VR147CueHandSource source repaired via atomic temp-file swap after file lock cleared. Unity batchmode validation now exits code 0 in 36.7s with no compile errors, no HTTP 500/undefined-path signature in filtered log. Guard/recovery contract remains documented.

- 2026-08-31 M7 long-run checkpoint: Input layer inventory confirmed: VR147CueHandSource, VR147InputSystemSource, VR147InputRouter, VR147SemanticInput, VR147InteractionContext, VR147DominantHand all present. Existing InputAction asset contains XR bindings. No CueWarp files imported. Next gate: verify semantic adapter mapping against actual asset actions, then compile/test before gameplay wiring.

- 2026-08-31 M7 checkpoint: semantic XR cue path advanced. SnookerCueController now prefers VR147CueHandSource pose and semantic GrabCue hold/release; direct XR trigger remains fallback only when semantic router is unavailable. CuePhysicsAdapter remains sole shot authority. Unity batch validation EXIT 0; no CS errors, HTTP 500, or undefined-path signature. Next gate: focused M7 input/runtime binding tests.
- 2026-08-31 M7.4 progress: VR147InputSystemSource hardened to resolve semantic action names with safe aliases (GrabCue/Interact, Strike/Attack, Aim/Look) and optional future actions instead of throwing when actions are absent. Existing asset remains unchanged; no CueWarp files copied. UPM restored 86 packages from cache in 0.02s during validation; no HTTP 500/undefined-path evidence. Next: establish explicit semantic XR bindings only where evidence supports them, then wire cue interaction.
- M7.2 evidence gate rerun 2026-09-01: VR147M7InputAudit.Run() => [M7 INPUT AUDIT PASS] XR Attack/Interact bindings verified; Unity batchmode exited return code 0. Existing InputAction asset remains unchanged.
- 2026-09-01 M7.4 continuation: audited semantic input implementation. VR147CueHandSource is hardware-facing pose/velocity only; VR147InputSystemSource resolves existing Player actions via safe aliases; VR147InputRouter publishes edge actions while state remains semantic. No direct CueWarp import. Scene-wide recursive audit was too slow and produced no evidence, so it was abandoned rather than touching project blindly. Next: targeted scene/prefab discovery using filenames/components, then focused runtime wiring test.
