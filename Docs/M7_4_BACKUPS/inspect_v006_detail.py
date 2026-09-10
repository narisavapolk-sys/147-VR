import bpy
for name in ['Plane','BALL Markings','D Marking']:
    o=bpy.data.objects.get(name)
    if not o: continue
    print('OBJ',name,'parent=',o.parent.name if o.parent else None,'hide=',o.hide_render)
    print('  mats=',[(m.name if m else None) for m in o.data.materials])
    print('  collections=',[c.name for c in o.users_collection])
    print('  modifiers=',[(m.name,m.type) for m in o.modifiers])
