# run-stage2.ps1
$blend  = "C:\Users\mongo\OneDrive\Desktop\147VR_147NEON_VINTAGE_ArtPass_AAA naris.blend"
$script = "C:\Users\mongo\AppData\Local\Temp\opencode\stage2_analysis.py"
$out    = "C:\Users\mongo\UnityProjects\147 VR\Docs\AAA\NeonVintageArtPass-analysis.json"
$log    = "C:\Users\mongo\AppData\Local\Temp\opencode\stage2-log.txt"
New-Item -ItemType Directory -Force -Path (Split-Path $out -Parent) | Out-Null
"RUN $(Get-Date -Format 'HH:mm:ss') -> $out" | Out-File $log -Encoding utf8
$outp = & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup $blend --python $script -- $out 2>&1 | Out-String
$exit = $LASTEXITCODE
$outp = & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup $blend --python $script -- $out 2>&1 | Out-String
$exit = $LASTEXITCODE
$outp | Add-Content $log -Encoding utf8
"exit: $exit" | Add-Content $log -Encoding utf8
if (Test-Path $out) { $j = Get-Item $out; "json size: {0:N0} KB" -f ($j.Length/1KB) | Add-Content $log -Encoding utf8 }
Write-Output "STAGE2 EXIT: $exit"
