using System.IO;
using Resonance.App;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// A53-T05 / T06 — real NaturalPlayBattleEvidence.Begin/Persist/WriteSessionIndex.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2A53EvidenceTests
    {
        [Fact]
        public void T05_TwoFights_ReloadFight1AfterFight2Persist()
        {
            var session = G2A53EvidenceTape.RecordTwoFights();
            try
            {
                G2A53EvidenceTape.AssertTwoBattlesIsolated(session);

                var first = G2A53EvidenceTape.Reload(session.Fight1);
                Assert.Contains(session.Fight1.BattleId, first.Scenario, System.StringComparison.Ordinal);
                Assert.Contains(session.Fight1.BattleId, first.Result, System.StringComparison.Ordinal);
                Assert.Equal(session.Fight1.FrozenHeader.Trim(), first.Header.Trim());
                Assert.Equal(session.Fight1.Digest.Trim(), first.Digest.Trim());
                Assert.Contains("seed=" + session.Sim1.Seed, first.Header, System.StringComparison.Ordinal);
                Assert.DoesNotContain("seed=" + session.Sim2.Seed, first.Header, System.StringComparison.Ordinal);
                Assert.Contains("Tap", first.Commands, System.StringComparison.Ordinal);
                Assert.DoesNotContain(session.Fight2.BattleId, first.Commands, System.StringComparison.Ordinal);
                Assert.False(string.IsNullOrEmpty(first.Events));
            }
            finally
            {
                G2A53EvidenceTape.Dispose(session);
            }
        }

        [Fact]
        public void T06_TypedRecorder_WriteSessionIndex_AndRefuseUnboundLiveSim()
        {
            var session = G2A53EvidenceTape.RecordTwoFights();
            try
            {
                Assert.IsType<NaturalPlayBattleEvidence>(session.Fight1);
                Assert.IsType<NaturalPlayBattleEvidence>(session.Fight2);
                Assert.Same(session.Sim1, session.Fight1.Sim);
                Assert.Same(session.Sim2, session.Fight2.Sim);

                G2A53EvidenceTape.AssertSessionIndexKeepsRun7Fail(session);
                var eventsAlias = Path.Combine(session.Root, "natural-play.events.txt");
                Assert.True(File.Exists(eventsAlias));
                Assert.Contains(session.Fight1.BattleId, File.ReadAllText(eventsAlias));
                Assert.Contains(session.Fight2.BattleId, File.ReadAllText(eventsAlias));

                G2A53EvidenceTape.AssertPersistRefusesUnboundLiveSim(session);
            }
            finally
            {
                G2A53EvidenceTape.Dispose(session);
            }
        }

        [Fact]
        public void T06_Begin_BindsSimAndFreezesHeader()
        {
            var sim = NewJp(5307, AutoMode.Manual, forceNoCrit: true);
            var ev = NaturalPlayBattleEvidence.Begin(sim, "a53-bind", 1, VerificationScenario.Basic);
            Assert.Same(sim, ev.Sim);
            Assert.Equal(NaturalPlayBattleEvidence.MakeBattleId("a53-bind", 1), ev.BattleId);
            Assert.Equal(sim.Seed, ev.Seed);
            Assert.Equal(sim.RunHeader(), ev.FrozenHeader);
            Assert.Equal(VerificationScenario.Basic.Name, ev.ScenarioName);
            Assert.False(ev.Persisted);
        }
    }
}
