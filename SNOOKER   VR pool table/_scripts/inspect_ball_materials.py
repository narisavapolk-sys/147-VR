import bpy
p=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Base_v001.blend'
bpy.ops.wm.open_mainfile(filepath=p)
for o in bpy.context.scene.objects:
 if o.get('asset_role')=='SNOOKER_BALL':
  m=o.data.materials[0] if o.type=='MESH' and o.data.materials else None
  bs=m.node_tree.nodes.get('Principled BSDF') if m and m.use_nodes else None
  print(o.name, 'loc', tuple(round(v,5) for v in o.location), 'dim', tuple(round(v,5) for v in o.dimensions), 'mat',m.name if m else None,'base',tuple(round(v,4) for v in bs.inputs['Base Color'].default_value[:3]) if bs and 'Base Color' in bs.inputs else None)
