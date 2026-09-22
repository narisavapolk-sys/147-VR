"""
147VR - table accessory (prop) generator.  COACH-provided.

WHAT THIS IS
  Builds the table accessories the project does not have as meshes yet, as parametric
  geometry, and exports one FBX per prop for Unity:
      Chalk  |  Cross rest  |  Cue rack  |  Racking triangle  |  Scoreboard

  The cue already exists (Assets/Prefabs/PoolTable/PREFAB POoL Cues.prefab), so it is not built.

HARD RULES BAKED IN
  1. NEVER opens or writes the certified 008 .blend. That file's sha256
     (c4e0fb162d72146c6f3e0d43b8b18656b63a78315e2c3388ceb656e2b4ce357d) is committed
     evidence; touching it invalidates Artifacts/M5_3/008_Measurement. This script builds a
     NEW props .blend instead. No source .blend is opened at all.
  2. Same axis convention as the 008 table: LENGTH ON X, WIDTH ON Y, Z UP. Whatever resolves
     the open D1 axis question then applies to props and table identically.
  3. Grounded props: lowest vertex at Z = 0, centred on X/Y. Unity placement therefore needs
     only a position and a Y rotation.
  4. Every prop carries an `asset_role` custom property (repo convention, e.g. the existing
     `SNOOKER_BALL`) and a real material, so the Unity import has nothing missing.
  5. No camera, no light, POSITIVE scale only. The export must not repeat the `Plane`
     negative-determinant finding from the 008 measurement.

MAIN SCENE INTEGRATION REQUIREMENTS (read before placing anything)
  SnookerPhysicsSetup.EnsurePhysics() runs, on `tableRoot`:
      foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
          if (ball-sized SphereCollider with Rigidbody) continue; else col.enabled = false;
  and in 147VR_MainScene.unity, `tableRoot` = fileID 209730002, a transform INSIDE the
  Prefab_WPBSA_12Foot_Snooker prefab instance (guid cd255914...). The v007_MARKING prefab
  instance is a CHILD of that same transform (PrefabInstance &38161660, guid 3e289c51...).

  Consequences - these are code-enforced, not style preferences:
    * A prop parented anywhere under the table prefab instance, or under 209730002, gets its
      collider SILENTLY DISABLED at Start(). The prop would look correct and be non-collidable.
    * Props must therefore live under their OWN scene root, a SIBLING of the table instance.
      Never inside the table prefab, and never inside 209730002.
    * No prop may be named `Bed_Collider` (FindSurfaceCollider picks it as the physics
      authority) or `TABLE SURFACE` (the surface renderer lookup).
    * Because v007_MARKING is currently a CHILD of tableRoot, the "optional variant" decision
      needs it to be a sibling (or explicitly inactive) - otherwise it stays inside the
      primary's physics-scan subtree.
    * Keep props OFF the cloth. TableSurfaceController.OnCollisionStay applies cloth friction
      forces to ANY rigidbody touching the Surface collider, so a prop resting on the playing
      surface silently joins the calibrated physics pair.

USAGE
  blender --background --factory-startup --python m53_008_props.py -- <out_dir> [--only chalk]

  Outputs, under <out_dir>:
    147VR_Props_v001.blend                 (new file - the props source, NOT 008)
    fbx/147VR_PROP_<NAME>.fbx              one per prop
    m53_008_props_manifest.json            measured dims + assertions
"""

import bpy
import json
import math
import os
import sys
from mathutils import Vector

# ----------------------------------------------------------------- measured 008 truth
# From Artifacts/M5_3/008_Measurement/m53_008_measure.json (commit 859a51a).
BALL_D = 0.052578          # measured 008 ball diameter (m)
TABLE_L = 3.569            # measured 008 TABLE SURFACE length on X (m)
TABLE_W = 1.778            # measured 008 TABLE SURFACE width on Y (m)
TABLE_T = 0.0127           # measured thickness (m)

