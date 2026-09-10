# M7.4 TABLE VISUAL v006 — Pocket Hero + Unity Integration

Date: 2026-09-03
Status: BLENDER VISUAL LOCKED / UNITY PREFAB AUTOMATION ARMED

## Visual execution
- Source preserved: `147VR_Table_WPBSA_Visual_Clean_v004.blend`
- Final Blender: `147VR_Table_WPBSA_Visual_Clean_v006.blend`
- v005 experimental torus pass was rejected after live render QA because its hard-coded Z placed rings on the floor. It is retained only as forensic history.
- v006 removes all V005 experimental geometry.
- Existing pocket leather geometry is preserved and upgraded with dark-brown procedural leather micrograin.
- Added restrained recessed dark throat lining shaped per pocket opening.
- Existing pocket net and hardware remain preserved.
- Physics authority remains external/locked.

## QA
- Dedicated close-up render: `Blender/v006_live_pocket_hero.png`
- Full table render: `Blender/v006_live_hero_table.png`
- Close-up was compared against real snooker pocket references: leather wraps the pocket mouth/throat, cloth transitions cleanly into the opening, and net/hardware remain visible below.
- No physics/collision objects were added.

## Unity asset
- Exported: `Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v006.fbx`
- FBX export selected 117 renderable mesh objects.
- Anchors were excluded from FBX export.

## Prefab integration
- Created automation: `Assets/Editor/M7_4_V006PrefabAuto.cs`
- It creates both:
  - `147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006.prefab`
  - `147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004.prefab` (compatibility name, sourced from v006)
- It does NOT modify `SampleScene.unity` or Physics Authority.
- Unity's current launcher/editor session is not yet reachable through Pipeline, so prefab save could not be independently confirmed in this run.
- The automation is guarded by an EditorPrefs one-shot flag and will execute after the project reaches a normal Editor domain reload.

## Boundary
Physics remains authority. Visual v006 is replaceable presentation content. No Golden JSON, calibration, shot lifecycle, scoring, turn logic, or live scene references were changed.
