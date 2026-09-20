using System;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Controlled timing/boundary fixtures. These are not natural-play or strategy acceptance footage.
    public sealed class OriginalEncounterIntegrationTests
    {
        static BattleSim Boss()
        {
            var input = RunBattleFactory.CreateInput("N7", "single", 889104, Array.Empty<string>(), null);
            foreach (var c in input.Characters) c.Crt = 0;
            var sim = new BattleSim(input);
            foreach (var ally in sim.Allies) ally.AutoTimer = -10000;
            foreach (var enemy in sim.Enemies) { enemy.AutoTimer = 1000; enemy.Charge = 100; }
            return sim;
        }

        static void Ticks(BattleSim sim, int ticks) { for (int i = 0; i < ticks; i++) sim.Tick(); }
        static void Tap(BattleSim sim, int slot)
        {
            sim.Allies[slot].Charge = 100;
            var result = sim.Submit(BattleCommand.Tap(slot, CommandSource.Fixture));
            Assert.True(result.Accepted, result.Reason + " " + sim.FailedReason);
        }

        [Fact]
        public void BossOwnsAllThreeSlots_UsesOneWorldClock_AndPauseFreezesIntent()
        {
            var sim = Boss();
            Ticks(sim, 89);
            Assert.Empty(sim.Casts);
            sim.Tick();
            Assert.Single(sim.Casts);
            Ticks(sim, 180);
            Assert.True(sim.OriginalEncounter.IsCasting);
            Assert.Equal(2, sim.Casts.Count);
            Assert.All(sim.Casts, c => { Assert.False(c.CasterAlly); Assert.Equal(0, c.CasterSlot); });
            var left = sim.OriginalEncounter.RemainingCastSec;
            var time = sim.TimeLeft; var tick = sim.TickIndex;
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause, Source = CommandSource.Fixture }).Accepted);
            Ticks(sim, 100);
            Assert.Equal(tick, sim.TickIndex);
            Assert.Equal(time, sim.TimeLeft);
            Assert.Equal(left, sim.OriginalEncounter.RemainingCastSec);
            sim.Submit(new BattleCommand { Kind = BattleCommandKind.Resume, Source = CommandSource.Fixture });
            Ticks(sim, 119);
            Assert.Equal(2, sim.Casts.Count);
            sim.Tick();
            Assert.Equal(1, sim.OriginalEncounter.AreaCasts);
            Assert.Equal(3, sim.Casts.Count);
            var area = sim.ExpeditionResolutions.Where(r => r.SkillId == "OE_BOSS_echo").ToArray();
            Assert.Equal(5, area.Length);
            Assert.All(area, r => { Assert.Equal(0, r.SourceSlot); Assert.False(r.SourceAlly); Assert.True(r.RequestedDamage > 0); });
            Assert.Equal(5, area.Select(r => r.TargetSlot).Distinct().Count());
        }

        [Fact]
        public void RemovingMaskDuringCastChangesActualAreaDamageAtResolution()
        {
            var two = Boss(); var one = Boss();
            Ticks(two, 270); Ticks(one, 270);
            Assert.True(one.OriginalEncounter.IsCasting);
            one.Enemies[1].Hp = 0; // Deliberately isolated live-mask multiplier boundary.
            Assert.Equal(2f, two.OriginalIntentSnapshot.AreaMultiplier);
            Assert.Equal(1.5f, one.OriginalIntentSnapshot.AreaMultiplier);
            Ticks(two, 120); Ticks(one, 120);
            var strong = two.ExpeditionResolutions.Where(r => r.SkillId == "OE_BOSS_echo").ToArray();
            var weak = one.ExpeditionResolutions.Where(r => r.SkillId == "OE_BOSS_echo").ToArray();
            Assert.Equal(5, strong.Length); Assert.Equal(5, weak.Length);
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(strong[i].TargetSlot, weak[i].TargetSlot);
                Assert.InRange(Math.Abs(strong[i].RequestedDamage * 0.75 - weak[i].RequestedDamage), 0, 1.1);
                Assert.True(weak[i].RequestedDamage < strong[i].RequestedDamage);
            }
        }

        [Fact]
        public void SpeedScalesTheEncounterClockOnce_AndPolicyHoldDoesNotAdvanceIt()
        {
            var normal = Boss(); var fast = Boss();
            Assert.True(fast.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 2, Source = CommandSource.Fixture }).Accepted);
            Ticks(normal, 270); Ticks(fast, 135);
            Assert.True(normal.OriginalEncounter.IsCasting && fast.OriginalEncounter.IsCasting);
            Assert.InRange(Math.Abs(normal.OriginalEncounter.ElapsedSec - fast.OriginalEncounter.ElapsedSec), 0, 0.00001);
            var left = normal.OriginalEncounter.RemainingCastSec;
            normal.FixtureHold(true); fast.FixtureHold(true);
            Ticks(normal, 20); Ticks(fast, 20);
            Assert.Equal(left, normal.OriginalEncounter.RemainingCastSec);
            Assert.Equal(left, fast.OriginalEncounter.RemainingCastSec);
            normal.FixtureHold(false); fast.FixtureHold(false);
            Ticks(normal, 120); Ticks(fast, 60);
            Assert.Equal(1, normal.OriginalEncounter.AreaCasts);
            Assert.Equal(1, fast.OriginalEncounter.AreaCasts);
            Assert.Equal(normal.ExpeditionResolutions.Select(r => r.RequestedDamage), fast.ExpeditionResolutions.Select(r => r.RequestedDamage));
        }

        [Fact]
        public void HalfHealthDuringCastDefersOnePhaseChange_AndRebuildsOnlyDeadMask()
        {
            var sim = Boss();
            Ticks(sim, 270);
            sim.Enemies[1].Hp = 0;
            sim.Enemies[2].Hp -= 100;
            var aliveHp = sim.Enemies[2].Hp;
            var deadInstance = sim.Enemies[1];
            sim.Enemies[0].Hp = sim.Enemies[0].MaxHp / 2;
            Tap(sim, 1); // A real action establishes the stable boundary after explicit HP fixture setup.
            Assert.True(sim.OriginalEncounter.PhasePending);
            Assert.True(sim.OriginalEncounter.IsCasting);
            Assert.Equal(1, sim.Enemies[1].InstanceGeneration);
            Assert.False(sim.Enemies[1].Alive);
            var bossHp = sim.Enemies[0].Hp;
            Ticks(sim, 120);
            Assert.Equal(1, sim.OriginalEncounter.AreaCasts);
            Assert.Equal(2, sim.OriginalEncounter.Phase);
            Assert.False(sim.OriginalEncounter.PhasePending);
            Assert.True(sim.Enemies[1].Alive);
            Assert.NotSame(deadInstance, sim.Enemies[1]);
            Assert.Equal(2, sim.Enemies[1].InstanceGeneration);
            Assert.Equal(1, sim.Enemies[2].InstanceGeneration);
            Assert.Equal(aliveHp, sim.Enemies[2].Hp);
            Assert.Equal(bossHp, sim.Enemies[0].Hp);
            Tap(sim, 1);
            Assert.Equal(2, sim.Enemies[1].InstanceGeneration);
            Assert.Equal(1, sim.OriginalEncounter.MaskRebuildCount);
        }

        [Fact]
        public void BossAndLastAllyDyingInOneRootIsVictory_AndCancelsPendingIntent()
        {
            var sim = Boss();
            Ticks(sim, 270);
            for (int i = 1; i < sim.Allies.Length; i++) sim.Allies[i].Hp = 0;
            sim.Enemies[0].Hp = 1;
            sim.ApplyStatus(sim.Allies[0], new EffectDef { Id = "fixture-last-action-poison", Group = "fixture-poison",
                Opcode = EffectOpcodes.PoisonApply, Kind = EffectKind.Poison, Magnitude = 1,
                MaxStack = 1, DurationSec = 10, Trigger = EffectDef.TriggerOnAction });
            sim.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture));
            Tap(sim, 0);
            Assert.Equal(BattleOutcome.Victory, sim.Outcome);
            Assert.All(sim.Allies, u => Assert.False(u.Alive));
            Assert.True(sim.Enemies[1].Alive && sim.Enemies[2].Alive);
            Assert.True(sim.OriginalEncounter.Cancelled);
            Assert.False(sim.OriginalEncounter.IsCasting);
            var resultCount = sim.ExpeditionResolutions.Count; var casts = sim.Casts.Count;
            Ticks(sim, 300);
            Assert.Equal(resultCount, sim.ExpeditionResolutions.Count);
            Assert.Equal(casts, sim.Casts.Count);
            Assert.Equal(0, sim.OriginalEncounter.AreaCasts);
        }

        [Fact]
        public void RuleFailureLaterInTheSameNativeActionHasPriorityOverBossDeath()
        {
            var input = RunBattleFactory.CreateInput("N7", "single", 889104, Array.Empty<string>(), null);
            input.Characters.Single(c => c.Id == "OE_BOSS").Hp = 1;
            input.Characters.Single(c => c.Id == "OE_MASK").Def = 20000;
            input.Stage.EnemyDefMul = 10;
            input.Skills.Single(s => s.Id == "OE_POINT_tap").HitCount = 2;
            var sim = new BattleSim(input);
            foreach (var mask in sim.Enemies.Skip(1))
                mask.Status.Add(new StatusInst { Def = new EffectDef { Id = "fixture-overflow-defense", Kind = EffectKind.DefBuff,
                    Magnitude = 100, DurationSec = 60 }, Stacks = 108, Remaining = 60 });
            sim.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture));
            sim.Allies[0].Charge = 100;
            var result = sim.Submit(BattleCommand.Tap(0, CommandSource.Fixture));
            Assert.False(result.Accepted);
            Assert.False(sim.Enemies[0].Alive);
            Assert.Equal(BattleOutcome.Failed, sim.Outcome);
            Assert.Contains("OverflowException", sim.FailedReason);
            Assert.True(sim.OriginalEncounter.Cancelled);
            Assert.Contains(sim.ExpeditionResolutions, r => r.NativeKill && !r.TargetAlly && r.TargetSlot == 0);
            Assert.DoesNotContain(sim.Events.Events, e => e.Kind == "result");
        }
    }
}
