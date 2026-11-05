import bpy
p=r'''C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V04.blend'''
bpy.ops.wm.open_mainfile(filepath=p)
m=bpy.data.materials.get('FELT')
changed=False
if m and m.use_nodes:
 for n in m.node_tree.nodes:
  if n.type=='BSDF_PRINCIPLED' and 'Base Color' in n.inputs:
   c=n.inputs['Base Color'].default_value
   c[:3]=[x*0.5 for x in c[:3]]
   changed=True
bpy.ops.wm.save_as_mainfile(filepath=p)
print('FELT_DARKENED_2X', changed, tuple(round(x,6) for x in c[:3]))
