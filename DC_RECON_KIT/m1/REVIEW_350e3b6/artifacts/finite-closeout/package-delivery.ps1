$ErrorActionPreference = 'Stop'
$evidenceRoot = $PSScriptRoot
$deliveryRoot = 'F:\Resonance\deliveries\G2-PathA-20260920'
$zipPath = 'F:\Resonance\deliveries\G2-PathA-20260920.zip'
if (Test-Path -LiteralPath $zipPath) { throw 'ZIP already exists; do not overwrite an inspected delivery.' }
$utf8 = New-Object System.Text.UTF8Encoding($false)
function Save-Json($path, $value) {
    [IO.File]::WriteAllText($path, ($value | ConvertTo-Json -Depth 20), $utf8)
}
$sourceVideo = Join-Path $evidenceRoot 'raw\standalone-player.mp4'
$deliveryVideo = Join-Path $deliveryRoot '试玩实录.mp4'
Copy-Item -LiteralPath $sourceVideo -Destination $deliveryVideo
$sourceVideoHash = (Get-FileHash -LiteralPath $sourceVideo -Algorithm SHA256).Hash
$deliveryVideoHash = (Get-FileHash -LiteralPath $deliveryVideo -Algorithm SHA256).Hash
if ($sourceVideoHash -ne $deliveryVideoHash) { throw 'Video copy hash mismatch.' }
$proof = [ordered]@{
    sourceHead = '672cdff6a7a821347cb14ab75e11760cc1b9fe6d'
    deliveredPath = $deliveryVideo
    sha256 = $deliveryVideoHash
    bytes = (Get-Item -LiteralPath $deliveryVideo).Length
    durationSeconds = 125.9
    codec = 'h264'
    width = 756
    height = 1344
    audioStreams = 0
    continuousUneditedCopy = $true
    sourceHash = $sourceVideoHash
    capture = 'GDI desktop region limited to visible game client: x903 y47 width756 height1344'
    fullDecode = 'ffmpeg decode completed with exit 0; video-decode.json and video-decode.log'
    visualInspection = 'Inspected contact sheets sampled at every 1 second throughout 0-125 seconds; seven key frames and full-resolution result/next frames. All inspected real frames show game client; no desktop occlusion observed. This is sampled inspection, not manual inspection of every frame.'
    keyFrameEvidence = @('video-frames/key-frames.jpg', 'video-frames/result-47s.png', 'video-frames/next-74s.png')
    input = '@oai/sky OS mouse clicks and upward drag; normal executable with no flags; no battle-state injection'
    coverage = 'Home -> normal first stage -> Slide -> CLEAR -> NEXT -> second stage phase 2/2 -> Pause at 01:06, enemy HP 54% -> Home. Second battle did not reach a terminal result.'
    exclusions = 'Does not certify accepted enemy focus, a Tap skill, Fever/Auto coverage, QTE grade, or original-game fidelity.'
    processEvidence = 'standalone-process.json'
    actionEvidence = 'standalone-actions.json'
    runtimeManifest = 'package-runtime-manifest.json'
}
Save-Json (Join-Path $evidenceRoot 'video-proof.json') $proof
Copy-Item -LiteralPath (Join-Path $evidenceRoot 'video-proof.json') -Destination (Join-Path $deliveryRoot 'video-proof.json')
Copy-Item -LiteralPath (Join-Path $evidenceRoot 'package-runtime-manifest.json') -Destination (Join-Path $deliveryRoot 'runtime-manifest.json')
$runtime = Get-Content -LiteralPath (Join-Path $evidenceRoot 'package-runtime-manifest.json') -Raw | ConvertFrom-Json
foreach ($item in $runtime.files) {
    $actual = (Get-FileHash -LiteralPath (Join-Path $deliveryRoot $item.path) -Algorithm SHA256).Hash
    if ($actual -ne $item.sha256) { throw "Runtime file changed: $($item.path)" }
}
$files = @(Get-ChildItem -LiteralPath $deliveryRoot -Recurse -File | Sort-Object FullName | ForEach-Object {
    [ordered]@{
        path = $_.FullName.Substring($deliveryRoot.Length + 1).Replace('\', '/')
        bytes = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
})
Save-Json (Join-Path $evidenceRoot 'delivery-files-manifest.json') ([ordered]@{ sourceHead = $proof.sourceHead; files = $files })
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($deliveryRoot, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $true)
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
$zipPrefix = (Split-Path -Leaf $deliveryRoot) + '/'
$verified = 0
try {
    foreach ($item in $files) {
        $entry = $archive.GetEntry($zipPrefix + $item.path)
        if ($null -eq $entry) { throw "Missing ZIP entry: $($item.path)" }
        if ($entry.Length -ne $item.bytes) { throw "ZIP size mismatch: $($item.path)" }
        $stream = $entry.Open()
        $algorithm = [Security.Cryptography.SHA256]::Create()
        try { $hash = [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '') }
        finally { $stream.Dispose(); $algorithm.Dispose() }
        if ($hash -ne $item.sha256) { throw "ZIP content mismatch: $($item.path)" }
        $verified++
    }
    $entryFiles = @($archive.Entries | Where-Object { $_.Name -ne '' }).Count
    if ($entryFiles -ne $files.Count) { throw 'Unexpected ZIP file count.' }
} finally { $archive.Dispose() }
$summary = [ordered]@{
    createdAt = [DateTime]::UtcNow.ToString('o')
    sourceHead = $proof.sourceHead
    path = $zipPath
    bytes = (Get-Item -LiteralPath $zipPath).Length
    sha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    folder = $deliveryRoot
    runtimeFiles = $runtime.fileCount
    zipFileCount = $entryFiles
    zipContentHashesVerified = $verified
    allZipFileHashesMatch = $true
    actualPlayerRun = 'standalone-process.json, standalone-actions.json, video-proof.json'
    requiredContents = @('Resonance.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe', 'Resonance_Data/', 'MonoBleedingEdge/', 'D3D12/', 'Assets/Content/catalog.json', '开始试玩.cmd', '试玩说明.md', '试玩实录.mp4')
    sourceManifest = 'frozen-source-hashes.json'
    runtimeManifest = 'package-runtime-manifest.json'
    deliveryManifest = 'delivery-files-manifest.json'
    videoSha256 = $deliveryVideoHash
    originalSavesIncluded = $false
    remotePushed = $false
}
Save-Json (Join-Path $evidenceRoot 'package-summary.json') $summary
$summary | ConvertTo-Json -Depth 5
