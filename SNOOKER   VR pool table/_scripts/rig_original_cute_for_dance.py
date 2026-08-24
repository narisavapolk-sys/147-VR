"""Rig the ORIGINAL full Cute Girl 5.2 (with clothes) to Mixamo armature, combine 6 dance actions, export FBX.

Steps:
1. Open original Cute Girl 5.2.blend (body + bikini/boot/jacket/pant/sock/top + hair/eyes/lashes)
2. Import first dance FBX -> armature + action
3. Parent ALL meshes to armature with automatic weights
4. Import remaining dance FBXs, keep their actions, delete their armatures
5. Export FBX (all actions baked)
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

# --- 1. Open original Cute Girl (with clothes) ---
orig = os.path.join(BLEND_DIR, "Cute Girl 5.2.blend")
log("open original: " + orig)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.open_mainfile(filepath=orig)

# remove lights/empties (keep meshes only)
for o in list(bpy.data.objects):
    if o.type not in ('MESH',):
        bpy.data.objects.remove(o, do_unlink=True)
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
log(f"original meshes: {[(m.name, len(m.data.vertices)) for m in meshes]}")

# --- 2. Import first dance -> rig ---
log("import first dance: " + DANCES[0])
bpy.ops.import_scene.fbx(filepath=os.path.join(BLEND_DIR, DANCES[0]))
rig = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
rig.name = "CuteDanceRig"
log(f"rig: {rig.name} bones={len(rig.data.bones)}")

# --- 3. Parent meshes to rig with auto weights ---
bpy.ops.object.select_all(action='DESELECT')
for m in meshes:
    m.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
log("parent with automatic weights...")
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
for m in meshes:
    log(f"  {m.name}: vgroups={len(m.vertex_groups)}")
log("weights done")

# Fix meshes that got no weights (open cloth meshes: bikini/pant/top)
# Copy weights from the body mesh via KDTree nearest-vertex lookup
from mathutils.kdtree import KDTree
body_mesh = bpy.data.objects['body']
kd = KDTree(len(body_mesh.data.vertices))
for i, v in enumerate(body_mesh.data.vertices):
    kd.insert(v.co, i)
kd.balance()
src_groups = list(body_mesh.vertex_groups)
src_weights = {}
for v in body_mesh.data.vertices:
    src_weights[v.index] = [(g.group, g.weight) for g in v.groups]
for m in meshes:
    if len(m.vertex_groups) == 0:
        log(f"copy weights body -> {m.name}")
        # create same-named groups
        name_to_idx = {}
        for g in src_groups:
            if g.name not in m.vertex_groups:
                ng = m.vertex_groups.new(name=g.name)
                name_to_idx[g.name] = ng.index
            else:
                name_to_idx[g.name] = m.vertex_groups[g.name].index
        for v in m.data.vertices:
            co, src_idx, dist = kd.find(v.co)
            if src_idx is None:
                continue
            for (gidx, w) in src_weights.get(src_idx, []):
                gname = src_groups[gidx].name
                v.groups.add(name_to_idx[gname], w, 'REPLACE')
        # normalize
        bpy.ops.object.select_all(action='DESELECT')
        m.select_set(True)
        bpy.context.view_layer.objects.active = m
        bpy.ops.object.vertex_group_normalize_all()
        bpy.ops.object.parent_set(type='ARMATURE_AUTO')
        log(f"  {m.name}: vgroups now={len(m.vertex_groups)}")

# --- 4. Import remaining dances, harvest actions ---
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
    log(f"  actions now: {[a.name for a in bpy.data.actions]}")

# --- 4b. Join all meshes into one (FBX/Unity drops cloth meshes embedded in body) ---
log("join all meshes into one")
scene_objs = set(bpy.context.scene.objects)
missing = [m.name for m in meshes if m not in scene_objs]
log(f"  meshes not in scene: {missing}")
# re-link any orphans back to the scene collection
for m in meshes:
    if m.name not in bpy.context.scene.objects:
        bpy.context.scene.collection.objects.link(m)
# unhide everything (original CC3i file hides bikini/pant/top)
for m in meshes:
    m.hide_set(False)
    m.hide_viewport = False
    m.hide_select = False
# temporarily unparent so join works cleanly (keep transform)
bpy.ops.object.select_all(action='DESELECT')
for m in meshes:
    m.select_set(True)
sel_names = [o.name for o in bpy.context.selected_objects]
not_sel = [m.name for m in meshes if m.name not in sel_names]
for mn in not_sel:
    mm = bpy.data.objects[mn]
    log(f"  UNSEL {mn}: hide={mm.hide_get()} hide_viewport={mm.hide_viewport} hide_select={mm.hide_select} visible_in_viewlayer={mm.visible_get()} in_cur_coll={mm.name in bpy.context.view_layer.active_layer_collection.collection.objects}")
log(f"  selected before join: {len(sel_names)} not_sel={not_sel} active={bpy.context.active_object.name if bpy.context.active_object else None}")
bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
for m in meshes:
    m.select_set(True)
log(f"  selected after clear: {len([o for o in bpy.context.selected_objects])} active={bpy.context.active_object.name if bpy.context.active_object else None}")
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.join()
joined = meshes[0]
joined.name = "CuteGirlFull"
# re-parent to rig + add armature modifier
bpy.ops.object.select_all(action='DESELECT')
joined.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
log(f"joined: {joined.name} verts={len(joined.data.vertices)} vgroups={len(joined.vertex_groups)} mats={len(joined.data.materials)}")

# --- 5. NLA strips for all actions ---
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

# --- 6. Export ---
out_path = os.path.join(OUT, "Cute Girl Original Dancing.fbx")
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
