# COACH WORK BRIEF — D-3: Unity 12f1 ↔ UPM end-to-end on an empty project

**Issued by:** Coach seat (per `COACH_OPERATING_CONTRACT_147VR.md`)
**Date:** 2026-09-23
**Executor:** Luna (this brief contains no action Coach can perform — no machine access)
**Predecessor:** `COACH_REVIEW_002_UPM_INSTALL_ARCHITECTURE_20260923.md`
**Scope:** D-3 only. No migration. No real project. No launcher edits.

---

## 0. STATUS OF RECORD AFTER D-1 / D-c

### 0.1 What D-1 established (OWNER-PROVIDED; not verifiable from this seat)

```
argv : server -s 10328 --ipc-path Unity-Upm-D1 -l 2
CWD  : C:\Temp\UnityEditors\6000.4.12f1\Editor\Data\Resources\PackageManager\Server
env  : the process-local guard
upm.log : Command-line: …6000.4.12f1…\UnityPackageManager.exe server -s 10328 --ipc-path Unity-Upm-D1 -l 2
          Detected environment variables: NO_PROXY=localhost,127.0.0.1
          IPC server started (IPC path=Unity-Upm-D1).
          Hint: … -upmIpcPath Upm-D1
time-to-IPC : ≈ 59 ms
termination : shutdown because parent PID 10328 is no longer running  (correct standalone behaviour)
```

**Corroboration available to this seat:** the committed 4f1 healthy values are IPC connects of **0.2 s (2026-09-01, `Upm-13172`)** and **0.3 s (2026-08-29)**. 59 ms is the **same order of magnitude** — consistent with a healthy Server. Nothing in the committed record contradicts it.

### 0.2 Withdrawal, recorded explicitly

> **`12f1 UPM = no IPC after ~302 s` is WITHDRAWN as evidence.**

Reason: the launch vector was never established. Per Review #002 §3.0, a run whose **argv / CWD / PATH are unrecorded** is **INVALID**, not FAIL — it cannot support any conclusion. That is exactly what happened.

The "guarded run survived ~302 s with no IPC and no stderr" observation is retained **only** as: *a standalone launch with an unspecified vector neither crashed nor logged IPC.* It is not evidence about 12f1 UPM capability.

### 0.3 What remains genuinely unproven

