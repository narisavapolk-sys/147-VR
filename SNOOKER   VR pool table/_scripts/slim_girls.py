"""Slim processing for Cute Girl 5.2 + Chubby magic girl.

- Deletes all clothing objects (keeps body/face/hair)
- Slims the body mesh: scales each vertex's (x,y) radius from the spine axis
  by a height-based profile (waist/hips/thighs most, head/hands/feet untouched)
- Chubby: scales whole rig down from ~2.9m to ~1.6m to match Cute Girl
- Saves "_SLIM" blends and exports FBX (Chubby keeps its armature/rig)
"""
import bpy, os
from mathutils import Vector

BASE = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"
OUT = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/FBX"

# (normalized height t 0=feet..1=head, radial factor) — lower = slimmer
PROFILE = [
    (0.00, 1.00),  # feet
    (0.06, 0.92),  # ankles
    (0.15, 0.85),  # calves
    (0.28, 0.82),  # knees
    (0.38, 0.78),  # thighs
    (0.48, 0.72),  # hips
    (0.56, 0.66),  # waist — slimmest
    (0.65, 0.70),  # lower ribs
    (0.75, 0.76),  # chest
    (0.83, 0.82),  # shoulders
    (0.88, 0.88),  # neck
    (0.93, 0.96),  # chin
    (1.00, 1.00),  # head
]


def interp(profile, t):
    if t <= profile[0][0]:
        return profile[0][1]
    if t >= profile[-1][0]:
        return profile[-1][1]
    for i in range(len(profile) - 1):
        t0, f0 = profile[i]
        t1, f1 = profile[i + 1]
        if t0 <= t <= t1:
            return f0 + (f1 - f0) * (t - t0) / (t1 - t0)
    return 1.0


def slim_mesh(obj, spine):
    """Scale each vertex radius from spine (x,y) by height profile. Rest-pose edit."""
    bm = bpy.context.blend_data.meshes.new  # noqa
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    zs = [v.co.z for v in bm.verts]
    zmin, zmax = min(zs), max(zs)
    for v in bm.verts:
        t = (v.co.z - zmin) / (zmax - zmin) if zmax > zmin else 0.5
        f = interp(PROFILE, t)
        v.co.x = spine.x + (v.co.x - spine.x) * f
        v.co.y = spine.y + (v.co.y - spine.y) * f
    bm.to_mesh(obj.data)
    bm.free()


def spine_xy(obj):
    """Average x,y of mesh vertices (character centered on origin)."""
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    return Vector((sum(xs) / len(xs), sum(ys) / len(ys), 0))


def strip_mesh(obj):
    """Apply ARMATURE modifiers (bake rest pose), keeping skin weights."""
    for m in list(obj.modifiers):
        if m.type == 'ARMATURE':
            obj.modifiers.remove(m)
    return obj


# ------------------------------------------------------------------ Cute Girl
def process_cute():
    src = BASE + r"/Cute Girl 5.2.blend"
    bpy.ops.wm.open_mainfile(filepath=src)
    scene = bpy.context.scene

    clothes = {"bikini", "boot", "jacket", "pant", "sock", "top"}
    for name in clothes:
        ob = bpy.data.objects.get(name)
        if ob:
            bpy.data.objects.remove(ob, do_unlink=True)
            print("removed:", name)

    body = bpy.data.objects.get("body")
    if body:
        slim_mesh(body, spine_xy(body))
        print("slimmed body, verts:", len(body.data.vertices))

    # cleanup: remove unused materials/datas
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in bpy.data.materials:
        if block.users == 0:
            bpy.data.materials.remove(block)

    out_blend = BASE + r"/Cute Girl 5.2 SLIM.blend"
    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print("saved:", out_blend)

    bpy.ops.export_scene.fbx(
        filepath=OUT + r"/Cute Girl SLIM.fbx",
        use_selection=False,
        object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL',
        apply_unit_scale=True,
    )
    print("exported:", OUT + r"/Cute Girl SLIM.fbx")


# ---------------------------------------------------------------- Chubby girl
def process_chubby():
    src = BASE + r"/Chubby magic girl.blend"
    bpy.ops.wm.open_mainfile(filepath=src)

    clothes = {"boot", "bra", "Corset", "skirt", "top", "hat"}
    for name in clothes:
        ob = bpy.data.objects.get(name)
        if ob:
            bpy.data.objects.remove(ob, do_unlink=True)
            print("removed:", name)

    body = bpy.data.objects.get("chubby_body")
    if body:
        # bake armature to rest pose on a copy-free basis: keep weights,
        # remove modifiers so mesh no longer depends on rig for its shape
        strip_mesh(body)
        slim_mesh(body, spine_xy(body))
        print("slimmed chubby_body, verts:", len(body.data.vertices))

    # also strip armature modifiers from face parts so they keep rest shape
    for name in ["eyes_brow", "eyes_L", "eyes_R", "eyes_lashes",
                 "teeth_down", "teeth_up", "tongue"]:
        ob = bpy.data.objects.get(name)
        if ob:
            strip_mesh(ob)

    # ---- scale whole scene down 2.9m -> 1.6m (around world origin so
    # locations like hair at z=2.68 scale down too) ----
    scale = 1.6 / 2.912
    from mathutils import Matrix
    S = Matrix.Scale(scale, 4)
    for ob in bpy.data.objects:
        if ob.type in ('MESH', 'ARMATURE', 'EMPTY'):
            ob.matrix_world = S @ ob.matrix_world
    # bake transforms into mesh/armature data
    bpy.ops.object.select_all(action='DESELECT')
    for ob in bpy.context.view_layer.objects:
        if ob.type in ('MESH', 'ARMATURE'):
            ob.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.select_all(action='DESELECT')
    print("scaled scene by", round(scale, 4))

    # remove rig widgets (WGT-*) — they are rig controls, not model parts
    for ob in list(bpy.data.objects):
        if "WGT-" in ob.name:
            bpy.data.objects.remove(ob, do_unlink=True)

    out_blend = BASE + r"/Chubby magic girl SLIM.blend"
    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print("saved:", out_blend)

    # export armature + meshes (rigged, ready for re-clothing)
    bpy.ops.export_scene.fbx(
        filepath=OUT + r"/Chubby magic girl SLIM.fbx",
        use_selection=False,
        object_types={'ARMATURE', 'MESH', 'EMPTY'},
        add_leaf_bones=False,
        apply_scale_options='FBX_SCALE_ALL',
        apply_unit_scale=True,
        bake_anim=False,
    )
    print("exported:", OUT + r"/Chubby magic girl SLIM.fbx")


process_cute()
process_chubby()
print("ALL DONE")
