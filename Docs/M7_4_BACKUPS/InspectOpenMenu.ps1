Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root=[System.Windows.Automation.AutomationElement]::RootElement
$els=$root.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
foreach($e in $els){if($e.Current.ControlType.ProgrammaticName -match 'MenuItem' -and $e.Current.Name){$r=$e.Current.BoundingRectangle;if($r.Width -gt 0){Write-Output ("[{0}] rect={1}" -f $e.Current.Name,$r)}}}
