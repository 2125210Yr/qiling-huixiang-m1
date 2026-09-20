param(
    [int]$TimeoutSeconds = 900,
    [ValidateSet('np.basic.v1','np.fever.v1','np.auto.v1')]
    [string]$Scenario = 'np.basic.v1',
    [switch]$ShowWindow
)

$ErrorActionPreference = 'Stop'
$repoPath = 'F:\天命之子'
$projectPath = 'F:\Resonance\client'
$unityPath = 'D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmss')
$runPath = Join-Path $PSScriptRoot ("artifacts\natural-play\$stamp-" + $Scenario.Replace('.', '_'))
$backupPath = Join-Path $runPath 'pre-run-captures'
$capturesPath = Join-Path $projectPath 'captures'
$tempPath = Join-Path $projectPath 'Temp'
$resultPath = Join-Path $tempPath 'natural-play.result.txt'
$requestPath = Join-Path $tempPath 'natural-play.request'
$editorLog = Join-Path $runPath 'editor.log'
$videoPath = Join-Path $runPath 'basic-focus.mp4'
$ffmpegPath = (Get-Command ffmpeg -ErrorAction Stop).Source

if (-not $ShowWindow) { throw 'Window recording requires -ShowWindow after explicit permission; hidden Unity windows produce blank captures.' }
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'An existing Unity Editor owns the workspace.' }
if (Test-Path -LiteralPath (Join-Path $projectPath 'Library\EditorInstance.json')) { throw 'EditorInstance.json exists.' }
if (Test-Path -LiteralPath $requestPath) { throw 'An existing natural-play request must be inspected first.' }
if (Test-Path -LiteralPath (Join-Path $tempPath 'natural-play.running')) { throw 'An existing natural-play run must be inspected first.' }
New-Item -ItemType Directory -Force -Path $runPath, $backupPath, $tempPath | Out-Null
$head = (& git -c safe.directory=F:/天命之子 -C $repoPath rev-parse HEAD).Trim()
$sourcePaths = @(
    'Assets\Scripts\Resonance.Battle\Core\BattleReplay.cs',
    'Assets\Scripts\Resonance.App\Debug\NaturalPlayRuntime.cs',
    'Assets\Scripts\Resonance.App\Debug\NaturalPlayBattleEvidence.cs'
)
$sourceHashes = foreach ($relative in $sourcePaths) { Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $projectPath $relative) }
$sourceHashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $runPath 'source-hashes.json') -Encoding utf8

# Preserve overwritten capture summaries/screenshots; all per-battle folders are unique.
$originalFiles = @(Get-ChildItem -LiteralPath $capturesPath -File | Where-Object { $_.Name -like 'np_*.png' -or $_.Name -in @('natural-play.result.txt','natural-play.events.txt') })
foreach ($item in $originalFiles) { Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $backupPath $item.Name) }
$oldIndex = Join-Path $capturesPath 'natural-play\BATTLE_INDEX.txt'
if (Test-Path -LiteralPath $oldIndex) { Copy-Item -LiteralPath $oldIndex -Destination (Join-Path $backupPath 'BATTLE_INDEX.txt') }
if (Test-Path -LiteralPath $resultPath) { Copy-Item -LiteralPath $resultPath -Destination (Join-Path $runPath 'pre-run-temp-result.txt'); Remove-Item -LiteralPath $resultPath }

