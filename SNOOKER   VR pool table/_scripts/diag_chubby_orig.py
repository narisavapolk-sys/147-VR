"""READ-ONLY diagnosis of ORIGINAL Chubby magic girl.blend.

Reports each mesh piece's world-space bounding box so we can compare to the
exported FBX and see which pieces moved/scattered. Opens the file, never saves.
"""
import bpy, os, time

BASE = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table"
ORIG = os.path.join(BASE, "Blender", "Chubby magic girl.blend")

t0 = time.time()
def log(msg):
    print(f"[{time.time()-t0:6.1f}s] {msg}", flush=True)

log("open " + ORIG)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath=ORIG)

meshes = [o for o in bpy.data.objects if o.type == 'MESH']
log(f"total mesh objects: {len(meshes)}")
body = None
for o in bpy.data.objects:
    if o.name == 'chubby_body':
        body = o
        break

# world bbox helper
def world_bbox(o):
    mat = o.matrix_world
    xs, ys, zs = [], [], []
    for v in o.data.vertices:
        c = mat @ v.co
        xs.append(c.x); ys.append(c.y); zs.append(c.z)
    return (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))

blo, bhi = world_bbox(body) if body else ((0,0,0),(0,0,0))
bcen = ((blo[0]+bhi[0])/2, (blo[1]+bhi[1])/2, (blo[2]+bhi[2])/2)
log(f"BODY {body.name if body else '?'}: z={blo[2]:.3f}..{bhi[2]:.3f} height={bhi[2]-blo[2]:.3f} "
    f"center=({bcen[0]:.3f},{bcen[1]:.3f},{bcen[2]:.3f})")

for o in meshes:
    if o == body:
        continue
    lo, hi = world_bbox(o)
    cen = ((lo[0]+hi[0])/2, (lo[1]+hi[1])/2, (lo[2]+hi[2])/2)
    dim = (hi[0]-lo[0], hi[1]-lo[1], hi[2]-lo[2])
    dx, dy, dz = cen[0]-bcen[0], cen[1]-bcen[1], cen[2]-bcen[2]
    flag = ""
    if cen[2] > bhi[2] + 0.15 or cen[2] < blo[2] - 0.15:
        flag += "  <-- OUTSIDE body vertically"
    if (dx*dx+dy*dy)**0.5 > 0.4:
        flag += "  <-- OFFSET sideways"
    parent = o.parent.name if o.parent else None
    log(f"  MESH {o.name!r}: parent={parent} verts={len(o.data.vertices)} "
        f"center=({cen[0]:.3f},{cen[1]:.3f},{cen[2]:.3f}) z={lo[2]:.3f}..{hi[2]:.3f} "
        f"dims=({dim[0]:.2f},{dim[1]:.2f},{dim[2]:.2f}) d=({dx:+.3f},{dy:+.3f},{dz:+.3f}){flag}")

log("DIAG_ORIG_DONE")
