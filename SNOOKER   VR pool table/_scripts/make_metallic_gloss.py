"""Pack per-skin PBR roughness variation into a URP metallic-gloss texture.

URP Lit reads smoothness from the ALPHA of the metallic-gloss map
(_MetallicGlossMap) when _SmoothnessTextureChannel = 1 (Metallic Alpha).

PolyHaven "Rough" maps carry roughness in the luminance channel. We invert
those to smoothness (smoothness = 1 - roughness) so that dark/rough areas on
the source map become low-smoothness (rough) on the target, and light/smooth
areas become high-smoothness (glossy).

Output:
  R  = metallic value (1 for metal, 0 for felt/wood)
  A  = smoothness (1 - roughness) * 0.33 for felt, * 0.55 for wood
       (scaled so the noise doesn't overpower, but variation is visible)
"""
import os

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
TEX = os.path.join(ROOT, "Assets", "Models", "PoolTable", "Textures")
os.makedirs(TEX, exist_ok=True)

JOBS = [
    # (roughness source, smoothness scale, metallic R, output)
    ("felt_rough.jpg",       0.33, 0.0, "felt_metallic_gloss.png"),
    ("wood_navy_rough.jpg",  0.55, 0.0, "wood_navy_metallic_gloss.png"),
    ("wood_walnut_rough.jpg", 0.55, 0.0, "wood_walnut_metallic_gloss.png"),
]

for src, smooth_scale, metallic, out in JOBS:
    src_path = os.path.join(TEX, src)
    out_path = os.path.join(TEX, out)
    if not os.path.exists(src_path):
        print(f"skip {src} (missing)")
        continue

    gray = np.asarray(Image.open(src_path).convert("L")).astype(np.float32) / 255.0
    # roughness = gray, smoothness = (1 - roughness) * scale
    smooth = np.clip((1.0 - gray) * smooth_scale, 0.0, 1.0)

    h, w = gray.shape
    rgba = np.zeros((h, w, 4), dtype=np.float32)
    rgba[..., 0] = metallic  # metallic in R
    rgba[..., 3] = smooth    # smoothness in A
    rgba_u8 = (rgba * 255.0).astype(np.uint8)

    Image.fromarray(rgba_u8, "RGBA").save(out_path)
    print(f"wrote {out} metallic={metallic} scale={smooth_scale} "
          f"smooth(min,max,mean)=({smooth.min():.3f},{smooth.max():.3f},{smooth.mean():.3f})")

print("DONE")