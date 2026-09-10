import bpy
for n in ('BALL Markings','D Marking'):
    o=bpy.data.objects.get(n)
    if o: o.hide_render=True
out=r'C:\Users\mongo\UnityProjects\147 VR\Docs\M7_4_BACKUPS\v006_no_markings_test.png'
bpy.context.scene.render.filepath=out
bpy.context.scene.render.resolution_percentage=50
bpy.ops.render.render(write_still=True)
print('TEST_RENDER',out)