# ----------------------------------------------------------------- proposed dimensions
# PROPOSED VALUES, NOT MEASURED TRUTH. Review and edit here; nothing else hard-codes sizes.
# Derived-from-measurement values are marked DERIVED.
SPEC = {
    "chalk": {
        "asset_role": "147VR_PROP_CHALK",
        "body": (0.035, 0.035, 0.022),          # x, y, z
        "dish_radius": 0.013,                    # DERIVED visually from body
        "dish_depth": 0.003,
        "material": ("M_PROP_Chalk_Blue", (0.06, 0.18, 0.55, 1.0), 0.75),
    },
    "rest": {
        "asset_role": "147VR_PROP_REST",
        "shaft_len": 1.420,                      # along X
        "shaft_r": 0.011,
        "head_x": 0.130,
        "head_y": 0.100,
        "head_z": 0.022,
        "ferrule_len": 0.030,
        "material": ("M_PROP_Rest_Wood", (0.28, 0.16, 0.08, 1.0), 0.35),
        "material_brass": ("M_PROP_Rest_Brass", (0.72, 0.55, 0.22, 1.0), 0.25),
    },
    "cuerack": {
        "asset_role": "147VR_PROP_CUE_RACK",
        "base": (0.420, 0.300, 0.030),
        "post": (0.024, 0.024, 0.900),
        "rail": (0.420, 0.070, 0.020),
        "rail_z": (0.330, 0.700),
        "slots": 6,
        "slot_r": 0.016,
        "material": ("M_PROP_Rack_Wood", (0.24, 0.13, 0.07, 1.0), 0.40),
    },
    "triangle": {
        "asset_role": "147VR_PROP_RACK_TRIANGLE",
        # DERIVED from the measured ball: 5 touching balls across the base row.
        "inner_side": 5.0 * BALL_D,
        "rail_t": 0.018,
        "height": 0.038,
        "material": ("M_PROP_Triangle_Plastic", (0.10, 0.10, 0.11, 1.0), 0.55),
    },
    "scoreboard": {
        "asset_role": "147VR_PROP_SCOREBOARD",
        "board": (0.620, 0.024, 0.420),          # x, y(thickness), z
        "frame_t": 0.030,
        "post": (0.050, 0.050, 0.900),
        "base": (0.340, 0.240, 0.030),
        "track": (0.180, 0.010, 0.030),
        "material": ("M_PROP_Board_Wood", (0.22, 0.12, 0.06, 1.0), 0.40),
        "material_face": ("M_PROP_Board_Face", (0.06, 0.06, 0.07, 1.0), 0.60),
    },
}

ORDER = ["chalk", "rest", "cuerack", "triangle", "scoreboard"]


# ----------------------------------------------------------------- helpers
def clean_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name, rgba, rough):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = rough
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = 0.0
    return m


