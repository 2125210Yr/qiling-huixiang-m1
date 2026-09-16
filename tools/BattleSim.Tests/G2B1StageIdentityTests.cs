using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// B1-A3 — Compute / DataIdentity must hash the StageDef the sim actually runs,
    /// not only the Catalog row found by Id.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2B1StageIdentityTests
    {
        const int Seed = 1303;
        const string UninstalledId = "B1-A3-UNINSTALLED";

        [Fact]
        public void UninstalledSameIdStages_DifferentTimeLimitOrHpMul_FlipComputeAndDataIdentity()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var party = Catalog.DefaultParty;
                    var profile = FormulaProfile.JP_LEGACY_EMPIRICAL;
                    var fp0 = BattleContentIdentity.Fingerprint();

                    var timeA = CloneUninstalled(120f, 1.0f);
                    var timeB = CloneUninstalled(180f, 1.0f);
                    AssertDistinctIdentities(party, profile, timeA, timeB);

                    var hpA = CloneUninstalled(120f, 1.0f);
                    var hpB = CloneUninstalled(120f, 3.5f);
                    AssertDistinctIdentities(party, profile, hpA, hpB);

                    Assert.Equal(fp0, BattleContentIdentity.Fingerprint());
                    Assert.Null(BattleContentIdentity.FindStage(UninstalledId));
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        static void AssertDistinctIdentities(string[] party, FormulaProfile profile, StageDef stageA, StageDef stageB)
        {
            Assert.Equal(stageA.Id, stageB.Id);
            var simA = NewSim(party, profile, stageA);
            var simB = NewSim(party, profile, stageB);

            var computeA = BattleContentIdentity.Compute(simA, party, stageA.Id);
            var computeB = BattleContentIdentity.Compute(simB, party, stageB.Id);
            Assert.NotEqual(computeA, computeB);

            var headerA = BattleInitialHeader.Snapshot(simA, party, stageA.Id, true);
            var headerB = BattleInitialHeader.Snapshot(simB, party, stageB.Id, true);
            Assert.NotEqual(headerA.DataIdentity, headerB.DataIdentity);
            Assert.Equal(0, simA.TickIndex);
            Assert.Equal(0, simB.TickIndex);
        }

        static BattleSim NewSim(string[] party, FormulaProfile profile, StageDef stage)
        {
            return new BattleSim(party, 0, Seed, stage, null)
            {
                Profile = profile,
                Speed = 1,
                Auto = AutoMode.Manual
            };
        }

        static StageDef CloneUninstalled(float timeLimitSec, float enemyHpMul)
        {
            var src = Catalog.VerticalSliceStage;
            return new StageDef
            {
                Id = UninstalledId,
                Name = "B1-A3 uninstalled clone",
                TimeLimitSec = timeLimitSec,
                Wave0 = src != null && src.Wave0 != null ? (string[])src.Wave0.Clone() : new[] { "E001" },
                Wave1 = src != null && src.Wave1 != null ? (string[])src.Wave1.Clone() : new string[0],
                EnemyHpMul = enemyHpMul,
                EnemyAtkMul = src != null ? src.EnemyAtkMul : 1f,
                EnemyDefMul = src != null ? src.EnemyDefMul : 1f,
                Difficulty = src != null ? src.Difficulty : 0
            };
        }
    }
}
