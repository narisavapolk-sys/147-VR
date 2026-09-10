import bpy
from mathutils import Vector
for o in bpy.context.scene.objects:
    if o.type == 'MESH':
        bb=[o.matrix_world @ Vector(c) for c in o.bound_box]
        xs=[v.x for v in bb]; ys=[v.y for v in bb]; zs=[v.z for v in bb]
        print(f"MESH|{o.name}|verts={len(o.data.vertices)}|faces={len(o.data.polygons)}|loc={tuple(round(x,4) for x in o.location)}|dim={tuple(round(x,4) for x in o.dimensions)}|min={tuple(round(x,4) for x in (min(xs),min(ys),min(zs)))}|max={tuple(round(x,4) for x in (max(xs),max(ys),max(zs)))}")
