$ErrorActionPreference='Stop'
$archive=Split-Path $PSScriptRoot -Parent
$local='F:/天命之子/dist/original-expedition-v01/full-a-7a6ede9b-20260921-1510'
$dll='F:/天命之子/tools/OriginalReplay.Verify/bin/Release/net6.0/OriginalReplay.Verify.dll'
[Reflection.Assembly]::LoadFrom($dll)|Out-Null
function Hash-Bytes([byte[]]$b){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($b)).ToLowerInvariant()}
function Hash-File($p){Hash-Bytes ([IO.File]::ReadAllBytes($p))}
function Write-Analysis($name,$value){
    $bytes=[Text.UTF8Encoding]::new($false).GetBytes(($value|ConvertTo-Json -Depth 70)+"`n")
    $s=[IO.FileStream]::new((Join-Path $PSScriptRoot $name),[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$s.Write($bytes,0,$bytes.Length);$s.Flush($true)}finally{$s.Dispose()}
}
function Sum($items,$field){[long](($items|Measure-Object -Property $field -Sum).Sum)}
$profiles=foreach($name in @('profile-new-a-run.json','profile-after-a-victory.json','profile-new-c-run.json')){
    $path=Join-Path $local $name
    $bytes=[IO.File]::ReadAllBytes($path)
    $ser=[Runtime.Serialization.Json.DataContractJsonSerializer]::new([Resonance.Battle.OriginalProfile])
    $s=[IO.MemoryStream]::new($bytes,$false)
    try{$p=$ser.ReadObject($s)}finally{$s.Dispose()}
    [Resonance.Battle.OriginalProfileStore]::Validate($p)
    $saved=$p.IntegritySha256
    $p.GetType().GetField('IntegritySha256').SetValue($p,$null)
    $s=[IO.MemoryStream]::new()
    try{$ser.WriteObject($s,$p);$computed=Hash-Bytes $s.ToArray()}finally{$s.Dispose();$p.IntegritySha256=$saved}
    if($saved -cne $computed){throw ('Profile integrity mismatch '+$name)}
    [ordered]@{
        File=$name;Sha256=(Hash-Bytes $bytes);Bytes=$bytes.Length;StructuralValidation='PASS';IntegrityMatches=$true;StoredIntegritySha256=$saved;RecomputedIntegritySha256=$computed
        Revision=$p.Revision;ActiveRunIsNull=($null -eq $p.ActiveRun);ContentVersion=$p.ContentVersion
        ActiveRun=if($p.ActiveRun){[ordered]@{RunId=$p.ActiveRun.RunId;RunSeed=$p.ActiveRun.RunSeed;Status=$p.ActiveRun.Status.ToString();CurrentNode=$p.ActiveRun.CurrentNode;BattleOrdinal=$p.ActiveRun.BattleOrdinal;InitialCoreId=$p.ActiveRun.InitialCoreId;OwnedRelicIds=$p.ActiveRun.OwnedRelicIds;PartyHp=$p.ActiveRun.PartyHp;CurrentCheckpointIsNull=($null -eq $p.ActiveRun.CurrentBattleCheckpoint);BossCheckpointIsNull=($null -eq $p.ActiveRun.BossCheckpoint);SettledBattleIds=$p.ActiveRun.SettledBattleIds;SelectedChoices=$p.ActiveRun.SelectedChoices}}else{$null}
        UnlockedPresets=$p.UnlockedPresets;ClearedChapters=$p.ClearedChapters;DiscoveredRelics=$p.DiscoveredRelics
        LastRunSummaryRunId=$p.LastRunSummary.RunId;LastRunSummaryHash=[Resonance.Battle.ExpeditionContent]::Fingerprint($p.LastRunSummary)
        LastRunSummaryVictory=$p.LastRunSummary.Victory;LastRunSummaryApplied=$p.LastRunSummary.ResultApplied;LastRunSummaryRelics=$p.LastRunSummary.RelicIds
        LastRunSummaryBattles=$p.LastRunSummary.BattlesCompleted;LastRunSummaryNewUnlock=$p.LastRunSummary.UnlockedNewPreset;LastRunSummaryRoute=$p.LastRunSummary.VisitedNodeIds;LastRunSummaryFacts=$p.LastRunSummary.Facts
    }
}
$profileReport=[ordered]@{
    Method='Read-only typed deserialization, structural Validate, original DataContractJsonSerializer integrity hash with genuine null set through reflection. Only selected fields and hashes are exported; full personal profiles remain local.'
    Profiles=@($profiles)
    AAfterVictoryActiveRunCleared=$profiles[1].ActiveRunIsNull
    FreshCOnlyC01=(@($profiles[2].ActiveRun.OwnedRelicIds).Count -eq 1 -and $profiles[2].ActiveRun.OwnedRelicIds[0] -ceq 'C01')
    FreshCPartyHpFull=(($profiles[2].ActiveRun.PartyHp -join ',') -ceq '5200,6800,5600,5000,5400')
    FreshCNoBattleCheckpoints=($profiles[2].ActiveRun.CurrentCheckpointIsNull -and $profiles[2].ActiveRun.BossCheckpointIsNull)
    ALastSummaryPreservedIntoC=($profiles[1].LastRunSummaryHash -ceq $profiles[2].LastRunSummaryHash)
    PermanentUnlocksRetainedIntoC=(($profiles[1].UnlockedPresets -join ',') -ceq ($profiles[2].UnlockedPresets -join ','))
    PriorDiscoveriesRetainedIntoC=(@($profiles[1].DiscoveredRelics|Where-Object {$_ -notin $profiles[2].DiscoveredRelics}).Count -eq 0)
}
Write-Analysis 'profile-transition-analysis.json' $profileReport
$ids=@('f8823184c2da44d2b2f208c9ce087ce3','cf4aed6fee7940b5aa2b27e0fc7eda93','a532095270a243dbb179e4ddc6e09169','89e7096e93e7401ea58ac258a93f3768','f8e6b477c8cf4bfe8b367bc04719dcf2')
$records=@()
$summaries=foreach($id in $ids){
    $name='battle-'+$id+'.original-replay.json'
    $path=Join-Path $archive ('replays/'+$name)
    $r=[Resonance.Battle.OriginalBattleRecord]::FromJson([IO.File]::ReadAllText($path))
    $records+=,$r
    $enemy=@($r.Resolutions|Where-Object {$_.SourceAlly -and -not $_.TargetAlly})
    $derived=@($enemy|Where-Object {$_.Origin.ToString() -eq 'Derived'})
    $ally=@($r.Resolutions|Where-Object TargetAlly)
    $active=@($r.Commands|Where-Object {$_.Kind.ToString() -in @('Tap','Slide')})
    $shieldRoots=@($r.Resolutions|Where-Object {$_.SourceAlly -and $_.ShieldProduced -gt 0}|Group-Object RootActionId|ForEach-Object {
        [ordered]@{RootActionId=[long]$_.Name;SkillId=$_.Group[0].SkillId;TotalShieldProduced=(Sum $_.Group 'ShieldProduced');Targets=@($_.Group|Select-Object TargetSlot,ShieldProduced)}
    })
    $triggers=[ordered]@{};for($i=0;$i -lt 4;$i++){$triggers['A0'+($i+1)]=$r.FinalState.RelicTriggerSerials[$i]}
    [ordered]@{
        Tape=$name;Sha256=(Hash-File $path);RunId=$r.Input.RunId;EncounterId=$r.Input.EncounterId;BattleOrdinal=$r.Input.BattleOrdinal;Seed=$r.Input.Seed;OpeningHp=$r.Input.OpeningHp;RelicIds=$r.Input.RelicIds
        EndTick=$r.EndTick;Outcome=([regex]::Match($r.FinalState.CoreState,'Outcome=([^\r\n]+)').Groups[1].Value)
        EffectiveEnemyDamage=(Sum $enemy 'EffectiveHpDamage')+(Sum $enemy 'ShieldAbsorbed');DerivedEffectiveEnemyDamage=(Sum $derived 'EffectiveHpDamage')+(Sum $derived 'ShieldAbsorbed')
        AllyHpDamage=(Sum $ally 'EffectiveHpDamage');ShieldAbsorbed=(Sum $ally 'ShieldAbsorbed');EffectiveHealing=(Sum $ally 'EffectiveHeal');Overhealing=(Sum $ally 'Overheal')
        ActiveAccepted=@($active|Where-Object Accepted).Count;ActiveRejected=@($active|Where-Object {-not $_.Accepted}).Count
        FinalBarrierThreshold=$r.FinalState.BarrierThreshold;FinalBarrierEnergy=$r.FinalState.BarrierEnergy;FinalRelicTriggerSerials=$triggers
        RelicResolutionGroups=@($derived|Group-Object SourceRelicId|ForEach-Object{[ordered]@{RelicId=$_.Name;RootCount=@($_.Group.RootActionId|Sort-Object -Unique).Count;HitCount=$_.Count;RequestedDamage=(Sum $_.Group 'RequestedDamage');EffectiveDamage=(Sum $_.Group 'EffectiveHpDamage')+(Sum $_.Group 'ShieldAbsorbed');Overkill=(Sum $_.Group 'Overkill')}})
        DerivedResolutions=@($derived);NativeShieldRoots=$shieldRoots
        ActiveCommands=@($active|Select-Object Seq,Tick,@{n='Kind';e={$_.Kind.ToString()}},Slot,RequiredEnemySlot,RequiredEnemyGeneration,Accepted,@{n='Reason';e={$_.Reason.ToString()}})
        AllCommands=@($r.Commands|Select-Object Seq,Tick,@{n='Kind';e={$_.Kind.ToString()}},Slot,Accepted)
        ReplayInputs=@($r.Inputs|Select-Object Tick,@{n='Kind';e={$_.Kind.ToString()}},ActorSlot,Command)
    }
}
$n1=$records[0]
$queue=[ordered]@{
    Tape=$summaries[0].Tape;Tick=206
    RecordedInputOrder=@($n1.Inputs|Where-Object Tick -eq 206|Select-Object Tick,@{n='Kind';e={$_.Kind.ToString()}},ActorSlot,Command)
    ExpectedExecutedSlots=@(1,4,0,3)
    ActualExecutedSlots=@($n1.Commands|Where-Object {$_.Tick -eq 206 -and $_.Kind.ToString() -eq 'Tap'}|Select-Object -ExpandProperty Slot)
    ExactOrderMatches=((@($n1.Commands|Where-Object {$_.Tick -eq 206 -and $_.Kind.ToString() -eq 'Tap'}|Select-Object -ExpandProperty Slot) -join ',') -ceq '1,4,0,3')
    Explanation='Queue guard(1), point(0), overwrite point(0), guide(4), ClearActor point(0), re-add point(0), blade(3), Resume; exactly guard/guide/point/blade executed once each at tick 206. Both original UI inputs and resulting commands are covered by strict replay MATCH.'
}
$report=[ordered]@{VerifierSha256=(Hash-File $dll);RunSeed=13486312;StrictVerification='Five independent CLI reports have status MATCH and exit 0';Battles=@($summaries);N1Queue=$queue;Scope='Settlements are per battle. RelicTriggerSerials are last global serial values, not trigger counts; counts come from distinct resolution roots. A02 changes native shield production rather than generating a separate relic-damage resolution.'}
Write-Analysis 'battle-and-queue-analysis.json' $report
$summaries|ForEach-Object{[pscustomobject]$_}|Select-Object EncounterId,EndTick,Outcome,ActiveAccepted,ActiveRejected,EffectiveEnemyDamage,DerivedEffectiveEnemyDamage,ShieldAbsorbed,EffectiveHealing,FinalBarrierEnergy,FinalRelicTriggerSerials,RelicResolutionGroups|ConvertTo-Json -Depth 10
[pscustomobject]$profileReport|Select-Object AAfterVictoryActiveRunCleared,FreshCOnlyC01,FreshCPartyHpFull,FreshCNoBattleCheckpoints,ALastSummaryPreservedIntoC,PermanentUnlocksRetainedIntoC,PriorDiscoveriesRetainedIntoC|ConvertTo-Json
