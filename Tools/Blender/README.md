# Tools/Blender

Offline Blender tooling for 147 VR art assets. **Nothing in this folder is part
of the shipping game.** These are authoring/QA scripts only.

## Contents

| File | Purpose | bytes | git blob sha |
| --- | --- | --- | --- |
| `patch_v3_aristocrat_legs.py` | Replaces the thin legs of the Aristocrat table with 8 turned + fluted gold legs, adds gold skirt and cream sight band | 11188 | `164600e5c10b7dc87f5ef7c14c09e81d05b728dc` |
| `render_v3_shots.py` | Headless render driver: runs v2 + v3.1, sets lights/cameras, writes 3 QA PNGs and `render_report.txt` | 7080 | `037a5a97fc0ce915f2ef9ae2e04e999129ee7e31` |

Still to be added (they live on the author's machine, not in this repo yet):

- `147VR_AAA_Aristocrat_SnookerTable.py` (v2 base table builder)
- `147VR_Arena_Crucible.py` (Crucible-style arena + spectators)

## Blender version

Target: **Blender 5.2** (also runs on 4.2+).

`mesh.use_auto_smooth` was removed in Blender 4.1. Any script here must not
touch it. The v3.0 build of the leg patch (11,185 bytes) crashed for this
reason and must not be used; `11188` is the only valid size.

## How to run

```powershell
$env:V2_PATH = "<path to 147VR_AAA_Aristocrat_SnookerTable.py>"
$env:V3_PATH = "<path to patch_v3_aristocrat_legs.py>"
$env:OUT_DIR = "C:\Temp\147VR_TABLE_V3"

& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" `
    --background --factory-startup --python "<path to render_v3_shots.py>"
```

Always render headless. Viewport screenshots are not acceptable QA evidence:
Solid shading hides all materials and made the gold table look grey once
already.

## QA gate

`render_report.txt` must contain:

```
=== ALL DONE ===
legs=8
skirt present  = True
sightband      = True
faces=<number>
engine = <engine name>
```

`legs=0` means the patch did not run. Stop and report; do not hand-edit the
script.

## Safety rules

1. Verify `bytes` + SHA-256 of the local file before executing. Never
   hand-type or hand-patch these scripts.
2. These scripts touch **Blender only**. They must never be pointed at Unity
   physics authority: `Bed_Collider`, `TBL_COLLIDER_*`, Golden data, physics
   spawn points or marking authority.
3. The Aristocrat table is a **visual candidate**, not a replacement. The
   certified in-game snooker table is
   `Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v007.fbx`, and
   `PoolTable_8Ball` / `PoolTable_9Ball` keep their own tables. No FBX from
   this folder may be imported into Unity without the owner's approval.
4. Geometry constants in the patch (`PLAY_L`, `PLAY_W`, `RAIL_W`,
   `RAIL_TOP_Z`, `CABINET_BOTTOM_Z`) must stay in sync with the v2 builder.
   Changing them silently breaks WPBSA dimensions.
