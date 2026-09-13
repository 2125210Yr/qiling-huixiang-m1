Add-Type -AssemblyName System.Drawing
$dir = Split-Path -Parent $PSScriptRoot
$src = [System.Drawing.Image]::FromFile("$dir\reference.jpg")

function Make-Grid {
  param($sx, $sy, $sw, $sh, $scale, $step, $out)
  $W = [int]($sw * $scale); $H = [int]($sh * $scale)
  $bmp = New-Object System.Drawing.Bitmap $W, $H
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.InterpolationMode = 'HighQualityBicubic'
  $g.DrawImage($src, (New-Object System.Drawing.Rectangle 0,0,$W,$H), (New-Object System.Drawing.Rectangle $sx,$sy,$sw,$sh), 'Pixel')
  $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(120,0,255,120)), 1
  $pen2 = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220,255,255,0)), 1
  $font = New-Object System.Drawing.Font 'Consolas', 9
  $br = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::Yellow)
  for ($x = $sx; $x -le $sx + $sw; $x += $step) {
    $px = ($x - $sx) * $scale
    $p = if ($x % ($step * 5) -eq 0) { $pen2 } else { $pen }
    $g.DrawLine($p, $px, 0, $px, $H)
    if ($x % ($step * 5) -eq 0) { $g.DrawString("$x", $font, $br, $px + 1, 1) }
  }
  for ($y = $sy; $y -le $sy + $sh; $y += $step) {
    $py = ($y - $sy) * $scale
    $p = if ($y % ($step * 5) -eq 0) { $pen2 } else { $pen }
    $g.DrawLine($p, 0, $py, $W, $py)
    if ($y % ($step * 5) -eq 0) { $g.DrawString("$y", $font, $br, 1, $py + 1) }
  }
  $bmp.Save("$dir\_tools\$out", [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  "wrote $out ($W x $H)"
}

Make-Grid 190 40 90 100 8.0 5 'grid_arm.png'
Make-Grid 90 110 130 110 7.0 5 'grid_leg.png'
Make-Grid 170 110 130 182 5.0 5 'grid_lower.png'
$src.Dispose()
