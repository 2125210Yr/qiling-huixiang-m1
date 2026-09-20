using Resonance.Battle;
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

        static ReplayReport Replay(BattleSim original)
        {
            var record = BattleRunRecord.Capture(original, Catalog.DefaultParty, original.ActiveStage.Id);
            // Exercise the same serialized digest boundary as natural-play readback.
            record = BattleRunRecord.ParseJsonLines(record.ToJsonLines());
            return BattleReplayer.Verify(record, BeforeVictory);
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
    }
}
