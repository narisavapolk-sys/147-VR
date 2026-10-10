## Purpose
Single source for Unity execution ownership between LUNA and Claude.

## Hard Rule
ONE Unity Executor at a time. Multiple AI may reason/review, but only the holder may open Unity, edit project files, run AssetDatabase-sensitive operations, build, or execute runtime tests.

## Current Lock
- Holder: LUNA
- Status: ACTIVE
- Domain: M5.1 clean installation, persisted-scene verification, lifecycle verification
- Started: 2026-09-08
- Release condition: LUNA explicitly marks RELEASED

## Before Taking the Lock
1. Read this file.
2. Read `147VR_AI_WORK_PROTOCOL.md`.
3. Read `147VR_CURRENT_STATE.md`.
4. Confirm no Unity/Unity Hub execution is active that could conflict.
5. Write the intended task and ownership here.

## Shared-Zone Rule
Scene setup, prefabs, rendering settings, build settings, and other shared files require the lock even if the change appears cosmetic.

## Handoff
The current holder must record changed files, verification, blockers, and next action before releasing the lock.

## Emergency Rule
If the holder becomes unreachable, do not assume the lock is stale. Inspect actual processes and state, then record a takeover with evidence before editing.

## Takeover 2026-10-10 (compile-gate verification)
- Holder: QwenWork agent (acting on project owner Mon's explicit authorization to run the UPM/batch proof).
- Prior holder LUNA lock left ACTIVE from 2026-09-08; owner authorized takeover to continue after a machine freeze/reboot.
- Evidence at takeover (2026-10-10 ~06:40 Europe/Rome): `Get-Process` found NO `Unity` and NO `UnityPackageManager` process running; no `Library/EditorInstance.json` present. No conflicting live Unity session.
- Intended task: run `Docs\Tools\Unity_Batch_Safe.ps1` in COMPILE-GATE mode (no `-executeMethod`, no `-runTests`) to obtain real evidence that UPM IPC connects and Unity batch exits 0.
- Scope boundary: tooling/verification only. No Physics Authority, no M5 code, no scene/prefab edits. If the bare open triggers asset import, that is Unity's own refresh only.
- Release condition: QwenWork records the run result (UPM connect / exit code / compile signature) in this file and to the current-state doc, then marks RELEASED.

## Takeover outcome 2026-10-10 — RELEASED
- Compile-gate result: UPM IPC **CONNECTED in 0.7s** on 4f1 (license OK, 86 packages registered, no "Received undefined"). The historical "Could not connect to IPC after 30s" blocker is **resolved** via the correct launcher `Docs\Tools\Unity_Batch_Safe.ps1` (sanitized PATH + full env incl. ALLUSERSPROFILE). Launcher drift already consolidated (`Tools\Launch_147VR_Safe.ps1` is `.DEPRECATED`).
- The run did NOT reach a clean exit-0 verdict: the machine **hard-froze and was power-cycled twice** (owner). Root cause identified = **RAM exhaustion** (16GB, only ~4.5GB free at idle with Blender+Chrome+QwenWork open). NOT Defender (`C:\Users\mongo` already AV-excluded), NOT HDD (C: is a 512GB SSD), pagefile is system-managed.
- Owner decision this session: **canonical editor = Unity 6000.4.4f1** (pinned in ProjectVersion.txt). Stop opening with 12f1 — it wipes/rebuilds `Library/Bee` (full recompile) every switch.
- Owner-approved cleanup: 5 redundant `Library_*` backups + `%TMP%` + `New Unity Project` moved to **Recycle Bin** (not permanently deleted; empty bin to reclaim space).
- **Lock now RELEASED.** No Unity/UnityPackageManager running. Next Unity batch attempt: close Blender/Chrome first, single instance; expect a one-time full rebuild, then fast incremental opens.
