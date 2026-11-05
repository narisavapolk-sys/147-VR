import bpy
P=r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V01.blend"
bpy.ops.wm.open_mainfile(filepath=P)
print("ENGINE", bpy.context.scene.render.engine)
print("RES", bpy.context.scene.render.resolution_x, bpy.context.scene.render.resolution_y, bpy.context.scene.render.resolution_percentage)
print("WORLD", bpy.context.scene.world.name if bpy.context.scene.world else None)
print("CAMERAS")
for o in bpy.data.objects:
    if o.type=="CAMERA": print(o.name, tuple(round(v,3) for v in o.location), tuple(round(v,3) for v in o.rotation_euler), "lens",o.data.lens)
print("LIGHTS")
for o in bpy.data.objects:
    if o.type=="LIGHT":
        print(o.name,o.data.type,"loc",tuple(round(v,3) for v in o.location),"energy",o.data.energy,"color",tuple(round(v,3) for v in o.data.color))
