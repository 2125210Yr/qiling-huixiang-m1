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
$durableResultPath = Join-Path $capturesPath 'natural-play.result.txt'
$requestPath = Join-Path $tempPath 'natural-play.request'
$editorLog = Join-Path $runPath 'editor.log'
$videoPath = Join-Path $runPath 'basic-focus.mp4'
$ffmpegPath = (Get-Command ffmpeg -ErrorAction Stop).Source
$giCachePath = Join-Path $tempPath 'GICache-D-batch'

if (-not $ShowWindow) { throw 'Window recording requires -ShowWindow after explicit permission; hidden Unity windows produce blank captures.' }
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'An existing Unity Editor owns the workspace.' }
if (Test-Path -LiteralPath (Join-Path $projectPath 'Library\EditorInstance.json')) { throw 'EditorInstance.json exists.' }
if (Test-Path -LiteralPath $requestPath) { throw 'An existing natural-play request must be inspected first.' }
if (Test-Path -LiteralPath (Join-Path $tempPath 'natural-play.running')) { throw 'An existing natural-play run must be inspected first.' }
if (-not ('NaturalPlayCaptureWindow' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class NaturalPlayCaptureWindow {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X, Y; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint one, uint two, bool attach);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr window);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr window, ref Point point);
}
'@
}
[NaturalPlayCaptureWindow]::SetProcessDPIAware() | Out-Null
New-Item -ItemType Directory -Force -Path $runPath, $backupPath, $tempPath, $giCachePath | Out-Null
$head = (& git -c safe.directory=F:/天命之子 -C $repoPath rev-parse HEAD).Trim()
$sourcePaths = @(
    'Assets\Scripts\Resonance.Battle\Core\BattleReplay.cs',
    'Assets\Scripts\Resonance.Battle\Progression\Growth.cs',
    'Assets\Scripts\Resonance.Battle\Progression\Bond.cs',
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
    # Unity removes Temp during shutdown; the captures result is the durable copy.
    if (Test-Path -LiteralPath $durableResultPath) { Remove-Item -LiteralPath $durableResultPath }
    # Compile/load first, then arm the existing EventSystem scenario after recording begins.
    $windowStyle = if ($ShowWindow) { 'Normal' } else { 'Hidden' }
    $unityProcess = Start-Process -FilePath $unityPath -WindowStyle $windowStyle -PassThru -ArgumentList @('-projectPath', $projectPath, '-giCustomCacheLocation', $giCachePath, '-logFile', $editorLog)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $window = $null
    while ((Get-Date) -lt $deadline) {
        $window = Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 -and $_.MainWindowTitle -match 'client' } | Select-Object -First 1
        if ($window) { break }
        if ($unityProcess.HasExited) { throw 'Unity exited before opening the project window.' }
        Start-Sleep -Seconds 2
    }
    if (-not $window) { throw 'No project window before the deadline; inspect editor.log.' }
    [NaturalPlayCaptureWindow]::ShowWindow($window.MainWindowHandle, 3) | Out-Null
    [NaturalPlayCaptureWindow]::SetForegroundWindow($window.MainWindowHandle) | Out-Null
    if ([NaturalPlayCaptureWindow]::GetForegroundWindow() -ne $window.MainWindowHandle) {
        # The shell may be a background process. Temporarily share the foreground input queue.
        $foregroundProcessId = [uint32]0
        $foregroundThread = [NaturalPlayCaptureWindow]::GetWindowThreadProcessId([NaturalPlayCaptureWindow]::GetForegroundWindow(), [ref]$foregroundProcessId)
        $currentThread = [NaturalPlayCaptureWindow]::GetCurrentThreadId()
        $attached = [NaturalPlayCaptureWindow]::AttachThreadInput($currentThread, $foregroundThread, $true)
        try {
            [NaturalPlayCaptureWindow]::BringWindowToTop($window.MainWindowHandle) | Out-Null
            [NaturalPlayCaptureWindow]::SetForegroundWindow($window.MainWindowHandle) | Out-Null
        } finally {
            if ($attached) { [NaturalPlayCaptureWindow]::AttachThreadInput($currentThread, $foregroundThread, $false) | Out-Null }
        }
    }
    Start-Sleep -Seconds 2
    # Keep the authorized recording window visible even if another app takes keyboard focus.
    if (-not [NaturalPlayCaptureWindow]::SetWindowPos($window.MainWindowHandle, [IntPtr](-1), 0, 0, 0, 0, 0x13)) { throw 'Cannot keep the Unity recording window visible.' }
    $recordingIssues = [Collections.Generic.List[string]]::new()
    if ([NaturalPlayCaptureWindow]::GetForegroundWindow() -ne $window.MainWindowHandle) {
        $recordingIssues.Add('Unity was not foreground at recording start; inspect the captured region for occlusion')
        Write-Warning $recordingIssues[0]
    }
    $clientRect = [NaturalPlayCaptureWindow+Rect]::new()
    $clientOrigin = [NaturalPlayCaptureWindow+Point]::new()
    if (-not [NaturalPlayCaptureWindow]::GetClientRect($window.MainWindowHandle, [ref]$clientRect)) { throw 'Cannot measure the Unity client window.' }
    if (-not [NaturalPlayCaptureWindow]::ClientToScreen($window.MainWindowHandle, [ref]$clientOrigin)) { throw 'Cannot locate the Unity client window.' }
    $captureRegion = [pscustomobject]@{x=$clientOrigin.X;y=$clientOrigin.Y;width=$clientRect.Right;height=$clientRect.Bottom}
    # Window-DC/title capture is white for this Unity renderer. Capture its visible desktop rectangle.
    $recordInfo = [Diagnostics.ProcessStartInfo]::new()
    $recordInfo.FileName = $ffmpegPath
    $recordInfo.UseShellExecute = $false
    $recordInfo.CreateNoWindow = $true
    $recordInfo.RedirectStandardInput = $true
    $recordInfo.RedirectStandardError = $true
    foreach ($arg in @('-hide_banner','-n','-f','gdigrab','-framerate','30','-draw_mouse','0','-offset_x',[string]$captureRegion.x,'-offset_y',[string]$captureRegion.y,'-video_size',("{0}x{1}" -f $captureRegion.width,$captureRegion.height),'-i','desktop','-vf','scale=trunc(iw/2)*2:trunc(ih/2)*2','-c:v','libx264','-preset','veryfast','-pix_fmt','yuv420p','-movflags','+frag_keyframe+empty_moov',$videoPath)) { $recordInfo.ArgumentList.Add($arg) }
    $recorder = [Diagnostics.Process]::Start($recordInfo)
    $recorderErrorTask = $recorder.StandardError.ReadToEndAsync()
    $recordedAt = Get-Date
    Start-Sleep -Seconds 2
    if ($recorder.HasExited) { throw 'Recorder exited before the scenario was armed.' }
    [System.IO.File]::WriteAllText($requestPath, $Scenario)
    while ((Get-Date) -lt $deadline) {
        if ($unityProcess.HasExited) { break }
        if ($recorder.HasExited) { throw 'Recorder exited before Unity finished; no complete video is claimed.' }
        # Stop checking foreground once the result is written and Unity starts shutting down.
        if (-not (Test-Path -LiteralPath $durableResultPath)) {
            if ([NaturalPlayCaptureWindow]::GetForegroundWindow() -ne $window.MainWindowHandle -and -not $recordingIssues.Contains('Unity lost foreground during the scenario')) { $recordingIssues.Add('Unity lost foreground during the scenario') }
            $currentRect = [NaturalPlayCaptureWindow+Rect]::new()
            $currentOrigin = [NaturalPlayCaptureWindow+Point]::new()
            if (-not [NaturalPlayCaptureWindow]::GetClientRect($window.MainWindowHandle, [ref]$currentRect) -or -not [NaturalPlayCaptureWindow]::ClientToScreen($window.MainWindowHandle, [ref]$currentOrigin) -or $currentOrigin.X -ne $captureRegion.x -or $currentOrigin.Y -ne $captureRegion.y -or $currentRect.Right -ne $captureRegion.width -or $currentRect.Bottom -ne $captureRegion.height) {
                if (-not $recordingIssues.Contains('Unity capture rectangle changed')) { $recordingIssues.Add('Unity capture rectangle changed') }
            }
        }
        Start-Sleep -Milliseconds 250
    }
    if (-not $unityProcess.HasExited) { throw 'Unity deadline exceeded; no result is claimed.' }
    if (-not $recorder.HasExited) {
        $recorder.StandardInput.WriteLine('q')
        $recorder.StandardInput.Flush()
        if (-not $recorder.WaitForExit(15000)) { throw 'Recorder did not finish; no complete video is claimed.' }
    }
    if ($recorder.ExitCode -ne 0) { throw "Recorder failed with exit code $($recorder.ExitCode)." }
    if (-not (Test-Path -LiteralPath $durableResultPath)) { throw 'Unity exited without a durable natural-play result.' }
    if ((Get-Item -LiteralPath $durableResultPath).LastWriteTime -lt $startedAt) { throw 'Natural-play result predates this run.' }
    $resultText = Get-Content -LiteralPath $durableResultPath -Raw
    Copy-Item -LiteralPath $durableResultPath -Destination (Join-Path $runPath 'natural-play.result.txt')
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
    [pscustomobject]@{head=$head;startedAt=$startedAt.ToString('o');recordedAt=$recordedAt.ToString('o');finishedAt=(Get-Date).ToString('o');unityExitCode=$unityProcess.ExitCode;recorderExitCode=$recorder.ExitCode;videoStatus='REQUIRES_VISUAL_VERIFICATION';recordingIssues=@($recordingIssues.ToArray());session=[regex]::Match($resultText,'(?m)^session=([^\r\n]+)').Groups[1].Value;battles=$battleIds;video=$videoPath;windowTitle=$window.MainWindowTitle;captureRegion=$captureRegion;result=($resultText -split '\r?\n')[0]} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $runPath 'run.json') -Encoding utf8
    Write-Output "Archive=$runPath"
    Write-Output ("Scenario=" + ($resultText -split '\r?\n')[0] + '; video requires visual verification')
    if ($recordingIssues.Count -gt 0) { Write-Warning ($recordingIssues -join '; ') }
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
    $instancePath = Join-Path $projectPath 'Library\EditorInstance.json'
    if ($unityProcess -and $unityProcess.HasExited -and (Test-Path -LiteralPath $instancePath)) {
        try {
            $instance = Get-Content -LiteralPath $instancePath -Raw | ConvertFrom-Json
            if ($instance.process_id -eq $unityProcess.Id) { Remove-Item -LiteralPath $instancePath }
        } catch { Write-Warning "Could not inspect this run's EditorInstance.json: $_" }
    }
}
