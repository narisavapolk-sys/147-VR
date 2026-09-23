# COACH WORK ORDER #008 — Blocked-window package: hash taxonomy, the one scope answer, and A1/A2 made mechanical

**Issued by:** Coach seat · **Date:** 2026-09-23
**Executor:** Luna · **Decision owner:** the project owner
**Base of record:** `integration/008-m53` @ `d209bb4535a33d4f00cff22c9da47689649ac31a`
**Predecessors:** WO#007 (next actions + convergence) · WO#006 (S2a disposition)

---

## 1. THE HASH CONFUSION — CLEARED, AND THE RECORD HARDENED

You were right to refuse to resolve `c974ad4f` as a commit. It is not one, and three different kinds of hash are in circulation. **They are never interchangeable:**

| Kind | Example today | What it identifies | Where it is used |
|---|---|---|---|
| **Content sha256** (canonical bytes, LF) | `c974ad4f…` (WO#007 doc) · `e4c4810d…` (D1 artifact) · `b465d8ec…` (S2a artifact) | the **bytes of a file** | `COACH_ARTIFACT_INDEX_20260923.md` rows, artifact headers, F9 evidence |
| **Git blob SHA-1** | `d0158146dc87…` (the index) · `5db58e7ae3bc…` (`M5_3_RulesUnitTests.cs`) · `c60138bc9ef1…` (`147VR_AI_WORK_PROTOCOL.md`) | a **blob object** in the object database | patch `index` lines, `git ls-tree`, base-blob preconditions |
| **Commit SHA-1** | `d209bb4535a33d4f00cff22c9da47689649ac31a` | a **commit** | `git rev-parse HEAD`, `WHICH_COMMIT`, push/apply anchors |

**The anchor for applying Pack v7 was always `HEAD = d209bb45…`** (plus the base blob `d0158146dc87…` for the index). `c974ad4f` appearing near it was the *artifact's own content hash* — a verification value for the file, never an apply anchor.

**Hardening applied in this pack:** the index header now carries an explicit legend stating that the column is **content sha256 of canonical bytes, not a git object id**, and pointing at where git ids live. That removes the ambiguity for every future reader.

---

## 2. A4 — THE ONE ANSWER THE OWNER OWES (Coach's recommendation)

**Question:** is multi-frame match progression in RC scope?

**Coach recommends: OUT OF SCOPE for RC. Record I19/I20 as `OUT OF SCOPE / DEFERRED`, tied to the post-RC multiplayer milestone.**

Reasoning, in order of weight:

1. **It is not needed for the game's own promise.** The product is called **147 VR**; 147 is the maximum break *in one frame*. The fantasy the game sells is the perfect frame, and GATE 5's acceptance is defined as completing **one full frame** end to end. Multi-frame progression adds nothing to that promise.
2. **No authority exists, and authority is the expensive part.** There is no match-state owner anywhere in the Quest/AAA spine. Creating one is not a patch: it is a **new authority adjacent to the frozen M5.3 rules core**, which under contract §6 requires its own design (one owner, no competing state machine, no hidden fallback) and an explicit authorisation **before** any implementation.
3. **It would block the critical path.** RC is gated on rules validation, 008 integration, and the device gate. A new state authority touching the rules boundary puts all three at risk for a feature the RC definition does not require.
4. **It belongs to the multiplayer milestone, where it becomes meaningful.** Frames matter when two players contest a match. Deferring it there keeps one coherent milestone instead of two half-built structures.

**If the owner decides otherwise** (in scope): the correct next artefact is a **design brief for a single match-state authority**, not code — blast radius against M5.3 stated up front, no implementation until it is authorised.

**Wording to record either way**, in `Docs/147VR_CURRENT_STATE.md`:
`I19/I20 = OUT OF SCOPE / DEFERRED (owner decision 2026-09-23); revisit at the post-RC multiplayer milestone.` — so it stops being re-read as debt every session.

---

## 3. EXECUTION LAYER BLOCKED — CLASSIFICATION AND WHAT IS UNAFFECTED

The remote-desktop/terminal channel that runs `git` and Unity is blocked by host security policy, including read-only commands. Per contract §10 this is **BLOCKED, not FAIL**, and it is an **infrastructure** condition:

- **Nothing is at risk.** Pack v7's bytes are fixed by its sha256, and the apply anchor is `d209bb4535a33d4f00cff22c9da47689649ac31a`. A delay changes no evidence and invalidates no gate.
- **A0–A6 are all blocked**, because every one of them needs either git or Unity on that host. There is no partial path.
- **Stopping before A1 was correct.** The alternative — editing files locally with no ability to commit — would have produced unverifiable change, which is exactly what this project forbids.

**Options to restore the channel** (owner's call, no preference from Coach beyond cost):
1. Owner runs the three commands by hand from a normal session (see §5) — cheapest, and it exercises the same provenance path;
2. have the host policy re-enable the executor channel for the project path only;
3. use a different shell already permitted on the host.

While blocked, the only useful work is **decision-only** work: §2 (scope), the RC scope approval, and GATE 5 acceptance. That is why this pack also makes A1/A2 mechanical — when the channel returns, the work is `git am` plus one `git rm`.

---

## 4. A1 / A2 MADE MECHANICAL — REVIEWED IN THIS PACK

Two of the four A1/A2 items are now **prepared and reviewed by Coach**, so the executor no longer has to author them (and cannot get them subtly wrong):

**4.1 A2 — the two `.ToString()` assertions.** Included as its own commit touching only `Assets/Editor/M5_3_RulesUnitTests.cs`, lines 161 and 408. Base blob verified: `5db58e7ae3bca5128fc0196b52744bba555f7aed`.

The fix is proven safe by the type, not by opinion:

```
Assets/Scripts/Quest/Rules/M5RulesTypes.cs:238
        public ReadOnlyCollection<M5FoulReason> FoulReasons { get; }
```

`FoulReasons` is a **strongly-typed `ReadOnlyCollection<M5FoulReason>`**, so `Does.Contain(…ToString())` compares a `string` against a collection of enums and can **never** match — a guaranteed failure independent of any game logic. Every sibling assertion (lines 64 / 77 / 88) already compares the enum member, which is why they pass. The engine side is correct (`M5SnookerRulesEngine.cs:163`).

**⇒ This is an assertion-form fix. No expectation changes.** The commit message says so in those words. If the owner would rather author it by hand, drop that commit.

**4.2 A1.1 — the artifact amendment rule.** Included as its own commit appending one section to `Docs/147VR_AI_WORK_PROTOCOL.md`. Base blob verified: `c60138bc9ef14123546b29554424e0bb9ee0134d`.

**4.3 Still yours to type** (one command each, no patch needed):

```powershell
git rm "Assets/Scripts/Quest/M5ShotLifecycle.cs.cleanupbak"   # A1.2 — tracked debris in the frozen M5 folder
```

**4.4 After applying:** re-run **only** R11 and R36 (A2 re-run), then continue A3 → A4 → S3/S5. Do not bundle the re-run with anything else.

---

## 5. A0–A6 AS COPY-PASTE COMMANDS (for when the channel returns)

```powershell
# --- A0: land pack v7 (and v8 if it is already published) ---
cd "C:\Users\mongo\UnityProjects\147 VR"
git rev-parse HEAD                       # must be d209bb4535a33d4f00cff22c9da47689649ac31a
$o = "$env:TEMP\coach-v7.patch"
Invoke-WebRequest "https://dpaste.com/8RN25W4DB.txt" -OutFile $o
(Get-FileHash $o -Algorithm SHA256).Hash.ToLower()
#   must equal 8597ac8501006cedfaf70f4d8e203fa80d935805319f30031c5b2b6c2e24d91f
git am $o
git log --oneline -3 ; git status --short
git push origin integration/008-m53

# --- A1: debris + integrity ---
git rm "Assets/Scripts/Quest/M5ShotLifecycle.cs.cleanupbak"
git commit -m "chore(assets): remove tracked M5ShotLifecycle.cs.cleanupbak debris from the frozen M5 folder"
git status --short

# --- A2 re-run: ONLY R11 and R36, as their own evidence artifact ---
# (no expectation changed; the register stays frozen)

# --- A3: reconcile I07 -> locate the assertion that expects null, by file and line ---
# --- A4: record the I19/I20 scope decision from section 2 ---
# --- A5: S3 (M4.2 fresh current-profile re-capture) then S5 (REAL10 / M5 re-run) ---
```

---

## 6. PROHIBITIONS AND STOP CONDITIONS

Unchanged: certified core (V007 / M5.1 / M5.2 / **M5.3 frozen rules** / D8 / W1) · `147VR_M53_VALIDATE` · the real project beyond what is authorised · system environment · firewall · the tracked launcher · any second authority.

**STOP** on: an expectation edited to match observed output · a register edited after results · a fix bundled with unrelated work · a case left classified without a located defect · implementation started before authorisation · evidence that cannot be tied to a commit and an editor build.

## 7. WHAT COACH DOES NOT DO

Coach does not authorise the RC scope, the multi-frame decision, or the GATE 5 definition, does not implement the match-state authority, and does not sign off the shipping tree. It reviews, challenges, specifies acceptance and — as here — removes ambiguity from the record.
