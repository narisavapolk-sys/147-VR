# M7.4 — TABLE VISUAL EXECUTION PLAN

Status: PLANNED / NOT YET EXECUTED
Project: 147 VR
Purpose: Build the first AAA-quality snooker table visual while preserving locked physics authority.

## CORE DECISION
Use a HYBRID pipeline, not a full rebuild from zero.
Reference images are the visual/design authority.
Existing `Assets/BlenderTest/SnookerTable_Hi3D.fbx` is the starting visual mesh.
Blender is the authoring/refinement stage.
Unity is the runtime integration stage.
Existing physics remains the gameplay authority.

## SOURCE MATERIAL
Reference folder:
`C:\Users\mongo\OneDrive\Desktop\TEST ART AAA\`

Known reference set: Xing Pai Aristocrat / tournament-style snooker table images.
Known Unity starting mesh: `Assets/BlenderTest/SnookerTable_Hi3D.fbx`

## NON-NEGOTIABLE RULE
CORRECT -> REALISTIC -> BEAUTIFUL

If visual/art work conflicts with locked physics:
STOP -> isolate the visual layer -> preserve physics authority -> resolve deliberately.
Never silently alter the certified physics foundation to fit artwork.

## EXECUTION PHASES

### 1 — AUDIT + REFERENCE
Inspect all supplied references and the Hi3D FBX.
Identify silhouette, proportions, rail profile, cushion profile, pocket construction, legs, hardware and material language.
Do not modify physics.
Deliverable: visual audit + target specification.

### 2 — SCALE / PROPORTIONS
Bring the visual mesh into the project's real-world metric scale.
Match playing-surface dimensions and important heights to the existing physics/table authority.
Deliverable: correctly scaled visual master.

### 3 — 22-BALL VISUAL LAYOUT
Validate the playing surface and create/align the standard 22-ball visual arrangement.
Use existing physics ball positions as authority where applicable.
Deliverable: visually aligned ball/table coordinate space.

### 4 — CUSHION + POCKET GEOMETRY
Refine cushion faces, rail transitions, pocket mouths, pocket throats and leather/net details.
Critical gameplay-facing geometry must be explicitly aligned with physics geometry.
Deliverable: table geometry that looks correct and does not contradict collision behavior.

### 5 — MATERIALS
Build production material separation for cloth, wood, rubber/cushion, leather, metal/brass and other trim.
Prioritize believable roughness/specular response over excessive texture complexity.
Deliverable: AAA-ready material pass.

### 6 — PHYSICS ↔ VISUAL ALIGNMENT
Overlay/check visual and physics anchors.
Validate ball resting plane, cushion height/face, pocket mouth and table bounds.
Run existing physics regression; art changes must not invalidate certified results.
Deliverable: alignment evidence.

### 7 — AAA POLISH
Lighting/readability, bevel quality, micro-detail, material polish, presentation and VR performance optimization.
This phase is deliberately late.

### 8 — CUE / HAND / REST INTEGRATION
Only after table authority is stable.
Validate cue clearance, hand interaction, rest placement and VR ergonomics against the finished table.

## ASSET FLOW
REF images -> Hi3D reconstruction/starting mesh -> Blender cleanup/refinement -> exported visual asset -> Unity integration -> alignment validation.

Hi3D output is NOT physics authority. AI-generated geometry is treated as a visual starting point and must be cleaned/validated.

## UNITY STRUCTURE TARGET
Table Visual and Table Physics remain separable layers.
Critical anchors should be identifiable and testable rather than hidden inside arbitrary mesh geometry.

## BACKUP / SAFETY
Never overwrite the original source asset while authoring the new table.
Create a versioned working asset before destructive Blender operations.
Do not modify CUEWARP or other source projects.

## USER OPERATION REQUIREMENT
The user does not need to know Blender modeling.
The workflow should be executed through the available tooling as much as possible, with the user only performing unavoidable UI/authentication actions.

## 2026-09-01 — Phase 1 Resume After Unexpected Shutdown

- Computer recovery was handled as a continuation; no assumption was made that the previous session completed Phase 1.
- Verified the reference folder still contains 11 Xing Pai / tournament-style table reference images.
- Verified the Unity starting asset exists and is intact: `Assets/BlenderTest/SnookerTable_Hi3D.fbx`, 89,947,920 bytes, modified 2026-09-01 13:49:37.
- Verified the Unity importer metadata: `globalScale=1`, `useFileUnits=1`, `addColliders=0`, `preserveHierarchy=0`, `weldVertices=1`, `meshCompression=0`.
- Visual reference audit can already establish the target design language: dark reddish-brown wood, green cloth, light/cream pocket leather, gold/brass trim, ornate turned legs, six pockets, and tournament-style proportions.
- A temporary Unity asset-inspection probe was prepared, but package-aware Unity startup is currently blocked by the same UPM IPC failure: `Upm-11716` could not connect after 30 seconds. Therefore mesh-level vertex/bounds/material-count inspection has NOT been claimed.
- The temporary probe file was removed after the failed attempt; no asset or physics authority was modified.
- Phase 1 status: **IN PROGRESS / UPM BLOCKER FOR DEEP FBX INSPECTION**.
- Next exact action: recover package-aware Unity/UPM IPC, then run the non-destructive FBX inspection probe and capture real mesh bounds, renderer/material counts, hierarchy and component data. Only after that decide whether the Hi3D mesh is clean enough to refine in Blender or requires partial rebuild.

## 2026-09-02 — Blender MCP Diagnostic Update

- UPM/Unity package-aware startup is now VERIFIED PASS; the previous UPM blocker is closed.
- The actual Hi3D FBX was independently audited in Blender 5.2.0 LTS.
- FBX is one mesh object with approximately 982,096 vertices and 1,966,736 faces, only 2 material datablocks, and no logical table-part hierarchy.
- Source mesh dimensions after imported transform are approximately 0.04463 × 0.02387 × 0.08000 m; imported object scale is approximately 0.01 and rotation approximately +90° X.
- Decision: treat Hi3D FBX as SOURCE/REFERENCE, not final runtime visual geometry.
- Blender MCP add-on was found at Blender 5.2 user extension path, manifest version 1.0.0, and port 9876 is confirmed LISTEN while Blender PID 9896 is running.
- MCP protocol was verified from the installed source: JSON + NUL framing; request type `execute`; request includes `code` and `strict_json`.
- Direct Commander TCP execute probe connects but receives no response within 5 seconds. Therefore LISTEN alone is NOT considered MCP execution health.
- Background Blender `--command blender_mcp` is explicitly rejected by the installed add-on because interactive GUI/main-loop execution is required.
- Full diagnostic recorded in `Docs/147VR_BLENDER_MCP_DIAGNOSTIC_20260902.md`.
- No Physics Authority, source FBX, CUEWARP, or other project was modified.
- Next action: diagnose the GUI timer/polling path or use a controlled Blender-native authoring route, then create a versioned Working Copy before structural cleanup.
