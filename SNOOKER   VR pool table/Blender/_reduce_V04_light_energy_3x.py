import bpy
src=r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V04.blend"
bpy.ops.wm.open_mainfile(filepath=src)
for o in bpy.data.objects:
    if o.type=='LIGHT' and o.data:
        o.data.energy /= 3.0
bpy.ops.wm.save_as_mainfile(filepath=src)
print('V04_SAVED', len([o for o in bpy.data.objects if o.type=='LIGHT']))
