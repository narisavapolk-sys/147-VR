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
