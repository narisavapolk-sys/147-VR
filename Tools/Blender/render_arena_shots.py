# =============================================================================
#  147 VR  -  HEADLESS RENDER DRIVER for the Crucible arena + Aristocrat table
# =============================================================================
#  WHAT IT DOES
#    1) resets Blender to an empty factory scene
#    2) runs 147VR_Arena_Crucible.py   (arena + spectators + table)
#    3) runs patch_v3_aristocrat_legs.py  (upgrades the table legs to gold)
#    4) renders the arena cameras and writes an inventory report
#
#  Nothing is saved over any .blend file. Nothing touches Unity.
#
#  HOW TO RUN
#    & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" `
#        --background --factory-startup --python render_arena_shots.py
#
#  ENV OVERRIDES
#    ARENA_PATH  full path to 147VR_Arena_Crucible.py   (required)
#    V3_PATH     full path to patch_v3_aristocrat_legs.py (optional)
#    OUT_DIR     output folder (default C:\Temp\147VR_ARENA)
#    QUALITY     "QUEST" (default) or "AAA"
#
#  NOTE ON THE TABLE
#    The arena script locates 147VR_AAA_Aristocrat_SnookerTable.py by looking
#    next to itself first. Keep the arena script in the SAME FOLDER as the
#    table script, or the arena will build without a table.
#
#  OUTPUT
#    01_arena_hero.png       broadcast 3/4 view
#    02_arena_wide.png       whole theatre
#    03_arena_broadcast.png  TV angle
#    arena_report.txt        counts + engine + gate lines
# =============================================================================

import bpy
import os
import re
import sys

ARENA_PATH = os.environ.get("ARENA_PATH", "")
V3_PATH = os.environ.get("V3_PATH", "")
OUT_DIR = os.environ.get("OUT_DIR", r"C:\Temp\147VR_ARENA")
QUALITY = os.environ.get("QUALITY", "QUEST").upper()

RES_X, RES_Y = 1600, 900
SAMPLES = 32

LOG = []


def log(msg):
    line = "[ARENA] " + str(msg)
    print(line)
    LOG.append(line)


def write_report(code):
    try:
        os.makedirs(OUT_DIR, exist_ok=True)
        rep = os.path.join(OUT_DIR, "arena_report.txt")
        with open(rep, "w", encoding="utf-8") as fh:
            fh.write("\n".join(LOG) + "\n")
        print("[ARENA] report -> " + rep)
    except Exception as exc:
        print("[ARENA] report failed: %s" % exc)
    return code


def exec_source(src, path, label):
    g = {"__name__": "__main__", "__file__": path}
    exec(compile(src, path, "exec"), g)
    log("%s done" % label)


def run_arena():
    if not ARENA_PATH or not os.path.isfile(ARENA_PATH):
        log("FATAL: ARENA_PATH not found -> %s" % ARENA_PATH)
        return False
    with open(ARENA_PATH, "r", encoding="utf-8") as fh:
        src = fh.read()
    # force the requested quality without editing the file on disk
    new_src, n = re.subn(r'(?m)^QUALITY(\s*)=\s*"[A-Za-z]+"',
                         'QUALITY\\1= "%s"' % QUALITY, src, count=1)
    if n == 1:
        log("QUALITY forced to %s" % QUALITY)
        src = new_src
    else:
        log("WARNING: could not override QUALITY, using file default")
    log("running arena -> %s" % ARENA_PATH)
    exec_source(src, ARENA_PATH, "arena")
    return True


def run_leg_patch():
    if not V3_PATH:
        log("leg patch skipped (V3_PATH not set)")
        return
    if not os.path.isfile(V3_PATH):
        log("WARNING: V3_PATH not found -> %s" % V3_PATH)
        return
    with open(V3_PATH, "r", encoding="utf-8") as fh:
        src = fh.read()
    log("running leg patch -> %s" % V3_PATH)
    exec_source(src, V3_PATH, "leg patch")


def pick_engine():
    scn = bpy.context.scene
    for name in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "BLENDER_WORKBENCH"):
        try:
            scn.render.engine = name
            log("engine = " + name)
            return name
        except Exception:
            continue
    log("engine = " + scn.render.engine)
    return scn.render.engine


def inventory():
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    faces = sum(len(o.data.polygons) for o in meshes)
    legs = [o for o in bpy.data.objects if "TBL_Legs" in o.name]
    seats = [o for o in bpy.data.objects if "Seat" in o.name]
    specs = [o for o in bpy.data.objects if "Spec" in o.name]
    lights = [o for o in bpy.data.objects if o.type == "LIGHT"]
    log("quality       = %s" % QUALITY)
    log("objects       = %d" % len(bpy.data.objects))
    log("meshes        = %d" % len(meshes))
    log("faces         = %d" % faces)
    log("lights        = %d" % len(lights))
    log("legs          = %d" % len(legs))
    log("seat objects  = %d" % len(seats))
    log("spec objects  = %d" % len(specs))
    log("table present = %s" % bool(bpy.data.objects.get("TBL_Cloth")))
    log("skirt present = %s" % bool(bpy.data.objects.get("TBL_GoldSkirt")))
    log("sightband     = %s" % bool(bpy.data.objects.get("TBL_SightBand")))


def render_cam(cam_name, filename, engine):
    cam = bpy.data.objects.get(cam_name)
    if cam is None or cam.type != "CAMERA":
        log("WARNING: camera %s missing - skipped %s" % (cam_name, filename))
        return False
    scn = bpy.context.scene
    scn.camera = cam
    scn.render.resolution_x = RES_X
    scn.render.resolution_y = RES_Y
    scn.render.resolution_percentage = 100
    scn.render.image_settings.file_format = "PNG"
    scn.render.filepath = os.path.join(OUT_DIR, filename)
    if engine.startswith("BLENDER_EEVEE"):
        try:
            scn.eevee.taa_render_samples = SAMPLES
        except Exception:
            pass
    bpy.ops.render.render(write_still=True)
    log("wrote %s.png from %s" % (filename, cam_name))
    return True


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    log("scene reset to empty")

    if not run_arena():
        return write_report(2)
    run_leg_patch()

    engine = pick_engine()
    inventory()

    ok = 0
    ok += 1 if render_cam("CAM_Hero", "01_arena_hero", engine) else 0
    ok += 1 if render_cam("CAM_Wide", "02_arena_wide", engine) else 0
    ok += 1 if render_cam("CAM_Broadcast", "03_arena_broadcast", engine) else 0
    log("renders written = %d" % ok)

    if ok == 0:
        log("=== FAILED: no cameras rendered ===")
        return write_report(4)

    log("=== ALL DONE ===")
    return write_report(0)


if __name__ == "__main__":
    sys.exit(main())
