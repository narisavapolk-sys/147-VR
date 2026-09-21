Add-Type @'
using System;
using System.Runtime.InteropServices;
public class RM {
 [DllImport("rstrtmgr.dll", CharSet=CharSet.Unicode)] public static extern int RmStartSession(out uint h,uint flags,string key);
 [DllImport("rstrtmgr.dll")] public static extern int RmEndSession(uint h);
 [DllImport("rstrtmgr.dll", CharSet=CharSet.Unicode)] public static extern int RmRegisterResources(uint h,uint nFiles,string[] files,uint nApps,IntPtr apps,uint nSvcs,string[] svcs);
 [DllImport("rstrtmgr.dll")] public static extern int RmGetList(uint h,out uint needed,ref uint count,IntPtr info,ref uint reboot);
 [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] public struct RI { public uint pid; public long st; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=256)] public string name; public uint type; public uint status; public uint session; [MarshalAs(UnmanagedType.Bool)] public bool restartable; }
}
'@
$files = @('C:\Users\mongo\UnityProjects\147 VR\Assets\Scenes\147VR_MainScene.unity','C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\Prefab_WPBSA_12Foot_Snooker.prefab','C:\Users\mongo\UnityProjects\147 VR\Assets\AAA\ImportedSnooker\Prefabs\147VR_Table_WPBSA_Visual_Clean_v007_MARKING.prefab')
$h=0; $key=''; [RM]::RmStartSession([ref]$h,0,$key) | Out-Null
[RM]::RmRegisterResources($h,[uint32]$files.Count,$files,0,[IntPtr]::Zero,0,$null)|Out-Null
$need=0;$count=0;$reboot=0;[RM]::RmGetList($h,[ref]$need,[ref]$count,[IntPtr]::Zero,[ref]$reboot)|Out-Null
if($need -gt 0){$size=[Runtime.InteropServices.Marshal]::SizeOf([type][RM+RI]);$ptr=[Runtime.InteropServices.Marshal]::AllocHGlobal($size*$need);$count=$need;[RM]::RmGetList($h,[ref]$need,[ref]$count,$ptr,[ref]$reboot)|Out-Null;for($i=0;$i -lt $count;$i++){ $x=[Runtime.InteropServices.Marshal]::PtrToStructure([IntPtr]::Add($ptr,$i*$size),[type][RM+RI]);Write-Output ("LOCKER PID={0} NAME={1}" -f $x.pid,$x.name)};[Runtime.InteropServices.Marshal]::FreeHGlobal($ptr)} else {Write-Output 'NO_LOCKERS_REPORTED'}
[RM]::RmEndSession($h)|Out-Null
