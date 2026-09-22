# COACH — 008 staging unblock + a rail-axis defect found while verifying

Reviewer: COACH (independent review agent)
Base: `7ef6dd4` (`tools/008-props-generator-20260922`)
Method: recomputed from the committed manifest, prefab YAML, source, and the committed measurement.
**No Unity, no Blender, no renders.** Date: 2026-09-22

---

## 1. Verification of the props workstream — clean

| Check | Result |
|---|---|
| `COACH_008_PROPS_GENERATOR.patch` sha256 as transferred | matches `0da97f8d…857d7b` |
| Applied commit | `f1521967` — parent **`859a51a`**, author **`COACH <coach@147vr.local>`** (preserved by `git am`) |
| Script blob SHA | **`eeae92dd563ae67d6ae1c76398e8dfa4fc3a3786` — exact match** |
| Asset commit | `7ef6dd45` — manifest + 5 FBX, all 5 as LFS pointers |
| Manifest assertions | **12/12**, recomputed independently from the JSON |
| `opened_source_blend` / `modified_source_blend` | `None` / `False` — the 008 hard rule held |
| Prop sizes vs generator SPEC | all five match (see table below) |

**End-to-end provenance is now proven**: patch hash → commit parent → file blob SHA. This is the
strongest chain we have run, and it is the pattern to keep.

⚠️ **COACH correction:** the commit message on `f1521967` says "13 ASSERT_*". The artifact contains
**12**. COACH miscounted while writing the message (the triangle check was later split into two and
the name-collision check added). The manifest and the script are correct; only that sentence is
wrong. Nothing to re-run.

| Prop | Manifest size (m, Blender frame) | Generated FBX |
|---|---|---|
| CHALK | 0.035 × 0.035 × 0.022 | 17,196 B |
| REST | 1.58 × 0.135 × 0.0253 | 29,292 B |
| CUERACK | 0.42 × 0.3 × 0.93 | 35,148 B |
| TRIANGLE | 0.312067 × 0.270258 × 0.038 | 20,668 B |
| SCOREBOARD | 0.62 × 0.24 × 1.35 | 37,644 B |

### Evidence gap — two files not committed

`7ef6dd45` contains the manifest and the 5 FBX but **not** `147VR_Props_v001.blend` (the props
source) and **not** the generator's stdout `.txt` (the run log). Both should be committed: the
`.blend` is the only thing that makes the FBX traceable, and the `.txt` is the run evidence —
as a `.txt`, never a `.log` (F1).

---

## 2. 🔴 NEW FINDING D8 — the procedural rails are built on the wrong axes

This was found by combining the now-closed D1 axis mapping with `SnookerPhysicsSetup.BuildRails()`.
It is **pre-existing**, unrelated to the props work, and not caused by D1. D1 is what made it
visible.

### The code (`Assets/Scripts/Quest/SnookerPhysicsSetup.cs`, lines 174-197)

```csharp
float halfX = _tableBounds.size.x * 0.5f;   // = 0.889  -> the WIDTH axis
float halfZ = _tableBounds.size.z * 0.5f;   // = 1.7845 -> the LENGTH axis
float y = _surfaceTopY + railHeight * 0.5f - 0.02f;

// Long rails (along X): two segments each, leaving the middle-pocket gap at x = cx.
BuildRail(parent, "Rail N", new Vector3(cx - (halfX - pocketGapHalf) * 0.5f, y, cz + halfZ),
    new Vector3(halfX - pocketGapHalf, railHeight, railThickness));      // ...4 of these
// Short rails (along Z): single segment each, leaving corner gaps at both ends.
BuildRail(parent, "Rail W", new Vector3(cx - halfX, y, cz),
    new Vector3(railThickness, railHeight, halfZ - pocketGapHalf));      // ...and Rail E
```

`_tableBounds` comes from `Bed_Collider.bounds`, whose world size is **x 1.778, z 3.569** (verified
independently in the previous review). So:

