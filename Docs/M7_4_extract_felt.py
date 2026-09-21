import bpy, os
blend=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007_MARKING.blend'
out=r'C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\Textures\Green_Felt_Texture_V007.png'
bpy.ops.wm.open_mainfile(filepath=blend)
m=bpy.data.materials.get('FELT')
if not m: raise RuntimeError('FELT missing')
imgs=[n.image for n in m.node_tree.nodes if n.bl_idname=='ShaderNodeTexImage' and n.image]
if not imgs: raise RuntimeError('FELT image missing')
img=imgs[0]
os.makedirs(os.path.dirname(out),exist_ok=True)
img.filepath_raw=out; img.file_format='PNG'; img.save()
print('EXTRACTED',out,img.size[:],bool(img.packed_file))
