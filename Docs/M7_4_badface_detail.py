import bpy
obj=bpy.data.objects.get('TABLE FRAME'); me=obj.data
for idx in [32,123,236,262,797,1025,1056,1147]:
 p=me.polygons[idx]; c=obj.matrix_world@p.center
 print('FACE',idx,'verts',len(p.vertices),'mat',p.material_index,'center',tuple(round(v,5) for v in c),'area',p.area,'normal',tuple(round(v,4) for v in (obj.matrix_world.to_3x3()@p.normal)))
 print('  vertex_ids',list(p.vertices)[:120])
