# 147 VR — M7.4 Table Visual v004 Certification

Date: 2026-09-03
Status: VISUAL BASE LOCKED / UNITY INTEGRATION PENDING

## Live audit performed
- Inspected the actual Blender workspace/assets, not only prior notes.
- Discovered existing source chain: `147VR_Table_WPBSA_Visual_Clean_v002.blend` and `v003.blend` plus existing Unity `v003` prefab.
- Loaded and inspected `v003.blend` directly in Blender 5.2.
- Existing v003 contained 102 objects / 94 meshes / 25 materials before the v004 pass.
- Current Unity `SampleScene.unity` still references the existing `147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v003` prefab. This was intentionally NOT changed, so Physics Authority and the live scene remain untouched.

## v004 execution
Source:
`Blender/147VR_Table_WPBSA_Visual_Clean_v003.blend`

Output:
`Blender/147VR_Table_WPBSA_Visual_Clean_v004.blend`

Changes:
1. Created explicit `147VR_POCKET_ART` collection.
2. Created explicit `147VR_SPATIAL_ANCHORS` collection.
3. Added six pocket anchors plus playfield center anchor.
4. Preserved the existing pocket geometry rather than replacing it with a speculative new cutter.
5. Reassigned the usable existing pocket-pad geometry to a dedicated leather material for closer real-table perception.
6. Preserved/used existing pocket net and brass hardware geometry.
7. Tuned felt, mahogany and gold material response for the visual pass.
8. Added root metadata declaring visual v004 and `physics_authority=EXTERNAL_LOCKED`.
9. Fixed the only detected invalid UV mesh (`TABLE FRAME`) with a clean UV map.
10. Exported a clean table-only FBX for Unity.

## Live Blender result
- v004 Blender objects: 163
- v004 meshes: 148
- Pocket anchors: 6
- Playfield center anchor: 1
- Invalid UV meshes after fix: 0
- Unity FBX export: PASS
- FBX size: 14,881,244 bytes

## Visual QA renders
- `Blender/v004_live_hero_table.png`
- `Blender/v004_live_hero2.png`
- `Blender/v004_live_pocket_top.png`

The pocket was checked at close range and from a broader table view. The final pass intentionally keeps the real existing pocket cut/shape and uses the existing pad geometry for the leather guard instead of leaving the earlier experimental custom plate geometry in the visible render.

## Physics boundary
NO Physics Authority assets, calibration cases, Golden JSON, collision geometry, shot lifecycle, scoring, turn logic, or physics scripts were modified in this pass.

## Unity integration state
Created:
`Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v004.fbx`
`Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v004.fbx.meta`

The v004 Unity prefab was NOT generated because the one-shot Unity batch invocation exited before the requested Editor method executed. No scene reference was changed as a workaround.

Next controlled step:
- Open/refresh Unity 6.
- Confirm v004 FBX import.
- Generate `147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004.prefab` from the imported FBX.
- Replace only the visual prefab reference in a controlled scene copy.
- Verify visual/physics spatial alignment before touching the live scene.

## Reference basis
Real-world reference inspection confirms professional snooker tables use leather pockets and visible pocket/net hardware; XingPai's S101-12S documentation also lists Strachan 6811 cloth, tournament steel cushions, Northern rubber and leather pockets. WPBSA remains the governing reference for official snooker rules/specification context.

## Golden rule
Physics remains authority. Visual v004 is a replaceable presentation layer. Do not merge visual colliders into the physics authority without an explicit calibration/regression pass.
