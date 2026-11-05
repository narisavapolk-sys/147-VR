# =============================================================================
#  147 VR  -  AAA Aristocrat Snooker Table
#  PATCH v3.1  :  LEGS + GOLD SKIRT + SIGHT BAND
#  v3.1 fix: removed mesh.use_auto_smooth (deleted from Blender 4.1+ / 5.x)
# =============================================================================
#  HOW TO RUN
#    1) Run 147VR_AAA_Aristocrat_SnookerTable.py  (v2)  -> table exists
#    2) Blender > Scripting > Open this file > Run Script
#    3) Numpad 0  (CAM_Hero if present)  then  Z > Material Preview
#
#  WHAT IT DOES
#    - Deletes the old thin legs
#    - Builds 8 turned + fluted (reeded) legs, ~4x visual bulk
#    - Gold + dark-gold two-tone, no boolean modifiers (cannot fail)
#    - Adds a gold skirt band under the cabinet
#    - Adds cream sight band on the rail tops (optional)
#
#  SAFE:  only touches objects it creates or objects matching LEG_MATCH.
#         Never touches cloth / cushions / markings / colliders / physics.
# =============================================================================

import bpy
import bmesh
import math
from mathutils import Matrix, Vector

# ----------------------------- KNOBS ----------------------------------------
LEG_DIA          = 0.230     # shaft diameter at widest point (v2 was ~0.058)
LEG_HEIGHT       = 0.700     # floor -> underside of cabinet
LEG_COUNT        = 8         # 8 = tournament Aristocrat (4 corner + 4 mid)
LEG_REEDS        = 36        # flutes around the shaft
LEG_SEGS         = 24        # shaft roundness
SQUARE_PLINTH    = True      # square base block under each leg
PLINTH_H         = 0.075
CAPITAL_H        = 0.090     # block where leg meets cabinet
TAPER            = 0.78      # top diameter / bottom diameter of shaft

BUILD_SKIRT      = True      # gold band running under the cabinet
SKIRT_H          = 0.045
SKIRT_OUT        = 0.006     # how far it stands out from the cabinet

BUILD_SIGHT_BAND = True      # cream strip on rail tops (sight-line inlay band)
SIGHT_W          = 0.022

# table geometry (must match v2)
PLAY_L           = 3.569
PLAY_W           = 1.778
RAIL_W           = 0.140
CLOTH_Z          = 0.805
CABINET_BOTTOM_Z = 0.700     # underside of the frame
RAIL_TOP_Z       = 0.864

# colours (linear RGB)
GOLD             = (0.855, 0.620, 0.205, 1.0)
GOLD_DARK        = (0.520, 0.360, 0.110, 1.0)
CREAM            = (0.760, 0.700, 0.585, 1.0)

ROOT_NAME        = "147VR_Table_AAA_Aristocrat"
COLL_NAME        = "147VR_SnookerTable_AAA"
LEG_MATCH        = ("TBL_Legs", "TBL_Leg", "LEG_", "_Leg")

OUT_HX = PLAY_L * 0.5 + RAIL_W      # outer cabinet half-length
OUT_HY = PLAY_W * 0.5 + RAIL_W      # outer cabinet half-width
LEG_INSET = 0.170                   # leg centre inset from outer edge


# --------------------------- helpers ----------------------------------------
def log(msg):
    print("[v3 LEGS] " + str(msg))


def get_collection():
    c = bpy.data.collections.get(COLL_NAME)
    if c is None:
        c = bpy.context.scene.collection
    return c


def ensure_mat(name, rgba, rough, metal):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
        try:
            bsdf.inputs["Roughness"].default_value = rough
            bsdf.inputs["Metallic"].default_value = metal
        except Exception:
            pass
    return m


def cone(bm, r1, r2, depth, z, segs=LEG_SEGS, mat=0):
    """cylinder / truncated cone centred at z, tagged with material index"""
    kw = dict(cap_ends=True, cap_tris=False, segments=segs, depth=depth,
              matrix=Matrix.Translation((0.0, 0.0, z)))
    try:
        res = bmesh.ops.create_cone(bm, radius1=r1, radius2=r2, **kw)
    except TypeError:
        res = bmesh.ops.create_cone(bm, diameter1=r1, diameter2=r2, **kw)
    for f in {f for v in res["verts"] for f in v.link_faces}:
        f.material_index = mat
    return res


