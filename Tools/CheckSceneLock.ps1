$p = 'C:\Users\mongo\UnityProjects\147 VR\Assets\Scenes\147VR_MainScene.unity'
try {
  $fs = [System.IO.File]::Open($p, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
  $fs.Close()
  Write-Output 'SCENE_UNLOCKED'
} catch {
  Write-Output ('SCENE_LOCKED: ' + $_.Exception.Message)
}
