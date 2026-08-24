import bpy
from mathutils import Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/FBX/Cute Girl SLIM.fbx")
o = bpy.data.objects['body']
mesh = o.data

# count disconnected islands via BMesh
import bmesh
bm = bmesh.new()
bm.from_mesh(mesh)
print("total verts:", len(bm.verts))
# find connected components
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
print("islands:", len(islands))
for i, comp in enumerate(sorted(islands, key=len, reverse=True)[:8]):
    zs = [v.co.z for v in comp]
    ys = [v.co.y for v in comp]
    xs = [v.co.x for v in comp]
    print(f"  island {i}: verts={len(comp)} x {min(xs):.3f}..{max(xs):.3f} y {min(ys):.3f}..{max(ys):.3f} z {min(zs):.3f}..{max(zs):.3f}")
