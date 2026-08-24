"""Rig ORIGINAL Chubby magic girl (with clothes) to Mixamo armature, 6 dance actions, export FBX.

Steps:
1. Open Chubby magic girl.blend, remove old rigs + WGT widgets + lights/empties
2. Scale all meshes 2.9m -> ~1.6m (match Cute Girl) around world origin
3. Import first dance FBX -> armature + action
4. Parent ALL meshes to armature with auto weights (+ KDTree copy for skipped cloth)
5. Import remaining dance FBXs, harvest actions
6. Join all meshes into one, export with all actions
"""
import bpy, os, time

BASE = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table"
BLEND_DIR = os.path.join(BASE, "Blender")
OUT = os.path.join(BASE, "FBX")

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

# --- 1. Open original ---
orig = os.path.join(BLEND_DIR, "Chubby magic girl.blend")
log("open " + orig)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath=orig)

# Remove non-mesh objects (rigs, widgets, lights, empties, cameras)
removed = []
for o in list(bpy.data.objects):
    if o.type != 'MESH':
        nm = o.name
        bpy.data.objects.remove(o, do_unlink=True)
        removed.append(nm)
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
# drop WGT-* widgets (rig control shapes, not model parts)
keep = [m for m in meshes if not (m.name.startswith('WGT-') or m.name.startswith('WGT_'))]
dropped_widgets = len(meshes) - len(keep)
for m in keep:
    if m.name not in bpy.context.scene.collection.objects:
        bpy.context.scene.collection.objects.link(m)
meshes = keep
# unhide all (widgets hidden, some clothes hidden?)
for m in meshes:
    m.hide_set(False)
    m.hide_viewport = False
    m.hide_select = False
log(f"removed {len(removed)} non-mesh + {dropped_widgets} WGT widgets; kept {len(meshes)} meshes")
for m in meshes:
    log(f"  MESH {m.name}: verts={len(m.data.vertices)} vgroups={len(m.vertex_groups)} dims={tuple(round(x,2) for x in m.dimensions)}")

# --- 2. Scale to ~1.6m (originally 2.912m tall) ---
# target: match Cute Girl 1.57m. Scale factor = 1.57 / 2.912
body = bpy.data.objects['chubby_body']
current_h = body.dimensions.z
target_h = 1.57
s = target_h / current_h
log(f"scale factor {s:.4f} ({current_h:.2f}m -> {target_h:.2f}m)")
for m in meshes:
    m.scale = (m.scale.x * s, m.scale.y * s, m.scale.z * s)
    # apply transform into verts
    bpy.ops.object.select_all(action='DESELECT')
    m.select_set(True)
    bpy.context.view_layer.objects.active = m
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
log(f"after scale body dims: {tuple(round(x,3) for x in body.dimensions)}")

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

# KDTree copy for meshes auto-weights skipped (hair/hat/etc)
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
