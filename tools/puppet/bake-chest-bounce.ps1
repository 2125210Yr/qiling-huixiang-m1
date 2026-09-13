$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$restPath = Join-Path $PSScriptRoot '../../art/characters/C001-焰刃/puppet-src/chest_rest.png'
$artDir = Join-Path $PSScriptRoot '../../art/characters/C001-焰刃/puppet-src'
$resPath = Join-Path $PSScriptRoot '../../client/Assets/Resources/Art/Characters/C001/PuppetMotion/chest_bounce.png'
$rest = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $restPath).Path)
$w = $rest.Width
$h = $rest.Height

function Smooth([double]$t) {
    if ($t -le 0) { return 0.0 }
    if ($t -ge 1) { return 1.0 }
    return $t * $t * (3.0 - 2.0 * $t)
}
function Sample([System.Drawing.Bitmap]$im, [double]$x, [double]$y) {
    $ww = $im.Width; $hh = $im.Height
    if ($x -lt 0) { $x = 0 }
    if ($y -lt 0) { $y = 0 }
    if ($x -gt $ww - 1.001) { $x = $ww - 1.001 }
    if ($y -gt $hh - 1.001) { $y = $hh - 1.001 }
    $x0 = [int][Math]::Floor($x); $y0 = [int][Math]::Floor($y)
    $x1 = [Math]::Min($x0 + 1, $ww - 1); $y1 = [Math]::Min($y0 + 1, $hh - 1)
    $fx = $x - $x0; $fy = $y - $y0
    $c00 = $im.GetPixel($x0, $y0); $c10 = $im.GetPixel($x1, $y0)
    $c01 = $im.GetPixel($x0, $y1); $c11 = $im.GetPixel($x1, $y1)
    $a = [int]((1 - $fy) * ((1 - $fx) * $c00.A + $fx * $c10.A) + $fy * ((1 - $fx) * $c01.A + $fx * $c11.A))
    $r = [int]((1 - $fy) * ((1 - $fx) * $c00.R + $fx * $c10.R) + $fy * ((1 - $fx) * $c01.R + $fx * $c11.R))
    $g = [int]((1 - $fy) * ((1 - $fx) * $c00.G + $fx * $c10.G) + $fy * ((1 - $fx) * $c01.G + $fx * $c11.G))
    $b = [int]((1 - $fy) * ((1 - $fx) * $c00.B + $fx * $c10.B) + $fy * ((1 - $fx) * $c01.B + $fx * $c11.B))
    return [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
}
function EllipseW([double]$x, [double]$y, [double]$cx, [double]$cy, [double]$rx, [double]$ry) {
    $nx = ($x - $cx) / $rx; $ny = ($y - $cy) / $ry
    $rad = [Math]::Sqrt($nx * $nx + $ny * $ny)
    if ($rad -ge 1) { return 0.0 }
    if ($rad -le 0.18) { return 1.0 }
    return 1.0 - (Smooth (($rad - 0.18) / 0.82))
}
function PinAt([double]$x, [double]$y) {
    $p = 0.0
    if ($y -le 24) { $p = [Math]::Max($p, (1.0 - (Smooth ($y / 24.0)))) }
    if ($y -ge 138) { $p = [Math]::Max($p, (Smooth (($y - 138) / 16.0))) }
    if ($x -le 20) { $p = [Math]::Max($p, (1.0 - (Smooth ($x / 20.0)))) }
    if ($x -ge 218) { $p = [Math]::Max($p, (Smooth (($x - 218) / 16.0))) }
    $vHalf = 12.0 + 10.0 * [Math]::Max(0.0, ($y - 50.0) / 90.0)
    $vd = [Math]::Abs($x - 118.0)
    if ($y -gt 42 -and $y -lt 145 -and $vd -lt ($vHalf + 8)) {
        $inner = [Math]::Max(0.0, $vHalf - 6.0)
        $t = 1.0 - (Smooth ([Math]::Max(0.0, ($vd - $inner)) / 14.0))
        if ($y -lt 58) { $t = $t * (Smooth (($y - 42) / 16.0)) }
        if ($y -gt 130) { $t = $t * (1.0 - (Smooth (($y - 130) / 15.0))) }
        $p = [Math]::Max($p, [double]$t)
    }
    if ($p -gt 1) { $p = 1 }
    return $p
}

$out = New-Object System.Drawing.Bitmap $w, $h
$diff = New-Object System.Drawing.Bitmap $w, $h
$maxD = 0
for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        $pin = PinAt $x $y
        $wL = EllipseW $x $y 64 80 46 52
        $wR = EllipseW $x $y 168 76 58 54
        $dx = ($wL * 3.6 + $wR * -4.2) * (1.0 - $pin)
        $dy = (($wL + $wR) * -3.4) * (1.0 - $pin)
        if ($pin -ge 0.995) {
            $c = $rest.GetPixel($x, $y)
        }
        else {
            $c = Sample $rest ($x - $dx) ($y - $dy)
            if ($pin -gt 0.02) {
                $o = $rest.GetPixel($x, $y)
                $c = [System.Drawing.Color]::FromArgb(
                    [int]($c.A + ($o.A - $c.A) * $pin),
                    [int]($c.R + ($o.R - $c.R) * $pin),
                    [int]($c.G + ($o.G - $c.G) * $pin),
                    [int]($c.B + ($o.B - $c.B) * $pin))
            }
        }
        $out.SetPixel($x, $y, $c)
        $o2 = $rest.GetPixel($x, $y)
        $d = [Math]::Max([Math]::Abs($c.R - $o2.R), [Math]::Max([Math]::Abs($c.G - $o2.G), [Math]::Abs($c.B - $o2.B)))
        if ($d -gt $maxD) { $maxD = $d }
        $v = [Math]::Min(255, $d * 8)
        $diff.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $v, $v, $v))
    }
}
$bounce = Join-Path $artDir 'chest_bounce.png'
$out.Save($bounce, [System.Drawing.Imaging.ImageFormat]::Png)
$out.Save((Join-Path $PSScriptRoot '../../client/Assets/Resources/Art/Characters/C001/PuppetMotion/chest_bounce.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$diff.Save((Join-Path $artDir 'chest_bounce_diff.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$out.Dispose(); $diff.Dispose(); $rest.Dispose()
Write-Output ("PASS baked registered bounce maxDelta=$maxD")
