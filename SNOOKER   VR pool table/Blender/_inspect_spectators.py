import bpy
for c in bpy.data.collections:
    print("COLLECTION", c.name)
print("OBJECTS")
for o in bpy.data.objects:
    n=o.name.lower()
    if any(k in n for k in ["spect","seat","chair","audien","crowd","viewer","tier","carpet","sofa"]):
        print("OBJ", o.name, o.type, tuple(round(x,3) for x in o.location), tuple(round(x,3) for x in o.rotation_euler), "PARENT", o.parent.name if o.parent else "")
