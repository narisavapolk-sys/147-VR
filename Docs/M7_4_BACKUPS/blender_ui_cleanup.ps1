Add-Type -AssemblyName System.Windows.Forms
$sh=New-Object -ComObject WScript.Shell
$sh.AppActivate(4436) | Out-Null
Start-Sleep -Milliseconds 700
[System.Windows.Forms.SendKeys]::SendWait('+{F4}')
Start-Sleep -Milliseconds 700
$code=@"
import bpy
for n in ('BALL Markings','D Marking'):
    o=bpy.data.objects.get(n)
    if o: bpy.data.objects.remove(o, do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\147VR_Table_WPBSA_Visual_Clean_v007.blend')
for o in bpy.context.scene.objects: o.select_set(False)
sel=[o for o in bpy.context.scene.objects if o.type=='MESH' and not o.name.startswith('ANCHOR_')]
for o in sel: o.select_set(True)
bpy.context.view_layer.objects.active=sel[0]
bpy.ops.export_scene.fbx(filepath=r'C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\147VR_Table_WPBSA_Visual_Clean_v007.fbx',use_selection=True,object_types={'MESH'},apply_scale_options='FBX_SCALE_ALL',path_mode='AUTO')
bpy.context.scene.render.filepath=r'C:\Users\mongo\UnityProjects\147 VR\SNOOKER   VR pool table\Blender\v007_live_hero_table.png'
bpy.ops.render.render(write_still=True)
print('V007_UI_DONE',len(sel))
"@
$cmd='exec('+($code | ConvertTo-Json -Compress)+')'
[System.Windows.Forms.Clipboard]::SetText($cmd)
[System.Windows.Forms.SendKeys]::SendWait('^v')
[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
Start-Sleep -Seconds 25
