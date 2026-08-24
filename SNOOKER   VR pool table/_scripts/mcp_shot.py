"""mcp_shot.py — Capture a REAL viewport image from the running Blender (via MCP).

Usage:
    python mcp_shot.py front   [out.png] [--shade MATERIAL|SOLID|RENDERED] [--max 1000]
    python mcp_shot.py side    [out.png] ...
    python mcp_shot.py top     [out.png] ...
    python mcp_shot.py iso     [out.png] ...
    python mcp_shot.py keep    [out.png] ...   (keep current camera, just capture)

This lets us VERIFY work visually (not just numbers) before importing into Unity:
set the viewport to a known angle, capture what Blender actually shows, and save
a PNG we can inspect. Works with the official blender-mcp addon commands
`execute_code` (set view) + `get_viewport_screenshot` (capture).

Requires: Blender GUI running with the BlenderMCP addon server started (port 9876).
"""
import socket
import json
import sys
import os
import math

HOST, PORT = "127.0.0.1", 9876

def send(cmd: dict, timeout: float = 120.0) -> dict:
    s = socket.create_connection((HOST, PORT), timeout=8)
    s.settimeout(timeout)
    try:
        s.sendall(json.dumps(cmd).encode("utf-8"))
        buf = b""
        while True:
            chunk = s.recv(65536)
            if not chunk:
                break
            buf += chunk
            try:
                return json.loads(buf.decode("utf-8"))
            except json.JSONDecodeError:
                continue
    finally:
        s.close()

def fail(msg):
    print("MCP_SHOT_ERROR: " + msg)
    sys.exit(1)

def main():
    if len(sys.argv) < 2:
        fail("usage: mcp_shot.py <front|side|top|iso|keep|all> [out.png] [--shade X] [--max N]")
    angle = sys.argv[1].lower()
    args = sys.argv[2:]

    out = None
    shade = "MATERIAL"
    max_size = 1000
    i = 0
    while i < len(args):
        a = args[i]
        if a == "--shade" and i + 1 < len(args):
            shade = args[i + 1].upper(); i += 2
        elif a == "--max" and i + 1 < len(args):
            max_size = int(args[i + 1]); i += 2
        elif not a.startswith("--"):
            out = a; i += 1
        else:
            i += 1
    if out is None:
        out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Images", f"_mcp_{angle}.png")
    out = os.path.abspath(out)
    os.makedirs(os.path.dirname(out), exist_ok=True)

    # 1) set the viewport angle + shading via execute_code
    if angle != "keep":
        view_code = _view_code(angle, shade)
        r = send({"type": "execute_code", "params": {"code": view_code}})
        if r.get("status") != "success":
            fail(f"execute_code failed: {r}")

    # 2) capture the real viewport image
    r = send({"type": "get_viewport_screenshot",
              "params": {"filepath": out, "max_size": max_size, "format": "png"}})
    res = r.get("result", r)
    if r.get("status") != "success" or not res.get("success"):
        fail(f"screenshot failed: {r}")
    print(json.dumps({"ok": True, "filepath": out, "method": res.get("method"),
                      "width": res.get("width"), "height": res.get("height")}, ensure_ascii=False))

def _view_code(angle, shade):
    """Blender Python: point the active 3D viewport at the scene objects, set angle."""
    # directions: where the camera sits (world space), looking at the scene center
    dirs = {
        "front": (0.0, -1.0, 0.0),   # look from -Y (in front)
        "side":  (1.0, 0.0, 0.0),    # look from +X
        "top":   (0.0, 0.0, 1.0),    # look from +Z
        "iso":   (0.7, -0.7, 0.7),   # 3/4 view
    }
    d = dirs.get(angle, dirs["iso"])
    dx, dy, dz = d
    code = (
        "import bpy, math\n"
        "from mathutils import Vector\n"
        "objs = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o.visible_get()]\n"
        "if not objs:\n"
        "    objs = [o for o in bpy.context.scene.objects if o.type in ('MESH','ARMATURE')]\n"
        "mn = Vector((1e9,1e9,1e9)); mx = Vector((-1e9,-1e9,-1e9))\n"
        "for o in objs:\n"
        "    for v in o.bound_box:\n"
        "        p = o.matrix_world @ Vector(v)\n"
        "        for i in range(3):\n"
        "            mn[i] = min(mn[i], p[i]); mx[i] = max(mx[i], p[i])\n"
        "center = (mn + mx) / 2\n"
        "radius = max((mx - mn).length / 2, 0.01)\n"
        "for a in bpy.context.screen.areas:\n"
        "    if a.type == 'VIEW_3D':\n"
        "        for r in a.regions:\n"
        "            if r.type == 'WINDOW':\n"
        "                r3d = a.spaces.active.region_3d\n"
        "                r3d.view_perspective = 'PERSP'\n"
        "                d = Vector((__DX__, __DY__, __DZ__))\n"
        "                if d.length < 0.5:\n"
        "                    d = Vector((0.7, -0.7, 0.7))\n"
        "                d.normalize()\n"
        "                r3d.view_rotation = d.to_track_quat('-Z', 'Y')\n"
        "                r3d.view_location = center\n"
        "                r3d.view_distance = radius * 2.6\n"
        "                a.spaces.active.shading.type = '__SHADE__'\n"
        "print('view set: angle=__ANGLE__ shade=__SHADE__ center=%.3f radius=%.3f' % (center.x, radius))\n"
    )
    return (code.replace("__DX__", repr(dx)).replace("__DY__", repr(dy)).replace("__DZ__", repr(dz))
            .replace("__SHADE__", shade).replace("__ANGLE__", angle))

if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1].lower() == "all":
        # capture 4 angles to a folder; each is a full round-trip
        base_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Images")
        for ang in ("front", "side", "top", "iso"):
            sys.argv = [sys.argv[0], ang] + sys.argv[2:]
            if not any(a == "--out-dir" for a in sys.argv):
                # force default output path per angle
                sys.argv = [s for s in sys.argv if not s.endswith(".png")]
            main()
    else:
        main()
