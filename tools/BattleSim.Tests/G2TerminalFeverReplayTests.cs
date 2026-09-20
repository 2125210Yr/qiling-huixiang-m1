using Resonance.Battle;
using System.Linq;
using Xunit;

namespace Resonance.Tests
{
    [Collection("G2RecheckCatalog")]
    public sealed class G2TerminalFeverReplayTests
    {
        const int Seed = 260920;

        [Fact]
        public void SettledTerminalRecord_ReplaysFeverEndAtTheResultTick()
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            Assert.Equal(BattleOutcome.Victory, original.Outcome);
            Assert.True(original.FeverActive);
            var resultTick = original.TickIndex;

            // Same post-result clock path as GameRoot; battle ticks stay frozen.
            for (var i = 0; i < BattleSim.TickHz && !original.Settled; i++)
                original.TickFeverOnly();
            Assert.True(original.Settled);
            Assert.Equal(resultTick, original.TickIndex);
            var events = original.Events.Events;
            Assert.Equal("result", events[events.Count - 2].Kind);
            Assert.Equal("fever_end", events[events.Count - 1].Kind);

            var report = Replay(original);

            Assert.True(report.Match, string.Join(" | ", report.Diff));
            Assert.True(report.Replayed.Settled);
            Assert.Equal(resultTick, report.Replayed.TickIndex);
            Assert.Equal(FeverEndReason.TimeUp, report.Replayed.LastFeverEnd);
            Assert.Empty(report.EventDiff);
            Assert.Empty(report.DigestDiff);
        }

        [Fact]
        public void ActiveTerminalSnapshot_DoesNotAdvanceFeverBeyondTheRecord()
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            Assert.Equal(BattleOutcome.Victory, original.Outcome);
            Assert.True(original.FeverActive);
            var remaining = original.FeverLeft;

            var report = Replay(original);

