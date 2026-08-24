import bpy
# load the slim file and verify legs
bpy.ops.wm.open_mainfile(filepath=r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender/Cute Girl 5.2 SLIM.blend")
o = bpy.data.objects.get('body')
if not o:
    print("BODY_MISSING")
else:
    print("BODY_FOUND verts:", len(o.data.vertices))
    zs = [v.co.z for v in o.data.vertices]
    print("z:", round(min(zs),3), "..", round(max(zs),3))
    mid = [z for z in zs if 0.17 < z < 0.70]
    print("knee-thigh verts (0.17..0.70):", len(mid))
    print("UV layers:", [u.name for u in o.data.uv_layers])
