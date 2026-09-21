import bpy
print('LONG MESHES')
for o in bpy.context.scene.objects:
 if o.type=='MESH':
  d=o.dimensions
  if max(d)>0.9 and min(d)<0.12:
   print(o.name, 'dim=',tuple(round(x,3) for x in d),'loc=',tuple(round(x,3) for x in o.matrix_world.translation))