- `halfX` = **0.889 = half the WIDTH**, but the code uses it as the length.
- `halfZ` = **1.7845 = half the LENGTH**, but the code uses it as the width.

The names are inverted relative to the table, and the geometry follows the names.

### What that produces, numerically

| Built rail | Position | Extent | Physical meaning |
|---|---|---|---|
| `Rail N` ×2 | `z = cz ± 1.7845` | `x ∈ [-0.719, +0.719]`, **gap 0.34 m at x = cx** | at the SHORT ENDS, with a **phantom 0.34 m opening at the centre of each end** |
| `Rail W` / `Rail E` | `x = cx ± 0.889` | `z ∈ [cz − 1.6145, cz + 1.6145]`, **continuous** | along the LONG SIDES, **with no middle-pocket opening** |

At each short end the cushion covers only 1.438 m of 1.778 m, and the missing 0.34 m sits in the
middle of the end. Along each long side the cushion is one unbroken 3.229 m box across `z = 0`.

### Where the middle pockets actually are — from the measurement

Permuting the measured anchors through the D1 mapping (Blender X → world Z, Blender Y → world X):

| Anchor | Blender world (x, y) | Unity world (x, z) | Meaning |
|---|---|---|---|
| `W` | (0.0005, −0.9303) | **x ≈ +0.930, z ≈ 0** | middle pocket, long side |
| `E` | (0.0005, +0.9321) | **x ≈ −0.932, z ≈ 0** | middle pocket, long side |
| `NW` | (−1.8078, +0.9164) | x ≈ −0.916, z ≈ +1.808 | corner |
| `NE` | (+1.8090, +0.9141) | x ≈ −0.914, z ≈ −1.809 | corner |
| `SW` | (−1.8078, −0.9117) | x ≈ +0.912, z ≈ +1.808 | corner |
| `SE` | (+1.8090, −0.9117) | x ≈ +0.912, z ≈ −1.809 | corner |

The two middle pockets sit on the **long sides** (`x ≈ ±0.93`) at the **middle of the length**
(`z ≈ 0`). The long-side rail box is centred at `x = ±0.889` and spans `z ∈ [−1.6145, +1.6145]` —
**it covers exactly that spot.**

### Impact

- **Middle pockets are physically blocked by a rail.** A ball rolled along a long cushion toward
  the middle pocket hits a box and bounces back instead of dropping.
- **Both short ends have a 0.34 m phantom opening** where a cushion should be, so a ball can leave
  the playfield through the middle of an end.
- This is a snooker/pool game. Middle pockets are a core rules surface, not a detail.

`TODO.md` still lists "Validate Cushion response against Golden cases" as open. COACH's assessment
is that this is why: the cushion layout has never been validated against the pocket anchors.

### Recommended correction (owner-gated — it is a physics change)

1. Rename to `halfAlongWidth` / `halfAlongLength` so the intent cannot invert again.
2. Put the **middle-pocket gap on the length axis**: the two long-side rails split at `z = cz`.
3. Make the **short-end rails single segments** across the width, with `pocketGapHalf` reserved for
   the corner gaps.
4. Add a **Golden regression case**: fire a ball along a long cushion at the middle-pocket anchor
   and assert it is caught. Without a test this can invert again silently.
5. This changes physics ⇒ it needs re-certification and a REAL10 re-run, exactly like D3. Do it in
   the same governed batch as the v009/008 staging, not on its own.

**COACH has not changed any code. This is a finding for the owner.**

---

## 3. Unity staging — the blocker and the unblock

### Likely cause

The project has exactly **one** asmdef (`Assets/Tests/PlayMode/147VR.PlayModeTests.asmdef`). All
**129** Editor-only scripts live under `Assets/Editor/` (89) or `Assets/Editor/AAA/` (40) — e.g.
`M5FinalReal10MainSceneRunner.cs`, `M7_4_CreateVisualV006Prefab.cs`, `TableVisualAudit.cs`.

