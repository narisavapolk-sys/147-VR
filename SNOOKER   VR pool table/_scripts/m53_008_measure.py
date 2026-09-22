"""
147VR - 008 read-only geometry / collider / calibration measurement pass.

Spec: Docs/AI_TEAM/COACH_008_CONTRACT_REV1_REVIEW_20260922.md section 5
Provided by COACH. LUNA runs it; COACH reviews the JSON.

READ-ONLY GUARANTEE
  This script never calls bpy.ops.wm.save_* and never writes to the .blend.
  Output is a separate .json plus a stdout summary.
  If the source cannot be opened it exits non-zero and writes NO artifact -
  a missing measurement must never be mistakable for a pass.

USAGE
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" ^
    --background --factory-startup --python m53_008_measure.py -- ^
    "C:\\...\\Blender\\147VR_Table_WPBSA_Visual_Clean_v008_MARKING_CLEAN.blend" ^
    "C:\\Temp\\m53_008_measure.json"
"""

import bpy
import hashlib
import json
import math
import os
import sys
from datetime import datetime, timezone
from mathutils import Vector

EXPECT_LENGTH = 3.569
EXPECT_WIDTH = 1.778
TOL = 0.01

POCKET_KEYS = ("E", "W", "NE", "NW", "SE", "SW")

GROUPS = {
    "table_surface": ("TABLE SURFACE", "TABLE_SURFACE"),
    "spatial_anchors": ("SPATIAL_ANCHOR", "SPATIAL ANCHOR"),
    "pocket_anchors": ("ANCHOR_POCKET", "ANCHOR POCKET"),
    "cushions": ("CUSHION",),
    "ball_catchers": ("BALL CATCHER", "BALL_CATCHER"),
    "pocket_pads": ("POCKET PAD", "POCKET_PAD"),
    "markings": ("MARKING",),
    "balls": ("BALL",),
}

def norm(s):
    out = []
    for ch in s.upper():
        out.append(ch if ch.isalnum() else " ")
    return " ".join("".join(out).split())

def r6(v):
    return round(float(v), 6)

def r6v(v):
    return [r6(x) for x in v]

def world_bounds(obj):
    try:
        dg = bpy.context.evaluated_depsgraph_get()
        oe = obj.evaluated_get(dg)
        mw = oe.matrix_world
        pts = [mw @ Vector(c) for c in oe.bound_box]
        src = "evaluated"
    except Exception:
        mw = obj.matrix_world
        pts = [mw @ Vector(c) for c in obj.bound_box]
        src = "bound_box_fallback"
    xs = [p.x for p in pts]
    ys = [p.y for p in pts]
    zs = [p.z for p in pts]
    mn = [min(xs), min(ys), min(zs)]
    mx = [max(xs), max(ys), max(zs)]
    return {
        "source": src,
        "min": r6v(mn),
        "max": r6v(mx),
        "size": r6v([mx[i] - mn[i] for i in range(3)]),
        "center": r6v([(mx[i] + mn[i]) * 0.5 for i in range(3)]),
    }

def safe_props(obj):
    out = {}
    try:
        for k in obj.keys():
            if k == "_RNA_UI":
                continue
            try:
                v = obj[k]
                if v is None or isinstance(v, (bool, int, float, str)):
                    out[k] = v
                elif hasattr(v, "__iter__"):
                    out[k] = [x if isinstance(x, (bool, int, float, str)) else str(x) for x in v]
                else:
                    out[k] = str(v)
            except Exception:
                out[k] = "<unreadable>"
    except Exception:
        pass
    return out

def sha256_of(path):
    h = hashlib.sha256()
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

def parse_args():
    argv = sys.argv
    if "--" in argv:
        tail = argv[argv.index("--") + 1:]
    else:
        tail = []
    if not tail:
        print("[FATAL] no arguments. Need: -- <source.blend> [out.json]")
        sys.exit(2)
    blend = tail[0]
    out = tail[1] if len(tail) > 1 else os.path.splitext(blend)[0] + "_m53_008_measure.json"
    return blend, out

