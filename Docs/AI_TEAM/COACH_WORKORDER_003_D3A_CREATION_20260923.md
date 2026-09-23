# COACH WORK ORDER #003 — D-3a: unblock project creation, and two corrections to Coach's own prior order

**Issued by:** Coach seat (per `COACH_OPERATING_CONTRACT_147VR.md`)
**Date:** 2026-09-23
**Executor:** Luna
**Predecessors:** `COACH_WORKORDER_D3A_RECOVERY_20260923.md` (WO#002) · `COACH_REVIEW_002_UPM_INSTALL_ARCHITECTURE_20260923.md`
**Scope:** get a genuinely empty project created by 12f1. No migration, no protected tree, no launcher edit.

---

## 0. VERDICTS ON YOUR REPORT

| Item | Verdict |
|---|---|
| H-b (default log location) | **PASS** — the logs exist, and you did the right thing by reading them before re-running creation. See §1.3 for what they *don't* tell you. |
| H-c (licence gate) | **NO BLOCK FOUND — accepted.** `Product: Unity Personal / Type: Assigned / Expiration: Unlimited` is a resolved entitlement. Your refusal to treat `Access token is unavailable` alone as a failure is correct — and it is the same discipline as §3.2 of WO#002. |
| Reclaim execution state | **PASS**, correctly scoped: two self-created orphans, no protected-tree involvement, decision recorded. |
| F11 decision = Option B | **Accepted in principle — but superseded by B′ (§2), which is strictly better.** |
| Hub CLI route | **Good find. Deprioritised, with a reason (§4).** |
| Bare 12f1 run as a discriminator | **Correct to run it; but it produced no attributable evidence — classified INVALID, see §3.1.** |

**One bonus corroboration you supplied:** the orphan `UnityPackageManager.exe` PID 12600 was still alive while its `ParentProcessId` 13564 had expired. That independently confirms the §0.1 inference of WO#002 — **lifetime follows the watched `-s` token, not the true parent.** Good.

---

## 1. WHAT YOUR LOG EVIDENCE ACTUALLY SETTLES — AND WHAT IT DOES NOT

### 1.1 The launcher proves 4f1 came from Hub and 12f1 did not (verbatim)

From `Docs/Tools/Unity_Batch_Safe.ps1` (read in this session, branch `checkpoint/147vr-apk-clean-publish`):

```powershell
param(
  [string]$ProjectPath = "C:\Users\mongo\UnityProjects\147 VR",
  [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe",
```

The **default 4f1 editor path is a Unity Hub path**. The 12f1 authority is `C:\Temp\UnityEditors\6000.4.12f1\Editor\Unity.exe` — not a Hub path. **FACT: 4f1 was a Hub-installed editor; 12f1 is a manually-placed install.** This single line is the strongest available support for the Hub hypothesis in §4.

### 1.2 The launcher already contains every hardening you said you verified (verbatim)

```powershell
$unityProcs = Get-Process -Name "Unity","UnityPackageManager" -ErrorAction SilentlyContinue
if ($unityProcs) {
  Write-Host "[BLOCKED] Unity or UnityPackageManager already running (PID: $($unityProcs.Id -join ',')). Close it before running this script."
  exit 2
}
$env:PROGRAMDATA = "C:\ProgramData"
$env:ALLUSERSPROFILE = "C:\ProgramData"
$env:APPDATA = "C:\Users\mongo\AppData\Roaming"
$env:LOCALAPPDATA = "C:\Users\mongo\AppData\Local"
$env:USERPROFILE = "C:\Users\mongo"
$env:TEMP = "C:\Users\mongo\AppData\Local\Temp"
$env:TMP = "C:\Users\mongo\AppData\Local\Temp"
$env:NO_PROXY = "localhost,127.0.0.1"
$env:UNITY_UPM_TIMEOUT = "120"
$env:PATH = (($env:PATH -split ';') | Where-Object { $_ -notmatch '_npx' -and $_ -notmatch 'npm-cache' }) -join ';'
```

Two consequences:
- **Environment gap from Review #002 §3.2 is CLOSED when the launcher is used:** `APPDATA` *is* set explicitly (the one value 09-16 never verified).
- **`-logFile` is always an absolute path** — never `-`:
  ```powershell
  $logFile = Join-Path $logDir "UnityBatch_$stamp.log"
  $argList = @("-batchmode","-nographics","-projectPath", "`"$ProjectPath`"","-logFile", "`"$logFile`"")
  ```

### 1.3 What the 4f1 log your found does NOT establish

Its argv contains **`-noUpm`**:

```text
Version is '6000.4.4f1'
COMMAND LINE ARGUMENTS:
-noUpm
-projectPath
C:\Users\mongo\UnityProjects\147 VR
```

⇒ **That log deliberately bypassed UPM. It cannot serve as a UPM baseline, a UPM control, or a "4f1 worked" datapoint.** Record it as: *last editor log on record belongs to a `-noUpm` run.* Also note it is a **4f1** log of the **real project** — so it is not evidence about 12f1 at all.

---

## 2. CORRECTION TO MY OWN WO#002 (§3): OPTION **B′** BEATS OPTION **B**

WO#002 §3 assumed the launcher would have to be modified to point at 12f1. **It does not.** The file is already parameterized:

```powershell
[string]$ProjectPath = "C:\Users\mongo\UnityProjects\147 VR",
[string]$UnityExe  = "C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe",
```

> **B′ — invoke the tracked launcher as-is with explicit overrides: `-UnityExe <12f1 path> -ProjectPath <empty project path>`.**

Why this is strictly better than B: **no fork, no derived vector, no tracked change, and full compliance with the 2026-09-08 standing decision** ("only ever invoke `Docs\Tools\Unity_Batch_Safe.ps1`"). It removes an entire class of risk (derived-vector drift) instead of managing it.

**Still mandatory if you hand-derive anything** (e.g. the creation step in §5, which the launcher's fixed argument template cannot express):
1. Pin the provenance: `DERIVED-FROM: Docs\Tools\Unity_Batch_Safe.ps1 @ <blob hash>`.
   - Hash computed **from my mirror** of `checkpoint/147vr-apk-clean-publish`: blob `78afd29cf5274207ccb7ce116ad92ac937052ab0` · sha256 `5abb7a90d909a55fcb071f411dc6b99e7fc7fad2d55921e0ca70055e70e22ccf`.
   - ⚠ **This was computed from a mirror, not your working tree, and your tree may be dirty (F8). Verify on your tree before citing it in an artifact.**
2. Store the derived script as a **tracked** artifact (not an ad-hoc console command), so a future session can audit it.
3. Copy the env-repair + PATH-sanitization + single-instance-guard blocks **verbatim** — not paraphrased.

---

## 3. TWO CLASSIFICATIONS THAT MUST BE CORRECTED BEFORE MORE RUNS

### 3.1 The bare 12f1 run is INVALID, not a finding

You ran the 12f1 editor with `-batchmode -nographics -quit -logFile -` and reported: started, lifetime ≈17.45 s, exit code 1, **no stdout**, and no 12f1 entry in `Editor.log`.

Three separate problems, all with the same remedy:

1. **`-logFile -` means "log to stdout"** — and the invocation used `Start-Process -PassThru -NoNewWindow` **without stdout redirection**. So "no stdout captured" is a **capture artifact**, not evidence that the editor was silent.
2. **Passing any `-logFile` redirects the log away from the default `Editor.log`.** Therefore *"12f1 did not write to `Editor.log`"* is **expected behaviour, not an anomaly**. Do not report it as a fact about 12f1.
   - **Cheap verification (one run):** use an **absolute** `-logFile` and check whether a 12f1 entry also appears in `Editor.log`. Whichever way it comes out, record the answer — it settles the rule on this machine permanently.
3. **Exit code 1 is not interpretable** without the log. The run is therefore **INVALID**, per the criteria we already agreed.

**What survives from that run:** the process **lived ~17.45 s**. That is not an instant crash, and it is a mild positive signal about the binary — record it *only* as an observation with that wording.

### 3.2 The earlier `-createProject` / `-projectPath` attempts were ALSO uninstrumented

They produced no directory and no usable log — but they also had no captured argv and no absolute `-logFile`.

> **Therefore `-createProject` is NOT yet shown to fail. It is UNTESTED.** Stop treating that route as broken, and stop compensating for it.

---

## 4. WHY THE HUB ROUTE HUNG — LEADING HYPOTHESIS (and why I am demoting Hub)

**H-hub: Hub cannot resolve `--editor-version 6000.4.12f1`, because 12f1 is not a Hub-installed editor (§1.1), and a Hub that cannot resolve a version may attempt to fetch it → the hang you observed with no usable output.**

Supporting facts: the Hub CLI invocation was reported as *"CLI ค้างโดยไม่สร้าง"* with no usable output before timeout; the 12f1 install lives under `C:\Temp\UnityEditors\` while the launcher's 4f1 default is under `C:\Program Files\Unity\Hub\Editor\`.

**Discriminators (cheap, before running Hub create again):**
1. Ask the Hub CLI for its **installed-editor list** (or Hub's project/editor view): does it know `6000.4.12f1` at all?
2. **Read Hub's own logs.** Hub logs are conventionally under `%APPDATA%\UnityHub\logs\` — **verify the actual path on this machine rather than assuming**, then record it.
3. Check whether Hub spawned any download/network activity during the hang.

**Rule: do not re-run Hub create until (1) and (2) explain the hang.** A resolved-looking command that silently triggers a network fetch is exactly how a "creation failure" becomes an uncontrolled background operation.

### 4.1 Amendment to my own WO#002 §5.2 ranking

I previously ranked **Hub first**. With the evidence in §1.1, that ranking is wrong: **the editor's own `-createProject` (instrumented) is now the preferred route**, and Hub becomes the fallback. Amendment recorded.

**Note `-createProject` vs the launcher's fixed template:** the launcher always injects `-projectPath` and `-quit` and has no `-createProject` slot (though `-ExtraArgs` can append). A **creation step** therefore may legitimately be a declared, derived vector — creation is not a project run: it has no UPM, no compile, no PlayMode semantics. **Declare it as: "creation-only vector, no project run."** Then use the tracked launcher (via B′) for the actual D-3a project run.

---

## 5. RANKED NEXT ACTIONS

**A1 — creation attempt, ONCE, fully instrumented (preferred route)**
- Editor: the 12f1 binary. Target: a fresh path, e.g. `C:\Temp\D3A_EMPTY_6412_B` (keep `C:\Temp` siblings; never clean that tree).
- Vector: declared **creation-only**; env-repair + PATH-sanitization + single-instance-guard blocks copied **verbatim** from the launcher (§2.3), **absolute** `-logFile`, argv recorded **as received**.
- Capture: exit code · process lifetime · **before/after directory listing** · **process inventory before/after** · the log file.
- Then: **read the log before anything else.**

**A2 — act on the log.** One variable at a time. If it says licensing → surface immediately.

**A3 — Hub route, only if A1 fails.** Prerequisites: §4 discriminators (1) and (2) resolved.

**A4 — declared variant**: neutral Unity template, **declared** with its version and the first-open upgrade event. Never merge its results with a 12f1-native run. **Never** hand-assemble a project from 147 VR files.

**A5 — the D-3a project run itself**, via the **tracked launcher with overrides** (B′):
```powershell
Docs\Tools\Unity_Batch_Safe.ps1 -UnityExe "C:\Temp\UnityEditors\6000.4.12f1\Editor\Unity.exe" -ProjectPath <empty project>
```
- Exit codes are a ready-made classifier (verbatim from the launcher): `0` PASS · `2` another Unity live (guard) · `3` timeout — **leaves the process running, which is exactly how the earlier orphan appeared** · `4` tooling signature · `5` compile error · `1` unknown.
- ⚠ **See §6: exit 4 must not be read as a UPM verdict.**
- The launcher writes its log to `$ProjectPath\Docs\UnityBatchLogs\UnityBatch_<stamp>.log` → **inside the empty project**. Declare that (it is a log directory, not asset contamination), and **copy the decisive lines into a tracked `.txt` artifact per F9**.

---

## 6. PRE-CLASSIFY THE FALSE ALARM — THE LAUNCHER WILL LIKELY RETURN EXIT 4 ON A HEALTHY RUN

Verbatim from the launcher:

```powershell
if ($logText -match "Could not connect to IPC stream" -or $logText -match "Received undefined") {
  Write-Host "[BLOCKED-TOOLING] UPM IPC/environment blocker signature found in log."
  exit 4
}
```

and

```powershell
$isTestRun = $ExtraArgs -match '(^|\s)-runTests(\s|$)'
if (-not $isTestRun) { $argList += "-quit" }
```

Combine with the committed finding that the **stock `-quit` path emits `path argument … undefined` as a documented, understood quirk** (`Docs/AI_TEAM/COACH_FINAL_SIGNOFF_WPBSA_20260908.md`) and that the stock `-quit` path **"does not return a clean process code in the Safe Batch wrapper"** (`Docs/AI_TEAM/WPBSA_FINAL_GATE_20260908.md`).

⇒ **A D-3a run through this launcher can return exit 4 while the Package Manager is perfectly healthy.** The launcher applies the *broad* rule that WO#002 §6 already amended. **The co-occurrence rule overrides it:**

| Observation | Classification |
|---|---|
| `Received undefined` **and** all three endpoints = **200** **and** compile succeeded | **documented `-quit`-path quirk** → not a failure; record whether it appears on 12f1 |
| `Received undefined` **without** the 200s, or with an IPC connect failure | **real UPM/config failure** |
| Exit code alone (any value) | **never gates PASS** |

Everything else in WO#002 §6 stands.

---

## 7. ACCEPTANCE FOR D-3a (unchanged from WO#002 §6)

`L2` = `config:project:get-registries` **200** + `project:list-packages` **200** + `packages:get-all-packageinfo` **200** in `upm.log` during a Unity-connected session.
`L3` = Unity 12f1 on the empty project: connects + compiles + clean shutdown, with the full §4 capture set and the F9 evidence header.
**A higher level may never be claimed from a lower level's evidence** — and a run missing any required capture is **INVALID**, not FAIL.

---

## 8. REGISTRY DELTA

| Item | Delta |
|---|---|
| H-b | **PASS** — `Editor.log` / `Editor-prev.log` located and read; the log on record is a **4f1 `-noUpm` run of the real project** ⇒ not a UPM baseline |
| H-c | **NO BLOCK FOUND** — entitlement resolved, licence Unlimited |
| H-a / reclaim | **PASS** — two self-created orphans terminated; no protected-tree involvement |
| F11 | decision **B → amended to B′** (call-site override, no fork, no tracked change) |
| **F11b** | **newly relevant**: 4f1 is a Hub install, 12f1 is not — check whether the 4f1 Hub install still exists (one `Get-Item` on the default path) |
| `-createProject` | reclassified **UNTESTED** (prior failures were uninstrumented) |
| Hub route | **demoted** pending §4 discriminators |
| Certified core / protected trees | unchanged, untouched |

## 9. ONE-LINE SUMMARY FOR LUNA

> Don't re-run Hub yet. Run **one instrumented `-createProject` with an absolute `-logFile`** using the launcher's hardening verbatim (§5 A1), read the log, and remember that `-logFile -` plus unredirected stdout is why your bare 12f1 run proved nothing. Then run D-3a through the **tracked launcher with `-UnityExe`/`-ProjectPath` overrides** — and expect exit 4 from the documented `-quit` quirk without it meaning UPM failed.
