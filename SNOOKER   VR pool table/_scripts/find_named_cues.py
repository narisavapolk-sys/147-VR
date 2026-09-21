import bpy
for o in bpy.context.scene.objects:
 n=o.name.lower()
 if any(k in n for k in ['cue','stick','billiard']):
  print(o.name,o.type,tuple(round(x,3) for x in o.dimensions),tuple(round(x,3) for x in o.matrix_world.translation))
