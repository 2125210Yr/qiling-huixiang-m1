using System;
using System.Collections.Generic;
using System.Globalization;

namespace Resonance.Battle
{
    public sealed class ExpeditionRelicTrigger
    {
        public string Id { get; internal set; }
        public long Serial { get; internal set; }
    }

    public sealed class ExpeditionFeedbackTarget
    {
        public int Slot { get; internal set; }
        public int Generation { get; internal set; }
        public long EffectiveDamage { get; internal set; }
    }

    public sealed class ExpeditionActionFeedback
    {
        public long RootActionId { get; internal set; }
        public long EnemyEffectiveDamage { get; internal set; }
        public long AllyHpDamageTaken { get; internal set; }
        public long AllyShieldAbsorbed { get; internal set; }
        public long AllyEffectiveHealing { get; internal set; }
    }

    /// <summary>Copied combat facts. Target identities are slots plus generations, never substituted art or names.</summary>
    public sealed class ExpeditionFeedbackSnapshot
    {
        public int BarrierEnergy { get; internal set; }
        public int BarrierThreshold { get; internal set; }
        public int HarmonyActorBits { get; internal set; }
        public bool ForteStored { get; internal set; }
        public long TriggerSerial { get; internal set; }
        public IReadOnlyList<ExpeditionRelicTrigger> RelicTriggers { get; internal set; } = Array.Empty<ExpeditionRelicTrigger>();
        public long LatestScatterRootActionId { get; internal set; }
        public IReadOnlyList<ExpeditionFeedbackTarget> LatestScatterTargets { get; internal set; } = Array.Empty<ExpeditionFeedbackTarget>();
        public ExpeditionActionFeedback LatestAction { get; internal set; } = new ExpeditionActionFeedback();
        public long EnemyEffectiveDamage { get; internal set; }
        public long RelicDerivedEnemyDamage { get; internal set; }
        public long AllyHpDamageTaken { get; internal set; }
        public long AllyShieldAbsorbed { get; internal set; }
        public long AllyEffectiveHealing { get; internal set; }
        public long AllyOverheal { get; internal set; }
        public int SuccessfulActiveCommands { get; internal set; }
        public int RejectedActiveCommands { get; internal set; }
        public int NotChargedRejections { get; internal set; }
        public int TargetInvalidRejections { get; internal set; }
    }

    public sealed class ExpeditionBattleSummary
    {
        public ExpeditionFeedbackSnapshot Snapshot { get; internal set; }
        public IReadOnlyList<string> Facts { get; internal set; } = Array.Empty<string>();
        public string Advice { get; internal set; } = "";
        public string AdviceCode { get; internal set; } = "";
    }

    public static class ExpeditionBattleFeedback
    {
        public static ExpeditionFeedbackSnapshot Capture(BattleSim sim)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            if (!sim.IsOriginalExpedition || sim.ExpeditionRelics == null)
                throw new ArgumentException("Original expedition runtime is required.", nameof(sim));
            var runtime = sim.ExpeditionRelics;
            var snapshot = new ExpeditionFeedbackSnapshot
            {
                BarrierEnergy = runtime.BarrierEnergy,
                BarrierThreshold = runtime.BarrierThreshold,
                HarmonyActorBits = runtime.DistinctActorBits,
                ForteStored = runtime.ForteStored,
                TriggerSerial = runtime.TriggerSerial
            };
            var triggers = new List<ExpeditionRelicTrigger>();
            foreach (var pair in runtime.RelicTriggerSerials)
                triggers.Add(new ExpeditionRelicTrigger { Id = pair.Key, Serial = pair.Value });
            triggers.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
            snapshot.RelicTriggers = triggers.AsReadOnly();

