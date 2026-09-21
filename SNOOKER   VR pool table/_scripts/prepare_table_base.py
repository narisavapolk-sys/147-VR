import bpy, os, shutil, re
src=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\REVISED_Snooker_Table_WPBSA_GOLD.blend'
out=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Base_v001.blend'
bpy.ops.wm.open_mainfile(filepath=src)
removed=[]
for o in list(bpy.context.scene.objects):
    n=o.name.lower()
    if any(k in n for k in ('cue','rest','spider','swan','stick','chalk')):
        if not any(k in n for k in ('ball','red','yellow','green','brown','blue','pink','black')):
            removed.append(o.name); bpy.data.objects.remove(o, do_unlink=True)
root=bpy.data.objects.get('147VR_TABLE_VISUAL_ROOT') or bpy.data.objects.new('147VR_TABLE_VISUAL_ROOT',None)
if root.name not in bpy.context.scene.collection.objects: bpy.context.scene.collection.objects.link(root)
root['asset_role']='WPBSA_12FT_VISUAL_BASE'; root['playing_area_m']=[3.569,1.778]; root['ball_diameter_m']=0.0525; root['art_status']='BASE_ONLY_BALLS_PRESERVED'; root['physics_authority']='EXTERNAL_LOCKED'
ball_names=[]
for o in bpy.context.scene.objects:
    n=o.name.lower()
    if any(n==x or n.startswith(x+'.') for x in ['white','cueball','yellow','green','brown','blue','pink','black','red']):
        o['asset_role']='SNOOKER_BALL'; ball_names.append(o.name)
bpy.ops.wm.save_as_mainfile(filepath=out)
print('CREATED',out); print('REMOVED',removed); print('BALL_COUNT',len(ball_names)); print('BALLS',ball_names)
