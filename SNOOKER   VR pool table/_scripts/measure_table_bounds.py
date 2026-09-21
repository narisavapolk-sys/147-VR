import bpy
from mathutils import Vector
p=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Base_v001.blend'
bpy.ops.wm.open_mainfile(filepath=p)
mins=[1e9,1e9,1e9]; maxs=[-1e9,-1e9,-1e9]
for o in bpy.context.scene.objects:
    if o.type!='MESH' or o.name=='Plane': continue
    for c in o.bound_box:
        w=o.matrix_world @ Vector(c)
        for i in range(3): mins[i]=min(mins[i],w[i]); maxs[i]=max(maxs[i],w[i])
print('TABLE_BOUNDS',mins,maxs,'SIZE',[maxs[i]-mins[i] for i in range(3)])
print('BALL_CENTER_Z',sorted([round(o.location.z,6) for o in bpy.context.scene.objects if o.get('asset_role')=='SNOOKER_BALL'])[:3])
