import bpy, json, os
path = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_REPAIR_02.blend"
bpy.ops.wm.open_mainfile(filepath=path)
print("SCENE", bpy.context.scene.name)
print("COLLECTIONS")
for c in bpy.data.collections:
    print("COL", c.name, "objs", len(c.objects))
print("OBJECTS")
for o in bpy.context.scene.objects:
    if any(k in o.name.upper() for k in ["TABLE","WPBSA","SPOT_","BALL","ANCHOR","COLLIDER"]):
        print(o.name, "type", o.type, "loc", tuple(round(v,5) for v in o.location), "dim", tuple(round(v,5) for v in o.dimensions), "parent", o.parent.name if o.parent else "")
