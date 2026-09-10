$p = 'C:\Users\mongo\UnityProjects\147 VR\Docs\Tools\Unity_Batch_Safe.ps1'
$s = Get-Content $p -Raw
$old = '$argList += "-quit"'
$new = 'if ($ExtraArgs -notmatch ''(^|\s)-runTests(\s|$)'') { $argList += "-quit" }'
if (-not $s.Contains($old)) { throw 'target line not found' }
Set-Content -Path $p -Value $s.Replace($old,$new) -NoNewline
Write-Host 'UPDATED_SAFE_LAUNCHER'
