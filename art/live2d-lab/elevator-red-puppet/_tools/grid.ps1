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
  $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(150,0,255,120)), 1
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

Make-Grid 0 0 546 292 2.4 20 'grid_full.png'
Make-Grid 110 10 220 282 4.2 10 'grid_char.png'
Make-Grid 170 20 90 90 9.0 5 'grid_head.png'
$src.Dispose()
