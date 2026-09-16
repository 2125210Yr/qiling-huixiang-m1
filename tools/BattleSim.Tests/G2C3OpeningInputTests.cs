using System;
using System.Collections.Generic;
using Resonance.App;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// C3 — Capture reads real opening inputs (Reserve + mods), not FromSim search.
    /// Catalog is the shared static; restore in finally. Collection is serial.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2C3OpeningInputTests
    {
        const int Seed = 4301;
        const int AutoTicks = 60;
        const string SlideReserve = "SSSSS";
        const string TapReserve = "TTTTT";
        const float FoodMul = 1.200f;

        [Fact]
        public void C3_T1_ReserveRoundTrip_DistinctAutoSequences_FactoryMatch()
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
                    var slideGrowth = GrownParty(SlideReserve);
                    var tapGrowth = GrownParty(TapReserve);
                    Assert.Equal(slideGrowth[0].Level, tapGrowth[0].Level);
                    Assert.Equal(slideGrowth[0].Gear0, tapGrowth[0].Gear0);

                    var recS = CaptureAuto(party, slideGrowth);
                    var recT = CaptureAuto(party, tapGrowth);

                    Assert.Equal(OpeningGrowth.SourceInput, recS.OpeningSource);
                    Assert.Equal(OpeningGrowth.SourceInput, recT.OpeningSource);
                    Assert.Equal(SlideReserve, recS.OpeningProgress[0].Reserve);
                    Assert.Equal(TapReserve, recT.OpeningProgress[0].Reserve);

                    var fmtS = OpeningGrowth.Format(recS.OpeningProgress);
                    var fmtT = OpeningGrowth.Format(recT.OpeningProgress);
                    Assert.Contains("r=" + SlideReserve, fmtS, StringComparison.Ordinal);
                    Assert.Contains("r=" + TapReserve, fmtT, StringComparison.Ordinal);
                    Assert.NotEqual(fmtS, fmtT);

                    var parsedS = OpeningGrowth.Parse(fmtS);
                    var parsedT = OpeningGrowth.Parse(fmtT);
                    Assert.Equal(SlideReserve, parsedS[0].Reserve);
                    Assert.Equal(TapReserve, parsedT[0].Reserve);
                    Assert.Equal(recS.OpeningProgress[0].SkinId ?? "", parsedS[0].SkinId ?? "");
                    Assert.Equal(recS.OpeningProgress[0].Level, parsedS[0].Level);
                    Assert.Equal(recS.OpeningProgress[0].Uncap, parsedS[0].Uncap);
                    Assert.Equal(recS.OpeningProgress[0].Ignition, parsedS[0].Ignition);
                    Assert.Equal(recS.OpeningProgress[0].Affection, parsedS[0].Affection);
                    Assert.Equal(recS.OpeningProgress[0].Gear0 ?? "", parsedS[0].Gear0 ?? "");

                    var old = OpeningGrowth.Parse("C001:lv=20;u=2");
                    Assert.Equal("EEEEE", old[0].Reserve);

                    var searched = OpeningGrowth.FromSim(
                        new BattleSim(party, 0, Seed, BattleContentIdentity.FindStage("NP-BASIC"), slideGrowth)
                        {
                            Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
                        },
                        party);
                    Assert.NotEqual(SlideReserve, searched[0].Reserve);

                    var viaParsedS = BattleReplayer.Verify(
                        recS, NaturalPlayBattleEvidence.ReplayFactory(recS, parsedS));
                    var viaParsedT = BattleReplayer.Verify(
                        recT, NaturalPlayBattleEvidence.ReplayFactory(recT, parsedT));
                    Assert.True(viaParsedS.Match, "SSSSS factory Diff=" + DiffBlob(viaParsedS));
                    Assert.True(viaParsedT.Match, "TTTTT factory Diff=" + DiffBlob(viaParsedT));

                    var kindsS = AutoSkillKinds(recS);
                    var kindsT = AutoSkillKinds(recT);
                    Assert.Contains(BattleCommandKind.Slide, kindsS);
                    Assert.Contains(BattleCommandKind.Tap, kindsT);
                    Assert.NotEqual(JoinKinds(kindsS), JoinKinds(kindsT));
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void C3_T2_FrozenInputs_IgnoreCallerMutation_LegacyIncompleteLabels()
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
                    var stage = BattleContentIdentity.FindStage("NP-BASIC");
                    Assert.NotNull(stage);
                    var growth = GrownParty(SlideReserve);
                    growth[0].Plus0 = 3;
                    growth[0].Ignition = 1;
                    growth[0].IgnAtk = 1;
                    growth[0].SkinId = "skin-c3";
                    var mods = new BattleMods { FoodAtkMul = FoodMul, CartaMul = 1f };

                    var sim = new BattleSim(party, 0, Seed, stage, growth, mods)
                    {
                        Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                        Speed = 1,
                        Auto = AutoMode.Manual,
                        ForceNoCrit = false
                    };
                    sim.FreezeInitialHeader(party, "NP-BASIC");
                    var frozenInputs = sim.InitialHeader.OpeningInputsIdentity;
                    var frozenGrowth = sim.InitialHeader.GrowthIdentity;
                    Assert.False(string.IsNullOrEmpty(frozenInputs));
                    Assert.Contains("r=" + SlideReserve, frozenInputs, StringComparison.Ordinal);
                    Assert.Contains("food=1.200", frozenInputs, StringComparison.Ordinal);

                    growth[0].Level = 1;
                    growth[0].Reserve = "EEEEE";
                    growth[0].Gear0 = "";
                    growth[0].Plus0 = 0;
                    growth[0].Ignition = 0;
                    mods.FoodAtkMul = 9f;

                    Assert.Equal(20, sim.OpeningGrowthInput[0].Level);
                    Assert.Equal(SlideReserve, sim.OpeningGrowthInput[0].Reserve);
                    Assert.Equal("EQ_WPN", sim.OpeningGrowthInput[0].Gear0);
                    Assert.Equal(3, sim.OpeningGrowthInput[0].Plus0);
                    Assert.Equal(FoodMul, sim.OpeningMods.FoodAtkMul, 3);
                    Assert.Equal(frozenInputs, sim.InitialHeader.OpeningInputsIdentity);
                    Assert.Equal(frozenGrowth, sim.InitialHeader.GrowthIdentity);

                    var rec = BattleRunRecord.Capture(sim, party, "NP-BASIC");
                    Assert.Equal(OpeningGrowth.SourceInput, rec.OpeningSource);
                    Assert.Equal(20, rec.OpeningProgress[0].Level);
                    Assert.Equal(SlideReserve, rec.OpeningProgress[0].Reserve);
                    Assert.Equal("EQ_WPN", rec.OpeningProgress[0].Gear0);
                    Assert.Equal(3, rec.OpeningProgress[0].Plus0);
                    Assert.Equal("skin-c3", rec.OpeningProgress[0].SkinId);
                    Assert.Equal(FoodMul, rec.OpeningMods.FoodAtkMul, 3);
                    Assert.Equal(frozenInputs, rec.Initial.OpeningInputsIdentity);

                    var json = rec.ToJsonLines();
                    Assert.Contains("\"openingSource\":\"input\"", json, StringComparison.Ordinal);
                    Assert.Contains("openingMods", json, StringComparison.Ordinal);
                    Assert.Contains("openingInputsIdentity", json, StringComparison.Ordinal);
                    var parsedTape = BattleRunRecord.ParseJsonLines(json);
                    Assert.Equal(OpeningGrowth.SourceInput, parsedTape.OpeningSource);
                    Assert.Equal(SlideReserve, parsedTape.OpeningProgress[0].Reserve);
                    Assert.Equal(FoodMul, parsedTape.OpeningMods.FoodAtkMul, 3);
                    Assert.Equal(frozenInputs, parsedTape.Initial.OpeningInputsIdentity);

                    var report = BattleReplayer.Verify(rec, NaturalPlayBattleEvidence.ReplayFactory(rec));
                    Assert.True(report.Match, "frozen-input factory Diff=" + DiffBlob(report));

                    var searchable = new BattleSim(party, 0, Seed, stage, GrownParty("EEEEE"))
                    {
                        Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
                    };
                    searchable.FreezeInitialHeader(party, "NP-BASIC");
                    var legacy = new BattleRunRecord
                    {
                        PartyIds = party,
                        StageId = "NP-BASIC",
                        Seed = Seed,
                        Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                        Initial = new BattleInitialHeader
                        {
                            GrowthIdentity = searchable.InitialHeader.GrowthIdentity,
                            PartyIds = party,
                            StageId = "NP-BASIC"
                        }
                    };
                    var legacyRows = OpeningGrowth.Recover(legacy);
                    Assert.Equal(OpeningGrowth.SourceLegacyRecovered, legacy.OpeningSource);
                    Assert.True(OpeningGrowth.HasRows(legacyRows));

                    var incomplete = new BattleRunRecord
                    {
                        PartyIds = new[] { "C001" },
                        StageId = "NP-BASIC",
                        Seed = Seed,
                        Initial = new BattleInitialHeader
                        {
                            GrowthIdentity = "C001:999999/1+0",
                            PartyIds = new[] { "C001" }
                        }
                    };
                    var incompleteRows = OpeningGrowth.Recover(incomplete);
                    Assert.Equal(OpeningGrowth.SourceIncomplete, incomplete.OpeningSource);
                    Assert.NotEqual(OpeningGrowth.SourceInput, incomplete.OpeningSource);
                    Assert.NotEqual(OpeningGrowth.SourceLegacyRecovered, incomplete.OpeningSource);
                    if (OpeningGrowth.HasRows(incompleteRows))
                        Assert.Equal(1, incompleteRows[0].Level);
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        static BattleRunRecord CaptureAuto(string[] party, UnitProgress[] growth)
        {
            var stage = BattleContentIdentity.FindStage("NP-BASIC");
            Assert.NotNull(stage);
            var sim = new BattleSim(party, 0, Seed, stage, growth)
            {
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                Speed = 1,
                Auto = AutoMode.Full,
                ForceNoCrit = false
            };
            sim.FreezeInitialHeader(party, "NP-BASIC");
            for (int i = 0; i < AutoTicks && sim.Outcome == BattleOutcome.InProgress; i++)
                sim.Tick();
            var rec = BattleRunRecord.Capture(sim, party, "NP-BASIC");
            Assert.NotNull(rec.OpeningProgress);
            return rec;
        }

        static UnitProgress[] GrownParty(string reserve)
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
                    Affection = 20,
                    Reserve = reserve
                };
            }
            a[0].Gear0 = "EQ_WPN";
            a[1].Gear1 = "EQ_ARM";
            a[2].Gear2 = "EQ_ACC";
            return a;
        }

        static List<BattleCommandKind> AutoSkillKinds(BattleRunRecord rec)
        {
            var kinds = new List<BattleCommandKind>();
            if (rec == null || rec.Commands == null) return kinds;
            for (int i = 0; i < rec.Commands.Count; i++)
            {
                var c = rec.Commands[i];
                if (c == null || !c.Accepted || c.Source != CommandSource.Auto) continue;
                if (c.Kind == BattleCommandKind.Tap || c.Kind == BattleCommandKind.Slide)
                    kinds.Add(c.Kind);
            }
            return kinds;
        }

        static string JoinKinds(List<BattleCommandKind> kinds)
        {
            if (kinds == null || kinds.Count == 0) return "";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < kinds.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(kinds[i]);
            }
            return sb.ToString();
        }
    }
}
