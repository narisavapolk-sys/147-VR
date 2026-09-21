import bpy
c=bpy.data.collections.get('Cue-.01')
print('CUECOL',c)
if c:
 for o in c.objects: print(o.name,o.type,tuple(round(x,3) for x in o.dimensions),tuple(round(x,3) for x in o.matrix_world.translation))
