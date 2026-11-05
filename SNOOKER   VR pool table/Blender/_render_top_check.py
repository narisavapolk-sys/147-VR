import bpy, os
from mathutils import Vector
P=r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V01.blend"
O=r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\LIGHTING_CHECK_top.png"
bpy.ops.wm.open_mainfile(filepath=P)
sc=bpy.context.scene
surf=bpy.data.objects["TABLE SURFACE"]
bb=[surf.matrix_world @ Vector(c) for c in surf.bound_box]
minx,maxx=min(p.x for p in bb),max(p.x for p in bb); miny,maxy=min(p.y for p in bb),max(p.y for p in bb); maxz=max(p.z for p in bb)
cx=(minx+maxx)/2; cy=(miny+maxy)/2
d=bpy.data.cameras.new("TEMP_TOP"); cam=bpy.data.objects.new("TEMP_TOP",d); bpy.context.scene.collection.objects.link(cam)
cam.location=(cx,cy,maxz+5.0); cam.rotation_euler=(0,0,0); cam.rotation_euler=(Vector((cx,cy,maxz))-cam.location).to_track_quat('-Z','Y').to_euler(); d.type='ORTHO'; d.ortho_scale=4.15
sc.camera=cam; sc.render.engine='BLENDER_EEVEE'; sc.render.resolution_x=1000; sc.render.resolution_y=650; sc.render.resolution_percentage=100; sc.render.image_settings.file_format='PNG'; sc.render.filepath=O
bpy.ops.render.render(write_still=True)
print("TOP_RENDER",O)
