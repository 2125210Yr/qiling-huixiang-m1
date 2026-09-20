using System;
using System.Collections.Generic;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Pure scheduling fixtures; request recording is not evidence of real battle damage.
    public sealed class OriginalBossControllerTests
    {
        sealed class Rig
        {
            public readonly BossEncounterController Controller;
            public readonly UnitState[] Enemies = Enumerable.Range(0, 3).Select(i => new UnitState
            { Slot = i, Ally = false, Hp = 1000, MaxHp = 1000, InstanceGeneration = 1,
                Def = new CharacterDef { Id = i == 0 ? "OE_BOSS" : "OE_MASK", IsBoss = i == 0 } }).ToArray();
            public readonly List<EncounterRequest> Requests = new List<EncounterRequest>();
            public readonly List<double> Times = new List<double>();
            public readonly EncounterView View;
            public Action<EncounterRequest> DuringApply;
            public Rig(bool elite = false, BossEncounterDef boss = null)
            {
                Controller = elite ? new BossEncounterController(new EliteEncounterDef()) : new BossEncounterController(boss ?? new BossEncounterDef());
                View = new EncounterView { Enemies = Enemies, Outcome = BattleOutcome.InProgress };
            }
            public void Tick(int count, float dt = 1f / 30f)
            { for (int i = 0; i < count; i++) Controller.Advance(dt, () => View, Apply); }
            public void Boundary() => Controller.ObserveStableBoundary(() => View, Apply);
            void Apply(EncounterRequest request)
            {
                Requests.Add(request); Times.Add(Controller.ElapsedSec);
                if (request.Kind == EncounterRequestKind.RebuildMissingMasks)
                    for (int i = 0; i < request.MaskSlots.Length; i++)
                    {
                        int slot = request.MaskSlots[i]; var old = Enemies[slot];
                        if (old != null && old.Alive) continue;
                        Enemies[slot] = new UnitState { Slot = slot, Hp = 1000, MaxHp = 1000,
                            InstanceGeneration = request.ExpectedGenerations[i] + 1, Def = new CharacterDef { Id = request.MaskCharacterId } };
                    }
                DuringApply?.Invoke(request);
            }
        }

        [Fact]
        public void OwnsExactlyAuthoredSlots()
        {
            var boss = new Rig().Controller; var elite = new Rig(elite: true).Controller;
            Assert.Equal(new[] { 0, 1, 2 }, Enumerable.Range(-1, 6).Where(boss.OwnsEnemySlot));
            Assert.Equal(new[] { 0 }, Enumerable.Range(-1, 6).Where(elite.OwnsEnemySlot));
        }

        [Fact]
        public void BossClockHasTwoAutosThenVisibleFourSecondCastWithoutExtraAuto()
        {
            var r = new Rig(); r.Tick(269);
            Assert.Equal(2, r.Requests.Count); Assert.All(r.Requests, x => Assert.Equal(EncounterRequestKind.AutoAttack, x.Kind));
            r.Tick(1); Assert.True(r.Controller.IsCasting); Assert.Equal(1, r.Controller.IntentSerial);
            Assert.InRange(r.Controller.RemainingCastSec, 3.999f, 4.001f);
            r.Tick(119); Assert.Equal(2, r.Requests.Count);
            r.Tick(1); var area = r.Requests.Last();
            Assert.Equal(EncounterRequestKind.AreaAttack, area.Kind); Assert.Equal("OE_BOSS_echo", area.SkillId);
            Assert.Equal(2f, area.DamageMultiplier); Assert.Equal(1, r.Controller.AreaCasts);
            Assert.False(r.Controller.IsCasting); Assert.InRange(r.Controller.NextIntentSec, 9.999f, 10.001f);
            Assert.InRange(r.Controller.AutoRemainingSec, 2.999f, 3.001f);
        }

        [Fact]
        public void ActualCastUsesMasksStillAliveAtResolutionAndSnapshotUpdatesDuringCast()
        {
            var r = new Rig(); r.Tick(270); r.Enemies[1].Hp = 0;
            var state = r.Controller.Snapshot(r.View);
            Assert.Equal(1, state.AliveMasks); Assert.Equal(1.5f, state.AreaMultiplier); Assert.True(state.IsCasting);
            r.Enemies[2].Hp = 0; r.Tick(120);
            Assert.Equal(1f, r.Requests.Last().DamageMultiplier);
        }

        [Fact]
        public void PhaseChangeDuringCastWaitsThenRebuildsOnlyDeadMaskWithoutHealingSurvivor()
        {
            var r = new Rig(); r.Tick(300); var remaining = r.Controller.RemainingCastSec;
            r.Enemies[0].Hp = 500; r.Enemies[1].Hp = 0; r.Enemies[2].Hp = 123; r.Boundary();
            Assert.Equal(1, r.Controller.Phase); Assert.True(r.Controller.PhasePending);
            Assert.Equal(remaining, r.Controller.RemainingCastSec);
            r.Tick(90);
            Assert.Equal(new[] { EncounterRequestKind.AreaAttack, EncounterRequestKind.RebuildMissingMasks }, r.Requests.Skip(2).Select(x => x.Kind));
            var rebuild = r.Requests.Last(); Assert.Equal(new[] { 1 }, rebuild.MaskSlots); Assert.Equal(new[] { 1 }, rebuild.ExpectedGenerations);
            Assert.Equal(1.5f, r.Requests[2].DamageMultiplier);
            Assert.Equal(2, r.Enemies[1].InstanceGeneration); Assert.Equal(123, r.Enemies[2].Hp); Assert.Equal(1, r.Enemies[2].InstanceGeneration);
            Assert.Equal(2, r.Controller.Phase); Assert.False(r.Controller.PhasePending); Assert.Equal(1, r.Controller.MaskRebuildCount);
            Assert.InRange(r.Controller.NextIntentSec, 7.999f, 8.001f);
            r.Boundary(); Assert.Equal(4, r.Requests.Count);
        }

        [Fact]
        public void StablePhaseBoundaryIsOnceAndDoesNotWaitForFirstIntent()
        {
            var r = new Rig(); r.Enemies[0].Hp = 499; r.Enemies[1].Hp = 0; r.Enemies[2].Hp = 0;
            r.Boundary(); r.Boundary();
            Assert.Equal(2, r.Controller.Phase); Assert.Single(r.Requests); Assert.Equal(2, r.Controller.MaskRebuildCount);
            Assert.Equal(new[] { 1, 2 }, r.Requests[0].MaskSlots);
            r.Tick(270); Assert.True(r.Controller.IsCasting);
        }

        [Fact]
        public void ReentrantBoundaryFromAreaDefersRebuildUntilAttackCallbackReturns()
        {
            var r = new Rig(); bool entered = false;
            r.DuringApply = request =>
            {
                if (request.Kind != EncounterRequestKind.AreaAttack) return;
                entered = true; r.Enemies[0].Hp = 450; r.Enemies[1].Hp = 0; r.Boundary();
                Assert.Equal(3, r.Requests.Count); Assert.True(r.Controller.PhasePending);
            };
            r.Tick(390); Assert.True(entered); Assert.Equal(4, r.Requests.Count); Assert.Equal(2, r.Controller.Phase);
        }

        [Theory]
        [InlineData(BattleOutcome.Victory)]
        [InlineData(BattleOutcome.Defeat)]
        [InlineData(BattleOutcome.Failed)]
        public void TerminalOutcomeCancelsPendingCastAndPhase(BattleOutcome outcome)
        {
            var r = new Rig(); r.Tick(300); r.Enemies[0].Hp = 400; r.Boundary();
            r.View.Outcome = outcome; r.Boundary(); r.Tick(1000);
            Assert.True(r.Controller.Cancelled); Assert.False(r.Controller.IsCasting); Assert.False(r.Controller.PhasePending);
            Assert.Equal(2, r.Requests.Count); Assert.Equal(0f, r.Controller.RemainingCastSec);
        }

        [Fact]
        public void DeadBossCancelsEvenWhenMasksAndInProgressRemain()
        {
            var r = new Rig(); r.Tick(300); r.Enemies[0].Hp = 0; r.Boundary(); r.Tick(1000);
            Assert.True(r.Controller.Cancelled); Assert.Equal(2, r.Requests.Count);
        }

        [Fact]
        public void EliteIntentStartsAtSixAndAreaCompletesAtNineWithoutOwningAdds()
        {
            var r = new Rig(elite: true); r.Tick(180);
            Assert.Single(r.Requests); Assert.True(r.Controller.IsCasting); Assert.InRange(r.Controller.RemainingCastSec, 2.999f, 3.001f);
            var snapshot = r.Controller.Snapshot(r.View); Assert.False(snapshot.IsBoss); Assert.Equal(0, snapshot.AliveMasks);
            r.Tick(90); Assert.Equal(2, r.Requests.Count); Assert.Equal("OE_PROMPTER_tap", r.Requests.Last().SkillId);
            Assert.Equal(1f, r.Requests.Last().DamageMultiplier); Assert.InRange(r.Controller.NextIntentSec, 11.999f, 12.001f);
        }

        [Fact]
        public void ActionLockDefersDueActionsAndSilenceDefersAreaButAllowsAuto()
        {
            var r = new Rig(); r.Enemies[0].Status.Add(new StatusInst { Def = new EffectDef { Kind = EffectKind.Silence } });
            r.Tick(270); Assert.Equal(2, r.Requests.Count); Assert.True(r.Controller.IsCasting);
            r.Tick(120); Assert.Equal(2, r.Requests.Count); Assert.True(r.Controller.IsCasting); Assert.Equal(0f, r.Controller.RemainingCastSec);
            r.Enemies[0].Status.Clear(); r.Enemies[0].Status.Add(new StatusInst { Def = new EffectDef { Kind = EffectKind.Stun } });
            r.Tick(30); Assert.Equal(2, r.Requests.Count);
            r.Enemies[0].Status.Clear(); r.Tick(1); Assert.Equal(3, r.Requests.Count);
        }

        [Fact]
        public void FrozenDefinitionAndOneVsTwoSpeedHaveSameGameTimeActions()
        {
            var def = new BossEncounterDef(); var one = new Rig(boss: def); var two = new Rig();
            def.FirstIntentSec = 1f; def.MaskSlots[0] = 8;
            one.Tick(390); two.Tick(195, 2f / 30f);
            Assert.Equal(3, one.Requests.Count); Assert.Equal(one.Requests.Select(x => x.Kind), two.Requests.Select(x => x.Kind));
            Assert.Equal(one.Requests.Select(x => x.DamageMultiplier), two.Requests.Select(x => x.DamageMultiplier));
            Assert.Equal(one.Controller.ElapsedSec, two.Controller.ElapsedSec);
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        public void InvalidElapsedInputFailsBeforeStateAdvances(float dt)
        {
            var r = new Rig(); Assert.Throws<ArgumentOutOfRangeException>(() => r.Tick(1, dt));
            Assert.Empty(r.Requests); Assert.Equal(0d, r.Controller.ElapsedSec);
        }

        [Fact]
        public void MissingMaskUsesGenerationZeroAndLivingMasksAreNotRefilled()
        {
            var r = new Rig(); r.Enemies[1] = null; r.Enemies[2].Hp = 7; r.Enemies[0].Hp = 500; r.Boundary();
            var request = Assert.Single(r.Requests); Assert.Equal(new[] { 1 }, request.MaskSlots);
            Assert.Equal(new[] { 0 }, request.ExpectedGenerations); Assert.Equal(1, r.Enemies[1].InstanceGeneration);
            Assert.Equal(7, r.Enemies[2].Hp); Assert.Equal(1, r.Controller.MaskRebuildCount);
        }

        [Fact]
        public void PhaseWithBothMasksLivingStillTransitionsOnceAndCountsNoRebuild()
        {
            var r = new Rig(); r.Enemies[0].Hp = 500; r.Enemies[1].Hp = 3; r.Enemies[2].Hp = 4;
            r.Boundary(); r.Enemies[0].Hp = 100; r.Boundary();
            var request = Assert.Single(r.Requests); Assert.Empty(request.MaskSlots);
            Assert.Equal(2, r.Controller.Phase); Assert.Equal(0, r.Controller.MaskRebuildCount);
            Assert.Equal(3, r.Enemies[1].Hp); Assert.Equal(4, r.Enemies[2].Hp);
        }

        [Fact]
        public void PendingPhaseRemainsPendingIfBossHealsBeforeItsCastCompletes()
        {
            var r = new Rig(); r.Tick(270); r.Enemies[0].Hp = 450; r.Boundary();
            r.Enemies[0].Hp = 900; r.Tick(120);
            Assert.Equal(2, r.Controller.Phase); Assert.Equal(900, r.Enemies[0].Hp);
        }

        [Fact]
        public void BossDeathInsideAreaCallbackCancelsRebuildAndPreservesHistoricalCounters()
        {
            var r = new Rig(); r.Tick(270); r.Enemies[0].Hp = 450; r.Enemies[1].Hp = 0; r.Boundary();
            r.DuringApply = request =>
            {
                if (request.Kind != EncounterRequestKind.AreaAttack) return;
                r.Enemies[0].Hp = 0; r.Boundary();
            };
            r.Tick(120); double end = r.Controller.ElapsedSec; r.Tick(600);
            Assert.True(r.Controller.Cancelled); Assert.Equal(3, r.Requests.Count);
            Assert.Equal(1, r.Controller.AreaCasts); Assert.Equal(1, r.Controller.IntentSerial); Assert.Equal(3, r.Controller.ActionSerial);
            Assert.Equal(0, r.Controller.MaskRebuildCount); Assert.Equal(end, r.Controller.ElapsedSec);
        }

        [Fact]
        public void FailedApplyCancelsAndPropagatesInsteadOfPretendingAreaSucceeded()
        {
            var r = new Rig(); r.Tick(389); r.DuringApply = request =>
            { if (request.Kind == EncounterRequestKind.AreaAttack) throw new InvalidOperationException("fixture failed"); };
            Assert.Throws<InvalidOperationException>(() => r.Tick(1));
            Assert.True(r.Controller.Cancelled); Assert.Equal(0, r.Controller.AreaCasts);
            r.Tick(300); Assert.Equal(3, r.Requests.Count);
        }

        [Fact]
        public void CoreReportedFailureWithoutThrowDoesNotCountAreaCompletion()
        {
            var r = new Rig(); r.Tick(389); r.DuringApply = request =>
            {
                if (request.Kind != EncounterRequestKind.AreaAttack) return;
                r.View.Outcome = BattleOutcome.Failed; r.Boundary();
            };
            r.Tick(1);
            Assert.True(r.Controller.Cancelled); Assert.Equal(0, r.Controller.AreaCasts);
            Assert.Equal(3, r.Controller.ActionSerial); Assert.Equal(1, r.Controller.IntentSerial);
        }

        [Fact]
        public void StunnedAutoDefersOnceWithoutAccumulatedBurstAfterUnlock()
        {
            var r = new Rig(); r.Enemies[0].Status.Add(new StatusInst { Def = new EffectDef { Kind = EffectKind.Stun } });
            r.Tick(180); Assert.Empty(r.Requests); r.Enemies[0].Status.Clear(); r.Tick(1);
            Assert.Single(r.Requests); r.Tick(1); Assert.Single(r.Requests);
            Assert.InRange(r.Controller.AutoRemainingSec, 2.966f, 2.968f);
        }

        [Fact]
        public void PhaseAfterCompletedAreaShortensExistingNextIntervalFromTheCompletion()
        {
            var r = new Rig(); r.Tick(390); r.Tick(150); r.Enemies[0].Hp = 500; r.Boundary();
            Assert.InRange(r.Controller.NextIntentSec, 2.999f, 3.001f);
            r.Tick(90); Assert.True(r.Controller.IsCasting); Assert.Equal(2, r.Controller.IntentSerial);
        }

        [Fact]
        public void SubsequentAreaAndAutoScheduleFromCompletionWithNoSameFrameAuto()
        {
            var r = new Rig(); r.Tick(690);
            Assert.True(r.Controller.IsCasting); Assert.Equal(2, r.Controller.IntentSerial);
            Assert.Equal(1, r.Controller.AreaCasts); Assert.Equal(5, r.Requests.Count(x => x.Kind == EncounterRequestKind.AutoAttack));
            r.Tick(120); Assert.Equal(2, r.Controller.AreaCasts);
            Assert.Equal(Enumerable.Range(1, r.Requests.Count).Select(x => (long)x), r.Requests.Select(x => x.ActionId));
        }

        [Fact]
        public void NoWorldTickLeavesAllIntentStateUnchangedAndSnapshotsAreCopies()
        {
            var r = new Rig(); r.Tick(300); var before = r.Controller.Snapshot(r.View);
            r.Controller.Advance(0f, () => r.View, _ => throw new Exception("unexpected request"));
            var after = r.Controller.Snapshot(r.View);
            Assert.Equal(before.ElapsedSec, after.ElapsedSec); Assert.Equal(before.RemainingCastSec, after.RemainingCastSec);
            before.RemainingCastSec = 999f; Assert.Equal(after.RemainingCastSec, r.Controller.RemainingCastSec);
        }
    }
}
