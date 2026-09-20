using System;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Explicit boundary fixtures, not ordinary-play evidence. Inputs are isolated and validated;
    // HP/timer setup and ApplyStatus establish the edge, then real Tick/Submit performs settlement.
    public sealed class OriginalRuleBoundaryTests
    {
        [Fact]
        public void PeriodicDotBossDeathSettlesBeforeAReadyMaskCanAttack()
        {
            var input = RunBattleFactory.CreateInput("N7", "single", 71403, Array.Empty<string>(), null);
            var dot = new EffectDef
            {
                Id = "OE_BOUNDARY_PERIODIC", Opcode = EffectOpcodes.PoisonApply, Kind = EffectKind.Poison,
                Magnitude = 0.001f, DurationSec = 10f, MaxStack = 1, SourceTier = 1,
                Group = "OE_BOUNDARY_PERIODIC", Trigger = EffectDef.TriggerPeriodic,
                PeriodSec = BattleSim.TickDt
            };
            input.Effects = input.Effects.Concat(new[] { dot }).ToArray();
            RunBattleFactory.Validate(input);
            var sim = RunBattleFactory.Create(input);
            var boss = sim.Enemies[0];
            boss.Hp = 1;
            foreach (var ally in sim.Allies) { ally.AutoTimer = 0; ally.Charge = 0; }
            foreach (var enemy in sim.Enemies) { enemy.AutoTimer = 0; enemy.Charge = 0; }
            sim.Enemies[1].AutoTimer = 3f; // A surviving mask would auto-attack on this very tick.
            sim.ApplyStatus(boss, dot); // Explicit unknown-source fixture; do not invent ally credit.
            var partyHp = sim.Allies.Select(u => u.Hp).ToArray();
            var maskHp = sim.Enemies.Skip(1).Select(u => u.Hp).ToArray();

            sim.Tick();

            var resolvedDot = Assert.Single(sim.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Dot);
            Assert.Equal(0, resolvedDot.TargetSlot);
            Assert.False(resolvedDot.TargetAlly);
            Assert.Equal(boss.InstanceGeneration, resolvedDot.TargetGeneration);
            Assert.Equal(-1, resolvedDot.SourceSlot);
            Assert.True(resolvedDot.RootActionId > 0);
            Assert.Equal(1, resolvedDot.EffectiveHpDamage);
            Assert.Equal(resolvedDot.RequestedDamage - 1, resolvedDot.Overkill);
            Assert.True(resolvedDot.Killed);
            Assert.False(resolvedDot.NativeKill);
            Assert.False(boss.Alive);
            Assert.All(sim.Enemies.Skip(1), mask => Assert.True(mask.Alive));
            Assert.DoesNotContain(sim.ExpeditionResolutions,
                r => r.Origin == ResolutionOrigin.Native && !r.SourceAlly && r.RequestedDamage > 0);
            Assert.Empty(sim.Casts);
            Assert.Equal(partyHp, sim.Allies.Select(u => u.Hp).ToArray());
            Assert.Equal(maskHp, sim.Enemies.Skip(1).Select(u => u.Hp).ToArray());
            Assert.Equal(0, sim.ExpeditionTotals.AllyEffectiveDamageTaken);
            Assert.Equal(0, sim.ExpeditionTotals.EnemyEffectiveDamage);
            Assert.Equal(BattleOutcome.Victory, sim.Outcome);

            var count = sim.ExpeditionResolutions.Count;
            var tick = sim.TickIndex;
            sim.Tick();
            Assert.Equal(BattleOutcome.Victory, sim.Outcome);
            Assert.Equal(tick, sim.TickIndex);
            Assert.Equal(count, sim.ExpeditionResolutions.Count);
            Assert.Equal(partyHp, sim.Allies.Select(u => u.Hp).ToArray());
        }

        [Fact]
        public void DefenseOverflowFailsBeforeDamageInsteadOfBecomingZeroDefense()
        {
            // The neighboring representable value remains playable: 200000 * (1 + 100 * 107).
            var safe = DefenseFixture(107);
            var safeTarget = safe.Enemies[0];
            var safeHp = safeTarget.Hp;
            safe.Allies[0].Charge = 100;
            Assert.True(safe.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture)).Accepted);
            Assert.True(safe.Submit(BattleCommand.Tap(0, CommandSource.Fixture)).Accepted);
            var safeHit = Assert.Single(safe.ExpeditionResolutions);
            Assert.Equal(ResolutionOrigin.Native, safeHit.Origin);
            Assert.True(safeHit.SourceAlly);
            Assert.Equal(0, safeHit.SourceSlot);
            Assert.Equal(0, safeHit.TargetSlot);
            Assert.Equal(1, safeHit.RequestedDamage);
            Assert.Equal(safeHp - 1, safeTarget.Hp);
            Assert.Equal(BattleOutcome.InProgress, safe.Outcome);

            var overflow = DefenseFixture(108);
            var target = overflow.Enemies[0];
            var stacks = Assert.Single(target.Status, s => s.Def.Kind == EffectKind.DefBuff).Stacks;
            var exactDefense = target.Def.Def * (1d + 100d * stacks);
            Assert.Equal(108, stacks);
            Assert.Equal(2160200000d, exactDefense);
            Assert.True(exactDefense > int.MaxValue);
            var hp = target.Hp;
            overflow.Allies[0].Charge = 100;
            Assert.True(overflow.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture)).Accepted);

            var attempt = overflow.Submit(BattleCommand.Tap(0, CommandSource.Fixture));

            Assert.Equal(BattleOutcome.Failed, overflow.Outcome);
            Assert.False(attempt.Accepted);
            Assert.StartsWith("ORIGINAL_RULE_ERROR ", overflow.FailedReason);
            Assert.Equal(hp, target.Hp);
            Assert.Empty(overflow.ExpeditionResolutions);
            Assert.Empty(overflow.Casts);
            Assert.Equal(0, overflow.ExpeditionTotals.ResultsCount);
            Assert.Equal(0, overflow.ExpeditionTotals.RequestedDamage);
            Assert.Equal(0, overflow.ExpeditionTotals.EnemyEffectiveDamage);
            var reason = overflow.FailedReason;
            var rejected = overflow.Submit(BattleCommand.Tap(0, CommandSource.Fixture));
            Assert.False(rejected.Accepted);
            Assert.Equal(CommandReject.NotInProgress, rejected.Reason);
            overflow.Tick();
            Assert.Equal(BattleOutcome.Failed, overflow.Outcome);
            Assert.Equal(reason, overflow.FailedReason);
            Assert.Equal(hp, target.Hp);
        }

        [Theory]
        [InlineData(false)] // Finite ATK has just crossed the supported Int32 domain.
        [InlineData(true)] // Explicitly corrupted live status, not valid authored content.
        public void InvalidScaledAttackFailsBeforeHealingCanPublishAResult(bool nan)
        {
            var input = RunBattleFactory.CreateInput("N1", "single", 71405, Array.Empty<string>(), null);
            input.Mods.FoodAtkMul = 10f;
            input.Characters.Single(c => c.Id == input.PartyIds[2]).Atk = 20000;
            var heal = input.Skills.Single(s => s.Id == "OE_HEALER_tap");
            Assert.Equal(0f, heal.HealCoef);
            Assert.True(heal.HealMaxHpFrac > 0);
            var buff = new EffectDef
            {
                Id = "OE_BOUNDARY_ATTACK", Opcode = EffectOpcodes.StatusApply, Kind = EffectKind.AtkBuff,
                Magnitude = 100f, DurationSec = 3600f, MaxStack = 108, SourceTier = 1,
                Group = "OE_BOUNDARY_ATTACK"
            };
            input.Effects = input.Effects.Concat(new[] { buff }).ToArray();
            RunBattleFactory.Validate(input);
            var sim = RunBattleFactory.Create(input);
            var healer = sim.Allies[2];
            Assert.Equal(200000, healer.Def.Atk);
            for (int i = 0; i < (nan ? 1 : 108); i++) sim.ApplyStatus(healer, buff);
            if (nan) buff.Magnitude = float.NaN;
            else Assert.Equal(2160200000d, healer.Def.Atk * (1d + 100d * 108));
            sim.Allies[0].Hp -= 100;
            var hp = sim.Allies.Select(u => u.Hp).ToArray();
            healer.Charge = 100;

            var attempted = sim.Submit(BattleCommand.Tap(2, CommandSource.Fixture));

            Assert.Equal(BattleOutcome.Failed, sim.Outcome);
            Assert.False(attempted.Accepted);
            Assert.StartsWith("ORIGINAL_RULE_ERROR ", sim.FailedReason);
            Assert.Equal(hp, sim.Allies.Select(u => u.Hp).ToArray());
            Assert.Empty(sim.ExpeditionResolutions);
            Assert.Empty(sim.Casts);
            Assert.Equal(0, sim.ExpeditionTotals.RequestedHeal);
            Assert.Equal(0, sim.ExpeditionTotals.EffectiveHeal);
            Assert.Equal(0, sim.ExpeditionTotals.ResultsCount);
        }

        static BattleSim DefenseFixture(int stacks)
        {
            var input = RunBattleFactory.CreateInput("N1", "single", 71404, Array.Empty<string>(), null);
            input.Stage.EnemyDefMul = 10f;
            input.Characters.Single(c => c.Id == input.Stage.Wave0[0]).Def = 20000;
            var buff = new EffectDef
            {
                Id = "OE_BOUNDARY_DEFENSE", Opcode = EffectOpcodes.StatusApply, Kind = EffectKind.DefBuff,
                Magnitude = 100f, DurationSec = 3600f, MaxStack = 108, SourceTier = 1,
                Group = "OE_BOUNDARY_DEFENSE"
            };
            input.Effects = input.Effects.Concat(new[] { buff }).ToArray();
            RunBattleFactory.Validate(input); // Both numerical configurations are currently valid inputs.
            var sim = RunBattleFactory.Create(input);
            Assert.Equal(200000, sim.Enemies[0].Def.Def);
            for (int i = 0; i < stacks; i++) sim.ApplyStatus(sim.Enemies[0], buff);
            Assert.Equal(BattleOutcome.InProgress, sim.Outcome);
            return sim;
        }
    }
}
