print("FILE:", bpy.data.filepath)
print("OBJECTS:", [o.name for o in bpy.data.objects if o.type=='MESH'])
