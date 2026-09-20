#requires -Version 7.0
[CmdletBinding()]
param([ValidateSet('RED', 'GREEN')][string] $Phase = 'GREEN')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$tool = Join-Path $PSScriptRoot 'package-player.ps1'
$runRoot = Join-Path ([IO.Path]::GetTempPath()) ('OriginalExpedition-Delivery-FIXTURE-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($runRoot) | Out-Null
$results = [Collections.Generic.List[object]]::new()
$commit = '0123456789abcdef0123456789abcdef01234567'
$contentHash = 'FIXTURE-content-hash'

function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}
function Write-Text([string] $Path, [string] $Text) {
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path)) | Out-Null
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}
function Save-Audit($Fixture) {
    Write-Text $Fixture.AuditPath ($Fixture.Audit | ConvertTo-Json -Depth 12)
}
function New-Fixture([string] $Name) {
    $root = Join-Path $runRoot $Name
    $player = Join-Path $root 'source-player'
    [IO.Directory]::CreateDirectory($player) | Out-Null
    $files = [ordered]@{
        'OriginalExpedition.exe' = 'FIXTURE ONLY: NOT AN EXECUTABLE'
        'OriginalExpedition_Data/data.dat' = 'FIXTURE PLAYER DATA'
        'UnityPlayer.dll' = 'FIXTURE ONLY: NOT A LIBRARY'
        'UnityCrashHandler64.exe' = 'FIXTURE ONLY: NOT AN EXECUTABLE'
        'OriginalExpedition_Data/Resources/unity default resources' = 'FIXTURE UNITY RESOURCES'
        'ThirdPartyNotices/NotoSansSC/COPYRIGHT.txt' = 'FIXTURE COPYRIGHT'
        'ThirdPartyNotices/NotoSansSC/OFL.txt' = 'FIXTURE FONT LICENSE'
        'ThirdPartyNotices/NotoSansSC/SOURCE.json' = '{"purpose":"FIXTURE"}'
        'ThirdPartyNotices/Inochi2D/LICENSE.md' = 'FIXTURE INOCHI LICENSE'
        'UnityThirdPartyNotices.txt' = 'FIXTURE UNITY NOTICE MUST BE PRESERVED'
        'package-input-manifest.json' = '{"purpose":"FIXTURE ONLY","sourceCommit":"0123456789abcdef0123456789abcdef01234567"}'
    }
    $outputs = @()
    foreach ($entry in $files.GetEnumerator()) {
        $path = Join-Path $player $entry.Key
        Write-Text $path $entry.Value
        $outputs += [ordered]@{ path = $entry.Key; bytes = (Get-Item -LiteralPath $path).Length;
            sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant(); role = 'FIXTURE' }
    }
    $fixture = [pscustomobject]@{
        Root = $root; Player = $player; AuditPath = (Join-Path $root 'build-audit.json')
        GuidePath = (Join-Path $root 'PLAY-GUIDE.md'); Destination = (Join-Path $root 'delivery')
        Audit = [ordered]@{ result = 'PASS'; sourceCommit = $commit; contentHash = $contentHash;
            inputManifestSha256 = (Get-FileHash -LiteralPath (Join-Path $player 'package-input-manifest.json')).Hash.ToLowerInvariant()
            exe = (Join-Path $player 'OriginalExpedition.exe'); outputs = $outputs }
    }
    Save-Audit $fixture
    Write-Text $fixture.GuidePath 'FIXTURE PLAY GUIDE; NOT A REAL PLAYER'
    return $fixture
}
function Invoke-Package($Fixture, [string] $ExpectedCommit = $commit, [string] $ExpectedContent = $contentHash) {
    if (-not (Test-Path -LiteralPath $tool -PathType Leaf)) { throw 'Package tool has not been implemented.' }
    $json = & $tool -BuildAuditPath $Fixture.AuditPath -Destination $Fixture.Destination `
        -ExpectedCommit $ExpectedCommit -ExpectedContentHash $ExpectedContent -GuidePath $Fixture.GuidePath
    return ($json | ConvertFrom-Json)
}
function Expect-Rejection($Fixture, [string] $Pattern, [string] $ExpectedCommit = $commit, [string] $ExpectedContent = $contentHash) {
    $rejected = $false
    try { Invoke-Package $Fixture $ExpectedCommit $ExpectedContent | Out-Null }
    catch {
        $rejected = $true
        Assert-True ($_.Exception.Message -match $Pattern) ("Unexpected rejection: " + $_.Exception.Message)
    }
    Assert-True $rejected 'Invalid input was accepted.'
}
function Test-Case([string] $Name, [scriptblock] $Body) {
    try { & $Body; $results.Add([ordered]@{ name = $Name; result = 'PASS' }) }
    catch { $results.Add([ordered]@{ name = $Name; result = 'FAIL'; error = $_.Exception.Message }) }
}

Test-Case 'valid FIXTURE is copied, manifested, zipped and independently verified' {
    $f = New-Fixture 'success'
    $result = Invoke-Package $f
    Assert-True ($result.Status -ceq 'BUILD_CANDIDATE_PACKAGED') 'Missing candidate status.'
    Assert-True ($result.ZipSha256 -ceq (Get-FileHash -LiteralPath ($f.Destination + '.zip')).Hash.ToLowerInvariant()) 'ZIP SHA mismatch.'
    Assert-True (-not $result.FinalAcceptancePassed) 'Must not claim UI or playthrough acceptance.'
    foreach ($row in $f.Audit.outputs) {
        $copy = Join-Path $f.Destination ('Player/' + $row.path)
        Assert-True ((Get-Item -LiteralPath $copy).Length -eq $row.bytes) 'Copied size differs.'
        Assert-True ((Get-FileHash -LiteralPath $copy).Hash.ToLowerInvariant() -ceq $row.sha256) 'Copied hash differs.'
    }
    $manifest = Get-Content -LiteralPath (Join-Path $f.Destination 'FILE-HASHES.json') -Raw | ConvertFrom-Json
    Assert-True ($manifest.files.Count -eq $f.Audit.outputs.Count + 3) 'Manifest must include Player, guide, audit and version.'
    foreach ($row in $manifest.files) {
        Assert-True ((Get-FileHash -LiteralPath (Join-Path $f.Destination $row.path)).Hash.ToLowerInvariant() -ceq $row.sha256) 'Delivery manifest hash differs.'
    }
    $zip = [IO.Compression.ZipFile]::OpenRead($f.Destination + '.zip')
    try {
        Assert-True ($zip.Entries.Count -eq $manifest.files.Count + 1) 'ZIP set differs.'
        foreach ($entry in $zip.Entries) {
            $stream = $entry.Open()
            $algorithm = [Security.Cryptography.SHA256]::Create()
            try { $sha = [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '').ToLowerInvariant() }
            finally { $algorithm.Dispose(); $stream.Dispose() }
            Assert-True ($sha -ceq (Get-FileHash -LiteralPath (Join-Path $f.Destination $entry.FullName)).Hash.ToLowerInvariant()) 'ZIP stream hash differs.'
        }
    }
    finally { $zip.Dispose() }
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Destination 'Player/UnityThirdPartyNotices.txt')) -ceq 'FIXTURE UNITY NOTICE MUST BE PRESERVED') 'Unity notice lost.'
}
Test-Case 'FAIL audit is rejected before destination creation' {
    $f = New-Fixture 'fail-audit'; $f.Audit.result = 'FAIL'; Save-Audit $f
    Expect-Rejection $f 'audit.*PASS'
    Assert-True (-not (Test-Path -LiteralPath $f.Destination)) 'Destination created for invalid audit.'
}
Test-Case 'commit mismatch is rejected' {
    $f = New-Fixture 'commit'; Expect-Rejection $f 'commit' 'wrong-commit'
}
Test-Case 'content mismatch is rejected' {
    $f = New-Fixture 'content'; Expect-Rejection $f 'content' $commit 'wrong-content'
}
Test-Case 'modified file is rejected' {
    $f = New-Fixture 'modified'
    $path = Join-Path $f.Player 'UnityPlayer.dll'
    Write-Text $path ('X' * (Get-Item -LiteralPath $path).Length)
    Expect-Rejection $f 'hash|bytes|size'
}
Test-Case 'missing audited file is rejected without deleting any fixture' {
    $f = New-Fixture 'missing'; Move-Item -LiteralPath (Join-Path $f.Player 'UnityPlayer.dll') -Destination (Join-Path $f.Root 'retained-fixture.dll')
    Expect-Rejection $f 'missing|count|set'
}
Test-Case 'extra file is rejected' {
    $f = New-Fixture 'extra'; Write-Text (Join-Path $f.Player 'extra.txt') 'EXTRA FIXTURE'
    Expect-Rejection $f 'unexpected|count|set'
}
Test-Case 'input manifest audit hash is required' {
    $f = New-Fixture 'input-hash'; $f.Audit.inputManifestSha256 = ('0' * 64); Save-Audit $f
    Expect-Rejection $f 'manifest.*hash'
}
Test-Case 'required third party notice must be audited' {
    $f = New-Fixture 'notice'; $f.Audit.outputs = @($f.Audit.outputs | Where-Object { $_.path -cne 'ThirdPartyNotices/NotoSansSC/OFL.txt' }); Save-Audit $f
    Expect-Rejection $f 'required.*notice|notice.*required'
}
foreach ($unsafe in @('../outside.txt', '/absolute.txt', 'C:/escape.txt', 'data.txt:stream', 'folder/../escape.txt', 'NUL.txt', 'trailing. /file.txt', 'double//slash.txt')) {
    Test-Case ("unsafe output path is rejected: " + $unsafe) {
        $f = New-Fixture ('unsafe-' + $results.Count); $f.Audit.outputs[1].path = $unsafe; Save-Audit $f
        Expect-Rejection $f 'unsafe|relative|path'
        Assert-True (-not (Test-Path -LiteralPath $f.Destination)) 'Unsafe audit created destination.'
    }
}
Test-Case 'case duplicate output is rejected' {
    $f = New-Fixture 'duplicate'; $f.Audit.outputs += [ordered]@{ path = 'ORIGINALEXPEDITION.EXE'; bytes = 1; sha256 = ('0' * 64) }; Save-Audit $f
    Expect-Rejection $f 'duplicate'
}
Test-Case 'audited exe must be OriginalExpedition.exe' {
    $f = New-Fixture 'wrong-exe'; $f.Audit.exe = Join-Path $f.Player 'UnityCrashHandler64.exe'; Save-Audit $f
    Expect-Rejection $f 'OriginalExpedition.exe'
}
Test-Case 'existing destination is preserved' {
    $f = New-Fixture 'existing'; Write-Text (Join-Path $f.Destination 'keep.txt') 'KEEP'
    Expect-Rejection $f 'already exists|overwrite'
    Assert-True ([IO.File]::ReadAllText((Join-Path $f.Destination 'keep.txt')) -ceq 'KEEP') 'Existing destination altered.'
}
Test-Case 'existing sibling ZIP is preserved' {
    $f = New-Fixture 'existing-zip'; Write-Text ($f.Destination + '.zip') 'KEEP ZIP FIXTURE'
    Expect-Rejection $f 'already exists|overwrite'
    Assert-True ([IO.File]::ReadAllText(($f.Destination + '.zip')) -ceq 'KEEP ZIP FIXTURE') 'Existing ZIP altered.'
}
Test-Case 'destination inside source Player is rejected' {
    $f = New-Fixture 'nested'; $f.Destination = Join-Path $f.Player 'delivery'
    Expect-Rejection $f 'outside|inside|within'
}
Test-Case 'source reparse directory is rejected' {
    $f = New-Fixture 'reparse'; $external = Join-Path $f.Root 'external-FIXTURE'
    [IO.Directory]::CreateDirectory($external) | Out-Null
    New-Item -ItemType Junction -Path (Join-Path $f.Player 'linked-FIXTURE') -Target $external | Out-Null
    Expect-Rejection $f 'reparse|junction|link'
}

$failed = @($results | Where-Object { $_.result -eq 'FAIL' })
$evidence = [ordered]@{ schema = 'original-expedition-delivery-tests-v1'; phase = $Phase;
    purpose = 'FIXTURE_ONLY_NOT_REAL_UNITY_PLAYER_VALIDATION'; createdUtc = [DateTime]::UtcNow.ToString('o');
    fixtureRoot = $runRoot; fixturesRetained = $true; unityExecuted = $false; playerExecuted = $false;
    total = $results.Count; passed = $results.Count - $failed.Count; failed = $failed.Count; results = @($results.ToArray()) }
$evidenceDirectory = Join-Path $PSScriptRoot 'tests/evidence'
[IO.Directory]::CreateDirectory($evidenceDirectory) | Out-Null
$evidencePath = Join-Path $evidenceDirectory ($Phase.ToLowerInvariant() + '-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '.json')
[IO.File]::WriteAllText($evidencePath, ($evidence | ConvertTo-Json -Depth 8) + "`n", [Text.UTF8Encoding]::new($false))
[pscustomobject]@{ Phase = $Phase; Total = $results.Count; Passed = $results.Count - $failed.Count; Failed = $failed.Count; Evidence = $evidencePath; FixtureRoot = $runRoot } | ConvertTo-Json
if ($failed.Count -gt 0) { $failed | ForEach-Object { Write-Host ("FAIL: " + $_.name + ': ' + $_.error) }; exit 1 }
