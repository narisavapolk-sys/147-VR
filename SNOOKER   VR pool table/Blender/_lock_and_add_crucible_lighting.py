import bpy, json, os, hashlib, math
from mathutils import Vector

BASE = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_BASELINE_LOCKED.blend"
OUT = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V01.blend"
SNAP = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_BASELINE_SNAPSHOT.json"
VERIFY = r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_VERIFY.json"

def world_bbox(o):
    return [(o.matrix_world @ Vector(c))[:] for c in o.bound_box]

def state_for(o):
    return {
        "type": o.type,
        "location": [round(x,9) for x in o.location],
        "rotation_euler": [round(x,9) for x in o.rotation_euler],
        "scale": [round(x,9) for x in o.scale],
        "matrix_world": [[round(x,9) for x in row] for row in o.matrix_world],
        "dimensions": [round(x,9) for x in o.dimensions],
        "parent": o.parent.name if o.parent else None,
        "collections": sorted(c.name for c in o.users_collection),
        "world_bbox": [[round(x,9) for x in p] for p in world_bbox(o)],
    }

def is_protected(o):
    n = o.name.upper()
    keys = ["TABLE", "WPBSA", "SPOT_", "BALL", "ANCHOR", "CATCHER", "CUSHION", "POCKET PAD"]
    colkeys = ["147VR_POCKET_ART","147VR_SPATIAL_ANCHORS","BALLS","CUSHIONS","LEGS","BASE TABLE SUPPORTS","POCKET PADS","MARKINGS"]
    return o.type in {"MESH","EMPTY"} and (any(k in n for k in keys) or any(c.name.upper() in colkeys for c in o.users_collection))

def scene_snapshot():
    protected = {}
    for o in bpy.context.scene.objects:
        if is_protected(o):
            protected[o.name] = state_for(o)
    return protected

bpy.ops.wm.open_mainfile(filepath=BASE)

# Baseline geometry/state before ART changes.
protected_before = scene_snapshot()
surface = bpy.data.objects.get("TABLE SURFACE")
if surface is None:
    raise RuntimeError("TABLE SURFACE not found; refusing to place lights.")
bb = world_bbox(surface)
minz = min(p[2] for p in bb)
maxz = max(p[2] for p in bb)
minx,maxx = min(p[0] for p in bb), max(p[0] for p in bb)
miny,maxy = min(p[1] for p in bb), max(p[1] for p in bb)
center = Vector(((minx+maxx)/2, (miny+maxy)/2, (minz+maxz)/2))
top_z = maxz
length = maxx-minx
width = maxy-miny

# Verify D/baulk/spot anchors from world-space meshes without editing them.
def mesh_world_points(name):
    o=bpy.data.objects.get(name)
    if not o or o.type!="MESH":
        return []
    return [o.matrix_world @ v.co for v in o.data.vertices]

dpts=mesh_world_points("WPBSA_DArc")
dinfo={}
if dpts:
    dx=[p.x for p in dpts]; dy=[p.y for p in dpts]; dz=[p.z for p in dpts]
    dinfo={"bbox":[min(dx),max(dx),min(dy),max(dy),min(dz),max(dz)]}
brown=bpy.data.objects.get("SPOT_Brown")
baulk=bpy.data.objects.get("WPBSA_BaulkLine")
if brown:
    dinfo["brown_world"]=[round(x,9) for x in brown.matrix_world.translation]
if baulk:
    dinfo["baulk_world"]=[round(x,9) for x in baulk.matrix_world.translation]

baseline_sha256=hashlib.sha256(open(BASE,"rb").read()).hexdigest()
snapshot={"source_baseline":BASE,"baseline_sha256":baseline_sha256,"table_surface":{"world_bbox":bb,"top_z":top_z,"center":list(center),"length":length,"width":width},"protected":protected_before,"d_arc_check":dinfo}
with open(SNAP,"w",encoding="utf-8") as f:
    json.dump(snapshot,f,indent=2)

