param(
    [Parameter(Mandatory=$true)][string]$Plan,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$planData = Get-Content -LiteralPath $Plan -Raw -Encoding utf8 | ConvertFrom-Json
$root = [IO.Path]::GetFullPath([string]$planData.root).TrimEnd('\')
if ($root -ne 'F:\天命之子') { throw 'Unexpected library root' }

function Assert-InRoot([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path).TrimEnd('\')
    if ($full -eq $root -or -not $full.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside the named library or is its root: $full"
    }
    return $full
}
function Assert-PhysicalParents([string]$Path) {
    $parent = [IO.Path]::GetDirectoryName($Path)
    while ($parent -and $parent -ne $root) {
        if (Test-Path -LiteralPath $parent) {
            $entry = Get-Item -LiteralPath $parent -Force
            if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "A move cannot cross a link in its parent path: $parent"
            }
        }
        $parent = [IO.Path]::GetDirectoryName($parent)
    }
}
function Ensure-Directory([string]$Path) {
    $null = Assert-InRoot $Path
    Assert-PhysicalParents $Path
    if (-not (Test-Path -LiteralPath $Path)) {
        $null = New-Item -ItemType Directory -Path $Path
    }
    $entry = Get-Item -LiteralPath $Path -Force
    if (-not $entry.PSIsContainer -or ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Expected a physical directory: $Path"
    }
}
function Hide-Entry([string]$Path) {
    $entry = Get-Item -LiteralPath $Path -Force
    $target = $null
    $targetAttributes = $null
    if ($entry.LinkType -eq 'Junction') {
        $target = [string]$entry.Target
        $targetAttributes = (Get-Item -LiteralPath $target -Force).Attributes
    }
    & attrib.exe +H /L $Path
    if ($LASTEXITCODE -ne 0) { throw "Cannot hide compatibility entry: $Path" }
    if (-not ((Get-Item -LiteralPath $Path -Force).Attributes -band [IO.FileAttributes]::Hidden)) {
        throw "Hidden attribute was not applied: $Path"
    }
    if ($target -and (Get-Item -LiteralPath $target -Force).Attributes -ne $targetAttributes) {
        throw "Hiding the link unexpectedly changed its target: $Path"
    }
}

# Validate every lexical target before any mutation. Each move also checks the
# existing source and its physical parents immediately before execution.
foreach ($action in $planData.actions) {
    if ($action.kind -notin @('move', 'move_with_junction', 'shortcut', 'hide')) { throw 'Unknown action' }
    if ($action.source) { $null = Assert-InRoot ([string]$action.source) }
    if ($action.destination) { $null = Assert-InRoot ([string]$action.destination) }
}
if (-not $Apply) {
    [pscustomobject]@{validated=$true; actions=$planData.actions.Count; root=$root} | ConvertTo-Json -Compress
    return
}

$journalPath = [string]$planData.journal
if (Test-Path -LiteralPath $journalPath) { throw 'Use a new journal; do not blindly replay a partial run' }
$journal = [IO.StreamWriter]::new($journalPath, $false, [Text.UTF8Encoding]::new($false))
$journal.AutoFlush = $true
$shell = New-Object -ComObject WScript.Shell
$completed = 0
try {
    foreach ($action in $planData.actions) {
        $journal.WriteLine((@{event='begin';at=[DateTimeOffset]::Now.ToString('o');action=$action} | ConvertTo-Json -Depth 10 -Compress))
        $source = [string]$action.source
        $destination = [string]$action.destination
        switch ($action.kind) {
            { $_ -in 'move', 'move_with_junction' } {
                Assert-PhysicalParents $source
                Assert-PhysicalParents $destination
                $entry = Get-Item -LiteralPath $source -Force
                if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Cannot move a link: $source" }
                if (Test-Path -LiteralPath $destination) { throw "Destination already exists: $destination" }
                if ([IO.Path]::GetPathRoot($source) -ne [IO.Path]::GetPathRoot($destination)) { throw 'Only same-volume moves are supported' }
                Ensure-Directory ([IO.Path]::GetDirectoryName($destination))
                Move-Item -LiteralPath $source -Destination $destination -ErrorAction Stop
                if ((Test-Path -LiteralPath $source) -or -not (Test-Path -LiteralPath $destination)) { throw 'Move postcondition failed' }
                if ($action.kind -eq 'move_with_junction') {
                    if (-not (Get-Item -LiteralPath $destination).PSIsContainer) { throw 'Compatibility junction requires a directory' }
                    $null = New-Item -ItemType Junction -Path $source -Target $destination
                    Hide-Entry $source
                }
            }
            'shortcut' {
                if (-not (Test-Path -LiteralPath $source)) { throw "Shortcut target missing: $source" }
                if (Test-Path -LiteralPath $destination) { throw "Shortcut already exists: $destination" }
                Ensure-Directory ([IO.Path]::GetDirectoryName($destination))
                $link = $shell.CreateShortcut($destination)
                $link.TargetPath = $source
                $link.WorkingDirectory = if ((Get-Item -LiteralPath $source).PSIsContainer) { $source } else { [IO.Path]::GetDirectoryName($source) }
                $link.Description = [string]$action.description
                $link.Save()
            }
            'hide' { Hide-Entry $source }
        }
        $completed++
        $journal.WriteLine((@{event='done';index=$completed;id=$action.id;at=[DateTimeOffset]::Now.ToString('o')} | ConvertTo-Json -Compress))
        if ($completed % 250 -eq 0) { Write-Output "Completed $completed / $($planData.actions.Count)" }
    }
} finally {
    $journal.Dispose()
    [Runtime.InteropServices.Marshal]::ReleaseComObject($shell) | Out-Null
}
[pscustomobject]@{completed=$completed;deletions=0;journal=$journalPath} | ConvertTo-Json -Compress
