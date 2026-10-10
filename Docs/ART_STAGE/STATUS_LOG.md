# 147 VR — ART Status Log (2026-10-11, owner present)

## Setup V008 — DONE (owner ran the menu)
- Scene `Assets/Scenes/147VR_MainScene_V008_TEST.unity` created (original MainScene untouched).
- `147VR_Table_V008_OPT_VISUAL` placed at old slot; URP materials + FELT applied; DARC 4-light rig added; old `Prefab_WPBSA_12Foot_Snooker` disabled (physics `Bed_Collider` kept).

## Venue — exported + staged
- `147VR_Venue_Crucible.fbx` (17.3MB, 321 meshes, ~300k tris) from the Crucible .blend, no table.
- Staged to `Assets/AAA/ImportedSnooker/Venue/`.
- NOTE: no image textures came through (venue materials are procedural/flat like the table) → needs URP material/bake pass for full look.

## Issue found: stray white "Cube" on the felt
- A placeholder cube (FBX leftover, named `Cube*`) sits on the play surface. Being removed by the new menu tool.

## New editor tool
- `147VR/Art/Cleanup Strays + Place Venue` (Assets/Editor/147VR_ArtMaterials_Tools.cs): unpacks the table prefab, destroys `Cube*` children, instantiates the venue as `147VR_VENUE_CRUCIBLE` dressing, saves the scene.

## Next
1. Run the Cleanup+Venue menu → screenshot.
2. If the white box isn't named `Cube*`, capture its name and target it precisely.
3. Assess look/lighting; align table felt to physics plane 0.756 if balls float/sink.
4. LOD/decimate (table 243k + venue 300k tris) for Quest; AudioMixer + wire SFX; re-run Golden regression.

## Constraints (unchanged)
- 16GB RAM: never Unity+Blender heavy at once. Don't delete Library/PackageCache. Batch -executeMethod unreliable here (ShaderGraph editor-asm compile abort) → GUI menu is the reliable path.
