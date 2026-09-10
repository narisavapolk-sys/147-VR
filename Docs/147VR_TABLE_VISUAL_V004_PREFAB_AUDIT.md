# 147VR Table Visual v004 — Prefab Audit

- Timestamp (UTC): 2026-09-03
- Prefab: `Assets/BlenderTest/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004.prefab`
- Visual source lineage: Blender `v006` → FBX `v006` → Unity Prefab `v004`
- Audit method: static Unity prefab/source verification; no prefab YAML was edited.

## Prefab safety
- Prefab is a `PrefabInstance` referencing FBX source GUID `89b8ac6b2fa35544caca610ec978a048`.
- `m_RemovedComponents: []`
- `m_RemovedGameObjects: []`
- `m_AddedGameObjects: []`
- `m_AddedComponents: []`
- No Unity physics components are added by the prefab wrapper.
- Scene replacement: NOT PERFORMED.
- Physics Authority: NOT TOUCHED.

## Visual source
- Blender source: `147VR_Table_AAA_Source_v006.blend`
- Main table dimensions: `2.142172 × 0.849856 × 3.84 m`
- Main table geometry: `982,096 verts / 1,966,736 polys`
- Materials: Cloth, Walnut, Brass, Cushion Rubber, Pocket Black, Ivory Finish.
- Six dedicated pocket objects are present in the v006 source.

## Result
- PASS: v004 is a visual-only prefab wrapper around the imported v006 FBX.
- PASS: no scene replacement performed.
- PASS: Physics Table / colliders / Golden cases / M5 remain outside this asset path.
- NEXT: isolated visual↔physics alignment check before any production scene replacement.
