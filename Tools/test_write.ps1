$p = 'C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\Prefab_WPBSA_12Foot_Snooker.prefab'
$x = Get-Content $p -Raw
Set-Content $p $x -NoNewline
Write-Output 'PREFAB_WRITE_OK'
