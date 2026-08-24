import bpy
base = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"
bpy.ops.wm.open_mainfile(filepath=base + r"/Chubby magic girl.blend")
print("NON-WGT OBJECTS:")
for o in bpy.data.objects:
    if "WGT-" in o.name:
        continue
    mods = ", ".join(m.type for m in o.modifiers) if o.type == 'MESH' else ""
    print(f"  {o.type:8s} {o.name:40s} mods=[{mods}]")
