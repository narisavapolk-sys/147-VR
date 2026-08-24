"""Rig Cute Girl SLIM to Mixamo armature, combine all 6 dance actions, export FBX.

Steps:
1. Import first dance FBX -> armature + action
2. Import Cute Girl SLIM.fbx (mesh, has rebuilt legs)
3. Parent mesh to armature with automatic weights
4. Import remaining dance FBXs, keep their actions, delete their armatures
5. Assign all actions to the single rig via NLA, export FBX (all actions baked)
"""
import bpy, os, sys, time

BASE = r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table"
BLEND_DIR = os.path.join(BASE, "Blender")
FBX_DIR = os.path.join(BASE, "FBX")
OUT = os.path.join(BASE, "FBX")

DANCES = [
    "Arms Hip Hop Dance.fbx",
    "Booty Hip Hop Dance.fbx",
    "Dancing Twerk.fbx",
    "Hip Hop Dancing (1).fbx",
    "Hip Hop Dancing.fbx",
    "Rumba Dancing.fbx",
]

def obj_type(t):
    return [o for o in bpy.data.objects if o.type == t]

t0 = time.time()
def log(msg):
    print(f"[{time.time()-t0:6.1f}s] {msg}", flush=True)

log("fresh scene")
bpy.ops.wm.read_factory_settings(use_empty=True)

# --- 1. Import first dance to get the rig ---
log("import first dance: " + DANCES[0])
bpy.ops.import_scene.fbx(filepath=os.path.join(BLEND_DIR, DANCES[0]))
rig = obj_type('ARMATURE')[0]
rig.name = "CuteDanceRig"
log(f"rig: {rig.name} bones={len(rig.data.bones)}")

# First action -> rename to a clean name
acts = [a for a in bpy.data.actions]
log(f"actions after first import: {[a.name for a in acts]}")

# --- 2. Import Cute Girl SLIM mesh ---
log("import Cute Girl SLIM.fbx")
cute_path = os.path.join(FBX_DIR, "Cute Girl SLIM.fbx")
bpy.ops.import_scene.fbx(filepath=cute_path)
meshes = [o for o in bpy.data.objects if o.type == 'MESH' and o.name not in ('Armature',)]
log(f"meshes: {[(m.name, len(m.data.vertices)) for m in meshes]}")

# --- 3. Parent meshes to rig with automatic weights ---
bpy.ops.object.select_all(action='DESELECT')
for m in meshes:
    m.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
log("parent with automatic weights (may take a while)...")
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
log("weights done")

# --- 4. Import remaining dances, harvest actions, delete their rigs ---
# First action -> rename cleanly now
first_base = DANCES[0].replace(".fbx", "").replace(" ", "_").replace("(", "").replace(")", "")
bpy.data.actions[0].name = "Cute_" + first_base

for fn in DANCES[1:]:
    log("import " + fn)
    known_actions = set(bpy.data.actions.keys())
    bpy.ops.import_scene.fbx(filepath=os.path.join(BLEND_DIR, fn))
    new_arms = [o for o in bpy.data.objects if o.type == 'ARMATURE' and o.name != 'CuteDanceRig']
    new_meshes = [o for o in bpy.data.objects if o.type == 'MESH' and o.name not in [m.name for m in meshes] and not o.name.startswith('CuteDance')]
    new_actions = [a for a in bpy.data.actions if a.name not in known_actions]
    log(f"  new armatures: {[a.name for a in new_arms]}, new actions: {[a.name for a in new_actions]}")
    # Rename the new action immediately (before deleting the rig that references it)
    base = fn.replace(".fbx", "").replace(" ", "_").replace("(", "").replace(")", "")
    if new_actions:
        new_actions[0].name = "Cute_" + base
    # Delete the extra armature + its meshes (we only want the action)
    for a in new_arms:
        bpy.data.objects.remove(a, do_unlink=True)
    for m in new_meshes:
        bpy.data.objects.remove(m, do_unlink=True)

log(f"all actions: {[a.name for a in bpy.data.actions]}")

# --- 5. Assign all actions to the rig via NLA strips ---
log("push actions into NLA on the rig")
anim = rig.animation_data
if anim is None:
    anim = rig.animation_data_create()
# clear existing action
anim.action = None
# remove existing tracks
for tr in list(anim.nla_tracks):
    anim.nla_tracks.remove(tr)
for a in bpy.data.actions:
    track = anim.nla_tracks.new()
    track.name = a.name
    strip = track.strips.new(a.name, 0, a)
    log(f"  NLA track {track.name} strip {strip.name}")

# --- 6. Export FBX with all actions ---
out_path = os.path.join(OUT, "Cute Girl Dancing.fbx")
log("export " + out_path)
bpy.ops.export_scene.fbx(
    filepath=out_path,
    use_selection=False,
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
