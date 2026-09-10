Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root=[System.Windows.Automation.AutomationElement]::RootElement
$cond=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'147 VR - SampleScene - Android - Unity 6.4 (6000.4.4f1) <DX11>')
$win=$root.FindFirst([System.Windows.Automation.TreeScope]::Children,$cond)
if($win){$kids=$win.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition);foreach($e in $kids){if($e.Current.Name -in @('File','Edit','Assets','GameObject','Component','Window','Help','Tools')){Write-Output ($e.Current.Name+' '+$e.Current.BoundingRectangle)}}}
