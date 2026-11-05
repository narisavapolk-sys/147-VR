## Purpose
Single source for Unity execution ownership between LUNA and Claude.

## Hard Rule
ONE Unity Executor at a time. Multiple AI may reason/review, but only the holder may open Unity, edit project files, run AssetDatabase-sensitive operations, build, or execute runtime tests.

## Current Lock
- Holder: OpenCode (GLM agent) — release performed with owner authorization ("cleanup and finish S2 #2c job")
- Status: RELEASED (handoff complete 2026-11-05)
- Domain: 2026-11-05 - S2 #2c PoolUndoStack QA + SoloPractice game-mode gate + BHP UNDO controller bootstrap + PlayMode verification
- Started: 2026-11-05 08:20 CET by ChatGPT session (owner-authorized)
- Released: 2026-11-05 by OpenCode (GLM) completing the holder's final release step; the holder became unreachable mid-sprint (process evidence: no Unity.exe/UnityHub/executor processes active at takeover inspection ~13:30 CET; the sprint's Unity runs had already finished at 09:17 CET).
- Release condition MET: package-aware PlayMode validation passed (5/5, see UnityLogs), exact results recorded in CURRENT_STATE/PROJECT_MEMORY/WireReports, and the entire sprint-scoped file set was committed as one unit before release: commit d9a63d6 "S2 #2c: Solo Practice UNDO + runtime integration" (40 files, includes the missing Unity .meta GUID files + headless undo test suite + all S2_2c evidence logs).
- Next action after this release: consolidate the two working copies — commit canonical `147 VR` cleanup, merge `feat/unity-event-physics-import` into the canonical branch, verify in Unity on canonical, then owner decides on pushing both branches to origin.

## Emergency Takeover Record — 2026-11-04
- Previous holder Claude last ACTIVE 2026-10-31 (Round 11/12 complete per logs; no blocker reported).
- Evidence of safe takeover: Get-Process shows NO Unity.exe and NO blender.exe; RAM free 7.9/15.4 GB; Library warm (last batch 2026-10-31 exit 0).
- S1 commits landed before this task: 6786490 (PoolPhysics S0), bec1455 (Quest gameplay), 43cb706 (AAA/lighting tools), e5a3db4 (state sync) — trunk clean except intentionally skipped Assets/_Recovery + _staging_old.

## Handoff Record (release) — 2026-11-04
- Changed files: Assets/PoolPhysics/Sim/TableSpecData.cs (S0 1.75x fix, in commit 6786490), PoolPhysics/Tests/GoldenRunner.cs (T3b/T3c/T3d regressions), Docs + WireReports updated this session, Unity_WIP_Batch_Launch.ps1 queued-cmd visibility patch.
- Verification: headless PoolSim.Tests green 10/10 (jartest incl. new regressions; props 1000 fuzz clean; golden round-trip 0.000E+000) AND Unity PlayMode 5/5 PASS (results PlayModePhaseTests_20261104_20261104_075912) — no blockers.
- Next action: S2 sprint (9-ball real rules, headless-first) per CURRENT_STATE 2026-11-04; next Unity touch keeps this handoff contract.

## Emergency Takeover Record — 2026-10-31
- Previous holder LUNA last ACTIVE 2026-09-08 (unreachable; lock stale per Emergency Rule).
- Evidence of safe takeover: tasklist shows NO Unity.exe / UnityPackageManager.exe / AssetImportWorker0.exe;
  grep found no Temp/UnityLockfile. Unity fully closed before any launch.
- Inputs verified before edit: 147VR_WORKING_DIRECTIVE.md, 147VR_AI_WORK_PROTOCOL.md, 147VR_CURRENT_STATE.md, this file.

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
