## Purpose
Single source for Unity execution ownership between LUNA and Claude.

## Hard Rule
ONE Unity Executor at a time. Multiple AI may reason/review, but only the holder may open Unity, edit project files, run AssetDatabase-sensitive operations, build, or execute runtime tests.

## Current Lock
- Holder: Claude (GLM agent) — takeover per Emergency Rule
- Status: ACTIVE
- Domain: Round 11 — TableKey shadows off, UV2 ensure, GI static flags, Phase 1-3 PlayMode tests, baked-GI prep tooling
- Started: 2026-10-31
- Release condition: record results in CURRENT_STATE/PROJECT_MEMORY + final report to owner

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
