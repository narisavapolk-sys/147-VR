import bpy
from mathutils import Vector

OUT = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.render.resolution_x = 1600
scene.render.resolution_y = 900

# import both FBX
bpy.ops.import_scene.fbx(filepath=OUT + r"/../FBX/Cute Girl SLIM.fbx")
cute = bpy.context.selected_objects
bpy.ops.import_scene.fbx(filepath=OUT + r"/../FBX/Chubby magic girl SLIM.fbx")
chub = bpy.context.selected_objects

for ob in cute:
    ob.location.x -= 1.1
for ob in chub:
    ob.location.x += 1.1

# lights
bpy.ops.object.select_all(action='DESELECT')
for i, (x, y, z, e) in enumerate([(-3, -4, 4, 800), (3, -4, 4, 800), (0, 5, 6, 300)]):
    light_data = bpy.data.lights.new(f"L{i}", 'AREA')
    light_data.energy = e
    light_data.size = 1.5
    lo = bpy.data.objects.new(f"L{i}", light_data)
    scene.collection.objects.link(lo)
    lo.location = (x, y, z)

# camera
cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
cam.location = (0, -4.2, 1.15)
cam.rotation_euler = (1.35, 0, 0)
cam_data.lens = 60
scene.camera = cam

scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = OUT + r"/../slim_girls_preview.png"
bpy.ops.render.render(write_still=True)
print("RENDERED", scene.render.filepath)
