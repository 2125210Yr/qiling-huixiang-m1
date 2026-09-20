using System;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// Controlled real-simulation fixtures. Readiness, HP and status changes explicitly model command
    /// boundaries; every resumed action still uses Submit. These are not ordinary-play evidence.
    /// </summary>
    public sealed class OriginalExpeditionQueueTests
    {
        [Fact]
        public void QueueReplacementPreservesOrder_ClearThenReinsertMovesToEnd_AndSnapshotsAreStable()
        {
            var sim = CreateReady();
            Pause(sim);
            Focus(sim, 0);
            Enqueue(sim, 0);
            Enqueue(sim, 1);
            Enqueue(sim, 2);
            Enqueue(sim, 3);
            Enqueue(sim, 4);
            var first = sim.ExpeditionQueue;
            var logCount = sim.CommandLog.Count;
            Focus(sim, 1);
            Enqueue(sim, 0);

            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, sim.ExpeditionQueue.Select(c => c.ActorSlot));
            Assert.Equal(1, sim.ExpeditionQueue[0].RequiredEnemySlot);
            Assert.Equal(sim.Enemies[1].InstanceGeneration, sim.ExpeditionQueue[0].RequiredEnemyGeneration);
            Assert.Equal(0, first[0].RequiredEnemySlot);
            Assert.Equal(CommandReject.None, sim.ClearExpeditionQueuedCommand(1));
            Enqueue(sim, 1);
            Assert.Equal(new[] { 0, 2, 3, 4, 1 }, sim.ExpeditionQueue.Select(c => c.ActorSlot));
            Assert.Equal(logCount + 1, sim.CommandLog.Count); // Only the explicit focus added a log record.
            Assert.Empty(sim.Casts);
            Assert.All(sim.Allies, a => Assert.Equal(100f, a.Charge));
            Assert.Equal(5, first.Count);
        }

        [Fact]
        public void PausedQueueEditingAndTicks_DoNotAdvanceAnyCapturedBattleState()
        {
            var sim = CreateReady();
            sim.Allies[0].SlideCd = 5;
            AddStatus(sim, 0, EffectKind.AtkBuff);
            Pause(sim);
            var before = BattleStateDigest.Of(sim).ToCanonicalString();
            Enqueue(sim, 0);
            Enqueue(sim, 1);
            Assert.Equal(CommandReject.None, sim.ClearExpeditionQueuedCommand(0));
            for (var i = 0; i < 300; i++) sim.Tick();

            Assert.True(sim.Paused);
            Assert.Equal(before, BattleStateDigest.Of(sim).ToCanonicalString());
            Assert.Empty(sim.Casts);
        }

        [Fact]
        public void ResumeIsLoggedBeforeQueuedActions_AndAllDrainBeforeTheNextTick()
        {
            var sim = CreateReady();
            Pause(sim);
            Enqueue(sim, 1);
            Enqueue(sim, 0);
            Enqueue(sim, 4);
            var start = sim.CommandLog.Count;
            var tick = sim.TickIndex;
            var resume = Resume(sim);
            var drained = sim.CommandLog.Skip(start).ToArray();

            Assert.False(sim.Paused);
            Assert.Equal(new[] { BattleCommandKind.Resume, BattleCommandKind.Tap,
                BattleCommandKind.Tap, BattleCommandKind.Tap }, drained.Select(c => c.Kind));
            Assert.Equal(new[] { 1, 0, 4 }, drained.Skip(1).Select(c => c.Slot));
            Assert.Equal(resume.Seq, drained[0].Seq);
            Assert.All(drained, c => { Assert.True(c.Accepted); Assert.Equal(tick, c.Tick); });
            Assert.Equal(new[] { 1, 0, 4 }, sim.Casts.Where(c => c.CasterAlly && c.Type == SkillType.Tap)
                .Select(c => c.CasterSlot));
            Assert.Equal(tick, sim.TickIndex);
            Assert.Empty(sim.ExpeditionQueue);
            Assert.Equal(drained.Skip(1).Select(c => c.Seq), sim.ExpeditionQueueResults.Select(c => c.Result.Seq));
            sim.Tick();
            Assert.Equal(tick + 1, sim.TickIndex);
        }

        [Theory]
        [InlineData(CommandReject.NotCharged)]
        [InlineData(CommandReject.UnitDead)]
        [InlineData(CommandReject.Silenced)]
        [InlineData(CommandReject.ActionLocked)]
        public void ResumeRevalidatesCurrentActorState_RejectsWithoutBlockingIndependentCommands(CommandReject reason)
        {
            var sim = CreateReady();
            Pause(sim);
            Enqueue(sim, 0);
            Enqueue(sim, 1);
            // Explicit changed boundary AFTER queuing proves this is not merely entry-time validation.
            if (reason == CommandReject.NotCharged) sim.Allies[0].Charge = 1;
            if (reason == CommandReject.UnitDead) sim.Allies[0].Hp = 0;
            if (reason == CommandReject.Silenced) AddStatus(sim, 0, EffectKind.Silence);
            if (reason == CommandReject.ActionLocked) AddStatus(sim, 0, EffectKind.Stun);
            Resume(sim);

            Assert.Equal(2, sim.ExpeditionQueueResults.Count);
            Assert.False(sim.ExpeditionQueueResults[0].Result.Accepted);
            Assert.Equal(reason, sim.ExpeditionQueueResults[0].Result.Reason);
            Assert.True(sim.ExpeditionQueueResults[1].Result.Accepted);
            Assert.Equal(1, sim.ExpeditionQueueResults[1].ActorSlot);
            Assert.Empty(sim.ExpeditionQueue);
            Assert.DoesNotContain(sim.Casts, c => c.CasterAlly && c.CasterSlot == 0);
            Assert.Contains(sim.CommandLog, c => c.Kind == BattleCommandKind.Tap && c.Slot == 0 && !c.Accepted && c.Reason == reason);
        }

        [Fact]
        public void NotReadyAtResume_IsConsumedWithoutRefillOrAutomaticRetryWhenChargeLaterBecomesReady()
        {
            var sim = CreateReady();
            sim.Allies[0].Charge = 0;
            Pause(sim);
            Enqueue(sim, 0); // Not-ready commands may be planned, but readiness is never manufactured.
            Resume(sim);
            Assert.Equal(CommandReject.NotCharged, sim.ExpeditionQueueResults.Single().Result.Reason);
            Assert.Equal(0f, sim.Allies[0].Charge);
            var count = sim.CommandLog.Count;
            for (var i = 0; i < BattleSim.TickHz * 12; i++) sim.Tick();

            Assert.Equal(100f, sim.Allies[0].Charge);
            Assert.Equal(count, sim.CommandLog.Count);
            Assert.DoesNotContain(sim.Casts, c => c.CasterAlly && c.Type == SkillType.Tap && c.CasterSlot == 0);
            Assert.Empty(sim.ExpeditionQueue);
        }

        [Fact]
        public void EarlierQueuedKill_InvalidatesLaterBoundTarget_WhileSupportAndGlobalFocusRemainIndependent()
        {
            var sim = CreateReady();
            sim.Enemies[0].Hp = 1; // Declared kill boundary; the first real skill supplies the kill.
            Pause(sim);
            Focus(sim, 0);
            Enqueue(sim, 0);
            Enqueue(sim, 3);
            Enqueue(sim, 2);
            Assert.Equal(0, sim.ExpeditionQueue[2].RequiredEnemyGeneration); // Heal does not bind a foe.
            Focus(sim, 1);
            Resume(sim);

            Assert.False(sim.Enemies[0].Alive);
            Assert.True(sim.Enemies[1].Alive);
            Assert.Equal(new[] { CommandReject.None, CommandReject.TargetInvalid, CommandReject.None },
                sim.ExpeditionQueueResults.Select(c => c.Result.Reason));
            Assert.Equal(100f, sim.Allies[3].Charge);
            Assert.Equal(1, sim.FocusEnemySlot);
            Assert.DoesNotContain(sim.ExpeditionResolutions, r => r.SourceAlly && r.SourceSlot == 3);
            var rejected = sim.CommandLog.Last(c => c.Kind == BattleCommandKind.Tap && c.Slot == 3);
            Assert.Equal(0, rejected.RequiredEnemySlot);
            Assert.Equal(1, rejected.RequiredEnemyGeneration);
            Assert.Equal(CommandReject.TargetInvalid, rejected.Reason);
        }

        [Fact]
        public void SameSlotWithANewGeneration_IsRejectedWithoutDamagingTheReplacement()
        {
            var sim = CreateReady();
            Pause(sim);
            Focus(sim, 0);
            Enqueue(sim, 0);
            Enqueue(sim, 1);
            var hp = sim.Enemies[0].Hp;
            sim.Enemies[0].InstanceGeneration++; // Models a mask being rebuilt into the same slot.
            Resume(sim);

            Assert.Equal(CommandReject.TargetInvalid, sim.ExpeditionQueueResults[0].Result.Reason);
            Assert.True(sim.ExpeditionQueueResults[1].Result.Accepted);
            Assert.Equal(hp, sim.Enemies[0].Hp);
            Assert.Equal(100f, sim.Allies[0].Charge);
            Assert.All(sim.ExpeditionResolutions, r => Assert.Equal(0, r.RequestedDamage));
        }

        [Fact]
        public void OnlySingleEnemyDamageBindsFocus_SupportSweepAndUnfocusedCommandsDoNot()
        {
            var sim = CreateReady("sweep");
            Pause(sim);
            Enqueue(sim, 0);
            Assert.Equal(0, sim.ExpeditionQueue[0].RequiredEnemyGeneration);
            Focus(sim, 0);
            Enqueue(sim, 0);
            Enqueue(sim, 1);
            Enqueue(sim, 2);
            Enqueue(sim, 3);
            Enqueue(sim, 4);
            Assert.True(sim.ExpeditionQueue.Single(q => q.ActorSlot == 3).RequiredEnemyGeneration > 0);
            Assert.All(sim.ExpeditionQueue.Where(q => q.ActorSlot != 3), q => Assert.Equal(0, q.RequiredEnemyGeneration));
        }

        [Fact]
        public void ClearForExitRemovesPendingAndPreviousResults_AndQueueApisRejectWrongStateOrSlot()
        {
            var sim = CreateReady();
            Assert.Equal(CommandReject.InvalidValue, sim.QueueExpeditionTap(0));
            Pause(sim);
            Assert.Equal(CommandReject.SlotInvalid, sim.QueueExpeditionTap(-1));
            Assert.Equal(CommandReject.SlotInvalid, sim.QueueExpeditionTap(5));
            Enqueue(sim, 1);
            Resume(sim);
            Assert.NotEmpty(sim.ExpeditionQueueResults);
            Pause(sim);
            Enqueue(sim, 0);
            sim.ClearExpeditionQueue();
            Assert.Empty(sim.ExpeditionQueue);
            Assert.Empty(sim.ExpeditionQueueResults);
            var before = sim.CommandLog.Count;
            Resume(sim);
            Assert.Equal(before + 1, sim.CommandLog.Count);
        }

        [Fact]
        public void ScopedTargetRestoresEvenWhenTheActionThrows_AndUnboundTapUsesGlobalFocus()
        {
            var sim = CreateReady();
            Focus(sim, 1);
            var bound = BattleCommand.Tap(0);
            bound.RequiredEnemySlot = 0;
            bound.RequiredEnemyGeneration = sim.Enemies[0].InstanceGeneration;
            Assert.Throws<InvalidOperationException>(() => sim.RunWithExpeditionTarget(bound,
                () => throw new InvalidOperationException("controlled scope exception")));
            Assert.True(sim.Submit(BattleCommand.Tap(0, CommandSource.Fixture)).Accepted);
            Assert.Equal(1, sim.FocusEnemySlot);
            Assert.All(sim.ExpeditionResolutions.Where(r => r.RequestedDamage > 0), r => Assert.Equal(1, r.TargetSlot));
        }

        static BattleSim CreateReady(string preset = "single")
        {
            var input = RunBattleFactory.CreateInput("N4", preset, 260921, new[] { "A01" }, null);
            input.RunId = "queue-integration-fixture";
            input.EncounterId = "queue-integration-fixture/N4/1";
            input.AttemptId = "controlled-boundaries";
            input.Stage.Wave0 = new[] { "OE_PROMPTER", "OE_PROMPTER" };
            input.Characters.Single(c => c.Id == "OE_PROMPTER").Hp = 1000000;
            input.Characters.Single(c => c.Id == "OE_PROMPTER").Atk = 1;
            var sim = RunBattleFactory.Create(input);
            foreach (var ally in sim.Allies) ally.Charge = 100; // Explicit readiness boundary fixture.
            return sim;
        }

        static void AddStatus(BattleSim sim, int slot, EffectKind kind) => sim.Allies[slot].Status.Add(new StatusInst
        {
            Def = new EffectDef { Id = "queue_boundary_" + kind, Kind = kind, DurationSec = 60, Magnitude = 0.1f },
            Remaining = 60
        });
        static void Enqueue(BattleSim sim, int slot) => Assert.Equal(CommandReject.None, sim.QueueExpeditionTap(slot));
        static void Focus(BattleSim sim, int slot) => Assert.True(sim.Submit(BattleCommand.FocusEnemy(slot, CommandSource.Fixture)).Accepted);
        static void Pause(BattleSim sim) => Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause, Source = CommandSource.Fixture }).Accepted);
        static CommandResult Resume(BattleSim sim)
        {
            var result = sim.Submit(new BattleCommand { Kind = BattleCommandKind.Resume, Source = CommandSource.Fixture });
            Assert.True(result.Accepted);
            return result;
        }
    }
}
