# 147VR — AI TEAM TECHNICAL CONSTRAINTS

## Authority
Physics has one authority. Visual systems must never become an alternate physics or gameplay authority.

## Reality Rule
REAL runtime -> MEASURE -> GOLDEN -> REGRESS -> CERTIFY. Never synthesize Truth, Golden values, PASS results, or measurements.

## Unity Rule
One Unity Executor at a time. `LOCK.md` is mandatory before any Unity-sensitive operation.

## Visual / Physics Boundary
Visual meshes, materials, decals, projectors, particles, lights, and cosmetic skins may not silently replace or modify physics colliders.

If a visual change requires collider, Rigidbody, PhysicMaterial, transform, layer, tag, or physics-setting changes, it is a shared/physics-sensitive change and requires LUNA review.

## VR Performance
Prefer GPU-friendly, instanced, batched, reusable assets. Avoid unnecessary real-time lights, transparent overdraw, per-frame allocations, and high-cost post effects.

Visual quality must be evaluated on the actual target hardware before production certification.

## Table Architecture
Table presentation should be data-driven where practical: table definition -> materials -> lighting profile -> cosmetic profile -> VFX profile.

A skin should not duplicate the physics table. Physics colliders remain stable unless a deliberate physics variant is being certified.

## Collaboration
Claude proposes visual work through `CLAUDE_STATUS.md` / `ART_BACKLOG.md`. LUNA validates technical boundaries before shared-zone execution.

## Evidence
Markdown is a handoff record, not proof. Actual Unity files, logs, runtime output, and device evidence remain authoritative.

## Current Priority
M2.1 Stun runtime evidence remains higher priority than cosmetic polish unless the active owner explicitly changes the execution order.

## Unity Launch Tooling (2026-09-06)
Recurring UPM IPC timeouts were traced to inherited npx PATH contamination.
`Docs/AI_TEAM/Tools/Unity_Safe_Batch_Launch.ps1` is now the required entry
point for any Unity Batchmode launch. See `147VR_AI_WORK_PROTOCOL.md` ->
"Canonical Unity Launch Tool" for full detail. Still subject to `LOCK.md`.
