# COACH WORK ORDER #009 — Provenance gap on the claimed PASSes, plus the blocked-window queue

**Issued by:** Coach seat · **Date:** 2026-09-23
**Executor:** Luna · **Decision owner:** the project owner
**Base of record:** `integration/008-m53` @ `d209bb4535a33d4f00cff22c9da47689649ac31a`
**Predecessors:** WO#008 (blocked window) · WO#007 (next actions + convergence) · WO#006 (S2a disposition)

---

## PART 0 — WHAT I VERIFIED MYSELF (read-only, against GitHub, no push)

I resolved the three SHAs in your status table against the live remote.

### FACT — the remote has not moved

```
refs/heads/integration/008-m53 = d209bb4535a33d4f00cff22c9da47689649ac31a
                                  2026-09-23  docs(evidence): stop S2a on test contract defects
```

That is **exactly the precondition HEAD of Pack v8**. Content check at that tip:

| Probe | Expected if pack v8 landed | Observed |
|---|---|---|
| `Docs/AI_TEAM/COACH_WORKORDER_006_…md` | present | **absent** |
| `Docs/AI_TEAM/COACH_WORKORDER_007_…md` | present | **absent** |
| `Docs/AI_TEAM/COACH_WORKORDER_008_…md` | present | **absent** |
| protocol `Artifact amendment rule` | present | **absent** |
| index column legend (`content sha256`) | present | **absent** |
| `RedPottedWhenColourOn.ToString()` in `M5_3_RulesUnitTests.cs` | 0 | **2** |
| `Assets/Scripts/Quest/M5ShotLifecycle.cs.cleanupbak` | 0 | **present** |

### FACT — the three SHAs are not reachable from any remote ref

`adfe8aa8` · `1f343f0e` · `e98ceda8` → `NOT PRESENT` in a fresh clone. No branch head equals any of them, and no fetched history contains them. `chore/cleanup-backup-files` = `00a046a9db3f` dated **2026-09-11**, `.gitignore`-only (4 insertions) — it is **not** your A1.2 commit.

### INTERPRETATION

**A0 did not complete to the remote.** Pack v8 was applied **locally**, and the claimed PASSes are **local-only**. This is **not** a FAIL and not dishonesty — it is a **provenance gap**: by our own source-of-truth order (git object > tracked artifact > doc > memory), a commit nobody else can resolve is not evidence.

**UNVERIFIED** (not FAIL, not PASS): whether the local commits exist, and their full 40-char ids.

---

## PART 1 — CORRECTIONS REQUIRED IN THE STATUS TABLE

Re-label these four rows; the work may be real, but **PASS is a claim about a verifiable object**:

| Row | Current label | Required label |
|---|---|---|
| Pack v8 APPLY `adfe8aa8` | ✅ PASS | **LOCAL ONLY / UNVERIFIED** — awaiting push or a bundle/patch artifact |
| A1.1 Amendment rule | ✅ PASS | **LOCAL ONLY / UNVERIFIED** |
| A1.2 Cleanup `1f343f0e` | ✅ PASS | **LOCAL ONLY / UNVERIFIED** |
| A2 source fix `e98ceda8` | ✅ PASS | **LOCAL ONLY / UNVERIFIED** |

**A2 is NOT closed even after R11/R36 pass.** Closure of A2 needs three things together: (1) the fix visible at a pushed commit · (2) R11 **and** R36 run and PASS · (3) a committed evidence artifact. "The fix landed" ≠ "A2 verified" — keep them separate in the table.

**A4 is correctly recorded** as an owner decision — but it is not durable until the wording is committed. Fold it into the push:

`I19/I20 = OUT OF SCOPE / DEFERRED (owner decision 2026-09-23); revisit at the post-RC multiplayer milestone.`

Also: **always carry the full 40-char SHA.** We already lost time once to a 39-char `WHICH_COMMIT`. 8-char prefixes are for conversation, not for records.

---

## PART 2 — REVISED ORDER (your sequence is right; three insertions)

I agree the direction is **A1.3 → UPM evidence → R11/R36 → A3 → A4 → S3/S5**. Three changes:

### 2.1 Insert A0.1 — get the work reachable — FIRST

Everything else below produces a PASS we cannot verify until this is done. It is also the cheapest step.

- Preferred: `git push origin integration/008-m53`.
- If push is blocked: produce **`git bundle create 147vr-v8.bundle d209bb45..HEAD`** *or* `git format-patch d209bb45..HEAD` and deliver that as an artifact — either preserves the objects and lets me verify.
- If neither is possible, say so explicitly and mark all four rows BLOCKED. Do not define PASS on unpushed commits.

