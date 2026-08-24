"""Give the newly added legs a UV map so body texture isn't smeared on them.

The original body had 44639 verts; the rebuild added 192 more (2 legs x
8 rings x 12 seg). We select only faces whose verts are all >= 44639 and
run Smart UV Project on just those faces.
"""
import bpy, bmesh

BASE = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"
OUT = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/FBX"

bpy.ops.wm.open_mainfile(filepath=BASE + r"/Cute Girl 5.2 SLIM.blend")
body = bpy.data.objects["body"]
bpy.context.view_layer.objects.active = body
bpy.ops.object.mode_set(mode='EDIT')
bm = bmesh.from_edit_mesh(body.data)
bm.verts.ensure_lookup_table()

NEW_V_START = 44639  # original body verts count (2 legs x 8 rings x 32 seg = 512 new verts)
bm.faces.ensure_lookup_table()
count = 0
for f in bm.faces:
    f.select = all(v.index >= NEW_V_START for v in f.verts)
    if f.select:
        count += 1
print("selected leg faces:", count)

if count:
    # ensure a UV layer exists
    if not body.data.uv_layers:
        bm.loops.layers.uv.new(name="UVMap")
    bmesh.update_edit_mesh(body.data)
    bpy.ops.uv.smart_project(angle_limit=66, island_margin=0.02, area_weight=0.0)

bpy.ops.object.mode_set(mode='OBJECT')
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
print("DONE")
