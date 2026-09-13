Add-Type -AssemblyName System.Drawing
$dir = Split-Path -Parent $PSScriptRoot
$bmp = New-Object System.Drawing.Bitmap "$dir\reference.jpg"
$x0 = 120; $x1 = 392; $step = 2
$sb = New-Object System.Text.StringBuilder

$h1 = New-Object System.Text.StringBuilder; [void]$h1.Append('     ')
$h2 = New-Object System.Text.StringBuilder; [void]$h2.Append('     ')
for ($x = $x0; $x -lt $x1; $x += $step) {
  if ($x % 20 -eq 0) { [void]$h1.Append([string]([math]::Floor($x / 100) % 10)); [void]$h2.Append([string]([math]::Floor($x / 10) % 10)) }
  else { [void]$h1.Append(' '); [void]$h2.Append(' ') }
}
[void]$sb.AppendLine($h1.ToString())
[void]$sb.AppendLine($h2.ToString())

for ($y = 0; $y -lt $bmp.Height; $y += $step) {
  $line = New-Object System.Text.StringBuilder
  [void]$line.Append(("{0,4}|" -f $y))
  for ($x = $x0; $x -lt $x1; $x += $step) {
    $p = $bmp.GetPixel($x, $y)
    $r = [int]$p.R; $g = [int]$p.G; $b = [int]$p.B
    $lum = (0.3 * $r + 0.6 * $g + 0.1 * $b)
    $c = '.'
    if ($r -gt 145 -and $g -gt 112) { $c = '#' }                   # skin
    elseif ($r -gt 110 -and $g -gt 78 -and $b -gt 90) { $c = '+' } # dim skin / shadowed skin
    elseif ($lum -lt 62 -and $b -ge ($r - 12)) { $c = 'H' }        # hair (dark, blue/violet)
    elseif ($r -gt 120 -and $g -lt 70) { $c = 'R' }                # saturated red
    elseif ($r -gt 60 -and $g -lt 55) { $c = 'r' }                 # dim red
    elseif ($lum -lt 34) { $c = ' ' }                              # black
    [void]$line.Append($c)
  }
  [void]$sb.AppendLine($line.ToString())
}
$bmp.Dispose()
Set-Content -Path "$dir\_tools\map2.txt" -Value $sb.ToString() -Encoding UTF8
"done"