# Create ART collection only; never delete or select-all.
art=bpy.data.collections.get("ART_Crucible_Lighting")
if art is None:
    art=bpy.data.collections.new("ART_Crucible_Lighting")
    bpy.context.scene.collection.children.link(art)

def move_to_art(o):
    for c in list(o.users_collection):
        c.objects.unlink(o)
    art.objects.link(o)

def add_area(name, x, y, z, energy, size, size_y):
    data=bpy.data.lights.new(name=name, type='AREA')
    data.energy=energy
    data.color=(1.0,0.965,0.92)
    data.shape='RECTANGLE'
    data.size=size
    data.size_y=size_y
    o=bpy.data.objects.new(name,data)
    art.objects.link(o)
    o.location=(x,y,z)
    target=center
    o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
    return o

# Four broad overhead sources for even head-to-tail illumination.
xs=[minx+0.20*length, minx+0.40*length, minx+0.60*length, minx+0.80*length]
areas=[]
for i,x in enumerate(xs,1):
    areas.append(add_area(f"ART_Table_Area_{i:02d}",x,center.y,top_z+2.4,350.0,1.20,0.90))

# Soft central spot as an atmospheric/definition supplement, not the sole table light.
sdata=bpy.data.lights.new("ART_Table_Spot_Fill",type='SPOT')
sdata.energy=700.0
sdata.color=(1.0,0.98,0.92)
sdata.spot_size=math.radians(65.0)
sdata.spot_blend=0.35
sdata.shadow_soft_size=0.25
spot=bpy.data.objects.new("ART_Table_Spot_Fill",sdata)
art.objects.link(spot)
spot.location=(center.x,center.y,top_z+3.2)
spot.rotation_euler=(center-spot.location).to_track_quat('-Z','Y').to_euler()

# Protect table-related transforms against accidental UI movement.
for o in bpy.context.scene.objects:
    if is_protected(o):
        o.lock_location=(True,True,True)
        o.lock_rotation=(True,True,True)
        o.lock_scale=(True,True,True)

# Save lighting work as a new file. Baseline remains untouched/read-only.
bpy.ops.wm.save_as_mainfile(filepath=OUT)

# Reopen the saved work file and compare protected state.
bpy.ops.wm.open_mainfile(filepath=OUT)
protected_after=scene_snapshot()
changed=[]
for name,b in protected_before.items():
    a=protected_after.get(name)
    if a is None:
        changed.append({"name":name,"issue":"missing"})
        continue
    for key in ["type","location","rotation_euler","scale","matrix_world","dimensions","parent","collections","world_bbox"]:
        if a[key] != b[key]:
            changed.append({"name":name,"field":key,"before":b[key],"after":a[key]})
locks_ok=all(o.lock_location==(True,True,True) and o.lock_rotation==(True,True,True) and o.lock_scale==(True,True,True) for o in bpy.context.scene.objects if is_protected(o))
art_objects=sorted((o.name,o.type,tuple(round(v,5) for v in o.location),round(o.data.energy,2) if o.type=='LIGHT' else None) for o in bpy.data.collections.get("ART_Crucible_Lighting",[]).objects)
out_sha256=hashlib.sha256(open(OUT,"rb").read()).hexdigest()
verify={"baseline":BASE,"baseline_sha256":baseline_sha256,"output":OUT,"output_sha256":out_sha256,"protected_object_count":len(protected_before),"changed_protected_objects":changed,"locks_ok":locks_ok,"art_collection_objects":art_objects}
with open(VERIFY,"w",encoding="utf-8") as f:
    json.dump(verify,f,indent=2)

print("BASELINE_SHA256",baseline_sha256)
print("OUTPUT_SHA256",out_sha256)
print("TABLE_TOP_Z",top_z,"TABLE_CENTER",tuple(center),"SIZE",length,width)
print("PROTECTED_COUNT",len(protected_before),"CHANGES",len(changed),"LOCKS_OK",locks_ok)
print("ART_OBJECTS",art_objects)
