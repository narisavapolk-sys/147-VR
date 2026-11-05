import bpy
p=r'''C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V04.blend'''
bpy.ops.wm.open_mainfile(filepath=p)
lights=[o for o in bpy.data.objects if o.type=='LIGHT']
for o in lights: o.data.energy /= 2.0
bpy.ops.wm.save_as_mainfile(filepath=p)
print('V04_LIGHT_ENERGY_HALVED', len(lights))
