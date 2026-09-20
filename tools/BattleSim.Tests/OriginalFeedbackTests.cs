using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Real BattleSim feedback integration. ReadyTap is an explicit charge-boundary fixture;
    // neither the feedback layer nor normal play receives those manual readiness changes.
    public sealed class OriginalFeedbackTests
    {
        [Fact]
        public void EveryRelicTriggeredByOneNativeActionRetainsItsOwnSerial_AndQueryCopiesStayUnchanged()
        {
            var sim = Create("B01", "B02", "B03", "B04");
            var runtime = sim.ExpeditionRelics;
            Assert.Equal(0, runtime.GetTriggerSerial("B01"));
            Assert.Equal(0, runtime.GetTriggerSerial("unknown"));
            ReadyTap(sim, 0);

            var serials = runtime.RelicTriggerSerials;
            Assert.True(serials["B01"] > 0);
            Assert.True(serials["B02"] > serials["B01"]);
            Assert.True(serials["B03"] > serials["B02"]);
            Assert.Equal(0, serials["B04"]); // The first ordinary point hit is not a native kill.
            Assert.Equal("B03", runtime.LastTriggeredRelicId);
            var snapshot = ExpeditionBattleFeedback.Capture(sim);
            Assert.Equal(serials["B01"], snapshot.RelicTriggers.Single(r => r.Id == "B01").Serial);
            Assert.Equal(serials["B02"], snapshot.RelicTriggers.Single(r => r.Id == "B02").Serial);
            Assert.Equal(serials["B03"], snapshot.RelicTriggers.Single(r => r.Id == "B03").Serial);
            var oldB01 = serials["B01"];
            ReadyTap(sim, 3);
            Assert.True(runtime.GetTriggerSerial("B01") > oldB01);
            Assert.Equal(oldB01, serials["B01"]);
            Assert.Equal(oldB01, snapshot.RelicTriggers.Single(r => r.Id == "B01").Serial);
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, long>)serials).Add("bogus", 999));
        }

        [Fact]
        public void BarrierAndScatterFromTheSameAction_BothKeepTheirActualTriggerSerial()
        {
            var sim = Create("A01", "B01", "C01", "C02");
            ReadyTap(sim, 1);
            for (var i = 0; i < 600 && sim.Outcome == BattleOutcome.InProgress
                && sim.ExpeditionRelics.BarrierEnergy < sim.ExpeditionRelics.BarrierThreshold; i++) sim.Tick();
            Assert.True(sim.ExpeditionRelics.BarrierEnergy >= sim.ExpeditionRelics.BarrierThreshold,
                "Real enemy damage must first supply actual shield absorption.");
            var before = sim.ExpeditionRelics.GetTriggerSerial("A01");
            var resultStart = sim.ExpeditionResolutions.Count;
            ReadyTap(sim, 0);
            var action = sim.ExpeditionResolutions.Skip(resultStart).ToArray();
            Assert.Contains(action, r => r.SourceRelicId == "A01" && r.EffectiveDamage > 0);
            Assert.Contains(action, r => r.SourceRelicId == "B01" && r.EffectiveDamage > 0);
            Assert.True(sim.ExpeditionRelics.GetTriggerSerial("A01") > before);
            Assert.True(sim.ExpeditionRelics.GetTriggerSerial("B01") > sim.ExpeditionRelics.GetTriggerSerial("A01"));
            var snapshot = ExpeditionBattleFeedback.Capture(sim);
            Assert.Equal(sim.ExpeditionRelics.BarrierEnergy, snapshot.BarrierEnergy);
            Assert.Equal(sim.ExpeditionRelics.BarrierThreshold, snapshot.BarrierThreshold);
            Assert.Equal(sim.ExpeditionRelics.GetTriggerSerial("A01"), snapshot.RelicTriggers.Single(r => r.Id == "A01").Serial);
        }

        [Fact]
        public void HarmonyBitsAndStoredForteComeFromRuntime_AndC04SerialAppearsOnlyWhenForteIsConsumed()
        {
            var sim = Create("C01", "C02", "C03", "C04");
            ReadyTap(sim, 1);
            ReadyTap(sim, 2);
            var twoActors = ExpeditionBattleFeedback.Capture(sim);
            Assert.Equal((1 << 1) | (1 << 2), twoActors.HarmonyActorBits);
            Assert.False(twoActors.ForteStored);
            ReadyTap(sim, 4);
            var stored = ExpeditionBattleFeedback.Capture(sim);
            Assert.Equal(0, stored.HarmonyActorBits);
            Assert.True(stored.ForteStored);
            Assert.True(stored.RelicTriggers.Single(r => r.Id == "C03").Serial > 0);
            Assert.Equal(0, stored.RelicTriggers.Single(r => r.Id == "C04").Serial);
            ReadyTap(sim, 0);
            var consumed = ExpeditionBattleFeedback.Capture(sim);
            Assert.False(consumed.ForteStored);
            Assert.True(consumed.RelicTriggers.Single(r => r.Id == "C04").Serial > 0);
            Assert.True(stored.ForteStored);
            Assert.Equal((1 << 1) | (1 << 2), twoActors.HarmonyActorBits);
        }

        [Fact]
        public void LatestScatterTargetsAreActualB01Resolutions_WithRootAndGeneration_AndSurviveLaterSupportActions()
        {
            var sim = Create("B01", "B02", "B03");
            ReadyTap(sim, 0);
            var scatter = sim.ExpeditionResolutions.Where(r => r.SourceRelicId == "B01" && r.EffectiveDamage > 0).ToArray();
            Assert.Equal(2, scatter.Length);
            var expectedRoot = scatter.Last().RootActionId;
            ReadyTap(sim, 1);
            var snapshot = ExpeditionBattleFeedback.Capture(sim);
            Assert.Equal(expectedRoot, snapshot.LatestScatterRootActionId);
            Assert.Equal(scatter.Select(r => r.TargetSlot), snapshot.LatestScatterTargets.Select(t => t.Slot));
            Assert.Equal(scatter.Select(r => r.TargetGeneration), snapshot.LatestScatterTargets.Select(t => t.Generation));
            Assert.Equal(scatter.Select(r => (long)r.EffectiveDamage), snapshot.LatestScatterTargets.Select(t => t.EffectiveDamage));
            Assert.Equal(sim.ExpeditionResolutions.Last().RootActionId, snapshot.LatestAction.RootActionId);
            Assert.Equal(0, snapshot.LatestAction.EnemyEffectiveDamage);
            sim.Enemies[scatter[0].TargetSlot].InstanceGeneration++; // Explicit later-instance boundary.
            Assert.Equal(scatter[0].TargetGeneration, snapshot.LatestScatterTargets[0].Generation);
        }

        [Fact]
        public void CapturedTotalsAndLatestActionRecomputeFromActualResults_AndReadingNeverMutatesSimulation()
        {
            var sim = Create("A01", "B01");
            ReadyTap(sim, 1);
            ReadyTap(sim, 0);
            for (var i = 0; i < 160; i++) sim.Tick();
            ReadyTap(sim, 2);
            var state = BattleStateDigest.Of(sim).ToCanonicalString();
            var commandCount = sim.CommandLog.Count;
            var triggerSerial = sim.ExpeditionRelics.TriggerSerial;
            var snapshot = ExpeditionBattleFeedback.Capture(sim);
            var results = sim.ExpeditionResolutions;
            Assert.Equal(results.Where(r => !r.TargetAlly && r.SourceAlly && r.SourceSlot >= 0).Sum(r => (long)r.EffectiveDamage), snapshot.EnemyEffectiveDamage);
            Assert.Equal(results.Where(r => !r.TargetAlly && r.SourceAlly && r.Origin == ResolutionOrigin.Derived).Sum(r => (long)r.EffectiveDamage), snapshot.RelicDerivedEnemyDamage);
            Assert.Equal(results.Where(r => r.TargetAlly).Sum(r => (long)r.EffectiveHpDamage), snapshot.AllyHpDamageTaken);
            Assert.Equal(results.Where(r => r.TargetAlly).Sum(r => (long)r.ShieldAbsorbed), snapshot.AllyShieldAbsorbed);
            Assert.Equal(results.Where(r => r.TargetAlly && r.SourceAlly).Sum(r => (long)r.EffectiveHeal), snapshot.AllyEffectiveHealing);
            Assert.Equal(results.Where(r => r.TargetAlly && r.SourceAlly).Sum(r => (long)r.Overheal), snapshot.AllyOverheal);
            var latest = results.Where(r => r.RootActionId == results.Last().RootActionId).ToArray();
            Assert.Equal(latest.Where(r => r.TargetAlly && r.SourceAlly).Sum(r => (long)r.EffectiveHeal), snapshot.LatestAction.AllyEffectiveHealing);
            Assert.Equal(sim.CommandLog.Count(c => c.Kind == BattleCommandKind.Tap && c.Accepted), snapshot.SuccessfulActiveCommands);
            Assert.Equal(state, BattleStateDigest.Of(sim).ToCanonicalString());
            Assert.Equal(commandCount, sim.CommandLog.Count);
            Assert.Equal(triggerSerial, sim.ExpeditionRelics.TriggerSerial);
        }

        [Fact]
        public void EmptyAndRejectedActionsDoNotInventFeedback_AndSummaryHasThreeCheckableFactsAndOneSpecificAdvice()
        {
            var sim = Create("C01", "C02", "C03", "C04");
            var empty = ExpeditionBattleFeedback.Capture(sim);
            Assert.Equal(0, empty.EnemyEffectiveDamage);
            Assert.Equal(0, empty.LatestAction.RootActionId);
            Assert.Empty(empty.LatestScatterTargets);
            Assert.All(empty.RelicTriggers, r => Assert.Equal(0, r.Serial));
            var rejection = sim.Submit(BattleCommand.Tap(0, CommandSource.Fixture));
            Assert.Equal(CommandReject.NotCharged, rejection.Reason);
            Assert.Equal(0, sim.ExpeditionRelics.TriggerSerial);
            FinishUsingReadySkills(sim);
            var before = BattleStateDigest.Of(sim).ToCanonicalString();
            var summary = ExpeditionBattleFeedback.Summarize(sim);
            Assert.Equal(3, summary.Facts.Count);
            Assert.Contains(N(sim.ExpeditionTotals.EnemyEffectiveDamage), summary.Facts[0]);
            Assert.Contains(N(sim.ExpeditionTotals.AllyShieldAbsorbed), summary.Facts[1]);
            Assert.Contains(N(sim.ExpeditionTotals.AllyEffectiveHealing), summary.Facts[2]);
            Assert.Equal(1, summary.Snapshot.NotChargedRejections);
            Assert.Equal("charge-readiness", summary.AdviceCode);
            Assert.Contains("充能", summary.Advice);
            Assert.Contains("恢复", summary.Advice);
            Assert.DoesNotContain("避免死亡", string.Join("", summary.Facts) + summary.Advice);
            Assert.DoesNotContain("本可", string.Join("", summary.Facts) + summary.Advice);
            Assert.Equal(before, BattleStateDigest.Of(sim).ToCanonicalString());
        }

        [Fact]
        public void EffectiveAndOverhealingAreKeptSeparate_AndAdviceRespondsToActualOverheal()
        {
            var sim = Create();
            ReadyTap(sim, 2); // Everyone is actually at full HP; all requested healing is overheal.
            var initial = ExpeditionBattleFeedback.Capture(sim);
            Assert.Equal(0, initial.AllyEffectiveHealing);
            Assert.True(initial.AllyOverheal > 0);
            // Force no combat outcome or damage numbers: finish with ordinary ready skills and real ticks.
            FinishUsingReadySkills(sim);
            var summary = ExpeditionBattleFeedback.Summarize(sim);
            Assert.True(summary.Snapshot.AllyOverheal > summary.Snapshot.AllyEffectiveHealing);
            Assert.Equal("overheal", summary.AdviceCode);
            Assert.Contains("缺血", summary.Advice);
            Assert.Contains(N(summary.Snapshot.AllyOverheal), summary.Advice);
        }

        static BattleSim Create(params string[] relics)
        {
            var input = RunBattleFactory.CreateInput("N5", "single", 260921, relics, null);
            input.RunId = "feedback-integration-fixture";
            input.EncounterId = "feedback-integration-fixture/N5/1";
            input.AttemptId = "controlled-readiness";
            var sim = RunBattleFactory.Create(input);
            Assert.True(sim.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture)).Accepted);
            return sim;
        }

        static void ReadyTap(BattleSim sim, int slot)
        {
            sim.Allies[slot].Charge = 100; // Declared test boundary, never used by the feedback implementation.
            Assert.True(sim.Submit(BattleCommand.Tap(slot, CommandSource.Fixture)).Accepted);
        }

        static void FinishUsingReadySkills(BattleSim sim)
        {
            for (var i = 0; i < BattleSim.TickHz * 180 && sim.Outcome == BattleOutcome.InProgress; i++)
            {
                var enemy = sim.Enemies.Where(e => e.Alive).OrderBy(e => e.Hp).ThenBy(e => e.Slot).FirstOrDefault();
                if (enemy != null) sim.Submit(BattleCommand.FocusEnemy(enemy.Slot, CommandSource.Fixture));
                foreach (var slot in new[] { 1, 2, 4, 0, 3 })
                    if (sim.CanAct(slot)) Assert.True(sim.Submit(BattleCommand.Tap(slot, CommandSource.Fixture)).Accepted);
                if (sim.Outcome == BattleOutcome.InProgress) sim.Tick();
            }
            Assert.True(sim.Outcome == BattleOutcome.Victory || sim.Outcome == BattleOutcome.Defeat, sim.FailedReason);
        }

        static string N(long value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
