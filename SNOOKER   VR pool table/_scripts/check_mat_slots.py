import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/FBX/Cute Girl SLIM.fbx")
o = bpy.data.objects['body']
print("material slots:", [m.name for m in o.data.materials])
print("num material indices:", len(o.data.materials) and max(o.data.polygons, key=lambda p: p.material_index).material_index + 1)
# count polys per material
from collections import Counter
c = Counter(p.material_index for p in o.data.polygons)
print("polys per material:", dict(c))
# check verts with no assigned material region? and vertex groups
print("vertex groups:", [g.name for g in o.vertex_groups])
