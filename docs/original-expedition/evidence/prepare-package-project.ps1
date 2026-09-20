#requires -Version 7.0
[CmdletBinding()]
param(
    [string] $SourceProject = 'F:/Resonance/client',
    [Parameter(Mandatory = $true)][string] $Destination,
    [string] $SourceCommit = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-PhysicalPath {
    param([string] $Path, [bool] $MustExist = $false)
    if ([string]::IsNullOrWhiteSpace($Path) -or $Path -notmatch '^[A-Za-z]:[\\/]') {
        throw 'Use an absolute local drive path; relative, UNC and device paths are not accepted.'
    }
    $full = [IO.Path]::GetFullPath($Path)
    $walk = $full
    while ($walk) {
        $item = Get-ExistingItem $walk
        if ($null -eq $item) {
            if ($MustExist -and $walk -eq $full) { throw "Required path does not exist: $full" }
        }
        elseif (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Reparse points, junctions and symbolic links are forbidden: $walk"
        }
        $parent = [IO.Directory]::GetParent($walk)
        $walk = if ($null -eq $parent) { $null } else { $parent.FullName }
    }
    if ($full -eq [IO.Path]::GetPathRoot($full)) { return $full }
    return $full.TrimEnd([IO.Path]::DirectorySeparatorChar)
}

function Get-ExistingItem {
    param([string] $Path)
    try { return Get-Item -LiteralPath $Path -Force -ErrorAction Stop }
    catch {
        if ($_.FullyQualifiedErrorId -notmatch 'PathNotFound|ItemNotFound') { throw }
        return $null
    }
}

function Test-WithinRoot {
    param([string] $Path, [string] $Root)
    return $Path.Equals($Root, [StringComparison]::OrdinalIgnoreCase) -or
        $Path.StartsWith($Root.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)
}

function Assert-NewDestination {
    param([string] $Path, [string] $SourceRoot)
    $full = Get-PhysicalPath $Path
    if ($null -ne (Get-ExistingItem $full)) { throw "Destination already exists; no overwrite or cleanup is allowed: $full" }
    if (Test-WithinRoot $full $SourceRoot) { throw 'Destination must be outside the source project.' }
    return $full
}

function Get-PhysicalFiles {
    param([string] $Root)
    $pending = [Collections.Generic.Stack[string]]::new()
    $pending.Push((Get-PhysicalPath $Root $true))
    while ($pending.Count -gt 0) {
        $current = $pending.Pop()
        foreach ($item in Get-ChildItem -LiteralPath $current -Force) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Reparse point found during enumeration: $($item.FullName)"
            }
            if ($item.PSIsContainer) { $pending.Push($item.FullName) }
            else { Write-Output $item }
        }
    }
}

