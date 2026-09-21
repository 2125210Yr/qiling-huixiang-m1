#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $BuildAuditPath,
    [Parameter(Mandatory = $true)][string] $Destination,
    [Parameter(Mandatory = $true)][string] $ExpectedCommit,
    [Parameter(Mandatory = $true)][string] $ExpectedContentHash,
    [string] $GuidePath = (Join-Path $PSScriptRoot '../../docs/original-expedition/PLAY-GUIDE.md'),
    # Existing ACCEPTANCE-PROGRESS.json shape. This validates a report, not player enjoyment.
    [string] $AcceptanceSummaryPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-RelativePath([string] $Path) {
    # The builder emits canonical '/' paths. Reject Windows aliases as well as traversal.
    if ([string]::IsNullOrWhiteSpace($Path) -or $Path -match '[\\:"<>|?*\x00-\x1f]' -or $Path.StartsWith('/')) {
        throw "Unsafe relative output path: $Path"
    }
    foreach ($part in $Path.Split('/')) {
        if ($part -eq '' -or $part -in '.', '..' -or $part -match '[. ]$' -or
            $part -match '^(?i:CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])(?:\.|$)') {
            throw "Unsafe relative output path: $Path"
        }
    }
}

function Get-ExistingItem([string] $Path) {
    try { return Get-Item -LiteralPath $Path -Force -ErrorAction Stop }
    catch {
        if ($_.FullyQualifiedErrorId -notmatch 'PathNotFound|ItemNotFound') { throw }
        return $null
    }
}

