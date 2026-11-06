# stage2_analysis.py — run INSIDE Blender headless, READ-ONLY (no save)
#	blender --background --factory-startup "<file.blend>" --python stage2_analysis.py -- <out.json>
import json
import sys

import bpy  # noqa


def img_info(img):
    return {
        "name": img.name,
        "size": list(img.size),
        "filepath": img.filepath_raw or "",
        "packed": img.packed_file is not None,
        "source": img.source,
    }


def node_tex(n):
    if n.type != 'TEX_IMAGE':
        return None
    if not n.image:
        return None
    return {"node": n.name, "image": n.image.name, "filepath": n.image.filepath_raw or ""}


def principled_data(m):
    bsdf = None
    for n in m.node_tree.nodes:
        if n.type == 'BSDF_PRINCIPLED':
            bsdf = n
            break
    if bsdf is None:
        return {"type": m.node_tree and ("custom-nodes") }
    data = {}
    ins = bsdf.inputs
    def val(sock):
        try:
            if hasattr(sock, "default_value"):
                v = sock.default_value
                if hasattr(v, "__len__"):
                    return list(round(x, 4) for x in v)
                return round(float(v), 4)
        except Exception:
            return None
        return None
    for sock_name in ("Base Color", "Metallic", "Roughness", "Alpha",
                      "Emission Color", "Emission Strength", "Normal"):
        if sock_name in ins:
            data[sock_name] = val(ins.get(sock_name))
    data["textures"] = [t for t in (node_tex_v for sock in ins for node_tex_v in [node_tex(sock)]) if t]
    data["any_images"] = [t for t in (node_tex(n) for n in m.node_tree.nodes) if t]
    return data


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out_path = argv[0] if argv else "analysis.json"

    scene = bpy.context.scene
    report = {"file": bpy.data.filepath, "engine": scene.render.engine}

    # render settings summary
    report["render"] = {
        "engine": scene.render.engine,
        "resolution": [scene.render.resolution_x, scene.render.resolution_y],
        "view_tone_map": getattr(scene.view_settings, "view_transform", None),
        "exposure": getattr(scene.view_settings, "exposure", None),
        "film_transparent": scene.render.film_transparent,
    }

    # world
    try:
        w = scene.world
        if w and w.use_nodes:
            bg = None
            for n in w.node_tree.nodes:
                if n.type == 'BACKGROUND':
                    bg = {"color": list(n.inputs["Color"].default_value),
                          "strength": round(n.inputs["Strength"].default_value, 3),
                          "linked_texture": node_tex(n)}
            report["world"] = {"name": w.name, "background": bg}
        else:
            report["world"] = None
    except Exception as e:
        report["world"] = {"error": str(e)}

    # collections and objects
    cols = {}
    for c in bpy.data.collections:
        objs = [o.name for o in c.objects]
        h_objs = [o.name for o in c.all_objects if o.name not in objs]
        cols[c.name] = {"direct_objects": objs, "total": len(c.all_objects)}
    report["collections"] = cols

    # objects summary (ALL)
    objs = []
    for o in bpy.data.objects:
        e = {
            "name": o.name, "type": o.type,
            "collection": o.users_collection[0].name if o.users_collection else None,
            "visible": o.visible_get(),
            "loc": [round(x, 4) for x in o.location],
            "rot": [round(x, 4) for x in o.rotation_euler],
            "scale": [round(x, 4) for x in o.scale],
        }
        if o.type == "MESH":
            try:
                e["tris"] = sum(len(p.vertices) - 2 for p in o.data.polygons)
            except Exception:
                e["tris"] = -1
            e["materials"] = [sl.material.name if sl.material else None for sl in o.material_slots]
        if o.type == "LIGHT":
            d = o.data
            e["light"] = {
                "type": d.type, "color": list(d.color),
                "energy": round(d.energy, 3), "spot_size": round(d.spot_size, 4) if d.type in ("SPOT",) else None,
                "spot_blend": round(d.spot_blend, 4) if d.type in ("SPOT",) else None,
                "shadow_soft_size": round(d.shadow_soft_size, 4),
                "diffuse_factor": d.use_shadow if hasattr(d, "use_shadow") else None,
                "cast_shadow": getattr(d, "cast_shadow", None),
            }
        if o.type == "CAMERA":
            e["camera"] = {"lens": round(o.data.lens, 3), "sensor": round(o.data.sensor_width, 3),
                           "shift": [round(x, 4) for x in (o.data.shift_x, o.data.shift_y)]}
        objs.append(e)
    report["objects"] = objs

    # lights quick list
    report["light_count"] = sum(1 for o in bpy.data.objects if o.type == "LIGHT")
    report["camera_count"] = sum(1 for o in bpy.data.objects if o.type == "CAMERA")

    # materials
    mats = []
    for m in bpy.data.materials:
        info = {"name": m.name, "use_nodes": m.use_nodes,
                "alpha_back": getattr(m, "blend_method", None)}
        if m.use_nodes:
            info["principled"] = principled_data(m)
        mats.append(info)
    report["materials"] = mats

    # textures
    report["images"] = [img_info(i) for i in bpy.data.images]

    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=1)
    print("STAGE2WROTE:" + out_path)


main()
