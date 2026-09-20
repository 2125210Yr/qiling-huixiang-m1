using System;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Controlled boundary fixtures, not ordinary play: starting charge is deliberately set.
    public sealed class OriginalRelicIntegrationTests
    {
        static BattleSim Sim(params string[] relics)
        {
            var input = RunBattleFactory.CreateInput("N1", "single", 44071, relics, null);
            input.Stage.EnemyHpMul = 10f; // Keep the reference native hit below remaining HP.
            return new BattleSim(input);
        }

        static void Active(BattleSim sim, int slot)
        {
            sim.Allies[slot].Charge = 100;
            Assert.True(sim.Submit(BattleCommand.Tap(slot, CommandSource.Fixture)).Accepted);
        }

        [Fact]
        public void A02_EnhancesTheActualLinkedTeamShieldExactlyOnce()
        {
            var ordinary = Sim("A01"); var reinforced = Sim("A01", "A02");
            Active(ordinary, 1); Active(reinforced, 1);
            for (var i = 0; i < 5; i++)
            {
                Assert.True(ordinary.Allies[i].Shield > 0);
                Assert.Equal((int)Math.Round(ordinary.Allies[i].Shield * 1.25, MidpointRounding.AwayFromZero), reinforced.Allies[i].Shield);
                Assert.Equal(ordinary.Allies[i].Hp, reinforced.Allies[i].Hp);
            }
        }

        [Theory]
        [InlineData(false, 0.4)]
        [InlineData(true, 0.7)]
        public void B01_B02_UseTheSameNativeHitWithoutSecondDefenseOrCrit(bool improved, double ratio)
        {
            var ordinary = Sim(); var scattered = improved ? Sim("B01", "B02") : Sim("B01");
            Assert.True(ordinary.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture)).Accepted);
            Assert.True(scattered.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture)).Accepted);
            var primary = ordinary.Enemies[0].Hp;
            var secondary = scattered.Enemies[1].Hp;
            Active(ordinary, 0); Active(scattered, 0);
            var requested = primary - ordinary.Enemies[0].Hp;
            Assert.True(requested > 0);
            Assert.Equal(ordinary.Enemies[0].Hp, scattered.Enemies[0].Hp);
            Assert.Equal((int)Math.Round(requested * ratio, MidpointRounding.AwayFromZero), secondary - scattered.Enemies[1].Hp);
            Assert.Single(scattered.Casts); // Derived damage is not another native cast.
        }

        [Theory]
        [InlineData(false, 12f)]
        [InlineData(true, 20f)]
        public void C01_C02_AdvanceTheNextEligibleAllyWithoutAutoCasting(bool improved, float bonus)
        {
            var sim = improved ? Sim("C01", "C02") : Sim("C01");
            sim.Allies[1].Charge = 100;
            sim.Allies[2].Hp = 0;
            sim.Allies[3].Charge = 40;
            Active(sim, 0);
            Assert.Equal(40 + bonus, sim.Allies[3].Charge);
            Assert.Equal(100f, sim.Allies[1].Charge);
            Assert.Single(sim.Casts);
            Assert.Single(sim.CommandLog);
        }

        [Fact]
        public void ScatterUsesSharedShieldHpSettlement_WithoutPoisonOrStunSideEffects()
        {
            var sim = Sim("B01", "B02", "B03", "B04");
            var target = sim.Enemies[1];
            sim.ApplyStatus(target, new EffectDef { Id = "fixture-shield", Group = "fixture-shield",
                Kind = EffectKind.Shield, Opcode = EffectOpcodes.ShieldApply, Magnitude = 0.001f, DurationSec = 10, MaxStack = 1 });
            sim.ApplyStatus(target, new EffectDef { Id = "fixture-poison", Group = "fixture-poison",
                Kind = EffectKind.Poison, Opcode = EffectOpcodes.PoisonApply, Magnitude = 0.1f, DurationSec = 10,
                MaxStack = 1, Trigger = EffectDef.TriggerOnHitTaken });
            sim.ApplyStatus(target, new EffectDef { Id = "fixture-stun", Group = "fixture-stun",
                Kind = EffectKind.Stun, Opcode = EffectOpcodes.ControlApply, DurationSec = 2, MaxStack = 1 });
            var hp = target.Hp; var shield = target.Shield;
            sim.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture));
            Active(sim, 0);
            var derived = Assert.Single(sim.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Derived && r.TargetSlot == 1);
            Assert.True(derived.RequestedDamage > shield, $"Scatter {derived.RequestedDamage}, fixture shield {shield}");
            Assert.Equal(shield, derived.ShieldAbsorbed);
            Assert.Equal(derived.RequestedDamage - shield, derived.EffectiveHpDamage);
            Assert.Equal(hp - derived.EffectiveHpDamage, target.Hp);
            Assert.Equal(0, target.Shield);
            Assert.DoesNotContain(sim.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Dot);
            Assert.Equal(2f, target.Status.Single(s => s.Def.Kind == EffectKind.Stun).Remaining);
            Assert.DoesNotContain(sim.ExpeditionResolutions, r => r.SourceRelicId == "B04");
            Assert.Single(sim.Casts);
        }
    }
}
