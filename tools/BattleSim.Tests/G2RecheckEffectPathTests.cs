using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// E01–E03 — SkillDef+Submit / enemy TickUnit. ApplyStatus is setup-only, never the path under test.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2RecheckEffectPathTests
    {
        [Fact]
        public void E01_ChargeSpeed_AllAllies_ViaSkillDefSubmit()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var fx = ChargeSpeedFx();
                    InstallEffect(fx);

                    var sim = NewJp(1501, AutoMode.Manual, forceNoCrit: true);
                    HoldTheLine(sim);
                    var slot = SlotOf(sim, "C005");
                    Assert.True(slot >= 0, "C005 missing from DefaultParty");
                    var tapId = sim.Allies[slot].Def.SlideSkillId;
                    sim.OverlaySkill(tapId, AllAlliesSupport(tapId, SkillType.Slide, fx.Id, 14));

                    ChargeAll(sim);
                    var accepted = Submit(sim, BattleCommandKind.Slide, slot);
                    Assert.True(accepted.Accepted, "E01: Slide Submit rejected: " + accepted.Reason);

                    for (int i = 0; i < sim.Allies.Length; i++)
                    {
                        var u = sim.Allies[i];
                        if (u == null || !u.Alive) continue;
                        Assert.True(u.Has(EffectKind.ChargeSpeed), "E01: ally slot " + i + " missing ChargeSpeed after AllAllies Submit");
                        Assert.Equal(1.25f, u.ChargeSpeedMul, 4);
                    }
                    for (int i = 0; i < sim.Enemies.Count; i++)
                    {
                        var e = sim.Enemies[i];
                        if (e == null || !e.Alive) continue;
                        Assert.False(e.Has(EffectKind.ChargeSpeed), "E01: enemy slot " + i + " received ChargeSpeed (Cast whitelist sent foes)");
                        Assert.Equal(1f, e.ChargeSpeedMul, 4);
                    }
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void E02_ChargeAmount_AllAllies_ViaSkillDefSubmit()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    const float add = 20f;
                    var fx = ChargeAmountFx(mag: add);
                    InstallEffect(fx);

                    var sim = NewJp(1502, AutoMode.Manual, forceNoCrit: true);
                    HoldTheLine(sim);
                    var slot = SlotOf(sim, "C005");
                    Assert.True(slot >= 0);
                    var skillId = sim.Allies[slot].Def.SlideSkillId;
                    sim.OverlaySkill(skillId, AllAlliesSupport(skillId, SkillType.Slide, fx.Id, 14));

                    ChargeAll(sim);
                    for (int i = 0; i < sim.Allies.Length; i++)
                        if (i != slot && sim.Allies[i] != null) sim.Allies[i].Charge = 40f;
                    for (int i = 0; i < sim.Enemies.Count; i++)
                        if (sim.Enemies[i] != null) sim.Enemies[i].Charge = 10f;

                    var accepted = Submit(sim, BattleCommandKind.Slide, slot);
                    Assert.True(accepted.Accepted, "E02 ChargeAmount Submit rejected: " + accepted.Reason);

                    Assert.Equal(add, sim.Allies[slot].Charge, 3);
                    for (int i = 0; i < sim.Allies.Length; i++)
                    {
                        if (i == slot || sim.Allies[i] == null || !sim.Allies[i].Alive) continue;
                        Assert.Equal(60f, sim.Allies[i].Charge, 3);
                    }
                    for (int i = 0; i < sim.Enemies.Count; i++)
                    {
                        var e = sim.Enemies[i];
                        if (e == null || !e.Alive) continue;
                        Assert.Equal(10f, e.Charge, 3);
                    }
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void E02_Barrier_AllAllies_ViaSkillDefSubmit()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var fx = BarrierFx();
                    InstallEffect(fx);

                    var sim = NewJp(1503, AutoMode.Manual, forceNoCrit: true);
                    HoldTheLine(sim);
                    var slot = SlotOf(sim, "C007");
                    Assert.True(slot >= 0, "C007 missing from DefaultParty");
                    var skillId = sim.Allies[slot].Def.SlideSkillId;
                    sim.OverlaySkill(skillId, AllAlliesSupport(skillId, SkillType.Slide, fx.Id, 14));

                    ChargeAll(sim);
                    var accepted = Submit(sim, BattleCommandKind.Slide, slot);
                    Assert.True(accepted.Accepted, "E02 Barrier Submit rejected: " + accepted.Reason);

                    for (int i = 0; i < sim.Allies.Length; i++)
                    {
                        var u = sim.Allies[i];
                        if (u == null || !u.Alive) continue;
                        Assert.True(u.Has(EffectKind.Barrier), "E02: ally slot " + i + " missing Barrier after AllAllies Submit");
                        Assert.True(u.Shield > 0, "E02: ally slot " + i + " Barrier applied but Shield=0");
                    }
                    for (int i = 0; i < sim.Enemies.Count; i++)
                    {
                        var e = sim.Enemies[i];
                        if (e == null || !e.Alive) continue;
                        Assert.False(e.Has(EffectKind.Barrier), "E02: enemy slot " + i + " received Barrier");
                        Assert.Equal(0, e.Shield);
                    }
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void E03_EnemySilence_BlocksTapSlide_AutoAttackStillRuns()
        {
            var sim = NewJp(1504, AutoMode.Manual, forceNoCrit: true);
            InflateAllies(sim);
            InflateEnemies(sim);
            SuppressAllyAutoAttacks(sim);

            var foe = sim.Enemies[0];
            Assert.NotNull(foe);
            for (int i = 1; i < sim.Enemies.Count; i++)
            {
                var stun = Catalog.TryEffect("stun");
                Assert.NotNull(stun);
                sim.ApplyStatus(sim.Enemies[i], stun);
            }

            sim.ApplyStatus(foe, SilenceFx("g2r_enemy_silence", 2f));
            Assert.True(foe.Has(EffectKind.Silence));
            Assert.True(foe.SkillLocked);
            Assert.False(foe.ActionLocked);

            foe.Charge = 100f;
            foe.SlideCd = 0f;
            foe.AutoTimer = 100f;
            var skill0 = CountEnemySkillCasts(sim, 0);
            var auto0 = CountEnemyAutoCasts(sim, 0);

            sim.Tick();

            Assert.True(CountEnemyAutoCasts(sim, 0) > auto0, "E03: silenced enemy did not auto-attack");
            Assert.Equal(skill0, CountEnemySkillCasts(sim, 0));

            for (int i = 0; i < BattleSim.TickHz * 3 && foe.Has(EffectKind.Silence); i++)
            {
                foe.Charge = 0f;
                foe.AutoTimer = -100000f;
                sim.Tick();
            }
            Assert.False(foe.Has(EffectKind.Silence), "E03: silence should expire so skills can resume");
            Assert.False(foe.SkillLocked);

            var skill1 = CountEnemySkillCasts(sim, 0);
            foe.Charge = 100f;
            foe.SlideCd = 0f;
            foe.AutoTimer = -100000f;
            sim.Tick();
            Assert.True(
                CountEnemySkillCasts(sim, 0) > skill1,
                "E03: after Silence expires the enemy must be able to Tap/Slide again");
        }
    }
}
