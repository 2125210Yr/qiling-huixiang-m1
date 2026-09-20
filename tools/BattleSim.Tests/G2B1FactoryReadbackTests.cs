using System;
using Resonance.App;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// B1-F1 — NewSim / ReplayFactory recover opening growth from the tape.
    /// Does not inject HP/Drive/Charge/Fever. Does not weaken A53 T01/T02.
    /// Does not call EnsurePartyPlayable or auto-bind strip on BuildBuiltin.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2B1FactoryReadbackTests
    {
        const int Seed = 4101;
        const string Fight1Growth =
            "C001:2460/1550+0|C007:3760/760+0|C010:2640/990+0|C003:3160/1050+0|C005:2700/1040+0";

        [Fact]
        public void NewSim_WithoutGrowth_RecoversCaptureOpening_NamedOpeningDiffNull()
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

                    var growth = GrownParty();
                    var rec = CaptureGrown(growth);
                    Assert.False(string.IsNullOrEmpty(rec.Initial.GrowthIdentity));
                    Assert.True(OpeningGrowth.HasRows(rec.OpeningProgress));

                    var rebuilt = NaturalPlayBattleEvidence.NewSim(rec);
                    var snap = BattleInitialHeader.Snapshot(rebuilt, rec.PartyIds, rec.StageId, true);
                    Assert.Equal(rec.Initial.GrowthIdentity, snap.GrowthIdentity);
                    Assert.Null(NaturalPlayBattleEvidence.NamedOpeningDiff(rec, rebuilt));
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void Factory_NullGrowth_And_ExplicitRecovered_BothVerifyMatch()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    VerificationCatalog.Apply(VerificationRunPlan.Resolve("np.basic.v1", null));
                    NaturalPlayBattleEvidence.BindOpeningPolicy(new NaturalPlayBattleEvidence
                    {
                        ScenarioName = VerificationScenario.Basic.Name
                    });

                    var rec = CaptureGrown(GrownParty());
                    var recovered = NaturalPlayBattleEvidence.RecoverOpeningGrowth(rec);
                    Assert.True(OpeningGrowth.HasRows(recovered));

                    var named = NaturalPlayBattleEvidence.NewSim(rec, recovered);
                    var auto = NaturalPlayBattleEvidence.NewSim(rec, null);
                    Assert.Null(NaturalPlayBattleEvidence.NamedOpeningDiff(rec, named));
                    Assert.Null(NaturalPlayBattleEvidence.NamedOpeningDiff(rec, auto));

                    var viaNull = BattleReplayer.Verify(rec, NaturalPlayBattleEvidence.ReplayFactory(rec));
                    var viaExplicit = BattleReplayer.Verify(
                        rec,
                        NaturalPlayBattleEvidence.ReplayFactory(rec, recovered));
                    Assert.True(viaNull.Match, "null-growth factory Diff=" + DiffBlob(viaNull));
                    Assert.True(viaExplicit.Match, "explicit recovered factory Diff=" + DiffBlob(viaExplicit));

                    rec.OpeningProgress = null;
                    rec.OpeningSource = null; // Historical tapes also omit the opening-source field.
                    var fromIdentity = NaturalPlayBattleEvidence.RecoverOpeningGrowth(rec);
                    var identitySim = NaturalPlayBattleEvidence.NewSim(rec, fromIdentity);
                    var identityNamed = NaturalPlayBattleEvidence.NamedOpeningDiff(rec, identitySim);
                    var identitySnap = BattleInitialHeader.Snapshot(identitySim, rec.PartyIds, rec.StageId, true);
                    Assert.Equal(rec.Initial.GrowthIdentity, identitySnap.GrowthIdentity);
                    var identityReport = BattleReplayer.Verify(
                        rec,
                        NaturalPlayBattleEvidence.ReplayFactory(rec, fromIdentity));
                    if (!identityReport.Match || identityNamed != null)
                    {
                        Assert.True(
                            identityNamed == null || identityNamed.IndexOf("GrowthIdentity", StringComparison.Ordinal) < 0,
                            "identity search must at least restore GrowthIdentity. Named="
                            + identityNamed + " Diff=" + DiffBlob(identityReport));
                    }
                    else
                    {
                        Assert.True(identityReport.Match);
                        Assert.Null(identityNamed);
                    }
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void CatalogDefaultFactory_DoesNotSilentlyMatchGrownTape()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    VerificationCatalog.Apply(VerificationRunPlan.Resolve("np.basic.v1", null));
                    NaturalPlayBattleEvidence.BindOpeningPolicy(new NaturalPlayBattleEvidence
                    {
                        ScenarioName = VerificationScenario.Basic.Name
                    });

                    var rec = CaptureGrown(GrownParty());
                    var forced = NaturalPlayBattleEvidence.NewSim(rec, null, false);
                    var named = NaturalPlayBattleEvidence.NamedOpeningDiff(rec, forced);
                    Assert.False(string.IsNullOrEmpty(named), "catalog-default opening must name a Diff");
                    Assert.True(
                        named.StartsWith("GrowthIdentity", StringComparison.Ordinal)
                        || named.StartsWith("DataIdentity", StringComparison.Ordinal),
                        "catalog-default Diff must be Growth/Data identity, got " + named);

                    var report = BattleReplayer.Verify(
                        rec,
                        NaturalPlayBattleEvidence.ReplayFactory(rec, null, false));
                    Assert.False(report.Match, "recovery-off factory must not Match a grown tape");

                    var empty = NaturalPlayBattleEvidence.NewSim(rec, new UnitProgress[0]);
                    var emptyNamed = NaturalPlayBattleEvidence.NamedOpeningDiff(rec, empty);
                    Assert.False(string.IsNullOrEmpty(emptyNamed));
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void Fight1GrowthIdentity_Search_RestoresStarterMaxHp()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    VerificationCatalog.Apply(VerificationRunPlan.Resolve("np.basic.v1", null));
                    var rec = new BattleRunRecord
                    {
                        PartyIds = Catalog.DefaultParty,
                        StageId = "NP-BASIC",
                        Seed = Seed,
                        Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                        Initial = new BattleInitialHeader
                        {
                            GrowthIdentity = Fight1Growth,
                            PartyIds = Catalog.DefaultParty,
                            StageId = "NP-BASIC"
                        }
                    };
                    var recovered = OpeningGrowth.FromIdentity(Fight1Growth, Catalog.DefaultParty);
                    Assert.True(OpeningGrowth.HasRows(recovered));
                    var sim = NaturalPlayBattleEvidence.NewSim(rec, recovered);
                    Assert.Equal(2460, sim.Allies[0].MaxHp);
                    Assert.Equal(3760, sim.Allies[1].MaxHp);
                    Assert.Equal(2640, sim.Allies[2].MaxHp);
                    Assert.Equal(3160, sim.Allies[3].MaxHp);
                    Assert.Equal(2700, sim.Allies[4].MaxHp);
                    var snap = BattleInitialHeader.Snapshot(sim, rec.PartyIds, rec.StageId, true);
                    Assert.Equal(Fight1Growth, snap.GrowthIdentity);
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        static BattleRunRecord CaptureGrown(UnitProgress[] growth)
        {
            var party = Catalog.DefaultParty;
            var stage = BattleContentIdentity.FindStage("NP-BASIC");
            Assert.NotNull(stage);
            var sim = new BattleSim(party, 0, Seed, stage, growth)
            {
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                Speed = 1,
                Auto = AutoMode.Manual,
                ForceNoCrit = false
            };
            sim.FreezeInitialHeader(party, "NP-BASIC");
            var rec = BattleRunRecord.Capture(sim, party, "NP-BASIC");
            Assert.NotNull(rec.Initial);
            Assert.False(string.IsNullOrEmpty(rec.Initial.GrowthIdentity));
            return rec;
        }

        static UnitProgress[] GrownParty()
        {
            var ids = Catalog.DefaultParty;
            var a = new UnitProgress[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                a[i] = new UnitProgress
                {
                    Id = ids[i],
                    Level = 20,
                    Uncap = 2,
                    Ignition = 0,
                    Affection = 20
                };
            }
            a[0].Gear0 = "EQ_WPN";
            a[1].Gear1 = "EQ_ARM";
            a[2].Gear2 = "EQ_ACC";
            return a;
        }
    }
}
