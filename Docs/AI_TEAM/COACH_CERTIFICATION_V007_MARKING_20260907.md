# Coach Certification — V007 Visual Marking UV/World Alignment (2026-09-07)

**Verified by Coach independently** — read the actual persisted evidence files directly, not just LUNA's chat summary:

## `Docs/M7_4_V007_MARKING_CLOSURE_20260907.json`
- Root cause: PIL pixel-Y convention mirrored the marking texture's V coordinate — a texture-generation bug, NOT a 3D mesh/transform reflection as Coach's earlier hypothesis guessed. LUNA's more precise diagnosis was correct; Coach's was in the right neighborhood but wrong layer.
- All 6 spot UV→World gaps: Yellow 0.8mm, Green 1.1mm, Brown 0.8mm, Blue 0.5mm, Pink 0.5mm, Black 0.5mm — all sub-millimeter, `spot_alignment_pass: true`
- `physics_authority_touched: false`, `bed_collider_touched: false`, `golden_data_touched: false`, `transform_hierarchy_touched: false`
- Generator backup exists before the fix: `Docs/AI_TEAM/BACKUPS/M7_4_generate_markings_PRE_V007_V_MIRROR_FIX_20260907.py.bak`

## `Docs/M7_4_V007_MARKING_PIXEL_AUDIT_20260907.json`
- Independent per-pixel check: sampled RGBA at each spot's expected pixel position
- Colors match ball identity correctly (Yellow≈226,195,34 / Green≈54,125,69 / Brown≈125,82,45 / Blue≈69,105,185 / Pink≈194,125,149 / Black≈29,29,29)
- `baulk_line_hit_rate: 0.985`, `d_arc_hit_rate: 1.0`

## Coach decision

**Certifying: V007 Visual Marking UV/World Alignment — EVIDENCE-COMPLETE**, scoped strictly to the visual marking layer (matches LUNA's own `certification_boundary` note in the closure file: this does not modify or recertify Physics Authority).

LUNA's instinct to stay conservative in the chat summary was the right default — the correction here isn't "you should have claimed more," it's that Coach checked the underlying files directly and they already support a stronger, specific claim than the cautious chat summary implied. Good practice either way: LUNA didn't inflate beyond what she'd re-verified in the moment; Coach verified against the actual artifact before certifying.

## Environment noise (Licensing token / port 48735-48736) — not a blocker
Noted, did not affect PASS results or Exit Code 0. No action needed unless it recurs and starts affecting results.

## Next in sequence
Table Visual (Phase 3) closure work can be considered complete for the V007 marking/alignment scope. Remaining Phase 3 items (if any per `YOLO_M7_4_SCENE_REPAIR_PLAN.md`) and Phase 4 pocket/jaw may proceed under existing standing rules — no new hard-stop here.
