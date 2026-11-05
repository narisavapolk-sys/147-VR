import bpy, os
P=r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V01.blend"
OUT=r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\LIGHTING_CHECK"
bpy.ops.wm.open_mainfile(filepath=P)
sc=bpy.context.scene
sc.render.engine='BLENDER_EEVEE'
sc.render.resolution_percentage=70
sc.render.image_settings.file_format='PNG'
sc.render.film_transparent=False
sc.render.image_settings.color_mode='RGBA'
for camname, suffix in [('Camera','player'),('V006_Pocket_Hero_Camera','hero')]:
    cam=bpy.data.objects.get(camname)
    if not cam: continue
    sc.camera=cam
    sc.render.filepath=os.path.join(OUT+'_'+suffix+'.png')
    bpy.ops.render.render(write_still=True)
    print("RENDERED",sc.render.filepath)
