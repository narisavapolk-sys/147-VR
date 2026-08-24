import bpy
from mathutils import Vector
base = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"
bpy.ops.wm.open_mainfile(filepath=base + r"/Chubby magic girl.blend")
s = bpy.context.scene
print("unit scale:", s.unit_settings.scale_length)
print("length unit:", s.unit_settings.length_unit)

o = bpy.data.objects['chubby_body']
deps = bpy.context.evaluated_depsgraph_get()
ev = o.evaluated_get(deps)
m = ev.to_mesh()
zs = [v.co.z for v in m.vertices]
print("mesh local z:", min(zs), max(zs))
ev.to_mesh_clear()

# check armature pose state vs rest
arm = bpy.data.objects['magic girl  pbr_Rigify']
moved = [b.name for b in arm.pose.bones if b.rotation_quaternion and b.rotation_quaternion.to_euler().length > 0.01]
print("posed bones:", len(moved), moved[:8])

# raw mesh (unmodified) height
raw = bpy.data.objects['chubby_body']
zs2 = [v.co.z for v in raw.data.vertices]
print("raw mesh z:", min(zs2), max(zs2))
print("raw mesh scale:", raw.scale)

# check vertex groups count on body (skin weights)
print("vertex groups:", len(raw.vertex_groups))
