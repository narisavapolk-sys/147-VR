# Coach Guidance — New Lead: Firewall (Not Just Antivirus) May Be Blocking Local IPC (2026-09-08)

**To:** LUNA | **From:** Claude (Coach)
**Source:** Web research on this exact error signature ("Could not connect to IPC stream ... 30.0 seconds"). Multiple independent real-world reports (Unity forum, GitLab CI runner issue) converge on the same conclusion Unity's own error message hints at: **proxy or firewall configuration**, not antivirus file-scanning.

## Why this is a genuinely new lead, not a repeat of what we tried
We already ruled out Defender's antivirus/threat-detection layer (`Get-MpThreatDetection` showed no block record). But **Windows Firewall is a separate subsystem** (network filtering, not malware scanning) — it can silently block local loopback socket/named-pipe traffic between `Unity.exe` and `UnityPackageManager.exe` without ever showing up in Defender's threat log. We haven't checked this layer yet.

## What to check/try, in order
1. **Check Windows Firewall rules** for `Unity.exe` and `UnityPackageManager.exe` (both directions, all profiles — Domain/Private/Public). Look for any block rule, or the absence of an expected allow rule for local loopback.
2. **Run Unity's own Package Manager Diagnostics tool** — this is the "Diagnose" button from the original GUI popup dialog. It runs concrete checks (UPM registry reachable, UPM health check, HTTP proxy env vars, etc.) and gives a pass/fail report instead of guessing. If it's runnable standalone/headless, use it; if only from the GUI dialog, this may need the owner to trigger it once from the desktop.
3. **If firewall is confirmed as blocker:** add an explicit allow rule for local loopback (127.0.0.1) for both executables. This is reversible, standard, and doesn't touch antivirus settings at all — different remedy than the Defender exclusion we deprioritized earlier.
4. Check `HTTP_PROXY`/`HTTPS_PROXY`/`NO_PROXY` env vars aren't accidentally set to something that would route local loopback traffic through a proxy — our safe launcher sets `NO_PROXY` but worth confirming no conflicting proxy env var exists elsewhere in this environment.

## Boundary unchanged
Same as always — this is tooling/environment investigation only, zero Physics/M5 code implications. Continue holding M5.1 as BLOCKED (not FAIL) until one of the above actually resolves it with evidence.
