"""Apply real PBR textures (felt + wood grain) to the pool table for a skin.

Usage:
  blender --background --python apply_pbr.py -- main|walnut

- Creates UV maps (smart project) for felt/wood parts if missing.
- Rebuilds AAA_Felt / AAA_Wood with albedo + normal + roughness image textures
  (albedos are pre-tinted by bake_tinted_albedo.py).
- Exports FBX with embedded textures.
- main: also saves the blend (source of truth becomes PBR).
"""
import bpy
import os
import sys

SKIN = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "main"

BLEND = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\poolTable2.blend"
FIG = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\figs\pbr"
FBX_OUT = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\FBX"
SAVE = SKIN == "main"

print(f"PBR_START skin={SKIN}")

bpy.ops.wm.open_mainfile(filepath=BLEND)

# ---- 1) Walnut palette (main keeps its Navy & Gold colors) ----
if SKIN == "walnut":
    palette = {
        "AAA_Felt": (0.07, 0.25, 0.11, 1.0),
        "AAA_Wood": (0.24, 0.13, 0.06, 1.0),
        "AAA_Metal": (0.50, 0.33, 0.16, 1.0),
    }
    for mname, col in palette.items():
        mat = bpy.data.materials.get(mname)
        if mat and mat.use_nodes:
            bsdf = mat.node_tree.nodes.get("Principled BSDF")
            if bsdf:
                bsdf.inputs["Base Color"].default_value = col
                print(f"  palette {mname} -> {col[:3]}")

# ---- 2) UVs: apply modifiers + smart project for felt/wood parts ----
for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue
    mats = {s.material.name for s in obj.material_slots if s.material}
    if not (mats & {"AAA_Felt", "AAA_Wood"}):
        continue
    bpy.context.view_layer.objects.active = obj
    for mod in list(obj.modifiers):
        try:
            bpy.ops.object.modifier_apply(modifier=mod.name)
            print(f"  applied modifier {mod.name} on {obj.name}")
        except Exception as e:
            print(f"  mod fail {obj.name}/{mod.name}: {e}")
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=66, island_margin=0.02, area_weight=0.0)
    bpy.ops.object.mode_set(mode="OBJECT")
    print(f"  UV-projected {obj.name} (verts={len(obj.data.vertices)})")

# ---- 3) PBR material nodes ----
def build_pbr(mat, albedo, normal, roughimg):
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    out.location = (500, 0)
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.location = (200, 0)
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.4
    else:
        bsdf.inputs["Specular"].default_value = 0.4

    tex = nodes.new("ShaderNodeTexImage")
    tex.location = (-220, 200)
    tex.image = bpy.data.images.load(albedo)
    links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])

    if normal and os.path.exists(normal):
        nimg = nodes.new("ShaderNodeTexImage")
        nimg.location = (-220, 0)
        nimg.image = bpy.data.images.load(normal)
        nimg.image.colorspace_settings.name = "Non-Color"
        nmap = nodes.new("ShaderNodeNormalMap")
        nmap.location = (-20, 40)
        links.new(nimg.outputs["Color"], nmap.inputs["Color"])
        links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])

    if roughimg and os.path.exists(roughimg):
        rimg = nodes.new("ShaderNodeTexImage")
        rimg.location = (-220, -200)
        rimg.image = bpy.data.images.load(roughimg)
        rimg.image.colorspace_settings.name = "Non-Color"
        links.new(rimg.outputs["Color"], bsdf.inputs["Roughness"])
    else:
        bsdf.inputs["Roughness"].default_value = 0.92 if "Felt" in mat.name else 0.3

    print(f"  rebuilt {mat.name}")


wood_key = "walnut" if SKIN == "walnut" else "navy"
felt = bpy.data.materials.get("AAA_Felt")
wood = bpy.data.materials.get("AAA_Wood")
build_pbr(felt, os.path.join(FIG, f"felt_albedo_{SKIN}.png"),
          os.path.join(FIG, "felt_nor_gl.jpg"),
          os.path.join(FIG, "felt_rough.jpg"))
build_pbr(wood, os.path.join(FIG, f"wood_albedo_{SKIN}.png"),
          os.path.join(FIG, f"wood_{wood_key}_nor_gl.jpg"),
          os.path.join(FIG, f"wood_{wood_key}_rough.jpg"))

if SKIN == "walnut":
    m = bpy.data.materials.get("AAA_Metal")
    if m and m.use_nodes:
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Roughness"].default_value = 0.25
            bsdf.inputs["Metallic"].default_value = 1.0

if SKIN == "blue":
    # Blue skin: navy felt + dark cabinet wood + brass/gold
    blue_palette = {
        "AAA_Felt": (0.02, 0.06, 0.22, 1.0),
        "AAA_Wood": (0.10, 0.06, 0.03, 1.0),
        "AAA_Metal": (0.75, 0.55, 0.18, 1.0),
        "AAA_Dark": (0.02, 0.02, 0.02, 1.0),
    }
    for mname, col in blue_palette.items():
        mat = bpy.data.materials.get(mname)
        if mat and mat.use_nodes:
            bsdf = mat.node_tree.nodes.get("Principled BSDF")
            if bsdf:
                bsdf.inputs["Base Color"].default_value = col
                if "Felt" in mname:
                    bsdf.inputs["Roughness"].default_value = 0.95
                    bsdf.inputs["Metallic"].default_value = 0.0
                elif "Wood" in mname:
                    bsdf.inputs["Roughness"].default_value = 0.35
                    bsdf.inputs["Metallic"].default_value = 0.0
                elif "Metal" in mname:
                    bsdf.inputs["Roughness"].default_value = 0.20
                    bsdf.inputs["Metallic"].default_value = 1.0
                print(f"  blue palette {mname} -> {col[:3]}")

# ---- 4) Export (table parts only: table* + Plaque*) ----
bpy.ops.object.select_all(action="DESELECT")
selected = []
for obj in bpy.data.objects:
    if obj.type == "MESH" and (obj.name.startswith("table") or obj.name.startswith("Plaque")):
        obj.select_set(True)
        selected.append(obj.name)
print("  selected", len(selected), "objects")

skin_names = {"main": "PREFAB POoL table.fbx", "walnut": "PREFAB POoL table Walnut.fbx", "blue": "PREFAB POoL table Blue.fbx"}
name = skin_names.get(SKIN, f"PREFAB POoL table {SKIN}.fbx")
out = os.path.join(FBX_OUT, name)
bpy.ops.export_scene.fbx(
    filepath=out,
    use_selection=True,
    use_mesh_modifiers=True,
    path_mode="COPY",
    embed_textures=True,
    object_types={"MESH"},
)
print("EXPORTED", out, os.path.getsize(out))

if SAVE:
    bpy.ops.wm.save_mainfile(filepath=BLEND)
    print("SAVED", BLEND)

print("SKIN_DONE", SKIN)
