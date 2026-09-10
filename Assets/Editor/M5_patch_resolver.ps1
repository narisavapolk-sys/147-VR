$p='C:\Users\mongo\UnityProjects\147 VR\Assets\Scripts\AAA\Physics\M5RealStraightBatchRunner.cs'
Copy-Item -LiteralPath $p -Destination ($p+'.M5Resolver.bak') -Force
$s=[IO.File]::ReadAllText($p)
$pattern='var info = tracker != null \? tracker\.FindBall\("White_CueBall"\) : null;\s*cueBall = info\?\.transform != null \? info\.transform\.GetComponent<Rigidbody>\(\) : null;'
$replacement='var info = tracker != null ? tracker.FindBall("White_CueBall") : null;'+[Environment]::NewLine+'            if (info == null && tracker != null)'+[Environment]::NewLine+'            {'+[Environment]::NewLine+'                foreach (var candidate in tracker.Balls)'+[Environment]::NewLine+'                    if (candidate != null && candidate.points == 0 && candidate.transform != null) { info = candidate; break; }'+[Environment]::NewLine+'            }'+[Environment]::NewLine+'            cueBall = info?.transform != null ? info.transform.GetComponent<Rigidbody>() : null;'+[Environment]::NewLine+'            if (cueBall == null)'+[Environment]::NewLine+'            {'+[Environment]::NewLine+'                var fallback = GameObject.Find("Sphere.009");'+[Environment]::NewLine+'                cueBall = fallback != null ? fallback.GetComponent<Rigidbody>() : null;'+[Environment]::NewLine+'            }'
$s2=[regex]::Replace($s,$pattern,$replacement,1)
if($s2 -eq $s){throw 'RESOLVER_PATTERN_NOT_FOUND'}
[IO.File]::WriteAllText($p,$s2,(New-Object Text.UTF8Encoding($false)))
Write-Host 'M5_REAL_RESOLVER_PATCHED'