using System;
using System.IO;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Lifecycle boundary fixtures, not ordinary-play evidence. Charge/cooldown and terminal HP are
    // explicitly arranged; shield, absorption, harmony and forte are produced by public Submit/Tick.
    // Flow receives declared fixture victories so these tests isolate transitions, not battle balance.
    public sealed class OriginalLifecycleBoundaryTests
    {
        sealed class Rig
        {
            public readonly OriginalProfileStore Store;
            public readonly ExpeditionFlow Flow;
            public Rig(string core = "A01")
            {
                var path = Path.Combine(Path.GetTempPath(), "original-lifecycle-tests", Guid.NewGuid().ToString("N"), "profile.v1.json");
                Store = new OriginalProfileStore(path); Flow = new ExpeditionFlow(Store);
                Flow.StartRun(core, "single", 260921);
            }
        }

        [Fact]
        public void OrdinaryVictoryCarriesRecoveredLivingHpAndDownStateButNoCombatTemporaries()
        {
            var rig = new Rig(); var firstInput = rig.Flow.BeginBattle(); var previous = RunBattleFactory.Create(firstInput);
            EstablishTemporaryState(previous, withHarmony: false);
            var terminalHp = ApplyTerminalHpFixture(previous);
            var max = previous.Allies.Select(u => u.MaxHp).ToArray();
            var expected = terminalHp.Select((hp, i) => hp == 0 ? 0 : Math.Min(max[i], hp + max[i] * 15 / 100)).ToArray();
            AssertDirty(previous, withHarmony: false);
            Assert.True(rig.Flow.CompleteBattle(firstInput.EncounterId, BattleOutcome.Victory, terminalHp, attemptId: firstInput.AttemptId));
            Assert.Equal(expected, rig.Flow.Profile.ActiveRun.PartyHp);
            var pending = rig.Flow.Profile;
            rig.Flow.ChooseReward(pending.ActiveRun.PendingOffer.Id, pending.Revision, pending.ActiveRun.PendingOffer.CandidateIds[0]);
            rig.Flow.ChooseRoute("N2-backstage"); rig.Flow.Reload();
            var nextInput = rig.Flow.BeginBattle(); var next = RunBattleFactory.Create(nextInput);

            Assert.Equal("N2-backstage", nextInput.Stage.Id); Assert.Equal(firstInput.RunId, nextInput.RunId);
            Assert.NotEqual(firstInput.EncounterId, nextInput.EncounterId); Assert.Equal(2, nextInput.BattleOrdinal);
            Assert.Equal(expected, nextInput.OpeningHp); Assert.Equal(expected, next.Allies.Select(u => u.Hp));
            Assert.False(next.Allies[0].Alive); Assert.True(next.Allies[1].Hp < next.Allies[1].MaxHp);
            Assert.NotSame(previous.ExpeditionRelics, next.ExpeditionRelics);
            AssertClean(next); AssertDirty(previous, withHarmony: false);
        }

        [Fact]
        public void MatureN5BuildReachesBossDoorWithFullHpAndRebuildsCleanAAndCState()
        {
            var rig = PersistedMatureN5Fixture(); var firstInput = rig.Flow.BeginBattle();
            var previous = RunBattleFactory.Create(firstInput); EstablishTemporaryState(previous, withHarmony: true);
            var terminalHp = ApplyTerminalHpFixture(previous); AssertDirty(previous, withHarmony: true);
            var oldRelics = rig.Flow.Profile.ActiveRun.OwnedRelicIds;
            Assert.True(rig.Flow.CompleteBattle(firstInput.EncounterId, BattleOutcome.Victory, terminalHp, attemptId: firstInput.AttemptId));
            rig.Flow.Reload(); var atDoor = rig.Flow.Profile.ActiveRun;
            Assert.Equal("N6", atDoor.CurrentNode); Assert.Equal(oldRelics, atDoor.OwnedRelicIds);
            Assert.Equal(ExpeditionContent.GetPartyMaxHp("single"), atDoor.PartyHp);
            var bossInput = rig.Flow.BeginBattle(); var boss = RunBattleFactory.Create(bossInput);

            Assert.Equal("N7", bossInput.Stage.Id); Assert.Equal(5, bossInput.BattleOrdinal);
            Assert.Equal(firstInput.RunId, bossInput.RunId); Assert.Equal(oldRelics, bossInput.RelicIds);
            Assert.Equal(ExpeditionContent.GetPartyMaxHp("single"), bossInput.OpeningHp);
            Assert.All(boss.Allies, u => { Assert.True(u.Alive); Assert.Equal(u.MaxHp, u.Hp); });
            AssertClean(boss); AssertDirty(previous, withHarmony: true);
            Assert.Equal(1, boss.OriginalEncounter.Phase); Assert.False(boss.OriginalEncounter.IsCasting);
            Assert.Equal(0d, boss.OriginalEncounter.ElapsedSec);
            Assert.All(boss.Enemies, u => Assert.Equal(1, u.InstanceGeneration));
        }

        [Fact]
        public void EndRunThenStartNewRunPreservesDiscoveriesButDropsBuildAndAllBattleTemporaries()
        {
            var rig = PersistedMatureN5Fixture(); var oldInput = rig.Flow.BeginBattle();
            var previous = RunBattleFactory.Create(oldInput); EstablishTemporaryState(previous, withHarmony: true);
            ApplyTerminalHpFixture(previous); AssertDirty(previous, withHarmony: true);
            var oldProfile = rig.Flow.Profile; var discoveries = oldProfile.DiscoveredRelics;
            rig.Flow.EndRun(); rig.Flow.Reload();
            Assert.Null(rig.Flow.Profile.ActiveRun); Assert.False(rig.Flow.Profile.LastRunSummary.Victory);
            Assert.Equal(oldInput.RunId, rig.Flow.Profile.LastRunSummary.RunId);
            rig.Flow.StartRun("A01", "single", 260922);
            var fresh = rig.Flow.Profile;
            Assert.NotEqual(oldInput.RunId, fresh.ActiveRun.RunId);
            Assert.Equal(new[] { "A01" }, fresh.ActiveRun.OwnedRelicIds);
            Assert.Equal(discoveries, fresh.DiscoveredRelics); Assert.Equal(oldProfile.UnlockedPresets, fresh.UnlockedPresets);
            Assert.Null(fresh.ActiveRun.BossCheckpoint); Assert.Null(fresh.ActiveRun.CurrentBattleCheckpoint);
            Assert.Empty(fresh.ActiveRun.SettledBattleIds); Assert.Null(fresh.ActiveRun.PendingOffer);
            var nextInput = rig.Flow.BeginBattle(); var next = RunBattleFactory.Create(nextInput);

            Assert.Equal("N1", nextInput.Stage.Id); Assert.Equal(1, nextInput.BattleOrdinal);
            Assert.Equal(new[] { "A01" }, nextInput.RelicIds);
            Assert.Equal(ExpeditionContent.GetPartyMaxHp("single"), nextInput.OpeningHp);
            Assert.All(next.Allies, u => { Assert.True(u.Alive); Assert.Equal(u.MaxHp, u.Hp); });
            Assert.NotSame(previous.ExpeditionRelics, next.ExpeditionRelics);
            AssertClean(next); AssertDirty(previous, withHarmony: true);
        }

        static Rig PersistedMatureN5Fixture()
        {
            var rig = new Rig("C01"); var profile = rig.Flow.Profile; var run = profile.ActiveRun;
            // Declared completed-route fixture: no claim that these earlier reward offers were played here.
            // Five relics fit the reinforcement route; C04 has C01+C03 and A02 has A01.
            run.CurrentNode = "N5"; run.Status = ExpeditionStatus.Ready; run.BattleOrdinal = 3;
            run.OwnedRelicIds = new[] { "C01", "C03", "A01", "A02", "C04" };
            profile.DiscoveredRelics = (string[])run.OwnedRelicIds.Clone();
            run.VisitedNodeIds = new[] { "N0", "N1", "N2-backstage", "N3", "N4" };
            run.SelectedChoices = new[] { "N0:C01", "R1:C03", "N2:N2-backstage", "R2:A01", "N3:reinforce", "R3:A02", "R4:C04" };
            run.SettledBattleIds = new[] { run.RunId + "/N1/1", run.RunId + "/N2-backstage/2", run.RunId + "/N4/3" };
            rig.Store.Save(profile.Revision, profile); rig.Flow.Reload();
            return rig;
        }

        static void EstablishTemporaryState(BattleSim sim, bool withHarmony)
        {
            // Explicit readiness fixture. The public skill path creates the actual shield/status and relic effects.
            foreach (var unit in sim.Allies) unit.Charge = 100f;
            Assert.True(sim.Submit(BattleCommand.Tap(1, CommandSource.Fixture)).Accepted);
            if (withHarmony)
            {
                Assert.True(sim.Submit(BattleCommand.Tap(2, CommandSource.Fixture)).Accepted);
                Assert.True(sim.Submit(BattleCommand.Tap(4, CommandSource.Fixture)).Accepted);
                Assert.True(sim.ExpeditionRelics.ForteStored); // Three distinct support actors formed harmony.
                sim.Allies[1].Charge = 100f; // Fourth support action preserves forte but starts the next harmony set.
                Assert.True(sim.Submit(BattleCommand.Tap(1, CommandSource.Fixture)).Accepted);
                Assert.NotEqual(0, sim.ExpeditionRelics.HarmonyActorMask);
            }
            // Native enemy damage genuinely consumes shield and fills A energy. Auto attacks cannot consume forte.
            for (int tick = 0; tick < 300 && sim.ExpeditionRelics.BarrierEnergy == 0 && sim.Outcome == BattleOutcome.InProgress; tick++) sim.Tick();
            Assert.Contains(sim.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Native && !r.SourceAlly && r.ShieldAbsorbed > 0);
            sim.Allies[3].SlideCd = 7f; // Explicit nonzero cooldown boundary fixture.
            AssertDirty(sim, withHarmony);
        }

        static int[] ApplyTerminalHpFixture(BattleSim sim)
        {
            var hp = new[] { 0, sim.Allies[1].MaxHp / 4, sim.Allies[2].MaxHp / 2,
                sim.Allies[3].MaxHp / 3, sim.Allies[4].MaxHp - 17 };
            for (int i = 0; i < hp.Length; i++) sim.Allies[i].Hp = hp[i];
            return hp;
        }

        static void AssertDirty(BattleSim sim, bool withHarmony)
        {
            Assert.Contains(sim.Allies, u => u.Shield > 0); Assert.Contains(sim.Allies, u => u.Status.Count > 0);
            Assert.Contains(sim.Allies, u => u.Charge > 0 && u.Charge != 35f); Assert.Contains(sim.Allies, u => u.SlideCd > 0);
            Assert.True(sim.ExpeditionRelics.BarrierEnergy > 0); Assert.True(sim.ExpeditionRelics.TriggerSerial > 0);
            Assert.True(sim.ExpeditionRelics.LastCompletedRootActionId > 0);
            if (withHarmony)
            {
                Assert.NotEqual(0, sim.ExpeditionRelics.HarmonyActorMask); Assert.True(sim.ExpeditionRelics.ForteStored);
                Assert.True(sim.ExpeditionRelics.GetTriggerSerial("C03") > 0);
            }
        }

        static void AssertClean(BattleSim sim)
        {
            Assert.Equal(0, sim.TickIndex); Assert.Equal(0L, sim.OriginalRootActionId);
            Assert.All(sim.Allies, u =>
            {
                // Spec resets per-battle charge; the authored allied opening value is 35, not zero.
                Assert.Equal(0, u.Shield); Assert.Empty(u.Status); Assert.Equal(35f, u.Charge);
                Assert.Equal(0f, u.SlideCd); Assert.Equal(0f, u.AutoTimer);
            });
            Assert.Equal(0, sim.ExpeditionRelics.BarrierEnergy); Assert.Equal(0, sim.ExpeditionRelics.HarmonyActorMask);
            Assert.False(sim.ExpeditionRelics.ForteStored); Assert.Equal(0L, sim.ExpeditionRelics.TriggerSerial);
            Assert.Equal(0L, sim.ExpeditionRelics.LastCompletedRootActionId); Assert.Equal(0, sim.ExpeditionRelics.LastRequestCount);
            Assert.All(sim.ExpeditionRelics.RelicTriggerSerials, pair => Assert.Equal(0L, pair.Value));
            Assert.Empty(sim.ExpeditionQueue); Assert.Empty(sim.ExpeditionQueueResults);
        }
    }
}
