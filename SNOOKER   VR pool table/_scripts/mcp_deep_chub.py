import bpy
bpy.ops.wm.open_mainfile(filepath=r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender/Chubby magic girl SLIM.blend")
print("FILE:", bpy.data.filepath)
for name in ['chubby_body','teeth_down','teeth_up','tongue','hair','eyes_L']:
    o = bpy.data.objects.get(name)
    if not o: 
        print(name, "MISSING"); continue
    print(f"{name}: loc={tuple(round(x,3) for x in o.location)} scale={tuple(round(x,3) for x in o.scale)}")
    # world z bounds
    import mathutils
    mn=[1e9]*3; mx=[-1e9]*3
    for v in o.bound_box:
        w = o.matrix_world @ mathutils.Vector(v)
        for i in range(3):
            mn[i]=min(mn[i],w[i]); mx[i]=max(mx[i],w[i])
    print(f"   world z: {mn[2]:.3f}..{mx[2]:.3f}")
