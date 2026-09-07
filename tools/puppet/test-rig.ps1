param([string]$Source = (Join-Path $PSScriptRoot '../../client/Assets/Scripts/Resonance.App/Characters/PuppetRig.cs'))
$ErrorActionPreference = 'Stop'
[System.Reflection.Assembly]::LoadFrom('D:\Unity\Hub\Editor\6000.3.23f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll') | Out-Null
$s = Get-Content -LiteralPath $Source -Raw
$methods = @()
foreach ($name in @('Falloff','SoftEllipse','SoftBox','PackWeights','AdvanceSpring')) {
    $m = [regex]::Match($s, '        static [^\r\n]+ ' + $name + '\(')
    if (!$m.Success) { continue }
    $start = $s.IndexOf('{', $m.Index); $depth = 1; $end = $start + 1
    while ($depth -gt 0) { if ($s[$end] -eq '{') {$depth++}; if ($s[$end] -eq '}') {$depth--}; $end++ }
    $methods += $s.Substring($m.Index,$end-$m.Index).Replace('        static ','        public static ')
}
$header = 'using System; using UnityEngine; public static class RigProbe { const int BoneRoot=0, BoneChest=1, BoneWrist=2, BoneAnkleR=3, BoneAnkleL=4, BoneHead=5;'
$code = $header + ($methods -join "`n") + "`n}"
Add-Type -TypeDefinition $code -ReferencedAssemblies @('D:\Unity\Hub\Editor\6000.3.23f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll', (Join-Path $PSHOME 'ref/netstandard.dll'), (Join-Path $PSHOME 'ref/System.Runtime.dll'))
$failures = [System.Collections.Generic.List[string]]::new()
function Check($ok,$name) { if (!$ok) { $failures.Add($name); Write-Output "FAIL: $name" } else { Write-Output "PASS: $name" } }
$rangeOK=$true; $last=1.0; $monotone=$true
for($i=0;$i -le 200;$i++) {
    $w=[RigProbe]::SoftEllipse([float]($i/100.0*.05),0,0,0,.05,.048,.014)
    if($w -lt 0 -or $w -gt 1){$rangeOK=$false}
    if($w -gt $last+0.00001){$monotone=$false}; $last=$w
}
Check $rangeOK 'ellipse weights stay in [0,1]'
Check $monotone 'ellipse feather decreases monotonically'
$a=[RigProbe]::SoftEllipse(.049999,0,0,0,.05,.048,.014)
$b=[RigProbe]::SoftEllipse(.050001,0,0,0,.05,.048,.014)
Check ([Math]::Abs($a-$b) -lt .001) 'ellipse inner boundary is continuous'
$a=[RigProbe]::Falloff([UnityEngine.Vector3]::new(.21999,0,0),[UnityEngine.Vector3]::zero,1,1)
$b=[RigProbe]::Falloff([UnityEngine.Vector3]::new(.22001,0,0),[UnityEngine.Vector3]::zero,1,1)
Check ([Math]::Abs($a-$b) -lt .001) 'joint falloff inner boundary is continuous'
$a=[RigProbe]::SoftBox(.39999,.5,.4,.6,.4,.6,.05)
$b=[RigProbe]::SoftBox(.40001,.5,.4,.6,.4,.6,.05)
Check ([Math]::Abs($a-$b) -lt .001) 'box feather inner boundary is continuous'
$bw=[RigProbe]::PackWeights(1,-.3,0,0,0,0)
Check ($bw.weight0 -ge 0 -and $bw.weight1 -ge 0 -and $bw.weight2 -ge 0 -and $bw.weight3 -ge 0 -and [Math]::Abs($bw.weight0+$bw.weight1+$bw.weight2+$bw.weight3-1) -lt .00001) 'packed weights are nonnegative and normalized'
$bw=[RigProbe]::PackWeights(-1,-.3,-.2,-.4,-.8,-.5)
Check ($bw.weight0 -eq 1 -and $bw.boneIndex0 -eq 0) 'invalid negative influences fall back to the root'
$bw=[RigProbe]::PackWeights([float]::NaN,[float]::PositiveInfinity,0,0,0,0)
Check ($bw.weight0 -eq 1 -and $bw.boneIndex0 -eq 0) 'nonfinite influences fall back to the root'
if ([RigProbe].GetMethod('AdvanceSpring')) {
    [float]$p30=0; [float]$v30=6; [float]$p120=0; [float]$v120=6
    for($i=0;$i -lt 30;$i++){[RigProbe]::AdvanceSpring([ref]$p30,[ref]$v30,(1.0/30))}
    for($i=0;$i -lt 120;$i++){[RigProbe]::AdvanceSpring([ref]$p120,[ref]$v120,(1.0/120))}
    Check ([Math]::Abs($p30-$p120) -lt .00001) 'tap motion agrees at 30 and 120 FPS'
    for($i=0;$i -lt 240;$i++){[RigProbe]::AdvanceSpring([ref]$p120,[ref]$v120,(1.0/120))}
    Check ([Math]::Abs($p120) -lt .0001 -and [Math]::Abs($v120) -lt .001) 'tap settles without a forced cutoff'
} else { Check $false 'tap spring exists instead of restarting a cosine at peak' }
if($failures.Count){throw "$($failures.Count) regression checks failed"}
