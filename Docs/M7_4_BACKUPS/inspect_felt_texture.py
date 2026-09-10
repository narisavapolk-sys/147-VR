import bpy
img=bpy.data.images.get('Green Felt Texture-2.png')
print('IMAGE_FOUND',bool(img))
if img:
    print('SIZE',img.size[0],img.size[1], 'CHANNELS',img.channels)
    print('SOURCE',img.filepath_raw)
    # Estimate bright-pixel coverage; D/spots would create significant bright areas.
    w,h=img.size
    px=list(img.pixels)
    step=max(1,int((w*h)/200000))
    bright=mid=0; total=0
    for i in range(0,w*h,step):
        r,g,b=px[i*img.channels:i*img.channels+3]
        total+=1
        if min(r,g,b)>0.65: bright+=1
        if min(r,g,b)>0.45 and max(r,g,b)>0.45: mid+=1
    print('BRIGHT_GT_065',bright,'/',total,'=',bright/total if total else 0)
    print('LIGHT_GT_045',mid,'/',total,'=',mid/total if total else 0)
