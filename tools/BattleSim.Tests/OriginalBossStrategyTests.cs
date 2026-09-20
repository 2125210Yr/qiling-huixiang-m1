using System;
using System.Linq;
using Resonance.Battle;
using Xunit;
using Xunit.Abstractions;

namespace Resonance.Tests
{
    // Deterministic legal-input strategies against unmodified frozen content. Not ordinary UI play.
    public sealed class OriginalBossStrategyTests
    {
        readonly ITestOutputHelper _output;
        public OriginalBossStrategyTests(ITestOutputHelper output) { _output = output; }

        [Theory]
        [InlineData("base-mask-first")]
        [InlineData("barrier-hold")]
        [InlineData("scatter-mask-first")]
        public void FixedBossAllowsDifferentLegalPlans(string strategy)
        {
            var relics = strategy == "barrier-hold" ? new[] { "A01", "A02", "A03", "A04" }
                : strategy == "scatter-mask-first" ? new[] { "B01", "B02", "B03", "B04", "C01" } : Array.Empty<string>();
            var input = RunBattleFactory.CreateInput("N7", "single", 260921, relics, null);
            input.RunId = "boss-strategy-check"; input.EncounterId = strategy; input.AttemptId = "1";
            var sim = RunBattleFactory.Create(input);
            var survivedAmplifiedArea = false;
            var maximumLiveMasksAtArea = 0;
            var priority = new[] { 1, 2, 4, 0, 3 };
            for (int tick = 0; tick < 300 * BattleSim.TickHz && sim.Outcome == BattleOutcome.InProgress; tick++)
            {
                var intent = sim.OriginalIntentSnapshot;
                var target = strategy == "barrier-hold" ? sim.Enemies[0]
                    : sim.Enemies.Where(u => u.Alive && u.Slot != 0).OrderBy(u => u.Hp).ThenBy(u => u.Slot).FirstOrDefault() ?? sim.Enemies[0];
                if (target.Alive && sim.FocusEnemySlot != target.Slot)
                    Assert.True(sim.Submit(BattleCommand.FocusEnemy(target.Slot, CommandSource.Player)).Accepted);
                foreach (var slot in priority)
                {
                    if (!sim.CanAct(slot)) continue;
                    if (slot == 1 && (intent == null || !intent.IsCasting || intent.RemainingCastSec > 0.2f)) continue;
                    if (slot == 2 && !sim.Allies.Any(u => u.Alive && u.Hp <= u.MaxHp * 0.8f)) continue;
                    Assert.True(sim.Submit(BattleCommand.Tap(slot, CommandSource.Player)).Accepted, sim.FailedReason);
                    if (sim.Outcome != BattleOutcome.InProgress) break;
                }
                if (sim.Outcome != BattleOutcome.InProgress) break;
                intent = sim.OriginalIntentSnapshot;
                var before = sim.OriginalEncounter.AreaCasts;
                sim.Tick();
                if (sim.OriginalEncounter.AreaCasts > before)
                {
                    maximumLiveMasksAtArea = Math.Max(maximumLiveMasksAtArea, intent.AliveMasks);
                    if (intent.AliveMasks == 2 && sim.Allies.Any(u => u.Alive)) survivedAmplifiedArea = true;
                }
            }
            _output.WriteLine("strategy={0}; outcome={1}; gameSeconds={2:0.00}; ticks={3}; areaCasts={4}; maxMasksAtArea={5}; effectiveEnemyDamage={6}; actualAbsorbed={7}; effectiveHealing={8}; endingHp={9}; error={10}",
                strategy, sim.Outcome, sim.OriginalEncounter.ElapsedSec, sim.TickIndex, sim.OriginalEncounter.AreaCasts,
                maximumLiveMasksAtArea, sim.ExpeditionTotals.EnemyEffectiveDamage, sim.ExpeditionTotals.AllyShieldAbsorbed,
                sim.ExpeditionTotals.AllyEffectiveHealing, string.Join(",", sim.Allies.Select(u => u.Hp)), sim.FailedReason);
            Assert.Equal(BattleOutcome.Victory, sim.Outcome);
            Assert.False(sim.ForceNoCrit);
            Assert.All(sim.CommandLog, c => Assert.True(c.Accepted));
            if (strategy == "barrier-hold")
            {
                Assert.True(survivedAmplifiedArea);
                Assert.True(sim.ExpeditionTotals.AllyShieldAbsorbed > 0);
                Assert.Contains(sim.ExpeditionResolutions, r => r.SourceRelicId == "A04" && r.EffectiveDamage > 0);
            }
        }
    }
}
