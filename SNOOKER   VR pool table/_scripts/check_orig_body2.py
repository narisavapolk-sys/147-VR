import bpy
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath=r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender/Cute Girl 5.2.blend")
o = bpy.data.objects['body']
import bmesh
bm = bmesh.new()
bm.from_mesh(o.data)
visited = set()
islands = []
for v in bm.verts:
    if v.index in visited: continue
    stack = [v]; comp = []
    while stack:
        cur = stack.pop()
        if cur.index in visited: continue
        visited.add(cur.index)
        comp.append(cur)
        for e in cur.link_edges:
            for vv in (e.verts[0], e.verts[1]):
                if vv.index not in visited:
                    stack.append(vv)
    islands.append(comp)
print("ALL islands:", len(islands))
for i, comp in enumerate(sorted(islands, key=len, reverse=True)):
    zs = [v.co.z for v in comp]
    xs = [v.co.x for v in comp]
    print(f"  {i}: verts={len(comp):6d} x {min(xs):.3f}..{max(xs):.3f} z {min(zs):.3f}..{max(zs):.3f}")

# also check z distribution: any verts in 0.17..0.70?
zs = [v.co.z for v in bm.verts]
mid = [z for z in zs if 0.17 < z < 0.70]
print("verts in knee-thigh range (0.17..0.70):", len(mid))
if mid: print("  sample:", sorted(mid)[:10])
