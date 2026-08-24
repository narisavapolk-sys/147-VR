"""FIX Chubby magic girl scattered head parts + re-rig + export + render verification.

Root cause of scatter: the previous script scaled each mesh around ITS OWN origin
(transform_apply), so pieces whose origin sat at head height (hat/hair/eyes/mouth)
shrank in SIZE but stayed at the OLD 2.9m head height -> floating 2m above body.

Fix: bake each mesh's world transform into its vertices, reset to identity, then
scale vertices around the WORLD ORIGIN (ground) so every piece scales together.

Pipeline (mirrors rig_original_chubby_for_dance.py):
1. Open original, bake world transforms, remove rigs/widgets, scale around world origin
2. VERIFY per-piece bboxes vs body (no SCATTERED)
3. Import first dance -> Mixamo rig, auto-weights + KDTree copy
4. Import remaining dances, harvest actions
5. Join all meshes, NLA strips, RENDER 2 frames for visual check
6. Export FBX
"""
import bpy, os, time, math, sys
from mathutils import Matrix, Vector

SKIP_RENDER = "--no-render" in sys.argv

BASE = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table"
BLEND_DIR = os.path.join(BASE, "Blender")
OUT = os.path.join(BASE, "FBX")
IMG = os.path.join(BASE, "Images")

DANCES = [
    "Arms Hip Hop Dance.fbx",
    "Booty Hip Hop Dance.fbx",
    "Dancing Twerk.fbx",
    "Hip Hop Dancing (1).fbx",
    "Hip Hop Dancing.fbx",
    "Rumba Dancing.fbx",
]

t0 = time.time()
def log(msg):
    print(f"[{time.time()-t0:6.1f}s] {msg}", flush=True)

def world_bbox(o):
    mat = o.matrix_world
    xs, ys, zs = [], [], []
    for v in o.data.vertices:
        c = mat @ v.co
        xs.append(c.x); ys.append(c.y); zs.append(c.z)
    return (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))

# --- 1. Open original ---
orig = os.path.join(BLEND_DIR, "Chubby magic girl.blend")
log("open " + orig)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath=orig)

# Bake EVERY mesh's world transform into its vertices BEFORE removing parents,
# so local coords = world coords and origins are all at world origin.
meshes = [o for o in bpy.data.objects if o.type == 'MESH' and
          not (o.name.startswith('WGT-') or o.name.startswith('WGT_'))]
log(f"meshes to process: {len(meshes)}")
for m in meshes:
    m.data.transform(m.matrix_world)
    m.matrix_world = Matrix.Identity(4)
    m.hide_set(False)
    m.hide_viewport = False
    m.hide_select = False
log("baked world transforms -> all origins at world origin")

# Remove non-mesh objects (rigs, lights, empties, cameras, widgets)
removed = []
for o in list(bpy.data.objects):
    if o.type != 'MESH':
        nm = o.name
        bpy.data.objects.remove(o, do_unlink=True)
        removed.append(nm)
log(f"removed {len(removed)} non-mesh objects")
for m in meshes:
    if m.name not in bpy.context.scene.collection.objects:
        bpy.context.scene.collection.objects.link(m)

# --- 2. Scale around WORLD ORIGIN ---
body = bpy.data.objects['chubby_body']
blo, bhi = world_bbox(body)
current_h = bhi[2] - blo[2]
target_h = 1.57
s = target_h / current_h
log(f"scale factor {s:.4f} ({current_h:.3f}m -> {target_h:.3f}m) around world origin")
sm = Matrix.Scale(s, 4)
for m in meshes:
    m.data.transform(sm)

# --- 2b. VERIFY per-piece bboxes vs body ---
blo, bhi = world_bbox(body)
bcen = ((blo[0]+bhi[0])/2, (blo[1]+bhi[1])/2, (blo[2]+bhi[2])/2)
log(f"BODY after scale: z={blo[2]:.3f}..{bhi[2]:.3f} height={bhi[2]-blo[2]:.3f}")
n_scattered = 0
for m in meshes:
    if m == body:
        continue
    lo, hi = world_bbox(m)
    cen = ((lo[0]+hi[0])/2, (lo[1]+hi[1])/2, (lo[2]+hi[2])/2)
    flag = ""
    if cen[2] > bhi[2] + 0.12 or cen[2] < blo[2] - 0.12:
        flag = "  <-- SCATTERED"
        n_scattered += 1
    log(f"  MESH {m.name!r}: z={lo[2]:.3f}..{hi[2]:.3f} center=({cen[0]:.3f},{cen[1]:.3f},{cen[2]:.3f}){flag}")
log(f"SCATTERED_COUNT={n_scattered} (must be 0)")

# --- 3. Import first dance -> rig ---
log("import first dance: " + DANCES[0])
bpy.ops.import_scene.fbx(filepath=os.path.join(BLEND_DIR, DANCES[0]))
rig = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
rig.name = "CuteDanceRig"
log(f"rig: {rig.name} bones={len(rig.data.bones)}")

# --- 4. Parent meshes to rig ---
bpy.ops.object.select_all(action='DESELECT')
for m in meshes:
    m.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
log("parent with automatic weights...")
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
for m in meshes:
    log(f"  {m.name}: vgroups={len(m.vertex_groups)}")

# KDTree copy for meshes auto-weights skipped
from mathutils.kdtree import KDTree
src = body
kd = KDTree(len(src.data.vertices))
for i, v in enumerate(src.data.vertices):
    kd.insert(v.co, i)
kd.balance()
src_groups = list(src.vertex_groups)
src_weights = {}
for v in src.data.vertices:
    src_weights[v.index] = [(g.group, g.weight) for g in v.groups]
