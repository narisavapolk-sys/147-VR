import bpy

def run():
    for o in bpy.context.scene.objects:
        o.select_set(False)
    sel=[o for o in bpy.context.scene.objects if o.type=='MESH' and not o.name.startswith('ANCHOR_')]
    for o in sel: o.select_set(True)
    if sel: bpy.context.view_layer.objects.active=sel[0]
    bpy.ops.export_scene.fbx(filepath=r'C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\147VR_Table_WPBSA_Visual_Clean_v007.fbx',use_selection=True,object_types={'MESH'},apply_scale_options='FBX_SCALE_ALL',path_mode='AUTO')
    bpy.context.scene.render.filepath=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\v007_live_hero_table.png'
    bpy.context.scene.render.resolution_percentage=100
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007.blend')
    print('V007_DELAYED_DONE',len(sel))
    return None
bpy.app.timers.register(run, first_interval=3.0)
