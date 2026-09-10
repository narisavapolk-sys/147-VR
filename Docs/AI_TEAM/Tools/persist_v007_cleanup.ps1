$main = 'C:\Users\mongo\UnityProjects\147 VR\Assets\Scenes\147VR_MainScene.unity'
$tmp = 'C:\Users\mongo\UnityProjects\147 VR\Assets\Scenes\147VR_MainScene_V007_CLEANUP_TEMP.unity'
if (-not (Test-Path -LiteralPath $tmp)) { throw 'cleanup temp scene missing' }
Get-Process Unity,UnityPackageManager -ErrorAction SilentlyContinue | Out-Null
if ($?) { }
Copy-Item -LiteralPath $tmp -Destination $main -Force
Remove-Item -LiteralPath $tmp -Force
Write-Host 'V007_ORPHAN_CLEANUP_PERSISTED'
