import bpy
from mathutils import Vector
base = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"

def meas(path, body_name):
    bpy.ops.wm.open_mainfile(filepath=path)
    o = bpy.data.objects.get(body_name)
    if not o or o.type != 'MESH':
        print(body_name, "NOT FOUND"); return
    bbox = [o.matrix_world @ Vector(v) for v in o.bound_box]
    mn = Vector((min(v[i] for v in bbox) for i in range(3)))
    mx = Vector((max(v[i] for v in bbox) for i in range(3)))
    print(f"{body_name}: origin={tuple(round(x,3) for x in o.location)} bbox_min={tuple(round(x,3) for x in mn)} bbox_max={tuple(round(x,3) for x in mx)}")
    print(f"  size={tuple(round(mx[i]-mn[i],3) for i in range(3))} verts={len(o.data.vertices)}")
    # sample slices: centroid x,y at heights
    deps = bpy.context.evaluated_depsgraph_get()
    ev = o.evaluated_get(deps)
    mesh = ev.to_mesh()
    print("  verts(evaluated):", len(mesh.vertices))
    zmin, zmax = mn.z, mx.z
    for f in (0.1, 0.25, 0.4, 0.55, 0.7, 0.85, 0.95):
        z = zmin + f*(zmax-zmin)
        vs = [v.co for v in mesh.vertices if abs(v.co.z - z) < 0.05]
        if vs:
            c = Vector((0,0,0))
            for v in vs: c += v
            c /= len(vs)
            r = sum((v-c).length for v in vs)/len(vs)
            print(f"  z={z:.3f} ({f:.2f}): n={len(vs)} cx={c.x:.3f} cy={c.y:.3f} avg_r={r:.3f}")
    ev.to_mesh_clear()

meas(base + r"/Cute Girl 5.2.blend", "body")
meas(base + r"/Chubby magic girl.blend", "chubby_body")
