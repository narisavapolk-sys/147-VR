import bpy, json, math
from mathutils import Vector
blend=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007.blend'
out=r'C:\Users\mongo\UnityProjects\147 VR\Docs\M7_4_V007_uv_fit.json'
bpy.ops.wm.open_mainfile(filepath=blend)
o=bpy.data.objects.get('TABLE SURFACE')
if not o or o.type!='MESH': raise RuntimeError('TABLE SURFACE missing')
uv=o.data.uv_layers.active.data
verts=o.data.vertices
pairs=[]
for poly in o.data.polygons:
    for li in poly.loop_indices:
        vi=o.data.loops[li].vertex_index
        w=o.matrix_world @ verts[vi].co
        u=uv[li].uv
        pairs.append((w.x,w.y,w.z,u.x,u.y))
# Fit u/v against candidate planar axes using least squares.
def fit(axis_i, axis_j, out_i):
    # affine q = a*x+b*y+c
    A=[]; B=[]
    for p in pairs:
        A.append([p[axis_i],p[axis_j],1.0]); B.append(p[out_i])
    import numpy as np
    X=np.array(A,float); y=np.array(B,float)
    coef=np.linalg.lstsq(X,y,rcond=None)[0]
    err=np.sqrt(np.mean((X@coef-y)**2))
    return [float(x) for x in coef],float(err)
result={'blend':blend,'object':'TABLE SURFACE','uv_layer':o.data.uv_layers.active.name,'vertex_count':len(verts),'polygon_count':len(o.data.polygons)}
for uv_i,name in [(3,'U'),(4,'V')]:
    result[name]={}
    for axes,label in [((0,1),'XY'),((0,2),'XZ'),((1,2),'YZ')]:
        c,e=fit(*axes,uv_i); result[name][label]={'coeff':[round(x,12) for x in c],'rmse':e}
result['world_bounds']={'min':[min(p[i] for p in pairs) for i in range(3)],'max':[max(p[i] for p in pairs) for i in range(3)]}
result['uv_bounds']={'min':[min(p[i] for p in pairs) for i in (3,4)],'max':[max(p[i] for p in pairs) for i in (3,4)]}
open(out,'w',encoding='utf-8').write(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
