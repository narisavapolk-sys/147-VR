# 147 VR — M7.4 Scale & Headless Runbook

**Status:** CERTIFIED after real Unity runtime PASS
**Date:** 2026-09-04
**Unity:** 6000.4.4f1
**Project:** `C:\Users\mongo\UnityProjects\147 VR`

## Purpose

Standard procedure for diagnosing and fixing table visual scale/orientation defects without changing Physics Authority.
This runbook records the proven M7.4 failure mode, Unity 6 headless workflow, and Desktop Commander rules.

## 1. Golden Rule: Physics Authority Is Untouchable

The approved Physics table uses `Bed_Collider` as the authority for play-area dimensions.
Certified runtime target:
- WorldScale = `(1,1,1)`
- BoxCollider Size = `(1.778, 0.05, 3.569)`
- WorldBounds = `(1.778, 0.05, 3.569)`

Never compensate for a visual import problem by scaling the Physics root or collider.

## 2. Root Cause: 100x Visual Scale

The V007 visual hierarchy contained an internal visual scale problem.
Runtime inspection showed a visual hierarchy carrying 100x scaling, while the Physics collider remained at real-world Unity metres.
The result was a table visually displaced from the physics coordinate truth.

The critical lesson: inspect runtime `lossyScale` and `Renderer.bounds`; do not infer scale from characters or screenshots alone.
## 3. Proven Visual Fix Pattern

Do the fix at the visual hierarchy only.
For the M7.4 scene instance, the final corrective action was:
- preserve Physics root scale at `(1,1,1)`
- preserve `Bed_Collider` size
- align `TABLE SURFACE` to the Physics X/Z convention by applying the required Y-axis 90° orientation correction
- save the scene instance override

The final runtime result was:
- `TABLE SURFACE WorldBounds = (1.78, 0.01, 3.57)`
- `Bed_Collider WorldBounds = (1.78, 0.05, 3.57)`
- Physics WorldScale gate = `True`
- Physics Collider Size gate = `True`
- Visual Surface Size gate = `True`

## 4. Unity 6 Two-Step Headless Workflow

Use two separate Unity invocations against the same project; never run them concurrently.

**Step A — import/compile:**
- `-batchmode -nographics -silent-crashes`
- explicit `-projectPath`
- `-logFile compile_step.log`
- `-quit`
- require successful completion before Step B

**Step B — execute audit/fix:**
- same Unity executable and project path
- `-executeMethod M7_4_OneShotFixAndAudit.Run`
- dedicated audit log
- wait for the explicit PASS marker

Do not use `-noUpm`. Unity 6 package initialization is required for this project.
## 5. Desktop Commander Environment Rule

Desktop Commander can strip or reinterpret `$env:` when PowerShell is sent inline.
Therefore:
1. put full literal Unity and Project paths inside `Tools\M7_4_TwoStepRun.ps1`
2. set required environment variables inside that script
3. invoke the script with `powershell.exe -NoProfile -ExecutionPolicy Bypass -File "...\Tools\M7_4_TwoStepRun.ps1"`

Known required environment values for this machine:
- `PROGRAMDATA=C:\ProgramData`
- `APPDATA=C:\Users\mongo\AppData\Roaming`
- `LOCALAPPDATA=C:\Users\mongo\AppData\Local`
- `USERPROFILE=C:\Users\mongo`
- `TEMP` and `TMP=C:\Users\mongo\AppData\Local\Temp`
- `NO_PROXY=localhost,127.0.0.1`
- `UNITY_UPM_TIMEOUT=120`

## 6. Process/Lock Safety

A frequent false failure was:
`It looks like another Unity instance is running with this project open.`

Before a headless run:
- ensure no interactive Unity instance has the project open
- ensure no stale M7.4 runner is still spawning Unity
- ensure only one batch Unity instance owns the project
- clear stale `Library` lock files only after confirming no legitimate Unity process is using the project

Never launch multiple retry loops concurrently. One controller owns the execution loop.
## 7. Failure Lessons

### A. `$env:` stripping
Inline Commander PowerShell is unsafe for environment-variable-heavy commands. Use a saved `.ps1` or `.bat`.

### B. Unity instance collision
Repeated retries can create a self-inflicted lock. Stop the controller before manual diagnostics and verify the Unity command line before killing a process.

### C. Imported/model prefab editing
`PrefabUtility.SaveAsPrefabAsset` is not a reliable path for overwriting imported model-prefab content. Prefer a scene-instance override when the defect is integration-specific, or generate a new source asset when the asset itself must change.

### D. Axis convention
Blender V007 uses a world XY table plane; Unity Physics uses XZ. A correct metric size can still fail the runtime gate if the axes are rotated incorrectly.

### E. Evidence discipline
A M7.4 PASS is valid only when the actual runtime log contains:
`>>> [M7.4 ONE-SHOT PASS] <<<`
and all three gate booleans are true.
Do not close the milestone from compile success alone.

## 8. Checkpoint

M7.4 PASS checkpoint created after the real runtime PASS:
`Backups\20260904_2239_M74_PASS`

Checkpoint contents:
- `147VR_MainScene.unity`
- `Prefab_WPBSA_12Foot_Snooker.prefab`
- `M7_4_OneShotFixAndAudit.cs`

## 9. Operational Checklist

1. Stop interactive/duplicate Unity processes.
2. Clear stale locks only when safe.
3. Run Step A import/compile with valid UPM environment.
4. Run exactly one Step B audit/fix controller.
5. Inspect the real audit log.
6. Require the explicit PASS marker and all gates.
7. Create the scene/prefab checkpoint.
8. Only then close M7.4.

**M7.4 Scale/Headless procedure: CERTIFIED.**
