# GT_SOURCE_HANDOFF direct fetch — no keyword search
# Dest ROOT: this folder
$ErrorActionPreference = 'Continue'
$dest = Split-Path -Parent $MyInvocation.MyCommand.Path
$log = Join-Path $dest 'FETCH_LOG.txt'
function Log([string]$m) {
  $line = "$(Get-Date -Format o) $m"
  Add-Content -Path $log -Value $line
  Write-Host $line
}

New-Item -ItemType Directory -Force -Path $dest | Out-Null
Log "==== DIRECT_YT_FETCH start dest=$dest ===="

Get-Process yt-dlp -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

$proxy = 'http://127.0.0.1:7890'
$jobs = @(
  @{ id = 'aSbBuFD12HY'; name = 'aSbBuFD12HY_GL_EN_Gameplay_Part1_202308'; url = 'https://www.youtube.com/watch?v=aSbBuFD12HY' },
  @{ id = 'hgqXY5M9gFk'; name = 'hgqXY5M9gFk_GL_ND_Robin_Boss'; url = 'https://www.youtube.com/watch?v=hgqXY5M9gFk' },
  @{ id = 'IRDqNAhKKr4'; name = 'IRDqNAhKKr4_GL_EternalVow_NormalHard_480p'; url = 'https://www.youtube.com/watch?v=IRDqNAhKKr4' }
)

$fmt = 'bv*[height<=480]+ba/b[height<=480]/b'
$clients = @('android', 'tv_embedded', 'web')

foreach ($j in $jobs) {
  $outTpl = Join-Path $dest ($j.name + '.%(ext)s')
  $final = Join-Path $dest ($j.name + '.mp4')
  if ((Test-Path $final) -and ((Get-Item $final).Length -gt 1MB)) {
    Log "SKIP existing $($j.id) size=$((Get-Item $final).Length)"
    continue
  }

  $ok = $false
  foreach ($client in $clients) {
    Log "ATTEMPT $($j.id) client=$client proxy=$proxy"
    & yt-dlp --proxy $proxy --force-ipv4 --legacy-server-connect `
      --socket-timeout 30 --retries 3 `
      --extractor-args "youtube:player_client=$client" `
      -f $fmt --no-playlist -o $outTpl $j.url
    $code = $LASTEXITCODE
    Log "exit_$($j.id)_$client=$code"
    if ((Test-Path $final) -and ((Get-Item $final).Length -gt 1MB)) {
      Log "OK $($j.id) size=$((Get-Item $final).Length)"
      $ok = $true
      break
    }
  }
  if (-not $ok) { Log "FAIL $($j.id) — no mp4 >1MB" }
}

Log '==== ROOT mp4 ===='
Get-ChildItem $dest -Filter '*.mp4' -File | ForEach-Object { Log ("FILE " + $_.Name + " " + $_.Length) }
Log '==== DIRECT_YT_FETCH end ===='
