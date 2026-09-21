import bpy, os, math
src=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Base_v001.blend'
out=r'C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\147VR_Table_WPBSA_Base_v001.fbx'
bpy.ops.wm.open_mainfile(filepath=src)
for o in list(bpy.context.scene.objects):
    if o.type in {'CAMERA','LIGHT'} or o.name=='Plane' or o.name=='Group_PremiumStudioLights': bpy.data.objects.remove(o, do_unlink=True)
root=bpy.data.objects.get('147VR_TABLE_VISUAL_ROOT')
if root is None: root=bpy.data.objects.new('147VR_TABLE_VISUAL_ROOT',None); bpy.context.scene.collection.objects.link(root)
for o in list(bpy.context.scene.objects):
    if o is not root and o.type=='MESH' and o.parent is None: o.parent=root
uv_fixed=0
for me in bpy.data.meshes:
    for uv_layer in me.uv_layers:
        for uv in uv_layer.data:
            if not (math.isfinite(uv.uv.x) and math.isfinite(uv.uv.y)): uv.uv=(0.0,0.0); uv_fixed+=1
bpy.ops.object.select_all(action='DESELECT'); root.select_set(True); bpy.context.view_layer.objects.active=root
for o in bpy.context.scene.objects:
    if o.type=='MESH': o.select_set(True)
os.makedirs(os.path.dirname(out),exist_ok=True)
bpy.ops.export_scene.fbx(filepath=out,use_selection=True,object_types={'EMPTY','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='AUTO',embed_textures=False)
print('EXPORTED',out); print('MESH_COUNT',sum(1 for o in bpy.context.scene.objects if o.type=='MESH')); print('BALL_COUNT',sum(1 for o in bpy.context.scene.objects if o.get('asset_role')=='SNOOKER_BALL')); print('UV_FIXED',uv_fixed)
