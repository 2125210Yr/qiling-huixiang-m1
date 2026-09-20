using System;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Tape-corruption fixtures. Live traces use the public command/queue APIs and normal Tick only.
    public sealed class OriginalReplayBoundaryTests
    {
        [Theory]
        [InlineData("missing-list")]
        [InlineData("missing-row")]
        [InlineData("negative-tick")]
        [InlineData("out-of-order")]
        [InlineData("after-boundary")]
        [InlineData("unknown-input-kind")]
        public void StructurallyInvalidInputJournalIsRejectedBeforeCreatingASimulation(string change)
        {
            var record = ShortTrace();
            switch (change)
            {
                case "missing-list": record.Inputs = null; break;
                case "missing-row": record.Inputs[0] = null; break;
                case "negative-tick": record.Inputs[0].Tick = -1; break;
                case "out-of-order": record.Inputs.Reverse(); break;
                case "after-boundary": record.Inputs.Last().Tick = record.EndTick + 1; break;
                case "unknown-input-kind": record.Inputs[0].Kind = (OriginalReplayInputKind)99; break;
            }
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotEmpty(report.Differences); Assert.Null(report.Replayed);
        }

        [Fact]
        public void RemovingAnActuallySubmittedInputCannotStillMatchItsCommandRecord()
        {
            var record = ShortTrace(); var expectedCommands = record.Commands.Count;
            record.Inputs.RemoveAt(0);
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotNull(report.Replayed);
            Assert.Contains(report.Differences, d => d.Contains("Commands"));
            Assert.NotEqual(expectedCommands, report.Replayed.CommandLog.Count);
        }

        [Fact]
        public void PausedSimulationCannotReachAnInventedLaterBoundary()
        {
            var sim = Create(); Tick(sim, 30);
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause }).Accepted);
            var record = OriginalBattleRecord.Capture(sim); record.EndTick++;
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotNull(report.Replayed);
            Assert.True(report.Replayed.Paused); Assert.Equal(sim.TickIndex, report.Replayed.TickIndex);
            Assert.Contains(report.Differences, d => d.IndexOf("boundary unreachable", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public void ResumePlacedInAFutureTickCannotMagicallyAdvanceAPausedClock()
        {
            var sim = Create(); Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause }).Accepted);
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Resume }).Accepted);
            var record = OriginalBattleRecord.Capture(sim);
            record.Inputs.Last().Tick = 1; record.EndTick = 1;
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.True(report.Replayed.Paused); Assert.Equal(0, report.Replayed.TickIndex);
            Assert.Single(report.Replayed.CommandLog);
            Assert.Contains(report.Differences, d => d.IndexOf("boundary unreachable", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public void NaturallyTerminalSimulationCannotReachAnInventedLaterBoundary()
        {
            var sim = Create("N1");
            for (int i = 0; i < 400 * BattleSim.TickHz && sim.Outcome == BattleOutcome.InProgress; i++) sim.Tick();
            Assert.NotEqual(BattleOutcome.InProgress, sim.Outcome); Assert.NotEqual(BattleOutcome.Failed, sim.Outcome);
            var record = OriginalBattleRecord.Capture(sim); record.EndTick++;
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotNull(report.Replayed);
            Assert.Equal(sim.TickIndex, report.Replayed.TickIndex); Assert.Equal(sim.Outcome, report.Replayed.Outcome);
            Assert.Contains(report.Differences, d => d.IndexOf("boundary unreachable", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public void ChangedEventDetailCannotHideBehindTheUnchangedRecordedHash()
        {
            var record = ShortTrace(120); Assert.NotEmpty(record.Events);
            string originalHash = record.EventHash; record.Events[0].Amount++;
            var report = OriginalBattleReplayer.Verify(record);
            Assert.Equal(originalHash, record.EventHash); Assert.False(report.Match); Assert.NotNull(report.Replayed);
            Assert.Equal(originalHash, report.Replayed.Events.ComputeHash());
            Assert.Contains(report.Differences, d => d.Contains("Events"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ChangedResolutionDetailCannotHideBehindTheUnchangedRecordedHash(bool changeGeneration)
        {
            var record = ShortTrace(120);
            int index = record.Resolutions.FindIndex(r => r.RequestedDamage > 0);
            Assert.True(index >= 0); var r = record.Resolutions[index];
            string originalHash = record.ResolutionHash;
            // Keep every conservation invariant valid: the discrepancy must be detected by replay comparison.
            record.Resolutions[index] = new ResolutionResult(r.Origin, r.RootActionId, r.ProcDepth, r.SourceRelicId,
                r.SkillId, r.SourceSlot, r.SourceAlly, r.TargetSlot, r.TargetAlly,
                r.TargetGeneration + (changeGeneration ? 1 : 0), requestedDamage: r.RequestedDamage + (changeGeneration ? 0 : 1),
                shieldAbsorbed: r.ShieldAbsorbed, effectiveHpDamage: r.EffectiveHpDamage + (changeGeneration ? 0 : 1),
                overkill: r.Overkill, requestedHeal: r.RequestedHeal, effectiveHeal: r.EffectiveHeal,
                overheal: r.Overheal, shieldProduced: r.ShieldProduced, killed: r.Killed);
            var report = OriginalBattleReplayer.Verify(record);
            Assert.Equal(originalHash, record.ResolutionHash); Assert.False(report.Match); Assert.NotNull(report.Replayed);
            Assert.Equal(originalHash, OriginalBattleRecord.Capture(report.Replayed).ResolutionHash);
            Assert.Contains(report.Differences, d => d.Contains("Resolutions"));
        }

        [Fact]
        public void LegalSpeedChangesAreReplayedAtTheirActualTicksIncludingTheBossCast()
        {
            var sim = Create();
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 2 }).Accepted); Tick(sim, 90);
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 1 }).Accepted); Tick(sim, 90);
            Assert.True(sim.OriginalEncounter.IsCasting);
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 2 }).Accepted); Tick(sim, 60);
            Assert.Equal(1, sim.OriginalEncounter.AreaCasts); Assert.InRange(sim.OriginalEncounter.ElapsedSec, 12.999d, 13.001d);
            var record = RoundTrip(OriginalBattleRecord.Capture(sim));
            var report = OriginalBattleReplayer.Verify(record); AssertMatch(report);
            Assert.Equal(240, report.Replayed.TickIndex); Assert.Equal(2, report.Replayed.Speed);
            Assert.Equal(sim.OriginalEncounter.ElapsedSec, report.Replayed.OriginalEncounter.ElapsedSec);
            Assert.Equal(sim.OriginalEncounter.AreaCasts, report.Replayed.OriginalEncounter.AreaCasts);
            Assert.Equal(new[] { 0, 90, 180 }, report.Replayed.CommandLog.Select(c => c.Tick));
        }

        [Fact]
        public void ClearQueueRemovesPendingCommandsAndFeedbackBeforeAnotherResume()
        {
            var sim = Create();
            for (int i = 0; i < 600 && !sim.CanAct(0); i++) sim.Tick(); Assert.True(sim.CanAct(0));
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause }).Accepted);
            Assert.Equal(CommandReject.None, sim.QueueExpeditionTap(0));
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Resume }).Accepted);
            Assert.Single(sim.ExpeditionQueueResults);
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause }).Accepted);
            sim.ClearExpeditionQueue(); Assert.Empty(sim.ExpeditionQueueResults);
            Assert.Equal(CommandReject.None, sim.QueueExpeditionTap(1));
            Assert.Equal(CommandReject.None, sim.QueueExpeditionTap(3)); sim.ClearExpeditionQueue();
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Resume }).Accepted);
            var record = RoundTrip(OriginalBattleRecord.Capture(sim));
            var report = OriginalBattleReplayer.Verify(record); AssertMatch(report);
            Assert.Empty(report.Replayed.ExpeditionQueue); Assert.Empty(report.Replayed.ExpeditionQueueResults);
            Assert.Single(report.Replayed.CommandLog, c => c.Kind == BattleCommandKind.Tap);
            Assert.Equal(sim.CommandLog.Count, report.Replayed.CommandLog.Count);
            Assert.Equal(sim.ExpeditionResolutions.Count, report.Replayed.ExpeditionResolutions.Count);
        }

        [Fact]
        public void MissingComparisonOutputsAreReportedWithoutBecomingReplayInputs()
        {
            var record = ShortTrace(); int end = record.EndTick;
            record.FinalState = null; record.Commands = null; record.Events = null; record.Resolutions = null;
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotNull(report.Replayed); Assert.Equal(end, report.Replayed.TickIndex);
            Assert.Contains(report.Differences, d => d.Contains("FinalState"));
            Assert.Contains(report.Differences, d => d.Contains("Commands"));
            Assert.Contains(report.Differences, d => d.Contains("Events"));
            Assert.Contains(report.Differences, d => d.Contains("Resolutions"));
        }

        [Fact]
        public void LegacyBattlesKeepTheLegacyTapePathAndRejectOriginalCapture()
        {
            var sim = new BattleSim(Catalog.DefaultParty, 0, 260921);
            Assert.False(sim.IsOriginalExpedition);
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause }).Accepted);
            Assert.Equal(CommandReject.ModeDisabled, sim.QueueExpeditionTap(0)); sim.ClearExpeditionQueue();
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Resume }).Accepted);
            Tick(sim, 1);
            Assert.Throws<ArgumentException>(() => OriginalBattleRecord.Capture(sim));
            Assert.Throws<ArgumentException>(() => OriginalBattleSnapshot.Capture(sim));
            var legacy = BattleRunRecord.Capture(sim, Catalog.DefaultParty, sim.ActiveStage.Id);
            var report = BattleReplayer.Verify(legacy, seed => new BattleSim(Catalog.DefaultParty, 0, seed));
            Assert.True(report.Match, string.Join(" | ", report.DigestDiff.Concat(report.EventDiff).Concat(report.CommandDiff).Concat(report.VersionDiff)));
            Assert.False(report.Replayed.IsOriginalExpedition);
            Assert.Null(report.Replayed.ExpeditionRelics); Assert.Null(report.Replayed.OriginalEncounter);
        }

        static BattleSim Create(string node = "N7")
        {
            var input = RunBattleFactory.CreateInput(node, "single", 260921, new[] { "C01" }, null);
            input.RunId = "replay-boundary-fixture"; input.EncounterId = "replay-boundary-fixture/" + node;
            input.AttemptId = "replay-boundary-attempt-1"; input.BattleOrdinal = node == "N7" ? 5 : 1;
            return RunBattleFactory.Create(input);
        }
        static void Tick(BattleSim sim, int count)
        { for (int i = 0; i < count; i++) sim.Tick(); }
        static OriginalBattleRecord ShortTrace(int ticks = 30)
        {
            var sim = Create(); Assert.True(sim.Submit(BattleCommand.FocusEnemy(1)).Accepted); Tick(sim, 10);
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 2 }).Accepted); Tick(sim, ticks - 10);
            return RoundTrip(OriginalBattleRecord.Capture(sim));
        }
        static OriginalBattleRecord RoundTrip(OriginalBattleRecord record) => OriginalBattleRecord.FromJson(record.ToJson());
        static void AssertMatch(OriginalReplayReport report)
        { Assert.True(report.Match, string.Join(" | ", report.Differences)); Assert.NotNull(report.Replayed); }
    }
}
