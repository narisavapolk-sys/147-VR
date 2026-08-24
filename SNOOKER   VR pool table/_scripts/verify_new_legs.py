import bpy
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/FBX/Cute Girl SLIM.fbx")
o = bpy.data.objects['body']
import bmesh
bm = bmesh.new()
bm.from_mesh(o.data)
zs = [v.co.z for v in bm.verts]
print("verts:", len(bm.verts), "z:", round(min(zs),3), "..", round(max(zs),3))
mid = [z for z in zs if 0.17 < z < 0.70]
print("verts in knee-thigh range (0.17..0.70):", len(mid))
# islands
visited = set()
islands = []
for v in bm.verts:
    if v.index in visited: continue
    stack=[v]; comp=[]
    while stack:
        cur = stack.pop()
        if cur.index in visited: continue
        visited.add(cur.index); comp.append(cur)
        for e in cur.link_edges:
            for vv in (e.verts[0], e.verts[1]):
                if vv.index not in visited: stack.append(vv)
    islands.append(comp)
print("islands:", len(islands))
for i, comp in enumerate(sorted(islands, key=len, reverse=True)[:3]):
    zs2=[v.co.z for v in comp]
    print(f"  island {i}: verts={len(comp)} z {min(zs2):.3f}..{max(zs2):.3f}")