def box(bm, sx, sy, sz, z, mat=0):
    res = bmesh.ops.create_cube(bm, size=1.0,
                                matrix=Matrix.Translation((0.0, 0.0, z))
                                @ Matrix.Diagonal((sx, sy, sz, 1.0)))
    for f in {f for v in res["verts"] for f in v.link_faces}:
        f.material_index = mat
    return res


def ring(bm, r, minor, z, mat=0):
    try:
        res = bmesh.ops.create_circle(bm, cap_ends=False, segments=LEG_SEGS,
                                      radius=r,
                                      matrix=Matrix.Translation((0, 0, z)))
        return res
    except Exception:
        return None


# --------------------------- leg builder ------------------------------------
def build_leg(name, x, y, coll, mats):
    """One turned + fluted Aristocrat leg, built as a single mesh."""
    bm = bmesh.new()

    R = LEG_DIA * 0.5
    z = 0.0

    # 1) square plinth on the floor
    if SQUARE_PLINTH:
        s = LEG_DIA * 1.32
        box(bm, s, s, PLINTH_H, PLINTH_H * 0.5, mat=0)
        z = PLINTH_H
        # chamfer cap
        cone(bm, R * 1.18, R * 1.06, 0.018, z + 0.009, mat=0)
        z += 0.018
    else:
        cone(bm, R * 1.25, R * 1.10, PLINTH_H, PLINTH_H * 0.5, mat=0)
        z = PLINTH_H

    # 2) lower torus ring
    cone(bm, R * 1.10, R * 1.10, 0.030, z + 0.015, mat=0)
    z += 0.030

    # 3) fluted shaft  (core + radial reeds, NO booleans)
    shaft_h = LEG_HEIGHT - z - CAPITAL_H - 0.040
    if shaft_h < 0.10:
        shaft_h = 0.10
    core_r_bot = R * 0.86
    core_r_top = R * 0.86 * TAPER
    cone(bm, core_r_bot, core_r_top, shaft_h, z + shaft_h * 0.5, mat=1)

    reed_r = (math.pi * R * 0.90) / LEG_REEDS * 0.62
    for i in range(LEG_REEDS):
        a = (2.0 * math.pi / LEG_REEDS) * i
        rx = math.cos(a) * core_r_bot * 0.97
        ry = math.sin(a) * core_r_bot * 0.97
        kw = dict(cap_ends=True, cap_tris=False, segments=8,
                  depth=shaft_h * 0.985,
                  matrix=Matrix.Translation((rx, ry, z + shaft_h * 0.5)))
        try:
            res = bmesh.ops.create_cone(bm, radius1=reed_r,
                                        radius2=reed_r * TAPER, **kw)
        except TypeError:
            res = bmesh.ops.create_cone(bm, diameter1=reed_r,
                                        diameter2=reed_r * TAPER, **kw)
        for f in {f for v in res["verts"] for f in v.link_faces}:
            f.material_index = 0
    z += shaft_h

    # 4) upper collar
    cone(bm, core_r_top * 1.18, R * 1.02, 0.040, z + 0.020, mat=0)
    z += 0.040

    # 5) capital block bolted to the cabinet
    s = LEG_DIA * 1.24
    box(bm, s, s, CAPITAL_H, z + CAPITAL_H * 0.5, mat=0)

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()

    ob = bpy.data.objects.new(name, me)
    for m in mats:
        ob.data.materials.append(m)
    ob.location = (x, y, 0.0)
    coll.objects.link(ob)
    return ob


# --------------------------- skirt + sight band ------------------------------
def build_skirt(coll, mat_gold):
    bm = bmesh.new()
    zc = CABINET_BOTTOM_Z + SKIRT_H * 0.5
    ox, oy = OUT_HX + SKIRT_OUT, OUT_HY + SKIRT_OUT
    t = 0.014
    for (sx, sy, cx, cy) in (
        (ox * 2, t, 0.0,  oy),
        (ox * 2, t, 0.0, -oy),
        (t, oy * 2,  ox, 0.0),
        (t, oy * 2, -ox, 0.0),
    ):
        res = bmesh.ops.create_cube(
            bm, size=1.0,
            matrix=Matrix.Translation((cx, cy, zc))
            @ Matrix.Diagonal((sx, sy, SKIRT_H, 1.0)))
        for f in {f for v in res["verts"] for f in v.link_faces}:
            f.material_index = 0
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("TBL_GoldSkirt")
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("TBL_GoldSkirt", me)
    ob.data.materials.append(mat_gold)
    coll.objects.link(ob)
    return ob


