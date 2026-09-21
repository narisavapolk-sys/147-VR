import bpy
p=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Base_v001.blend'
bpy.ops.wm.open_mainfile(filepath=p)
print('OBJECTS')
for o in bpy.context.scene.objects:
    role=o.get('asset_role','')
    if role=='SNOOKER_BALL' or any(k in o.name.lower() for k in ['cue','rod','stick','rest']):
        print(o.name, role, tuple(round(v,6) for v in o.location), 'MATS', [m.name if m else None for m in getattr(o,'data',[]).materials] if o.type=='MESH' else [])
print('BALLS',sum(1 for o in bpy.context.scene.objects if o.get('asset_role')=='SNOOKER_BALL'))
