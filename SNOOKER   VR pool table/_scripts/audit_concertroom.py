import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
path=r"C:\Users\mongo\UnityProjects\147 VR\Assets\147 main\ConcertRoom\ConcertRoom_WithTable.fbx"
bpy.ops.import_scene.fbx(filepath=path)
print('OBJECT_COUNT',len(bpy.context.scene.objects))
for o in bpy.context.scene.objects:
 print(o.type, repr(o.name), 'parent=',repr(o.parent.name if o.parent else None))
