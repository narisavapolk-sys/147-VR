"""Render the FIXED Chubby FBX with sun light + dark background, tight framing.
Goal: clean silhouette check that hat/hair/eyes/mouth sit on the body.
"""
import bpy, os
from mathutils import Vector

BASE = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table"
FBX = os.path.join(BASE, "FBX", "Chubby Girl Dancing.fbx")
IMG = os.path.join(BASE, "Images")

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 900
scene.render.resolution_y = 1400

# dark world
w = bpy.data.worlds.new("W")
scene.world = w
w.use_nodes = True
bg = w.node_tree.nodes.get("Background")
if bg:
    bg.inputs[0].default_value = (0.05, 0.05, 0.07, 1.0)

# import
bpy.ops.import_scene.fbx(filepath=FBX)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
rig = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
mn = Vector((1e9,)*3); mx = Vector((-1e9,)*3)
for o in meshes:
    for v in o.bound_box:
        p = o.matrix_world @ Vector(v)
        for i in range(3):
            mn[i] = min(mn[i], p[i]); mx[i] = max(mx[i], p[i])
c = (mn + mx) / 2
h = mx.z - mn.z
print("center", tuple(round(v,3) for v in c), "h", round(h,3), flush=True)

# sun light (doesn't brighten background)
sun_d = bpy.data.lights.new("Sun", 'SUN')
sun_d.energy = 3.0
sun_d.angle = 0.5
sun = bpy.data.objects.new("Sun", sun_d)
scene.collection.objects.link(sun)
sun.rotation_euler = (0.9, 0.2, 0.6)

# camera
cam_d = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_d)
scene.collection.objects.link(cam)
dist = h * 2.4
cam.location = (c.x, c.y - dist, c.z + h * 0.15)
dirv = c - cam.location
cam.rotation_euler = dirv.to_track_quat('-Z', 'Y').to_euler()
cam_d.lens = 60
scene.camera = cam
scene.render.image_settings.file_format = 'PNG'

# two frames
for fname, frame in [("_chubby_fix2_rest", 5), ("_chubby_fix2_dance", 150)]:
    scene.frame_set(frame)
    scene.render.filepath = os.path.join(IMG, fname + ".png")
    bpy.ops.render.render(write_still=True)
    print("RENDERED", fname, flush=True)
print("CHECK_RENDER_DONE")
