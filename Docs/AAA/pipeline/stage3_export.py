# stage3_export.py — Blender headless (v3): decimate to Quest budget, export FBX + textures + manifest
#   blender --background --factory-startup "<blend>" --python stage3_export.py -- <analysis.json> <out_root> <budget>
import json
import os
import re
import shutil
import sys

import bpy

EXCLUDE_PREFIX = (
    "147VR_Bay2", "147VR_GameAssets_Imported", "147VR_PlayerSpawn_FX",
    "147VR_Pool_Marks", "147VR_Plant_Alternates_Hidden",
)

# per-collection ratios (of BASE tris, from analysis) — target total env ~155k
# note: several ArtPass objects carry pre-existing subsurf/decimate modifiers, so
# evaluated input can be several× denser than the raw base tris in the analysis.
RATIO_MAP = [
    ("147 VR", 0.10),
    ("Static_Baked", 0.03),
    ("Dynamic", 0.03),
    ("Imported_Props_And_Maxell", 0.025),
    ("PartyBalloons", 0.005),
    ("AAA_147VR_Bar_Transfer", 0.05),
    ("147VR_Club_Furnishings", 0.05),
    ("147VR_Ceiling_Lighting", 0.10),
    ("147VR_AAA_Mirror", 0.12),
    ("147VR_NewBulbs", 0.05),
    ("AAA_Lounge_Furniture", 0.05),
    ("AAA_Architectural_Finish", 1.0),
]
SMALL_KEEP = 6000  # collections with base tris below this: no decimation
default_ratio = 0.10
MIN_RATIO = 0.004

TEX_EXTS = ('.png', '.jpg', '.jpeg', '.exr', '.hdr', '.tif', '.tiff',
            '.dds', '.tga', '.webp', '.bmp')


def sanitize(name: str) -> str:
    bad = '<>:"/\\|?*'
    s = name
    for ch in bad:
        s = s.replace(ch, "_")
    s = re.sub(r"\s+", " ", s).strip()
    return s[:120] if len(s) > 120 else s


def read_json(path: str):
    with open(path, "r", encoding="utf-8-sig") as f:
        return json.load(f)


