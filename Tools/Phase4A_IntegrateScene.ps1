$scene = 'C:\Users\mongo\UnityProjects\147 VR\Assets\Scenes\147VR_MainScene.unity'
$phys = 'C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\Prefab_WPBSA_12Foot_Snooker.prefab'
$vis = 'C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\Prefabs\147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab'
$s = Get-Content $scene -Raw
$needle = 'm_SourcePrefab: {fileID: 100100000, guid: cd255914573c9b04f9b2b77c0fe43f7c, type: 3}'
$i = $s.IndexOf($needle)
if ($i -lt 0) { throw 'OUTER_PREFAB_NOT_FOUND' }
$prefix = $s.Substring(0,$i)
$old = 'm_TransformParent: {fileID: 1713820525}'
$p = $prefix.LastIndexOf($old)
if ($p -lt 0) { throw 'OUTER_PARENT_NOT_FOUND' }
$s = $s.Remove($p,$old.Length).Insert($p,'m_TransformParent: {fileID: 0}')
Set-Content $scene $s -NoNewline
$v = Get-Content $vis -Raw
$v = $v.Replace('propertyPath: m_LocalScale.x`r`n      value: 0.01','propertyPath: m_LocalScale.x`r`n      value: 1')
$v = $v.Replace('propertyPath: m_LocalScale.y`r`n      value: 0.01','propertyPath: m_LocalScale.y`r`n      value: 1')
$v = $v.Replace('propertyPath: m_LocalScale.z`r`n      value: 0.01','propertyPath: m_LocalScale.z`r`n      value: 1')
Set-Content $vis $v -NoNewline
$p = Get-Content $phys -Raw
$anchor = "    - target: {fileID: 919132149155446097, guid: 1ccd8aa316db96d48a7113b34b093648, type: 3}`r`n      propertyPath: m_Name`r`n      value: 147VR_Table_WPBSA_12ft_VISUAL_MAIN`r`n      objectReference: {fileID: 0}"
$mods = @"
    - target: {fileID: 919132149155446097, guid: 1ccd8aa316db96d48a7113b34b093648, type: 3}
      propertyPath: m_LocalScale.x
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 919132149155446097, guid: 1ccd8aa316db96d48a7113b34b093648, type: 3}
      propertyPath: m_LocalScale.y
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 919132149155446097, guid: 1ccd8aa316db96d48a7113b34b093648, type: 3}
      propertyPath: m_LocalScale.z
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 919132149155446097, guid: 1ccd8aa316db96d48a7113b34b093648, type: 3}
      propertyPath: m_LocalRotation.w
      value: 0.7071068
      objectReference: {fileID: 0}
    - target: {fileID: 919132149155446097, guid: 1ccd8aa316db96d48a7113b34b093648, type: 3}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 919132149155446097, guid: 1ccd8aa316db96d48a7113b34b093648, type: 3}
      propertyPath: m_LocalRotation.y
      value: 0.7071068
      objectReference: {fileID: 0}
    - target: {fileID: 919132149155446097, guid: 1ccd8aa316db96d48a7113b34b093648, type: 3}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {fileID: 0}
"@
if ($p -notmatch 'target: \{fileID: 919132149155446097.*?m_LocalScale\.x') { $p = $p.Replace($anchor,$anchor+"`r`n"+$mods.TrimEnd()) }
Set-Content $phys $p -NoNewline
Write-Output 'INTEGRATION_TEXT_FIX_APPLIED'
# Normalize all explicit 0.01 scale values in the visual wrapper prefab only.
$v = $v -replace '(propertyPath: m_LocalScale\.[xyz]\r?\n\s+value: )0\.01','$1'+'1'
Set-Content $vis $v -NoNewline