Anything under a folder named `Editor` compiles into `Assembly-CSharp-Editor` and is reachable by
`-executeMethod`. A helper placed anywhere else lands in `Assembly-CSharp` (or fails to compile if
it references `UnityEditor`), which matches the reported symptom precisely: no compile error, but
`Stage008Props` never appears in `Assembly-CSharp-Editor.dll`.

**COACH recommends not spending more time on batch invocation.** Five static props do not need an
automated pipeline, and their placement needs eyes on it anyway. A menu item does the same work in
one click, in the Editor, with visual review.

### Provided: `Assets/Editor/Stage008Props.cs`

- `147VR / 008 / 1. Configure Prop FBX Import Settings` — pins scale 1, no file scale, no axis
  bake, no cameras/lights/animation on the five prop FBX.
- `147VR / 008 / 2. Stage Table Props Into Current Scene` — idempotent; replaces the prop root's
  children rather than duplicating.
- `-executeMethod VR147.EditorTools.Stage008Props.StageMainSceneBatch` — same thing headless, for
  whoever still wants it.

Rules enforced in code:

1. Locates the table via `SnookerPhysicsSetup.tableRoot` and reads `Bed_Collider.bounds` in world
   space for `surfaceTopY`, half-width, half-length and the rail top — no hard-coded offsets from
   an assumed origin.
2. Creates `147VR_PROPS_ROOT` as a **sibling** and refuses to continue if it and the table end up
   in the same hierarchy. Props are never parented under the table transform.
3. Strips any Rigidbody from prop instances — static scenery adds no simulation cost.
4. Errors on any `Bed_Collider` / `TABLE SURFACE` name collision inside a prop.
5. **Measures every instantiated prop's world-space Renderer bounds against the manifest sizes,
   permuted into Unity axes** (Blender (X,Y,Z) → Unity (x=Y, y=Z, z=X)). That single check catches
   an import-scale error *and* an axis-convention error, and it is logged line by line.

The measured-vs-expected table is the point: if the FBX arrive with the wrong scale or the wrong
axis convention, staging reports `SIZE MISMATCH` instead of quietly placing wrong-sized props.

### Placement used by the tool (table-relative; X width / Z length / Y up)

| Prop | Position | Note |
|---|---|---|
| `CHALK` | `x = cx + halfX`, `y = railTopY`, `z = cz − 0.35·halfZ` | on the +X long rail top, **not on the cloth** |
| `REST` | `x = cx − (halfX + 0.30)`, `y = FloorY`, `z = cz` | on the floor, long axis already along Z |
| `CUERACK` | `x = cx`, `y = FloorY`, `z = cz + halfZ + 0.45` | past the +Z end, yaw 180° to face the table |
| `TRIANGLE` | `x = cx + (halfX + 0.30)`, `y = FloorY`, `z = cz + 0.60` | on the floor beside the +X long side |
| `SCOREBOARD` | `x = cx`, `y = FloorY`, `z = cz − (halfZ + 0.60)` | beyond the −Z head end |

`FloorY = 0` is an **explicit assumption**, logged on every run. If the scene floor is not at Y = 0,
the floor-standing props need a nudge — visual review catches it immediately.

---

## 4. Gate statement

| Gate | State |
|---|---|
| Props patch provenance | 🟢 **PASS — end-to-end proven** |
| Props generation + manifest | 🟢 **12/12 PASS** |
| FBX assets | 🟢 committed + LFS pushed |
| Unity staging | 🟢 **unblocked** — menu item provided, batch optional |
| **D8 rail axis** | 🔴 **NEW — owner decision, physics change, needs re-certification** |
| 008 source `.blend` / props `.blend` / run `.txt` | 🟡 two files still uncommitted |
| Main Scene / M5 core / V007 / Golden / REAL10 | 🔒 unchanged |

No file other than this document and `Assets/Editor/Stage008Props.cs` was added. No scene, no
`.blend`, no existing source file was modified. No Unity, Blender, or hardware execution performed.
