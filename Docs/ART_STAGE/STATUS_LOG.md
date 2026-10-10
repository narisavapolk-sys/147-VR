# 147 VR — CURRENT STATUS / HANDOFF (2026-10-11) — authoritative

> For any AI/human resuming. Read THIS first; older docs may be stale. Honest state, not aspirational.

## Git
- Branch `integration/008-m53`. Commit **`377eb543`** "feat(art): integrate v008_OPT table + arena HDRI skybox + crowd billboards + SFX" (129 files) is **LOCAL, NOT PUSHED** (PAT is read-only; owner pushes).
- Still untracked (not in commit): folder `.meta`s, `Assets/AAA/TEST_ART_AAA/` (v008 source), scenes `147VR_MainScene_ART_V006`, `..._V008_CLEAN_TEST`, `..._PRE_V008` (backup), `Assets/Editor/LunaV008CleanSwapTest.cs` (not authored here).
- LFS covers fbx/png/hdr/wav/blend. Scratch `Docs/ART_STAGE/{blend_archive,export,bake}/` gitignored.

## DONE + VERIFIED
- UPM/toolchain blocker fixed (connects 0.7s on Unity **6000.4.4f1**, the only editor installed). Canonical launcher `Docs/Tools/Unity_Batch_Safe.ps1`.
- Table **v008_OPT** game FBX (243k tris) + LOD1(95k)/LOD2(45k) + URP materials (from extracted params) + FELT (`Green_Felt_Texture_V007.png`) + AO — staged under `Assets/AAA/ImportedSnooker/V008_OPT/`.
- WPBSA verified vs v007: baulk 737mm / D 292mm / Brown-centre correct; **pockets(6)+cushions(4) identical** → swap is physics-compatible. `TABLE FRAME` confirmed present (dark material). Evidence: `Docs/ART_STAGE/*.json`.
- 13 SFX clips in `Assets/Audio/SFX`.
- Editor tools (MenuItem, no auto-run): `147VR/Art/{Setup V008 Table, Cleanup Strays+Place Venue, Strip Venue, Strip Props+Placeholder Cube, Apply Arena Skybox, Clear Skybox, Promote V008_TEST->MainScene}` + `147VR/Audio/Configure SFX Import`.

## DONE but NOT YET VERIFIED (must check)
- **Skybox**: `arena_panorama.png` (no-table) + Apply menu exist, but owner saw GRAY — likely Scene-view not rendering skybox, or Apply not run. **Unconfirmed working.**
- **Table placement/height** in `147VR_MainScene_V008_TEST` / promoted MainScene: not screenshot-verified against physics plane 0.756 (felt may float ~7cm).
- **SFX import settings** (Vorbis/Streaming): menu written; not confirmed executed.

## NOT DONE
- AudioMixer asset + wiring SFX to gameplay events (cue strike / ball-ball / cushion / pocket / crowd / frame-win).
- LODGroup assembly in Unity (LOD meshes exist but not wired).
- Material **bake** for full AAA look (materials are flat colors; DARC lighting approximated by 3 point + 1 dir lights, not baked).
- **Re-run Golden regression** after the swap (confirm physics unchanged) — REQUIRED before trusting gameplay.
- Quest perf / device gate (no device evidence).

## GOTCHAS (do not repeat)
- 16GB RAM: NEVER run Unity + Blender together; NEVER leave multiple Unity instances (found 3 earlier = freeze cause).
- Do NOT delete `Library/PackageCache` (no global cache on this PC → offline-unrecoverable).
- Batch `-executeMethod` unreliable (ShaderGraph editor-asm compile aborts batch) → use the **GUI editor**.
- A compile error in an `Assets/Editor` script forces Safe Mode (avoid tuple-dict initializers; use `AudioImporter.SaveAndReimport()`; `UnpackPrefabInstance` before destroying prefab children).
- Keep raw `.blend` OUT of `Assets/` (Unity tries to convert → error).
- GitHub PAT is read-only → agent cannot push; owner pushes.

## Suggested next order
1. Verify/fix skybox display (Game view / Scene-view skybox; Lighting>Skybox Material = ArenaSkybox).
2. Screenshot table placement; align felt to 0.756; dedup extra DARC lights.
3. Run `Configure SFX Import`; create AudioMixer; wire SFX to events.
4. Bake materials (Cycles) for AAA look.
5. Assemble LODGroups; then re-run Golden regression + perf.
