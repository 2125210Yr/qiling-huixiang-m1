Add-Type -AssemblyName System.Drawing
$dir = Split-Path -Parent $PSScriptRoot
$bmp = New-Object System.Drawing.Bitmap "$dir\reference.jpg"
$step = 4
$sb = New-Object System.Text.StringBuilder

# header rows: tens and units of x
$h1 = New-Object System.Text.StringBuilder
$h2 = New-Object System.Text.StringBuilder
[void]$h1.Append('     ')
[void]$h2.Append('     ')
for ($x = 0; $x -lt $bmp.Width; $x += $step) {
  [void]$h1.Append([string]([math]::Floor($x / 100) % 10))
  [void]$h2.Append([string]([math]::Floor($x / 10) % 10))
}
[void]$sb.AppendLine($h1.ToString())
[void]$sb.AppendLine($h2.ToString())

for ($y = 0; $y -lt $bmp.Height; $y += $step) {
  $line = New-Object System.Text.StringBuilder
  [void]$line.Append(("{0,4}|" -f $y))
  for ($x = 0; $x -lt $bmp.Width; $x += $step) {
    $p = $bmp.GetPixel($x, $y)
    $r = [int]$p.R; $g = [int]$p.G; $b = [int]$p.B
    $lum = (0.3 * $r + 0.6 * $g + 0.1 * $b)
    $c = '.'
    if ($r -gt 150 -and $g -gt 118) { $c = '#' }          # skin / bright
    elseif ($r -gt 110 -and $g -lt 75) { $c = 'R' }        # bright red dress
    elseif ($lum -lt 55 -and $b -ge ($r - 14)) { $c = 'H' } # dark blue/purple hair
    elseif ($r -gt 70 -and $g -lt 55) { $c = 'r' }         # mid red
    elseif ($lum -lt 30) { $c = ' ' }                      # near black
    [void]$line.Append($c)
  }
  [void]$sb.AppendLine($line.ToString())
}
$bmp.Dispose()
Set-Content -Path "$dir\_tools\map.txt" -Value $sb.ToString() -Encoding UTF8
"done"
