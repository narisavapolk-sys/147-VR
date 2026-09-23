# COACH REVIEW #002 — 12f1 UPM: installation architecture, right-discriminator check, PASS criteria, migration provenance

**Reviewer seat:** Coach (per `COACH_OPERATING_CONTRACT_147VR.md`)
**Date:** 2026-09-23
**Trigger:** Luna's new finding — `PackageManager\Server` directory structure differs between 12f1 and 4f1 (Luna's H1 = incomplete install · H2 = new packaging with the app bundled in the EXE). Luna's verdict: `12f1 UPM = BLOCKED / installation architecture not yet established`.
**Method:** read-only, no machine access. Machine facts below are **OWNER-PROVIDED** and labelled as such; committed-document facts are cited by path.
**Files modified during this review:** none. No issue created.

---

## 0. VERDICT ON THE VERDICT

Luna's *verdict* is accepted: **`BLOCKED / installation architecture not yet established`** is the correct status, and stopping before mutation is correct.

Luna's *proposed next discriminator* (inspect the 12f1 installer/package manifest to decide whether `Server\app` "should" exist) is **the wrong next step**, for the reason in §2. There is a cheaper discriminator that is already 80% available and that can settle H1 vs H2 without the installer.

One correction to Luna's framing: **do not call this "an incomplete 12f1 installation" yet.** The directory diff is an **anomaly**, not a classification.

---

## 1. INDEPENDENT CHALLENGE OF THE FINDING

### 1.1 The finding, restated as evidence

- 4f1 `Server\`: `UnityPackageManager.exe` · `app\app.js` · `app\package.json` · `app\embedded\…` · `node_modules\@edt\proxy-helper` · `node_modules\node-addon-api`
- 12f1 `Server\`: `UnityPackageManager.exe` · `node_modules\@edt\proxy-helper` · **no `app\`** · no `app.js` · no `app\package.json` · no `node-addon-api`
- A search inside 12f1 `Server` finds no `app.js`

### 1.2 The finding that outranks the directory diff

The **unguarded 12f1 run produced this** (owner-provided, Review #001 §A2):

```
TypeError [ERR_INVALID_ARG_TYPE]: The "path" argument must be of type string. Received undefined
   stack: … getLocalConfigFolder → readConfig → initialize
