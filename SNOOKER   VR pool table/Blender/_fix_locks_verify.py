import bpy, json, hashlib
P=r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V01.blend"
V=r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v01_LOCK_VERIFY.json"
bpy.ops.wm.open_mainfile(filepath=P)
def prot(o):
    n=o.name.upper()
    keys=["TABLE","WPBSA","SPOT_","BALL","ANCHOR","CATCHER","CUSHION","POCKET PAD"]
    cols=["147VR_POCKET_ART","147VR_SPATIAL_ANCHORS","BALLS","CUSHIONS","LEGS","BASE TABLE SUPPORTS","POCKET PADS","MARKINGS"]
    return o.type in {"MESH","EMPTY"} and (any(k in n for k in keys) or any(c.name.upper() in cols for c in o.users_collection))
for o in bpy.context.scene.objects:
    if prot(o):
        o.lock_location=(True,True,True); o.lock_rotation=(True,True,True); o.lock_scale=(True,True,True)
bpy.ops.wm.save_as_mainfile(filepath=P)
bpy.ops.wm.open_mainfile(filepath=P)
bad=[]
for o in bpy.context.scene.objects:
    if prot(o) and not (tuple(o.lock_location)==(True,True,True) and tuple(o.lock_rotation)==(True,True,True) and tuple(o.lock_scale)==(True,True,True)):
        bad.append(o.name)
report={"bad_locks":bad,"protected_count":sum(1 for o in bpy.context.scene.objects if prot(o)),"sha256":hashlib.sha256(open(P,"rb").read()).hexdigest()}
with open(V,"w",encoding="utf-8") as f: json.dump(report,f,indent=2)
print(report)
