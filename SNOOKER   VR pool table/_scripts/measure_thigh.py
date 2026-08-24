import bpy
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath=r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender/Cute Girl 5.2.blend")
o = bpy.data.objects['body']
# sample cross-sections of the torso at various z to get thigh shape
for z in [0.71, 0.75, 0.80, 0.85, 0.90]:
    vs = [v.co for v in o.data.vertices if abs(v.co.z - z) < 0.02]
    if not vs: continue
    xs = [v.x for v in vs]; ys = [v.y for v in vs]
    cx = sum(xs)/len(xs); cy = sum(ys)/len(ys)
    # find left/right extremes to get two thigh centers
    left = [v for v in vs if v.x < cx]
    right = [v for v in vs if v.x > cx]
    for tag, grp in [("L", left), ("R", right)]:
        if not grp: continue
        gx = sum(v.x for v in grp)/len(grp)
        gy = sum(v.y for v in grp)/len(grp)
        # radius: avg dist from group center
        r = sum((v - Vector((gx, gy, v.z))).length for v in grp)/len(grp)
        print(f"z={z:.2f} {tag}: center=({gx:.3f},{gy:.3f}) radius={r:.3f} n={len(grp)}")
    # ankle reference from foot islands
print("\nfoot islands at z 0.004..0.170, x +-0.08, y -0.15..0.06")
