import bpy
print('BLEND',bpy.data.filepath)
for o in bpy.data.objects:
 if o.name in ['TABLE SURFACE','147VR_TABLE_VISUAL_ROOT'] or 'TABLE SURFACE' in o.name:
  print('OBJ',o.name,'parent',o.parent.name if o.parent else None,'loc',tuple(round(x,6) for x in o.location),'rotdeg',tuple(round(x*180/3.14159265,4) for x in o.rotation_euler),'scale',tuple(round(x,6) for x in o.scale),'det',round(o.matrix_world.to_3x3().determinant(),6))
  if o.type=='MESH':
   for u in o.data.uv_layers:
    xs=[d.uv.x for d in u.data]; ys=[d.uv.y for d in u.data]; print('UV',u.name,'bounds',min(xs),max(xs),min(ys),max(ys))
