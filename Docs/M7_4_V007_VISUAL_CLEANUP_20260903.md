# M7.4 V007 — Visual Junk Cleanup — 2026-09-03

## Result
- Source v006 preserved; no source overwrite.
- Created `Blender/147VR_Table_WPBSA_Visual_Clean_v007.blend`.
- Created `Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v007.fbx`.
- Removed exactly two visual mesh objects: `BALL Markings` and `D Marking`.
- Verification: both objects absent from v007; Blender verification exit 0.
- Hero render `Blender/v007_live_hero_table.png` shows the green cloth clean with the two white artifacts gone.

## Safety
- Physics authority was not edited.
- `Bed_Collider` was identified in MainScene as a scene-added child of the old visual prefab and was not modified.
- Unity integration was attempted only after visual asset completion.

## Blocker
- Unity 6000.4.4f1 package-aware batch startup currently fails before the integration method runs: UPM IPC timeout after 30s (`Upm-14216`).
- This is an infrastructure/UPM blocker, not a V007 asset or physics failure.
- Do NOT use `-noUpm` to bypass the package-aware gate.

## Next safe action
- Once package-aware Unity startup is healthy, import/validate V007 FBX, create the V007 visual prefab, swap the inactive visual instance in `147VR_MainScene`, and preserve `Bed_Collider`.