function Get-PhysicalPath([string] $Path, [bool] $MustExist = $false) {
    if ([string]::IsNullOrWhiteSpace($Path) -or $Path -notmatch '^[A-Za-z]:[\\/]') {
        throw 'Use an absolute local drive path; relative, UNC and device paths are forbidden.'
    }
    # Normalize caller paths before checking every existing ancestor for links.
    $full = [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    if ($full.Length -eq 2) { $full += '\' }
    if ($full.Length -gt 3) { Assert-RelativePath ($full.Substring(3).Replace('\', '/')) }
    $walk = $full
    while ($walk) {
        $item = Get-ExistingItem $walk
        if ($null -eq $item) {
            if ($MustExist -and $walk -eq $full) { throw "Required path is missing: $full" }
        }
        elseif (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Reparse point, junction or symbolic link is forbidden: $walk"
        }
        $parent = [IO.Directory]::GetParent($walk)
        $walk = if ($null -eq $parent) { $null } else { $parent.FullName }
    }
    return $full
}

function Test-WithinRoot([string] $Path, [string] $Root) {
    return $Path.Equals($Root, [StringComparison]::OrdinalIgnoreCase) -or
        $Path.StartsWith($Root.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)
}

function Get-PhysicalFiles([string] $Root) {
    $pending = [Collections.Generic.Stack[string]]::new()
    $rootPath = Get-PhysicalPath $Root $true
    if (-not (Get-Item -LiteralPath $rootPath -Force).PSIsContainer) { throw "Expected a directory: $rootPath" }
    $pending.Push($rootPath)
    while ($pending.Count -gt 0) {
        $current = $pending.Pop()
        Get-PhysicalPath $current $true | Out-Null
        foreach ($item in Get-ChildItem -LiteralPath $current -Force) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Reparse point in Player or delivery: $($item.FullName)" }
            $relative = [IO.Path]::GetRelativePath($rootPath, $item.FullName).Replace('\', '/')
            Assert-RelativePath $relative
            if ($item.PSIsContainer) { $pending.Push($item.FullName) }
            else { Write-Output $item }
        }
    }
}

function Get-StreamHash([IO.Stream] $Stream) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($algorithm.ComputeHash($Stream)).Replace('-', '').ToLowerInvariant() }
    finally { $algorithm.Dispose() }
}

function Get-FileRecord([string] $Path, [string] $RelativePath) {
    $physical = Get-PhysicalPath $Path $true
    $stream = [IO.File]::Open($physical, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try { return [ordered]@{ path = $RelativePath; bytes = $stream.Length; sha256 = (Get-StreamHash $stream) } }
    finally { $stream.Dispose() }
}

function Assert-FileSet([string] $Root, $Expected) {
    $files = @(Get-PhysicalFiles $Root)
    if ($files.Count -ne $Expected.Count) { throw "File set count differs from audit/manifest: $Root" }
    foreach ($file in $files) {
        $relative = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
        if (-not $Expected.ContainsKey($relative)) { throw "Unexpected file: $relative" }
        $row = $Expected[$relative]
        if ($relative -cne $row.path) { throw "File path case differs from audit/manifest: $relative" }
        $actual = Get-FileRecord $file.FullName $relative
        if ($actual.bytes -ne $row.bytes -or $actual.sha256 -cne $row.sha256) {
            throw "File bytes/hash differ from audit/manifest: $relative"
        }
    }
}

function Ensure-PhysicalDirectory([string] $Path) {
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

function Copy-NewFile([string] $Source, [string] $Target) {
    Get-PhysicalPath $Source $true | Out-Null
    Get-PhysicalPath $Target | Out-Null
    Ensure-PhysicalDirectory ([IO.Path]::GetDirectoryName($Target))
    # Stream only the audited file bytes; do not copy unaudited NTFS alternate streams.
    $inputStream = [IO.File]::Open($Source, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $outputStream = [IO.File]::Open($Target, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $inputStream.CopyTo($outputStream) }
        finally { $outputStream.Dispose() }
    }
    finally { $inputStream.Dispose() }
}

function Write-NewJson([string] $Path, $Value) {
    Get-PhysicalPath $Path | Out-Null
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Value | ConvertTo-Json -Depth 12) + "`n")
        $stream.Write($bytes, 0, $bytes.Length)
    }
    finally { $stream.Dispose() }
}

function Get-AcceptanceSummary([string] $Path, $Audit) {
    try {
        $physical = Get-PhysicalPath $Path $true
        $record = Get-FileRecord $physical 'ACCEPTANCE-SUMMARY.json'
        $report = [IO.File]::ReadAllText($physical) | ConvertFrom-Json -AsHashtable
        if ($report -isnot [Collections.IDictionary] -or
            $report.document -cne 'ORIGINAL-EXPEDITION-MVP v0.1 actual acceptance progress') {
            throw 'Unsupported actual acceptance progress report.'
        }
        $identity = $report.current_package_evidence
        if ($identity -isnot [Collections.IDictionary] -or
            $identity.SourceCommit -cne $Audit.sourceCommit -or $identity.ContentHash -cne $Audit.contentHash) {
            throw 'Report build commit/content must exactly match the audited Player.'
        }
        if ($report.counts -isnot [Collections.IDictionary]) { throw 'Report counts are required.' }
        foreach ($name in @('PASS', 'PARTIAL', 'NOT_RUN')) {
            $value = $report.counts[$name]
            $expected = if ($name -ceq 'PASS') { 38 } else { 0 }
            if (($value -isnot [int] -and $value -isnot [long]) -or $value -ne $expected) {
                throw 'Report counts must be integer PASS=38, PARTIAL=0, NOT_RUN=0.'
            }
        }
        if ($report.checks -isnot [array] -or $report.checks.Count -ne 38) { throw 'Exactly 38 checks are required.' }
        $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $evidenceFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $repository = Get-PhysicalPath (Join-Path $PSScriptRoot '../..') $true
        foreach ($check in $report.checks) {
            if ($check -isnot [Collections.IDictionary] -or $check.id -isnot [string] -or
                $check.id -cnotmatch '^O-(00[1-9]|0[12][0-9]|03[0-8])$' -or -not $ids.Add($check.id)) {
                throw 'Check IDs must be the unique exact set O-001 through O-038.'
            }
            if ($check.actual_status -cne 'PASS' -or $check.actual_result -isnot [string] -or
                [string]::IsNullOrWhiteSpace($check.actual_result)) { throw "Check $($check.id) needs PASS and an actual result." }
            if ($check.remaining_requirements -isnot [array] -or $check.remaining_requirements.Count -ne 0) {
                throw "Check $($check.id) still has remaining requirements or omits their empty array."
            }
            if ($check.evidence_paths -isnot [array] -or $check.evidence_paths.Count -eq 0) {
                throw "Check $($check.id) must cite evidence files."
            }
            foreach ($evidence in $check.evidence_paths) {
                if ($evidence -isnot [string] -or [string]::IsNullOrWhiteSpace($evidence)) { throw 'Evidence path is empty or not text.' }
                if ($evidenceFiles.Add($evidence)) {
                    # Existing reports use repository-relative paths. Absolute local physical files are also allowed.
                    $resolved = $evidence
                    if ($evidence -notmatch '^[A-Za-z]:[\\/]') {
                        Assert-RelativePath $evidence
                        $resolved = Join-Path $repository $evidence
                    }
                    $resolved = Get-PhysicalPath $resolved $true
                    if ((Get-Item -LiteralPath $resolved -Force).PSIsContainer) { throw 'Evidence must cite a file, not a directory.' }
                }
            }
        }
        # Existence/identity validation cannot establish the truth of written conclusions or user acceptance.
        return [pscustomobject]@{ Path = $physical; Record = $record }
    }
    catch { throw "Acceptance report rejected: $($_.Exception.Message)" }
}

try {
    $auditPath = Get-PhysicalPath $BuildAuditPath $true
    $guide = Get-PhysicalPath $GuidePath $true
    $auditRecord = Get-FileRecord $auditPath 'build-audit.json'
    $guideRecord = Get-FileRecord $guide 'PLAY-GUIDE.md'
    $audit = [IO.File]::ReadAllText($auditPath) | ConvertFrom-Json -AsHashtable
    if ($audit.result -cne 'PASS') { throw 'Build audit result must be exactly PASS.' }
    if ([string]::IsNullOrWhiteSpace($ExpectedCommit) -or $audit.sourceCommit -cne $ExpectedCommit) { throw 'Build audit source commit does not exactly match ExpectedCommit.' }
    if ([string]::IsNullOrWhiteSpace($ExpectedContentHash) -or $audit.contentHash -cne $ExpectedContentHash) { throw 'Build audit content hash does not exactly match ExpectedContentHash.' }
    $exe = Get-PhysicalPath $audit.exe $true
    if ([IO.Path]::GetFileName($exe) -cne 'OriginalExpedition.exe') { throw 'Audit exe must be OriginalExpedition.exe.' }
    $source = Get-PhysicalPath ([IO.Path]::GetDirectoryName($exe)) $true
    $target = Get-PhysicalPath $Destination
    $zipPath = Get-PhysicalPath ($target + '.zip')
    if (Test-WithinRoot $target $source) { throw 'Destination must be outside the source Player.' }
    foreach ($path in @($target, $zipPath)) {
        if ($null -ne (Get-ExistingItem $path)) { throw "Destination or ZIP already exists; overwrite is forbidden: $path" }
    }

    $outputs = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
    if ($null -eq $audit.outputs -or $audit.outputs.Count -eq 0) { throw 'Build audit output set is empty.' }
    foreach ($row in $audit.outputs) {
        Assert-RelativePath $row.path
        if ($outputs.ContainsKey($row.path)) { throw "Duplicate audit output path (including case aliases): $($row.path)" }
        if ($row.sha256 -notmatch '^[0-9a-fA-F]{64}$' -or
            ($row.bytes -isnot [long] -and $row.bytes -isnot [int]) -or $row.bytes -lt 0) {
            throw "Invalid output bytes/hash in audit: $($row.path)"
        }
        $outputs.Add($row.path, [ordered]@{ path = $row.path; bytes = [long]$row.bytes; sha256 = $row.sha256.ToLowerInvariant() })
    }
    foreach ($required in @('OriginalExpedition.exe', 'package-input-manifest.json',
        'ThirdPartyNotices/NotoSansSC/COPYRIGHT.txt', 'ThirdPartyNotices/NotoSansSC/OFL.txt',
        'ThirdPartyNotices/NotoSansSC/SOURCE.json', 'ThirdPartyNotices/Inochi2D/LICENSE.md')) {
        if (-not $outputs.ContainsKey($required) -or $outputs[$required].path -cne $required) {
            throw "Required Player input/notice must be audited: $required"
        }
    }
    if ($audit.inputManifestSha256 -notmatch '^[0-9a-fA-F]{64}$' -or
        $outputs['package-input-manifest.json'].sha256 -cne $audit.inputManifestSha256.ToLowerInvariant()) {
        throw 'Input manifest hash differs from the build audit.'
    }
    Assert-FileSet $source $outputs
    $acceptance = $null
    $remainingAcceptance = @('UI inspection', 'Recorded playthrough')
    if ($PSBoundParameters.ContainsKey('AcceptanceSummaryPath')) {
        $acceptance = Get-AcceptanceSummary $AcceptanceSummaryPath $audit
        $remainingAcceptance = @('User experience review')
    }

    # All preflight checks finish before any destination writes. No cleanup, even on failure.
    foreach ($path in @($target, $zipPath)) {
        Get-PhysicalPath $path | Out-Null
        if ($null -ne (Get-ExistingItem $path)) { throw "Destination or ZIP already exists: $path" }
    }
    Ensure-PhysicalDirectory ([IO.Path]::GetDirectoryName($target))
    New-Item -ItemType Directory -Path $target -ErrorAction Stop | Out-Null
    Get-PhysicalPath $target $true | Out-Null
    $playerTarget = Join-Path $target 'Player'
    Ensure-PhysicalDirectory $playerTarget
    $delivery = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($row in @($outputs.Values | Sort-Object path)) {
        Copy-NewFile (Join-Path $source $row.path) (Join-Path $playerTarget $row.path)
        $relative = 'Player/' + $row.path
        $delivery.Add($relative, [ordered]@{ path = $relative; bytes = $row.bytes; sha256 = $row.sha256 })
    }
    Assert-FileSet $playerTarget $outputs
    Assert-FileSet $source $outputs
    Copy-NewFile $auditPath (Join-Path $target 'build-audit.json')
    Copy-NewFile $guide (Join-Path $target 'PLAY-GUIDE.md')
    $delivery.Add('build-audit.json', $auditRecord)
    $delivery.Add('PLAY-GUIDE.md', $guideRecord)
    if ($null -ne $acceptance) {
        Copy-NewFile $acceptance.Path (Join-Path $target 'ACCEPTANCE-SUMMARY.json')
        $delivery.Add('ACCEPTANCE-SUMMARY.json', $acceptance.Record)
    }
    $createdUtc = [DateTime]::UtcNow.ToString('o')
    $version = [ordered]@{
        schema = 'original-expedition-build-candidate-v1'; status = 'BUILD_CANDIDATE'; createdUtc = $createdUtc
        sourceCommit = $ExpectedCommit; contentHash = $ExpectedContentHash; inputManifestSha256 = $audit.inputManifestSha256
        buildAuditSha256 = $auditRecord.sha256; playerExe = 'Player/OriginalExpedition.exe'
        finalAcceptancePassed = $false; remainingAcceptance = $remainingAcceptance
        packagingScriptSha256 = (Get-FileRecord $PSCommandPath 'package-player.ps1').sha256
    }
    if ($null -ne $acceptance) {
        $version.technicalAcceptancePassed = $true
        $version.userAcceptance = 'PENDING_USER_REVIEW'
        $version.acceptanceSummary = 'ACCEPTANCE-SUMMARY.json'
        $version.acceptanceSummarySha256 = $acceptance.Record.sha256
    }
    Write-NewJson (Join-Path $target 'VERSION.json') $version
    $delivery.Add('VERSION.json', (Get-FileRecord (Join-Path $target 'VERSION.json') 'VERSION.json'))
    Write-NewJson (Join-Path $target 'FILE-HASHES.json') ([ordered]@{
        schema = 'original-expedition-delivery-files-v1'; algorithm = 'SHA-256'; createdUtc = $createdUtc
        manifestSelfExcluded = $true; files = @($delivery.Values | Sort-Object path)
    })
    $delivery.Add('FILE-HASHES.json', (Get-FileRecord (Join-Path $target 'FILE-HASHES.json') 'FILE-HASHES.json'))
    Assert-FileSet $target $delivery

    Get-PhysicalPath $zipPath | Out-Null
    $zipStream = [IO.File]::Open($zipPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $archive = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            foreach ($row in @($delivery.Values | Sort-Object path)) {
                $entry = $archive.CreateEntry($row.path, [IO.Compression.CompressionLevel]::Optimal)
                $entryStream = $entry.Open()
                try {
                    $path = Get-PhysicalPath (Join-Path $target $row.path) $true
                    $fileStream = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
                    try { $fileStream.CopyTo($entryStream) }
                    finally { $fileStream.Dispose() }
                }
                finally { $entryStream.Dispose() }
            }
        }
        finally { $archive.Dispose() }
    }
    finally { $zipStream.Dispose() }

    Get-PhysicalPath $zipPath $true | Out-Null
    $archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        if ($archive.Entries.Count -ne $delivery.Count) { throw 'ZIP file set count differs from delivery.' }
        $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($entry in $archive.Entries) {
            Assert-RelativePath $entry.FullName
            if (-not $seen.Add($entry.FullName) -or -not $delivery.ContainsKey($entry.FullName)) { throw "Duplicate or unexpected ZIP entry: $($entry.FullName)" }
            $row = $delivery[$entry.FullName]
            if ($entry.FullName -cne $row.path -or $entry.Length -ne $row.bytes) { throw "ZIP path/bytes differ: $($entry.FullName)" }
            $entryStream = $entry.Open()
            try { $hash = Get-StreamHash $entryStream }
            finally { $entryStream.Dispose() }
            if ($hash -cne $row.sha256) { throw "ZIP stream hash differs: $($entry.FullName)" }
        }
    }
    finally { $archive.Dispose() }
    Assert-FileSet $target $delivery
    Assert-FileSet $source $outputs
    $result = [ordered]@{
        Status = 'BUILD_CANDIDATE_PACKAGED'; Destination = $target; Zip = $zipPath
        ZipSha256 = (Get-FileRecord $zipPath ([IO.Path]::GetFileName($zipPath))).sha256
        SourceCommit = $ExpectedCommit; ContentHash = $ExpectedContentHash
        PlayerFileCount = $outputs.Count; ZipFileCount = $delivery.Count
        FinalAcceptancePassed = $false; RemainingAcceptance = $remainingAcceptance
    }
    if ($null -ne $acceptance) {
        $result.TechnicalAcceptancePassed = $true
        $result.UserAcceptance = 'PENDING_USER_REVIEW'
        $result.AcceptanceSummarySha256 = $acceptance.Record.sha256
    }
    [pscustomobject]$result | ConvertTo-Json -Depth 4
}
catch { throw "Packaging stopped; existing and partial outputs retained. $($_.Exception.Message)" }
