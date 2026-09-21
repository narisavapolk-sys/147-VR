import bpy, bmesh, math
from mathutils import Vector
from mathutils.geometry import tessellate_polygon
obj=bpy.data.objects.get('TABLE FRAME')
print('OBJECT',obj.name,'verts',len(obj.data.vertices),'polys',len(obj.data.polygons),'edges',len(obj.data.edges))
mesh=obj.data
ng=[p for p in mesh.polygons if len(p.vertices)>4]
print('NGONS',len(ng),'maxverts',max((len(p.vertices) for p in mesh.polygons),default=0))
# Degenerate faces / repeated vertices
rep=[]; zero=[]
for p in mesh.polygons:
    vs=list(p.vertices)
    if len(vs)!=len(set(vs)): rep.append(p.index)
    if p.area < 1e-10: zero.append(p.index)
print('REPEATED_VERTEX_FACES',len(rep),rep[:30])
print('ZERO_AREA_FACES',len(zero),zero[:30])
# non-manifold via bmesh
bm=bmesh.new(); bm.from_mesh(mesh); bm.verts.ensure_lookup_table(); bm.edges.ensure_lookup_table(); bm.faces.ensure_lookup_table()
non=[e.index for e in bm.edges if not e.is_manifold]
bound=[e.index for e in bm.edges if e.is_boundary]
print('NON_MANIFOLD_EDGES',len(non),'BOUNDARY_EDGES',len(bound),'NON_SAMPLE',non[:30])
# tessellation diagnostics for ngons
bad=[]
for p in ng:
    pts=[obj.matrix_world @ mesh.vertices[i].co for i in p.vertices]
    tris=tessellate_polygon([pts])
    if not tris: bad.append(p.index); continue
    a=sum((b-a).cross(c-a).length*0.5 for a,b,c in tris)
    if abs(a-p.area)>max(1e-8,p.area*1e-5): bad.append(p.index)
print('NGON_TESSELLATION_MISMATCH',len(bad),bad[:100])
# Bounding boxes for suspicious bad faces
for idx in bad[:20]:
    p=mesh.polygons[idx]; pts=[obj.matrix_world @ mesh.vertices[i].co for i in p.vertices]; xs=[v.x for v in pts]; ys=[v.y for v in pts]; zs=[v.z for v in pts]
    print('BAD_FACE',idx,'verts',len(p.vertices),'center',tuple(round(x,5) for x in p.center),'bbox',tuple(round(x,5) for x in (min(xs),max(xs),min(ys),max(ys),min(zs),max(zs))))
bm.free()
