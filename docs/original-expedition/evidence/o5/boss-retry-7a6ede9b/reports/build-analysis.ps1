$ErrorActionPreference = 'Stop'
$archive = Split-Path $PSScriptRoot -Parent
$local = 'F:/天命之子/dist/original-expedition-v01/resume-7a6ede9b-20260921-144630'
$personal = 'C:/Users/Administrator/AppData/LocalLow/Resonance/契灵回响'
$verifier = 'F:/天命之子/tools/OriginalReplay.Verify/bin/Release/net6.0/OriginalReplay.Verify.dll'
[Reflection.Assembly]::LoadFrom($verifier) | Out-Null
function Hash-Bytes([byte[]] $bytes) { [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant() }
function Hash-File([string] $path) { Hash-Bytes ([IO.File]::ReadAllBytes($path)) }
function Write-AnalysisJson([string] $name, $value) {
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($value | ConvertTo-Json -Depth 70) + "`n")
    $stream = [IO.FileStream]::new((Join-Path $PSScriptRoot $name), [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
}
function Read-Profile([string] $name) {
    $path = Join-Path $local $name
    $bytes = [IO.File]::ReadAllBytes($path)
    $serializer = [Runtime.Serialization.Json.DataContractJsonSerializer]::new([Resonance.Battle.OriginalProfile])
    $inputStream = [IO.MemoryStream]::new($bytes, $false)
    try { $profile = $serializer.ReadObject($inputStream) } finally { $inputStream.Dispose() }
    [Resonance.Battle.OriginalProfileStore]::Validate($profile)
    $saved = $profile.IntegritySha256
    $profile.GetType().GetField('IntegritySha256').SetValue($profile, $null)
    $outputStream = [IO.MemoryStream]::new()
    try { $serializer.WriteObject($outputStream, $profile); $computed = Hash-Bytes $outputStream.ToArray() } finally { $outputStream.Dispose(); $profile.IntegritySha256 = $saved }
    return [pscustomobject]@{ Name = $name; Sha256 = (Hash-Bytes $bytes); Bytes = $bytes.Length; Profile = $profile; Validation = 'PASS'; StoredIntegrity = $saved; ComputedIntegrity = $computed; IntegrityMatches = ($saved -ceq $computed) }
}
function Input-Summary($inputValue, [string] $label) {
    $clone = $inputValue.DeepClone()
    $clone.GetType().GetField('AttemptId').SetValue($clone, $null)
    [pscustomobject]@{ Label = $label; AttemptId = $inputValue.AttemptId; FullHash = [Resonance.Battle.ExpeditionContent]::Fingerprint($inputValue); HashIgnoringOnlyAttemptId = [Resonance.Battle.ExpeditionContent]::Fingerprint($clone); RunId=$inputValue.RunId; EncounterId=$inputValue.EncounterId; Seed=$inputValue.Seed; PresetId=$inputValue.PresetId; OpeningHp=$inputValue.OpeningHp; RelicIds=$inputValue.RelicIds; ContentVersion=$inputValue.ContentVersion; ContentHash=$inputValue.ContentHash }
}
function Leaf-Diff($left, $right, [string] $path = 'Input') {
    if ($null -eq $left -or $null -eq $right) { if ($null -ne $left -or $null -ne $right) { [pscustomobject]@{ Path=$path; Left=$left; Right=$right } }; return }
    if ($left -is [pscustomobject]) {
        foreach ($key in @($left.psobject.Properties.Name + $right.psobject.Properties.Name | Sort-Object -Unique)) { Leaf-Diff $left.$key $right.$key ($path + '.' + $key) }
    } elseif ($left -is [array]) {
        if ($left.Count -ne $right.Count) { [pscustomobject]@{Path=($path+'.Length');Left=$left.Count;Right=$right.Count} }
        for ($i=0; $i -lt [Math]::Min($left.Count,$right.Count); $i++) { Leaf-Diff $left[$i] $right[$i] ($path+'['+$i+']') }
    } elseif ($left -cne $right) { [pscustomobject]@{Path=$path;Left=$left;Right=$right} }
}
function Sum-Field($items, [string] $field) { [long](($items | Measure-Object -Property $field -Sum).Sum) }
$defeatName = 'battle-0aed9659585c4708995d6c591a206753.original-replay.json'
$victoryName = 'battle-5481ae3998634dd98a63671583599c11.original-replay.json'
$defeatJson = [IO.File]::ReadAllText((Join-Path $archive ('replays/'+$defeatName)))
$victoryJson = [IO.File]::ReadAllText((Join-Path $archive ('replays/'+$victoryName)))
$defeat = [Resonance.Battle.OriginalBattleRecord]::FromJson($defeatJson)
$victory = [Resonance.Battle.OriginalBattleRecord]::FromJson($victoryJson)
$before = Read-Profile 'checkpoint-before-close.json'
$reopen = Read-Profile 'checkpoint-after-reopen.json'
$after = Read-Profile 'profile-after-victory.json'
$hub = Read-Profile 'profile-at-hub-after-victory.json'
$inputs = @(
    Input-Summary $defeat.Input 'Natural defeat tape'
    Input-Summary $victory.Input 'Victory tape after ordinary reopen'
    Input-Summary $before.Profile.ActiveRun.BossCheckpoint 'Before close BossCheckpoint'
    Input-Summary $before.Profile.ActiveRun.CurrentBattleCheckpoint 'Before close CurrentBattleCheckpoint'
    Input-Summary $reopen.Profile.ActiveRun.BossCheckpoint 'After reopen BossCheckpoint'
    Input-Summary $reopen.Profile.ActiveRun.CurrentBattleCheckpoint 'After reopen CurrentBattleCheckpoint'
)
$inputDiffs = @(Leaf-Diff ($defeatJson | ConvertFrom-Json).Input ($victoryJson | ConvertFrom-Json).Input)
$comparison = [ordered]@{
    CheckedUtc = [DateTime]::UtcNow.ToString('o')
    Method = 'Typed OriginalBattleRecord and OriginalProfile; exact IEEE canonical fingerprints after nulling only AttemptId on in-memory clones. JSON leaf diff also included; original files remain unchanged.'
    VerifierPath = $verifier; VerifierSha256 = Hash-File $verifier
    InputFingerprints = $inputs
    AllInputsMatchIgnoringOnlyAttemptId = (@($inputs.HashIgnoringOnlyAttemptId | Sort-Object -Unique).Count -eq 1)
    DefeatVersusVictoryInputLeafDifferences = $inputDiffs
    BeforeCloseVersusAfterReopenProfileBytesIdentical = ($before.Sha256 -ceq $reopen.Sha256)
    RunSeed = $before.Profile.ActiveRun.RunSeed
    AfterVictoryVersusHubProfileBytesIdentical = ($after.Sha256 -ceq $hub.Sha256)
    ProfileSnapshots = @($before,$reopen,$after,$hub | ForEach-Object {
        [ordered]@{
            File=$_.Name; Sha256=$_.Sha256; Bytes=$_.Bytes; StructuralValidation=$_.Validation
            StoredIntegritySha256=$_.StoredIntegrity; RecomputedIntegritySha256=$_.ComputedIntegrity; IntegrityMatches=$_.IntegrityMatches
            Revision=$_.Profile.Revision; ActiveRunIsNull=($null -eq $_.Profile.ActiveRun)
            Status=if($_.Profile.ActiveRun){$_.Profile.ActiveRun.Status.ToString()}else{$null}
            SettledBattleCount=if($_.Profile.ActiveRun){$_.Profile.ActiveRun.SettledBattleIds.Length}else{$null}
            UnlockedPresets=$_.Profile.UnlockedPresets; ClearedChapters=$_.Profile.ClearedChapters; DiscoveredRelics=$_.Profile.DiscoveredRelics
        }
    })
    AfterVictoryLastRunSummary=$after.Profile.LastRunSummary
    LiveOriginalProfileAtReadSha256=Hash-File (Join-Path $personal 'OriginalExpedition/profile.v1.json')
    LegacyFiles=@('save.json','save.json.bak' | ForEach-Object {
        $fileName=$_
        $legacyBefore = Get-Content -LiteralPath (Join-Path $local 'legacy-before.json') -Raw | ConvertFrom-Json
        $prior = @($legacyBefore | Where-Object { [IO.Path]::GetFileName($_.path) -eq $fileName })
        if($prior.Count -ne 1){throw 'Missing unique legacy baseline'}
        $expected=$prior[0].sha256
        $actual=Hash-File (Join-Path $personal $fileName)
        [ordered]@{File=$fileName;BeforeSha256=$expected;AfterSha256=$actual;Unchanged=($expected -ceq $actual)}
    })
}
Write-AnalysisJson 'checkpoint-profile-comparison.json' $comparison
$tapeSummaries = foreach ($entry in @(@{Name=$defeatName;Record=$defeat},@{Name=$victoryName;Record=$victory})) {
    $r=$entry.Record
    $enemyHits=@($r.Resolutions | Where-Object {-not $_.TargetAlly -and $_.SourceAlly})
    $allyHits=@($r.Resolutions | Where-Object {$_.TargetAlly})
    $derived=@($enemyHits | Where-Object {$_.Origin.ToString() -eq 'Derived'})
    $active=@($r.Commands | Where-Object {$_.Kind.ToString() -in @('Tap','Slide')})
    $b03=@($r.Resolutions | Where-Object {$_.SourceRelicId -eq 'B01'} | Group-Object RootActionId | Where-Object {@($_.Group | Select-Object TargetSlot,TargetGeneration -Unique).Count -eq 2})
    $b04=@($r.Resolutions | Where-Object {$_.SourceRelicId -eq 'B04'})
    [ordered]@{
        Tape=$entry.Name; EndTick=$r.EndTick; Input=Input-Summary $r.Input $entry.Name
        CommandCounts=@($r.Commands | Group-Object Kind | ForEach-Object {[ordered]@{Kind=$_.Name;Count=$_.Count}})
        ActiveAccepted=@($active | Where-Object Accepted).Count; ActiveRejected=@($active | Where-Object {-not $_.Accepted}).Count
        ActiveTimeline=@($active | Select-Object Seq,Tick,@{Name='Kind';Expression={$_.Kind.ToString()}},Slot,RequiredEnemySlot,RequiredEnemyGeneration,Accepted,@{Name='Reason';Expression={$_.Reason.ToString()}})
        EffectiveEnemyDamage=(Sum-Field $enemyHits 'EffectiveHpDamage')+(Sum-Field $enemyHits 'ShieldAbsorbed')
        DerivedEffectiveEnemyDamage=(Sum-Field $derived 'EffectiveHpDamage')+(Sum-Field $derived 'ShieldAbsorbed')
        AllyShieldAbsorbed=Sum-Field $allyHits 'ShieldAbsorbed'; AllyHpDamage=Sum-Field $allyHits 'EffectiveHpDamage'
        EffectiveHealing=Sum-Field $allyHits 'EffectiveHeal'; Overhealing=Sum-Field $allyHits 'Overheal'
        B03RootsHittingTwoDistinctTargets=$b03.Count
        B03FirstExample=if($b03.Count){$b03[0].Group}else{@()}
        B04EchoCount=$b04.Count; B04EchoEffectiveDamage=(Sum-Field $b04 'EffectiveHpDamage')+(Sum-Field $b04 'ShieldAbsorbed')
        B04CausalRoots=@($b04 | ForEach-Object {$rootId=$_.RootActionId; [ordered]@{RootActionId=$rootId;Chain=@($r.Resolutions | Where-Object RootActionId -eq $rootId)}})
        BossEvents=@($r.Events | Where-Object {$_.Kind -eq 'encounter'} | Select-Object Seq,Tick,Kind,Opcode,Amount,CasterSlot,TargetSlot)
    }
}
Write-AnalysisJson 'battle-settlement-analysis.json' @($tapeSummaries)
[pscustomobject]$comparison | Select-Object AllInputsMatchIgnoringOnlyAttemptId,DefeatVersusVictoryInputLeafDifferences,BeforeCloseVersusAfterReopenProfileBytesIdentical,AfterVictoryVersusHubProfileBytesIdentical,ProfileSnapshots,LegacyFiles | ConvertTo-Json -Depth 10
$tapeSummaries | ForEach-Object {[pscustomobject]$_} | Select-Object Tape,EndTick,ActiveAccepted,ActiveRejected,EffectiveEnemyDamage,DerivedEffectiveEnemyDamage,AllyShieldAbsorbed,AllyHpDamage,EffectiveHealing,Overhealing,B03RootsHittingTwoDistinctTargets,B04EchoCount,B04EchoEffectiveDamage | ConvertTo-Json -Depth 5
