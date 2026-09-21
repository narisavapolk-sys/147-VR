import bpy
p=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Base_v001.blend'
bpy.ops.wm.open_mainfile(filepath=p)
for o in bpy.context.scene.objects:
 if o.type=='MESH' and max(o.dimensions)>0.8:
  print(o.name,tuple(round(v,3) for v in o.dimensions),[m.name if m else None for m in o.data.materials])
