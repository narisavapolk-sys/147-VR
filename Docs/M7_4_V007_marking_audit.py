import bpy
from mathutils import Vector
import json

PATH = r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007.blend'
OUT = r'C:\Users\mongo\UnityProjects\147 VR\Docs\M7_4_V007_TABLE_SURFACE_AUDIT.json'
bpy.ops.wm.open_mainfile(filepath=PATH)
obj = bpy.data.objects.get('TABLE SURFACE')
if obj is None or obj.type != 'MESH':
    raise RuntimeError('TABLE SURFACE mesh not found')
mesh = obj.data
world = obj.matrix_world
verts = [world @ v.co for v in mesh.vertices]
mins = Vector((min(v.x for v in verts), min(v.y for v in verts), min(v.z for v in verts)))
maxs = Vector((max(v.x for v in verts), max(v.y for v in verts), max(v.z for v in verts)))
uv_layer = mesh.uv_layers.active
uvs = [uv_layer.data[i].uv[:] for poly in mesh.polygons for i in poly.loop_indices] if uv_layer else []
uv_min = (min(u[0] for u in uvs), min(u[1] for u in uvs)) if uvs else None
uv_max = (max(u[0] for u in uvs), max(u[1] for u in uvs)) if uvs else None
result = {
 'blend': PATH,
 'object': obj.name,
 'vertices': len(mesh.vertices),
 'polygons': len(mesh.polygons),
 'dimensions_local': list(obj.dimensions),
 'location_world': list(world.translation),
 'rotation_euler_world': list(world.to_euler()),
 'scale_world': list(world.to_scale()),
 'world_min': list(mins), 'world_max': list(maxs), 'world_size': list(maxs-mins),
 'uv_layer': uv_layer.name if uv_layer else None,
 'uv_min': uv_min, 'uv_max': uv_max,
 'materials': [m.name if m else None for m in obj.data.materials],
}
print(json.dumps(result, indent=2))
with open(OUT,'w',encoding='utf-8') as f: json.dump(result,f,indent=2)
