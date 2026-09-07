param([string]$Source=(Join-Path $PSScriptRoot '../../client/Assets/Scripts/Resonance.App/Characters/PuppetLegIK.cs'))
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $Source)){throw 'FAIL: two-bone leg solver missing'}
Add-Type -TypeDefinition (Get-Content -LiteralPath $Source -Raw)
function Check($ok,$name){if(!$ok){throw "FAIL: $name"};Write-Output "PASS: $name"}
$maxError=0.0
for($i=0;$i -le 100;$i++){
 $p=[Resonance.App.PuppetLegIK]::Solve(0,0,-.5,-5.2+$i*.004,2.8,2.8,1)
 $a=[Math]::Sqrt($p.KneeX*$p.KneeX+$p.KneeY*$p.KneeY)
 $b=[Math]::Sqrt([Math]::Pow($p.AnkleX-$p.KneeX,2)+[Math]::Pow($p.AnkleY-$p.KneeY,2))
 $maxError=[Math]::Max($maxError,[Math]::Max([Math]::Abs($a-2.8),[Math]::Abs($b-2.8)))
}
Check ($maxError -lt 1e-8) 'both segment lengths remain constant throughout lift'
$p=[Resonance.App.PuppetLegIK]::Solve(0,0,0,-10,2,3,1)
Check ([Math]::Abs([Math]::Sqrt($p.AnkleX*$p.AnkleX+$p.AnkleY*$p.AnkleY)-5) -lt .00001) 'unreachable targets are clamped without stretching'
$p=[Resonance.App.PuppetLegIK]::Solve(0,0,0,0,2,3,1)
Check (![double]::IsNaN($p.KneeX) -and [Math]::Abs([Math]::Sqrt($p.AnkleX*$p.AnkleX+$p.AnkleY*$p.AnkleY)-1) -lt .00001) 'coincident target avoids singularity'
$a=[Resonance.App.PuppetLegIK]::Solve(0,0,0,-4,3,3,1)
$b=[Resonance.App.PuppetLegIK]::Solve(0,0,0,-4,3,3,-1)
Check ($a.KneeX -gt 0 -and $b.KneeX -lt 0) 'bend direction is explicit and stable'
