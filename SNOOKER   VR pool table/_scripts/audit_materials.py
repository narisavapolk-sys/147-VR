import bpy
for n in ['Red','Yellow','Green','Brown','Blue','Pink','Black','Material','Material.001','green']:
 m=bpy.data.materials.get(n); print('MAT',n, 'exists',bool(m))
 if m and m.use_nodes:
  for node in m.node_tree.nodes:
   if node.type=='BSDF_PRINCIPLED': print(' BASE',tuple(round(v,3) for v in node.inputs['Base Color'].default_value),'ROUGH',node.inputs['Roughness'].default_value)
