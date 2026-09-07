param([string]$Source = (Join-Path $PSScriptRoot '../../client/Assets/Scripts/Resonance.App/Characters/PuppetMotion.cs'))
$ErrorActionPreference='Stop'
Add-Type -TypeDefinition (Get-Content -LiteralPath $Source -Raw)
function Check($ok,$message){if(!$ok){throw ('FAIL: '+$message)};Write-Output ('PASS: '+$message)}
$a=[Resonance.App.PuppetMotion]::new();$b=[Resonance.App.PuppetMotion]::new()
Check ($a.StartClasp()) 'clasp starts'
Check (!$a.StartClasp()) 'repeat input preserves current action'
$b.StartClasp() | Out-Null
$a.Step(.20);$b.Step(.20)
Check ($a.ArmClosure -gt $a.ClaspAmount) 'arms lead compression'
$peak=0.0;$bounded=$true;$maxDifference=0.0
for($i=0;$i -lt 150;$i++){
 $a.Step(1.0/30);for($j=0;$j -lt 4;$j++){$b.Step(1.0/120)}
 $maxDifference=[Math]::Max($maxDifference,[Math]::Abs($a.ClaspAmount-$b.ClaspAmount))
 $peak=[Math]::Max($peak,$a.ClaspAmount)
 if($a.ClaspAmount -lt 0 -or $a.ClaspAmount -gt 1 -or $a.ArmClosure -lt 0 -or $a.ArmClosure -gt 1){$bounded=$false}
}
Check ($maxDifference -lt .000001) 'clasp agrees at 30 and 120 FPS'
Check ($peak -gt .99 -and $bounded) 'compression reaches target without overshoot'
Check (!$a.ClaspActive -and $a.ClaspAmount -eq 0 -and $a.ArmClosure -eq 0) 'arms and clothing return exactly to rest'
Check ($a.StartClasp()) 'action can start again after settling'
