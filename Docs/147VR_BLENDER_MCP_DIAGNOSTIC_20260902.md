# 147 VR — Blender MCP Diagnostic

Date: 2026-09-02
Status: DIAGNOSED — GUI addon is installed/listening, direct socket execution does not respond

## Machine
- Device: wIn-NaRIs
- Blender: 5.2.0 LTS
- Blender PID during test: 9896
- Blender executable: C:\Program Files\Blender Foundation\Blender 5.2\blender.exe

## Add-on discovered
- Location: C:\Users\mongo\AppData\Roaming\Blender Foundation\Blender\5.2\extensions\user_default\mcp
- Main server: mcp_to_blender_server.py
- Manifest ID: mcp
- Add-on version: 1.0.0
- Minimum Blender version: 5.1.0
- Maintainer: Blender Lab
- Manifest permission: local TCP socket server for MCP client communication

## Confirmed protocol
- Server binds localhost:9876
- Requests are JSON terminated by a NUL byte (\\0)
- Request type must be exactly: execute
- Required request fields include: type, code, strict_json
- Responses are JSON terminated by a NUL byte

## Live tests
- TCP port 9876: LISTEN confirmed while Blender PID 9896 is running.
- HTTP probe: not applicable; socket is not HTTP.
- Direct TCP execute probe: connection succeeds but no response within 5 seconds.
- Result: SOCKET_TIMEOUT.
- Background Blender test also confirmed the add-on refuses background execution and reports that it requires a GUI/interactive session.

## Important interpretation
The MCP add-on is present and its socket is listening, but the interactive Blender main-loop polling/execution path is not responding to our direct Commander socket probe.
Do NOT assume that LISTEN on 9876 means MCP execution is healthy.
Do NOT fabricate a Blender MCP tool endpoint.

## Safety boundary
- No Unity Physics Authority changes were made.
- No source FBX was modified.
- No Blender source asset was overwritten.

## Next diagnostic direction
1. Inspect Blender GUI/add-on state and timer registration.
2. If GUI MCP remains non-responsive, use a separate controlled Blender GUI session or Blender-native script route for Phase 1.5.
3. Keep SnookerTable_Hi3D.fbx as immutable source/reference.
