import bpy
import json
p=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\REVISED_Snooker_Table_WPBSA_GOLD.blend'
bpy.ops.wm.open_mainfile(filepath=p)
out={'objects':[]}
for o in bpy.context.scene.objects:
    if o.type=='MESH':
        out['objects'].append({'name':o.name,'dims':list(o.dimensions),'loc':list(o.location),'rot':list(o.rotation_euler),'verts':len(o.data.vertices),'faces':len(o.data.polygons),'mats':[m.name if m else None for m in o.data.materials]})
    else:
        out['objects'].append({'name':o.name,'type':o.type,'loc':list(o.location)})
print(json.dumps(out,indent=2))
