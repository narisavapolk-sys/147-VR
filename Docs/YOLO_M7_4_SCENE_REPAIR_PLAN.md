# 147 VR — YOLO M7.4 Scene Repair Plan

## Authority
- Physics Authority remains untouched.
- Visual fixes only unless an explicit runtime evidence gate proves otherwise.
- No fabricated Golden values.
- Active scene: `147VR_MainScene`.

## Current execution targets
1. Lock the intended V007 visual source.
2. Preserve `Snooker_Markings_V007.png` as the surface marking source.
3. Validate ball/spot alignment against runtime Physics spawn.
4. Repair visual pocket/fascia/rail defects without changing colliders.
5. Remove confirmed visual junk and redundant inactive scene objects safely.
6. Verify environment dependencies before disabling anything.

## Known V007 assets
- `Assets/AAA/ImportedSnooker/Textures/Snooker_Markings_V007.png`
- `Assets/AAA/ImportedSnooker/Source/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.fbx`
- `Assets/Shaders/147VR_TableSurfaceMarking.shader`

## Guardrails
- Do NOT modify Physics Golden cases.
- Do NOT modify `Bed_Collider` dimensions as part of visual repair.
- Do NOT replace runtime spawn coordinates with visual prefab guesses.
- Do NOT delete source assets; prefer additive/candidate prefab workflow.
- Do NOT disable environment systems without dependency evidence.
