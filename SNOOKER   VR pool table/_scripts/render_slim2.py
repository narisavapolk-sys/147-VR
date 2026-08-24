import bpy
from mathutils import Vector

OUT = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.render.resolution_x = 1500
scene.render.resolution_y = 900

bpy.ops.import_scene.fbx(filepath=OUT + r"/../FBX/Cute Girl SLIM.fbx")
cute = [o for o in bpy.context.selected_objects if o.type == 'MESH']
bpy.ops.import_scene.fbx(filepath=OUT + r"/../FBX/Chubby magic girl SLIM.fbx")
chub = [o for o in bpy.context.selected_objects if o.type == 'MESH']

def bounds(objs):
    mn = Vector((1e9, 1e9, 1e9)); mx = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        for v in o.bound_box:
            w = o.matrix_world @ Vector(v)
            for i in range(3):
                mn[i] = min(mn[i], w[i]); mx[i] = max(mx[i], w[i])
    return mn, mx

for name, objs, dx in [("CUTE", cute, -1.15), ("CHUB", chub, 1.15)]:
    mn, mx = bounds(objs)
    print(name, "min", tuple(round(v,3) for v in mn), "max", tuple(round(v,3) for v in mx),
          "size", tuple(round(mx[i]-mn[i],3) for i in range(3)))
    for o in objs:
        o.location.x += dx - (mn.x + mx.x) / 2

# lights
for i, (x, y, z, e) in enumerate([(-3, -3.5, 4, 600), (3, -3.5, 4, 600), (0, 4, 5, 250)]):
    ld = bpy.data.lights.new(f"L{i}", 'AREA')
    ld.energy = e; ld.size = 1.5
    lo = bpy.data.objects.new(f"L{i}", ld)
    scene.collection.objects.link(lo)
    lo.location = (x, y, z)

# camera: look at origin (between the two girls) from front
cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
cam.location = (0, -3.2, 0.9)
# point at (0, 0, 0.85)
dirv = Vector((0, 0, 0.85)) - cam.location
cam.rotation_euler = dirv.to_track_quat('-Z', 'Y').to_euler()
cam_data.lens = 50
scene.camera = cam

scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = OUT + r"/../slim_girls_preview.png"
bpy.ops.render.render(write_still=True)
print("RENDERED", scene.render.filepath)
