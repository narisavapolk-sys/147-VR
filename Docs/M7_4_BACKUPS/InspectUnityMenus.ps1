Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root=[System.Windows.Automation.AutomationElement]::RootElement
$wins=$root.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)
foreach($w in $wins){if($w.Current.Name -like '147 VR - *Unity*'){
 $kids=$w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
 foreach($k in $kids){if($k.Current.ControlType.ProgrammaticName -match 'MenuItem' -and $k.Current.Name){Write-Output ("MENU [{0}]" -f $k.Current.Name)}}
}}
