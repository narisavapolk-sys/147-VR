"""Generate WPA/BCA standard pool ball textures (n1.png .. n15.png + cueball.png).

Solid 1-7 (colored ball, white circle, black number), 8 black, striped 9-15
(white ball, colored equator band, white circle, black number), cue white.
"""
from PIL import Image, ImageDraw, ImageFont
import os

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Blender", "figs")
os.makedirs(OUT, exist_ok=True)
SIZE = 512

# WPA standard colors (approx)
COLORS = {
    1: (238, 185, 14),     # yellow
    2: (0, 80, 200),       # blue
    3: (210, 30, 30),      # red
    4: (110, 45, 160),     # purple
    5: (255, 130, 0),      # orange
    6: (0, 130, 55),       # green
    7: (130, 25, 35),      # maroon/burgundy
    8: (15, 15, 15),       # black
    9: (238, 185, 14),     # yellow stripe
    10: (0, 80, 200),      # blue stripe
    11: (210, 30, 30),     # red stripe
    12: (110, 45, 160),    # purple stripe
    13: (255, 130, 0),     # orange stripe
    14: (0, 130, 55),      # green stripe
    15: (130, 25, 35),     # maroon stripe
}

font_path = r"C:\Windows\Fonts\arialbd.ttf"
font = ImageFont.truetype(font_path, 130)


def draw_number(img, num, circle_color=(255, 255, 255), text_color=(0, 0, 0)):
    d = ImageDraw.Draw(img)
    cx, cy, r = SIZE // 2, SIZE // 2 - 10, 108
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=circle_color, outline=(200, 200, 200), width=3)
    text = str(num)
    bbox = d.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    d.text((cx - tw / 2 - bbox[0], cy - th / 2 - bbox[1]), text, fill=text_color, font=font)
    return img


def make_solid(num, color):
    img = Image.new("RGB", (SIZE, SIZE), color)
    return draw_number(img, num)


def make_stripe(num, color):
    img = Image.new("RGB", (SIZE, SIZE), (245, 245, 245))
    d = ImageDraw.Draw(img)
    # colored equator band (wraps the middle of the ball in equirect UV)
    d.rectangle([0, SIZE * 0.30, SIZE, SIZE * 0.70], fill=color)
    return draw_number(img, num)


def make_cue():
    img = Image.new("RGB", (SIZE, SIZE), (250, 250, 250))
    d = ImageDraw.Draw(img)
    # subtle sheen dot so cue ball isn't pure flat white
    d.ellipse([SIZE * 0.30, SIZE * 0.22, SIZE * 0.52, SIZE * 0.44], fill=(255, 255, 255))
    return img


for n in range(1, 16):
    if n <= 8:
        img = make_solid(n, COLORS[n])
    else:
        img = make_stripe(n, COLORS[n])
    img.save(os.path.join(OUT, f"n{n}.png"))

make_cue().save(os.path.join(OUT, "cueball.png"))
print("written to", OUT)
print(sorted(os.listdir(OUT)))