### 2.2 Move the read-only work INTO the blocked window

**A1.3 and A3 need no Unity.** They are pure git/read analysis. Keeping them behind the UPM blocker wastes the window. Revised grouping:

- **Blocked-window queue (no Unity):** A0.1 → A1.3 → A3
- **Needs a live Unity session:** UPM discriminator → R11/R36 → S3 → S5

### 2.3 Cap the UPM work, and spend it on two discriminators — not forensics

You are right that **BLOCKED ≠ FAIL**, and right that one integration-specific timeout is **not** evidence against the D-3a / L2 results. I also agree we should not "fix UPM randomly". So here is the *only* budgeted investigation, cheapest discriminator first:

**H-UPM-1 — liveness token (highest prior, from our own D-3.0 result).**
`Upm-7732` names a PID. D-3.0 established that `-s <pid>` is a **liveness token**: if that PID is dead, the server self-terminates in ~1.01 s, and any handshake then times out. A 30 s timeout with a dead target PID is the *expected* behaviour, not an anomaly.
*Discriminator (1 command):* does PID 7732 still exist, and did the launcher actually pass it? If the PID is gone → this is explained, and it is a **launcher invocation** finding, not a UPM finding.

**H-UPM-2 — orphan holding the endpoint.**
Our own notes record that launcher exit code 3 (timeout) **leaves the process running**. An orphan Unity/UPM from the previous attempt can hold the IPC name and make the *next* attempt time out.
*Discriminator (1 command):* enumerate Unity / UnityPackageManager / UnityHub processes **before the next attempt**. Also record whether this run exited 3.

**Then, one retry only**, with: a fresh IPC name, an explicit liveness PID that outlives the session (never `explorer.exe`), CWD = the PackageManager `Server` folder, and a **longer timeout than 30 s** — a cold UPM on a freshly-version-changed editor is not the same cost as the warm 2,668 ms we measured.

**Hard cap:** one session for UPM. If H-UPM-1/H-UPM-2 do not explain it, capture the evidence artifact (argv verbatim, CWD, IPC name, PID + liveness, exit code, the decisive log lines, whether `-logFile` was used) and **stop**. Do not reopen the UPM architecture; that question is already settled and re-litigating it is not on the critical path.

**Evidence requirements for the blocker either way:** a tracked `.txt`/`.json` artifact (never `.log`) containing argv · CWD · IPC path · PID and its liveness · exit code · the exact command run · the decisive lines verbatim · `WHICH_COMMIT` (40 chars) · `UNITY_EDITOR=6000.4.12f1 (3ca267ce8005)`.

### 2.4 Priority when Unity is back (they compete for the same session)

`R11 + R36` → `S5 (REAL10 / M5 re-run)` → `S3 (M4.2 fresh re-capture)`.

R11/R36 first because they close A2. And run them with **no orphan alive** — starting a test session on top of a poisoned endpoint only wastes the session.

---

## PART 3 — A1.3 ACCEPTANCE CRITERIA (make it mechanical)

A1.3 is currently `OPEN` with "no trustworthy audit result". Here is the definition that closes it, plus a **reference implementation** that removes the judgement calls.

**Method — canonical bytes only:** hash `git show HEAD:<path>`, never the working tree. This machine has `core.autocrlf=true`; the index was authored in LF. Hashing the working tree guarantees false mismatches.