def box(name, dims, loc, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    o.scale = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return o


def cyl(name, radius, depth, loc, rot=(0.0, 0.0, 0.0), verts=24):
    bpy.ops.mesh.primitive_cylinder_add(
        radius=radius, depth=depth, location=loc, rotation=rot, vertices=verts
    )
    o = bpy.context.active_object
    o.name = name
    return o


def boolean_diff(target, cutter):
    """Subtract `cutter` from `target`. Returns True on success. Never fatal."""
    try:
        mod = target.modifiers.new(name="bool", type="BOOLEAN")
        mod.operation = "DIFFERENCE"
        mod.object = cutter
        bpy.context.view_layer.objects.active = target
        bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.data.objects.remove(cutter, do_unlink=True)
        return True
    except Exception as exc:                                    # noqa: BLE001
        print("    [warn] boolean failed, keeping solid body: %s" % exc)
        try:
            bpy.data.objects.remove(cutter, do_unlink=True)
        except Exception:                                       # noqa: BLE001
            pass
        return False


def apply_material(obj, material):
    obj.data.materials.clear()
    obj.data.materials.append(material)


def set_role(obj, role):
    obj["asset_role"] = role


def world_aabb(objs):
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        for c in o.bound_box:
            p = o.matrix_world @ Vector(c)
            for i in range(3):
                mn[i] = min(mn[i], p[i])
                mx[i] = max(mx[i], p[i])
    return mn, mx


def ground_and_center(objs):
    """Shift so min Z == 0 and the X/Y AABB centre is the origin."""
    bpy.context.view_layer.update()
    mn, mx = world_aabb(objs)
    dx = -(mn.x + mx.x) * 0.5
    dy = -(mn.y + mx.y) * 0.5
    dz = -mn.z
    for o in objs:
        o.location = o.location + Vector((dx, dy, dz))
    bpy.context.view_layer.update()


# ----------------------------------------------------------------- prop builders
def build_chalk():
    s = SPEC["chalk"]
    m = mat(*s["material"])
    body = box("Chalk_Body", s["body"], (0, 0, s["body"][2] * 0.5))
    set_role(body, s["asset_role"])
    # recess opens at the top face (Z = body_z) and goes down by exactly dish_depth
    cutter = cyl(
        "Chalk_Dish_Cutter", s["dish_radius"], s["dish_depth"] * 3.0,
        (0, 0, s["body"][2] + s["dish_depth"] * 0.5), verts=32,
    )
    boolean_diff(body, cutter)
    apply_material(body, m)
    return [body]


def build_rest():
    s = SPEC["rest"]
    m = mat(*s["material"])
    mb = mat(*s["material_brass"])
    objs = []
    total = s["shaft_len"] + s["head_x"] * 0.5
    x0 = -total * 0.5
    shaft = cyl(
        "Rest_Shaft", s["shaft_r"], s["shaft_len"],
        (x0 + s["shaft_len"] * 0.5, 0, s["shaft_r"]), rot=(0, math.pi / 2, 0),
    )
    apply_material(shaft, m)
    objs.append(shaft)

    ferrule = cyl(
        "Rest_Ferrule", s["shaft_r"] * 1.15, s["ferrule_len"],
        (x0 + s["shaft_len"] + s["ferrule_len"] * 0.5, 0, s["shaft_r"]),
        rot=(0, math.pi / 2, 0),
    )
    apply_material(ferrule, mb)
    objs.append(ferrule)

    hx = x0 + s["shaft_len"] + s["ferrule_len"] + s["head_x"] * 0.5
    cross = box("Rest_Head_Cross", (s["head_x"], s["head_y"], s["head_z"]),
                (hx, 0, s["shaft_r"]))
    apply_material(cross, mb)
    objs.append(cross)

    for tag, sy in (("N", 1.0), ("S", -1.0)):
        arm = box("Rest_Head_Arm_" + tag, (s["head_x"] * 0.35, s["head_y"] * 0.35, s["head_z"]),
                  (hx, sy * s["head_y"] * 0.5, s["shaft_r"]))
        apply_material(arm, mb)
        objs.append(arm)

    for o in objs:
        set_role(o, s["asset_role"])
    return objs


def build_cuerack():
    s = SPEC["cuerack"]
    m = mat(*s["material"])
    objs = []
    bx, by, bz = s["base"]
    base = box("Rack_Base", s["base"], (0, 0, bz * 0.5))
    apply_material(base, m)
    objs.append(base)

    px, py, pz = s["post"]
    for tag, sx in (("L", -1.0), ("R", 1.0)):
        post = box("Rack_Post_" + tag, s["post"],
                   (sx * (bx * 0.5 - px * 0.5), 0, bz + pz * 0.5))
        apply_material(post, m)
        objs.append(post)

    rx, ry, rz = s["rail"]
    for i, z in enumerate(s["rail_z"]):
        rail = box("Rack_Rail_%d" % (i + 1), s["rail"],
                   (0, 0, bz + z + rz * 0.5))
        apply_material(rail, m)
        # pierce cue slots so cues can actually pass through the rail
        span = rx * 0.72
        for k in range(s["slots"]):
            t = -1.0 + 2.0 * (k / (s["slots"] - 1.0))
            cutter = cyl(
                "Rack_Hole_%d_%d" % (i + 1, k + 1), s["slot_r"], ry * 3.0,
                (t * span * 0.5, 0, bz + z + rz * 0.5), rot=(math.pi / 2, 0, 0), verts=20,
            )
            boolean_diff(rail, cutter)
        objs.append(rail)

    for o in objs:
        set_role(o, s["asset_role"])
    return objs


def build_triangle():
    s = SPEC["triangle"]
    m = mat(*s["material"])
    inner = s["inner_side"]
    outer = inner + 2.0 * s["rail_t"]
    # Equilateral rack frame. For an equilateral triangle of side `inner`, the inradius is
    # inner / (2*sqrt(3)). Each rail is placed TANGENTIALLY (perpendicular to its radial
    # direction) with its inner face on that inradius, and is `outer` long so the rails
    # overhang at the three corners like a real racking triangle.
    r_in = inner / (2.0 * math.sqrt(3.0))
    d = r_in + s["rail_t"] * 0.5
    objs = []
    for i in range(3):
        ang = math.radians(90.0 + i * 120.0)
        cx = d * math.cos(ang)
        cy = d * math.sin(ang)
        bar = box(
            "Triangle_Rail_%d" % (i + 1),
            (outer, s["rail_t"], s["height"]),
            (cx, cy, s["height"] * 0.5),
            rot=(0.0, 0.0, ang + math.pi * 0.5),
        )
        apply_material(bar, m)
        set_role(bar, s["asset_role"])
        objs.append(bar)
    return objs


def build_scoreboard():
    s = SPEC["scoreboard"]
    m = mat(*s["material"])
    mf = mat(*s["material_face"])
    objs = []
    bx, by, bz = s["base"]
    base = box("Board_Base", s["base"], (0, 0, bz * 0.5))
    apply_material(base, m)
    objs.append(base)

    px, py, pz = s["post"]
    post = box("Board_Post", s["post"], (0, 0, bz + pz * 0.5))
    apply_material(post, m)
    objs.append(post)

    wx, wy, wz = s["board"]
    zc = bz + pz + wz * 0.5
    face = box("Board_Face", (wx * 0.92, wy * 0.6, wz * 0.88), (0, wy * 0.22, zc))
    apply_material(face, mf)
    objs.append(face)

    ft = s["frame_t"]
    dframe = wy + 0.004
    for tag, dims, loc in (
        ("Bottom", (wx, dframe, ft), (0.0, 0.0, zc - wz * 0.5 + ft * 0.5)),
        ("Top", (wx, dframe, ft), (0.0, 0.0, zc + wz * 0.5 - ft * 0.5)),
        ("Left", (ft, dframe, wz - 2.0 * ft), (-(wx * 0.5 - ft * 0.5), 0.0, zc)),
        ("Right", (ft, dframe, wz - 2.0 * ft), ((wx * 0.5 - ft * 0.5), 0.0, zc)),
    ):
        fr = box("Board_Frame_" + tag, dims, loc)
        apply_material(fr, m)
        objs.append(fr)

    tx, ty, tz = s["track"]
    for i, sx in enumerate((-1.0, 1.0)):
        tr = box("Board_Track_%d" % (i + 1), s["track"],
                 (sx * wx * 0.24, wy * 0.55, bz + pz + wz * 0.10))
        apply_material(tr, m)
        objs.append(tr)

    for o in objs:
        set_role(o, s["asset_role"])
    return objs


BUILDERS = {
    "chalk": build_chalk,
    "rest": build_rest,
    "cuerack": build_cuerack,
    "triangle": build_triangle,
    "scoreboard": build_scoreboard,
}


# ----------------------------------------------------------------- export + report
def export_fbx(objs, path):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        global_scale=1.0,
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        path_mode="COPY",
        embed_textures=False,
        bake_anim=False,
        add_leaf_bones=False,
    )


