# 147 VR — Table Visual Audit — Phase 1

Date: 2026-09-01
Status: AUDIT COMPLETE — REBUILD/CLEANUP DECISION READY
Scope: Visual/Art only. Physics Authority untouched.

## 1. Preflight

- Unity 6000.4.4f1 + UPM integration: PASS.
- Exact child-process environment regression: PASS.
- UPM IPC connected; resolved package state restored; 86 packages registered.
- No `Received undefined` error in the authoritative regression.
- Existing fix documented in `Docs/147VR_UPM_CHILD_PROCESS_INHERITANCE_FIX.md`.

## 2. Source Asset

Path: `Assets/BlenderTest/SnookerTable_Hi3D.fbx`
File size: 89,947,920 bytes (~85.8 MiB).
FBX version reported by Blender: 7500.
Imported with Blender 5.2.0 LTS in background mode; no GUI session left open.

## 3. Hierarchy Audit

- Objects: 1
- Mesh objects: 1
- Non-mesh objects: 0
- Parent hierarchy: none
- Imported object name: `meshes[0]`

Conclusion: the asset is effectively a single monolithic mesh. It does not expose useful table-part hierarchy for targeted art edits.
## 4. Geometry Audit

Imported mesh statistics:
- Vertices: 982,096
- Faces: 1,966,736
- Mesh datablocks: 1

Imported object transform:
- Location: (0, 0, 0)
- Rotation X: ~90 degrees
- Scale: ~0.01 on all axes
- Blender scene units: Metric, scale length 1.0

World-space dimensions after FBX import:
- X: 0.04462859 m
- Y: 0.02387235 m
- Z: 0.08000000 m

Local bounding-box extents before object scale/rotation are approximately:
- X: 4.46286 units
- Y: 2.38724 units
- Z: 8.00000 units

Conclusion: the geometry carries a significant transform/unit ambiguity. The aspect ratio is plausible for a long table-like asset, but the imported world size is clearly not production table scale. Do not use this file as direct gameplay-scale visual authority until scale is reconciled against the existing 147 VR table/physics dimensions.

## 5. Materials / Renderer

- Blender material datablocks: 2 (`Material`, `pbr_material`).
- Active mesh material slot: 1 (`pbr_material`).
- The second material datablock is not assigned to the mesh slot.
- Mesh is monolithic, so material/renderer separation is not yet production-ready.

Conclusion: material setup is not ready for AAA visual production as-is. The mesh should be separated into logical visual components before final material authoring.
## 6. Decision

DECISION: Do NOT attempt to polish this FBX directly inside Unity.

Recommended path:
1. Preserve this FBX as the untouched source/reference asset.
2. Send a working copy to Blender for structural cleanup/rebuild.
3. Establish correct real-world table scale against the existing visual/physics reference.
4. Separate logical components: table body/wood, rails, cushions/rubber, cloth, pocket rims/liners, pocket interiors, and justified metal/details.
5. Build clean material slots and renderer boundaries.
6. Keep gameplay/Physics Authority geometry and transforms out of this art pass.
7. Reconcile visual ↔ physics alignment only at the explicit Phase 1.6 boundary after visual structure is stable.

## 7. Physics Safety

No Physics Authority asset, collider, calibration scene, Golden value, or physics script was modified by this audit.

Next work item: Blender-side visual cleanup/rebuild on a copy of the source asset, not modification of the physics-authoritative table.
