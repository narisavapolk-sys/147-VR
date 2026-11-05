import bpy
p=r'''C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V04.blend'''
bpy.ops.wm.open_mainfile(filepath=p)
for m in bpy.data.materials:
 if m.use_nodes:
  for n in m.node_tree.nodes:
   if n.type=='BSDF_PRINCIPLED' and 'Base Color' in n.inputs:
    c=n.inputs['Base Color'].default_value
    if c[1]>c[0]*1.15 and c[1]>c[2]*1.1: print(m.name, tuple(round(x,3) for x in c[:3]))
