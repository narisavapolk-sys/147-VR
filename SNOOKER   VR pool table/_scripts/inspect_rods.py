import bpy
p=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Base_v001.blend'
bpy.ops.wm.open_mainfile(filepath=p)
for o in bpy.context.scene.objects:
 if 'rod' in o.name.lower() or 'cue' in o.name.lower() or 'stick' in o.name.lower() or 'rest' in o.name.lower():
  print(o.name,o.type,tuple(round(v,3) for v in o.dimensions),tuple(round(v,3) for v in o.location))
