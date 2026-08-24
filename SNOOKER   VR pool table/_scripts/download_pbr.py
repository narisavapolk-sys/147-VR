"""Download CC0 PBR textures from PolyHaven for the pool table materials.

Felt  -> scuba_suede          (fine napped matte fabric, closest to billiard felt)
Walnut wood -> black_walnut_veneer_01 (warm brown fine grain)
Navy/cabinet wood -> wood_cabinet_worn_long (dark furniture wood, will be tinted navy)
"""
import json
import os
import urllib.request

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                    "Blender", "figs", "pbr")
os.makedirs(OUT, exist_ok=True)

JOBS = [
    # (asset name, map type, local filename)
    ("scuba_suede", "Diffuse", "felt_diff.jpg"),
    ("scuba_suede", "nor_gl", "felt_nor_gl.jpg"),
    ("scuba_suede", "Rough", "felt_rough.jpg"),
    ("black_walnut_veneer_01", "Diffuse", "wood_walnut_diff.jpg"),
    ("black_walnut_veneer_01", "nor_gl", "wood_walnut_nor_gl.jpg"),
    ("black_walnut_veneer_01", "Rough", "wood_walnut_rough.jpg"),
    ("wood_cabinet_worn_long", "Diffuse", "wood_navy_diff.jpg"),
    ("wood_cabinet_worn_long", "nor_gl", "wood_navy_nor_gl.jpg"),
    ("wood_cabinet_worn_long", "Rough", "wood_navy_rough.jpg"),
]


def get_json(url):
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    return json.load(urllib.request.urlopen(req, timeout=40))


for asset, mtype, local in JOBS:
    out_path = os.path.join(OUT, local)
    if os.path.exists(out_path) and os.path.getsize(out_path) > 1000:
        print(f"skip {local} (exists)")
        continue
    try:
        files = get_json(f"https://api.polyhaven.com/files/{asset}")
        url = files[mtype]["2k"]["jpg"]["url"]
        req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
        with urllib.request.urlopen(req, timeout=90) as r, open(out_path, "wb") as f:
            f.write(r.read())
        print(f"ok {local} <- {asset}/{mtype}")
    except Exception as e:
        print(f"FAIL {local}: {e}")

print("DONE")
