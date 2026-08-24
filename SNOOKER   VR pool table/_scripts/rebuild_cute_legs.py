"""Rebuild the missing legs on Cute Girl 5.2 body (thigh-to-ankle gap).

Original model has torso starting at z=0.708 (mid-thigh) and feet at
z 0.004..0.170 — the knee/thigh span is empty. We loft cylinders per leg
from the thigh cross-section (z 0.71, r 0.067) down to the ankle
(z 0.17, r 0.030), then join into the body mesh, slim, save and export.
"""
import bpy, bmesh
from mathutils import Vector
import math

BASE = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Blender"
OUT = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/FBX"

bpy.ops.wm.open_mainfile(filepath=BASE + r"/Cute Girl 5.2.blend")
scene = bpy.context.scene

# ---- remove clothes ----
for name in ["bikini", "boot", "jacket", "pant", "sock", "top"]:
    ob = bpy.data.objects.get(name)
    if ob:
        bpy.data.objects.remove(ob, do_unlink=True)
        print("removed:", name)

body = bpy.data.objects["body"]
bpy.context.view_layer.objects.active = body
bpy.ops.object.mode_set(mode='EDIT')

# ---- loft one leg with bmesh ----
SEG = 32          # ring resolution
HEIGHTS = [       # (z, radius, xoff, yoff)  — interpolated down the leg
    (0.78, 0.062, 0.084, -0.004),   # inside hip (hidden in torso)
    (0.71, 0.067, 0.084, -0.002),   # thigh top (matches existing cross-section)
    (0.60, 0.062, 0.082, -0.012),
    (0.48, 0.052, 0.080, -0.020),   # knee
    (0.38, 0.045, 0.079, -0.028),   # calf upper
    (0.28, 0.038, 0.078, -0.033),   # calf lower
    (0.20, 0.032, 0.078, -0.036),   # ankle
    (0.15, 0.030, 0.078, -0.038),   # into foot (hidden)
]


def make_leg(sign):
    """sign = +1 for right leg (x>0), -1 for left (x<0)."""
    verts = []
    for (z, r, xo, yo) in HEIGHTS:
        ring = []
        for i in range(SEG):
            a = 2 * math.pi * i / SEG
            ring.append((sign * xo + r * math.cos(a), yo + r * math.sin(a), z))
        verts.append(ring)

    bm = bmesh.new()
    for ring in verts:
        for (x, y, z) in ring:
            bm.verts.new((x, y, z))
    bm.verts.ensure_lookup_table()
    idx = 0
    for ri in range(len(verts) - 1):
        for i in range(SEG):
            i2 = (i + 1) % SEG
            v00 = bm.verts[idx + i]
            v01 = bm.verts[idx + i2]
            v10 = bm.verts[idx + SEG + i]
            v11 = bm.verts[idx + SEG + i2]
            bm.faces.new((v00, v01, v11, v10))
        idx += SEG
    bm.faces.ensure_lookup_table()
    return bm


# add both legs into body mesh (edit mode already active on body)
bpy.ops.mesh.select_all(action='DESELECT')

for sign in (-1, 1):
    bm = bmesh.from_edit_mesh(body.data)
    leg = make_leg(sign)
    for v in leg.verts:
        bm.verts.new(v.co)
    bm.verts.ensure_lookup_table()
    # build faces for this leg (new verts are at the end)
    n_new = len(leg.verts)
    start = len(bm.verts) - n_new
    rows = len(HEIGHTS)
    for ri in range(rows - 1):
        for i in range(SEG):
            i2 = (i + 1) % SEG
            a = start + ri * SEG + i
            b_ = start + ri * SEG + i2
            c = start + (ri + 1) * SEG + i
            d = start + (ri + 1) * SEG + i2
            new_face = bm.faces.new((bm.verts[a], bm.verts[b_], bm.verts[d], bm.verts[c]))
            new_face.smooth = True
    leg.free()

bmesh.update_edit_mesh(body.data)
bpy.ops.object.mode_set(mode='OBJECT')
print("legs added to body, verts now:", len(body.data.vertices))

# ---- slim (same height profile as slim_girls.py) ----
PROFILE = [
    (0.00, 1.00), (0.06, 0.92), (0.15, 0.85), (0.28, 0.82), (0.38, 0.78),
    (0.48, 0.72), (0.56, 0.66), (0.65, 0.70), (0.75, 0.76), (0.83, 0.82),
    (0.88, 0.88), (0.93, 0.96), (1.00, 1.00),
]


def interp(profile, t):
    if t <= profile[0][0]:
        return profile[0][1]
    if t >= profile[-1][0]:
        return profile[-1][1]
    for i in range(len(profile) - 1):
        t0, f0 = profile[i]
        t1, f1 = profile[i + 1]
        if t0 <= t <= t1:
            return f0 + (f1 - f0) * (t - t0) / (t1 - t0)
    return 1.0


bm = bmesh.new()
bm.from_mesh(body.data)
zs = [v.co.z for v in bm.verts]
zmin, zmax = min(zs), max(zs)
for v in bm.verts:
    t = (v.co.z - zmin) / (zmax - zmin)
    f = interp(PROFILE, t)
    v.co.x *= f
    v.co.y *= f
bm.to_mesh(body.data)
bm.free()
print("slimmed body")

# ---- clean unused ----
for block in bpy.data.meshes:
    if block.users == 0:
        bpy.data.meshes.remove(block)
for block in bpy.data.materials:
    if block.users == 0:
        bpy.data.materials.remove(block)

out_blend = BASE + r"/Cute Girl 5.2 SLIM.blend"
bpy.ops.wm.save_as_mainfile(filepath=out_blend)
print("saved:", out_blend)

bpy.ops.export_scene.fbx(
    filepath=OUT + r"/Cute Girl SLIM.fbx",
    use_selection=False,
    object_types={'MESH'},
    apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True,
)
print("exported:", OUT + r"/Cute Girl SLIM.fbx")
print("ALL DONE")
