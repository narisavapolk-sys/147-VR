import bpy
src=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007.blend'
outfbx=r'C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\147VR_Table_WPBSA_Visual_Clean_v007.fbx'
bpy.ops.wm.open_mainfile(filepath=src)
for o in bpy.context.scene.objects: o.select_set(False)
selected=[]
for o in bpy.context.scene.objects:
    if o.type=='MESH' and not o.name.startswith('ANCHOR_'):
        o.select_set(True); selected.append(o)
if selected: bpy.context.view_layer.objects.active=selected[0]
bpy.ops.export_scene.fbx(filepath=outfbx,use_selection=True,object_types={'MESH'},apply_scale_options='FBX_SCALE_ALL',path_mode='AUTO')
bpy.context.scene.render.filepath=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\v007_live_hero_table.png'
bpy.context.scene.render.resolution_percentage=100
bpy.ops.render.render(write_still=True)
print('V007_EXPORT_OK',len(selected),outfbx)
