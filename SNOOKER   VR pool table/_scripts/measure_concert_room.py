import bpy, sys
from mathutils import Vector

path = r"C:/Users/mongo/UnityProjects/147 VR/Assets/147 main/ConcertRoom_WithTable.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path)

mins = [1e9, 1e9, 1e9]
maxs = [-1e9, -1e9, -1e9]
total = 0
for obj in bpy.data.objects:
    if obj.type != 'MESH':
        continue
    total += 1
    # account for transform
    m = obj.matrix_world
    for v in obj.bound_box:
        w = m @ Vector(v)
        for i in range(3):
            mins[i] = min(mins[i], w[i])
            maxs[i] = max(maxs[i], w[i])

print(f"MESH_OBJECTS: {total}")
print(f"MIN: {mins}")
print(f"MAX: {maxs}")
print(f"CENTER: {[(mins[i]+maxs[i])/2 for i in range(3)]}")
print(f"SIZE: {[maxs[i]-mins[i] for i in range(3)]}")
print("FLOOR_Y:", mins[1])