| Level (Review #002 §4) | State |
|---|---|
| L0 attributable launch | **PASS** (D-1 log names the path; hash recorded by owner) |
| L1 standalone IPC | **PASS** (59 ms, `IPC server started`) |
| L2 client round-trip (3 endpoints = 200) | **UNKNOWN** — no client has connected |
| L3 Unity end-to-end on empty project | **BLOCKED / not yet run** |
| H1 / H2 / H3 (install-architecture hypotheses) | **now NON-BLOCKING.** Standalone IPC working means the packaging question no longer gates anything. The `Server\app` directory diff stays recorded as an **anomaly** for a future D-d pass — it must not be described as a defect. |
| migration | NOT STARTED · real project UNTOUCHED · certified core UNTOUCHED |

### 0.4 One thing D-1 did NOT answer — and why D-3a matters

D-1 fixed variables that **Unity supplies itself** (`argv`, `CWD`). The previously recorded failure *"empty-project 12f1 launch: Unity failed to connect to UPM after 30 s"* is therefore **not explained** by D-1. In the Unity-spawned path, a perfectly formed `--ipc-path` is expected by default — so either something about Unity's spawn vector differs from the now-known-good vector, or the 09-16 delayed-start class (S3) is real and recurring.

**D-3a below exists to settle exactly this by comparison, not by speculation.**

---

## 1. THREE OPERATIONAL FACTS DERIVED FROM D-1 (get these wrong and D-3b cannot work)

1. **IPC path name transform.** The server receives `--ipc-path Unity-Upm-<tag>`; Unity is then told **`-upmIpcPath Upm-<tag>`** — the `Unity-` prefix is dropped. Same transform in the 09-16 record (`--ipc-path Unity-Upm-10328` → `-upmIpcPath Upm-10328`). **Do not pass the untransformed string to Unity.**
2. **`-s <pid>` appears to be the *supervised parent* PID.** The observed shutdown ("parent process [10328] is no longer running") follows an argv of `-s 10328`. Strong inference — but it has a direct consequence: **a pre-started server dies as soon as its watched PID dies**, so D-3b requires a **watched PID that outlives the entire Unity session**. Resolve this before D-3b (§2, D-3.0).
3. **CWD = the Editor `Server` directory** was part of the known-good vector. Record Unity's own spawned CWD in D-3a and compare.

---

## 2. D-3.0 — resolve `-s` semantics (cheap; decides the D-3b choreography)

**Question:** is `-s <pid>` a supervised parent PID, or something else (session id / socket id) that only *coincidentally* matched?

**Method (one or two short runs, no Unity):**
- **A:** start the server with `-s` = PID of a long-lived process you control (e.g. the host shell). Observe: does it stay alive after `IPC server started`?
- **B:** start the server with `-s` = a PID that does not exist. Observe: same shutdown message immediately after IPC, or different behaviour?

**Acceptance:** the two runs differ in a predictable way ⇒ semantics established. Identical behaviour in both ⇒ `-s` is not a liveness check; record that and use the observed lifetime rules instead.

**Output:** one `.txt` artifact with both runs' argv, observed lifetime, and `upm.log` excerpts.

---

## 3. D-3a — PLAIN 12f1 empty-project launch (run this BEFORE any workaround)

**Purpose:** answer "does the Unity-spawned path still fail, and if so at which stage" — with the log that has always classified this failure class.

**Preconditions:**
- A **genuinely empty, valid** project exists per §5 (do not use `147VR_M53_VALIDATE`, do not use the real project).
- No `Unity.exe` / `UnityPackageManager.exe` running before launch (record the process count as evidence).
- Guard applied in the launching process (same 9 values, **plus `APPDATA` explicitly recorded** — it was the one value 09-16 never verified).
- `PATH` filtered for `_npx` / `npm-cache` — **and the filtered PATH recorded**.
- Orphaned `Upm-*` entries in `%TEMP%` checked and recorded (present/absent).

**Launch:** plain 12f1 batch/headless on that project with `-logFile` at an **absolute** path. **No** `-noUpm`. **No** `-upmIpcPath` yet.

**Capture (all required):**
1. `Editor.log` (or the `-logFile`) — full.
2. **`upm.log` for this run** — located and copied. This is the decisive artifact.
3. **Unity's spawned UPM process vector:** command line, executable path, CWD, parent PID — captured **while it is alive**.
4. **Time-to-IPC** (or an explicit statement that IPC never started within the observation window).
5. UPM binary identity re-hashed at run time (path + size + SHA-256).
6. Exit code, and the process count after the run.

**Decision:**
- `IPC server started` + the three API endpoints return **200** + compile OK + exit 0 ⇒ **L2 and L3 PASS.** Stop. The workaround is unnecessary — do **not** add `-upmIpcPath` to anything.
- UPM connects but later than Unity's 30 s window ⇒ **`DEGRADED-PASS`, 09-16 class (S3)** → then D-3b.
- No IPC within the window ⇒ then D-3b.
- Any other signature ⇒ record verbatim as a **new** signature (do not force it into S1–S4).

---

## 4. D-3b — pre-started server + `-upmIpcPath` (only if D-3a fails to connect)

**Recipe (with D-3.0's answer applied):**
1. Start a **host process that will outlive the whole Unity session**.
2. Start the server with `-s <that PID>` and `--ipc-path Unity-Upm-<tag>`, CWD = the 12f1 `Server` dir, guarded env.
3. **Wait for `IPC server started (IPC path=Unity-Upm-<tag>)` in `upm.log` BEFORE launching Unity.**
4. Launch Unity with `-upmIpcPath Upm-<tag>` (**transformed name**), guarded env, absolute `-logFile`.
5. Verify the client actually used that endpoint: the `upm.log` must show the API calls arriving **as 200s**.

**Acceptance (L2 + L3 in this single run):**
- `upm.log`: `IPC server started` **and** `config:project:get-registries = 200` **and** `project:list-packages = 200` **and** `packages:get-all-packageinfo = 200` **and no** `Received undefined`.
- `Editor.log`: no "Failed to start the Unity Package Manager local server process", no "Could not connect to IPC stream".
- Script compile completes; **process exit code 0**.
- All six captures from §3 present.

**If Unity 12f1 rejects `-upmIpcPath`** (it was rejected pre-09-20 and accepted 09-20 — environment-sensitive): **stop D-3b**, record the rejection verbatim, and fall back to characterising the Unity-spawned race (measure S3 if it reproduces). Do **not** iterate flags blindly.

---

## 5. "GENUINELY EMPTY VALID PROJECT" — definition + acquisition, given the failed `-createProject`

### 5.1 Definition (all must hold)
1. Created **by 12f1 itself** (so its `ProjectVersion.txt` is 12f1 — otherwise the experiment contains a second upgrade event).
2. Unity **opens it and resolves packages**; `Library/` is produced.
3. Contains **no game assets**, **no `Assets/Editor` auto-run or `[InitializeOnLoad]` hooks**, **no untracked `*_TMP.cs`**, no scene that mutates on open.
4. Lives **outside** the repo and outside `C:\Temp\147VR_M53_VALIDATE` — e.g. a dedicated folder such as `C:\Temp\147VR_EMPTY_12F1`.
5. ⚠ `C:\Temp` **contains the 12f1 editor install** — never wipe/clean that tree. Create a subfolder; do not delete siblings.
6. Path contains no characters that need quoting gymnastics (keep it simple).
7. Recorded: absolute path, creation method, creation timestamp, `ProjectVersion.txt` content, initial `Packages/manifest.json` blob hash.

### 5.2 The failed `-createProject` attempt is itself an artifact
Exit 0 with no directory created is a **completed-without-effect** result, not a crash. Do not guess the cause:
- **Capture that run's Unity log** and read why (arg order, `-quit`, `-nographics`, an existing path, a licensing/permission refusal — the log says which).
- Record it as an artifact under §6. **Do not** re-run variations blindly; read the log first.

### 5.3 Fallback chain if the CLI route cannot create a valid project
Try in order, stopping at the first that satisfies §5.1:
1. Fix the reason found in 5.2 (one variable at a time) and retry once.
2. Create the project through Unity Hub with the 12f1 editor.
3. Copy a **third-party/neutral empty Unity template** from a location that is **not** the 147 VR repo and **not** the validation worktree (e.g. a Unity-shipped template), then open it with 12f1.
4. **Do not** manufacture a "project" by hand-copying `147VR` `ProjectSettings`/`Packages` files — that would carry unknown state into the experiment and produce a non-empty-project result that looks like evidence.

---

## 6. EVIDENCE PACKAGE (F9-compliant)

All artifacts: `.txt` / `.json` / `.xml` under a **tracked** path, **never** `.log`, with sha256 recorded in the commit message or a tracked index file.

Required header on every artifact:

```
EVIDENCE_SCOPE   : D-3 empty-project (no repo content)
LINE_BASE        : <branch>@<sha>            (or N/A for a standalone empty project)
UNITY_EDITOR     : 6000.4.12f1 (<revision>)
EDITOR_EXE       : C:\Temp\UnityEditors\6000.4.12f1\Editor\Unity.exe
UNITY_PACKAGE_MANAGER_SHA256 : 95FDDF…2BEE6 (re-hashed at run time)
UPM_ARGV         : server -s <pid> --ipc-path Unity-Upm-<tag> -l 2
UPM_CWD          : <recorded>
ENV_APPLIED      : <9 values + APPDATA, verbatim>
PATH_FILTERED    : _npx/npm-cache removed = yes/no
SCENE            : N/A (empty project)
COMMIT           : <sha or N/A>
TIMESTAMP        : <ISO8601>
RUNTIME_TARGET   : desktop batch / empty project
CLEAN_WORKTREE   : N/A
RESULT           : PASS | FAIL | BLOCKED | INVALID
```

One **index** artifact per D-3 step listing the member files + their sha256s.

---

## 7. PROHIBITIONS AND STOP CONDITIONS (unchanged, restated because they now bite)

**Do not touch:** `C:\Temp\147VR_M53_VALIDATE` · `C:\Users\mongo\UnityProjects\147 VR` · Main Scene · M5 Rules/Scoring/Turn · V007 / Golden / `147VR-PHY-008.asset` · D8/W1 · the tracked safe launchers (F11) · system-wide environment · firewall · `Library`/`Packages`/`packages-lock.json` deletions · Unity reinstall.

**STOP immediately on:** an unexpected file/scene mutation · an unknown auto-run script · a second physics authority · dirty state of unknown origin · a run whose argv/CWD/PATH were not captured · any result that would require touching a certified system to make it pass.

**Never** report a run as PASS when any §3 capture is missing — that run is **INVALID**.

---

## 8. WHAT COACH WILL DO WITH THE RESULT

- Audit the artifacts against §3/§4/§6 criteria and issue a verdict: `L2 PASS / L3 PASS`, `DEGRADED-PASS`, `BLOCKED`, or `INVALID` (with the missing capture named).
- Only after a clean L3 PASS: propose the **L4 migration discriminator** on `C:\Temp\147VR_M53_VALIDATE` (preflight `TARGET_TREE_HEAD = 7cf232e` + `PRELAUNCH_SAFE`, dirty state captured as content), plus the F8 disposition lines for V007 / M5.1 / M5.2 / M5.3 / D8.
- Coach does **not** authorize production integration. Per contract §18 the human remains the authorization boundary, and per F7 final shipping validation must occur on an explicit integration line.

## 9. ONE-LINE SUMMARY FOR LUNA

> Run **D-3.0** (settle `-s`), then **D-3a** (plain empty-project launch, capture `upm.log` + Unity's spawned UPM vector). Only if it fails to connect, run **D-3b** with the transformed `-upmIpcPath`. Get a genuinely empty 12f1 project first — and record why `-createProject` produced nothing, instead of re-trying it blindly.