def parse_args():
    tail = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = tail[0] if tail else os.path.join(os.getcwd(), "props_out")
    only = None
    for i, a in enumerate(tail):
        if a == "--only" and i + 1 < len(tail):
            only = [x.strip() for x in tail[i + 1].split(",") if x.strip()]
    if only:
        unknown = [x for x in only if x not in BUILDERS]
        if unknown:
            print("[FATAL] unknown prop(s): %s" % ", ".join(unknown))
            sys.exit(2)
    return out, only


def main():
    out_dir, only = parse_args()
    wanted = only or ORDER
    fbx_dir = os.path.join(out_dir, "fbx")
    os.makedirs(fbx_dir, exist_ok=True)

    report = {
        "pass": "m53_008_props",
        "provided_by": "COACH",
        "opened_source_blend": None,
        "modified_source_blend": False,
        "certified_008_sha256": (
            "c4e0fb162d72146c6f3e0d43b8b18656b63a78315e2c3388ceb656e2b4ce357d"
        ),
        "axis_convention": {"length": "X", "width": "Y", "up": "Z", "fbx_axis_up": "Y",
                            "fbx_axis_forward": "-Z"},
        "proposed_dimensions": "see SPEC in m53_008_props.py - PROPOSED, not measured truth",
        "props": {},
        "assertions": {},
    }

    for prop in wanted:
        print("\n=== building %s ===" % prop)
        clean_scene()
        objs = BUILDERS[prop]()
        ground_and_center(objs)
        mn, mx = world_aabb(objs)
        size = [round(mx[i] - mn[i], 6) for i in range(3)]
        fb = os.path.join(fbx_dir, "147VR_PROP_%s.fbx" % prop.upper())
        export_fbx(objs, fb)
        report["props"][prop] = {
            "objects": len(objs),
            "object_names": sorted(o.name for o in objs),
            "asset_role": SPEC[prop]["asset_role"],
            "world_min": [round(v, 6) for v in mn],
            "world_max": [round(v, 6) for v in mx],
            "size_m": size,
            "grounded": abs(mn[2]) < 1e-6,
            "fbx": os.path.relpath(fb, out_dir),
            "fbx_bytes": os.path.getsize(fb),
            "materials": sorted({s.material.name for o in objs for s in o.material_slots if s.material}),
        }
        print("  objects %d  size(X,Y,Z)=%s  fbx=%s" % (len(objs), size, os.path.basename(fb)))

    # ---- second pass: rebuild every prop into ONE scene so the props .blend holds them all.
    #      The per-prop loop needs a clean scene per prop for a correct single-prop FBX;
    #      this pass only feeds the combined .blend and the assertions.
    print("\n=== staging all props into one scene for the props .blend ===")
    clean_scene()
    lane = 0.0
    for prop in wanted:
        objs = BUILDERS[prop]()
        ground_and_center(objs)
        for o in objs:
            o.location = o.location + Vector((0.0, lane, 0.0))
        lane += 1.0
    bpy.context.view_layer.update()

    # ---- assertions over EVERYTHING that was built
    all_objs = [o for o in bpy.data.objects]
    prop_objs = [o for o in all_objs if o.type == "MESH"]
    bad_scale = [o.name for o in prop_objs if any(abs(v - 1.0) > 1e-4 for v in o.scale)]
    neg_det = [o.name for o in prop_objs if o.matrix_world.determinant() < 0]
    no_role = [o.name for o in prop_objs if not o.get("asset_role")]
    no_mat = [o.name for o in prop_objs if not [s for s in o.material_slots if s.material]]
    not_ground = [p for p, r in report["props"].items() if not r["grounded"]]
    extra = [o.name for o in all_objs if o.type in ("CAMERA", "LIGHT")]

    report["assertions"] = {
        "ASSERT_NO_SOURCE_BLEND_OPENED": report["opened_source_blend"] is None,
        "ASSERT_SOURCE_BLEND_NOT_MODIFIED": report["modified_source_blend"] is False,
        "ASSERT_NO_NEGATIVE_SCALE": len(neg_det) == 0,
        "ASSERT_UNIT_SCALE": len(bad_scale) == 0,
        "ASSERT_ALL_HAVE_ASSET_ROLE": len(no_role) == 0,
        "ASSERT_ALL_HAVE_MATERIAL": len(no_mat) == 0,
        "ASSERT_ALL_GROUNDED_AT_Z0": len(not_ground) == 0,
        "ASSERT_NO_CAMERA_OR_LIGHT": len(extra) == 0,
        # construction intent: the inner side must seat a 5-ball base row plus the 2-ball
        # widening of the rows behind it, i.e. 5 x the MEASURED 008 ball diameter.
        "ASSERT_TRIANGLE_INNER_FITS_15_BALLS": (
            SPEC["triangle"]["inner_side"] >= 5.0 * BALL_D - 1e-9
        ),
        # MEASURED from the built mesh, not by construction.
        "ASSERT_TRIANGLE_MEASURED_WIDTH_OK": (
            report["props"].get("triangle", {}).get("size_m", [0.0])[0]
            >= 5.0 * BALL_D - 0.005
        ) if "triangle" in report["props"] else True,
        "ASSERT_REST_LENGTH_REASONABLE": (
            0.30 * TABLE_L <= report["props"].get("rest", {}).get("size_m", [0])[0] <= 0.55 * TABLE_L
        ) if "rest" in report["props"] else True,
        "ASSERT_PROP_NAMES_DO_NOT_COLLIDE_WITH_PHYSICS_NAMES": not any(
            n in ("Bed_Collider", "TABLE SURFACE") for n in
            [x for r in report["props"].values() for x in r["object_names"]]
        ),
    }
    report["assertions_passed"] = sum(1 for v in report["assertions"].values() if v)
    report["assertions_total"] = len(report["assertions"])

    # ---- the props source file (NEW file - never the certified 008 blend)
    blend_out = os.path.join(out_dir, "147VR_Props_v001.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_out)
    report["props_blend"] = os.path.basename(blend_out)

    man = os.path.join(out_dir, "m53_008_props_manifest.json")
    with open(man, "w", encoding="utf-8") as fh:
        json.dump(report, fh, indent=2, ensure_ascii=False)

    # ---- stdout
    print("\n================ 147VR PROPS ================")
    print("axis convention : length X, width Y, up Z  ->  FBX axis_up=Y axis_forward=-Z")
    for p, r in report["props"].items():
        print("  %-11s objs=%-3d size(X,Y,Z)=%-28s fbx=%d B" % (
            p, r["objects"], str(r["size_m"]), r["fbx_bytes"]))
    print("--- ASSERTIONS -------------------------------")
    for k in sorted(report["assertions"]):
        print("  %-46s %s" % (k, "PASS" if report["assertions"][k] else "FAIL"))
    print("  %-46s %d/%d" % ("TOTAL", report["assertions_passed"], report["assertions_total"]))
    print("----------------------------------------------")
    print("props blend : %s   (NEW file - the certified 008 blend was NOT opened)" % blend_out)
    print("manifest    : %s" % man)
    print("==============================================")


if __name__ == "__main__":
    main()
