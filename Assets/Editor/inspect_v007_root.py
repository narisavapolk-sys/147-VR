import bpy
p='C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender/147VR_Table_WPBSA_Visual_Clean_v007.blend'
bpy.ops.wm.open_mainfile(filepath=p)
o=bpy.data.objects.get('147VR_TABLE_VISUAL_ROOT')
s=bpy.data.objects.get('TABLE SURFACE')
print('ROOT_NAME', o.name if o else None)
print('ROOT_SCALE', tuple(o.scale) if o else None)
print('ROOT_DIMS', tuple(o.dimensions) if o else None)
print('ROOT_PARENT', o.parent.name if o and o.parent else None)
print('SURFACE_SCALE', tuple(s.scale) if s else None)
print('SURFACE_DIMS', tuple(s.dimensions) if s else None)
