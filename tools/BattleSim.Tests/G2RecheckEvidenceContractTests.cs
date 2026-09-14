using Resonance.App;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// L01/L02 — strongly-typed NaturalPlayBattleEvidence.Begin/Persist/WriteSessionIndex.
    /// Unity NEXT/HOME timing remains N01–N03 (Skip). Do not invent a second store type.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2RecheckEvidenceContractTests
    {
        [Fact]
        public void L01_TwoBattleIds_FirstFightEventsSurviveNext()
        {
            var session = G2A53EvidenceTape.RecordTwoFights();
            try
            {
                Assert.IsType<NaturalPlayBattleEvidence>(session.Fight1);
                Assert.IsType<NaturalPlayBattleEvidence>(session.Fight2);
                G2A53EvidenceTape.AssertTwoBattlesIsolated(session);
            }
            finally
            {
                G2A53EvidenceTape.Dispose(session);
            }
        }

        [Fact]
        public void L02_EachBattle_HasFrozenHeaderCommandsAndExitSnapshot()
        {
            var session = G2A53EvidenceTape.RecordTwoFights();
            try
            {
                G2A53EvidenceTape.AssertEachBattleHasFullTape(session);
                G2A53EvidenceTape.AssertSessionIndexKeepsRun7Fail(session);
            }
            finally
            {
                G2A53EvidenceTape.Dispose(session);
            }
        }
    }
}
