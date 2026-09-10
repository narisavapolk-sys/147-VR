import bpy, math
obj=bpy.data.objects.get('TABLE SURFACE'); me=obj.data; uv=me.uv_layers.active.data
pts=[('Yellow',-.330263,-1.019668),('Green',-.000329,-1.019668),('Brown',.329844,-1.019668),('Blue',0,0),('Pink',0,.859162),('Black',0,1.434759)]
for name,x,y in pts:
 best=(1e9,None,None)
 for poly in me.polygons:
  vs=poly.vertices
  for i in range(1,len(vs)-1):
   ids=(vs[0],vs[i],vs[i+1]); a=me.vertices[ids[0]].co; b=me.vertices[ids[1]].co; c=me.vertices[ids[2]].co
   ax,ay=a.x,a.y; bx,by=b.x,b.y; cx,cy=c.x,c.y; den=(by-cy)*(ax-cx)+(cx-bx)*(ay-cy)
   if abs(den)<1e-12: continue
   w0=((by-cy)*(x-cx)+(cx-bx)*(y-cy))/den; w1=((cy-ay)*(x-cx)+(ax-cx)*(y-cy))/den; w2=1-w0-w1
   if w0>=0 and w1>=0 and w2>=0:
    q=uv[ids[0]].uv*w0+uv[ids[1]].uv*w1+uv[ids[2]].uv*w2; best=(0,ids,q); break
  if best[1]: break
 if not best[1]:
  continue
 print(name,'UV',tuple(round(float(v),6) for v in best[2]),'expectedDirect',(round(.5+x/3.569,6),round(.5+y/1.778,6)))
