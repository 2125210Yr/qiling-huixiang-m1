using System;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// A53-T03 — fields the sim actually consumes must enter BattleContentIdentity.
    /// Catalog rows use Fingerprint(); per-fight policy uses Compute().
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2A53IdentityTests
    {
        [Theory]
        [InlineData("Side")]
        [InlineData("HasTarget")]
        [InlineData("Target")]
        [InlineData("Opcode")]
        [InlineData("Group")]
        [InlineData("SourceTier")]
        [InlineData("Element")]
        [InlineData("AutoSkillId")]
        [InlineData("FlatHeal")]
        [InlineData("HonorDeclaredAutoDriveGain")]
        public void T03_SingleFieldChange_FlipsBattleContentIdentity(string field)
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                DesignPlaceholderPolicy.Bind(null);
                try
                {
                    var sim = NewJp(5303, AutoMode.Manual, forceNoCrit: true);
                    var party = Catalog.DefaultParty;
                    var stageId = Catalog.VerticalSliceStage != null ? Catalog.VerticalSliceStage.Id : "VS-1";
                    var fp0 = BattleContentIdentity.Fingerprint();
                    var compute0 = BattleContentIdentity.Compute(sim, party, stageId);

                    Flip(field);

                    if (string.Equals(field, "HonorDeclaredAutoDriveGain", StringComparison.Ordinal))
                    {
                        var compute1 = BattleContentIdentity.Compute(sim, party, stageId);
                        Assert.NotEqual(
                            compute0,
                            compute1);
                    }
                    else
                    {
                        var fp1 = BattleContentIdentity.Fingerprint();
                        var compute1 = BattleContentIdentity.Compute(sim, party, stageId);
                        Assert.NotEqual(fp0, fp1);
                        Assert.NotEqual(compute0, compute1);
                    }
                }
                finally
                {
                    DesignPlaceholderPolicy.Bind(null);
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void T03_IdenticalConfig_IdentityStable()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                DesignPlaceholderPolicy.Bind(null);
                try
                {
                    var sim = NewJp(5303, AutoMode.Manual, forceNoCrit: true);
                    var party = Catalog.DefaultParty;
                    var stageId = Catalog.VerticalSliceStage != null ? Catalog.VerticalSliceStage.Id : "VS-1";
                    Assert.Equal(BattleContentIdentity.Fingerprint(), BattleContentIdentity.Fingerprint());
                    Assert.Equal(
                        BattleContentIdentity.Compute(sim, party, stageId),
                        BattleContentIdentity.Compute(sim, party, stageId));
                    Assert.Equal(BattleSim.ContentFingerprint(), BattleSim.ContentFingerprint());
                }
                finally
                {
                    DesignPlaceholderPolicy.Bind(null);
                    RestoreBuiltinCatalog();
                }
            }
        }

        static void Flip(string field)
        {
            switch (field)
            {
                case "Side":
                    Catalog.TryEffect("atk_up").Side = TargetSide.Foe;
                    break;
                case "HasTarget":
                    Catalog.TryEffect("atk_up").HasTarget = true;
                    break;
                case "Target":
                    Catalog.TryEffect("atk_up").Target = TargetRule.AllEnemies;
                    break;
                case "Opcode":
                    Catalog.TryEffect("atk_up").Opcode = EffectOpcodes.ControlApply;
                    break;
                case "Group":
                    Catalog.TryEffect("atk_up").Group = "a53_atk";
                    break;
                case "SourceTier":
                    Catalog.TryEffect("atk_up").SourceTier = Catalog.TryEffect("atk_up").SourceTier + 1;
                    break;
                case "Element":
                    Catalog.MustChar("C001").Element = Element.Water;
                    break;
                case "AutoSkillId":
                    Catalog.MustChar("C001").AutoSkillId = "C007_auto";
                    break;
                case "FlatHeal":
                    Catalog.MustSkill("C003_tap").FlatHeal = Catalog.MustSkill("C003_tap").FlatHeal + 17;
                    break;
                case "HonorDeclaredAutoDriveGain":
                    DesignPlaceholderPolicy.Bind(new VerificationScenario
                    {
                        Name = "a53-identity-honor",
                        HonorDeclaredAutoDriveGain = true,
                        DeclaredAutoDriveGain = 2,
                        ChargeTimeSec = 1.25f
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(field), field, "unknown identity field");
            }
        }
    }
}
