Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Win32 { [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd,uint Msg,IntPtr wParam,IntPtr lParam); }
'@
$h=[IntPtr]525402
[Win32]::PostMessage($h,0x0010,[IntPtr]0,[IntPtr]0)|Out-Null
