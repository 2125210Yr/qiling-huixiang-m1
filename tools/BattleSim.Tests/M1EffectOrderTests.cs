using System;
using System.Collections.Generic;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// M1-G2-EFFECT-TESTS: 效果门闩夹具。
    /// 盾吸收、pierce 仍失败、毒 on_action 剖面门闩、反射未实现失败。
    /// 不是 T11 还原。
    /// </summary>
    public sealed class M1EffectOrderTests
    {
        [Fact]
        public void ShieldAbsorbsIncomingDamage()
        {
            var sim = JpSim(1);
            var shield = new EffectDef
            {
                Id = "shield_fx",
                Opcode = EffectOpcodes.ShieldApply,
                Kind = EffectKind.Shield,
                Magnitude = 2f,
                DurationSec = 8f,
                MaxStack = 1,
                SourceTier = 1,
                Group = "shield"
            };
            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                var foe = sim.Enemies[i];
                if (foe == null || !foe.Alive) continue;
                sim.ApplyStatus(foe, shield);
                Assert.True(foe.Shield > 0);
                Assert.True(foe.Has(EffectKind.Shield));
            }

            var hpBefore = SnapshotHp(sim.Enemies);
            var shieldBefore = SnapshotShield(sim.Enemies);
            Assert.True(Sum(shieldBefore) > 0);

            sim.Allies[0].Charge = 100f;
            Assert.True(sim.TryTap(0));

            var hpAfter = SnapshotHp(sim.Enemies);
            var shieldAfter = SnapshotShield(sim.Enemies);
            Assert.Equal(hpBefore, hpAfter);
            Assert.True(Sum(shieldAfter) < Sum(shieldBefore));
            Assert.True(Sum(shieldAfter) >= 0);
        }

        [Fact]
        public void UnknownPierceStillFails()
        {
            Assert.True(EffectOpcodes.IsKnown(EffectOpcodes.DmgPierce));
            Assert.False(EffectOpcodes.IsImplemented(EffectOpcodes.DmgPierce));

            var sim = new BattleSim(new[] { "C001" }, 0, 2) { Deterministic = true };
            var hp = sim.Allies[0].Hp;
            Assert.Throws<UnknownOpcodeException>(() => sim.ExecuteOpcode(EffectOpcodes.DmgPierce));
            Assert.Equal(BattleOutcome.Failed, sim.Outcome);
            Assert.Contains("UNKNOWN_OPCODE", sim.FailedReason);
            Assert.Equal(hp, sim.Allies[0].Hp);
            Assert.Contains(sim.Events.Events, e => e.Kind == "fail" && e.Opcode == EffectOpcodes.DmgPierce);

            var sim2 = new BattleSim(new[] { "C001" }, 0, 3) { Deterministic = true };
            var before = sim2.Allies[0].Hp;
            var pierce = new EffectDef
            {
                Id = "pierce_fx",
                Opcode = EffectOpcodes.DmgPierce,
                Kind = EffectKind.Damage,
                Magnitude = 1f,
                DurationSec = 1f,
                Group = "pierce"
            };
            Assert.Throws<UnknownOpcodeException>(() => sim2.ApplyStatus(sim2.Allies[0], pierce));
            Assert.Equal(BattleOutcome.Failed, sim2.Outcome);
            Assert.Equal(before, sim2.Allies[0].Hp);
            Assert.DoesNotContain(sim2.Allies[0].Status, s => s != null && s.Def != null && s.Def.Id == "pierce_fx");
        }

        [Fact]
        public void PoisonOnActionChangesHpOnJpProfile()
        {
            var poison = PoisonFx();
            var sim = JpSim(4);
            var actor = sim.Allies[0];
            sim.ApplyStatus(actor, poison);
            Assert.True(actor.Has(EffectKind.Poison));
            var hp = actor.Hp;
            actor.Charge = 100f;
            Assert.True(sim.TryTap(0));
            Assert.True(actor.Hp < hp);
            Assert.Contains(sim.Events.Events, e =>
                e.Kind == "on_action" && e.Opcode == EffectOpcodes.PoisonApply);
        }

        [Fact]
        public void GlUnknownPoisonDoesNotChangeHp()
        {
            var poison = PoisonFx();
            var sim = new BattleSim(new[] { "C001" }, 0, 5) { Deterministic = true };
            Assert.Equal(FormulaProfile.GL_UNKNOWN, sim.Profile);
            var actor = sim.Allies[0];
            sim.ApplyStatus(actor, poison);
            Assert.True(actor.Has(EffectKind.Poison));
            var hp = actor.Hp;
            actor.Charge = 100f;
            Assert.True(sim.TryTap(0));
            Assert.Equal(hp, actor.Hp);
            Assert.Contains(sim.Events.Events, e =>
                e.Kind == "unresolved"
                && e.Opcode == EffectOpcodes.PoisonApply
                && e.FormulaStatus == FormulaStatus.NotMeasured
                && e.Profile == FormulaProfile.GL_UNKNOWN);
            Assert.DoesNotContain(sim.Events.Events, e =>
                e.Kind == "hit" && e.Opcode == EffectOpcodes.PoisonApply);
            Assert.DoesNotContain(sim.Events.Events, e =>
                e.Kind == "on_action" && e.Opcode == EffectOpcodes.PoisonApply);
        }

        [Fact]
        public void UnimplementedReflectFails()
        {
            const string reflectOp = "reflect";
            Assert.False(EffectOpcodes.IsKnown(reflectOp));
            Assert.False(EffectOpcodes.IsImplemented(reflectOp));
            Assert.Equal(EffectKind.Reflect, (EffectKind)13);

            var sim = new BattleSim(new[] { "C001" }, 0, 6) { Deterministic = true };
            var hp = sim.Allies[0].Hp;
            var fx = new EffectDef
            {
                Id = "reflect_fx",
                Opcode = reflectOp,
                Kind = EffectKind.Reflect,
                Magnitude = 0.3f,
                DurationSec = 8f,
                Group = "reflect"
            };
            Assert.Throws<UnknownOpcodeException>(() => sim.ApplyStatus(sim.Allies[0], fx));
            Assert.Equal(BattleOutcome.Failed, sim.Outcome);
            Assert.Contains("UNKNOWN_OPCODE", sim.FailedReason);
            Assert.False(sim.Allies[0].Has(EffectKind.Reflect));
            Assert.Equal(hp, sim.Allies[0].Hp);
            Assert.DoesNotContain(sim.Allies[0].Status, s => s != null && s.Def != null && s.Def.Id == "reflect_fx");

            var sim2 = new BattleSim(new[] { "C001" }, 0, 7) { Deterministic = true };
            Assert.Throws<UnknownOpcodeException>(() => sim2.ExecuteOpcode(reflectOp));
            Assert.Equal(BattleOutcome.Failed, sim2.Outcome);
            Assert.Contains(sim2.Events.Events, e => e.Kind == "fail" && e.Opcode == reflectOp);
        }

        static BattleSim JpSim(int seed)
        {
            return new BattleSim(new[] { "C001" }, 0, seed)
            {
                Deterministic = true,
                Speed = 1,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
            };
        }

        static EffectDef PoisonFx()
        {
            return new EffectDef
            {
                Id = "poison",
                Opcode = EffectOpcodes.PoisonApply,
                Kind = EffectKind.Poison,
                Magnitude = 0.20f,
                DurationSec = 12f,
                MaxStack = 1,
                SourceTier = 1,
                Group = "poison"
            };
        }

        static int[] SnapshotHp(IList<UnitState> units)
        {
            var snap = new int[units.Count];
            for (int i = 0; i < units.Count; i++)
                snap[i] = units[i] != null ? units[i].Hp : 0;
            return snap;
        }

        static int[] SnapshotShield(IList<UnitState> units)
        {
            var snap = new int[units.Count];
            for (int i = 0; i < units.Count; i++)
                snap[i] = units[i] != null ? units[i].Shield : 0;
            return snap;
        }

        static int Sum(int[] values)
        {
            var n = 0;
            for (int i = 0; i < values.Length; i++)
                n += values[i];
            return n;
        }
    }
}
