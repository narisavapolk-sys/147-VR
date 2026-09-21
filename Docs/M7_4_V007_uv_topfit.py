import bpy, json, numpy as np
from mathutils import Vector
blend=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007.blend'
out=r'C:\Users\mongo\UnityProjects\147 VR\Docs\M7_4_V007_uv_topfit.json'
bpy.ops.wm.open_mainfile(filepath=blend)
o=bpy.data.objects.get('TABLE SURFACE'); uv=o.data.uv_layers.active.data
rows=[]
for p in o.data.polygons:
    wn=(o.matrix_world.to_3x3()@p.normal).normalized()
    if wn.z < 0.9: continue
    for li in p.loop_indices:
        vi=o.data.loops[li].vertex_index
        w=o.matrix_world@o.data.vertices[vi].co
        q=uv[li].uv
        rows.append((w.x,w.y,w.z,q.x,q.y))

def fit(ai,aj,qi):
    A=np.array([[r[ai],r[aj],1] for r in rows]); b=np.array([r[qi] for r in rows])
    c=np.linalg.lstsq(A,b,rcond=None)[0]; e=float(np.sqrt(np.mean((A@c-b)**2)))
    return [float(x) for x in c],e
R={'top_loop_count':len(rows),'top_world_min':[min(r[i] for r in rows) for i in range(3)],'top_world_max':[max(r[i] for r in rows) for i in range(3)],'top_uv_min':[min(r[i] for r in rows) for i in (3,4)],'top_uv_max':[max(r[i] for r in rows) for i in (3,4)]}
for qi,n in [(3,'U'),(4,'V')]: R[n]={}
for qi,n in [(3,'U'),(4,'V')]:
    for axes,label in [((0,1),'XY'),((0,2),'XZ'),((1,2),'YZ')]: R[n][label]={'coeff':fit(*axes,qi)[0],'rmse':fit(*axes,qi)[1]}
open(out,'w').write(json.dumps(R,indent=2)); print(json.dumps(R,indent=2))