def main():
    blend_path, out_path = parse_args()

    if not os.path.isfile(blend_path):
        print("[FATAL] source .blend not found: %s" % blend_path)
        print("[FATAL] no artifact written - a missing source must not look like a pass.")
        sys.exit(2)

    size_bytes = os.path.getsize(blend_path)
    with open(blend_path, "rb") as fh:
        head = fh.read(80)
    if head.startswith(b"version https://git-lfs"):
        print("[FATAL] source is a Git LFS pointer (%d bytes), not a real .blend." % size_bytes)
        print("[FATAL] run: git lfs install && git lfs pull   then retry.")
        print("[FATAL] no artifact written.")
        sys.exit(3)

    print("[1/4] hashing source ...")
    src_sha = sha256_of(blend_path)

    print("[2/4] opening %s" % blend_path)
    bpy.ops.wm.open_mainfile(filepath=blend_path)

    scene = bpy.context.scene
    objects = list(scene.objects)

    print("[3/4] measuring %d objects ..." % len(objects))
    obj_rows = []
    for o in objects:
        row = {
            "name": o.name,
            "type": o.type,
            "parent": o.parent.name if o.parent else None,
            "collections": sorted(c.name for c in o.users_collection),
            "hide_viewport": bool(o.hide_viewport),
            "hide_render": bool(o.hide_render),
            "location": r6v(o.location),
            "local_scale": r6v(o.scale),
            "rotation_mode": o.rotation_mode,
            "local_rotation_euler_deg": r6v([math.degrees(a) for a in o.rotation_euler]),
            "world_scale": r6v(o.matrix_world.to_scale()),
            "world_rotation_euler_deg": r6v(
                [math.degrees(a) for a in o.matrix_world.to_euler()]
            ),
            "world_determinant": r6(o.matrix_world.determinant()),
            "custom_props": safe_props(o),
        }
        if o.type == "MESH":
            row["world_bounds"] = world_bounds(o)
            row["mesh"] = {
                "vertices": len(o.data.vertices),
                "polygons": len(o.data.polygons),
                "uv_layers": [uv.name for uv in o.data.uv_layers],
            }
            row["materials"] = [
                (s.material.name if s.material else None) for s in o.material_slots
            ]
        obj_rows.append(row)

    coll_counts = {}
    for c in bpy.data.collections:
        coll_counts[c.name] = len(c.objects)
    root_colls = sorted(c.name for c in scene.collection.children)

    groups = {}
    for key, needles in GROUPS.items():
        hits = []
        for o in objects:
            n = norm(o.name)
            if any(norm(x) in n for x in needles):
                hits.append(o.name)
        groups[key] = sorted(hits)

    surface_names = groups["table_surface"]
    surface_measure = None
    orientation = None
    for o in objects:
        if o.name in surface_names and o.type == "MESH":
            wb = world_bounds(o)
            lbl = ["X", "Y", "Z"]
            longest = max(range(3), key=lambda i: wb["size"][i])
            other = [i for i in range(3) if i != longest]
            widest = max(other, key=lambda i: wb["size"][i])
            orientation = {
                "long_axis": lbl[longest],
                "long_axis_size_m": wb["size"][longest],
                "width_axis": lbl[widest],
                "width_axis_size_m": wb["size"][widest],
                "up_axis_candidates": [
                    {"axis": lbl[i], "size_m": wb["size"][i]}
                    for i in range(3) if i not in (longest, widest)
                ],
            }
            surface_measure = {"object": o.name, "world_bounds": wb}
            break

    anchors = []
    seen_keys = set()
    for o in objects:
        n = norm(o.name)
        if "ANCHOR POCKET" not in n:
            continue
        tail = n.rsplit("ANCHOR POCKET", 1)[-1].strip()
        key = tail if tail in POCKET_KEYS else None
        if key:
            seen_keys.add(key)
        anchors.append({
            "name": o.name,
            "compass": key,
            "world_location": r6v(o.matrix_world.translation),
            "parent": o.parent.name if o.parent else None,
        })

    images = {}
    for mat in bpy.data.materials:
        if not getattr(mat, "use_nodes", False) or not mat.node_tree:
            continue
        for node in mat.node_tree.nodes:
            if node.type != "TEX_IMAGE" or node.image is None:
                continue
            img = node.image
            entry = images.get(img.name)
            if entry is None:
                fp = img.filepath or ""
                packed = bool(img.packed_file)
                try:
                    resolved = packed or os.path.exists(bpy.path.abspath(fp))
                except Exception:
                    resolved = False
                entry = {
                    "filepath": fp,
                    "packed": packed,
                    "resolved": resolved,
                    "used_by": [],
                }
                images[img.name] = entry
            if mat.name not in entry["used_by"]:
                entry["used_by"].append(mat.name)
    for entry in images.values():
        entry["used_by"] = sorted(entry["used_by"])

    missing_material_objs = sorted(
        r["name"] for r in obj_rows
        if r["type"] == "MESH" and not [m for m in r.get("materials", []) if m]
    )
    unresolved = sorted(k for k, v in images.items() if not v["resolved"])

    neg_det = sorted(r["name"] for r in obj_rows if r["world_determinant"] < 0)
    non_unit = sorted(
        r["name"] for r in obj_rows
        if any(abs(v - 1.0) > 1e-4 for v in r["world_scale"])
    )
    rotated = sorted(
        r["name"] for r in obj_rows
        if any(abs(a) > 1e-3 for a in r["world_rotation_euler_deg"])
    )

    def close(a, b):
        return abs(a - b) <= TOL

    surface_size = surface_measure["world_bounds"]["size"] if surface_measure else [0.0, 0.0, 0.0]
    assertions = {
        "ASSERT_TABLE_SURFACE_FOUND": bool(surface_measure),
        "ASSERT_LENGTH_3p569": any(close(v, EXPECT_LENGTH) for v in surface_size),
        "ASSERT_WIDTH_1p778": any(close(v, EXPECT_WIDTH) for v in surface_size),
        "ASSERT_THICKNESS_UNDER_0p05": any(v < 0.05 for v in surface_size),
        "ASSERT_SIX_POCKET_ANCHORS": len(seen_keys) == 6,
        "ASSERT_POCKET_ANCHOR_KEYS_COMPLETE": sorted(seen_keys) == sorted(POCKET_KEYS),
        "ASSERT_NO_NEGATIVE_SCALE": len(neg_det) == 0,
        "ASSERT_NO_MISSING_MATERIAL": len(missing_material_objs) == 0,
        "ASSERT_NO_UNRESOLVED_TEXTURE": len(unresolved) == 0,
        "ASSERT_WHITE_CUEBALL_PRESENT": any(
            "WHITE CUEBALL" in norm(r["name"]) or "WHITECUEBALL" in norm(r["name"])
            for r in obj_rows
        ),
    }

    payload = {
        "pass": "m53_008_measure",
        "role": "read-only measurement artifact (COACH spec section 5)",
        "read_only": True,
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "blender_version": bpy.app.version_string,
        "command_line": list(sys.argv),
        "source": {
            "blend_path": os.path.abspath(blend_path),
            "sha256": src_sha,
            "size_bytes": size_bytes,
        },
        "scene": {
            "name": scene.name,
            "unit_system": scene.unit_settings.system,
            "scale_length": scene.unit_settings.scale_length,
            "object_count": len(objects),
            "collection_count": len(bpy.data.collections),
            "root_collections": root_colls,
            "material_count": len(bpy.data.materials),
        },
        "collections": coll_counts,
        "groups": groups,
        "table_surface": {
            "matches": surface_names,
            "measurement": surface_measure,
            "orientation": orientation,
            "expected_length_m": EXPECT_LENGTH,
            "expected_width_m": EXPECT_WIDTH,
        },
        "pocket_anchors": {"count": len(anchors), "compass_keys_found": sorted(seen_keys), "items": anchors},
        "materials": {
            "missing_material_objects": missing_material_objs,
            "unresolved_images": unresolved,
            "images": images,
        },
        "hygiene": {
            "negative_determinant_world": neg_det,
            "non_unit_world_scale": non_unit,
            "non_zero_world_rotation": rotated,
        },
        "assertions": assertions,
        "assertions_passed": sum(1 for v in assertions.values() if v),
        "assertions_total": len(assertions),
        "objects": obj_rows,
        "notes": [
            "Bounds are evaluated world-space AABB (modifiers applied) where possible.",
            "Non-unit world scale is reported as data, not a failure: the project intentionally imports some meshes with large compensated scales.",
            "negative_determinant_world / non_unit_world_scale are the FBX->Unity traps named in COACH's spec section 5 [hygiene].",
            "Group matchers overlap by design (a name containing BALL also appears under balls). The 'objects' array is the authoritative inventory; groups are navigation aids.",
            "A group with zero hits is a reported gap, not an automatic failure.",
        ],
    }

    print("[4/4] writing %s" % out_path)
    with open(out_path, "w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=2, ensure_ascii=False)

    print("")
    print("================ 147VR 008 MEASUREMENT ================")
    print("blender        : %s" % bpy.app.version_string)
    print("source sha256  : %s" % src_sha)
    print("source size    : %d bytes" % size_bytes)
    print("objects        : %d" % len(objects))
    print("collections    : %d" % len(bpy.data.collections))
    print("-------------------------------------------------------")
    if surface_measure:
        wb = surface_measure["world_bounds"]
        print("TABLE SURFACE  : %s" % surface_measure["object"])
        print("  min          : %s" % wb["min"])
        print("  max          : %s" % wb["max"])
        print("  size (x,y,z) : %s" % wb["size"])
        if orientation:
            print("  long axis    : %s = %.4f m" % (orientation["long_axis"], orientation["long_axis_size_m"]))
            print("  width axis   : %s = %.4f m" % (orientation["width_axis"], orientation["width_axis_size_m"]))
    else:
        print("TABLE SURFACE  : NOT FOUND")
    print("pocket anchors : %d  (keys: %s)" % (len(anchors), ",".join(sorted(seen_keys))))
    print("-------------------------------------------------------")
    for k, v in groups.items():
        print("group %-16s: %d" % (k, len(v)))
    print("--- ASSERTIONS ----------------------------------------")
    for k in sorted(assertions):
        print("  %-38s %s" % (k, "PASS" if assertions[k] else "FAIL"))
    n_pass = sum(1 for v in assertions.values() if v)
    print("  %-38s %d/%d" % ("TOTAL", n_pass, len(assertions)))
    print("=======================================================")
    print("artifact: %s" % os.path.abspath(out_path))

if __name__ == "__main__":
    main()
