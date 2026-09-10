# 147 VR — Table Visual v006 Export Audit

Date: 2026-09-03
Unity target: 6000.4.4f1

## Source
- Blender source: `147VR_Table_AAA_Source_v006.blend`
- Source path: `C:\Users\mongo\OneDrive\Desktop\snooker table idea\`
- Blender: 5.2
- Blender MCP: port 9876, verified responsive

## Scene audit
- Scene object count: 7
- Main visual mesh: `147VR_Table_Visual_HighPoly`
- Pocket meshes: `Pocket_FL`, `Pocket_FR`, `Pocket_ML`, `Pocket_MR`, `Pocket_BL`, `Pocket_BR`
- Main mesh dimensions: X 2.142172 m, Y 0.849856 m, Z 3.840000 m
- Main mesh vertices: 982,096
- Main mesh polygons: 1,966,736
- Object scales: 1,1,1

## Materials
- `MAT_Cloth_Wool_Green` — roughness 0.72
- `MAT_Walnut_Dark` — roughness 0.30
- `MAT_Brass` — metallic 0.82, roughness 0.22
- `MAT_Cushion_Rubber` — roughness 0.82
- `MAT_Pocket_Black` — roughness 0.62
- `MAT_Ivory_Finish` — roughness 0.34

## Pocket geometry
- Six dedicated pocket meshes are present.
- Each pocket mesh: 128 vertices / 66 polygons.
- Pocket centers: X +/-0.89 m, Y -1.79 / 0 / +1.79 m, Z 0.314 m.

## Export
- FBX: `Assets\BlenderTest\147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006.fbx`
- FBX export completed successfully.
- FBX size: 55,251,692 bytes (~52.7 MiB)
- Export: mesh selection only, unit scale applied, Y-up / -Z forward, no leaf bones.

## Lineage / safety
- Production compatibility prefab name remains `147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004.prefab`.
- Visual source of that production asset is v006; v004 is an identity/compatibility name only.
- No `.unity` or `.prefab` YAML was edited.
- Physics Authority was not modified.
- SampleScene was not modified.
- No other Unity project was modified.

## Next gate
Unity must import this FBX successfully before the real v004 prefab is created.
Do not alter Physics Table during visual prefab assembly.
