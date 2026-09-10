import bpy
obj=bpy.data.objects.get('TABLE FRAME'); me=obj.data
z=[p for p in me.polygons if p.area<1e-10]
print('ZERO_COUNT',len(z))
# cluster by world-space center rounded
from collections import Counter
c=Counter((round((obj.matrix_world@p.center).x,3),round((obj.matrix_world@p.center).y,3),round((obj.matrix_world@p.center).z,3)) for p in z)
print('ZERO_CENTER_CLUSTERS',len(c))
for k,n in c.most_common(30): print('CLUSTER',k,n)
for p in z[:40]:
 pts=[obj.matrix_world@me.vertices[i].co for i in p.vertices]; xs=[v.x for v in pts]; ys=[v.y for v in pts]; zs=[v.z for v in pts]
 print('ZERO_FACE',p.index,'n',len(p.vertices),'bbox',tuple(round(x,4) for x in (min(xs),max(xs),min(ys),max(ys),min(zs),max(zs))))
