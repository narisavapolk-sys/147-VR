import bpy
print('CAMERAS')
for o in bpy.data.objects:
 if o.type=='CAMERA': print(o.name)
print('COLLECTIONS')
for c in bpy.data.collections: print(c.name,len(c.objects))
scene=bpy.context.scene
scene.render.engine='BLENDER_WORKBENCH'
scene.render.resolution_x=1200; scene.render.resolution_y=800; scene.render.resolution_percentage=100
scene.render.filepath='C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender/v002_audit.png'
if scene.camera: bpy.ops.render.render(write_still=True); print('RENDERED',scene.render.filepath)
