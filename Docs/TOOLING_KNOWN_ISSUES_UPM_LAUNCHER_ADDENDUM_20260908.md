## Addendum — New stall symptom (2026-09-08 19:16), NOT yet root-caused

Distinct from the two documented/fixed issues above (PATH contamination, dual-launcher). This stall: Unity progressed past Licensing/Physics init but accumulated almost no CPU time over 4+ minutes and log stopped growing. Killed safely (no data touched, never reached install step) and retried.

**Root cause unknown.** Possible contributor: heavy concurrent load (10+ Chrome processes, ~2GB combined RSS) observed at the time. Not confirmed as the cause — just correlated.

**Recovery procedure (known-working, not a fix):** if log stops growing for several minutes and Unity process CPU time stays near-zero, verify with `Get-Process -Name Unity | Select CPU`, then terminate cleanly and retry via `Unity_Batch_Safe.ps1`. Safe because the install step (`M5_1_Install_TMP.Run()` or equivalent) hadn't started, so no partial scene/physics state exists to worry about.

**Do not assume this won't recur** until an actual root cause is found — this addendum documents the symptom and safe recovery, not a permanent fix.
