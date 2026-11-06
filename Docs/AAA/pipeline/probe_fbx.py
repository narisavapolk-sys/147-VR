# probe_fbx.py — inspect Blender 5.2 FBX export props + image states (read-only)
import bpy  # noqa

props = []
try:
    rt = bpy.ops.export_scene.fbx.get_rna_type()
    props = [p.identifier for p in rt.properties]
except Exception as e:
    print("PROBE_ERR:" + repr(e))
print("FBXPROPS:" + ",".join(props))

imgs = [i for i in bpy.data.images if i.source in ('FILE', 'PACKED')]
packed = [i for i in imgs if i.packed_file is not None]
free = [i for i in imgs if i.packed_file is None]
print('IMG_TOTAL:' + str(len(imgs)))
print('IMG_PACKED:' + str(len(packed)))
print('IMG_FREE:' + str(len(free)))

import os
exist = 0
missing = []
for i in free:
    p = bpy.path.abspath(i.filepath_raw or '')
    if p and os.path.isfile(p):
        exist += 1
    else:
        missing.append(i.name + '|' + (i.filepath_raw or ''))
print('IMG_FREE_EXIST:' + str(exist))
print('IMG_FREE_MISSING:' + str(len(missing)))
for m in missing[:8]:
    print('MISSING:' + m)

if packed:
    d = packed[0].packed_file.data
    print('PACKED_DATA:' + str(type(d).__name__) + ' ' + str(len(d) if d is not None else -1) + ' name=' + packed[0].name)
