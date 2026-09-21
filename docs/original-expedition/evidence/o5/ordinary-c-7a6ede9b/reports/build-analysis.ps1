$ErrorActionPreference='Stop'
$archive=Split-Path $PSScriptRoot -Parent
$local='F:/天命之子/dist/original-expedition-v01/ordinary-c-7a6ede9b-20260921-1526'
$dll='F:/天命之子/tools/OriginalReplay.Verify/bin/Release/net6.0/OriginalReplay.Verify.dll'
[Reflection.Assembly]::LoadFrom($dll)|Out-Null
function Hash-Bytes([byte[]]$b){[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($b)).ToLowerInvariant()}
function Hash-File($p){Hash-Bytes ([IO.File]::ReadAllBytes($p))}
function Write-Analysis($name,$value){$b=[Text.UTF8Encoding]::new($false).GetBytes(($value|ConvertTo-Json -Depth 70)+"`n");$s=[IO.FileStream]::new((Join-Path $PSScriptRoot $name),[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None);try{$s.Write($b,0,$b.Length);$s.Flush($true)}finally{$s.Dispose()}}
function Sum($items,$field){[long](($items|Measure-Object -Property $field -Sum).Sum)}
function Snapshot($sim){
    $rel=$sim.ExpeditionRelics
    $serials=[ordered]@{};foreach($id in @('C01','C02','C03','C04')){$serials[$id]=$rel.GetTriggerSerial($id)}
    $enemyHits=@($sim.ExpeditionResolutions|Where-Object {$_.SourceAlly -and -not $_.TargetAlly})
    [ordered]@{Tick=$sim.TickIndex;Paused=$sim.Paused;ForteStored=$rel.ForteStored;HarmonyActorMask=$rel.HarmonyActorMask;TriggerSerials=$serials;AllyCharge=@($sim.Allies|ForEach-Object {$_.Charge});AllyChargeIeee=@($sim.Allies|ForEach-Object {[BitConverter]::SingleToInt32Bits($_.Charge).ToString('x8')});EnemyHp=@($sim.Enemies|ForEach-Object {$_.Hp});EffectiveEnemyDamage=(Sum $enemyHits 'EffectiveHpDamage')+(Sum $enemyHits 'ShieldAbsorbed')}
}
$profiles=foreach($name in @('profile-C-four-relics-before-N5.json','profile-C-after-N5.json')){
    $bytes=[IO.File]::ReadAllBytes((Join-Path $local $name));$ser=[Runtime.Serialization.Json.DataContractJsonSerializer]::new([Resonance.Battle.OriginalProfile]);$s=[IO.MemoryStream]::new($bytes,$false)
    try{$p=$ser.ReadObject($s)}finally{$s.Dispose()};[Resonance.Battle.OriginalProfileStore]::Validate($p)
    $saved=$p.IntegritySha256;$p.GetType().GetField('IntegritySha256').SetValue($p,$null);$s=[IO.MemoryStream]::new()
    try{$ser.WriteObject($s,$p);$computed=Hash-Bytes $s.ToArray()}finally{$s.Dispose();$p.IntegritySha256=$saved}
    if($saved -cne $computed){throw ('Integrity mismatch '+$name)}
    [ordered]@{File=$name;Sha256=(Hash-Bytes $bytes);Bytes=$bytes.Length;StructuralValidation='PASS';IntegrityMatches=$true;StoredIntegritySha256=$saved;RecomputedIntegritySha256=$computed;Revision=$p.Revision;RunId=$p.ActiveRun.RunId;RunSeed=$p.ActiveRun.RunSeed;CurrentNode=$p.ActiveRun.CurrentNode;Status=$p.ActiveRun.Status.ToString();BattleOrdinal=$p.ActiveRun.BattleOrdinal;OwnedRelicIds=$p.ActiveRun.OwnedRelicIds;PartyHp=$p.ActiveRun.PartyHp;SelectedChoices=$p.ActiveRun.SelectedChoices;SettledBattleIds=$p.ActiveRun.SettledBattleIds;VisitedNodeIds=$p.ActiveRun.VisitedNodeIds;CurrentCheckpointIsNull=($null -eq $p.ActiveRun.CurrentBattleCheckpoint);BossCheckpointIsNull=($null -eq $p.ActiveRun.BossCheckpoint);DiscoveredRelics=$p.DiscoveredRelics;UnlockedPresets=$p.UnlockedPresets;LastRunSummaryRunId=$p.LastRunSummary.RunId;LastRunSummaryHash=[Resonance.Battle.ExpeditionContent]::Fingerprint($p.LastRunSummary)}
}
Write-Analysis 'profile-acquisition-analysis.json' @($profiles)
$ids=@('167669bfd74d464b9f3384738e041ae9','db0d55a8efa9457ea92db0e6132f724d','2248fe9cbb79421bbd0d21ef23950fb2','01d69be1b02d4d5cb28c1aaa1ad4e467')
$battles=foreach($id in $ids){
    $name='battle-'+$id+'.original-replay.json';$path=Join-Path $archive ('replays/'+$name)
    $r=[Resonance.Battle.OriginalBattleRecord]::FromJson([IO.File]::ReadAllText($path));$sim=[Resonance.Battle.RunBattleFactory]::Create($r.Input)
    $trace=foreach($inputEntry in $r.Inputs){
        while($sim.TickIndex -lt $inputEntry.Tick){$beforeTick=$sim.TickIndex;$sim.Tick();if($sim.TickIndex -le $beforeTick){throw 'Unreachable replay tick'}}
        $before=Snapshot $sim;$resolutionStart=$sim.ExpeditionResolutions.Count
        switch($inputEntry.Kind.ToString()){
            'Command' {$sim.Submit($inputEntry.Command)|Out-Null}
            'QueueTap' {$sim.QueueExpeditionTap($inputEntry.ActorSlot)|Out-Null}
            'ClearActor' {$sim.ClearExpeditionQueuedCommand($inputEntry.ActorSlot)|Out-Null}
            'ClearQueue' {$sim.ClearExpeditionQueue()|Out-Null}
            default {throw 'Unknown input'}
        }
        [ordered]@{Tick=$inputEntry.Tick;Kind=$inputEntry.Kind.ToString();ActorSlot=$inputEntry.ActorSlot;CommandKind=$inputEntry.Command.Kind.ToString();CommandSlot=$inputEntry.Command.Slot;Before=$before;After=(Snapshot $sim);NewResolutions=@($sim.ExpeditionResolutions|Select-Object -Skip $resolutionStart)}
    }
    while($sim.TickIndex -lt $r.EndTick){$beforeTick=$sim.TickIndex;$sim.Tick();if($sim.TickIndex -le $beforeTick){throw 'Unreachable final tick'}}
    $captured=[Resonance.Battle.OriginalBattleRecord]::Capture($sim)
    $finalMatch=($captured.FinalState.Hash -ceq $r.FinalState.Hash -and $captured.EventHash -ceq $r.EventHash -and $captured.ResolutionHash -ceq $r.ResolutionHash -and [Resonance.Battle.ExpeditionContent]::Fingerprint($captured.Commands) -ceq [Resonance.Battle.ExpeditionContent]::Fingerprint($r.Commands))
    if(-not $finalMatch){throw ('Instrumented trace final mismatch '+$name)}
    Write-Analysis ($id+'.input-trace.json') ([ordered]@{Tape=$name;Method='Read-only .NET replay of exact actual Player Inputs; before/after instrumentation only. No altered inputs or state injection. Final state, commands, event hash and resolution hash rechecked against original.';TraceFinalMatchesOriginal=$true;Trace=@($trace)})
    $enemy=@($r.Resolutions|Where-Object {$_.SourceAlly -and -not $_.TargetAlly});$ally=@($r.Resolutions|Where-Object TargetAlly);$active=@($r.Commands|Where-Object {$_.Kind.ToString() -in @('Tap','Slide')})
    $trigger=[ordered]@{};for($i=0;$i -lt 4;$i++){$trigger['C0'+($i+1)]=$r.FinalState.RelicTriggerSerials[8+$i]}
    [ordered]@{Tape=$name;Sha256=(Hash-File $path);RunId=$r.Input.RunId;EncounterId=$r.Input.EncounterId;Seed=$r.Input.Seed;OpeningHp=$r.Input.OpeningHp;RelicIds=$r.Input.RelicIds;EndTick=$r.EndTick;Outcome=$sim.Outcome.ToString();EffectiveEnemyDamage=(Sum $enemy 'EffectiveHpDamage')+(Sum $enemy 'ShieldAbsorbed');ShieldAbsorbed=(Sum $ally 'ShieldAbsorbed');EffectiveHealing=(Sum $ally 'EffectiveHeal');Overhealing=(Sum $ally 'Overheal');ActiveAccepted=@($active|Where-Object Accepted).Count;ActiveRejected=@($active|Where-Object {-not $_.Accepted}).Count;FinalTriggerSerials=$trigger;FinalHarmonyMask=$r.FinalState.HarmonyActorMask;FinalForteStored=$r.FinalState.ForteStored;FrozenCParameters=[ordered]@{RelayCharge=$r.Input.RelicParameters.RelayCharge;ImprovedRelayCharge=$r.Input.RelicParameters.ImprovedRelayCharge;HarmonyCharge=$r.Input.RelicParameters.HarmonyCharge;ForteDamageBonus=$r.Input.RelicParameters.ForteDamageBonus};Commands=@($r.Commands|Select-Object Seq,Tick,@{n='Kind';e={$_.Kind.ToString()}},Slot,Accepted,RequiredEnemySlot,RequiredEnemyGeneration);TraceFinalMatchesOriginal=$finalMatch}
}
Write-Analysis 'battle-causal-analysis.json' ([ordered]@{VerifierSha256=(Hash-File $dll);RunSeed=14556671;Scope='Four completed battles to N6, not boss completion. All four independent CLI reports MATCH. Charge grants and forte do not emit independent damage resolutions; input traces expose unchanged replay runtime state transitions.';Battles=@($battles)})
$battles|ForEach-Object{[pscustomobject]$_}|Select-Object EncounterId,EndTick,Outcome,ActiveAccepted,ActiveRejected,EffectiveEnemyDamage,FinalTriggerSerials,TraceFinalMatchesOriginal|ConvertTo-Json -Depth 8
