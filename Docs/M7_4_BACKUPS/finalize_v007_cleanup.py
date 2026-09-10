import bpy
src=r'C:\Users\mongo\UnityProjects\147 VR\Docs\M7_4_BACKUPS\147VR_Table_WPBSA_Visual_Clean_v006_work_20260903.blend'
outblend=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007.blend'
outfbx=r'C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\147VR_Table_WPBSA_Visual_Clean_v007.fbx'
bpy.ops.wm.open_mainfile(filepath=src)
for n in ('BALL Markings','D Marking'):
    o=bpy.data.objects.get(n)
    if o: bpy.data.objects.remove(o, do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=outblend)
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.context.scene.objects:
    if o.type=='MESH' and not o.name.startswith('ANCHOR_'):
        o.select_set(True)
if bpy.context.selected_objects:
    bpy.context.view_layer.objects.active=bpy.context.selected_objects[0]
bpy.ops.export_scene.fbx(filepath=outfbx,use_selection=True,object_types={'MESH'},apply_scale_options='FBX_SCALE_ALL',path_mode='AUTO')
scene=bpy.context.scene
scene.render.filepath=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\v007_live_hero_table.png'
scene.render.resolution_percentage=100
bpy.ops.render.render(write_still=True)
print('V007_FINALIZED',outblend)
print('V007_FBX',outfbx)
print('V007_MESH_COUNT',len([o for o in scene.objects if o.type=='MESH']))
