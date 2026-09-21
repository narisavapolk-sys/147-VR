import bpy, os
from mathutils import Vector
src=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007.blend'
blend_out=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007_MARKING.blend'
fbx_out=r'C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\Source\147VR_Table_WPBSA_Visual_Clean_v007_MARKING.fbx'
os.makedirs(os.path.dirname(fbx_out),exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=src)
o=bpy.data.objects.get('TABLE SURFACE')
if not o or o.type!='MESH': raise RuntimeError('TABLE SURFACE missing')
mesh=o.data
# Preserve source UVMap. Add a dedicated normalized surface UV channel.
old=mesh.uv_layers.get('MarkingUV')
if old: mesh.uv_layers.remove(old)
uv=mesh.uv_layers.new(name='MarkingUV')
coords=[(o.matrix_world@v.co) for v in mesh.vertices]
xs=[p.x for p in coords]; ys=[p.y for p in coords]
minx,maxx=min(xs),max(xs); miny,maxy=min(ys),max(ys)
for p in mesh.polygons:
    # Only the upward-facing bed gets the exact planar mapping.
    wn=(o.matrix_world.to_3x3()@p.normal).normalized()
    for li in p.loop_indices:
        vi=mesh.loops[li].vertex_index; w=coords[vi]
        u=(w.x-minx)/(maxx-minx); v=(w.y-miny)/(maxy-miny)
        uv.data[li].uv=(u,v)
# Make TABLE SURFACE active UV layer the dedicated one for export consumers that only use UV0.
mesh.uv_layers.active=uv
mesh.uv_layers.active_index=len(mesh.uv_layers)-1
bpy.ops.wm.save_as_mainfile(filepath=blend_out)
# Export a parallel candidate FBX; do not touch the active Unity scene/prefab.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=fbx_out,use_selection=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',embed_textures=False,add_leaf_bones=False)
print('SAVED_BLEND',blend_out)
print('EXPORTED_FBX',fbx_out)
print('SURFACE_BOUNDS',minx,maxx,miny,maxy)
print('UV_LAYER',uv.name)
