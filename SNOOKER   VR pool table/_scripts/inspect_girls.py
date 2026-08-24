import bpy, sys

def inspect(path, label):
    print(f"\n===== {label} =====")
    try:
        bpy.ops.wm.open_mainfile(filepath=path)
    except Exception as e:
        print("OPEN FAIL:", e)
        return
    print("SCENE:", bpy.context.scene.name)
    print(f"OBJECTS ({len(bpy.data.objects)}):")
    for o in bpy.data.objects:
        mods = ", ".join(m.type for m in o.modifiers) if o.type == 'MESH' else ""
        mats = ", ".join(m.name for m in o.data.materials) if o.type == 'MESH' and o.data else ""
        print(f"  {o.type:8s} {o.name:40s} mods=[{mods}] mats=[{mats[:80]}]")
    print("ARMATURES:", [a.name for a in bpy.data.armatures])
    print("ACTIONS:", [a.name for a in bpy.data.actions][:20])

base = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"
inspect(base + r"/Chubby magic girl.blend", "Chubby magic girl")
