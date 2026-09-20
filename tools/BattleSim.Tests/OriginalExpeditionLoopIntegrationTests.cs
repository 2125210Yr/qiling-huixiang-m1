using System;
using System.IO;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Deterministic command-policy integration fixture, not UI or natural-play evidence.
    public sealed class OriginalExpeditionLoopIntegrationTests
    {
        [Theory]
        [InlineData(91021, "N2-backstage")]
        [InlineData(92021, "N2-audience")]
        public void ActualSimulation_FiveBattlesRewardsAndNextRun_UseNormalCommands(int seed, string branch)
        {
            var path = Path.Combine(Path.GetTempPath(), "original-loop-" + Guid.NewGuid().ToString("N"), "profile.v1.json");
            var flow = new ExpeditionFlow(new OriginalProfileStore(path));
            flow.StartRun("A01", "single", seed);
            int battles = 0;
            for (int transitions = 0; transitions < 25 && flow.Profile.ActiveRun != null; transitions++)
            {
                var run = flow.Profile.ActiveRun;
                if (run.Status == ExpeditionStatus.Reward)
                {
                    var choices = run.PendingOffer.CandidateIds;
                    var chosen = choices.FirstOrDefault(x => x.StartsWith("A", StringComparison.Ordinal)) ?? choices.FirstOrDefault();
                    flow.ChooseReward(run.PendingOffer.Id, flow.Profile.Revision, chosen);
                }
                else if (run.CurrentNode == "N2") flow.ChooseRoute(branch);
                else if (run.CurrentNode == "N3") flow.ChooseWorkshop(true);
                else
                {
                    var opening = flow.BeginBattle();
                    var sim = new BattleSim(opening);
                    for (int i = 0; i < 12000 && sim.Outcome == BattleOutcome.InProgress; i++)
                    {
                        var target = sim.Enemies.Where(e => e.Alive).OrderBy(e => e.Hp).FirstOrDefault();
                        if (target != null && sim.FocusEnemySlot != target.Slot)
                            sim.Submit(BattleCommand.FocusEnemy(target.Slot, CommandSource.Fixture));
                        // Guard, healer, support then attackers; all resource checks remain authoritative.
                        foreach (var slot in new[] { 1, 2, 4, 0, 3 })
                            if (sim.CanAct(slot)) sim.Submit(BattleCommand.Tap(slot, CommandSource.Fixture));
                        sim.Tick();
                    }
                    Assert.True(sim.Outcome == BattleOutcome.Victory,
                        opening.Stage.Id + " seed=" + opening.Seed + " outcome=" + sim.Outcome + " hp=" + string.Join(",", sim.Allies.Select(x => x.Hp)));
                    flow.CompleteBattle(opening.EncounterId, sim.Outcome, sim.Allies.Select(x => x.Hp).ToArray(), attemptId: opening.AttemptId);
                    battles++;
                    flow = new ExpeditionFlow(new OriginalProfileStore(path)); // Reopen every persisted result.
                }
            }
            Assert.Null(flow.Profile.ActiveRun);
            Assert.Equal(5, battles);
            Assert.True(flow.Profile.LastRunSummary.Victory);
            Assert.Contains("sweep", flow.Profile.UnlockedPresets);
            flow.StartRun("C01", "sweep", seed + 1);
            Assert.Equal(new[] { "C01" }, flow.Profile.ActiveRun.OwnedRelicIds);
            Assert.Equal(ExpeditionContent.GetPartyMaxHp("sweep"), flow.Profile.ActiveRun.PartyHp);
        }
    }
}
