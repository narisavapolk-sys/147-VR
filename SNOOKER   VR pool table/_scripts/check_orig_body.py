import bpy
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath=r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender/Cute Girl 5.2.blend")
o = bpy.data.objects['body']
mesh = o.data
import bmesh
bm = bmesh.new()
bm.from_mesh(mesh)
print("orig total verts:", len(bm.verts))
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
print("orig islands:", len(islands))
for i, comp in enumerate(sorted(islands, key=len, reverse=True)[:6]):
    zs = [v.co.z for v in comp]
    print(f"  island {i}: verts={len(comp)} z {min(zs):.3f}..{max(zs):.3f}")
