# 147 VR — TABLE AUTHORITY CONTRACT

**Status:** PERMANENT ARCHITECTURE CONTRACT  
**Effective:** 2026-09-03  
**Project:** 147 VR / Unity 6000.4.4f1

## 1. Single Visual Authority

- `147VR_Table_WPBSA_Visual_Clean_v006` is the **single and permanent Visual Authority**.
- Do not use, promote, regenerate, or create another table visual version as a replacement.
- Legacy visual versions are reference/archive only.
- V006 is presentation geometry; it is not Physics Authority.

## 2. Physics Authority Lock — ZERO TOLERANCE

The existing certified physics geometry is authoritative and must not be edited to fit visual geometry.

**Never move, resize, rotate, replace, regenerate, or retune:**
- `Bed_Collider`
- `Cushion_Colliders`
- existing Pocket Colliders / pocket physics
- certified Physics Authority root and its collider measurements

If visual and physics do not align, **move/adjust only the Visual Mesh**.

Do not modify Golden cases, calibration truth, physics scripts, shot lifecycle, scoring, or turn logic as part of visual integration.

## 3. No Visual Colliders

- V006 visual assets must contain **no Collider components**.
- Visual geometry must live under `Visual_Meshes_Drop_Here` only.
- Physics colliders remain separate from presentation meshes.
- Auxiliary source geometry (for example a giant Plane or bundled balls) may have its `MeshRenderer` disabled when it obstructs Unity presentation.
- Do not delete source geometry merely to hide an obstruction.

## 4. MainScene Migration

Target architecture:

`147VR_MainScene`  
→ `Prefab_WPBSA_12Foot_Snooker`  
→ `Visual_Meshes_Drop_Here`  
→ `147VR_Table_WPBSA_Visual_Clean_v006`

Migration rule:
- Replace the legacy `v004` visual reference only with the Physics prefab containing V006.
- Do not duplicate the physics authority.
- Do not change collider transforms during migration.
- Preserve a rollback backup before any MainScene serialization change.

## 5. Alignment Gate

Verify in Unity before declaring migration complete:
1. Cloth / playing-surface height against authoritative `Bed_Collider`.
2. Cushion nose against authoritative `Cushion_Colliders`.
3. Six pocket mouths against authoritative pocket geometry.
4. Ball center / ball roll plane against the authoritative physics surface.
5. Visual origin, rotation, and scale are identity unless an explicitly measured visual-only offset is required.

Acceptance principle: **Physics stays fixed; Visual conforms to Physics.**

## 6. Legacy Editor Scripts Lockdown

Legacy table builders must be treated as **DEPRECATED / QUARANTINED** and must not be used to generate a competing table visual.

Known legacy builder/audit candidates include:
- `Assets/Editor/Create147VRTableVisualPrefab.cs`
- `Assets/Editor/Create147VRTableVisualPrefabV002.cs`
- `Assets/Editor/Create147VRTableVisualPrefabV003.cs`
- `Assets/Editor/147VR_TableVisualPrefabBuilder.cs`
- Other M7.x table visual builder/audit scripts discovered during migration.

These builder files have now been moved to:
`Assets/_Archive/Editor_Legacy/`

Archived on 2026-09-03:
- `Create147VRTableVisualPrefab.cs` + `.meta`
- `Create147VRTableVisualPrefabV002.cs` + `.meta`
- `Create147VRTableVisualPrefabV003.cs` + `.meta`
- `147VR_TableVisualPrefabBuilder.cs` + `.meta`
- `PoolTablePrefabBuilder.cs` + `.meta`

M7.4 audit/verification scripts remain in `Assets/Editor/` until the current V006 migration audit is complete; they are not visual authorities and must not regenerate a competing table.

## 7. Critical Scene Integrity Finding

Before MainScene migration, an audit found **two active serialized `Physics Table (runtime)` roots** in `147VR_MainScene.unity`.

This is a migration blocker because blindly replacing the table visual or deleting one physics root could change collision behavior. The duplicate physics roots must be identified and reconciled against the certified Physics Authority before any destructive scene cleanup.

**Rule:** do not delete or modify either physics root until its provenance and collider set are verified.
