import bpy
print('HAS_BALL_MARKINGS',bpy.data.objects.get('BALL Markings') is not None)
print('HAS_D_MARKING',bpy.data.objects.get('D Marking') is not None)
print('MESH_COUNT',len([o for o in bpy.context.scene.objects if o.type=='MESH']))
