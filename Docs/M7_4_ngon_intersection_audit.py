import bpy
obj=bpy.data.objects.get('TABLE FRAME'); me=obj.data

def orient(a,b,c): return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
def proper(a,b,c,d,eps=1e-9):
    o1,o2,o3,o4=orient(a,b,c),orient(a,b,d),orient(c,d,a),orient(c,d,b)
    return ((o1>eps and o2<-eps) or (o1<-eps and o2>eps)) and ((o3>eps and o4<-eps) or (o3<-eps and o4>eps))
found=[]
for p in me.polygons:
    n=len(p.vertices)
    if n<5: continue
    pts=[obj.matrix_world@me.vertices[i].co for i in p.vertices]
    for ax in range(3):
        dims=[i for i in range(3) if i!=ax]
        q=[(v[dims[0]],v[dims[1]]) for v in pts]
        hits=[]
        for i in range(n):
            i2=(i+1)%n
            for j in range(i+1,n):
                j2=(j+1)%n
                if i==j or i2==j or j2==i: continue
                if proper(q[i],q[i2],q[j],q[j2]): hits.append((i,j))
        if hits: found.append((p.index,n,ax,hits[:10]))
print('NGON_SELF_INTERSECTIONS',len(found))
for x in found[:50]:
    idx,n,ax,h=x; p=me.polygons[idx]; c=obj.matrix_world@p.center
    print('FACE',idx,'N',n,'axis_dropped',ax,'center',tuple(round(v,4) for v in c),'hits',h)