```

**A missing/unloadable application payload cannot produce that stack.**

If `app\app.js` and its module graph were absent such that the app could not load, the failure would be a **module-resolution error** (`Cannot find module …`, `ENOENT` on a require path) raised by the loader — *before* any application function body runs. Instead, 12f1 executed `initialize()` → `readConfig()` → `getLocalConfigFolder()`, i.e. **application-layer code ran and called an application-layer function that then received `undefined`.**

> **INFERENCE (strong, not absolute): on 12f1 the Package Manager application payload is present and executing.** Therefore H1 in its strong form — *"the app is missing, so UPM cannot run"* — is **contradicted by evidence already on the table**, not merely unproven.
>
> **Residual alternative (must be stated, not hidden):** a thin bootstrap could itself expose functions with those names. Judge this by the stack's **file paths** (§3, D-a), not by the names.

### 1.3 A second challenge: the comparison's premise is unverified

The diff compares two directories. It does **not** establish that the binary *observed failing* is the binary whose directory was compared.

- 12f1 was declared at a **custom path** (`C:\Temp\UnityEditors\6000.4.12f1\Editor\Unity.exe`), outside Unity Hub. The machine plausibly holds **more than one** Editor install (Hub 4f1 is the documented rollback anchor).
- The authoritative answer to *"which UPM ran?"* is not a directory listing — it is **the UPM's own log line**, because the 09-16 record proves UPM logs its full command line and path:

```
[2026-09-16T10:10:44.003Z][INFO] Command-line: 'C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Data\Resources\PackageManager\Server\UnityPackageManager.exe' server -s 10328 --ipc-path Unity-Upm-10328 -l 2
```

> **New hypothesis, cheap to close — H3 (wrong-target comparison):** the directory compared is not the directory of the binary that produced the observed failure.

### 1.4 A third challenge: `node_modules\@edt\proxy-helper` being present cuts both ways

The presence of a `node_modules` subtree on 12f1 is evidence that the Server is **still a Node-based application with on-disk module resolution**. That weakens the *strong* form of H2 ("everything is inside the EXE"), but does not exclude a **hybrid** packaging (payload bundled/snapshotted in the binary, some modules still on disk). Only runtime observation or the stack paths can separate these.

### 1.5 Hypotheses, ranked by fit to existing evidence — and one that fits better than either of Luna's

| ID | Hypothesis | Fit to the *guarded* observation (app boots, then no IPC, no stderr) | Fit to the *unguarded* crash (fatal config error) | Verdict |
|---|---|---|---|---|
| **H1′** | **Installation partially present/blocked: env fixed → app boots → a *post-init* dependency (native addon / `app\embedded` content / proxy path) cannot be loaded → silence, no IPC** | **Fits exactly** | Explained independently by env | **Leading candidate** |
| H1 | Incomplete install, app payload absent | Would predict a load-time failure, not silence | Would predict load-time failure | **Contradicted in strong form** (§1.2) |
| H2 | Packaging changed; payload bundled in EXE | Fits (no on-disk payload needed) | Fits | **Live, not established** |
| H3 | The directory compared is not the binary that ran | Either | Either | **Live, cheapest to close** |
| H4 | Missing native addon specifically (`node-addon-api` absent on 12f1) | Plausible (native deps are loaded late, and fail quietly if optional) | Independent | **Sub-case of H1′** |
| H5 | AV/EDR or partial extraction removed files, and/or first-execution scanning delays | Plausible (also explains the 09-16 218 s anomaly and the >300 s silence) | Independent | **Live, cheap (Review #001 D-4)** |

**H1′ is important because it is the only hypothesis that explains *both* observations simultaneously without a second defect:** broken env → fatal `initialize()` crash; fixed env → init passes → then a missing/blocked *post-init* resource prevents the IPC endpoint from ever being created. `node-addon-api` missing + `app\embedded` missing + "boots but silent" is a coherent single story.

**Do not promote H1′ to fact.** It is the best-fitting hypothesis and it dictates the next two discriminators (§3), nothing more.

---

## 2. ARE WE CHASING THE RIGHT DISCRIMINATOR? — NO

Luna proposes: *inspect the 12f1 installer/package manifest and compare against 4f1 to establish whether `Server\app` should exist.*

**Challenge — three independent reasons this is the wrong next step:**

1. **It answers a different question.** The manifest answers *"what does the vendor ship?"*. The open question is *"why is there no IPC endpoint, given the app executes (or appears to)?"*. Even a definitive manifest answer ("`app\` should be there") would not tell us which dependency fails, and a "it should not be there" answer would not explain the silence either.
2. **Its premise is unverified.** It presumes the compared directory belongs to the running binary (§1.3). Settle H3 first; otherwise the manifest comparison may characterise an install that never ran.
3. **Cost asymmetry.** Locating a retained installer for a custom-path editor, enumerating its payload, and comparing against 4f1 is session-scale work with a **decision payoff that is cheap either way** — if the install is broken, the action is "re-verify/replace the 12f1 install from a verified source", which is a *provenance* action, not a forensics result.

**What to chase instead, in order — cheapest and most causal first:**

- **D-a: the verbatim stack with file paths.** The reported stack was filtered to function names. A Node stack trace normally carries **file:line**. If frames resolve to `<12f1 root>\…\Server\app\app.js:NNN`, then the payload exists *and* the compared directory was the wrong one (H3) or the listing was non-recursive/incomplete. If frames resolve to an internal/embedded/snapshot location, H2 is supported. **One artifact, near-conclusive for H1/H2/H3.**
- **D-b: observe the live process.** While the guarded run is alive, capture the PID's **open handles / loaded modules / attempted file opens** (whatever the host tooling allows) and the **pipe endpoint state**. This is the only observation that shows what the app is *actually* waiting on, and it directly discriminates "post-init dependency missing" (H1′) from "healthy but slow".
- **D-c: argv + CWD + PATH**, held byte-identical to the 09-16 shape (`server -s <port> --ipc-path <name> -l 2`, CWD = the Editor/Server context). See §3.0 — **this alone may dissolve the whole question** if the earlier guarded run omitted `--ipc-path`.
- Only then: **D-d** — installation provenance inspection (installer/archive retention, per-file integrity, MOTW, AV quarantine events, second-execution timing).

---

## 3. GUARDED ENVIRONMENT/CONFIG vs THE 2026-09-16 EVIDENCE

### 3.0 HARD GATE BEFORE ANY COMPARISON — three fields are unknown in the guarded run

The 09-16 known-good invocation is fully specified: `server -s 10328 --ipc-path Unity-Upm-10328 -l 2`, spawned **by Unity**, with Unity setting the process context. The guarded standalone run's **argv, CWD, and PATH are not recorded anywhere**.

Consequences:
- If `--ipc-path` was **not** passed, "no IPC endpoint" is **expected behaviour, not a defect**. Every conclusion drawn so far would be void.
- If `-l 2` was not passed, the log verbosity differs from 09-16 and apples-to-apples log comparison is impossible.
- A different CWD changes `getLocalConfigFolder()` resolution and relative module resolution.

> **A comparison without these three fields is not a controlled comparison.** They are recoverable from the wrapper script / console history — cheap.

### 3.1 Value-by-value comparison

| Variable | 09-16 sanitised set (committed) | Guarded run (owner-provided) | Assessment |
|---|---|---|---|
| `PROGRAMDATA` | `C:\ProgramData` | `C:\ProgramData` | match |
| `ALLUSERSPROFILE` | `C:\ProgramData` | `C:\ProgramData` | match — this was one of the two variables proven missing on 08-28 |
| `USERPROFILE` | `C:\Users\mongo` | `C:\Users\mongo` | match |
| `LOCALAPPDATA` | `C:\Users\mongo\AppData\Local` | same | match |
| `TEMP` / `TMP` | `…\AppData\Local\Temp` | same | match — `TMP` was the other variable proven missing on 08-28 |
| `APPDATA` | **not listed** in 09-16's sanitised set (08-28 doc: "already present") | `C:\Users\mongo\AppData\Roaming` (explicitly set) | **Difference — and it is the critical one (see 3.2)** |
| `NO_PROXY` | `localhost,127.0.0.1` | same | match |
| `UNITY_UPM_TIMEOUT` | `120` | `120` | match — but see 3.3 |
| `PATH` `_npx`/`npm-cache` entries | **explicitly removed** | **not asserted** | **Difference — unverified variable** |
| stale `Library\EditorInstance.json` removed; `Unity.exe` count = 0 | done on 09-16 | not stated for the guarded run | **Difference — unverified variable** |
| orphaned `Upm-*` entries in `%TEMP%` | on the 09-16 checklist | not stated | **Difference — unverified variable** |

### 3.2 The one difference that matters most: `APPDATA`

The crashing function is `getLocalConfigFolder()`. On Windows the *local config folder* is a per-user configuration location derived from the user profile/app-data environment — precisely the family that 09-16 never had to repair, and therefore never verified.

Two consequences:

1. **Setting `APPDATA` in the launching shell is not proof that the spawned process received it, nor that resolution yields a valid, existing, writable directory.** The repair that "fixed" 08-28 was verified by the *symptom* (HTTP 200s), not by printing the resolved path.
2. Therefore the **guarded run's acceptance must include the resolved config path, printed**, not merely the env values set. This is the cheapest possible closure of reviewer Review #001's unknown U2.

### 3.3 Do not attribute behaviour to `UNITY_UPM_TIMEOUT`

It is present in both sets, and the project already established (09-08) that it **does not govern the 30-second connect window**. Including it is harmless; treating it as an experimental variable that changed the guarded run's behaviour is not supported.

### 3.4 The 09-16 evidence also proves something the guarded run has not used

09-16's UPM log recorded, in this order: `Command-line:` → `Detected environment variables:` (only `NO_PROXY` in the excerpt) → `IPC server started` (+218 s) → the `-upmIpcPath` hint → shutdown because the parent had died.

Two operational points:
- **The `Detected environment variables:` line is the app's own view of its env.** That is the artifact that closes "did the process actually receive the guard?" — better than any parent-side dump. The guarded run must produce the equivalent line for 12f1.
- **09-16's failure was a *race*, not a defect**: IPC arrived at +218 s, Unity gave up at 30 s. A guarded 12f1 run that produces no IPC *at all* by an unbounded observation window is **not** the 09-16 signature. Keep them separate in the register (F10: S3 ≠ S4).

---

## 4. ACCEPTANCE CRITERIA — WHEN MAY WE CALL "12f1 UPM = PASS"?

Tiered; **a higher level may never be claimed from a lower level's evidence**. All artifacts follow F9 (tracked `Artifacts/**`, `.txt`/`.json`/`.xml`, never `.log`) with a sha256, and carry the standard header including `UNITY_EDITOR=6000.4.12f1 (<revision>)`, `UNITY_PACKAGE_MANAGER_SHA256`, `COMMIT`, `TIMESTAMP`, `RESULT∈{PASS,FAIL,BLOCKED,INVALID}`.

### Level 0 — attributable launch (provenance, not a pass)
- The exact UPM `Command-line:` line from **the app's own log** names a path under the 12f1 root.
- That binary's size + SHA-256 recorded (`95,112,112` / `95FDDF58…2BEE6` per owner; re-hashed at run time).
- Argv, CWD, and filtered PATH recorded.
- **Level 0 alone may never be reported as PASS.**

### Level 1 — standalone IPC **PASS** (this is the gate currently in question)
All of the following in one run, with no exceptions:
1. Launch args byte-identical in shape to the registered reference: `server -s <port> --ipc-path <name> -l 2` (same flag set; values may differ but must be recorded).
2. CWD = the Editor `Server` directory context (or a documented, justified substitute).
3. Guard applied **and** the app's own `Detected environment variables:` line shows the expected values.
4. The **resolved local config folder is printed** (closes 3.2/U2).
5. `IPC server started (IPC path=<name>)` appears in the app's own log.
6. The named-pipe endpoint is **observed independently** by the pipe probe, with the timestamp it appeared.
7. Time-to-IPC measured.
8. `RESULT` recorded with the observation window stated; a run that is terminated by the wrapper before the pre-registered window is **INVALID**, not FAIL.

Outcome vocabulary:
- **`PASS`** — conditions 1–7 met, time-to-IPC ≤ a pre-registered bound (propose ≤ 10 s; justify any other bound in advance).
- **`DEGRADED-PASS`** — IPC established but later than Unity's 30 s window → documented with the measured time; the 30 s window problem is then a **separate, named** problem (09-16 class), not hidden inside this one.
- **`BLOCKED`** — no IPC within the pre-registered window (propose ≥ 900 s), with the app's log captured.
- **`INVALID`** — argv/CWD/PATH unrecorded or diverging without declaration; no app log captured; wrapper bound shorter than the window.

### Level 2 — client round-trip PASS
IPC alone is **not** a working Package Manager: 08-28 proved IPC-PASS + HTTP 500. Required, from the app's own log:
`config:project:get-registries = 200` · `project:list-packages = 200` · `packages:get-all-packageinfo = 200` · **no** `Received undefined`.

### Level 3 — Unity end-to-end PASS (empty/controlled project, **not** the real project)
12f1 Unity on an empty project: UPM connects, packages register, script compile succeeds (`no error CS`), process exit code 0, and the artifact captures the editor path + revision, UPM path + hash, argv, filtered env, exit code, and the decisive log lines. Only this level may be described as *"12f1 UPM works"*.

### Level 4 — migration discriminator (still not the shipping tree)
Only after Level 3. On `C:\Temp\147VR_M53_VALIDATE`: preflight shows `TARGET_TREE_HEAD = 7cf232e` and `PRELAUNCH_SAFE`, dirty state captured as **content** (porcelain + numstat + raw diffs of `ProjectVersion.txt`, `manifest.json`, `packages-lock.json`), never from `git status` alone.

### Standing rule
A technical PASS at any level is **not** production authorization. Per contract §18 the human remains the authorization boundary; and per F7, final shipping validation must occur on an explicit integration line.

---

## 5. PROVENANCE CHECKLIST BEFORE 12f1 TOUCHES THE REAL MIGRATION

| # | Requirement | Why it blocks |
|---|---|---|
| P1 | **Which install did 12f1 come from?** installer/archive identity, retention, checksum, install method (Hub vs manual extraction to `C:\Temp`) | H3/H4/H5 cannot be closed without it; a manual extraction is a documented partial-install risk class |
| P2 | Enumerate **all** `Unity.exe` / `UnityPackageManager.exe` on disk with version + hash + install root | prevents comparing/attributing the wrong binary (H3) |
| P3 | **4f1 Hub install still exists?** (F11b) state it explicitly | if gone, the migration is one-way, not controlled |
| P4 | **Zone.Identifier / MOTW + AV quarantine events** on the 12f1 binaries; second-execution timing | covers H5; native addons are the usual quarantine target |
| P5 | `ProjectVersion.txt` bump committed as **its own change**, with `baseline 4f1 → change 12f1` | F8: otherwise the first 12f1 open re-serializes locked state with no diff |
| P6 | Explicit disposition for **V007 / M5.1 / M5.2 / M5.3 / D8**: `RE-AFFIRMED under 12f1` (with evidence) or `CERTIFIED on 4f1, re-validation pending` | silence is not an option (F8); also resolves C4 from Review #001 |
| P7 | Preflight against the validation worktree: `TARGET_TREE_HEAD = 7cf232e`, `PRELAUNCH_SAFE`, no live `InitializeOnLoad` sentinels, no untracked auto-runners | prevents a "diagnostic" launch from mutating the scene |
| P8 | Every 12f1 artifact carries `UNITY_EDITOR` + `UNITY_PACKAGE_MANAGER_SHA256` in its header | with `Library/` untracked, this is the only durable environment attribution |
| P9 | Launcher edits (F11) **deferred** until the recipe is validated | brief §10; a launcher change would silently alter the evidence path |
| P10 | `-upmIpcPath` validated **on an empty project only** | rejected-pre-09-20 / accepted-post-09-20 = environment-sensitive; keep it off the real project |

---

## 6. WHAT I AM NOT DOING, AND WHY

- **Not re-asking for data.** Everything needed for §§1–3 is either already provided or recoverable from the wrapper/console.
- **Not classifying 12f1 as an incomplete install.** Anomaly ≠ classification (contract §1 Precise, §17 Golden Rule).
- **Not proposing any patch, reinstall, launcher edit, or system change.** Per brief §10 and contract §15.
- **Not treating the directory diff as the causal path.** Per §2.

## 7. REGISTER / STATE DELTA

| Item | Delta |
|---|---|
| F10 signature set | **add S4 variant note:** S4 = unguarded fatal config crash + guarded silence; **do not merge S4 with S3 (09-16 race)** until a 12f1 run produces a comparable `IPC server started` line or proves it never does |
| F7 / F8 / F9 / F11 / F12 | unchanged |
| F1 | still OPEN — the 12f1 run's app log is not yet a tracked artifact |
| Certified core | unchanged; no physics/scene file appears in any reviewed change |
| Current status | `BLOCKED — UPM initialization/IPC; installation architecture not yet established` (Luna's wording accepted) |

## 8. THE TWO CHEAPEST CLOSURES, IN ONE LINE EACH

1. **The stack, verbatim, with file paths** → closes H1 vs H2 vs H3.
2. **argv + CWD + PATH of the guarded run** → determines whether the current "no IPC" observation means anything at all.
