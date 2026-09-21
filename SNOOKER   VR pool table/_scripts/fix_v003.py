import bpy, os
src=bpy.data.filepath
out=src.replace('147VR_Table_WPBSA_Visual_Clean_v002.blend','147VR_Table_WPBSA_Visual_Clean_v003.blend')
# remove the two cue sticks: the dedicated Cue-.01 collection contains both cue assemblies
c=bpy.data.collections.get('Cue-.01')
if c:
 for o in list(c.objects): bpy.data.objects.remove(o, do_unlink=True)
# exact table-center blue spot
blue=bpy.data.objects.get('Blue')
if blue: blue.location.x=0.0; blue.location.y=0.0
# enforce visible snooker ball materials
cols={'Red':(0.8,0.0,0.0,1),'Yellow':(0.95,0.75,0.02,1),'Green':(0.03,0.3,0.06,1),'Brown':(0.2,0.055,0.015,1),'Blue':(0.02,0.18,0.8,1),'Pink':(0.95,0.2,0.55,1),'Black':(0.006,0.006,0.006,1),'White':(0.92,0.92,0.92,1)}
for name,color in cols.items():
 m=bpy.data.materials.get('147VR_BALL_'+name) or bpy.data.materials.new('147VR_BALL_'+name); m.diffuse_color=color; m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=color; bs.inputs['Roughness'].default_value=.22
for o in bpy.data.collections.get('BALLS').objects if bpy.data.collections.get('BALLS') else []:
 key='White' if 'White' in o.name else next((k for k in cols if k in o.name),None)
 if key:
  m=bpy.data.materials['147VR_BALL_'+key]; o.data.materials.clear(); o.data.materials.append(m)
# preserve original; save corrected source
bpy.ops.wm.save_as_mainfile(filepath=out)
fbx='C:/Users/mongo/UnityProjects/147 VR/Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Base_v001.fbx'
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'MESH','EMPTY'}, use_mesh_modifiers=True, use_custom_props=True, path_mode='AUTO', embed_textures=True, bake_space_transform=False)
print('V003_SAVED',out); print('FBX_UPDATED',fbx); print('CUES_REMOVED',len(c.objects) if c else 0); print('BLUE',tuple(blue.location) if blue else None)
