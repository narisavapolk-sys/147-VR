"""Send one command to the running Blender MCP addon (raw TCP JSON on 127.0.0.1:9876)."""
import socket
import json
import sys
import time
import os

HOST, PORT = "127.0.0.1", 9876

def send(cmd: dict, timeout: float = 180.0) -> dict:
    s = socket.create_connection((HOST, PORT), timeout=10)
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

if __name__ == "__main__":
    cmd_type = sys.argv[1]
    params = {}
    if len(sys.argv) > 2:
        if os.path.exists(sys.argv[2]):
            with open(sys.argv[2], "r", encoding="utf-8") as f:
                params = {"code": f.read()}
        else:
            params = json.loads(sys.argv[2])
    resp = send({"type": cmd_type, "params": params})
    print(json.dumps(resp, ensure_ascii=False)[:8000])
