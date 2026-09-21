import bpy, sys
p=bpy.data.filepath
print('BLEND',p)
for n in ['TABLE SURFACE','147VR_TABLE_VISUAL_ROOT']:
 o=bpy.data.objects.get(n)
 if o: print('OBJ',n,'parent',o.parent.name if o.parent else None,'loc',tuple(round(x,6) for x in o.location),'rotdeg',tuple(round(x*180/3.14159265,4) for x in o.rotation_euler),'scale',tuple(round(x,6) for x in o.scale),'det',round(o.matrix_world.to_3x3().determinant(),6))