function Assert-CopyPath {
    param([string] $RelativePath)
    $rel = $RelativePath.Replace('\', '/')
    if ($rel -match '(^|/)\.\.?(/|$)' -or $rel -match '^/|:' -or $rel -match '[*?]') {
        throw "Unsafe relative copy path: $rel"
    }
    if ($rel -match '(?i)(^|/)(Native|StreamingAssets|EmeraldBunny|_Recovery)(/|$)' -or
        $rel -match '(?i)\.(dll|so|dylib|bytes|inp|inx|moc3?|mtn|fbx|obj|png|jpe?g|psd|wav|mp3|ogg|mp4|zip)(\.meta)?$') {
        throw "Native, model, reference or media asset is forbidden: $rel"
    }
    if ($rel -match '(?i)(^|/)Resources/' -and $rel -notmatch '^Assets/Resources/Fonts\.meta$|^Assets/Resources/Fonts/NotoSansSC\.otf(\.meta)?$') {
        throw "Resources allowlist violation: $rel"
    }
}

function Add-CopyFile {
    param([string] $SourcePath, [string] $RelativePath, [string] $SourceKind)
    Assert-CopyPath $RelativePath
    $from = Get-PhysicalPath $SourcePath $true
    if ((Get-Item -LiteralPath $from -Force).PSIsContainer) { throw "Expected a file: $from" }
    $relative = $RelativePath.Replace('\', '/')
    if ($script:CopyPlan.ContainsKey($relative)) {
        if ($script:CopyPlan[$relative].SourcePath -ne $from) { throw "Conflicting source for $relative" }
        return
    }
    $script:CopyPlan.Add($relative, [pscustomobject]@{
        RelativePath = $relative; SourcePath = $from; SourceKind = $SourceKind
        Sha256 = $null; Bytes = 0L
    })
}

function Add-ProjectFile {
    param([string] $RelativePath)
    $from = Get-PhysicalPath ([IO.Path]::Combine($script:SourceRoot, $RelativePath)) $true
    if (-not (Test-WithinRoot $from $script:SourceRoot)) { throw "Source escapes project root: $RelativePath" }
    Add-CopyFile $from $RelativePath 'project'
}

function Add-Asset {
    param([string] $RelativePath)
    Add-ProjectFile $RelativePath
    $assetMeta = $RelativePath + '.meta'
    if ($null -ne (Get-ExistingItem ([IO.Path]::Combine($script:SourceRoot, $assetMeta)))) {
        Add-ProjectFile $assetMeta
    }
    else { throw "Required asset meta is missing: $assetMeta" }
    $folder = [IO.Path]::GetDirectoryName($RelativePath)
    while ($folder -and $folder -ne 'Assets') {
        $folderMeta = $folder + '.meta'
        if ($null -ne (Get-ExistingItem ([IO.Path]::Combine($script:SourceRoot, $folderMeta)))) {
            Add-ProjectFile $folderMeta
        }
        $folder = [IO.Path]::GetDirectoryName($folder)
    }
}

function Add-CodeTree {
    param([string] $RelativeRoot)
    foreach ($file in Get-PhysicalFiles ([IO.Path]::Combine($script:SourceRoot, $RelativeRoot))) {
        if ($file.Extension -in '.cs', '.asmdef') {
            $rel = [IO.Path]::GetRelativePath($script:SourceRoot, $file.FullName)
            Add-Asset $rel
        }
    }
}

function Ensure-PhysicalDirectory {
    param([string] $Path)
    $full = Get-PhysicalPath $Path
    $pending = [Collections.Generic.Stack[string]]::new()
    $walk = $full
    while ($null -eq (Get-ExistingItem $walk)) {
        $pending.Push($walk)
        $parent = [IO.Directory]::GetParent($walk)
        if ($null -eq $parent) { throw "No existing physical parent for $full" }
        $walk = $parent.FullName
    }
    if (-not (Get-Item -LiteralPath $walk -Force).PSIsContainer) { throw "Parent is not a directory: $walk" }
    while ($pending.Count -gt 0) {
        $next = $pending.Pop()
        Get-PhysicalPath ([IO.Path]::GetDirectoryName($next)) $true | Out-Null
        New-Item -ItemType Directory -Path $next -ErrorAction Stop | Out-Null
        Get-PhysicalPath $next $true | Out-Null
    }
}

$script:SourceRoot = Get-PhysicalPath $SourceProject $true
$targetRoot = Assert-NewDestination $Destination $script:SourceRoot
$script:CopyPlan = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
$settingsNames = @('DefaultVolumeProfile', 'Mobile_RPAsset', 'Mobile_Renderer', 'PC_RPAsset',
    'PC_Renderer', 'SampleSceneProfile', 'UniversalRenderPipelineGlobalSettings')

Add-CodeTree 'Assets/Scripts/Resonance.Battle'
Add-CodeTree 'Assets/Scripts/Resonance.App'
Add-CodeTree 'Assets/Inochi2D/Runtime'
foreach ($name in @('FootStretchTimeline.cs', 'InochiStandee.cs', 'NativeSurface.cs', 'EmeraldInochi.asmdef')) {
    Add-Asset ('Assets/EmeraldInochi/' + $name)
}
Add-Asset 'Assets/Scenes/Boot.unity'
foreach ($name in $settingsNames) { Add-Asset ('Assets/Settings/' + $name + '.asset') }
Add-Asset 'Assets/Resources/Fonts/NotoSansSC.otf'
Add-Asset 'Assets/Editor/Build/WindowsBuild.cs'
Add-Asset 'Assets/Editor/Build/OriginalExpeditionBuild.cs'
Add-Asset 'Assets/Editor/Resonance.Editor.asmdef'
Add-ProjectFile 'Packages/manifest.json'
Add-ProjectFile 'Packages/packages-lock.json'
Add-ProjectFile 'ProjectSettings/ProjectVersion.txt'
foreach ($file in Get-ChildItem -LiteralPath ([IO.Path]::Combine($script:SourceRoot, 'ProjectSettings')) -File -Force) {
    if ($file.Extension -eq '.asset') { Add-ProjectFile ('ProjectSettings/' + $file.Name) }
}

$noticeRoot = Get-PhysicalPath ([IO.Path]::GetFullPath([IO.Path]::Combine($PSScriptRoot, '../ThirdPartyNotices/NotoSansSC'))) $true
foreach ($name in @('COPYRIGHT.txt', 'OFL.txt', 'SOURCE.json')) {
    Add-CopyFile ([IO.Path]::Combine($noticeRoot, $name)) ('ThirdPartyNotices/NotoSansSC/' + $name) 'font-notice'
}
Add-CopyFile ([IO.Path]::Combine($script:SourceRoot, 'Assets/Inochi2D/LICENSE.md')) 'ThirdPartyNotices/Inochi2D/LICENSE.md' 'project-license'

# Preflight completes before any destination directory is created. No Unity execution.
$entries = @($script:CopyPlan.Values | Sort-Object RelativePath)
foreach ($entry in $entries) {
    $entry.Sha256 = (Get-FileHash -LiteralPath $entry.SourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
    $entry.Bytes = (Get-Item -LiteralPath $entry.SourcePath -Force).Length
}
$expectedFontHash = 'a2b93e6c2db05d6bbbf6f27d413ec73269735b7b679019c8a5aa9670ff0ffbf2'
if ($script:CopyPlan['Assets/Resources/Fonts/NotoSansSC.otf'].Sha256 -ne $expectedFontHash) {
    throw 'Font differs from the audited binary; update the font provenance audit before copying.'
}

$targetRoot = Assert-NewDestination $targetRoot $script:SourceRoot
Ensure-PhysicalDirectory ([IO.Path]::GetDirectoryName($targetRoot))
New-Item -ItemType Directory -Path $targetRoot -ErrorAction Stop | Out-Null
Get-PhysicalPath $targetRoot $true | Out-Null
foreach ($entry in $entries) {
    Get-PhysicalPath $entry.SourcePath $true | Out-Null
    $to = Get-PhysicalPath ([IO.Path]::Combine($targetRoot, $entry.RelativePath))
    if (-not (Test-WithinRoot $to $targetRoot)) { throw "Destination escapes root: $to" }
    Ensure-PhysicalDirectory ([IO.Path]::GetDirectoryName($to))
    Get-PhysicalPath $to | Out-Null
    if ($null -ne (Get-ExistingItem $to)) { throw "Refusing to overwrite an existing file: $to" }
    [IO.File]::Copy($entry.SourcePath, $to, $false)
    if ((Get-FileHash -LiteralPath $to -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.Sha256) {
        throw "Source changed during copy, or copy hash mismatch: $($entry.RelativePath). Partial output retained."
    }
}

$resourceData = [Collections.Generic.List[string]]::new()
$copiedFiles = @(Get-PhysicalFiles $targetRoot)
foreach ($file in $copiedFiles) {
    $relative = [IO.Path]::GetRelativePath($targetRoot, $file.FullName).Replace('\', '/')
    Assert-CopyPath $relative
    if (-not $script:CopyPlan.ContainsKey($relative)) { throw "Unexpected file in destination: $relative" }
    if ($relative -match '(^|/)Resources/' -and $file.Extension -ne '.meta') { $resourceData.Add($relative) }
}
if ($resourceData.Count -ne 1 -or $resourceData[0] -ne 'Assets/Resources/Fonts/NotoSansSC.otf') {
    throw 'The isolated project must contain exactly one Resources data file: the audited Noto font.'
}
if ($copiedFiles.Count -ne $entries.Count) { throw 'Copied file count differs from the allowlist.' }
foreach ($entry in $entries) {
    Get-PhysicalPath $entry.SourcePath $true | Out-Null
    if ((Get-FileHash -LiteralPath $entry.SourcePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.Sha256) {
        throw "Source changed while preparing snapshot: $($entry.RelativePath). Partial output retained."
    }
}

$manifest = [ordered]@{
    schema = 'original-expedition-package-preparation-v1'
    purpose = 'INTERMEDIATE_SOURCE_SNAPSHOT_NOT_A_RELEASE_BUILD'
    createdUtc = [DateTime]::UtcNow.ToString('o')
    sourceProject = $script:SourceRoot
    sourceCommit = $SourceCommit
    sourceCommitMeaning = 'Caller-supplied base reference; this snapshot may include uncommitted changes. Per-file SHA-256 values identify the actual copied bytes.'
    destination = $targetRoot
    preparationScriptSha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    unityExecuted = $false
    finalDeliveryRequiresNewSnapshot = $true
    sourceHashesRecheckedAfterCopy = $true
    fileCount = $entries.Count
    resourcesData = @($resourceData)
    assetMetaPolicy = 'Every copied asset has its original meta; a missing meta stops preflight. No source meta is generated or edited.'
    files = @($entries | ForEach-Object { [ordered]@{
        path = $_.RelativePath; sha256 = $_.Sha256; bytes = $_.Bytes; sourceKind = $_.SourceKind
    } })
    manifestSelfExcludedFromFileHashes = $true
    copiedBuildEntriesNotExecuted = @('Assets/Editor/Build/WindowsBuild.cs', 'Assets/Editor/Build/OriginalExpeditionBuild.cs')
}
$manifestPath = [IO.Path]::Combine($targetRoot, 'package-input-manifest.json')
Get-PhysicalPath $manifestPath | Out-Null
$manifestStream = [IO.File]::Open($manifestPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($manifest | ConvertTo-Json -Depth 8) + "`n")
    $manifestStream.Write($bytes, 0, $bytes.Length)
}
finally { $manifestStream.Dispose() }

[pscustomobject]@{ Status = 'PREPARED_INTERMEDIATE_SNAPSHOT'; Destination = $targetRoot;
    Manifest = $manifestPath; FileCount = $entries.Count; ResourcesData = @($resourceData);
    UnityExecuted = $false } | ConvertTo-Json -Depth 4