            Assert.True(report.Match, string.Join(" | ", report.Diff));
            Assert.True(report.Replayed.FeverActive);
            Assert.False(report.Replayed.Settled);
            Assert.Equal(remaining, report.Replayed.FeverLeft);
            Assert.Equal(original.FeverHitsLeft, report.Replayed.FeverHitsLeft);
            Assert.DoesNotContain(report.Replayed.Events.Events, e => e.Kind == "fever_end");
        }

        [Fact]
        public void PartialTerminalSnapshot_ReplaysOnlyTheRecordedClockCalls()
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            original.TickFeverOnly();
            original.TickFeverOnly();
            Assert.True(original.FeverActive);

            var report = Replay(original);

            Assert.True(report.Match, string.Join(" | ", report.Diff));
            Assert.Equal(original.FeverLeft, report.Replayed.FeverLeft);
            Assert.Equal(original.TickIndex, report.Replayed.TickIndex);
        }

        [Fact]
        public void TamperedExpectedFeverActive_CannotChangeTheReplayClock()
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            var record = Record(original);
            var baseline = BattleReplayer.Verify(record, BeforeVictory);
            Assert.True(baseline.Match);
            record.FinalDigest.FeverActive = false;

            var report = BattleReplayer.Verify(record, BeforeVictory);

            Assert.False(report.Match);
            Assert.Equal(baseline.Replayed.FeverLeft, report.Replayed.FeverLeft);
            Assert.Equal(baseline.ReplayedDigest.ToCanonicalString(), report.ReplayedDigest.ToCanonicalString());
            Assert.Equal(EventKeys(baseline.Replayed), EventKeys(report.Replayed));
            Assert.Contains(report.DigestDiff, diff => diff.StartsWith("FeverActive:"));
        }

        [Fact]
        public void TamperedExpectedEvents_CannotChangeTheReplayClockOrEvents()
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            for (var i = 0; i < BattleSim.TickHz && !original.Settled; i++)
                original.TickFeverOnly();
            var record = Record(original);
            var baseline = BattleReplayer.Verify(record, BeforeVictory);
            Assert.True(baseline.Match);
            record.EventSummaries[record.EventSummaries.Count - 1].Opcode = "forged_end";

            var report = BattleReplayer.Verify(record, BeforeVictory);

            Assert.False(report.Match);
            Assert.Equal(baseline.Replayed.FeverLeft, report.Replayed.FeverLeft);
            Assert.Equal(baseline.ReplayedDigest.ToCanonicalString(), report.ReplayedDigest.ToCanonicalString());
            Assert.Equal(EventKeys(baseline.Replayed), EventKeys(report.Replayed));
            Assert.NotEmpty(report.EventDiff);
        }

        [Fact]
        public void TamperedExpectedOutcome_CannotPreventTheRecordedClockCalls()
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            for (var i = 0; i < BattleSim.TickHz && !original.Settled; i++)
                original.TickFeverOnly();
            var record = Record(original);
            var baseline = BattleReplayer.Verify(record, BeforeVictory);
            Assert.True(baseline.Match);
            record.FinalDigest.Outcome = BattleOutcome.InProgress;

            var report = BattleReplayer.Verify(record, BeforeVictory);

            Assert.False(report.Match);
            Assert.Equal(baseline.ReplayedDigest.ToCanonicalString(), report.ReplayedDigest.ToCanonicalString());
            Assert.Equal(EventKeys(baseline.Replayed), EventKeys(report.Replayed));
            Assert.Equal(original.TerminalFeverTicks, report.Replayed.TerminalFeverTicks);
            Assert.Contains(report.DigestDiff, diff => diff.StartsWith("Outcome:"));
        }

        [Fact]
        public void TamperedExpectedTickIndex_CannotChangeTheReplayBoundary()
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            original.TickFeverOnly();
            var record = Record(original);
            var baseline = BattleReplayer.Verify(record, BeforeVictory);
            Assert.True(baseline.Match);
            record.FinalDigest.TickIndex = 0;

            var report = BattleReplayer.Verify(record, BeforeVictory);

            Assert.False(report.Match);
            Assert.Equal(baseline.ReplayedDigest.ToCanonicalString(), report.ReplayedDigest.ToCanonicalString());
            Assert.Equal(baseline.Replayed.FeverLeft, report.Replayed.FeverLeft);
            Assert.Equal(EventKeys(baseline.Replayed), EventKeys(report.Replayed));
            Assert.Contains(report.DigestDiff, diff => diff.StartsWith("TickIndex:"));
        }

        [Fact]
        public void TerminalSpeedChange_ReplaysBetweenItsRecordedClockCalls()
        {
            var original = BeforeVictoryWithScalingClock(Seed);
            original.Tick();
            original.TickFeverOnly();
            Assert.True(original.Submit(new BattleCommand
            {
                Kind = BattleCommandKind.SetSpeed, Value = 3, Source = CommandSource.Player
            }).Accepted);
            original.TickFeverOnly();

            var report = BattleReplayer.Verify(Record(original), BeforeVictoryWithScalingClock);

            Assert.True(report.Match, string.Join(" | ", report.Diff));
            Assert.Equal(original.FeverLeft, report.Replayed.FeverLeft);
            Assert.Equal(original.TerminalFeverTicks, report.Replayed.TerminalFeverTicks);
            Assert.Equal(EventKeys(original), EventKeys(report.Replayed));
        }

        [Fact]
        public void CapturedBoundary_IsAnIndependentValueAndSurvivesSerialization()
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            original.TickFeverOnly();
            original.TickFeverOnly();
            var record = BattleRunRecord.Capture(original, Catalog.DefaultParty, original.ActiveStage.Id);
            Assert.Equal(2, original.TerminalFeverTicks);
            Assert.Equal(2, record.TerminalFeverTicks);
            original.TickFeverOnly();

            var parsed = BattleRunRecord.ParseJsonLines(record.ToJsonLines());

            Assert.Equal(3, original.TerminalFeverTicks);
            Assert.Equal(2, record.TerminalFeverTicks);
            Assert.Equal(2, parsed.TerminalFeverTicks);
            Assert.Equal(original.TickIndex, parsed.BattleTickIndex);
            var replayed = BattleReplayer.Verify(parsed, BeforeVictory);
            Assert.True(replayed.Match, string.Join(" | ", replayed.Diff));
            Assert.Equal(2, replayed.Replayed.TerminalFeverTicks);
        }

        [Fact]
        public void ClockCounter_ExcludesBattleTicksPausedAndInactiveCalls()
        {
            var original = BeforeVictory(Seed);
            original.TickFeverOnly(); // In-progress calls are not post-result input.
            Assert.Equal(0, original.TerminalFeverTicks);
            original.Tick();
            Assert.Equal(0, original.TerminalFeverTicks);
            original.Paused = true;
            original.TickFeverOnly();
            Assert.Equal(0, original.TerminalFeverTicks);
            original.Paused = false;
            original.TickFeverOnly();
            Assert.Equal(1, original.TerminalFeverTicks);
            original.FeverActive = false;
            original.TickFeverOnly();
            Assert.Equal(1, original.TerminalFeverTicks);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void InvalidRecordedBoundary_FailsWithoutAdvancingTheClock(int calls)
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            var record = Record(original);
            record.TerminalFeverTicks = calls;
            record = BattleRunRecord.ParseJsonLines(record.ToJsonLines());

            var report = BattleReplayer.Verify(record, BeforeVictory);

            Assert.False(report.Match);
            Assert.Equal(0, report.Replayed.TerminalFeverTicks);
            Assert.Contains(report.CommandDiff, diff => diff.StartsWith("TerminalFeverTicks:"));
        }

        [Fact]
        public void MissingLegacyBoundary_DoesNotInferSettledFeverFromExpectedOutput()
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            for (var i = 0; i < BattleSim.TickHz && !original.Settled; i++)
                original.TickFeverOnly();
            var record = Record(original);
            record.TerminalFeverTicks = null;
            record.BattleTickIndex = null;
            var text = record.ToJsonLines();
            Assert.DoesNotContain("terminalFeverTicks", text);
            record = BattleRunRecord.ParseJsonLines(text);
            Assert.Null(record.TerminalFeverTicks);

            var report = BattleReplayer.Verify(record, BeforeVictory);

            Assert.False(report.Match);
            Assert.True(report.Replayed.FeverActive);
            Assert.Equal(0, report.Replayed.TerminalFeverTicks);
            Assert.Contains(report.CommandDiff, diff => diff.Contains("legacy record has no terminal Fever clock boundary"));
        }

        [Fact]
        public void MissingLegacyBoundary_StillAllowsOrdinaryNoTailRecords()
        {
            var original = G2ReviewFixtures.NewJp(Seed);
            for (var i = 0; i < 10; i++) original.Tick();
            var record = Record(original);
            record.TerminalFeverTicks = null;
            record.BattleTickIndex = null;
            record = BattleRunRecord.ParseJsonLines(record.ToJsonLines());

            var report = BattleReplayer.Verify(record, seed => G2ReviewFixtures.NewJp(seed));

            Assert.True(report.Match, string.Join(" | ", report.Diff));
        }

        [Theory]
        [InlineData("{\"rec\":\"boundary\",\"battleTickIndex\":1,\"terminalFeverTicks\":0}\n{\"rec\":\"boundary\",\"battleTickIndex\":1,\"terminalFeverTicks\":0}")]
        [InlineData("{\"rec\":\"boundary\",\"battleTickIndex\":1,\"terminalFeverTicks\":-1}\n{\"rec\":\"boundary\",\"battleTickIndex\":1,\"terminalFeverTicks\":0}")]
        [InlineData("{\"rec\":\"boundary\",\"terminalFeverTicks\":0}")]
        [InlineData("{\"rec\":\"boundary\",\"battleTickIndex\":1}")]
        [InlineData("{\"rec\":\"boundary\",\"battleTickIndex\":1,\"terminalFeverTicks\":\"bad\"}")]
        public void MalformedBoundaryRows_AreRejected(string replacement)
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            var text = Record(original).ToJsonLines();
            var line = text.Split('\n').Single(row => row.StartsWith("{\"rec\":\"boundary\""));
            var record = BattleRunRecord.ParseJsonLines(text.Replace(line, replacement));

            var report = BattleReplayer.Verify(record, BeforeVictory);

            Assert.False(report.Match);
            Assert.Contains(report.CommandDiff, diff => diff.Contains("boundary"));
            Assert.Equal(0, report.Replayed.TerminalFeverTicks);
        }

        [Fact]
        public void DuplicateBoundaryError_CannotBeRemovedByReserializingTheRecord()
        {
            var original = BeforeVictory(Seed);
            original.Tick();
            var text = Record(original).ToJsonLines();
            var line = text.Split('\n').Single(row => row.StartsWith("{\"rec\":\"boundary\""));
            var record = BattleRunRecord.ParseJsonLines(text.Replace(line, line + "\n" + line));
            Assert.False(BattleReplayer.Verify(record, BeforeVictory).Match);

            var error = Assert.Throws<System.InvalidOperationException>(() => record.ToJsonLines());

            Assert.Contains("duplicate boundary", error.Message);
        }

        [Theory]
        [InlineData("negative")]
        [InlineData("missing")]
        [InlineData("beyond")]
        [InlineData("backwards")]
        public void InvalidTerminalCommandOffsets_AreRejected(string corruption)
        {
            var original = BeforeVictoryWithScalingClock(Seed);
            original.Tick();
            original.TickFeverOnly();
            original.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 2, Source = CommandSource.Player });
            original.TickFeverOnly();
            original.Submit(new BattleCommand { Kind = BattleCommandKind.SetAuto, Value = (int)AutoMode.Full, Source = CommandSource.Player });
            original.TickFeverOnly();
            var record = Record(original);
            var commands = record.Commands.Where(c => c.Accepted && c.Source == CommandSource.Player).ToArray();
            Assert.Equal(1, commands[0].TerminalFeverTick);
            Assert.Equal(2, commands[1].TerminalFeverTick);
            if (corruption == "negative") commands[0].TerminalFeverTick = -1;
            else if (corruption == "missing") commands[0].TerminalFeverTick = null;
            else if (corruption == "beyond") commands[1].TerminalFeverTick = int.MaxValue;
            else commands[1].TerminalFeverTick = 0;
            record = BattleRunRecord.ParseJsonLines(record.ToJsonLines());

            var report = BattleReplayer.Verify(record, BeforeVictoryWithScalingClock);

            Assert.False(report.Match);
            Assert.Contains(report.CommandDiff, diff => diff.StartsWith("TerminalFeverTick:"));
        }

        [Fact]
        public void TerminalSpeedAndAutoChanges_PreserveSameOffsetSequenceAndClockSegments()
        {
            var original = BeforeVictoryWithScalingClock(Seed);
            original.Tick();
            original.TickFeverOnly();
            original.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 2, Source = CommandSource.Player });
            original.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 3, Source = CommandSource.Player });
            original.Submit(new BattleCommand { Kind = BattleCommandKind.SetAuto, Value = (int)AutoMode.Full, Source = CommandSource.Player });
            for (var i = 0; i < 3; i++) original.TickFeverOnly();
            original.Submit(new BattleCommand { Kind = BattleCommandKind.SetAuto, Value = (int)AutoMode.Manual, Source = CommandSource.Player });
            original.TickFeverOnly();

            var report = BattleReplayer.Verify(Record(original), BeforeVictoryWithScalingClock);

            Assert.True(report.Match, string.Join(" | ", report.Diff));
            Assert.Equal(original.FeverLeft, report.Replayed.FeverLeft);
            Assert.Equal(original.TerminalFeverTicks, report.Replayed.TerminalFeverTicks);
            var expectedCommands = original.CommandLog.Where(c => c.TerminalFeverTick.HasValue).ToArray();
            var actualCommands = report.Replayed.CommandLog.Where(c => c.TerminalFeverTick.HasValue).ToArray();
            Assert.Equal(expectedCommands.Select(c => c.Kind), actualCommands.Select(c => c.Kind));
            Assert.Equal(expectedCommands.Select(c => c.Accepted), actualCommands.Select(c => c.Accepted));
            Assert.Equal(expectedCommands.Select(c => c.TerminalFeverTick), actualCommands.Select(c => c.TerminalFeverTick));
        }

        [Theory]
        [InlineData(AutoMode.Manual)]
        [InlineData(AutoMode.Semi)]
        [InlineData(AutoMode.Full)]
        public void TerminalClock_DoesNotDealDamageOrEmitAnotherResult(AutoMode auto)
        {
            var original = BeforeVictory(Seed);
            original.Auto = auto;
            original.Tick();
            var before = BattleStateDigest.Of(original);
            var eventCount = original.Events.Events.Count;
            Assert.Single(original.Events.Events, e => e.Kind == "result");

            for (var i = 0; i < BattleSim.TickHz && !original.Settled; i++)
                original.TickFeverOnly();

            var after = BattleStateDigest.Of(original);
            Assert.True(original.Settled);
            Assert.Equal(before.TickIndex, after.TickIndex);
            Assert.Equal(before.WaveIndex, after.WaveIndex);
            Assert.Equal(before.Outcome, after.Outcome);
            Assert.Equal(before.TimeLeft, after.TimeLeft);
            Assert.Equal(before.Units.Select(u => u.Hp), after.Units.Select(u => u.Hp));
            Assert.Single(original.Events.Events, e => e.Kind == "result");
            Assert.Single(original.Events.Events.Skip(eventCount));
            Assert.Equal("fever_end", original.Events.Events.Last().Kind);
            var report = BattleReplayer.Verify(Record(original), BeforeVictory);
            Assert.True(report.Match, string.Join(" | ", report.Diff));
            Assert.Equal(original.TerminalFeverTicks, report.Replayed.TerminalFeverTicks);
        }

        static string[] EventKeys(BattleSim sim) => sim.Events.Events.Select(e => EventSummary.From(e).Key()).ToArray();

        static BattleRunRecord Record(BattleSim original)
        {
            var record = BattleRunRecord.Capture(original, Catalog.DefaultParty, original.ActiveStage.Id);
            return BattleRunRecord.ParseJsonLines(record.ToJsonLines());
        }

        static ReplayReport Replay(BattleSim original)
        {
            // Exercise the same serialized digest boundary as natural-play readback.
            return BattleReplayer.Verify(Record(original), BeforeVictory);
        }

        static BattleSim BeforeVictory(int seed)
        {
            var sim = G2ReviewFixtures.NewJp(seed);
            // DESIGN_PLACEHOLDER fixture: last wave is defeated while Fever has
            // time and hit budget left, as in the recorded natural Fever run.
            sim.WaveIndex = 1;
            for (var i = 0; i < sim.Enemies.Count; i++)
                sim.Enemies[i].Hp = 0;
            sim.FeverActive = true;
            sim.FeverLeft = 0.25f;
            sim.FeverHitsLeft = 68;
            return sim;
        }

        static BattleSim BeforeVictoryWithScalingClock(int seed)
        {
            var sim = BeforeVictory(seed);
            sim.Clocks.FeverWindowScalesWithSpeed = true;
            sim.FeverLeft = 1f;
            return sim;
        }
    }
}