$unityProcess = $null
$recorder = $null
$startedAt = Get-Date
$recordedAt = $null
$recorderErrorTask = $null
try {
    # Compile/load first, then arm the existing EventSystem scenario after recording begins.
    $windowStyle = if ($ShowWindow) { 'Normal' } else { 'Hidden' }
    $unityProcess = Start-Process -FilePath $unityPath -WindowStyle $windowStyle -PassThru -ArgumentList @('-projectPath', $projectPath, '-logFile', $editorLog)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $window = $null
    while ((Get-Date) -lt $deadline) {
        $window = Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 -and $_.MainWindowTitle -match 'client' } | Select-Object -First 1
        if ($window) { break }
        if ($unityProcess.HasExited) { throw 'Unity exited before opening the project window.' }
        Start-Sleep -Seconds 2
    }
    if (-not $window) { throw 'No project window before the deadline; inspect editor.log.' }
    $recordInfo = [Diagnostics.ProcessStartInfo]::new()
    $recordInfo.FileName = $ffmpegPath
    $recordInfo.UseShellExecute = $false
    $recordInfo.CreateNoWindow = $true
    $recordInfo.RedirectStandardInput = $true
    $recordInfo.RedirectStandardError = $true
    foreach ($arg in @('-hide_banner','-n','-f','gdigrab','-framerate','30','-draw_mouse','0','-i',('title='+$window.MainWindowTitle),'-vf','scale=trunc(iw/2)*2:trunc(ih/2)*2','-c:v','libx264','-preset','veryfast','-pix_fmt','yuv420p','-movflags','+frag_keyframe+empty_moov',$videoPath)) { $recordInfo.ArgumentList.Add($arg) }
    $recorder = [Diagnostics.Process]::Start($recordInfo)
    $recorderErrorTask = $recorder.StandardError.ReadToEndAsync()
    $recordedAt = Get-Date
    Start-Sleep -Seconds 2
    if ($recorder.HasExited) { throw 'Recorder exited before the scenario was armed.' }
    [System.IO.File]::WriteAllText($requestPath, $Scenario)
    while ((Get-Date) -lt $deadline) {
        if ($unityProcess.HasExited) { break }
        Start-Sleep -Seconds 2
    }
    if (-not $unityProcess.HasExited) { throw 'Unity deadline exceeded; no result is claimed.' }
    if (-not (Test-Path -LiteralPath $resultPath)) { throw 'Unity exited without a natural-play result.' }
    $resultText = Get-Content -LiteralPath $resultPath -Raw
    Copy-Item -LiteralPath $resultPath -Destination (Join-Path $runPath 'natural-play.result.txt')
    Copy-Item -LiteralPath (Join-Path $capturesPath 'natural-play.events.txt') -Destination $runPath
    $battleIds = [regex]::Match($resultText, '(?m)^battle_ids=([^\r\n]+)').Groups[1].Value.Split(',')
    $battleRoot = Join-Path $runPath 'battles'
    New-Item -ItemType Directory -Path $battleRoot | Out-Null
    foreach ($id in $battleIds) {
        if ($id -notmatch '^np-[0-9]{8}T[0-9]{6}-[0-9]{3}$') { throw "Invalid battle id: $id" }
        Copy-Item -LiteralPath (Join-Path $capturesPath "natural-play\battles\$id") -Destination $battleRoot -Recurse
    }
    $shots = [regex]::Match($resultText, '(?m)^captures=([^\r\n]+)').Groups[1].Value.Split(',')
    foreach ($shot in $shots) { if ($shot -match '^np_[a-zA-Z0-9_]+\.png$') { Copy-Item -LiteralPath (Join-Path $capturesPath $shot) -Destination $runPath } }
    [pscustomobject]@{head=$head;startedAt=$startedAt.ToString('o');recordedAt=$recordedAt.ToString('o');finishedAt=(Get-Date).ToString('o');unityExitCode=$unityProcess.ExitCode;session=[regex]::Match($resultText,'(?m)^session=([^\r\n]+)').Groups[1].Value;battles=$battleIds;video=$videoPath;windowTitle=$window.MainWindowTitle;result=($resultText -split '\r?\n')[0]} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $runPath 'run.json') -Encoding utf8
    Write-Output "Archive=$runPath"
    Write-Output (($resultText -split '\r?\n')[0])
    if (-not $resultText.StartsWith('PASS')) { throw 'Natural-play scenario failed; its original result is archived.' }
} finally {
    if ($recorder -and -not $recorder.HasExited) {
        $recorder.StandardInput.WriteLine('q')
        $recorder.StandardInput.Flush()
        if (-not $recorder.WaitForExit(15000)) { Write-Warning 'Recorder has not exited; do not mark video complete.' }
    }
    if ($recorderErrorTask -and $recorder.HasExited) { $recorderErrorTask.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $runPath 'ffmpeg.log') -Encoding utf8 }
    if ($unityProcess -and -not $unityProcess.HasExited) {
        Stop-Process -Id $unityProcess.Id
        $unityProcess.WaitForExit(10000) | Out-Null
    }
    # Restore only the exact pre-run capture files saved above, leaving all new battle evidence intact.
    foreach ($item in $originalFiles) { Copy-Item -LiteralPath (Join-Path $backupPath $item.Name) -Destination $item.FullName -Force }
    if (Test-Path -LiteralPath (Join-Path $backupPath 'BATTLE_INDEX.txt')) { Copy-Item -LiteralPath (Join-Path $backupPath 'BATTLE_INDEX.txt') -Destination $oldIndex -Force }
}
