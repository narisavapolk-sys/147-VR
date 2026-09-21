from PIL import Image, ImageDraw
import os, math

root = r'C:\Users\mongo\UnityProjects\147 VR'
out = os.path.join(root, 'Assets', 'AAA', 'ImportedSnooker', 'Textures', 'Snooker_Markings_V007.png')
os.makedirs(os.path.dirname(out), exist_ok=True)

W, H = 8192, 4096
S = 2
im = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))
d = ImageDraw.Draw(im)

# V007 UV/world basis is verified in the actual Unity scene:
# U = 0.5 - worldX / L
# V = 0.5 - worldZ / A
# PIL pixel Y is inverted relative to UV V, therefore:
# pixelX = (0.5 - worldX / L) * W
# pixelY = (0.5 + worldZ / A) * H
# Do NOT rotate/mirror V007_VISUAL_MAIN. The correction belongs here.
L = 3.569000244140625
A = 1.777999997138977

def px(x, z):
    return ((0.5 - x / L) * W * S, (0.5 + z / A) * H * S)

def line_width_px(mm):
    return max(2 * S, int(round((mm / 1000.0) / L * W * S)))

def spot(x, z, r_mm, fill):
    cx, cy = px(x, z)
    rx = (r_mm / 1000.0 / L) * W * S
    ry = (r_mm / 1000.0 / A) * H * S
    d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=fill)

# Physics-authoritative ball-center coordinates, in the actual Unity X/Z playfield frame.
baulk_x = -1.047500
yellow_z = -0.292000
green_z = 0.292000
brown_z = 0.000000
pink_x = 0.892250
black_x = 1.460500
D_RADIUS = 0.292

line = (220, 220, 210, 210)
dw = line_width_px(2.2)

# Baulk line: constant X, spanning the full table width in Z.
x0, y0 = px(baulk_x, -A / 2)
x1, y1 = px(baulk_x, A / 2)
d.line((x0, y0, x1, y1), fill=line, width=dw)

# D: right/interior half-circle from the baulk line toward the black end.
pts = []
for i in range(181):
    t = math.radians(-90 + i)
    x = baulk_x + D_RADIUS * math.cos(t)
    z = D_RADIUS * math.sin(t)
    pts.append(px(x, z))
d.line(pts, fill=line, width=dw, joint='curve')

# Six spots: 3.5 mm radius, physics-authoritative centers.
spot(baulk_x, yellow_z, 3.5, (226, 196, 35, 220))
spot(baulk_x, green_z, 3.5, (55, 125, 70, 220))
spot(baulk_x, brown_z, 3.5, (125, 82, 45, 220))
spot(0.0, 0.0, 3.5, (70, 105, 185, 220))
spot(pink_x, 0.0, 3.5, (195, 125, 150, 220))
spot(black_x, 0.0, 3.5, (30, 30, 30, 230))

im = im.resize((W, H), Image.Resampling.LANCZOS)
im.save(out, optimize=True)

print('MARKING_TEXTURE', out)
print('SIZE', W, H)
print('BAULK_X', baulk_x, 'D_RADIUS', D_RADIUS)
print('SPOTS', [(baulk_x, yellow_z), (baulk_x, green_z), (baulk_x, brown_z), (0.0, 0.0), (pink_x, 0.0), (black_x, 0.0)])

