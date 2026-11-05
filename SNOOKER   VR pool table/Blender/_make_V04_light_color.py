import bpy, os, shutil, hashlib

src = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V03.blend"
dst = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V04.blend"

# Open V03 as source
bpy.ops.wm.open_mainfile(filepath=src)

# Change ONLY Light.color on every actual LIGHT datablock.
target = (0.85, 0.90, 1.00)
lights = []
for obj in bpy.data.objects:
    if obj.type == 'LIGHT' and obj.data:
        obj.data.color = target
        lights.append((obj.name, obj.data.name))

# Save as V04; V03 remains untouched.
bpy.ops.wm.save_as_mainfile(filepath=dst)

print("V04_CREATED")
print("LIGHT_COUNT", len(lights))
for name, data_name in lights:
    print("LIGHT", name, "|", data_name, "|", target)
print("SAVED", dst)
