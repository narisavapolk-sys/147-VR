# rebind_manifest.ps1 — flatten manifest.json into Unity JsonUtility-friendly shape
$root = "C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\Imports\NeonVintageArtPass"
$src  = Get-Content "$root\manifest.json" -Raw | ConvertFrom-Json

# image name -> file path
$imgFile = @{}
foreach ($t in $src.textures) {
    $f = $t.file
    if ($f) { $imgFile[$t.name] = $f }
}

$mats = New-Object System.Collections.Generic.List[object]
foreach ($m in $src.materials) {
    $base = $m.base_color; if (-not $base) { $base = @(1,1,1,1) }
    $emit = $m.emission_color; if (-not $emit) { $emit = @(0,0,0,0) }
    $tex = $m.tex
    $hasTex = ($null -ne $tex)
    $gt = { param($k) if ($hasTex -and $tex.PSObject.Properties[$k]) { $v = $tex.PSObject.Properties[$k].Value; if ($v) { return $imgFile[$v] } } return "" }
    $mats.Add([ordered]@{
        name = $m.name
        baseColor   = @( [float]$base[0], [float]$base[1], [float]$base[2], [float]$base[3] )
        emissionColor = @( [float]$emit[0], [float]$emit[1], [float]$emit[2], [float]$emit[3] )
        metallic = [float]($(if ($null -ne $m.metallic) { $m.metallic } else { 0 }))
        roughness = [float]($(if ($null -ne $m.roughness) { $m.roughness } else { 0.5 }))
        alpha = [float]($(if ($null -ne $m.alpha) { $m.alpha } else { 1 }))
        emissionStrength = [float]($(if ($null -ne $m.emission_strength) { $m.emission_strength } else { 0 }))
        baseTex      = & $gt "base"
        normalTex    = & $gt "normal"
        roughTex     = & $gt "rough"
        metalTex     = & $gt "metal"
        emissionTex  = & $gt "emission"
        specTex      = & $gt "spec"
        clearcoatTex = & $gt "clearcoat"
    })
}

$lmats = New-Object System.Collections.Generic.List[object]
foreach ($m in $mats) { $lmats.Add([pscustomobject]$m) }

$fbx = New-Object System.Collections.Generic.List[object]
foreach ($f in $src.fbx) {
    $fbx.Add([pscustomobject]@{
        name = $f.name
        collection = $f.collection
        tris_after = [int]($(if ($null -ne $f.tris_after) { $f.tris_after } else { 0 }))
    })
}

$lights = New-Object System.Collections.Generic.List[object]
foreach ($l in $src.lights) {
    $c = $l.color; if (-not $c) { $c = @(1,1,1) }
    $sz = $l.size; if (-not $sz -or $sz.Count -lt 2) { $sz = @(0,0) }
    $lights.Add([pscustomobject]@{
        name = $l.name
        collection = $l.collection
        type = $l.type
        color = @( [float]$c[0], [float]$c[1], [float]$c[2] )
        energy = [float]$(if ($null -ne $l.energy) { $l.energy } else { 0 })
        loc = @( [float]$l.loc[0], [float]$l.loc[1], [float]$l.loc[2] )
        rot_euler = @( [float]$l.rot_euler[0], [float]$l.rot_euler[1], [float]$l.rot_euler[2] )
        vis = [int]($(if ($l.vis -eq $true) { 1 } else { 0 }))
        spot_size = [float]($(if ($null -ne $l.spot_size) { $l.spot_size } else { -1 }))
        spot_blend = [float]($(if ($null -ne $l.spot_blend) { $l.spot_blend } else { 0 }))
        size = @( [float]$sz[0], [float]$sz[1] )
    })
}

$textures = New-Object System.Collections.Generic.List[object]
foreach ($t in $src.textures) {
    $textures.Add([pscustomobject]@{
        name = $t.name
        file = $(if ($t.file) { $t.file } else { "" })
        perc_width  = [int]$t.size[0]
        perc_height = [int]$t.size[1]
    })
}

$w = $src.world
$world = if ($w) {
    [pscustomobject]@{
        color = @( [float]$w.color[0], [float]$w.color[1], [float]$w.color[2], 1 )
        strength = [float]$w.strength
    }
} else {
    [pscustomobject]@{ color = @(0.42,0.34,0.26,1); strength = 0.28 }
}

$out = [ordered]@{
    materials = $lmats
    textures  = $textures
    fbx       = $fbx
    lights    = $lights
    world     = $world
}
$json = $out | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText("$root\manifest_flat.json", $json, (New-Object System.Text.UTF8Encoding $false))
Write-Output ("flat written: {0:N0} bytes" -f ((Get-Item "$root\manifest_flat.json").Length))
Write-Output ("materials: {0}  textures: {1}  fbx: {2}  lights: {3}" -f $lmats.Count, $textures.Count, $fbx.Count, $lights.Count)
