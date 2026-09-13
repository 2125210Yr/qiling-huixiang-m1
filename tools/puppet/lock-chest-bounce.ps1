$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
$restPath = Join-Path $root 'art/characters/C001-焰刃/puppet-src/chest_rest.png'
$genPath = Join-Path $root 'art/characters/C001-焰刃/puppet-src/chest_bounce_raw.jpg'
$artBounce = Join-Path $root 'art/characters/C001-焰刃/puppet-src/chest_bounce.png'
$resBounce = Join-Path $root 'client/Assets/Resources/Art/Characters/C001/PuppetMotion/chest_bounce.png'

function Smooth([double]$t) {
    if ($t -le 0) { return 0.0 }
    if ($t -ge 1) { return 1.0 }
    return $t * $t * (3.0 - 2.0 * $t)
}
function EllipseW([double]$x, [double]$y, [double]$cx, [double]$cy, [double]$rx, [double]$ry) {
    $nx = ($x - $cx) / $rx
    $ny = ($y - $cy) / $ry
    $r = [Math]::Sqrt($nx * $nx + $ny * $ny)
    if ($r -ge 1) { return 0.0 }
    if ($r -le 0.2) { return 1.0 }
    return 1.0 - (Smooth (($r - 0.2) / 0.8))
}

$rest = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $restPath).Path)
$genSrc = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $genPath).Path)
$w = $rest.Width
$h = $rest.Height
$gen = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($gen)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($genSrc, 0, 0, $w, $h)
$g.Dispose()
$genSrc.Dispose()

$out = New-Object System.Drawing.Bitmap $w, $h
for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        $r = $rest.GetPixel($x, $y)
        $n = $gen.GetPixel($x, $y)
        $pin = 0.0
        if ($y -le 26) { $pin = [Math]::Max($pin, (1.0 - (Smooth ($y / 26.0)))) }
        if ($y -ge 136) { $pin = [Math]::Max($pin, (Smooth (($y - 136) / 18.0))) }
        if ($x -le 22) { $pin = [Math]::Max($pin, (1.0 - (Smooth ($x / 22.0)))) }
        if ($x -ge 214) { $pin = [Math]::Max($pin, (Smooth (($x - 214) / 20.0))) }
        $vd = [Math]::Abs($x - 118.0)
        $vHalf = 14.0 + 8.0 * [Math]::Max(0.0, ($y - 48.0) / 90.0)
        if ($y -gt 40 -and $y -lt 148 -and $vd -lt ($vHalf + 6)) {
            $t = 1.0 - (Smooth ([Math]::Max(0.0, ($vd - ($vHalf - 7))) / 13.0))
            $pin = [Math]::Max($pin, $t)
        }
        if ($r.R -lt 55 -and $r.G -lt 55 -and $r.B -lt 55) { $pin = 1.0 }
        $globe = [Math]::Max((EllipseW $x $y 64 80 48 54), (EllipseW $x $y 168 76 60 56))
        $useGen = $globe * (1.0 - $pin)
        $c = [System.Drawing.Color]::FromArgb(
            [int]($r.A + ($n.A - $r.A) * $useGen),
            [int]($r.R + ($n.R - $r.R) * $useGen),
            [int]($r.G + ($n.G - $r.G) * $useGen),
            [int]($r.B + ($n.B - $r.B) * $useGen))
        $out.SetPixel($x, $y, $c)
    }
}
$out.Save($artBounce, [System.Drawing.Imaging.ImageFormat]::Png)
$out.Save($resBounce, [System.Drawing.Imaging.ImageFormat]::Png)
$out.Dispose(); $gen.Dispose(); $rest.Dispose()
Write-Output 'PASS locked generated bounce onto rest seams'
