# =============================================================================
#  147 VR  -  HEADLESS RENDER DRIVER for the AAA Aristocrat table (v2 + v3.1)
# =============================================================================
#  WHY THIS EXISTS
#    Manual screenshots failed once already because the viewport was in Solid
#    shading, so the gold table looked grey. This driver renders with a real
#    engine and real lights, so shading can never be wrong.
#
#  HOW TO RUN  (one line, no GUI, no clicking)
#    & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" `
#        --background --factory-startup --python render_v3_shots.py
#
#  OPTIONAL ENV OVERRIDES
#    V2_PATH   full path to 147VR_AAA_Aristocrat_SnookerTable.py
#    V3_PATH   full path to patch_v3_aristocrat_legs.py
#    OUT_DIR   where the PNGs go (default C:\Temp\147VR_TABLE_V3)
#
#  OUTPUT
#    01_hero.png        hero 3/4 view
#    02_lowangle.png    low angle, whole leg visible
#    03_legcloseup.png  close-up of one leg (reeding + gold must be visible)
#    render_report.txt  object/face counts + engine used
# =============================================================================

import bpy
import os
import sys
import math
from mathutils import Vector

V2_PATH = os.environ.get(
    "V2_PATH",
    r"C:\Users\mongo\OneDrive\Desktop\\โต๊ะ ฉาก brender\147VR_AAA_Aristocrat_SnookerTable.py",
)
V3_PATH = os.environ.get("V3_PATH", r"C:\Temp\patch_v3_aristocrat_legs.py")
OUT_DIR = os.environ.get("OUT_DIR", r"C:\Temp\147VR_TABLE_V3")

RES_X, RES_Y = 1600, 900
SAMPLES = 48

LOG = []


def log(msg):
    line = "[RENDER] " + str(msg)
    print(line)
    LOG.append(line)


def run_script(path, label):
    if not os.path.isfile(path):
        log("FATAL: %s not found -> %s" % (label, path))
        return False
    log("running %s -> %s" % (label, path))
    with open(path, "r", encoding="utf-8") as fh:
        src = fh.read()
    g = {"__name__": "__main__", "__file__": path}
    exec(compile(src, path, "exec"), g)
    log("%s done" % label)
    return True


def clean_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    log("scene reset to empty")


def pick_engine():
    scn = bpy.context.scene
    for name in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "BLENDER_WORKBENCH"):
        try:
            scn.render.engine = name
            log("engine = " + name)
            return name
        except Exception:
            continue
    log("engine = default (%s)" % scn.render.engine)
    return scn.render.engine


def setup_world():
    w = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    bpy.context.scene.world = w
    w.use_nodes = True
    bg = w.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.045, 0.048, 0.055, 1.0)
        bg.inputs[1].default_value = 0.9
    log("world set")


def add_light(name, kind, loc, energy, size=2.0, rot=(0, 0, 0)):
    d = bpy.data.lights.new(name, type=kind)
    d.energy = energy
    if kind == "AREA":
        d.size = size
    ob = bpy.data.objects.new(name, d)
    ob.location = loc
    ob.rotation_euler = rot
    bpy.context.scene.collection.objects.link(ob)
    return ob


def setup_lights():
    # broadcast-style canopy over the table, plus two rim lights
    add_light("KEY_Canopy", "AREA", (0.0, 0.0, 2.60), 900.0, size=3.2,
              rot=(0.0, 0.0, 0.0))
    add_light("FILL_Left", "AREA", (-3.20, -2.60, 1.90), 260.0, size=2.0,
              rot=(math.radians(58), 0.0, math.radians(35)))
    add_light("RIM_Right", "AREA", (3.40, 2.40, 1.70), 220.0, size=2.0,
              rot=(math.radians(62), 0.0, math.radians(-140)))
    log("3 lights added")


def find_a_leg():
    legs = sorted([o for o in bpy.data.objects if "TBL_Legs" in o.name],
                  key=lambda o: o.name)
    return legs[0] if legs else None


def make_target(name, loc):
    ob = bpy.data.objects.new(name, None)
    ob.empty_display_size = 0.1
    ob.location = loc
    bpy.context.scene.collection.objects.link(ob)
    return ob


def make_cam(name, loc, target, lens):
    d = bpy.data.cameras.new(name)
    d.lens = lens
    ob = bpy.data.objects.new(name, d)
    ob.location = loc
    bpy.context.scene.collection.objects.link(ob)
    c = ob.constraints.new("TRACK_TO")
    c.target = target
    c.track_axis = "TRACK_NEGATIVE_Z"
    c.up_axis = "UP_Y"
    return ob


def render_to(cam, filename, engine):
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
    log("wrote " + scn.render.filepath + ".png"
        if not filename.endswith(".png") else "wrote " + scn.render.filepath)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)

    clean_scene()
    if not run_script(V2_PATH, "v2 base table"):
        return 2
    if not run_script(V3_PATH, "v3.1 leg patch"):
        return 3

    engine = pick_engine()
    setup_world()
    setup_lights()

    # ---- inventory -------------------------------------------------------
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    faces = sum(len(o.data.polygons) for o in meshes)
    legs = [o for o in bpy.data.objects if "TBL_Legs" in o.name]
    log("objects=%d  meshes=%d  faces=%d  legs=%d"
        % (len(bpy.data.objects), len(meshes), faces, len(legs)))
    log("skirt present  = %s" % bool(bpy.data.objects.get("TBL_GoldSkirt")))
    log("sightband      = %s" % bool(bpy.data.objects.get("TBL_SightBand")))

    # ---- cameras ---------------------------------------------------------
    tgt_table = make_target("AIM_Table", (0.0, 0.0, 0.82))
    cam_hero = make_cam("RCAM_Hero", (-4.30, -4.00, 2.10), tgt_table, 40.0)
    cam_low = make_cam("RCAM_Low", (-2.60, -3.10, 0.42), tgt_table, 35.0)

    leg = find_a_leg()
    if leg:
        lp = leg.matrix_world.translation
        tgt_leg = make_target("AIM_Leg", (lp.x, lp.y, 0.38))
        cam_leg = make_cam(
            "RCAM_Leg",
            (lp.x - 0.85, lp.y - 0.95, 0.52), tgt_leg, 85.0)
        log("leg closeup on %s at (%.3f, %.3f)" % (leg.name, lp.x, lp.y))
    else:
        cam_leg = None
        log("WARNING: no TBL_Legs object found - skipping leg closeup")

    # ---- render ----------------------------------------------------------
    render_to(cam_hero, "01_hero", engine)
    render_to(cam_low, "02_lowangle", engine)
    if cam_leg:
        render_to(cam_leg, "03_legcloseup", engine)

    rep = os.path.join(OUT_DIR, "render_report.txt")
    with open(rep, "w", encoding="utf-8") as fh:
        fh.write("\n".join(LOG) + "\n")
    print("[RENDER] report -> " + rep)
    print("[RENDER] === ALL DONE ===")
    return 0


if __name__ == "__main__":
    sys.exit(main())
