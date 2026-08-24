import bpy
from mathutils import Vector

OUT = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.render.resolution_x = 1000
scene.render.resolution_y = 1400

def render_one(fbx_path, out_png, label):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=fbx_path)
    objs = [o for o in bpy.context.selected_objects if o.type == 'MESH']
    mn = Vector((1e9, 1e9, 1e9)); mx = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        for v in o.bound_box:
            w = o.matrix_world @ Vector(v)
            for i in range(3):
                mn[i] = min(mn[i], w[i]); mx[i] = max(mx[i], w[i])
    c = (mn + mx) / 2
    h = mx.z - mn.z
    print(label, "center", tuple(round(v,3) for v in c), "h", round(h,3))
    # lights
    for i, (x, y, z, e) in enumerate([(c.x-1.5, c.y-2, c.z+1.5, 400), (c.x+1.5, c.y-2, c.z+1.5, 400)]):
        ld = bpy.data.lights.new(f"L{i}", 'AREA')
        ld.energy = e; ld.size = 1.5
        lo = bpy.data.objects.new(f"L{i}", ld)
        scene.collection.objects.link(lo)
        lo.location = (x, y, z)
    cam_data = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    dist = h * 2.6
    cam.location = (c.x, c.y - dist, c.z + h*0.35)
    dirv = c - cam.location
    cam.rotation_euler = dirv.to_track_quat('-Z', 'Y').to_euler()
    cam_data.lens = 55
    scene.camera = cam
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = out_png
    bpy.ops.render.render(write_still=True)
    print("RENDERED", out_png)

render_one(OUT + r"/../FBX/Cute Girl SLIM.fbx", OUT + r"/../cute_slim_preview.png", "CUTE")
render_one(OUT + r"/../FBX/Chubby magic girl SLIM.fbx", OUT + r"/../chubby_slim_preview.png", "CHUB")
print("ALL DONE")
