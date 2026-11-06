# run-stage3.ps1
$blend  = "C:\Users\mongo\OneDrive\Desktop\147VR_147NEON_VINTAGE_ArtPass_AAA naris.blend"
$script = "C:\Users\mongo\AppData\Local\Temp\opencode\stage3_export.py"
$analysis = "C:\Users\mongo\UnityProjects\147 VR\Docs\AAA\NeonVintageArtPass-analysis.json"
$outroot = "C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\Imports\NeonVintageArtPass"
$log    = "C:\Users\mongo\AppData\Local\Temp\opencode\stage3-log.txt"
$budget = 155000
"RUN $(Get-Date -Format 'HH:mm:ss') budget=$budget -> $outroot" | Out-File $log -Encoding utf8
New-Item -ItemType Directory -Force -Path $outroot | Out-Null
$outp = & "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup $blend --python $script -- $analysis $outroot $budget 2>&1 | Out-String
$exit = $LASTEXITCODE
$outp | Add-Content $log -Encoding utf8
"exit: $exit" | Add-Content $log -Encoding utf8
Write-Output "STAGE3 EXIT: $exit"
Get-Content $log | Where-Object { $_ -match "STAGE3TRIS|STAGE3WROTE|Traceback|Error|error" } | Select-Object -First 25
if (Test-Path "$outroot\manifest.json") {
    $j = Get-Content "$outroot\manifest.json" -Raw | ConvertFrom-Json
    Write-Output "== report =="
    $j.report | ConvertTo-Json -Depth 3
    Write-Output "== fbx files =="
    $j.fbx | ForEach-Object { $_.name }
    Write-Output ("fbx count: " + $j.fbx.Count)
}
