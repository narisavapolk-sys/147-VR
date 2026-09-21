import bpy, os, math, shutil
SRC=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Base_v001.blend'
OUT=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v002.blend'
bpy.ops.wm.open_mainfile(filepath=SRC)
# 1) Identify the existing white/cue ball: this mesh is already white, but was misnamed Green.001.
white=bpy.data.objects.get('Green.001')
if white and white.get('asset_role')=='SNOOKER_BALL':
    white.name='White_CueBall'
    white['ball_type']='WHITE'
# 2) Rebuild the 15-red triangle using the measured 52.578 mm ball diameter.
reds=[o for o in bpy.context.scene.objects if o.get('asset_role')=='SNOOKER_BALL' and o.name.lower().startswith('red')]
reds.sort(key=lambda o:o.name)
pink=bpy.data.objects.get('Pink')
D=0.052578
if pink and len(reds)==15:
    apex_x=pink.location.x-D
    k=math.sqrt(3.0)/2.0
    i=0
    for row in range(5):
        x=apex_x-row*k*D
        for j in range(row+1):
            y=(j-row/2.0)*D
            reds[i].location.x=x
            reds[i].location.y=y
            reds[i].location.z=pink.location.z
            i+=1
# 3) Ensure exact visual colors in Blender materials.
colors={'Red':(0.8,0.0,0.0,1),'Yellow':(0.95,0.75,0.02,1),'Green':(0.03,0.30,0.06,1),'Brown':(0.20,0.055,0.015,1),'Blue':(0.02,0.18,0.80,1),'Pink':(0.95,0.20,0.55,1),'Black':(0.006,0.006,0.006,1),'White':(0.92,0.92,0.92,1)}
def set_color(mat,c):
    mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF')
    if bs:
        bs.inputs['Base Color'].default_value=c
        bs.inputs['Roughness'].default_value=0.22
        if 'Specular IOR Level' in bs.inputs: bs.inputs['Specular IOR Level'].default_value=0.45
for o in bpy.context.scene.objects:
    if o.get('asset_role')!='SNOOKER_BALL' or o.type!='MESH': continue
    key='Red' if o.name.lower().startswith('red') else ('White' if o.name=='White_CueBall' else o.name.split('.')[0])
    if key in colors and o.data.materials:
        set_color(o.data.materials[0],colors[key])
# 4) There are no cue-stick objects in this source (no Cue/Stick names and no long cue mesh); do not delete the table's short support rods.
# Save as new version, preserving v001 untouched.
bpy.ops.wm.save_as_mainfile(filepath=OUT)
print('SAVED',OUT)
print('BALLS',sum(1 for o in bpy.context.scene.objects if o.get('asset_role')=='SNOOKER_BALL'))
print('WHITE',white.name if white else 'MISSING')
print('REDS',len(reds))
for o in sorted(reds,key=lambda x:(x.location.x,x.location.y)):
    print('RED',tuple(round(v,6) for v in o.location))
