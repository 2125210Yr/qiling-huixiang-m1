using System;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Controlled core fixtures, not ordinary player-run evidence.
    public sealed class OriginalExpeditionModeTests
    {
        static ExpeditionBattleInput Input() => RunBattleFactory.CreateInput("N1", "single", 91731, new[] { "A01" }, null);

        [Fact]
        public void DefaultMode_IsOriginal_LegacyRequiresExplicitOptIn()
        {
            Assert.True(OriginalModeSelection.UseOriginal(null));
            Assert.True(OriginalModeSelection.UseOriginal(new[] { "Resonance.exe" }));
            Assert.True(OriginalModeSelection.UseOriginal(new[] { "Resonance.exe", "--unrelated" }));
            Assert.False(OriginalModeSelection.UseOriginal(new[] { "Resonance.exe", "--legacy" }));
        }

        [Fact]
        public void OriginalMode_RejectsDriveFeverAutoOwnershipAndThreeTimesSpeed()
        {
            var sim = new BattleSim(Input());
            sim.Drive = 100;
            sim.FeverGauge = 100;
            Assert.False(sim.Submit(BattleCommand.DriveBegin(0)).Accepted);
            Assert.False(sim.TryBeginDrive(0));
            Assert.False(sim.Submit(BattleCommand.FeverTap(0)).Accepted);
            Assert.False(sim.Submit(new BattleCommand { Kind = BattleCommandKind.SetAuto, Value = 2 }).Accepted);
            Assert.False(sim.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 3 }).Accepted);
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 2 }).Accepted);
            Assert.Equal(AutoMode.Manual, sim.Auto);
            Assert.Equal(-1, sim.PendingDriveSlot);
            for (int i = 0; i < 90; i++) sim.Tick();
            Assert.False(sim.FeverActive);
            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.Contains(sim.Casts, c => c.Type == SkillType.Auto && c.CasterAlly);
        }

        [Fact]
        public void FrozenInput_DeadMembersStayDownAndCallerCannotAlterBattle()
        {
            var input = Input();
            input.OpeningHp[0] = 0;
            input.OpeningHp[1] = 200;
            var fingerprint = BattleSim.ContentFingerprint();
            var sim = new BattleSim(input);
            var originalAttack = sim.Allies[1].Def.Atk;
            var originalTime = sim.TimeLeft;
            input.OpeningHp[1] = 1;
            input.Characters.First(c => c.Id == input.PartyIds[1]).Atk = 999999;
            input.Stage.TimeLimitSec = 1;
            input.Stage.Wave0[0] = "missing";
            input.Skills[0].FlatPower = 999999;
            Assert.True(sim.IsOriginalExpedition);
            Assert.False(sim.Allies[0].Alive);
            Assert.Equal(200, sim.Allies[1].Hp);
            Assert.Equal(originalAttack, sim.Allies[1].Def.Atk);
            Assert.Equal(originalTime, sim.TimeLeft);
            Assert.Equal(fingerprint, BattleSim.ContentFingerprint());
            var exposed = sim.OpeningExpeditionInput;
            exposed.OpeningHp[1] = 3;
            Assert.Equal(200, sim.OpeningExpeditionInput.OpeningHp[1]);
        }

        [Fact]
        public void OriginalPause_FreezesSimulationAndResumedSkillsUseSubmit()
        {
            var sim = new BattleSim(Input());
            sim.Tick();
            var before = BattleStateDigest.Of(sim).ToCanonicalString();
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause }).Accepted);
            Assert.False(sim.Submit(BattleCommand.Tap(0)).Accepted);
            for (int i = 0; i < 300; i++) sim.Tick();
            Assert.Equal(before, BattleStateDigest.Of(sim).ToCanonicalString());
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Resume }).Accepted);
            var attempt = sim.Submit(BattleCommand.Tap(0));
            Assert.Equal(CommandReject.NotCharged, attempt.Reason);
            Assert.Equal(4, sim.CommandLog.Count);
        }

        [Fact]
        public void OriginalSingleWave_EndsWithoutEnteringPhantomSecondWave()
        {
            var sim = new BattleSim(Input());
            foreach (var enemy in sim.Enemies) enemy.Hp = 0;
            sim.Tick();
            Assert.Equal(BattleOutcome.Victory, sim.Outcome);
            Assert.Equal(0, sim.WaveIndex);
        }
    }
}