            var resolutions = sim.ExpeditionResolutions;
            if (resolutions.Count > 0) snapshot.LatestAction.RootActionId = resolutions[resolutions.Count - 1].RootActionId;
            // Only settled, effective B01 damage is a scatter hit. A request or an overkill-only row is not one.
            for (var i = resolutions.Count - 1; i >= 0; i--)
            {
                var result = resolutions[i];
                if (IsScatter(result)) { snapshot.LatestScatterRootActionId = result.RootActionId; break; }
            }
            var scatterTargets = new List<ExpeditionFeedbackTarget>();
            foreach (var result in resolutions)
            {
                var enemyDamage = result.SourceAlly && result.SourceSlot >= 0 && !result.TargetAlly;
                var allyHealing = result.SourceAlly && result.SourceSlot >= 0 && result.TargetAlly;
                if (enemyDamage)
                {
                    snapshot.EnemyEffectiveDamage = checked(snapshot.EnemyEffectiveDamage + result.EffectiveDamage);
                    if (result.Origin == ResolutionOrigin.Derived)
                        snapshot.RelicDerivedEnemyDamage = checked(snapshot.RelicDerivedEnemyDamage + result.EffectiveDamage);
                }
                if (result.TargetAlly)
                {
                    snapshot.AllyHpDamageTaken = checked(snapshot.AllyHpDamageTaken + result.EffectiveHpDamage);
                    snapshot.AllyShieldAbsorbed = checked(snapshot.AllyShieldAbsorbed + result.ShieldAbsorbed);
                }
                if (allyHealing)
                {
                    snapshot.AllyEffectiveHealing = checked(snapshot.AllyEffectiveHealing + result.EffectiveHeal);
                    snapshot.AllyOverheal = checked(snapshot.AllyOverheal + result.Overheal);
                }
                var latest = snapshot.LatestAction;
                if (result.RootActionId == latest.RootActionId)
                {
                    if (enemyDamage) latest.EnemyEffectiveDamage = checked(latest.EnemyEffectiveDamage + result.EffectiveDamage);
                    if (result.TargetAlly)
                    {
                        latest.AllyHpDamageTaken = checked(latest.AllyHpDamageTaken + result.EffectiveHpDamage);
                        latest.AllyShieldAbsorbed = checked(latest.AllyShieldAbsorbed + result.ShieldAbsorbed);
                    }
                    if (allyHealing) latest.AllyEffectiveHealing = checked(latest.AllyEffectiveHealing + result.EffectiveHeal);
                }
                if (result.RootActionId == snapshot.LatestScatterRootActionId && IsScatter(result))
                {
                    // Keep the historical instance generation; never relabel a dead mask as its replacement.
                    var target = scatterTargets.Find(t => t.Slot == result.TargetSlot && t.Generation == result.TargetGeneration);
                    if (target == null)
                    {
                        target = new ExpeditionFeedbackTarget { Slot = result.TargetSlot, Generation = result.TargetGeneration };
                        scatterTargets.Add(target);
                    }
                    target.EffectiveDamage = checked(target.EffectiveDamage + result.EffectiveDamage);
                }
            }
            snapshot.LatestScatterTargets = scatterTargets.AsReadOnly();
            foreach (var command in sim.CommandLog)
            {
                if (command.Kind != BattleCommandKind.Tap && command.Kind != BattleCommandKind.Slide) continue;
                if (command.Accepted) snapshot.SuccessfulActiveCommands = checked(snapshot.SuccessfulActiveCommands + 1);
                else
                {
                    snapshot.RejectedActiveCommands = checked(snapshot.RejectedActiveCommands + 1);
                    if (command.Reason == CommandReject.NotCharged) snapshot.NotChargedRejections = checked(snapshot.NotChargedRejections + 1);
                    if (command.Reason == CommandReject.TargetInvalid) snapshot.TargetInvalidRejections = checked(snapshot.TargetInvalidRejections + 1);
                }
            }
            return snapshot;
        }

