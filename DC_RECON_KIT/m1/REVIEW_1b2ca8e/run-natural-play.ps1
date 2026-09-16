# B1-A4 helper. Default is print-only (no Unity launch).
# After A2 binds A53_SUB_STRIP_DOT_FLAME in VerificationCatalog.Apply:
#   powershell -NoProfile -File .\run-natural-play.ps1 -Scenario np.basic.v1 -Record -Run
# No HP/Drive/Charge/Fever/time/outcome writes. EventSystem only. run7 stays FAIL.
# Does not hash leftover dist/windows or the 2026-08-28 Win64 exe.

param(
    [ValidateSet("np.basic.v1", "np.fever.v1", "np.auto.v1", "np.matrix.v1", "matrix")]
    [string]$Scenario = "np.basic.v1",
    [switch]$Record,
    [switch]$Run,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$Unity = "D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$Repo = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
if (-not (Test-Path (Join-Path $Repo "client\Assets"))) {
    $Repo = "F:\天命之子"
}
$Proj = Join-Path $Repo "client"
$Kit = $PSScriptRoot
$Stamp = [DateTime]::UtcNow.ToString("yyyyMMddTHHmmss")
$LogDir = Join-Path $Kit "logs"
$RecDir = Join-Path $Kit "recordings"
$ArtDir = Join-Path $Kit "artifacts\natural-play\$Stamp-$($Scenario.Replace('.', '_'))"
$Log = Join-Path $LogDir "editor-$($Scenario.Replace('.', '-'))-$Stamp.log"
$CatalogApply = Join-Path $Proj "Assets\Scripts\Resonance.Battle\Content\VerificationCatalog.cs"

$Cli = @{
    "np.basic.v1"  = "Resonance.EditorTools.NaturalPlaySmoke.CliBasic"
    "np.fever.v1"  = "Resonance.EditorTools.NaturalPlaySmoke.CliFever"
    "np.auto.v1"   = "Resonance.EditorTools.NaturalPlaySmoke.CliAuto"
    "np.matrix.v1" = "Resonance.EditorTools.NaturalPlaySmoke.CliMatrix"
    "matrix"       = "Resonance.EditorTools.NaturalPlaySmoke.CliMatrix"
}[$Scenario]

$cmd = @(
    $Unity
    "-projectPath", $Proj
    "-natural-play"
    "-natural-scenario", $Scenario
    "-executeMethod", $Cli
    "-logFile", $Log
)

Write-Host "B1-A4 helper  scenario=$Scenario  Run=$Run  Record=$Record"
Write-Host "Unity:  $Unity  exists=$([bool](Test-Path $Unity))"
Write-Host "HEAD:   $(git -C $Repo rev-parse HEAD)"
Write-Host "Command (no -batchmode / -nographics / -quit):"
Write-Host ("  " + ($cmd -join " "))
Write-Host "Result: $Proj\captures\natural-play.result.txt"
Write-Host "Battles: $Proj\captures\natural-play\battles\<id>\"
Write-Host "Archive: $ArtDir"
Write-Host "Pointer: UnityEngine.EventSystem  historical run7 stays FAIL"

$a2 = $false
if (Test-Path -LiteralPath $CatalogApply) {
    $a2 = [bool](Select-String -LiteralPath $CatalogApply -Pattern "StripDotFlame|A53_SUB_STRIP_DOT_FLAME" -Quiet)
}
Write-Host "A2 bind StripDotFlame in VerificationCatalog.cs: $a2"

if (-not $Run) {
    Write-Host "Print-only. After A2+A3 merge, re-invoke with -Run (and -Record)."
    exit 0
}

if (-not $a2 -and -not $Force) {
    Write-Error "BLOCKED: VerificationCatalog.cs has no StripDotFlame / A53_SUB_STRIP_DOT_FLAME bind. Do not open Editor."
}
if (-not (Test-Path $Unity)) {
    Write-Error "BLOCKED: Unity.exe missing at $Unity"
}
if (Test-Path (Join-Path $Proj "Library\EditorInstance.json")) {
    Write-Error "BLOCKED: EditorInstance.json present — another Editor owns client/"
}

New-Item -ItemType Directory -Force -Path $LogDir, $RecDir, $ArtDir | Out-Null

$ffPid = $null
$mp4 = Join-Path $RecDir "np-$($Scenario.Replace('.', '-'))-$Stamp.mp4"
if ($Record) {
    $ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if (-not $ffmpeg) {
        Write-Error "BLOCKED: -Record requested but ffmpeg not on PATH"
    }
    Write-Host "Recording (one continuous file) -> $mp4"
    $ff = Start-Process -FilePath $ffmpeg.Source -ArgumentList @(
        "-y", "-f", "gdigrab", "-framerate", "30", "-draw_mouse", "1", "-i", "desktop",
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-preset", "veryfast", $mp4
    ) -PassThru -WindowStyle Minimized
    $ffPid = $ff.Id
}

$result = Join-Path $Proj "captures\natural-play.result.txt"
$tempResult = Join-Path $Proj "Temp\natural-play.result.txt"
if (Test-Path $result) { Remove-Item $result -Force }
if (Test-Path $tempResult) { Remove-Item $tempResult -Force }

Write-Host "Launching Unity Play Mode (graphics required)..."
$unityProc = Start-Process -FilePath $Unity -ArgumentList @(
    "-projectPath", $Proj,
    "-natural-play",
    "-natural-scenario", $Scenario,
    "-executeMethod", $Cli,
    "-logFile", $Log
) -PassThru

$deadline = (Get-Date).AddMinutes(35)
while ((Get-Date) -lt $deadline) {
    if (Test-Path $result) { break }
    if (Test-Path $tempResult) { break }
    if ($unityProc.HasExited) { break }
    Start-Sleep -Seconds 2
}

if ($ffPid) {
    try { Stop-Process -Id $ffPid -ErrorAction SilentlyContinue } catch {}
}

$srcCaptures = Join-Path $Proj "captures"
if (Test-Path $srcCaptures) {
    Copy-Item -Recurse -Force (Join-Path $srcCaptures "natural-play*") $ArtDir
    Get-ChildItem $srcCaptures -Filter "np_*.png" -ErrorAction SilentlyContinue |
        Copy-Item -Destination $ArtDir -Force
}
Copy-Item -Force $Log $ArtDir -ErrorAction SilentlyContinue
if ($Record -and (Test-Path $mp4)) {
    Copy-Item -Force $mp4 $ArtDir
}

$exe = Join-Path $Proj "Builds\Win64\Resonance.exe"
$hashFile = Join-Path $ArtDir "BUILD_HASHES.txt"
"HEAD=$(git -C $Repo rev-parse HEAD)" | Set-Content $hashFile
"scenario=$Scenario stamp=$Stamp" | Add-Content $hashFile
$staleCutoff = Get-Date "2026-09-01"
if ((Test-Path $exe) -and ((Get-Item $exe).LastWriteTime -ge $staleCutoff)) {
    Get-FileHash -Algorithm SHA256 $exe | Format-List | Add-Content $hashFile
} elseif (Test-Path $exe) {
    "player_build=LEFTOVER_NOT_HASHED mtime=$((Get-Item $exe).LastWriteTime.ToString('o')) path=$exe" | Add-Content $hashFile
} else {
    "player_build=BLOCKED missing $exe — run WindowsBuild.BuildAndExit after A2" | Add-Content $hashFile
}

Write-Host "Unity exit=$(if ($unityProc.HasExited) { $unityProc.ExitCode } else { 'still-running' })"
Write-Host "Artifacts: $ArtDir"
if (-not (Test-Path $result) -and -not (Test-Path $tempResult)) {
    Write-Host "natural-play.result.txt missing — BLOCKED or still running. See $Log"
    exit 2
}
Get-Content -TotalCount 8 $(if (Test-Path $result) { $result } else { $tempResult })
