param([string]$Source = (Join-Path $PSScriptRoot '../../client/Assets/Scripts/Resonance.App/Characters/PuppetMotion.cs'))
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $Source)){throw 'FAIL: PuppetMotion does not exist'}
Add-Type -TypeDefinition (Get-Content -LiteralPath $Source -Raw)
function Assert($ok,$message){if(!$ok){throw ('FAIL: '+$message)}; Write-Output ('PASS: '+$message)}
$a=[Resonance.App.PuppetMotion]::new();$b=[Resonance.App.PuppetMotion]::new()
$a.TapChest(-1);$b.TapChest(-1)
for($i=0;$i -lt 120;$i++){$a.Step(1.0/30)}
for($i=0;$i -lt 480;$i++){$b.Step(1.0/120)}
Assert ([Math]::Abs($a.ChestLeft-$b.ChestLeft) -lt .000001 -and [Math]::Abs($a.HairLeftTip-$b.HairLeftTip) -lt .000001) 'secondary motion agrees at 30 and 120 FPS'
Assert ([Math]::Abs($a.ChestLeft) -lt .0056) 'chest impulse settles into idle band'
$a=[Resonance.App.PuppetMotion]::new();$a.TapChest(-1);$a.Step(.05)
Assert ([Math]::Abs($a.ChestLeft) -gt [Math]::Abs($a.ChestRight)) 'left and right chest respond independently'
$before=$a.ChestLeft;$a.TapChest(1)
Assert ($a.ChestLeft -eq $before) 'repeated input does not teleport current shape'
$bounded=$true
for($i=0;$i -lt 200;$i++){$a.TapChest(-1);$a.Step(1.0/120);if([Math]::Abs($a.ChestLeft) -gt .0061){$bounded=$false}}
Assert $bounded 'repeated impulses remain bounded'
$a=[Resonance.App.PuppetMotion]::new()
Assert ($a.StartSkill()) 'skill starts'
Assert (!$a.StartSkill()) 'skill does not restart mid-swing'
$max=0.0
for($i=0;$i -lt 300;$i++){$a.Step(1.0/120);$max=[Math]::Max($max,$a.Swing)}
Assert ($max -gt .95) 'skill completes its swing'
Assert (!$a.SkillActive -and [Math]::Abs($a.Swing) -lt .00001) 'skill returns exactly to rest'

$a=[Resonance.App.PuppetMotion]::new()
$minFast=1.0;$maxFast=-1.0
for($i=0;$i -lt 90;$i++){
    $a.Step(1.0/120)
    if($a.ChestLeft -lt $minFast){$minFast=$a.ChestLeft}
    if($a.ChestLeft -gt $maxFast){$maxFast=$a.ChestLeft}
}
Assert (($maxFast-$minFast) -lt 0.0028) 'idle is not a 1Hz water-drop metronome'
$minL=1.0;$maxL=-1.0;$maxSway=0.0
$a=[Resonance.App.PuppetMotion]::new()
for($i=0;$i -lt 720;$i++){
    $a.Step(1.0/120)
    if($a.ChestLeft -lt $minL){$minL=$a.ChestLeft}
    if($a.ChestLeft -gt $maxL){$maxL=$a.ChestLeft}
    if([Math]::Abs($a.BodySway) -gt $maxSway){$maxSway=[Math]::Abs($a.BodySway)}
}
Assert (($maxL-$minL) -gt 0.002) 'idle still has a slow breath/weight heave'
Assert ($a.ClaspAmount -eq 0) 'idle does not start clasp'
Assert ($maxSway -gt 0.2) 'idle body sway remains a modest driver'
$tap=[Resonance.App.PuppetMotion]::new();$tap.TapChest(-1)
$early=0.0;$late=0.0
for($i=0;$i -lt 180;$i++){
    $tap.Step(1.0/120)
    $d=[Math]::Abs($tap.ChestLeft)
    if($i -lt 36){if($d -gt $early){$early=$d}}
    if($i -ge 144){if($d -gt $late){$late=$d}}
}
Assert ($early -gt $late * 1.3) 'tap jiggle decays instead of running forever'
Assert ($a.StartClasp()) 'clasp API remains for phase B'
for($i=0;$i -lt 144;$i++){$a.Step(1.0/120)}
Assert ($a.ArmClosure -gt 0.98) 'clasp API still reaches hold'
$a=[Resonance.App.PuppetMotion]::new();$a.BlinkNow();$peak=0.0
for($i=0;$i -lt 60;$i++){$a.Step(1.0/120);$peak=[Math]::Max($peak,$a.Blink)}
Assert ($peak -gt .98 -and $a.Blink -eq 0) 'blink closes and reopens'
$a.Step([double]::NaN);$a.Step(-1);$a.Step(100)
Assert (![float]::IsNaN($a.ChestLeft) -and [Math]::Abs($a.HairLeftTip) -le 5) 'invalid time and app resume remain bounded'
$a=[Resonance.App.PuppetMotion]::new();$b=[Resonance.App.PuppetMotion]::new()
Assert ($a.StartLegLift()) 'leg lift starts'
Assert (!$a.StartLegLift()) 'repeated click does not restart the leg'
$b.StartLegLift() | Out-Null
for($i=0;$i -lt 48;$i++){$a.Step(1.0/30)}
for($i=0;$i -lt 192;$i++){$b.Step(1.0/120)}
Assert ($a.LegLift -gt .99 -and [Math]::Abs($a.LegLift-$b.LegLift) -lt .000001) 'leg reaches its raised pose consistently across frame rates'
for($i=0;$i -lt 90;$i++){$a.Step(1.0/30)}
Assert (!$a.LegActive -and $a.LegLift -eq 0) 'leg lift returns exactly to rest'