        public static ExpeditionBattleSummary Summarize(BattleSim sim)
        {
            var snapshot = Capture(sim);
            var summary = new ExpeditionBattleSummary
            {
                Snapshot = snapshot,
                Facts = Array.AsReadOnly(new[]
                {
                    "有效敌伤 " + N(snapshot.EnemyEffectiveDamage) + "，其中遗物独立追加伤害 " + N(snapshot.RelicDerivedEnemyDamage) + "。",
                    "护盾实际吸收 " + N(snapshot.AllyShieldAbsorbed) + "，队员累计承受生命伤害 " + N(snapshot.AllyHpDamageTaken) + "。",
                    "有效治疗 " + N(snapshot.AllyEffectiveHealing) + "，过量治疗 " + N(snapshot.AllyOverheal)
                        + "；主动指令成功 " + N(snapshot.SuccessfulActiveCommands) + "、未执行 " + N(snapshot.RejectedActiveCommands) + "。"
                })
            };
            if (snapshot.NotChargedRejections > 0)
                Advice(summary, "charge-readiness", "本场 " + N(snapshot.NotChargedRejections)
                    + " 条主动指令因充能不足未执行；下次恢复暂停队列前，先检查对应队员是否达到 100 充能。");
            else if (snapshot.TargetInvalidRejections > 0)
                Advice(summary, "invalid-target", "本场 " + N(snapshot.TargetInvalidRejections)
                    + " 条主动指令的原目标已失效；前一条指令击杀目标后，为后续角色重新选敌并替换其队列指令。");
            else if (snapshot.ForteStored)
                Advice(summary, "unused-forte", "本场结束仍留有强奏；下次形成和声后，优先安排一名输出角色释放伤害技能来消耗它。");
            else if (snapshot.AllyOverheal > snapshot.AllyEffectiveHealing)
                Advice(summary, "overheal", "本场产生 " + N(snapshot.AllyOverheal)
                    + " 点过量治疗；下次等队员出现缺血后再安排治疗技能。");
            else if (Owns(snapshot, "B01") && snapshot.LatestScatterTargets.Count == 0)
                Advice(summary, "no-scatter", "本场没有实际散射命中；多目标战斗中，先用单体技能攻击一个目标，让散射作用于其他存活敌人。");
            else if (Owns(snapshot, "A01") && snapshot.AllyShieldAbsorbed == 0)
                Advice(summary, "no-absorption", "本场护盾没有实际吸收伤害；下一场看到敌方预告时再安排守护技能，为屏障蓄能创造机会。");
            else if (snapshot.AllyHpDamageTaken > 0 && snapshot.AllyEffectiveHealing == 0)
                Advice(summary, "healing-gap", "本场承受 " + N(snapshot.AllyHpDamageTaken)
                    + " 点生命伤害但没有有效治疗；有队员缺血时，把治疗角色的技能加入队列。");
            else if (snapshot.LatestScatterTargets.Count > 0)
                Advice(summary, "preserve-scatter", "本场散射确实命中了其他敌人；多个敌人存活时，继续用单体技能集火低生命目标来触发散射。");
            else if (snapshot.SuccessfulActiveCommands == 0)
                Advice(summary, "use-primary", "本场没有成功释放主动技能；下一场队员充能达到 100 后，点击其主要技能或加入暂停队列。");
            else
                Advice(summary, "sequence-primary", "本场成功执行 " + N(snapshot.SuccessfulActiveCommands)
                    + " 次主动指令；下一场可用战术暂停，把护盾和增益排在输出技能之前。");
            return summary;
        }

        static bool IsScatter(ResolutionResult result) => result.Origin == ResolutionOrigin.Derived
            && result.SourceRelicId == "B01" && result.SourceAlly && result.SourceSlot >= 0
            && !result.TargetAlly && result.EffectiveDamage > 0;
        static bool Owns(ExpeditionFeedbackSnapshot snapshot, string id)
        {
            foreach (var relic in snapshot.RelicTriggers) if (relic.Id == id) return true;
            return false;
        }
        static void Advice(ExpeditionBattleSummary summary, string code, string text)
        {
            summary.AdviceCode = code;
            summary.Advice = text;
        }
        static string N(long value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
