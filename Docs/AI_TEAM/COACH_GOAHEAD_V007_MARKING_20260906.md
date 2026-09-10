# Coach Go-Ahead — V007 MARKING Prefab Promotion (2026-09-06)

**Verified by Coach independently:**
- `Docs/AI_TEAM/BACKUPS/147VR_MainScene_PRE_V007_MARKINGUV_20260906.bak` exists ✓
- `Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab` exists ✓ (pre-existing wrapper, correct architecture with 0.01 normalization + UV1)
- Roadmap correction confirmed applied (M5 line now reads NOT CERTIFIED, citing authority doc) ✓
- Session log confirms no Physics/Gameplay/Golden data touched ✓

**Assessment:** Good engineering judgment — catching the UV1/scale problem before certifying, and abandoning the first (scaled, wrong-architecture) approach in favor of the existing correct wrapper prefab, is exactly the kind of thing this process is supposed to produce.

**Go-ahead:** This promotion is reversible (backup exists). Per delegated authority, LUNA may proceed without further owner/Coach confirmation. Proceed with:

`V007 MARKING prefab → persist MainScene → verify UV1 + material → verify 3.569×1.778 bounds → verify ball centers ↔ markings → Play Mode validation → Phase 4 pocket/jaw`

**Reminder of the one rule that still applies:** if at any point persisting this change would require modifying Physics Authority (colliders, ball spawn data, Bed_Collider), stop that specific sub-step and report — everything else in this sequence may continue.
