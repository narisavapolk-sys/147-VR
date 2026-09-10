$w=New-Object -ComObject WScript.Shell
$w.AppActivate('147 VR - SampleScene - Android - Unity 6.4 (6000.4.4f1) <DX11>')
Start-Sleep -Milliseconds 300
$w.SendKeys('%t')
Start-Sleep -Milliseconds 500