def build_sight_band(coll, mat_cream):
    bm = bmesh.new()
    z = RAIL_TOP_Z + 0.0006
    mid = RAIL_W * 0.5
    ox = PLAY_L * 0.5 + mid
    oy = PLAY_W * 0.5 + mid
    for (sx, sy, cx, cy) in (
        (PLAY_L + RAIL_W * 2 * 0.92, SIGHT_W, 0.0,  oy),
        (PLAY_L + RAIL_W * 2 * 0.92, SIGHT_W, 0.0, -oy),
        (SIGHT_W, PLAY_W * 0.92,  ox, 0.0),
        (SIGHT_W, PLAY_W * 0.92, -ox, 0.0),
    ):
        res = bmesh.ops.create_cube(
            bm, size=1.0,
            matrix=Matrix.Translation((cx, cy, z))
            @ Matrix.Diagonal((sx, sy, 0.0012, 1.0)))
        for f in {f for v in res["verts"] for f in v.link_faces}:
            f.material_index = 0
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("TBL_SightBand")
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("TBL_SightBand", me)
    ob.data.materials.append(mat_cream)
    coll.objects.link(ob)
    return ob


# --------------------------- main -------------------------------------------
def main():
    coll = get_collection()
    log("collection = " + coll.name)

    mat_gold = ensure_mat("147VR_Gold_Aristocrat", GOLD, 0.26, 0.92)
    mat_dark = ensure_mat("147VR_GoldDark_Recess", GOLD_DARK, 0.42, 0.88)
    mat_crm  = ensure_mat("147VR_Cream_Sight", CREAM, 0.55, 0.00)
    mats = [mat_gold, mat_dark]

    # ---- remove old legs ----
    doomed = [o for o in bpy.data.objects
              if any(k in o.name for k in LEG_MATCH)]
    for o in doomed:
        log("remove old -> " + o.name)
        bpy.data.objects.remove(o, do_unlink=True)
    log("removed %d old leg object(s)" % len(doomed))

    for n in ("TBL_GoldSkirt", "TBL_SightBand"):
        old = bpy.data.objects.get(n)
        if old:
            bpy.data.objects.remove(old, do_unlink=True)

    # ---- leg positions ----
    lx = OUT_HX - LEG_INSET
    ly = OUT_HY - LEG_INSET
    pos = [(lx, ly), (lx, -ly), (-lx, ly), (-lx, -ly)]
    if LEG_COUNT >= 6:
        pos += [(0.0, ly), (0.0, -ly)]
    if LEG_COUNT >= 8:
        pos += [(lx * 0.5, ly), (lx * 0.5, -ly),
                (-lx * 0.5, ly), (-lx * 0.5, -ly)]
        pos = pos[:8] if LEG_COUNT == 8 else pos

    made = []
    for i, (x, y) in enumerate(pos[:LEG_COUNT]):
        ob = build_leg("TBL_Legs_%02d" % (i + 1), x, y, coll, mats)
        made.append(ob)
    log("built %d legs  dia=%.3f  h=%.3f  reeds=%d"
        % (len(made), LEG_DIA, LEG_HEIGHT, LEG_REEDS))

    if BUILD_SKIRT:
        made.append(build_skirt(coll, mat_gold))
        log("skirt built")
    if BUILD_SIGHT_BAND:
        made.append(build_sight_band(coll, mat_crm))
        log("sight band built")

    # ---- parent to table root ----
    root = bpy.data.objects.get(ROOT_NAME)
    if root:
        for ob in made:
            ob.parent = root
            ob.matrix_parent_inverse = root.matrix_world.inverted()
        log("parented to " + ROOT_NAME)
    else:
        log("WARNING: root '%s' not found - objects left unparented" % ROOT_NAME)

    tris = sum(len(o.data.polygons) for o in made if o.type == "MESH")
    log("=== v3 PATCH DONE ===  new objects=%d  faces=%d" % (len(made), tris))
    log("Now press Z > Material Preview, then Numpad 0 for CAM_Hero")


if __name__ == "__main__":
    main()
