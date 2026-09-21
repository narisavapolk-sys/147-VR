import bpy, os, shutil
src=r"C:\Users\mongo\UnityProjects\147 VR\Assets\147 main\ConcertRoom\ConcertRoom_WithTable.fbx"
backup=r"C:\Users\mongo\UnityProjects\147 VR\Assets\147 main\ConcertRoom\Backups\ConcertRoom_WithTable_PRE_M7_4_ENV_ONLY_20260902.fbx"
os.makedirs(os.path.dirname(backup),exist_ok=True)
if not os.path.exists(backup): shutil.copy2(src,backup)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
keep=[o for o in bpy.context.scene.objects if o.name=='ConcertRoom_Floor']
for o in list(bpy.context.scene.objects):
    if o not in keep: bpy.data.objects.remove(o, do_unlink=True)
for o in keep: o.select_set(True)
bpy.context.view_layer.objects.active=keep[0] if keep else None
bpy.ops.export_scene.fbx(filepath=src, use_selection=True, object_types={'MESH'}, apply_unit_scale=True, bake_space_transform=False)
print('ENV_ONLY_EXPORT',src)
print('KEPT', [o.name for o in keep])
print('BACKUP',backup)
