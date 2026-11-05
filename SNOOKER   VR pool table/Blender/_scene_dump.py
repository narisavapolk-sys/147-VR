import bpy
out=[]
def walk(c,depth=0):
    out.append("  "*depth+"COL "+c.name)
    for o in c.objects:
        out.append("  "*(depth+1)+f"OBJ {o.name} | {o.type} | loc={tuple(round(x,3) for x in o.location)} | rot={tuple(round(x,3) for x in o.rotation_euler)} | parent={o.parent.name if o.parent else ''}")
    for ch in c.children:
        walk(ch,depth+1)
for c in bpy.data.collections:
    if c.name not in [x.name for x in c.children]:
        walk(c)
open(r"C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\_scene_dump.txt","w",encoding="utf-8").write("\n".join(out))
print("DUMPED",len(out))
