# 147VR M3 Cushion Authority — YOLO Plan

## Approved Direction — 2026-08-29
- M3 is Cushion Physics Authority certification, not a physics lookup table.
- Golden cases are test oracles / regression evidence only.
- PHY-001 through PHY-006 remain locked baselines and must not be modified.
- Ball-to-ball impact-angle coverage is isolated from M3.

## Certification Cases
- M3.1 / PHY-007: 0° incident angle, no spin, single cushion, REAL x5.
- M3.2 / PHY-008: 30° incident angle, no spin, single cushion, REAL x5.
- M3.3 / PHY-009: 45° incident angle, no spin, single cushion, REAL x5.
- M3.4 / PHY-010: 60° incident angle, no spin, single cushion, REAL x5.
- M3.5 / PHY-011: 90° incident angle, no spin, single cushion, REAL x5.
- M3.6 / PHY-012: 30° incident angle with English, single cushion, REAL x5.
- M3.7 / PHY-013: 45° incident angle, no spin, double cushion, REAL x5.

## Validation
- Require real Cushion contact evidence before accepting a sample.
- REAL -> MEASURE -> GOLDEN -> REGRESS -> CERTIFY.
- Add Left/Right English symmetry validation as a diagnostic invariant.
- Validate mirrored angle magnitude, speed and spin magnitude where applicable.

## Coverage Sweep (post-certification)
- 5° increments from 0° through 85° are diagnostic coverage data, not automatic Goldens.
- Add speed and spin variants selectively; avoid Cartesian explosion.
- Use sweep data to detect discontinuities and parameter/model anomalies.

## Safety / Integrity
- No fake hits, JSON, Golden values, or PASS states.
- Do not delete or rebuild Library unless independently justified by evidence.
- Do not run competing Unity instances against the same project.
- Source projects outside 147 VR are read-only / untouched.
- Continue automatically in YOLO mode until a real blocker requires intervention.
