using System;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// A53-T01 / T02 — incomplete skills must not Accepted+spend+damage+skip fx.
    /// Historical inventory tolerance is not a playable exemption.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2A53PlayableGateTests
    {
        const string ReflectId = "a53_t01_reflect";

        [Fact]
        public void T01_DamageSkill_UnsupportedStatusApplyReflect_SubmitMustNotAcceptSpendDamageAndSkipFx()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var fx = UnsupportedReflect(ReflectId);
                    Assert.False(
                        EffectCapability.Check(fx).Ok,
                        "A53-T01 fixture: status.apply+Reflect must be unplayable");

                    var sim = NewJp(5301, AutoMode.Manual, forceNoCrit: true);
                    HoldTheLine(sim);
                    var slot = SlotOf(sim, "C001");
                    Assert.True(slot >= 0, "C001 missing from DefaultParty");

                    var tapId = sim.Allies[slot].Def.TapSkillId;
                    var src = Catalog.MustSkill(tapId);
                    var skill = Catalog.CloneSkill(src);
                    skill.EffectId = fx.Id;
                    Assert.True(
                        skill.AtkCoef > 0f || skill.FlatPower > 0,
                        "A53-T01 fixture: C001 tap must remain a damage skill");
                    Assert.False(
                        EffectCapability.IsPlayable(skill, OverlayEffects(fx)),
                        "A53-T01: damage skill + Reflect must not be playable");

                    sim.OverlaySkill(tapId, skill);
                    sim.OverlayEffect(fx.Id, fx);

                    ChargeAll(sim);
                    var chargeBefore = sim.Allies[slot].Charge;
                    var driveBefore = sim.Drive;
                    var hpBefore = SumEnemyHp(sim);

                    var result = Submit(sim, BattleCommandKind.Tap, slot);

                    var spent = sim.Allies[slot].Charge < chargeBefore || sim.Drive > driveBefore;
                    var damaged = SumEnemyHp(sim) < hpBefore;
                    var skippedFx = HasEventKind(sim, "unplayable_effect") || HasEventKind(sim, "missing_effect");

                    Assert.False(
                        result.Accepted && spent && damaged && skippedFx,
                        "A53-T01: Submit/entry must not Accepted+spend+damage+skip fx. Accepted="
                        + result.Accepted + " reason=" + result.Reason
                        + " spent=" + spent + " damaged=" + damaged + " skippedFx=" + skippedFx);

                    Assert.False(
                        result.Accepted,
                        "A53-T01: legal Submit must refuse an incomplete skill, not return Accepted. Reason="
                        + result.Reason);
                    Assert.Equal(chargeBefore, sim.Allies[slot].Charge);
                    Assert.Equal(driveBefore, sim.Drive);
                    Assert.Equal(hpBefore, SumEnemyHp(sim));
                    Assert.False(
                        skippedFx,
                        "A53-T01: must not skip the linked effect after a successful cast");
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void T02_OverlayExistingAtkUpToReflect_ReferencingSkillsNotSilentlyPlayable()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var json = "{\"effects\":[{\"id\":\"atk_up\",\"kind\":" + (int)EffectKind.Reflect + "}]}";
                    var loadInstalled = false;
                    try
                    {
                        CatalogJson.Load(json);
                        loadInstalled = true;
                    }
                    catch (ContentValidationException)
                    {
                        // Import may later block the overlay. Inventory mutation below
                        // still proves the historical ID is not a playable exemption.
                    }

                    var atk = Catalog.TryEffect("atk_up");
                    Assert.NotNull(atk);
                    atk.Kind = EffectKind.Reflect;
                    atk.Opcode = EffectOpcodes.StatusApply;

                    var slide = Catalog.MustSkill("C005_slide");
                    Assert.Equal("atk_up", slide.EffectId);
                    Assert.False(
                        Catalog.IsPlayable(slide),
                        "A53-T02: C005_slide must not stay playable after atk_up→Reflect");
                    if (loadInstalled)
                    {
                        Assert.False(
                            Catalog.PlayableSkills.ContainsKey("C005_slide"),
                            "A53-T02: PlayableSkills must not keep a skill whose overlay is unsupported Reflect");
                    }

                    var sim = NewJp(5302, AutoMode.Manual, forceNoCrit: true);
                    HoldTheLine(sim);
                    sim.OverlayEffect("atk_up", Catalog.CloneEffect(atk));

                    var slot = SlotOf(sim, "C005");
                    Assert.True(slot >= 0, "C005 missing from DefaultParty");
                    ChargeAll(sim);
                    var chargeBefore = sim.Allies[slot].Charge;
                    var driveBefore = sim.Drive;

                    var result = Submit(sim, BattleCommandKind.Slide, slot);
                    var spent = sim.Allies[slot].Charge < chargeBefore || sim.Drive > driveBefore;
                    var skippedFx = HasEventKind(sim, "unplayable_effect") || HasEventKind(sim, "missing_effect");

                    Assert.False(
                        result.Accepted && spent && skippedFx,
                        "A53-T02: historical atk_up id overlayed to Reflect must not Accepted+spend+skip fx. Accepted="
                        + result.Accepted + " reason=" + result.Reason
                        + " spent=" + spent + " skippedFx=" + skippedFx);
                    Assert.False(
                        result.Accepted,
                        "A53-T02: referencing skill must not be silently playable. Reason=" + result.Reason);
                    Assert.Equal(chargeBefore, sim.Allies[slot].Charge);
                    Assert.Equal(driveBefore, sim.Drive);
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        static EffectDef UnsupportedReflect(string id)
        {
            return new EffectDef
            {
                Id = id,
                Opcode = EffectOpcodes.StatusApply,
                Kind = EffectKind.Reflect,
                Magnitude = 0.20f,
                DurationSec = 8f,
                MaxStack = 1,
                SourceTier = 1,
                Group = id
            };
        }

        static System.Collections.Generic.Dictionary<string, EffectDef> OverlayEffects(EffectDef fx)
        {
            var map = new System.Collections.Generic.Dictionary<string, EffectDef>(StringComparer.Ordinal);
            foreach (var kv in Catalog.Effects)
                map[kv.Key] = kv.Value;
            map[fx.Id] = fx;
            return map;
        }

        static int SumEnemyHp(BattleSim sim)
        {
            var hp = 0;
            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                var e = sim.Enemies[i];
                if (e != null) hp += e.Hp;
            }
            return hp;
        }

        static bool HasEventKind(BattleSim sim, string kind)
        {
            if (sim == null || sim.Events == null || sim.Events.Events == null) return false;
            for (int i = 0; i < sim.Events.Events.Count; i++)
            {
                var e = sim.Events.Events[i];
                if (e != null && string.Equals(e.Kind, kind, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
