"""mcp_verify.py — One-command pre-import visual verification via MCP.

Captures REAL viewport images from the running Blender (4 angles) using
mcp_shot.py, and runs a geometry sanity check (per-mesh bbox vs body) via
execute_code, so we VERIFY WITH EYES + NUMBERS before importing into Unity.

Usage:
    python mcp_verify.py                 # 4 screenshots + geometry check
    python mcp_verify.py --no-geom       # screenshots only
    python mcp_verify.py --shade SOLID   # solid shading for screenshots
"""
import socket
import json
import sys
import os
import subprocess

HOST, PORT = "127.0.0.1", 9876

def ping():
    try:
        s = socket.create_connection((HOST, PORT), timeout=5)
        s.settimeout(5)
        s.sendall(json.dumps({"type": "ping", "params": {}}).encode("utf-8"))
        buf = b""
        while True:
            chunk = s.recv(65536)
            if not chunk:
                break
            buf += chunk
            try:
                r = json.loads(buf.decode("utf-8"))
                return r.get("status") == "success"
            except json.JSONDecodeError:
                continue
    except Exception:
        return False
    return False

def send(cmd, timeout=120.0):
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

GEOM_CODE = r"""
import bpy
from mathutils import Vector

def wbbox(o):
    mat = o.matrix_world
    xs, ys, zs = [], [], []
    for v in o.data.vertices:
        c = mat @ v.co
        xs.append(c.x); ys.append(c.y); zs.append(c.z)
    return (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))

meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o.visible_get()]
body = None
for o in meshes:
    if 'body' in o.name.lower() or 'BODY' in o.name:
        body = o
        break
if body is None and meshes:
    body = max(meshes, key=lambda m: len(m.data.vertices))
if body is None:
    print('GEOM: no meshes'); raise SystemExit
blo, bhi = wbbox(body)
bcen = ((blo[0]+bhi[0])/2, (blo[1]+bhi[1])/2, (blo[2]+bhi[2])/2)
print('GEOM_BODY z=%.3f..%.3f h=%.3f center=(%.3f,%.3f,%.3f)' % (blo[2], bhi[2], bhi[2]-blo[2], bcen[0], bcen[1], bcen[2]))
scat = 0
for o in meshes:
    if o == body:
        continue
    lo, hi = wbbox(o)
    cen = ((lo[0]+hi[0])/2, (lo[1]+hi[1])/2, (lo[2]+hi[2])/2)
    dz = cen[2] - bcen[2]
    flag = ''
    if cen[2] > bhi[2] + 0.12 or cen[2] < blo[2] - 0.12:
        flag = '  <-- SCATTERED'; scat += 1
    print('GEOM_MESH %s: z=%.3f..%.3f dz=%+.3f%s' % (o.name, lo[2], hi[2], dz, flag))
print('GEOM_SCATTERED=%d' % scat)
"""

def main():
    do_geom = "--no-geom" not in sys.argv
    shade = "MATERIAL"
    if "--shade" in sys.argv:
        i = sys.argv.index("--shade")
        if i + 1 < len(sys.argv):
            shade = sys.argv[i + 1].upper()

    if not ping():
        print("MCP_VERIFY_ERROR: Blender MCP not reachable on 127.0.0.1:9876")
        print("  -> Open Blender GUI, enable BlenderMCP addon, press Start MCP Server.")
        sys.exit(1)

    base = os.path.dirname(os.path.abspath(__file__))
    shot = os.path.join(base, "mcp_shot.py")
    # 1) screenshots (4 angles)
    for ang in ("front", "side", "top", "iso"):
        r = subprocess.run([sys.executable, shot, ang, "--shade", shade], capture_output=True, text=True)
        out = (r.stdout or "").strip()
        print(f"[shot {ang}] {out if out else r.stderr.strip()[:300]}")

    # 2) geometry sanity check
    if do_geom:
        r = send({"type": "execute_code", "params": {"code": GEOM_CODE}})
        res = r.get("result", {})
        print(res.get("result", r))
    print("MCP_VERIFY_DONE")

if __name__ == "__main__":
    main()
