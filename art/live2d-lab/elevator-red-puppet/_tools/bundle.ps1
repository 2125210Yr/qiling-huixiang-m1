# Inlines assets/*.png + reference.jpg + src/app.js into a single index.html,
# so the page works straight off the filesystem (no fetch, no CORS, no server).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'

$map = [ordered]@{}
function AddAsset([string]$name, [string]$path, [string]$mime) {
  if (-not (Test-Path $path)) { Write-Warning "missing $path"; return }
  $b64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($path))
  $map[$name] = "data:$mime;base64,$b64"
  "  {0,-11} {1,8:N0} KB" -f $name, (([IO.FileInfo]$path).Length / 1KB)
}

AddAsset 'bg' (Join-Path $assets 'bg.jpg') 'image/jpeg'
foreach ($n in 'hair_back', 'head', 'eyes', 'body', 'dress_hem', 'arm', 'leg_raised', 'leg_stand') {
  AddAsset $n (Join-Path $assets "$n.png") 'image/png'
}
AddAsset 'reference' (Join-Path $root 'reference.jpg') 'image/jpeg'

$sb = New-Object System.Text.StringBuilder
[void]$sb.Append('{')
$first = $true
foreach ($k in $map.Keys) {
  if (-not $first) { [void]$sb.Append(',') }
  [void]$sb.Append("`n""$k"":""$($map[$k])""")
  $first = $false
}
[void]$sb.Append("`n}")

$tpl = Get-Content (Join-Path $root 'src\index.template.html') -Raw -Encoding UTF8
$app = Get-Content (Join-Path $root 'src\app.js') -Raw -Encoding UTF8

$pattern = '/\*__ASSETS__\*/\{\}/\*__ASSETS_END__\*/'
$out = [Text.RegularExpressions.Regex]::Replace($tpl, $pattern, { param($m) $sb.ToString() })
$out = $out.Replace('/*__APP__*/', $app)

$dst = Join-Path $root 'index.html'
[IO.File]::WriteAllText($dst, $out, (New-Object Text.UTF8Encoding $false))
"index.html -> {0:N0} KB" -f (([IO.FileInfo]$dst).Length / 1KB)
