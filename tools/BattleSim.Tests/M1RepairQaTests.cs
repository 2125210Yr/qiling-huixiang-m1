using System;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class M1RepairQaTests
    {
        [Fact]
        public void GlUnknownCastDoesNotApplyJpHpAsSettled()
        {
            Assert.Equal("NOT_MEASURED", DamageMath.NotMeasuredCode);
            Assert.Equal("DESIGN_PLACEHOLDER", DamageMath.DesignPlaceholderCode);
            var unresolved = DamageMath.Resolve(
                FormulaProfile.GL_UNKNOWN, SkillType.Tap, 1000, 1f, 707, 2500,
                Element.Fire, Element.Wood, false, 1f, 1f);
            Assert.False(unresolved.Measured);
            Assert.Equal(DamageMath.NotMeasuredCode, unresolved.Code);

            var sim = new BattleSim(Catalog.DefaultParty, 0, 3) { Deterministic = true, Speed = 1 };
            Assert.Equal(FormulaProfile.GL_UNKNOWN, sim.Profile);
            var enemy = sim.Enemies[0];
            var hp = enemy.Hp;
            sim.Allies[0].Charge = 100f;
            Assert.True(sim.TryTap(0));
            Assert.Equal(hp, enemy.Hp);
            Assert.Contains(sim.Events.Events, e =>
                e.Kind == "unresolved"
                && e.FormulaStatus == FormulaStatus.NotMeasured
                && e.Profile == FormulaProfile.GL_UNKNOWN);
            Assert.DoesNotContain(Enum.GetNames(typeof(FormulaProfile)), n => n.Contains("FINAL_VERIFIED"));
        }

        [Fact]
        public void EmptyOrUnknownOpcodeCastFails()
        {
            Catalog.BuildBuiltin();
            var proto = Catalog.TrySkill("C001_tap");
            Assert.False(string.IsNullOrEmpty(proto.Opcode));
            var shared = proto.Opcode;

            var empty = Catalog.CloneSkill(proto);
            empty.Opcode = "";
            var sim = new BattleSim(new[] { "C001" }, 0, 1) { Deterministic = true };
            sim.OverlaySkill("C001_tap", empty);
            sim.Allies[0].Charge = 100f;
            Assert.Throws<UnknownOpcodeException>(() => sim.TryTap(0));
            Assert.Equal(BattleOutcome.Failed, sim.Outcome);
            Assert.Contains("UNKNOWN_OPCODE", sim.FailedReason);
            Assert.Equal(shared, Catalog.TrySkill("C001_tap").Opcode);

            var unknown = Catalog.CloneSkill(proto);
            unknown.Opcode = "not.implemented";
            var sim2 = new BattleSim(new[] { "C001" }, 0, 2) { Deterministic = true };
            sim2.OverlaySkill("C001_tap", unknown);
            sim2.Allies[0].Charge = 100f;
            Assert.Throws<UnknownOpcodeException>(() => sim2.TryTap(0));
            Assert.Equal(BattleOutcome.Failed, sim2.Outcome);
            Assert.Equal(shared, Catalog.TrySkill("C001_tap").Opcode);
        }

        [Fact]
        public void EmptyOrUnknownOpcodeApplyEffectFails()
        {
            var sim = new BattleSim(new[] { "C001" }, 0, 1) { Deterministic = true };
            var empty = new EffectDef
            {
                Id = "no_op",
                Kind = EffectKind.Stun,
                Magnitude = 1f,
                DurationSec = 3f,
                Group = "stun"
            };
            Assert.Throws<UnknownOpcodeException>(() => sim.ApplyStatus(sim.Allies[0], empty));
            Assert.Equal(BattleOutcome.Failed, sim.Outcome);
            Assert.Contains("UNKNOWN_OPCODE", sim.FailedReason);
            Assert.False(sim.Allies[0].Has(EffectKind.Stun));

            var sim2 = new BattleSim(new[] { "C001" }, 0, 2) { Deterministic = true };
            var unknown = new EffectDef
            {
                Id = "bad_fx",
                Opcode = "made.up.fx",
                Kind = EffectKind.AtkBuff,
                Magnitude = 0.2f,
                DurationSec = 8f,
                Group = "atk"
            };
            Assert.Throws<UnknownOpcodeException>(() => sim2.ApplyStatus(sim2.Allies[0], unknown));
            Assert.Equal(BattleOutcome.Failed, sim2.Outcome);
            Assert.DoesNotContain(sim2.Allies[0].Status, s => s != null && s.Def != null && s.Def.Id == "bad_fx");
        }

        [Fact]
        public void GlUnknownPoisonDoesNotApplyMaxHpAsSettled()
        {
            Assert.Equal("DESIGN_PLACEHOLDER", DamageMath.DesignPlaceholderCode);
            Assert.Equal("NOT_MEASURED", DamageMath.NotMeasuredCode);
            var poison = new EffectDef
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
            var sim = new BattleSim(new[] { "C001" }, 0, 3) { Deterministic = true };
            Assert.Equal(FormulaProfile.GL_UNKNOWN, sim.Profile);
            var actor = sim.Allies[0];
            sim.ApplyStatus(actor, poison);
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
        }

        [Fact]
        public void ExtraDmgDoesNotFoldAutoOrFeverIntoTapTsAmp()
        {
            const float ts = 0.30f;
            Assert.Equal(ts, DamageMath.ExtraDmg(SkillType.Tap, false, ts, 0.4f, 0.5f, 0f, 0f));
            Assert.Equal(0f, DamageMath.ExtraDmg(SkillType.Auto, false, ts, 0.4f, 0.5f, 0f, 0f));
            Assert.Equal(0f, DamageMath.ExtraDmg(SkillType.Fever, false, ts, 0.4f, 0.5f, 0f, 0f));
            Assert.Equal(0.4f, DamageMath.ExtraDmg(SkillType.Slide, false, ts, 0.4f, 0.5f, 0f, 0f));
            Assert.Equal(EffectOpcodes.DmgFeverParts, DamageMath.ChannelOpcode(SkillType.Fever));
            Assert.NotEqual(DamageMath.ChannelOpcode(SkillType.Tap), DamageMath.ChannelOpcode(SkillType.Fever));
            Assert.NotEqual(DamageMath.ChannelOpcode(SkillType.Tap), DamageMath.ChannelOpcode(SkillType.Auto));
        }

        [Fact]
        public void FeverChannelIsNotTap()
        {
            var sim = JpSim();
            sim.FeverActive = true;
            sim.FeverEver = true;
            sim.FeverLeft = 7f;
            sim.FeverHitsLeft = 70;
            var before = sim.Enemies[0].Hp;
            for (int i = 0; i < BattleSim.TickHz && sim.FeverHitsLeft > 60; i++)
                sim.TickFeverOnly();
            Assert.True(sim.Enemies[0].Hp < before);
            Assert.Contains(sim.Events.Events, e => e.Channel == SkillType.Fever && e.Opcode == EffectOpcodes.DmgFeverParts);
            Assert.DoesNotContain(sim.Events.Events, e =>
                (e.Kind == "hit" || e.Kind == "unresolved")
                && e.Opcode == EffectOpcodes.DmgFeverParts
                && e.Channel == SkillType.Tap);
        }

        [Fact]
        public void KnownOpcodeSettlesShieldStunAndTaunt()
        {
            var sim = JpSim();
            var u = sim.Allies[0];
            Assert.Equal(0, u.Shield);
            var shield = new EffectDef
            {
                Id = "shield_fx",
                Opcode = EffectOpcodes.ShieldApply,
                Kind = EffectKind.Shield,
                Magnitude = 0.22f,
                DurationSec = 8f,
                MaxStack = 1,
                SourceTier = 1,
                Group = "shield"
            };
            sim.ApplyStatus(u, shield);
            Assert.True(u.Shield > 0);
            Assert.True(u.Has(EffectKind.Shield));

            var stun = new EffectDef
            {
                Id = "stun_fx",
                Opcode = EffectOpcodes.ControlApply,
                Kind = EffectKind.Stun,
                Magnitude = 1f,
                DurationSec = 3f,
                Group = "stun"
            };
            u.Charge = 80f;
            u.SlideCd = 4f;
            sim.ApplyStatus(u, stun);
            Assert.True(u.Has(EffectKind.Stun));
            Assert.True(u.ActionLocked);
            Assert.Equal(0f, u.Charge);
            var cd = u.SlideCd;
            for (int i = 0; i < BattleSim.TickHz; i++)
                sim.Tick();
            Assert.Equal(0f, u.Charge);
            Assert.True(u.SlideCd < cd);

            var freeze = new EffectDef
            {
                Id = "freeze_fx",
                Opcode = EffectOpcodes.ControlApply,
                Kind = EffectKind.Freeze,
                Magnitude = 1f,
                DurationSec = 2f,
                Group = "freeze"
            };
            var foe = sim.Enemies[0];
            sim.ApplyStatus(foe, freeze);
            Assert.True(foe.Has(EffectKind.Freeze));
            Assert.True(foe.ActionLocked);

            var taunt = new EffectDef
            {
                Id = "taunt_fx",
                Opcode = EffectOpcodes.ControlApply,
                Kind = EffectKind.Taunt,
                Magnitude = 1f,
                DurationSec = 6f,
                Group = "taunt"
            };
            sim.ApplyStatus(u, taunt);
            Assert.True(u.Has(EffectKind.Taunt));

            var bleed = new EffectDef
            {
                Id = "bleed_fx",
                Opcode = EffectOpcodes.PoisonApply,
                Kind = EffectKind.Bleed,
                Magnitude = 0.05f,
                DurationSec = 8f,
                Group = "bleed"
            };
            sim.ApplyStatus(u, bleed);
            Assert.True(u.Has(EffectKind.Bleed));
            Assert.Equal(EffectOpcodes.PoisonApply, EffectOpcodes.ForKind(EffectKind.Bleed));
        }

        [Fact]
        public void UnimplementedOpcodeStillFails()
        {
            Assert.True(EffectOpcodes.IsKnown(EffectOpcodes.Revive));
            Assert.False(EffectOpcodes.IsImplemented(EffectOpcodes.Revive));
            Assert.True(EffectOpcodes.IsKnown(EffectOpcodes.DmgPierce));
            Assert.False(EffectOpcodes.IsImplemented(EffectOpcodes.DmgPierce));

            var sim = new BattleSim(new[] { "C001" }, 0, 1) { Deterministic = true };
            var hp = sim.Allies[0].Hp;
            Assert.Throws<UnknownOpcodeException>(() => sim.ExecuteOpcode(EffectOpcodes.Revive));
            Assert.Equal(BattleOutcome.Failed, sim.Outcome);
            Assert.Equal(hp, sim.Allies[0].Hp);
            Assert.Contains(sim.Events.Events, e => e.Kind == "fail");

            var sim2 = new BattleSim(new[] { "C001" }, 0, 2) { Deterministic = true };
            var before = sim2.Allies[0].Hp;
            var revive = new EffectDef
            {
                Id = "revive_fx",
                Opcode = EffectOpcodes.Revive,
                Kind = EffectKind.Heal,
                Magnitude = 1f,
                DurationSec = 1f,
                Group = "revive"
            };
            Assert.Throws<UnknownOpcodeException>(() => sim2.ApplyStatus(sim2.Allies[0], revive));
            Assert.Equal(BattleOutcome.Failed, sim2.Outcome);
            Assert.Equal(before, sim2.Allies[0].Hp);
            Assert.DoesNotContain(sim2.Allies[0].Status, s => s != null && s.Def != null && s.Def.Id == "revive_fx");

            var sim3 = new BattleSim(new[] { "C001" }, 0, 3) { Deterministic = true };
            Assert.Throws<UnknownOpcodeException>(() => sim3.ExecuteOpcode(EffectOpcodes.Retarget));
            Assert.Equal(BattleOutcome.Failed, sim3.Outcome);
        }

        [Fact]
        public void FeverDoesNotReadTapAtkCoefOrFlatPower()
        {
            Catalog.BuildBuiltin();
            var proto = Catalog.TrySkill("C001_tap");
            var sharedCoef = proto.AtkCoef;
            var sharedFlat = proto.FlatPower;

            var inflated = Catalog.CloneSkill(proto);
            inflated.AtkCoef = sharedCoef * 50f;
            inflated.FlatPower = sharedFlat * 50 + 99999;

            var baseline = JpSim();
            ArmFever(baseline);
            var baseHp = baseline.Enemies[0].Hp;
            RunFeverHits(baseline);
            var baseLost = baseHp - baseline.Enemies[0].Hp;
            Assert.True(baseLost > 0);

            var over = JpSim();
            over.OverlaySkill("C001_tap", inflated);
            ArmFever(over);
            var overHp = over.Enemies[0].Hp;
            RunFeverHits(over);
            Assert.Equal(baseLost, overHp - over.Enemies[0].Hp);
            Assert.Contains(over.Events.Events, e => e.Channel == SkillType.Fever && e.Opcode == EffectOpcodes.DmgFeverParts);
            Assert.DoesNotContain(over.Events.Events, e =>
                e.Opcode == EffectOpcodes.DmgTap && e.Channel == SkillType.Fever);

            var kr = new BattleSim(Catalog.DefaultParty, 0, 3)
            {
                Deterministic = true,
                Speed = 1,
                Profile = FormulaProfile.KR_LEGACY_REPORTED
            };
            kr.OverlaySkill("C001_tap", inflated);
            ArmFever(kr);
            var krHp = kr.Enemies[0].Hp;
            RunFeverHits(kr);
            Assert.True(kr.Enemies[0].Hp < krHp);

            var gl = new BattleSim(Catalog.DefaultParty, 0, 3) { Deterministic = true, Speed = 1 };
            Assert.Equal(FormulaProfile.GL_UNKNOWN, gl.Profile);
            ArmFever(gl);
            var glHp = gl.Enemies[0].Hp;
            RunFeverHits(gl);
            Assert.Equal(glHp, gl.Enemies[0].Hp);
            Assert.Contains(gl.Events.Events, e =>
                e.Kind == "unresolved"
                && e.Opcode == EffectOpcodes.DmgFeverParts
                && e.FormulaStatus == FormulaStatus.NotMeasured);

            Assert.Equal(sharedCoef, Catalog.TrySkill("C001_tap").AtkCoef);
            Assert.Equal(sharedFlat, Catalog.TrySkill("C001_tap").FlatPower);
        }

        [Fact]
        public void SlideDoesNotUseUniformVarianceRng()
        {
            var lo = DamageMath.ComputeSkill(SkillType.Slide, 1000, 1f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 0.95f);
            var mid = DamageMath.ComputeSkill(SkillType.Slide, 1000, 1f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 1f);
            var hi = DamageMath.ComputeSkill(SkillType.Slide, 1000, 1f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 1.05f);
            Assert.Equal(mid, lo);
            Assert.Equal(mid, hi);

            var sim = new BattleSim(Catalog.DefaultParty, 0, 99)
            {
                Deterministic = false,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
            };
            var hp = 0;
            for (int i = 0; i < sim.Enemies.Count; i++)
                if (sim.Enemies[i] != null) hp += sim.Enemies[i].Hp;
            sim.Allies[0].Charge = 100f;
            Assert.True(sim.TrySlide(0));
            var after = 0;
            for (int i = 0; i < sim.Enemies.Count; i++)
                if (sim.Enemies[i] != null) after += sim.Enemies[i].Hp;
            Assert.True(hp > after);
            var viaRange = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Slide, 1000, 1f, 707, 2500,
                Element.Fire, Element.Wood, false, 1f, 0.95f);
            var viaOne = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Slide, 1000, 1f, 707, 2500,
                Element.Fire, Element.Wood, false, 1f, 1.05f);
            Assert.True(viaRange.Measured && viaOne.Measured);
            Assert.Equal(viaOne.RequireInt(), viaRange.RequireInt());
        }

        [Fact]
        public void SelfTargetsCasterNotAliveZero()
        {
            Catalog.BuildBuiltin();
            var proto = Catalog.TrySkill("C003_tap");
            var oldTarget = proto.Target;
            var clone = Catalog.CloneSkill(proto);
            clone.Target = TargetRule.Self;
            var sim = new BattleSim(new[] { "C001", "C003" }, 1, 3)
            {
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
            };
            sim.OverlaySkill("C003_tap", clone);
            var first = sim.Allies[0];
            var caster = sim.Allies[1];
            first.Hp = 1;
            caster.Hp = 40;
            var firstHp = first.Hp;
            var casterHp = caster.Hp;
            caster.Charge = 100f;
            Assert.True(sim.TryTap(1));
            Assert.Equal(firstHp, first.Hp);
            Assert.True(caster.Hp > casterHp);
            Assert.Equal(oldTarget, Catalog.TrySkill("C003_tap").Target);
        }

        [Fact]
        public void PartyCapSaveAndStatsFollowPartyLength()
        {
            Catalog.BuildBuiltin();
            var seven = new[] { "C001", "C007", "C010", "C003", "C005", "C002", "C006" };
            var sim = new BattleSim(seven, 6, 1)
            {
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
            };
            Assert.Equal(7, sim.Allies.Length);
            Assert.Equal(7, sim.Stats.PartyCap);
            Assert.Equal(7, sim.Stats.AllyDealt.Length);
            sim.Allies[6].Charge = 100f;
            Assert.True(sim.TryTap(6));
            Assert.True(sim.Stats.AllyDealt[6] > 0);

            var blob = new SaveBlob { PartyIds = (string[])seven.Clone(), LeaderSlot = 6 };
            Assert.Equal(7, blob.PartyLength);
            Assert.Equal(6, blob.ClampLeaderSlot(6));
            Assert.Equal("C006", blob.LeaderId());
            var prog = blob.ProgressForParty();
            Assert.Equal(7, prog.Length);
            var parsed = SaveStore.Parse(SaveStore.Serialize(blob));
            Assert.Equal(7, parsed.PartyIds.Length);
            Assert.Equal(6, parsed.LeaderSlot);
            Assert.Equal("C006", parsed.PartyIds[6]);
            Assert.Equal(7, blob.LivePartyCap);
            Assert.Equal(FightStats.DefaultPartyCap, FightStats.CapForLength(0));
            Assert.Equal(7, FightStats.CapForLength(7));
            Assert.Equal(FightStats.DefaultPartyCap, new SaveBlob { PartyIds = new string[0] }.LivePartyCap);
        }

        static BattleSim JpSim()
        {
            return new BattleSim(Catalog.DefaultParty, 0, 3)
            {
                Deterministic = true,
                Speed = 1,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
            };
        }

        static void ArmFever(BattleSim sim)
        {
            sim.FeverActive = true;
            sim.FeverEver = true;
            sim.FeverLeft = 7f;
            sim.FeverHitsLeft = 70;
        }

        static void RunFeverHits(BattleSim sim)
        {
            for (int i = 0; i < BattleSim.TickHz && sim.FeverHitsLeft > 60; i++)
                sim.TickFeverOnly();
        }
    }
}
