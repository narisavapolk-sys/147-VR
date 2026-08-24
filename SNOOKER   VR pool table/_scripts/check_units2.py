import bpy
from mathutils import Vector
base = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"
bpy.ops.wm.open_mainfile(filepath=base + r"/Chubby magic girl.blend")

arm = bpy.data.objects['magic girl  pbr_Rigify']
moved = []
for b in arm.pose.bones:
    e = b.rotation_quaternion.to_euler()
    if abs(e.x) + abs(e.y) + abs(e.z) > 0.01:
        moved.append(b.name)
print("posed bones:", len(moved), moved[:8])

raw = bpy.data.objects['chubby_body']
zs2 = [v.co.z for v in raw.data.vertices]
print("raw mesh z:", min(zs2), max(zs2))
print("vertex groups:", len(raw.vertex_groups))
# check if armature modifier target is the rig
for m in raw.modifiers:
    if m.type == 'ARMATURE':
        print("  armature mod target:", m.object.name if m.object else None)
# clothes objects: check parent/scale
for name in ['top','skirt','bra','Corset','boot','hat']:
    ob = bpy.data.objects.get(name)
    if ob:
        print(name, "scale:", tuple(round(x,3) for x in ob.scale), "loc:", tuple(round(x,3) for x in ob.location), "verts:", len(ob.data.vertices))
