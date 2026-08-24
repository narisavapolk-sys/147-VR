import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/FBX/Cute Girl SLIM.fbx")
o = bpy.data.objects['body']
print("UV layers:", [u.name for u in o.data.uv_layers])
m = o.data.materials[0]
print("material nodes:")
if m.use_nodes:
    for n in m.node_tree.nodes:
        if n.type in ('TEX_IMAGE','BSDF_PRINCIPLED'):
            img = n.image.name if n.type=='TEX_IMAGE' and n.image else None
            print("  ", n.type, n.name, "img:", img)
