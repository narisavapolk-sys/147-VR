"""READ-ONLY diagnosis: analyze per-material vertex positions in Chubby Girl Dancing.fbx.

Finds which pieces (hat/hair/shoes/clothes) ended up scattered from the body
after rig deletion + scale in the earlier export. Does NOT modify anything.
"""
import bpy, os, time, math

BASE = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table"
FBX = os.path.join(BASE, "FBX", "Chubby Girl Dancing.fbx")

t0 = time.time()
def log(msg):
    print(f"[{time.time()-t0:6.1f}s] {msg}", flush=True)

log("open " + FBX)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)

meshes = [o for o in bpy.data.objects if o.type == 'MESH']
log(f"meshes: {[m.name for m in meshes]}")
rigs = [o for o in bpy.data.objects if o.type == 'ARMATURE']
log(f"armatures: {[r.name for r in rigs]} bones={[len(r.data.bones) for r in rigs]}")

for m in meshes:
    mats = m.data.materials
    log(f"--- MESH {m.name}: verts={len(m.data.vertices)} mats={len(mats)} ---")
    # world matrix (import usually identity)
    mat = m.matrix_world
    by_mat = {}
    for v in m.data.vertices:
        i = v.index
        # material index from loops
        for loop in m.data.loops:
            pass
        break
    # build per-material vertex sets from polygons
    mat_of_vert = {}
    for p in m.data.polygons:
        mi = p.material_index
        for li in p.loop_indices:
            vi = m.data.loops[li].vertex_index
            mat_of_vert[vi] = mi
    per_mat = {}
    for vi, mi in mat_of_vert.items():
        per_mat.setdefault(mi, []).append(vi)
    # compute bbox per material
    body_box = None
    for mi in sorted(per_mat):
        vs = per_mat[mi]
        xs, ys, zs = [], [], []
        for vi in vs:
            c = mat @ m.data.vertices[vi].co
            xs.append(c.x); ys.append(c.y); zs.append(c.z)
        lo = (min(xs), min(ys), min(zs))
        hi = (max(xs), max(ys), max(zs))
        cen = ((lo[0]+hi[0])/2, (lo[1]+hi[1])/2, (lo[2]+hi[2])/2)
        dim = (hi[0]-lo[0], hi[1]-lo[1], hi[2]-lo[2])
        nm = mats[mi].name if mi < len(mats) and mats[mi] else f"mat{mi}"
        log(f"  mat[{mi}] {nm!r}: verts={len(vs)} center=({cen[0]:.3f},{cen[1]:.3f},{cen[2]:.3f}) "
            f"z-range=({lo[2]:.3f}..{hi[2]:.3f}) dims=({dim[0]:.3f},{dim[1]:.3f},{dim[2]:.3f})")
        if 'body' in nm.lower() or 'skin' in nm.lower():
            body_box = (lo, hi, cen)
    if body_box:
        blo, bhi, bcen = body_box
        body_h = bhi[2] - blo[2]
        log(f"  BODY: z={blo[2]:.3f}..{bhi[2]:.3f} height={body_h:.3f}")
        for mi in sorted(per_mat):
            vs = per_mat[mi]
            xs, ys, zs = [], [], []
            for vi in vs:
                c = mat @ m.data.vertices[vi].co
                xs.append(c.x); ys.append(c.y); zs.append(c.z)
            lo = (min(xs), min(ys), min(zs))
            hi = (max(xs), max(ys), max(zs))
            cen = ((lo[0]+hi[0])/2, (lo[1]+hi[1])/2, (lo[2]+hi[2])/2)
            nm = mats[mi].name if mi < len(mats) and mats[mi] else f"mat{mi}"
            dz = cen[2] - bcen[2]
            dx = cen[0] - bcen[0]
            dy = cen[1] - bcen[1]
            flag = ""
            # piece center beyond body vertical extent -> scattered
            if cen[2] > bhi[2] + 0.1 or cen[2] < blo[2] - 0.1:
                flag += "  <-- SCATTERED (vertical)"
            if math.hypot(dx, dy) > 0.35:
                flag += "  <-- OFFSET sideways"
            log(f"  vs BODY: {nm!r} dxyz=({dx:+.3f},{dy:+.3f},{dz:+.3f}){flag}")

# also check any loose/empty objects
for o in bpy.data.objects:
    if o.type not in ('MESH', 'ARMATURE'):
        log(f"  OTHER: {o.type} {o.name} loc={tuple(round(x,3) for x in o.location)}")

log("DIAG_DONE")
