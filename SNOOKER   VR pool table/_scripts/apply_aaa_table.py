import bpy
import os

SRC = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\poolTable2.blend"
OUT = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\FBX\PREFAB POoL table.fbx"

bpy.ops.wm.open_mainfile(filepath=SRC)
bpy.context.view_layer.update()

COLOR_FELT = (0.05, 0.35, 0.10)   # tournament green
COLOR_WOOD = (0.40, 0.20, 0.05)   # mahogany
COLOR_METAL = (0.62, 0.62, 0.62)  # chrome silver
COLOR_DARK = (0.03, 0.03, 0.035)  # rubber / pocket interior

ROLES = {
    "tableBed": "felt", "tableCushion": "felt",
    "tableRail": "wood", "tableSide": "wood", "tableSideCorner": "wood",
    "tableLeg": "wood", "tableInlay": "wood", "tableInlay.001": "wood",
    "tableDecor": "metal", "tableFoot": "metal", "tablePocketO": "metal",
    "tablePocketI": "dark", "tableFootPad": "dark",
}

def make_mat(name, base, rough, metallic=0.0, spec=0.5):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nt = mat.node_tree
    for node in list(nt.nodes):
        nt.nodes.remove(node)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = (*base, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metallic
    key = "Specular IOR Level" if "Specular IOR Level" in bsdf.inputs else "Specular"
    bsdf.inputs[key].default_value = spec
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat

mats = {
    "felt": make_mat("AAA_Felt", COLOR_FELT, 0.95, 0.0, 0.10),
    "wood": make_mat("AAA_Wood", COLOR_WOOD, 0.35, 0.0, 0.55),
    "metal": make_mat("AAA_Metal", COLOR_METAL, 0.18, 1.0, 0.9),
    "dark": make_mat("AAA_Dark", COLOR_DARK, 0.55, 0.0, 0.2),
}

for obj in bpy.data.objects:
    if obj.type != "MESH" or not obj.name.startswith("table"):
        continue
    role = ROLES.get(obj.name, "wood")
    mat = mats[role]
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)
    print(f"{obj.name} -> {mat.name}")

bpy.ops.wm.save_mainfile(filepath=SRC)
print("SAVED:", SRC)

bpy.ops.object.select_all(action="DESELECT")
for obj in bpy.data.objects:
    if obj.type == "MESH" and obj.name.startswith("table"):
        obj.select_set(True)
if os.path.exists(OUT):
    os.remove(OUT)
bpy.ops.export_scene.fbx(
    filepath=OUT,
    use_selection=True,
    object_types={"MESH"},
    use_mesh_modifiers=True,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    path_mode="COPY",
    embed_textures=True,
)
print("EXPORTED:", OUT, os.path.getsize(OUT))
