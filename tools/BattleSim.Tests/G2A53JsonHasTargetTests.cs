using System;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// A53-T04 — JSON hasTarget/target must distinguish false / true+target /
    /// target-only / neither. Production bugs stay red.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2A53JsonHasTargetTests
    {
        [Fact]
        public void T04_HasTargetFalse_CancelsOverride()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    CatalogJson.Load("{\"effects\":[{\"id\":\"atk_up\",\"hasTarget\":false}]}");
                    var fx = Catalog.TryEffect("atk_up");
                    Assert.NotNull(fx);
                    Assert.False(
                        fx.HasTarget,
                        "A53-T04: overlay hasTarget:false must cancel target override, not force HasTarget=true");

                    var slide = Catalog.MustSkill("C005_slide");
                    Assert.Equal(TargetRule.AllAllies, slide.Target);
                    var rule = TargetSemantics.Rule(slide, fx);
                    Assert.Equal(TargetRule.AllAllies, rule);
                    Assert.NotEqual(TargetRule.Self, rule);
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void T04_HasTargetTrue_WithLegalTarget()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    CatalogJson.Load(
                        "{\"effects\":[{\"id\":\"atk_up\",\"hasTarget\":true,\"target\":"
                        + (int)TargetRule.AllEnemies + "}]}");
                    var fx = Catalog.TryEffect("atk_up");
                    Assert.NotNull(fx);
                    Assert.True(fx.HasTarget);
                    Assert.Equal(TargetRule.AllEnemies, fx.Target);
                    Assert.NotEqual(TargetRule.Self, fx.Target);
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void T04_TargetOnly_SetsHasTargetAndKeepsNamedRule()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    CatalogJson.Load(
                        "{\"effects\":[{\"id\":\"atk_up\",\"target\":"
                        + (int)TargetRule.AllAllies + "}]}");
                    var fx = Catalog.TryEffect("atk_up");
                    Assert.NotNull(fx);
                    Assert.True(
                        fx.HasTarget,
                        "A53-T04: target-only overlay is the compat form and must set HasTarget");
                    Assert.Equal(TargetRule.AllAllies, fx.Target);
                    Assert.NotEqual(TargetRule.Self, fx.Target);
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void T04_NeitherKey_KeepsDefinedSemantics()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                var before = Catalog.TryEffect("atk_up");
                Assert.NotNull(before);
                var has0 = before.HasTarget;
                var tgt0 = before.Target;
                var side0 = before.Side;
                try
                {
                    CatalogJson.Load("{\"effects\":[{\"id\":\"atk_up\",\"mag\":0.18}]}");
                    var fx = Catalog.TryEffect("atk_up");
                    Assert.NotNull(fx);
                    Assert.Equal(has0, fx.HasTarget);
                    Assert.Equal(tgt0, fx.Target);
                    Assert.Equal(side0, fx.Side);

                    CatalogJson.Load(
                        "{\"effects\":[{\"id\":\"a53_ht_neither\",\"op\":\""
                        + EffectOpcodes.StatusApply + "\",\"kind\":" + (int)EffectKind.AtkBuff
                        + ",\"mag\":0.1,\"dur\":4,\"stack\":1,\"tier\":1,\"group\":\"a53ht\"}]}");
                    var fresh = Catalog.TryEffect("a53_ht_neither");
                    Assert.NotNull(fresh);
                    Assert.False(
                        fresh.HasTarget,
                        "A53-T04: new row with neither hasTarget nor target must not invent HasTarget");
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void T04_HasTargetTrueWithoutTarget_MustErrorNotBecomeSelf()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    Exception caught = null;
                    try
                    {
                        CatalogJson.Load(
                            "{\"effects\":[{\"id\":\"a53_ht_true_notarget\",\"op\":\""
                            + EffectOpcodes.StatusApply + "\",\"kind\":" + (int)EffectKind.AtkBuff
                            + ",\"mag\":0.1,\"dur\":4,\"stack\":1,\"tier\":1,\"group\":\"a53ht\""
                            + ",\"hasTarget\":true}]}");
                    }
                    catch (Exception ex)
                    {
                        caught = ex;
                    }

                    if (caught == null)
                    {
                        var fx = Catalog.TryEffect("a53_ht_true_notarget");
                        Assert.True(
                            fx == null
                            || (fx.HasTarget && fx.Target != TargetRule.Self),
                            "A53-T04: hasTarget:true without target must error; must not silently become Self");
                        Assert.Fail(
                            "A53-T04: hasTarget:true without a legal target must reject the row");
                    }
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }
    }
}
