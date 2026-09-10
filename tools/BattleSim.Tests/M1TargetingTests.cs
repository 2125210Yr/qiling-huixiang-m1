using System;
using System.Collections.Generic;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// M1-G2-TARGET-RATIO：公开选取面的内部一致性夹具。
    /// Self=caster；LowestHp* 只比绝对 HP；LowestHpRatio* 只比 Hp/MaxHp。
    /// 平局用 Slot 次序，同 seed 可重复。不宣称 T07 还原。
    /// </summary>
    public sealed class M1TargetingTests
    {
        static readonly string[] Party = { "C001", "C003", "C007" };

        [Fact]
        public void SelfSelectsCasterNotLowestHpAlly()
        {
            Catalog.BuildBuiltin();
            var catalogTarget = Catalog.TrySkill("C003_tap").Target;
            var sim = NewSim(1, 3);
            OverlayTap(sim, "C003_tap", TargetRule.Self);
            var other = sim.Allies[0];
            var caster = sim.Allies[1];
            other.MaxHp = 4000;
            other.Hp = 1;
            caster.MaxHp = 800;
            caster.Hp = 40;
            var otherHp = other.Hp;
            var casterHp = caster.Hp;
            ClearObserve(sim);
            caster.Charge = 100f;
            Assert.True(sim.TryTap(1));
            Assert.Equal(otherHp, other.Hp);
            Assert.Equal(casterHp, caster.Hp);
            Assert.Equal(new[] { caster.Slot }, PickedSlots(sim, ally: true));
            Assert.Equal(catalogTarget, Catalog.TrySkill("C003_tap").Target);
        }

        [Fact]
        public void LowestHpAbsoluteAndRatioPickDifferentUnits()
        {
            var names = Enum.GetNames(typeof(TargetRule));
            Assert.Contains("LowestHpAlly", names);
            Assert.Contains("LowestHpEnemies", names);
            Assert.Contains("LowestHpRatioAlly", names);
            Assert.Contains("LowestHpRatioEnemies", names);

            // slot0：绝对 400 / 比例 10%。slot2：绝对 200 / 比例 20%。
            var absAllies = NewSim(0, 11);
            OverlayTap(absAllies, "C003_tap", TargetRule.LowestHpAlly);
            SetHp(absAllies.Allies[0], 4000, 400);
            SetHp(absAllies.Allies[1], 800, 800);
            SetHp(absAllies.Allies[2], 1000, 200);
            ClearObserve(absAllies);
            absAllies.Allies[1].Charge = 100f;
            Assert.True(absAllies.TryTap(1));
            Assert.Equal(new[] { 2 }, PickedSlots(absAllies, ally: true));

            var ratioAllies = NewSim(0, 11);
            OverlayTap(ratioAllies, "C003_tap", TargetRule.LowestHpRatioAlly);
            SetHp(ratioAllies.Allies[0], 4000, 400);
            SetHp(ratioAllies.Allies[1], 800, 800);
            SetHp(ratioAllies.Allies[2], 1000, 200);
            ClearObserve(ratioAllies);
            ratioAllies.Allies[1].Charge = 100f;
            Assert.True(ratioAllies.TryTap(1));
            Assert.Equal(new[] { 0 }, PickedSlots(ratioAllies, ally: true));

            var absFoes = NewSim(0, 13);
            OverlayTap(absFoes, "C001_tap", TargetRule.LowestHpEnemies);
            Assert.True(absFoes.Enemies.Count >= 3);
            SetHp(absFoes.Enemies[0], 4000, 400);
            SetHp(absFoes.Enemies[1], 1000, 200);
            SetHp(absFoes.Enemies[2], 5000, 5000);
            ClearObserve(absFoes);
            absFoes.Allies[0].Charge = 100f;
            Assert.True(absFoes.TryTap(0));
            Assert.Equal(new[] { 1 }, PickedSlots(absFoes, ally: false));

            var ratioFoes = NewSim(0, 13);
            OverlayTap(ratioFoes, "C001_tap", TargetRule.LowestHpRatioEnemies);
            Assert.True(ratioFoes.Enemies.Count >= 3);
            SetHp(ratioFoes.Enemies[0], 4000, 400);
            SetHp(ratioFoes.Enemies[1], 1000, 200);
            SetHp(ratioFoes.Enemies[2], 5000, 5000);
            ClearObserve(ratioFoes);
            ratioFoes.Allies[0].Charge = 100f;
            Assert.True(ratioFoes.TryTap(0));
            Assert.Equal(new[] { 0 }, PickedSlots(ratioFoes, ally: false));
        }

        [Fact]
        public void EqualRankPicksAreRepeatableOnSameSeed()
        {
            var a = LowestHpAllyTie(17);
            var b = LowestHpAllyTie(17);
            Assert.Equal(a, b);
            Assert.Equal(a, LowestHpAllyTie(17));
            Assert.True(a == 0 || a == 2);

            var ratioA = LowestHpRatioAllyTie(17);
            var ratioB = LowestHpRatioAllyTie(17);
            Assert.Equal(ratioA, ratioB);
            Assert.Equal(ratioA, LowestHpRatioAllyTie(17));
            Assert.True(ratioA == 0 || ratioA == 2);

            var atkA = HighestAtkTie(21);
            var atkB = HighestAtkTie(21);
            Assert.Equal(atkA, atkB);
            Assert.Equal(atkA, HighestAtkTie(21));
            Assert.True(atkA == 0 || atkA == 1);
        }

        static int LowestHpAllyTie(int seed)
        {
            var sim = NewSim(0, seed);
            OverlayTap(sim, "C003_tap", TargetRule.LowestHpAlly);
            // 绝对 HP 平局、比例不同。
            SetHp(sim.Allies[0], 500, 100);
            SetHp(sim.Allies[1], 800, 800);
            SetHp(sim.Allies[2], 4000, 100);
            ClearObserve(sim);
            sim.Allies[1].Charge = 100f;
            Assert.True(sim.TryTap(1));
            var slots = PickedSlots(sim, ally: true);
            Assert.Single(slots);
            return slots[0];
        }

        static int LowestHpRatioAllyTie(int seed)
        {
            var sim = NewSim(0, seed);
            OverlayTap(sim, "C003_tap", TargetRule.LowestHpRatioAlly);
            // 比例平局（20%）、绝对 HP 不同。
            SetHp(sim.Allies[0], 500, 100);
            SetHp(sim.Allies[1], 800, 800);
            SetHp(sim.Allies[2], 4000, 800);
            ClearObserve(sim);
            sim.Allies[1].Charge = 100f;
            Assert.True(sim.TryTap(1));
            var slots = PickedSlots(sim, ally: true);
            Assert.Single(slots);
            return slots[0];
        }

        static int HighestAtkTie(int seed)
        {
            var sim = NewSim(0, seed);
            OverlayTap(sim, "C001_tap", TargetRule.HighestAtkEnemies);
            Assert.True(sim.Enemies.Count >= 3);
            sim.Enemies[0].Def.Atk = 900;
            sim.Enemies[1].Def.Atk = 900;
            sim.Enemies[2].Def.Atk = 100;
            ClearObserve(sim);
            sim.Allies[0].Charge = 100f;
            Assert.True(sim.TryTap(0));
            var slots = PickedSlots(sim, ally: false);
            Assert.Single(slots);
            return slots[0];
        }

        static BattleSim NewSim(int leaderSlot, int seed)
        {
            Catalog.BuildBuiltin();
            var stage = new StageDef
            {
                Id = "m1-target-fixture",
                Name = "m1-target-fixture",
                TimeLimitSec = 180f,
                Wave0 = new[] { "E001", "E002", "E003" },
                Wave1 = new string[0],
                EnemyHpMul = 1f,
                EnemyAtkMul = 1f,
                EnemyDefMul = 1f
            };
            return new BattleSim(Party, leaderSlot, seed, stage, null)
            {
                Deterministic = true,
                Speed = 1,
                Auto = AutoMode.Manual
            };
        }

        static void OverlayTap(BattleSim sim, string skillId, TargetRule rule)
        {
            var clone = Catalog.CloneSkill(Catalog.TrySkill(skillId));
            clone.Target = rule;
            clone.TargetCount = 1;
            if (clone.HealCoef <= 0f && clone.FlatHeal <= 0 && clone.HealMaxHpFrac <= 0f)
                clone.HitCount = 1;
            sim.OverlaySkill(skillId, clone);
        }

        static void SetHp(UnitState u, int maxHp, int hp)
        {
            u.MaxHp = maxHp;
            u.Hp = hp;
        }

        static void ClearObserve(BattleSim sim)
        {
            sim.Log.Clear();
            sim.Events.Events.Clear();
        }

        static List<int> PickedSlots(BattleSim sim, bool ally)
        {
            var slots = new List<int>();
            for (int i = 0; i < sim.Events.Events.Count; i++)
            {
                var e = sim.Events.Events[i];
                if (e.TargetSlot < 0) continue;
                if (e.TargetAlly != ally) continue;
                if (e.Kind != "unresolved" && e.Kind != "hit") continue;
                slots.Add(e.TargetSlot);
            }
            Assert.NotEmpty(slots);
            return slots;
        }
    }
}
