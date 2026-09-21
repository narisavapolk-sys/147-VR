import bpy
for f in ['147VR_Table_WPBSA_Visual_Clean_v007.blend','147VR_Table_WPBSA_Visual_Clean_v007_MARKING.blend']:
 bpy.ops.wm.open_mainfile(filepath='C:\\Users\\mongo\\UnityProjects\\147 VR\\SNOOKER   VR pool table\\Blender\\'+f)
 o=bpy.data.objects.get('TABLE SURFACE'); print('FILE',f,'dims',tuple(round(x,6) for x in o.dimensions),'bounds',[(round(min(v[i] for v in o.bound_box),6),round(max(v[i] for v in o.bound_box),6)) for i in range(3)],'scale',tuple(round(x,6) for x in o.scale))
