using System;
using Resonance.App;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;

namespace Resonance.Tests
{
    [Collection("G2RecheckCatalog")]
    public sealed class G2OpeningDefaultsAuditTests
    {
        [Theory]
        [InlineData("null")]
        [InlineData("empty")]
        [InlineData("all-null")]
        [InlineData("mixed")]
        public void ExplicitOpeningInputs_KeepDefaultsAndNullSlots_ThroughRecoveryAndReplay(string kind)
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var note = VerificationCatalog.Apply(VerificationRunPlan.Resolve("np.basic.v1", null));
                    Assert.DoesNotContain("apply failed", note, StringComparison.Ordinal);
                    NaturalPlayBattleEvidence.BindOpeningPolicy(new NaturalPlayBattleEvidence
                    {
                        ScenarioName = VerificationScenario.Basic.Name
                    });

                    var party = Catalog.DefaultParty;
                    var growth = OpeningInput(kind, party);
                    var stage = BattleContentIdentity.FindStage("NP-BASIC");
                    Assert.NotNull(stage);
                    var sim = new BattleSim(party, 0, 4501, stage, growth,
                        new BattleMods { FoodAtkMul = 1.2f, CartaMul = 1f })
                    {
                        Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                        Auto = AutoMode.Manual,
                        Speed = 1
                    };
                    sim.FreezeInitialHeader(party, stage.Id);
                    for (int i = 0; i < 60 && sim.Outcome == BattleOutcome.InProgress; i++)
                        sim.Tick();
                    var captured = BattleRunRecord.Capture(sim, party, stage.Id);
                    Assert.False(string.IsNullOrEmpty(captured.Initial.GrowthIdentity));

                    var recovered = OpeningGrowth.Recover(captured);
                    Assert.Equal(OpeningGrowth.SourceInput, captured.OpeningSource);
                    AssertInputShape(growth, recovered, allowEmptyAsNull: false);
                    AssertReplay(captured, sim);

                    var parsed = BattleRunRecord.ParseJsonLines(captured.ToJsonLines());
                    Assert.NotNull(parsed);
                    Assert.Equal(OpeningGrowth.SourceInput, parsed.OpeningSource);
                    AssertInputShape(growth, parsed.OpeningProgress, allowEmptyAsNull: true);
                    var restored = OpeningGrowth.Recover(parsed);
                    AssertInputShape(growth, restored, allowEmptyAsNull: true);
                    Assert.Equal(OpeningGrowth.SourceInput, parsed.OpeningSource);
                    Assert.Equal(captured.Initial.GrowthIdentity, parsed.Initial.GrowthIdentity);
                    Assert.Equal(captured.Initial.OpeningInputsIdentity, parsed.Initial.OpeningInputsIdentity);
                    Assert.Equal(captured.DataIdentity, parsed.DataIdentity);
                    AssertReplay(parsed, sim);
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                    DesignPlaceholderPolicy.Bind(null);
                }
            }
        }

        [Fact]
        public void LegacyRecord_WithoutInputSource_StillRecoversGrowthIdentity()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    DesignPlaceholderPolicy.Bind(null);
                    var party = Catalog.DefaultParty;
                    var sim = new BattleSim(party, 0, 4502);
                    sim.FreezeInitialHeader(party, Catalog.VerticalSliceStage.Id);
                    var legacy = new BattleRunRecord
                    {
                        PartyIds = party,
                        Initial = new BattleInitialHeader { GrowthIdentity = sim.InitialHeader.GrowthIdentity }
                    };

                    var recovered = OpeningGrowth.Recover(legacy);
                    Assert.Equal(OpeningGrowth.SourceLegacyRecovered, legacy.OpeningSource);
                    Assert.True(OpeningGrowth.HasRows(recovered));
                    var rebuilt = new BattleSim(party, 0, 4502, null, recovered);
                    rebuilt.FreezeInitialHeader(party, Catalog.VerticalSliceStage.Id);
                    Assert.Equal(sim.InitialHeader.GrowthIdentity, rebuilt.InitialHeader.GrowthIdentity);

                    var missing = new BattleRunRecord { PartyIds = party };
                    Assert.Null(OpeningGrowth.Recover(missing));
                    Assert.Equal(OpeningGrowth.SourceIncomplete, missing.OpeningSource);
                }
                finally
                {
                    RestoreBuiltinCatalog();
                    DesignPlaceholderPolicy.Bind(null);
                }
            }
        }

        static UnitProgress[] OpeningInput(string kind, string[] party)
        {
            if (kind == "null") return null;
            if (kind == "empty") return Array.Empty<UnitProgress>();
            var rows = new UnitProgress[party.Length];
            if (kind == "mixed")
                rows[1] = new UnitProgress { Id = party[1], Level = 20, Reserve = "SSSSS" };
            return rows;
        }

        static void AssertInputShape(UnitProgress[] expected, UnitProgress[] actual, bool allowEmptyAsNull)
        {
            if (expected == null || (allowEmptyAsNull && expected.Length == 0))
            {
                Assert.Null(actual);
                return;
            }
            Assert.NotNull(actual);
            Assert.NotSame(expected, actual);
            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] == null)
                    Assert.Null(actual[i]);
                else
                {
                    Assert.NotNull(actual[i]);
                    Assert.NotSame(expected[i], actual[i]);
                    Assert.Equal(expected[i].Id, actual[i].Id);
                    Assert.Equal(expected[i].Level, actual[i].Level);
                    Assert.Equal(expected[i].Reserve, actual[i].Reserve);
                }
            }
            Assert.Equal(OpeningGrowth.Format(expected), OpeningGrowth.Format(actual));
        }

        static void AssertReplay(BattleRunRecord record, BattleSim original)
        {
            var rebuilt = NaturalPlayBattleEvidence.NewSim(record);
            Assert.Null(NaturalPlayBattleEvidence.NamedOpeningDiff(record, rebuilt));
            Assert.Equal(record.Initial.OpeningInputsIdentity, rebuilt.InitialHeader.OpeningInputsIdentity);
            Assert.Equal(record.Initial.GrowthIdentity, rebuilt.InitialHeader.GrowthIdentity);
            var report = BattleReplayer.Verify(record, NaturalPlayBattleEvidence.ReplayFactory(record));
            Assert.True(report.Match, string.Join(" | ", report.Diff));
            Assert.NotSame(original, report.Replayed);
            Assert.Empty(report.DigestDiff);
            Assert.Empty(report.EventDiff);
            Assert.Equal(original.TickIndex, report.Replayed.TickIndex);
        }
    }
}