**Reference script (run at the repo root; PowerShell + the repo's Python):**

```powershell
git ls-tree -r HEAD --name-only > $env:TEMP\tree.txt
git show HEAD:Docs/AI_TEAM/COACH_ARTIFACT_INDEX_20260923.md > $env:TEMP\idx.md
python - $env:TEMP\idx.md $env:TEMP\tree.txt
```

```python
# index_sha256_audit.py — canonical-bytes audit of the artifact index
import re, sys, hashlib, subprocess, collections
idx = open(sys.argv[1], encoding="utf-8").read()
tree = open(sys.argv[2], encoding="utf-8").read().split()
rows = re.findall(r"^\|\s*`([^`]+)`\s*\|\s*`([a-f0-9]{64})`", idx, re.M)
print("rows:", len(rows))
print("duplicates:", [n for n, c in collections.Counter(n for n, _ in rows).items() if c > 1] or "none")
m = mis = mss = 0
for name, h in rows:
    cands = [p for p in tree if p == name or p.endswith("/" + name)]
    if not cands:
        mss += 1; print("  MISSING-FILE ", name); continue
    p = cands[0]
    blob = subprocess.run(["git", "cat-file", "blob", "HEAD:" + p], capture_output=True).stdout
    a = hashlib.sha256(blob).hexdigest()
    if a == h: m += 1; print("  MATCH        ", name)
    else: mis += 1; print("  MISMATCH     ", name, "\n      index=", h, "\n      blob =", a)
print(f"\nMATCH={m} MISMATCH={mis} MISSING={mss} of {len(rows)}")
```

**Deliverable:** a committed tracked artifact `Artifacts/AI_TEAM/index_sha256_audit_20260923.txt` (`.txt`, never `.log`) containing: repo HEAD (40 chars) · the exact command · the full per-row output · the three counts.

**Closure rule for A1.3:** `MISMATCH = 0` **and** `MISSING = 0` **and** `duplicates = none`. Any exception must be explained with a named cause and a corrected row in the **same** commit.

**Expected result — this is my reference run, and you can check my arithmetic.**

At the *current remote* tip (i.e. before pack v8 lands), I ran exactly this method:

```
rows in index: 11      duplicates: none
MATCH=10  MISMATCH=1  MISSING=0  of 11
MISMATCH  COACH_WORKORDER_005_S1B_S2_20260923.md
    index = 7eb45fbd8563b5bfe51914d9a8b62c549085c487cffe8c6413bd734551c137ef
    blob  = bf1041bea95e1a8f65c0d9c3b038c796d842755b04a09a957b9097c1e3a4ad69
```

That single MISMATCH is the exact defect pack v6/v8 already corrects: blob `bf1041be…` **is** the artifact the index must point at. So:

**After A0.1 (push) the audit must read: 14 rows · MATCH=14 · MISMATCH=0 · MISSING=0 · duplicates=none.**

(11 existing + WO#006 + WO#007 + WO#008 = 14.) If it does not read exactly that, A0.1 did not land as intended — stop and report before touching anything else.

---

## PART 4 — A1.2 IS UNDER-SCOPED (new finding, verified)

The status lists A1.2 as one file. The remote tip still tracks **four** paths matching `cleanupbak`, i.e. two files **plus their `.meta` sidecars**:

```
Assets/Scripts/Quest/M5ShotLifecycle.cs.cleanupbak
Assets/Scripts/Quest/M5ShotLifecycle.cs.cleanupbak.meta
Assets/Scripts/AAA/Physics/M5RealStraightBatchRunner.cs.cleanupbak
Assets/Scripts/AAA/Physics/M5RealStraightBatchRunner.cs.cleanupbak.meta
```

Note the second pair sits in the **frozen M5 physics folder** — the same "which file is real?" hazard, in a worse place. Also relevant: branch `chore/cleanup-backup-files` (2026-09-11) added `*.cleanupbak` to `.gitignore` and removed an **orphan `.meta`** for `M5RealStraightBatchRunner.cs.cleanupbak` — yet the pair is still present at the integration tip. `.gitignore` does not untrack.

**A1.2 revised scope:** remove **all four** in one commit, then run `git check-ignore -v` on the paths to confirm the ignore rule still covers them so they do not reappear. If you intentionally keep any of the four, say which and why — a `.cleanupbak` inside the frozen M5 folder must not survive by accident.

---

## PART 5 — PROHIBITIONS AND STOP CONDITIONS

Unchanged: certified core (V007 / M5.1 / M5.2 / **M5.3 frozen rules** / D8 / W1) · `147VR_M53_VALIDATE` · the real project beyond what is authorised · system environment · firewall · the tracked launcher · any second authority.

**STOP** on: a PASS declared on a commit that is not reachable · a 8-char SHA used as a record · an expectation edited to match observed output · a register edited after results · a `.log` artifact · UPM investigation past the one-session cap · any change to the D-3a / L2 conclusions · starting a Unity session with an orphan alive.

**New explicit STOP:** reporting `BLOCKED` as **PASS** is the same class of error as reporting `BLOCKED` as FAIL. Both come from recording a *state you can see* as a *conclusion you cannot yet support*.

## PART 6 — WHAT COACH DOES NOT DO

Coach does not push to the shipping tree, authorise the RC scope, implement the match-state authority, or sign off the release. Here Coach only resolved three SHAs against the live remote, produced a reference audit, and expanded one under-scoped cleanup.
