import bpy
obj=bpy.data.objects.get('TABLE FRAME'); print('MATERIALS',[(i,m.name if m else None) for i,m in enumerate(obj.data.materials)])
for name in ['TABLE FRAME','TABLE SURFACE']:
 o=bpy.data.objects.get(name)
 if o: print(name,'dims',tuple(round(v,4) for v in o.dimensions),'loc',tuple(round(v,4) for v in o.location))
