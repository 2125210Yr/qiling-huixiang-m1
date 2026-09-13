param([string]$Source = (Join-Path $PSScriptRoot '../../client/Assets/Scripts/Resonance.App/Characters/PuppetSoftMath.cs'))
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Source)) { throw 'FAIL: PuppetSoftMath does not exist' }
Add-Type -TypeDefinition (Get-Content -LiteralPath $Source -Raw)
function Check($ok, $name) { if (!$ok) { throw ('FAIL: ' + $name) }; Write-Output ('PASS: ' + $name) }

$c = 0.0; $s = 0.0
[Resonance.App.PuppetSoftMath]::Polar2(1, 0, 0, 1, [ref]$c, [ref]$s)
Check ([Math]::Abs($c - 1) -lt 1e-9 -and [Math]::Abs($s) -lt 1e-9) 'identity polar is no rotation'

[Resonance.App.PuppetSoftMath]::Polar2(0, -1, 1, 0, [ref]$c, [ref]$s)
Check ([Math]::Abs($c) -lt 1e-9 -and [Math]::Abs($s - 1) -lt 1e-9) '90-degree polar extracts the rotation'

$px = [double[]]@(1, -1, -1, 1)
$py = [double[]]@(1, 1, -1, -1)
$relX = [double[]]@(1, -1, -1, 1)
$relY = [double[]]@(1, 1, -1, -1)
$mass = [double[]]@(1, 1, 1, 1)
$rad = [Math]::PI / 6
$cs = [Math]::Cos($rad); $sn = [Math]::Sin($rad)
for ($i = 0; $i -lt 4; $i++) {
    $x = $relX[$i]; $y = $relY[$i]
    $px[$i] = 3 + $cs * $x - $sn * $y
    $py[$i] = -2 + $sn * $x + $cs * $y
}
$px[0] += 0.4
[Resonance.App.PuppetSoftMath]::Match($px, $py, $relX, $relY, $mass, 4, 3, -2, 1, 1)
$area = 0.0
for ($i = 0; $i -lt 4; $i++) {
    $j = ($i + 1) % 4
    $area += $px[$i] * $py[$j] - $px[$j] * $py[$i]
}
Check ([Math]::Abs([Math]::Abs($area) - 8) -lt 0.0001) 'shape match restores rigid cluster area'
$cx = 0.0; $cy = 0.0
[Resonance.App.PuppetSoftMath]::Com($px, $py, $mass, 4, [ref]$cx, [ref]$cy)
Check ([Math]::Abs($cx - 3) -lt 0.0001 -and [Math]::Abs($cy + 2) -lt 0.0001) 'shape match puts the cluster on the goal center'

$px = [double[]]@(0, 0)
$py = [double[]]@(0, 0)
$prevX = [double[]]@(0, 0)
$prevY = [double[]]@(0, 0)
[Resonance.App.PuppetSoftMath]::Verlet($px, $py, $prevX, $prevY, 2, 1, 0, 2, 0.1)
[Resonance.App.PuppetSoftMath]::Verlet($px, $py, $prevX, $prevY, 2, 1, 0, 2, 0.1)
Check ($py[0] -gt 0.02 -and $py[0] -eq $py[1]) 'verlet integrates both particles equally'