for m in meshes:
    if len(m.vertex_groups) == 0:
        log(f"copy weights body -> {m.name}")
        name_to_idx = {}
        for g in src_groups:
            if g.name not in m.vertex_groups:
                name_to_idx[g.name] = m.vertex_groups.new(name=g.name).index
            else:
                name_to_idx[g.name] = m.vertex_groups[g.name].index
        for v in m.data.vertices:
            co, src_idx, dist = kd.find(v.co)
            if src_idx is None:
                continue
            for (gidx, w) in src_weights.get(src_idx, []):
                v.groups.add(name_to_idx[src_groups[gidx].name], w, 'REPLACE')
        bpy.ops.object.select_all(action='DESELECT')
        m.select_set(True)
        bpy.context.view_layer.objects.active = m
        bpy.ops.object.vertex_group_normalize_all()
        bpy.ops.object.parent_set(type='ARMATURE_AUTO')
        log(f"  {m.name}: vgroups now={len(m.vertex_groups)}")

# --- 5. Import remaining dances, harvest actions ---
first_base = DANCES[0].replace(".fbx", "").replace(" ", "_").replace("(", "").replace(")", "")
bpy.data.actions[0].name = "Cute_" + first_base
for fn in DANCES[1:]:
    log("import " + fn)
    known_actions = set(bpy.data.actions.keys())
    bpy.ops.import_scene.fbx(filepath=os.path.join(BLEND_DIR, fn))
    new_arms = [o for o in bpy.data.objects if o.type == 'ARMATURE' and o.name != 'CuteDanceRig']
    new_meshes = [o for o in bpy.data.objects if o.type == 'MESH' and o not in meshes]
    new_actions = [a for a in bpy.data.actions if a.name not in known_actions]
    base = fn.replace(".fbx", "").replace(" ", "_").replace("(", "").replace(")", "")
    if new_actions:
        new_actions[0].name = "Cute_" + base
    for a in new_arms:
        bpy.data.objects.remove(a, do_unlink=True)
    for m in new_meshes:
        bpy.data.objects.remove(m, do_unlink=True)

# --- 6. Join all meshes into one ---
log("join all meshes into one")
coll_names = set(bpy.context.scene.collection.objects.keys())
for m in meshes:
    if m.name not in coll_names:
        bpy.context.scene.collection.objects.link(m)
    m.hide_set(False)
    m.hide_viewport = False
    m.hide_select = False
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
for m in meshes:
    m.select_set(True)
bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
for m in meshes:
    m.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.join()
joined = meshes[0]
joined.name = "ChubbyGirlFull"
bpy.ops.object.select_all(action='DESELECT')
joined.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
log(f"joined: {joined.name} verts={len(joined.data.vertices)} vgroups={len(joined.vertex_groups)} mats={len(joined.data.materials)}")

# --- 7. NLA strips ---
log("push actions into NLA on the rig")
anim = rig.animation_data
if anim is None:
    anim = rig.animation_data_create()
anim.action = None
for tr in list(anim.nla_tracks):
    anim.nla_tracks.remove(tr)
for a in bpy.data.actions:
    track = anim.nla_tracks.new()
    track.name = a.name
    track.strips.new(a.name, 0, a)

# --- 7b. RENDER visual check (2 frames, front view) ---
def render_check(joined):
    log("render verification frames...")
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1600
    # world
    if scene.world is None:
        w = bpy.data.worlds.new("CheckWorld")
        scene.world = w
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.22, 0.24, 0.28, 1.0)
    # lights
    for i, (x, y, z, e) in enumerate([(1.0, -3.0, 1.2, 400), (-1.0, -3.0, 1.2, 400), (0.5, -2.0, 3.0, 300)]):
        ld = bpy.data.lights.new(f"ChkL{i}", 'AREA')
        ld.energy = e; ld.size = 1.0
        lo = bpy.data.objects.new(f"ChkL{i}", ld)
        scene.collection.objects.link(lo)
        lo.location = (x, y, z)
    # camera
    lo_, hi_ = world_bbox(joined)
    lo_ = Vector(lo_); hi_ = Vector(hi_)
    c = (lo_ + hi_) / 2
    h = hi_[2] - lo_[2]
    cam_data = bpy.data.cameras.new("CheckCam")
    cam = bpy.data.objects.new("CheckCam", cam_data)
    scene.collection.objects.link(cam)
    dist = h * 2.2
    cam.location = (c.x, c.y - dist, c.z + h * 0.25)
    dirv = Vector((c.x, c.y, c.z + h * 0.2)) - cam.location
    cam.rotation_euler = dirv.to_track_quat('-Z', 'Y').to_euler()
    cam_data.lens = 55
    scene.camera = cam
    scene.render.image_settings.file_format = 'PNG'
    # two frames: near-rest + mid-dance
    for fname, frame in [("_chubby_fix_rest", 5), ("_chubby_fix_dance", 150)]:
        scene.frame_set(frame)
        scene.render.filepath = os.path.join(IMG, fname + ".png")
        bpy.ops.render.render(write_still=True)
        log("RENDERED " + fname + ".png @ frame " + str(frame))

if SKIP_RENDER:
    log("skipping render (--no-render)")
else:
    render_check(joined)

# --- 8. Export ---
out_path = os.path.join(OUT, "Chubby Girl Dancing.fbx")
log("export " + out_path)
bpy.ops.export_scene.fbx(
    filepath=out_path,
    object_types={'ARMATURE', 'MESH'},
    add_leaf_bones=False,
    apply_unit_scale=True,
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=True,
    bake_anim_step=1,
)
log("EXPORTED " + out_path)
log("ALL_DONE")