def mat_binding(m):
    """Per-material data for Unity: scalars + per-socket image names via node walk."""
    out = {"name": m.name}
    if not getattr(m, "use_nodes", False) or not getattr(m, "node_tree", None):
        return out
    tree = m.node_tree
    bsdf = None
    for n in tree.nodes:
        if n.type == 'BSDF_PRINCIPLED':
            bsdf = n
            break
    if bsdf is None:
        return out

    def gv(socket_name):
        s = bsdf.inputs.get(socket_name)
        if s is None:
            return None
        try:
            v = s.default_value
        except Exception:
            return None
        if hasattr(v, "__len__"):
            return [round(float(x), 4) for x in v]
        return round(float(v), 4)

    out.update({
        "base_color": gv("Base Color"),
        "metallic": gv("Metallic"),
        "roughness": gv("Roughness"),
        "alpha": gv("Alpha"),
        "emission_color": gv("Emission Color"),
        "emission_strength": gv("Emission Strength"),
    })

    socket_map = {"Base Color": "base", "Normal": "normal", "Roughness": "rough",
                  "Metallic": "metal", "Emission Color": "emission", "Alpha": "alpha",
                  "Specular IOR Level": "spec", "Clearcoat": "clearcoat"}

    pass_through = ('BSDF_DIFFUSE', 'BSDF_GLOSSY', 'BSDF_GLASS', 'BSDF_REFRACTION',
                    'BSDF_TRANSLUCENT', 'BSDF_TRANSPARENT', 'BSDF_SSS', 'BSDF_PRINCIPLED',
                    'BSDF_EMISSION', 'BSDF_VELVET', 'OUTPUT_MATERIAL', 'OUTPUT', 'BSDF_SHEEN')

    hits = {}
    for link in tree.links:
        if link.to_node != bsdf:
            continue
        n = link.from_node
        hops = 0
        while n is not None and n.type != 'TEX_IMAGE' and hops < 3:
            nxt = None
            for l2 in tree.links:
                if l2.to_node == n and l2.from_node.type in ('TEX_IMAGE', 'NORMAL_MAP',
                                                             'MIX', 'MIX_RGB', 'VALTORGB',
                                                             'BRIGHTCONTRAST', 'HUE_SAT'):
                    nxt = l2.from_node
                    break
            if nxt is None or nxt.type in pass_through:
                n = None
            else:
                n = nxt
            hops += 1
        if n is not None and n.type == 'TEX_IMAGE' and getattr(n, "image", None):
            key = socket_map.get(link.to_socket.name)
            if key and key not in hits:
                hits[key] = n.image.name
    if hits:
        out["tex"] = hits
    return out


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    analysis_path, out_root, budget = argv[0], argv[1], float(argv[2])
    analysis = read_json(analysis_path)

    # ---- per-object classification (single source of truth) -------------------
    obj_first_col = {}
    for o in analysis["objects"]:
        if o["type"] == "MESH":
            obj_first_col.setdefault(o["name"], o.get("collection"))

    def ratio_for(cname: str, base_tris: int) -> float:
        if base_tris <= SMALL_KEEP:
            return 1.0
        for prefix, r in RATIO_MAP:
            if cname.startswith(prefix):
                return r
        return default_ratio

    base_tris_by_name = {o["name"]: o.get("tris", 0) for o in analysis["objects"]
                         if o.get("type") == "MESH"}
    col_entries = []
    for cname, data in analysis["collections"].items():
        if any(cname.startswith(p) for p in EXCLUDE_PREFIX):
            continue
        sub = [base_tris_by_name[n] for n in data["direct_objects"]
               if base_tris_by_name.get(n, 0) > 0
               and obj_first_col.get(n) == cname]
        col_entries.append({"name": cname, "tris": sum(sub), "n": len(sub)})
    included = [e for e in col_entries if e["n"] > 0]

    # twin detection: identical tri+count signature in one family, keep the first
    fam_sig, fam_keep, twins = {}, {}, {}
    for e in included:
        fam = e["name"].split(" | ")[0]
        sig = (e["tris"], e["n"])
        if fam not in fam_sig:
            fam_sig[fam] = sig
            fam_keep[fam] = e["name"]
        elif fam_sig[fam] == sig:
            twins[e["name"]] = fam_keep[fam]
    included = [e for e in included if e["name"] not in twins]

    col_names = {e["name"] for e in included}

    # ---- AT MOST ONE decimate modifier per mesh object -------------------------
    def our_mods(o):
        return [m for m in o.modifiers if m.name.startswith("DEC_QST")]

    def set_ratio(o, ratio):
        mods = our_mods(o)
        for m in mods:
            m.ratio = ratio
        return mods[0] if mods else None

    def ensure_one(o, ratio):
        mods = our_mods(o)
        if not mods:
            mods = [o.modifiers.new("DEC_QST", 'DECIMATE')]
        for m in mods:
            m.ratio = ratio
        return mods[0]

    applied = {}
    for cname in col_names:
        for o in bpy.data.collections[cname].objects:
            if o.type != 'MESH' or not o.visible_get():
                continue
            if o.name in applied:
                continue
            bt = base_tris_by_name.get(o.name, 0)
            r = max(MIN_RATIO, min(1.0, ratio_for(cname, bt)))
            ensure_one(o, r)
            applied[o.name] = (cname, r)

    # ---- exact measurement with fresh depsgraph each round --------------------
    def measured_total():
        bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()
        total = 0
        for oname in applied:
            o = bpy.data.objects.get(oname)
            if o is None:
                continue
            ev = o.evaluated_get(dg)
            me = ev.to_mesh()
            total += sum(len(p.vertices) - 2 for p in me.polygons)
            ev.to_mesh_clear()
        return total

    total = measured_total()
    rounds = 0
    while total > budget and rounds < 6:
        f = (budget / total) ** 0.85
        f = max(0.5, f)
        for oname, (cname, r) in applied.items():
            o = bpy.data.objects.get(oname)
            if o:
                new_r = max(MIN_RATIO, min(1.0, r * f))
                set_ratio(o, new_r)
                applied[oname] = (cname, new_r)
        total = measured_total()
        rounds += 1

    # ---- clean + export --------------------------------------------------------
    if os.path.isdir(out_root):
        for f in os.listdir(out_root):
            p = os.path.join(out_root, f)
            if f.endswith(".fbx"):
                os.remove(p)
    os.makedirs(out_root, exist_ok=True)
    texdir = os.path.join(out_root, "textures")
    os.makedirs(texdir, exist_ok=True)

    scene = bpy.context.scene
    manifest = {"source": bpy.data.filepath, "budget": budget, "fbx": [],
                "textures": [], "materials": [], "lights": [], "world": None,
                "cameras": [], "report": {}}

    world = scene.world
    if world and getattr(world, "use_nodes", False):
        for n in world.node_tree.nodes:
            if n.type == 'BACKGROUND':
                manifest["world"] = {"color": list(n.inputs["Color"].default_value),
                                     "strength": n.inputs["Strength"].default_value}
    manifest["tone"] = {"engine": scene.render.engine,
                        "view_transform": scene.view_settings.view_transform,
                        "exposure": scene.view_settings.exposure}

    for o in bpy.data.objects:
        if o.type == "CAMERA":
            mt = o.matrix_world
            manifest["cameras"].append({
                "name": o.name,
                "active": scene.camera is not None and o.name == scene.camera.name,
                "loc": list(mt.to_translation()), "rot_euler": list(mt.to_euler()),
                "lens": o.data.lens, "sensor": o.data.sensor_width,
                "clip": [o.data.clip_start, o.data.clip_end],
                "resolution": [scene.render.resolution_x, scene.render.resolution_y]})

    # ---- textures ---------------------------------------------------------------
    for img in bpy.data.images:
        if img.source not in ('FILE', 'PACKED') or img.size[0] == 0:
            continue
        entry = {"name": img.name, "size": list(img.size), "file": None}
        base, ext = os.path.splitext(img.name)
        if ext.lower() not in TEX_EXTS:
            srcext = os.path.splitext(bpy.path.abspath(img.filepath_raw or ''))[1].lower()
            ext = srcext if srcext else '.png'
        fname = sanitize(base) + ext
        target = os.path.join(texdir, fname)
        try:
            if img.packed_file is not None:
                with open(target, 'wb') as fo:
                    fo.write(img.packed_file.data)
            else:
                src = bpy.path.abspath(img.filepath_raw)
                if src and os.path.isfile(src):
                    shutil.copy2(src, target)
                else:
                    manifest["textures"].append(entry)
                    continue
            entry["file"] = "textures/" + fname
            entry["bpp"] = img.depth
        except Exception as e:
            entry["error"] = str(e)
        manifest["textures"].append(entry)

    # ---- materials ---------------------------------------------------------------
    for m in bpy.data.materials:
        try:
            manifest["materials"].append(mat_binding(m))
        except Exception as e:
            manifest["materials"].append({"name": m.name, "error": str(e)})

    # ---- per-collection FBX ------------------------------------------------------
    exp_ok, exp_fail = 0, 0
    post_cols = {}
    for cname in sorted(col_names):
        objs = [o for o in bpy.data.collections[cname].objects
                if o.type in ('MESH', 'EMPTY') and o.visible_get()]
        if not objs:
            continue
        for o in bpy.data.objects:
            o.select_set(False)
        for o in objs:
            o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        fpath = os.path.join(out_root, sanitize(cname) + ".fbx")
        try:
            dgx = bpy.context.evaluated_depsgraph_get()
            tris = 0
            for o in objs:
                if o.type != 'MESH':
                    continue
                ev = o.evaluated_get(dgx)
                me = ev.to_mesh()
                tris += sum(len(p.vertices) - 2 for p in me.polygons)
                ev.to_mesh_clear()
            post_cols[cname] = tris
            bpy.ops.export_scene.fbx(filepath=fpath, use_selection=True,
                                     object_types={'MESH', 'EMPTY'},
                                     use_mesh_modifiers=True,
                                     mesh_smooth_type='OFF',
                                     path_mode='ABSOLUTE', embed_textures=False,
                                     bake_anim=False, add_leaf_bones=False,
                                     axis_forward='-Z', axis_up='Y',
                                     use_space_transform=True, global_scale=1.0,
                                     use_custom_props=False, use_metadata=True)
            if os.path.isfile(fpath):
                exp_ok += 1
                manifest["fbx"].append({"name": os.path.basename(fpath),
                                        "collection": cname,
                                        "objects": len(objs), "tris_after": tris})
            else:
                exp_fail += 1
                manifest["fbx"].append({"name": os.path.basename(fpath),
                                        "collection": cname, "error": "no file written"})
        except Exception as err:
            exp_fail += 1
            manifest["fbx"].append({"name": os.path.basename(fpath),
                                    "collection": cname, "error": str(err)})

    # ---- lights (replicas decided in Unity) --------------------------------------
    for o in bpy.data.objects:
        if o.type == "LIGHT":
            d = o.data
            li = {"name": o.name,
                  "collection": o.users_collection[0].name if o.users_collection else None,
                  "type": d.type, "color": list(d.color), "energy": d.energy,
                  "loc": list(o.matrix_world.to_translation()),
                  "rot_euler": list(o.matrix_world.to_euler()), "vis": o.visible_get()}
            if d.type == 'SPOT':
                li["spot_size"] = d.spot_size
                li["spot_blend"] = d.spot_blend
            if d.type == 'AREA':
                li["size"] = [d.size, d.size_y if d.shape in ('RECTANGLE', 'ELLIPSE') else d.size]
            manifest["lights"].append(li)

    manifest["report"] = {"collections_included": sorted(col_names),
                          "twins_skipped": list(twins.keys()),
                          "post_tris_per_collection": post_cols,
                          "post_total": total, "iterations": rounds,
                          "fbx_ok": exp_ok, "fbx_failed": exp_fail,
                          "textures_written": sum(1 for t in manifest["textures"] if t.get("file")),
                          "textures_total": len(manifest["textures"]),
                          "materials_total": len(manifest["materials"])}

    with open(os.path.join(out_root, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=1)
    print("STAGE3TRIS:" + str(total))
    print("STAGE3FBX:" + str(exp_ok) + " ok / " + str(exp_fail) + " failed")
    print("STAGE3TEX:" + str(manifest["report"]["textures_written"]) + " / "
          + str(manifest["report"]["textures_total"]))
    print("STAGE3WROTE:" + out_root)


main()
