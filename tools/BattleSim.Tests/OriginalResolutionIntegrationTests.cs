using System;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Arithmetic boundary fixtures. Frozen stats and charge are controlled, not natural-play evidence.
    public sealed class OriginalResolutionIntegrationTests
    {
        static BattleSim ShieldFixture(int incoming, params string[] relics)
        {
            var input = RunBattleFactory.CreateInput("N4", "single", 71041, relics, null);
            input.Stage.Wave0 = new[] { "OE_PROMPTER", "OE_USHER", "OE_CHORUS" };
            foreach (var c in input.Characters)
            {
                c.Def = 0; c.Crt = 0; c.Element = Element.Fire;
                if (c.IsEnemy) { c.Hp = 100000; c.ChargeTimeSec = 100000; c.SlideSkillId = c.TapSkillId; }
            }
            var skill = input.Skills.Single(s => s.Id == "OE_PROMPTER_tap");
            skill.AtkCoef = 0; skill.FlatPower = incoming; skill.SkillFlat = 0; skill.PercentAtk = 0;
            skill.Target = TargetRule.AllEnemies; skill.TargetCount = 5; skill.HitCount = 1; skill.EffectId = null;
            var sim = new BattleSim(input);
            foreach (var u in sim.Allies.Concat(sim.Enemies)) u.AutoTimer = -100;
            Active(sim, 1);
            return sim;
        }

        static void Active(BattleSim sim, int slot)
        {
            sim.Allies[slot].Charge = 100;
            var result = sim.Submit(BattleCommand.Tap(slot, CommandSource.Fixture));
            Assert.True(result.Accepted, result.Reason + " " + sim.FailedReason);
        }

        static void EnemyGroupAction(BattleSim sim)
        {
            sim.Enemies[0].Charge = 100;
            sim.Tick();
            Assert.Equal(BattleOutcome.InProgress, sim.Outcome);
        }

        [Theory]
        [InlineData(223, 1115)]
        [InlineData(224, 1120)]
        [InlineData(449, 2245)]
        [InlineData(672, 3360)]
        [InlineData(1000, 3360)]
        public void A01_FullyAbsorbedNativeGroupHit_RecordsEveryTargetAndCapsEnergy(int hit, int energy)
        {
            var sim = ShieldFixture(hit, "A01");
            var hp = sim.Allies.Select(u => u.Hp).ToArray();
            EnemyGroupAction(sim);
            var results = sim.ExpeditionResolutions.Where(r => !r.SourceAlly && r.Origin == ResolutionOrigin.Native && r.RequestedDamage > 0).ToArray();
            Assert.Equal(5, results.Length);
            Assert.All(results, r => { Assert.Equal(hit, r.RequestedDamage); Assert.Equal(hit, r.ShieldAbsorbed); Assert.Equal(0, r.EffectiveHpDamage); });
            Assert.Equal(hp, sim.Allies.Select(u => u.Hp).ToArray());
            Assert.Equal(1120, sim.ExpeditionRelics.Threshold);
            Assert.Equal(energy, sim.ExpeditionRelics.BarrierEnergy);
            Assert.Equal(hit * 5L, sim.ExpeditionTotals.AllyShieldAbsorbed);
        }

        [Fact]
        public void A04_ReplacesBaseShock_AndA03UsesComputedMainForceAfterNativeHeal()
        {
            var sim = ShieldFixture(449, "A01", "A03", "A04");
            EnemyGroupAction(sim);
            Active(sim, 2); // Successful full-health healing still completes an active skill.
            var hits = sim.ExpeditionResolutions.Where(r => r.Origin == ResolutionOrigin.Derived).ToArray();
            Assert.Equal(3, hits.Length);
            Assert.Single(hits, r => r.SourceRelicId == "A04");
            Assert.DoesNotContain(hits, r => r.SourceRelicId == "A01");
            Assert.Equal(4480, hits[0].RequestedDamage);
            Assert.All(hits.Skip(1), r => { Assert.Equal("A03", r.SourceRelicId); Assert.Equal(2240, r.RequestedDamage); });
            Assert.Equal(3, hits.Select(r => r.TargetSlot).Distinct().Count());
            Assert.All(hits, r => { Assert.Equal(1, r.ProcDepth); Assert.True(r.SourceAlly); Assert.Equal(hits[0].RootActionId, r.RootActionId); });
            Assert.Equal(5, sim.ExpeditionRelics.BarrierEnergy);
            Assert.Equal(0, sim.ExpeditionTotals.EffectiveHeal);
            Assert.True(sim.ExpeditionTotals.Overheal > 0);
        }

        [Fact]
        public void ShieldReplacementExpiryAndDotAbsorption_DoNotChargeA01()
        {
            var sim = ShieldFixture(224, "A01");
            Active(sim, 1);
            Assert.Equal(0, sim.ExpeditionRelics.BarrierEnergy);
            var poison = new EffectDef { Id = "fixture-periodic", Group = "fixture-dot", Opcode = EffectOpcodes.PoisonApply,
                Kind = EffectKind.Poison, Magnitude = 0.05f, DurationSec = 0.08f, MaxStack = 1,
                Trigger = EffectDef.TriggerPeriodic, PeriodSec = BattleSim.TickDt };
            sim.ApplyStatus(sim.Allies[0], poison);
            sim.Tick();
            var dot = Assert.Single(sim.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Dot);
            Assert.True(dot.ShieldAbsorbed > 0);
            Assert.Equal(-1, dot.SourceSlot);
            Assert.Equal(0, sim.ExpeditionRelics.BarrierEnergy);
            for (var i = 0; i < 260; i++) sim.Tick();
            Assert.All(sim.Allies, u => Assert.Equal(0, u.Shield));
            Assert.Equal(0, sim.ExpeditionRelics.BarrierEnergy);
        }

        [Fact]
        public void NativeOverkillAndFullHealthHealing_DoNotInflateEffectiveContribution()
        {
            var input = RunBattleFactory.CreateInput("N1", "single", 7771, Array.Empty<string>(), null);
            input.Characters.Single(c => c.Id == "OE_CHORUS").Hp = 1;
            var sim = new BattleSim(input);
            sim.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture));
            Active(sim, 0);
            var damage = Assert.Single(sim.ExpeditionResolutions, r => r.RequestedDamage > 0);
            Assert.True(damage.RequestedDamage > 1);
            Assert.Equal(1, damage.EffectiveHpDamage);
            Assert.Equal(damage.RequestedDamage - 1, damage.Overkill);
            Assert.True(damage.NativeKill);
            Active(sim, 2);
            Assert.Equal(0, sim.ExpeditionTotals.AllyEffectiveHealing);
            Assert.Equal(1, sim.ExpeditionTotals.EnemyEffectiveDamage);
            Assert.True(sim.ExpeditionTotals.Overheal > 0);
        }
    }
}
