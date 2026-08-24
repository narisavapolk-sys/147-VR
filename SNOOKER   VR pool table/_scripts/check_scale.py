import bpy
from mathutils import Vector
base = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"

def chk(path, label):
    bpy.ops.wm.open_mainfile(filepath=path)
    print(f"--- {label} ---")
    for o in bpy.data.objects:
        if o.type == 'MESH' and ('body' in o.name.lower() or 'hair' in o.name.lower() or 'top' in o.name.lower()):
            print(f"  {o.name}: loc={tuple(round(x,3) for x in o.location)} scale={tuple(round(x,3) for x in o.scale)}")
    for a in bpy.data.armatures:
        ob = bpy.data.objects.get(a.name)
        if ob:
            print(f"  ARMATURE {a.name}: loc={tuple(round(x,3) for x in ob.location)} scale={tuple(round(x,3) for x in ob.scale)}")
            # rest pose height: bounding box of bones
            mn=[1e9]*3; mx=[-1e9]*3
            for b in a.bones:
                h = b.head_local
                for i in range(3):
                    mn[i]=min(mn[i],h[i]); mx[i]=max(mx[i],h[i])
            print(f"    bone rest bbox z: {mn[2]:.3f}..{mx[2]:.3f}")
    # check pose mode bones (current pose)
    print("  pose bones (rotated from rest):", sum(1 for o in bpy.data.objects if o.type=='ARMATURE' for b in o.pose.bones if b.rotation_quaternion != b.parent.rotation_quaternion if False))
    for o in bpy.data.objects:
        if o.type=='ARMATURE':
            moved = [b.name for b in o.pose.bones if b.rotation_quaternion.to_euler().length > 0.01]
            print(f"  posed bones count: {len(moved)} e.g. {moved[:6]}")

chk(base + r"/Cute Girl 5.2.blend", "Cute Girl")
chk(base + r"/Chubby magic girl.blend", "Chubby")
