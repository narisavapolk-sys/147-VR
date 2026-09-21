import bpy, json
print('FILE',bpy.data.filepath)
for o in bpy.context.scene.objects:
    if o.type=='MESH':
        n=o.name.lower()
        if any(k in n for k in ['red','pink','blue','yellow','green','brown','black','white','cue','rod']):
            print(json.dumps({'name':o.name,'loc':[round(x,6) for x in o.matrix_world.translation],'mats':[m.name if m else None for m in o.data.materials]}))
