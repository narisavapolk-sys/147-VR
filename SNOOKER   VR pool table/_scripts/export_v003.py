import bpy, math
for me in bpy.data.meshes:
 for uv in me.uv_layers:
  for d in uv.data:
   if not (math.isfinite(d.uv.x) and math.isfinite(d.uv.y)): d.uv=(0.0,0.0)
fbx='C:/Users/mongo/UnityProjects/147 VR/Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Base_v001.fbx'
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=False,object_types={'MESH','EMPTY'},use_mesh_modifiers=True,use_custom_props=True,path_mode='AUTO',embed_textures=True,bake_space_transform=False)
print('FBX_UPDATED',fbx)
print('CUE_COLLECTION_OBJECTS',len(bpy.data.collections.get('Cue-.01').objects) if bpy.data.collections.get('Cue-.01') else -1)
print('BALLS',len(bpy.data.collections.get('BALLS').objects))
print('BLUE',tuple(round(x,6) for x in bpy.data.objects['Blue'].location))
