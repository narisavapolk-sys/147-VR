# 147 VR — M7.4 Table Visual Base — 2026-09-02
Status: BASE PREFAB CREATED — READY FOR BEAUTY PASS
Scope: visual asset only. Physics Authority untouched.

## Source / Reference
- Source: `SNOOKER   VR pool table/Blender/REVISED_Snooker_Table_WPBSA_GOLD.blend`
- Reference: Xing Pai Star / Aristocrat tournament-style table.
- Target language: green cloth, mahogany/golden woodwork, turned legs, tournament cushion construction.

## Verified Geometry
- Playing area: 3.569 m × 1.778 m.
- Outer frame bounds: approximately 3.847 m × 2.076 m.
- Ball diameter: 0.052578 m (~52.58 mm).
- Ball objects preserved: 22.
- Ball positions preserved from the WPBSA working source.
- Visual table remains separate from certified physics authority.

## Cleanup / Export
- Working copy: `147VR_Table_WPBSA_Base_v001.blend`.
- Source `REVISED_Snooker_Table_WPBSA_GOLD.blend` was not overwritten.
- Removed presentation-only floor/camera/lights from runtime export.
- No cue/rest/stick object was present in the working source.
- Sanitized 16 invalid legacy UV coordinates during FBX export; geometry was otherwise not reshaped.

## Unity Deliverables
- FBX: `Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Base_v001.fbx`
- Prefab: `Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_12ft_VISUAL.prefab`
- Unity package-aware import succeeded with UPM IPC connected in 0.3 s using the clean child environment.

## Boundary
This is the correct-scale visual foundation, not the final AAA beauty pass. Pocket fall/jaw details, cushion nose verification, materials, bevel/micro-detail and VR optimization remain downstream. Do not change physics colliders or Golden values to fit artwork.
