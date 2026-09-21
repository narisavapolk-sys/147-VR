import bpy, bmesh, os
blend=r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007_MARKING.blend"
outblend=r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v008_MARKING_CLEAN.blend"
outfbx=r"C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\Source\147VR_Table_WPBSA_Visual_Clean_v008_MARKING_CLEAN.fbx"
bpy.ops.wm.open_mainfile(filepath=blend)
o=bpy.data.objects['TABLE FRAME']; me=o.data
# Identify the same 8 genuinely self-intersecting ngons found by the deterministic audit.
def orient(a,b,c): return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
def proper(a,b,c,d,eps=1e-9):
 o1,o2,o3,o4=orient(a,b,c),orient(a,b,d),orient(c,d,a),orient(c,d,b)
 return ((o1>eps and o2<-eps) or (o1<-eps and o2>eps)) and ((o3>eps and o4<-eps) or (o3<-eps and o4>eps))
bad=set()
for p in me.polygons:
 n=len(p.vertices)
 if n<5: continue
 pts=[o.matrix_world@me.vertices[i].co for i in p.vertices]
 for drop in range(3):
  ds=[i for i in range(3) if i!=drop]; q=[(v[ds[0]],v[ds[1]]) for v in pts]
  for i in range(n):
   i2=(i+1)%n
   for j in range(i+1,n):
    j2=(j+1)%n
    if i==j or i2==j or j2==i: continue
    if proper(q[i],q[i2],q[j],q[j2]): bad.add(p.index); break
   if p.index in bad: break
  if p.index in bad: break
print('BAD_FACE_IDS',sorted(bad))
# Triangulate only the offending faces; preserve every other face untouched.
bm=bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table(); faces=[bm.faces[i] for i in sorted(bad)]
bmesh.ops.triangulate(bm,faces=faces,quad_method='BEAUTY',ngon_method='BEAUTY')
bm.to_mesh(me); bm.free(); me.update()
print('POST_POLYGONS',len(me.polygons),'NGONS',sum(1 for p in me.polygons if len(p.vertices)>4))
bpy.ops.wm.save_as_mainfile(filepath=outblend)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=outfbx,use_selection=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',embed_textures=False,add_leaf_bones=False)
print('SAVED',outblend); print('EXPORTED',outfbx)
