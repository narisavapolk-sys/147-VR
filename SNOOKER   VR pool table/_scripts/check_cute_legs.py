import bpy
from mathutils import Vector

# check the exported FBX
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/FBX/Cute Girl SLIM.fbx")
objs = [o for o in bpy.context.selected_objects if o.type == 'MESH']
print("objects:", [o.name for o in objs])
for o in objs:
    mn = Vector((1e9,1e9,1e9)); mx = Vector((-1e9,-1e9,-1e9))
    for v in o.bound_box:
        w = o.matrix_world @ Vector(v)
        for i in range(3):
            mn[i]=min(mn[i],w[i]); mx[i]=max(mx[i],w[i])
    print(f"  {o.name}: z {mn.z:.3f}..{mx.z:.3f}  y {mn.y:.3f}..{mx.y:.3f}  x {mn.x:.3f}..{mx.x:.3f}")
