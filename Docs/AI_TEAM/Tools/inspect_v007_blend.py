import bpy, os
p=bpy.data.filepath
print('BLEND',p)
print('UNIT_SYSTEM',bpy.context.scene.unit_settings.system,'scale',bpy.context.scene.unit_settings.scale_length)
for o in bpy.data.objects:
    if o.name=='TABLE SURFACE' or o.name.startswith('V007') or 'TABLE SURFACE' in o.name:
        print('OBJ',o.name,'type',o.type,'parent',o.parent.name if o.parent else None,'loc',tuple(round(x,6) for x in o.location),'rot',tuple(round(x,6) for x in o.rotation_euler),'scale',tuple(round(x,6) for x in o.scale),'det',round(o.matrix_world.to_3x3().determinant(),6))
        if o.type=='MESH':
            me=o.data; print('MESH',me.name,'verts',len(me.vertices),'polys',len(me.polygons),'uv_layers',[(u.name,u.active_render) for u in me.uv_layers])
            for u in me.uv_layers: 
                xs=[d.uv.x for d in u.data]; ys=[d.uv.y for d in u.data]
                print('UV',u.name,'bounds',min(xs),max(xs),min(ys),max(ys))
print('ROOTS')
for o in bpy.context.scene.objects:
    if o.parent is None: print(o.name,o.type,tuple(round(x,4) for x in o.scale),round(o.matrix_world.to_3x3().determinant(),5))
print('CUSTOM')
for k,v in bpy.context.scene.items(): print(k,repr(v))
