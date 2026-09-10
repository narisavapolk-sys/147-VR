import bpy
obj=bpy.data.objects.get('TABLE SURFACE'); me=obj.data; uv=me.uv_layers.get('MarkingUV').data
pts=[('Y',-1.019668,-.330263),('G',-1.019668,-.000329),('Br',-1.019668,.329844),('Bl',0,0),('P',.859162,0),('K',1.434759,0)]
for name,x,y in pts:
 best=(1e9,None)
 for poly in me.polygons:
  ids=list(poly.vertices)
  for j in range(1,len(ids)-1):
   a,b,c=ids[0],ids[j],ids[j+1]; A=me.vertices[a].co;B=me.vertices[b].co;C=me.vertices[c].co; den=(B.y-C.y)*(A.x-C.x)+(C.x-B.x)*(A.y-C.y)
   if abs(den)<1e-12: continue
   w0=((B.y-C.y)*(x-C.x)+(C.x-B.x)*(y-C.y))/den; w1=((C.y-A.y)*(x-C.x)+(A.x-C.x)*(y-C.y))/den; w2=1-w0-w1
   if w0>=0 and w1>=0 and w2>=0:
    q=uv[a].uv*w0+uv[b].uv*w1+uv[c].uv*w2; best=(0,q); break
  if best[1] is not None: break
 if best[1] is None: print(name,'OUTSIDE'); continue
 print(name,'uv',tuple(round(float(v),6) for v in best[1]),'expectedGenUV',round(.5+x/3.569,6),round(.5+y/1.778,6))
