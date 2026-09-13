$dir = Join-Path $PSScriptRoot '..\正文'
$under = @(); $ok = 0; $over = 0; $total = 0; $n = 0; $missing = @()
$have = @{}
Get-ChildItem $dir -Filter '*.md' | ForEach-Object {
  $t = Get-Content $_.FullName -Raw -Encoding UTF8
  $chars = ([regex]::Matches($t, '[\u4e00-\u9fff]')).Count
  $num = 0
  if ($_.BaseName -match '(\d+)') { $num = [int]$Matches[1] }
  $have[$num] = $chars
  $n++; $total += $chars
  if ($chars -lt 2200) { $under += [pscustomobject]@{ch=$num; han=$chars} }
  elseif ($chars -gt 3500) { $over++ } else { $ok++ }
}
1..500 | ForEach-Object { if (-not $have.ContainsKey($_)) { $missing += $_ } }
Write-Output "files=$n missing=$($missing.Count) han_total=$total avg=$([int]($total / [math]::Max($n,1))) under2200=$($under.Count) inRange=$ok over3500=$over"
if ($missing.Count) { Write-Output ("missing_sample=" + (($missing | Select-Object -First 40) -join ',')) }
$under | Sort-Object ch | Select-Object -First 40 | ForEach-Object { "short ch$($_.ch)=$($_.han)" }
