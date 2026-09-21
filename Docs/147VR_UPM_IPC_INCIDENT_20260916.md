# 147 VR — Unity Package Manager IPC Incident Record

**Incident date:** 2026-09-16  
**Unity:** 6000.4.4f1 (360f97ecca93)  
**Project:** `C:\Users\mongo\UnityProjects\147 VR`  
**Status:** INFRASTRUCTURE BLOCKED / INCONCLUSIVE  
**Rules status:** NOT REACHED  

## Executive Summary

A Phase A targeted EditMode test run for `M5_3_RulesUnitTests` could not start because Unity failed to establish its local Package Manager IPC connection.

This is **not evidence of an M5.3 Rules failure**. Compilation and the 43-test gate were never reached.

The investigation established that `UnityPackageManager.exe` itself launches successfully and eventually creates the requested IPC server. The critical observed problem is that IPC server startup took approximately 218 seconds, while Unity abandoned the connection after its internal 30-second IPC timeout.

The UPM log then records shutdown because the Unity parent process had already exited.

## Verified Classification

- V6 source review: PASS
- Environment sanitation: PASS
- UPM executable identity: PASS
- UPM process spawn: PASS
- UPM IPC server startup: PASS
- Unity ↔ UPM IPC handshake: FAIL
- Rules compile: NOT REACHED
- Targeted 43-test gate: NOT REACHED
- M5.3 Phase A certification: NOT GRANTED

## Do Not Misdiagnose

Do not classify this incident as a Rules, NUnit, M5.3, scene, or scoring failure.
Do not patch Rules source to solve this incident.
Do not use synthetic tests or other test classes as a substitute for the blocked Phase A gate.
## Evidence — 2026-09-16 Safe Run

Original Unity log: `C:\Temp\M5_3_PHASE_A_TEST\Unity.log`  
Safe-run Unity log: `C:\Temp\M5_3_PHASE_A_TEST_SAFE\Unity.log`  
Diagnostic folder: `C:\Temp\M5_3_UPM_IPC_DIAG_20260916\`

Observed Unity-side failure:

`Could not connect to IPC stream "Upm-10328" after 30.0 seconds.`

UPM log evidence:

```text
[2026-09-16T10:10:44.003Z][INFO] Command-line: 'C:\\Program Files\\Unity\\Hub\\Editor\\6000.4.4f1\\Editor\\Data\\Resources\\PackageManager\\Server\\UnityPackageManager.exe' server -s 10328 --ipc-path Unity-Upm-10328 -l 2
[2026-09-16T10:10:44.005Z][INFO] Detected environment variables:
  NO_PROXY=localhost,127.0.0.1
[2026-09-16T10:14:22.446Z][INFO] IPC server started (IPC path=Unity-Upm-10328).
[2026-09-16T10:14:22.446Z][INFO] Hint: To connect to this UPM instance, launch Unity with the command-line parameter -upmIpcPath Upm-10328.
[2026-09-16T10:14:29.049Z][INFO] Shutting down UnityPackageManager.exe: parent process [10328] is no longer running.
```

Timeline: UPM launch at 10:10:44 → IPC server ready at 10:14:22 (~218 seconds) → shutdown at 10:14:29.

Interpretation: UPM did not show a startup crash in this evidence. It eventually created the IPC endpoint, but too late for Unity's 30-second connection window. Unity's parent process then exited, causing UPM to shut down.
## Environment Evidence

Sanitized environment used before the safe retry:

- `USERPROFILE=C:\Users\mongo`
- `LOCALAPPDATA=C:\Users\mongo\AppData\Local`
- `TEMP=C:\Users\mongo\AppData\Local\Temp`
- `TMP=C:\Users\mongo\AppData\Local\Temp`
- `PROGRAMDATA=C:\ProgramData`
- `ALLUSERSPROFILE=C:\ProgramData`
- `NO_PROXY=localhost,127.0.0.1`
- PATH entries matching `_npx` / `npm-cache` were removed.
- Stale `Library\EditorInstance.json` was removed only after confirming Unity.exe count was zero.

TEMP write/delete probe: PASS.  
C: used: 464,705,675,264 bytes.  
C: free: 46,336,708,608 bytes.

The environment sanitation therefore did not resolve the delayed IPC startup.

## UPM Binary Identity

Expected Unity 6000.4.4f1 UPM path:
`C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Data\Resources\PackageManager\Server\UnityPackageManager.exe`

Observed size: 64,349,104 bytes.  
Observed SHA-256: `8F9C6D223CE5FC5154BECD8B58312BBD76B36C0618F4FC721119D0CC5BF9D14E`

## Earlier Known UPM Failure Pattern

A separate earlier incident established that inherited Commander `PATH` contamination containing npm `_npx` / `npm-cache` paths can break UPM child startup. The project already documents the clean-environment recovery pattern.

That historical cause must not automatically be assigned to this 2026-09-16 incident: the current sanitized run still reproduced the IPC failure, and the current `upm.log` shows a delayed successful IPC server start.
## Recovery / Prevention Protocol

Before any future Unity batch test:

1. Confirm `Unity.exe` count is zero before removing stale `Library\EditorInstance.json`.
2. Do not use `-noUpm` for package-aware tests.
3. Launch with the known-safe sanitized environment and `NO_PROXY=localhost,127.0.0.1`.
4. Remove inherited `_npx` / `npm-cache` PATH contamination.
5. Capture Unity and UPM process identity before changing or terminating anything.
6. Capture `Editor.log` and `upm.log` immediately after a failed launch.
7. If Unity reports a 30-second UPM IPC timeout, inspect `upm.log` before assuming UPM crashed.
8. Check whether the UPM IPC server actually starts, how long startup takes, and whether the endpoint appears/disappears.
9. Preserve the exact Unity command line and environment snapshot.
10. Check Windows security/event evidence before changing Defender/AV settings.
11. Never delete `Library`, `Library\PackageCache`, `Packages`, or `ProjectSettings` as a first response.
12. Do not reinstall Unity as a first response.
13. Do not modify M5 Rules source to compensate for infrastructure failure.
14. Do not declare M5.3 Phase A PASS unless the authoritative `M5_3_RulesUnitTests` gate reaches 43/43.

## Current Hard Stop

The current evidence identifies a **delayed UPM IPC server startup** but does not yet identify why startup takes ~218 seconds.

Therefore no root-cause fix is certified yet.

Required next investigation: Windows/Event Viewer and security evidence around the UPM launch, plus IPC endpoint behavior while UPM is alive. The first causal error from UPM/Windows evidence must be recorded before changing system configuration.

## Protected Project Rules

During this incident investigation, do not modify:

- `Assets/Scripts/Quest/Rules/M5RulesTypes.cs`
- `Assets/Scripts/Quest/Rules/M5SnookerRulesEngine.cs`
- `Assets/Editor/M5_3_RulesUnitTests.cs`
- `Packages/`
- `ProjectSettings/`
- `Library/`
- `Assets/Scenes/147VR_MainScene.unity`

Do not stage or commit incident-diagnostic changes until the user reviews them.
