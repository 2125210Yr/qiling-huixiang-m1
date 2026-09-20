using System;
using System.IO;
using Resonance.Battle;
using Xunit;
using Xunit.Abstractions;

namespace Resonance.Tests
{
    // Each test retains its own temporary directory. These are real short battle prefixes, not UI runs.
    public sealed class OriginalReplayStoreTests
    {
        readonly ITestOutputHelper _output;
        public OriginalReplayStoreTests(ITestOutputHelper output) { _output = output; }

        sealed class Rig
        {
            public readonly string Root = Path.Combine(Path.GetTempPath(), "original-replay-store-tests", Guid.NewGuid().ToString("N"));
            public readonly OriginalProfileStore Store;
            public readonly ExpeditionFlow Flow;
            public readonly BattleSim Sim;
            public string ReplayDirectory => Path.Combine(Root, "replays");
            public Rig()
            {
                Store = new OriginalProfileStore(Path.Combine(Root, "profile.v1.json"));
                Flow = new ExpeditionFlow(Store); Flow.StartRun("C01", "single", 260921);
                Sim = RunBattleFactory.Create(Flow.BeginBattle());
                for (int i = 0; i < 120 && Sim.Outcome == BattleOutcome.InProgress; i++) Sim.Tick();
                Assert.NotEmpty(Sim.ExpeditionResolutions);
            }
        }

        [Fact]
        public void SavedShortBattleDeserializesAndVerifiesFromItsFrozenOpening()
        {
            var rig = new Rig(); var record = OriginalBattleRecord.Capture(rig.Sim);
            var saved = OriginalReplayStore.SaveVerified(record, rig.ReplayDirectory);
            Assert.True(File.Exists(saved)); Assert.Equal(Path.GetFullPath(rig.ReplayDirectory), Path.GetDirectoryName(saved));
            Assert.EndsWith(".original-replay.json", saved); Assert.False(File.Exists(saved + ".pending"));
            var text = File.ReadAllText(saved); var restored = OriginalBattleRecord.FromJson(text);
            var report = OriginalBattleReplayer.Verify(restored);
            Assert.True(report.Match, string.Join(" | ", report.Differences)); Assert.NotNull(report.Replayed);
            Assert.Equal(record.InputHash, restored.InputHash); Assert.Equal(record.Input.AttemptId, restored.Input.AttemptId);
            Assert.Equal(rig.Sim.TickIndex, report.Replayed.TickIndex);
            Assert.Equal(rig.Sim.Events.ComputeHash(), report.Replayed.Events.ComputeHash());
            _output.WriteLine("saved={0}; sha256={1}; endTick={2}; verified=true", saved, BattleEventLog.HashUtf8(text), restored.EndTick);
        }

        [Fact]
        public void RepeatedCaptureOfTheSameAttemptUsesUniqueNamesWithoutOverwriting()
        {
            var rig = new Rig(); var first = OriginalBattleRecord.Capture(rig.Sim);
            string firstPath = OriginalReplayStore.SaveVerified(first, rig.ReplayDirectory);
            byte[] firstBytes = File.ReadAllBytes(firstPath);
            for (int i = 0; i < 30; i++) rig.Sim.Tick();
            var second = OriginalBattleRecord.Capture(rig.Sim);
            Assert.Equal(first.Input.AttemptId, second.Input.AttemptId); Assert.NotEqual(first.EndTick, second.EndTick);
            string secondPath = OriginalReplayStore.SaveVerified(second, rig.ReplayDirectory);
            Assert.NotEqual(firstPath, secondPath); Assert.Equal(firstBytes, File.ReadAllBytes(firstPath));
            Assert.Equal(2, Directory.GetFiles(rig.ReplayDirectory, "*.original-replay.json", SearchOption.TopDirectoryOnly).Length);
            Assert.Empty(Directory.GetFiles(rig.ReplayDirectory, "*.pending", SearchOption.TopDirectoryOnly));
            var firstParsed = OriginalBattleRecord.FromJson(File.ReadAllText(firstPath));
            var secondParsed = OriginalBattleRecord.FromJson(File.ReadAllText(secondPath));
            Assert.Equal(first.EndTick, firstParsed.EndTick); Assert.Equal(second.EndTick, secondParsed.EndTick);
            Assert.True(OriginalBattleReplayer.Verify(firstParsed).Match); Assert.True(OriginalBattleReplayer.Verify(secondParsed).Match);
            _output.WriteLine("sameAttempt={0}; retainedFirst={1}; uniqueSecond={2}", first.Input.AttemptId, firstPath, secondPath);
        }

        [Fact]
        public void MismatchingExpectedStateIsRejectedWithoutPublishingAnyFile()
        {
            var rig = new Rig(); Directory.CreateDirectory(rig.ReplayDirectory);
            var record = OriginalBattleRecord.Capture(rig.Sim); record.FinalState.BarrierEnergy++;
            var error = Assert.Throws<InvalidOperationException>(() => OriginalReplayStore.SaveVerified(record, rig.ReplayDirectory));
            Assert.Contains("mismatch", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(Directory.GetFiles(rig.ReplayDirectory, "*", SearchOption.TopDirectoryOnly));
            _output.WriteLine("rejectedDirectory={0}; publishedFiles=0; error={1}", rig.ReplayDirectory, error.Message);
        }

        [Fact]
        public void DirectoryPathThatIsAFileRaisesIoErrorWithoutChangingCallerBattleOrProfile()
        {
            var rig = new Rig(); var record = OriginalBattleRecord.Capture(rig.Sim);
            var blocker = Path.Combine(rig.Root, "replays-is-a-file"); File.WriteAllText(blocker, "explicit IO fixture");
            var beforeState = OriginalBattleSnapshot.Capture(rig.Sim).ToCanonicalString();
            var beforeRecord = record.ToJson(); var beforeProfile = File.ReadAllBytes(rig.Store.FilePath);
            var beforeBackup = File.ReadAllBytes(rig.Store.FilePath + ".bak");
            long beforeRevision = rig.Flow.Profile.Revision;
            Assert.Throws<IOException>(() => OriginalReplayStore.SaveVerified(record, blocker));
            Assert.Equal("explicit IO fixture", File.ReadAllText(blocker));
            Assert.Equal(beforeState, OriginalBattleSnapshot.Capture(rig.Sim).ToCanonicalString());
            Assert.Equal(beforeRecord, record.ToJson()); Assert.Equal(beforeRevision, rig.Flow.Profile.Revision);
            Assert.Equal(beforeProfile, File.ReadAllBytes(rig.Store.FilePath));
            Assert.Equal(beforeBackup, File.ReadAllBytes(rig.Store.FilePath + ".bak"));
            Assert.Equal(ExpeditionStatus.Battle, new OriginalProfileStore(rig.Store.FilePath).Load().ActiveRun.Status);
            Assert.Empty(Directory.GetFiles(rig.Root, "*.original-replay.json", SearchOption.TopDirectoryOnly));
            _output.WriteLine("blockedPath={0}; battleUnchanged=true; profileUnchanged=true; revision={1}", blocker, beforeRevision);
        }
    }
}
