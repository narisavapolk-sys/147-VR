# S1_COMMIT_4GROUPS.ps1 — 147VR-import-wip — 4-group safety commits (one click)
# Run: powershell -ExecutionPolicy Bypass -File Docs\AI_TEAM\Tools\S1_COMMIT_4GROUPS.ps1
# Prepared 2026-11-04 (S0 fix + regressions verified headless green 10/10 before this)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'C:\Users\mongo\UnityProjects\147VR-import-wip'

function Commit-Group {
  param([string]$Title, [string[]]$Paths, [string]$Msg)
  Write-Host ""
  Write-Host "=== $Title ==="
  git add -A -- $Paths
  if ($LASTEXITCODE -ne 0) { throw "git add failed: $Title" }
  $files = @(git diff --cached --name-only | Where-Object { $_ })
  Write-Host ("staged {0} files" -f $files.Count)
  if ($files.Count -eq 0) { Write-Host "  (nothing to commit, skipped)"; return }
  git commit -m $Msg
  if ($LASTEXITCODE -ne 0) { throw "git commit failed: $Title" }
  Write-Host "  committed."
}

# G1 - PoolPhysics core + headless QA + S0 report (fix 1.75x + T3b/T3c/T3d + golden bay4)
Commit-Group 'G1 PoolPhysics + QA' `
  @('Assets/PoolPhysics','Assets/PoolPhysics.meta','PoolPhysics','WireReports','Assets/Tests') `
  'PoolPhysics S0: corner capture 1.75x fix + headless QA green (jartest 10/10, props 400, golden 0.000E+000) + rail-hug & side pass-bye regressions + WireReports/S0_CLOSE'

# G2 - Quest gameplay scripts (M5 lifecycle, trackers, cue)
Commit-Group 'G2 Quest gameplay' `
  @('Assets/Scripts') `
  'Quest gameplay: shot lifecycle, ball tracker, cue controller, snooker physics wiring, quality/authority scripts'

# G3 - AAA imports + lighting/QA editor tooling + room assets
Commit-Group 'G3 AAA/Lighting/Tools' `
  @('Assets/AAA','Assets/Audio','Assets/Audio.meta','Assets/Editor','Assets/Prefabs','Assets/Resources','Assets/Settings','Assets/147 main') `
  'AAA imports + lighting/QA editor tooling: LightRigAB, BakeGI, QuestQuality, ToolRunner, FbxAutoImportRules, room textures + audio + settings'

# G4 - Docs / ProjectSettings / Packages / Scenes / root agent files (state sync)
Commit-Group 'G4 Docs+Settings sync' `
  @('Assets/Scenes','Docs','ProjectSettings','Packages','AGENTS.md','CLAUDE.md') `
  'State sync 2026-11-04: docs, project settings, packages, scenes, agent rules'

Write-Host ""
Write-Host "=== LEFTOVER (expected ONLY: Assets/_Recovery, _staging_old) ==="
git status --porcelain
$leftover = git status --porcelain | Where-Object { $_ -notmatch '_Recovery|_staging_old' }
if (@($leftover).Count -gt 0) {
  Write-Warning ("UNEXPECTED leftover x{0} - review above before thinking S1 done" -f @($leftover).Count)
} else {
  Write-Host "S1 CLEAN - trunk safe. Next: S2."
}
Write-Host ""
git log --oneline -5
