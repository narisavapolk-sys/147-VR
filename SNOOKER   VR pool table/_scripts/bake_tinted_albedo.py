"""Bake each skin's color into the PBR albedo textures.

A plain RGB multiply can't shift hue (a brown wood texture with almost no blue
channel can never become navy that way), so we rotate the texture's hue in HSV
space toward the skin color, keep saturation, then normalize value so the
result keeps readable brightness. Texture grain/detail is fully preserved.
"""
import os

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PBR = os.path.join(ROOT, "SNOOKER   VR pool table", "Blender", "figs", "pbr")
os.makedirs(PBR, exist_ok=True)


def rgb_to_hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    d = mx - mn
    h = np.zeros_like(mx)
    nz = d > 1e-6
    rn, gn, bn = r[nz], g[nz], b[nz]
    dn = d[nz]
    mxn = mx[nz]
    h[nz] = np.where(mxn == rn, ((gn - bn) / dn) % 6,
           np.where(mxn == gn, (bn - rn) / dn + 2.0, (rn - gn) / dn + 4.0))
    h = h / 6.0
    s = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0.0)
    v = mx
    return np.stack([h, s, v], axis=-1)


def hsv_to_rgb(hsv):
    h, s, v = hsv[..., 0] * 6.0, hsv[..., 1], hsv[..., 2]
    i = np.floor(h).astype(int) % 6
    f = h - np.floor(h)
    p = v * (1 - s)
    q = v * (1 - f * s)
    t = v * (1 - (1 - f) * s)
    out = np.empty_like(hsv)
    r = np.select([i == 0, i == 1, i == 2, i == 3, i == 4, i == 5],
                  [v, q, p, p, t, v])
    g = np.select([i == 0, i == 1, i == 2, i == 3, i == 4, i == 5],
                  [t, v, v, q, p, p])
    b = np.select([i == 0, i == 1, i == 2, i == 3, i == 4, i == 5],
                  [p, p, t, v, v, q])
    return np.stack([r, g, b], axis=-1)


def recolor(src_path, target_hue, target_lum, out_path):
    img = np.asarray(Image.open(src_path).convert("RGB")).astype(np.float32) / 255.0
    hsv = rgb_to_hsv(img)
    # Mean hue of the source (hue is circular; average via complex numbers).
    ang = np.angle(np.mean(np.exp(1j * 2 * np.pi * hsv[..., 0])))
    mean_hue = (ang / (2 * np.pi)) % 1.0
    delta = (target_hue - mean_hue) % 1.0
    hsv[..., 0] = (hsv[..., 0] + delta) % 1.0
    out = hsv_to_rgb(hsv)
    # Normalize value so mean luminance lands near the target.
    lum = out.mean()
    scale = target_lum / max(lum, 1e-5)
    out = np.clip(out * scale, 0.0, 1.0)
    res = Image.fromarray((out * 255.0).astype(np.uint8))
    res.save(out_path)
    print(f"baked {os.path.basename(out_path)} mean_hue={mean_hue:.3f} shift={delta:.3f} scale={scale:.2f}")


# target hues (0..1): red~0.0/1.0, navy~0.61, forest green~0.33, walnut brown~0.08
SKINS = {
    "main": {
        "felt_src": "felt_diff.jpg", "felt_hue": 0.985, "felt_lum": 0.30,
        "wood_src": "wood_navy_diff.jpg", "wood_hue": 0.615, "wood_lum": 0.18,
    },
    "walnut": {
        "felt_src": "felt_diff.jpg", "felt_hue": 0.333, "felt_lum": 0.24,
        "wood_src": "wood_walnut_diff.jpg", "wood_hue": 0.085, "wood_lum": 0.30,
    },
    "blue": {
        "felt_src": "felt_diff.jpg", "felt_hue": 0.580, "felt_lum": 0.26,
        "wood_src": "wood_navy_diff.jpg", "wood_hue": 0.075, "wood_lum": 0.20,
    },
}

for skin, cfg in SKINS.items():
    recolor(os.path.join(PBR, cfg["felt_src"]), cfg["felt_hue"], cfg["felt_lum"],
            os.path.join(PBR, f"felt_albedo_{skin}.png"))
    recolor(os.path.join(PBR, cfg["wood_src"]), cfg["wood_hue"], cfg["wood_lum"],
            os.path.join(PBR, f"wood_albedo_{skin}.png"))

print("DONE")
