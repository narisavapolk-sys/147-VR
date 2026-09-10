import bpy
for o in bpy.context.scene.objects:
    if 'TABLE SURFACE' in o.name.upper() or 'SURFACE' in o.name.upper():
        print('OBJECT',o.name,'TYPE',o.type)
        for m in o.data.materials if hasattr(o.data,'materials') else []:
            print(' MATERIAL',m.name)
            if m and m.use_nodes:
                for n in m.node_tree.nodes:
                    if n.type in {'TEX_IMAGE','GROUP','BSDF_PRINCIPLED'}:
                        print('  NODE',n.type,n.name, 'IMAGE='+ (n.image.name if getattr(n,'image',None) else ''))
for m in bpy.data.materials:
    if any(k in m.name.upper() for k in ('CLOTH','FELT','TABLE','SURFACE')):
        print('MAT',m.name)
        if m.use_nodes:
            for n in m.node_tree.nodes:
                if n.type=='TEX_IMAGE': print(' IMG',n.image.name if n.image else 'NONE', n.image.filepath if n.image else '')
