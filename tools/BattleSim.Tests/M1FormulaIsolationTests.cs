using System;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// M1-G2-MATH-T04: T15 通道隔离与剖面门闩的内部一致性夹具。
    /// JP/KR 只证明对照可出数，不是还原。
    /// </summary>
    public sealed class M1FormulaIsolationTests
    {
        const int Atk = 1000;
        const float Coef = 1f;
        const int Flat = 707;
        const int Def = 2500;

        [Fact]
        public void T15ChannelsStayIsolated()
        {
            var tap = DamageMath.ComputeSkill(SkillType.Tap, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f);
            var slide = DamageMath.ComputeSkill(SkillType.Slide, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f);
            var auto = DamageMath.ComputeSkill(SkillType.Auto, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f);
            var drive = DamageMath.ComputeSkill(SkillType.Drive, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f);
            var fever = DamageMath.ComputeSkill(SkillType.Fever, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f, DamageMath.FeverMul);
            var skillDmg = DamageMath.SkillDmg(Atk, Coef, Flat);

            Assert.Equal(DamageMath.ComputeTs(0, skillDmg, Def, 1.4f, 1f, 1f, 0, 0, false, 0), tap);
            Assert.Equal(DamageMath.ComputeSs(0, skillDmg, Def, 1.4f, 1f, 0, 0, false, 0), slide);
            Assert.Equal(DamageMath.ComputeAuto(0, Atk, skillDmg, Def, 1.4f, 1f, 0, 0, false, 0), auto);
            Assert.Equal(DamageMath.ComputeDs(0, skillDmg, Def, 1.4f, 1f, 0, 0, false, 0), drive);
            Assert.Equal(DamageMath.ComputeFever(0, skillDmg, Def, 1.4f, 1f, 0, 0, false, 0, DamageMath.FeverMul), fever);

            Assert.NotEqual(tap, slide);
            Assert.NotEqual(tap, auto);
            Assert.NotEqual(tap, drive);
            Assert.NotEqual(tap, fever);
            Assert.NotEqual(slide, auto);
            Assert.NotEqual(slide, drive);
            Assert.NotEqual(auto, drive);

            Assert.Equal(tap, DamageMath.Compute(Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f));
            Assert.NotEqual(auto, DamageMath.Compute(Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f));

            var tapPct = DamageMath.ComputeSkill(SkillType.Tap, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f, 1f, 1f);
            var autoPct = DamageMath.ComputeSkill(SkillType.Auto, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f, 1f, 1f);
            var feverPct = DamageMath.ComputeSkill(SkillType.Fever, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f, DamageMath.FeverMul, 1f);
            var drivePct = DamageMath.ComputeSkill(SkillType.Drive, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1f, 1f, 1f);
            Assert.NotEqual(tap, tapPct);
            Assert.Equal(auto, autoPct);
            Assert.Equal(fever, feverPct);
            Assert.Equal(drive, drivePct);

            const float ts = 0.30f;
            Assert.Equal(ts, DamageMath.ExtraDmg(SkillType.Tap, false, ts, 0.4f, 0.5f, 0f, 0f));
            Assert.Equal(0f, DamageMath.ExtraDmg(SkillType.Auto, false, ts, 0.4f, 0.5f, 0f, 0f));
            Assert.Equal(0f, DamageMath.ExtraDmg(SkillType.Fever, false, ts, 0.4f, 0.5f, 0f, 0f));
            Assert.Equal(0.4f, DamageMath.ExtraDmg(SkillType.Slide, false, ts, 0.4f, 0.5f, 0f, 0f));
            Assert.Equal(0.5f, DamageMath.ExtraDmg(SkillType.Drive, false, ts, 0.4f, 0.5f, 0f, 0f));

            var slideLo = DamageMath.ComputeSkill(SkillType.Slide, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 0.95f, 0.6f);
            var slideHi = DamageMath.ComputeSkill(SkillType.Slide, Atk, Coef, Flat, Def, Element.Fire, Element.Wood, false, 1f, 1.05f, 0.6f);
            Assert.Equal(slide, slideLo);
            Assert.Equal(slide, slideHi);

            Assert.Equal(EffectOpcodes.DmgTap, DamageMath.ChannelOpcode(SkillType.Tap));
            Assert.Equal(EffectOpcodes.DmgSlide, DamageMath.ChannelOpcode(SkillType.Slide));
            Assert.Equal(EffectOpcodes.DmgAuto, DamageMath.ChannelOpcode(SkillType.Auto));
            Assert.Equal(EffectOpcodes.DmgDriveActual, DamageMath.ChannelOpcode(SkillType.Drive));
            Assert.Equal(EffectOpcodes.DmgFeverParts, DamageMath.ChannelOpcode(SkillType.Fever));
            Assert.Equal(5, DistinctCount(
                DamageMath.ChannelOpcode(SkillType.Tap),
                DamageMath.ChannelOpcode(SkillType.Slide),
                DamageMath.ChannelOpcode(SkillType.Auto),
                DamageMath.ChannelOpcode(SkillType.Drive),
                DamageMath.ChannelOpcode(SkillType.Fever)));
        }

        [Fact]
        public void FeverDoesNotReadTapCoefficients()
        {
            Catalog.BuildBuiltin();
            var proto = Catalog.TrySkill("C001_tap");
            var sharedCoef = proto.AtkCoef;
            var sharedFlat = proto.FlatPower;
            Assert.NotEqual(sharedCoef, DamageMath.FeverChannelAtkCoef);
            Assert.NotEqual(sharedFlat, DamageMath.FeverChannelFlat);

            var viaFeverChannel = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Fever, Atk,
                DamageMath.FeverChannelAtkCoef, DamageMath.FeverChannelFlat, Def,
                Element.Fire, Element.Wood, false, 1f, 1f, DamageMath.FeverMul);
            var viaTapCoef = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Fever, Atk,
                sharedCoef, sharedFlat, Def,
                Element.Fire, Element.Wood, false, 1f, 1f, DamageMath.FeverMul);
            Assert.True(viaFeverChannel.Computed);
            Assert.True(viaTapCoef.Computed);
            Assert.NotEqual(viaTapCoef.RequireInt(), viaFeverChannel.RequireInt());

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
            Assert.DoesNotContain(over.Events.Events, e =>
                (e.Kind == "hit" || e.Kind == "unresolved")
                && e.Opcode == EffectOpcodes.DmgFeverParts
                && e.Channel == SkillType.Tap);

            Assert.Equal(sharedCoef, Catalog.TrySkill("C001_tap").AtkCoef);
            Assert.Equal(sharedFlat, Catalog.TrySkill("C001_tap").FlatPower);
        }

        [Fact]
        public void GlUnknownDoesNotSettle()
        {
            Assert.Equal("NOT_MEASURED", DamageMath.NotMeasuredCode);
            Assert.DoesNotContain(Enum.GetNames(typeof(FormulaProfile)), n => n.Contains("FINAL_VERIFIED"));

            foreach (var type in new[] { SkillType.Tap, SkillType.Slide, SkillType.Auto, SkillType.Drive, SkillType.Fever })
            {
                var unresolved = DamageMath.Resolve(
                    FormulaProfile.GL_UNKNOWN, type, Atk, Coef, Flat, Def,
                    Element.Fire, Element.Wood, false, 1f, 1f);
                Assert.False(unresolved.Measured);
                Assert.Equal(FormulaStatus.NotMeasured, unresolved.Status);
                Assert.Equal(DamageMath.NotMeasuredCode, unresolved.Code);
                Assert.Throws<InvalidOperationException>(() => unresolved.RequireInt());
            }

            var sim = new BattleSim(Catalog.DefaultParty, 0, 3) { Deterministic = true, Speed = 1 };
            Assert.Equal(FormulaProfile.GL_UNKNOWN, sim.Profile);
            var enemy = sim.Enemies[0];
            var hp = enemy.Hp;
            sim.Allies[0].Charge = 100f;
            Assert.True(sim.TryTap(0));
            Assert.Equal(hp, enemy.Hp);
            Assert.Contains(sim.Events.Events, e =>
                e.Kind == "unresolved"
                && e.Opcode == EffectOpcodes.DmgTap
                && e.FormulaStatus == FormulaStatus.NotMeasured
                && e.Profile == FormulaProfile.GL_UNKNOWN
                && e.Channel == SkillType.Tap);

            ArmFever(sim);
            RunFeverHits(sim);
            Assert.Equal(hp, enemy.Hp);
            Assert.Contains(sim.Events.Events, e =>
                e.Kind == "unresolved"
                && e.Opcode == EffectOpcodes.DmgFeverParts
                && e.FormulaStatus == FormulaStatus.NotMeasured
                && e.Profile == FormulaProfile.GL_UNKNOWN
                && e.Channel == SkillType.Fever);
            Assert.DoesNotContain(sim.Events.Events, e =>
                e.Kind == "hit" && e.Amount > 0);
        }

        [Fact]
        public void JpContrastResolveYieldsNumbers()
        {
            var jpTap = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Tap, Atk, Coef, Flat, Def,
                Element.Fire, Element.Wood, false, 1f, 1f);
            var jpSlide = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Slide, Atk, Coef, Flat, Def,
                Element.Fire, Element.Wood, false, 1f, 1f);
            var jpAuto = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Auto, Atk, Coef, Flat, Def,
                Element.Fire, Element.Wood, false, 1f, 1f);
            var jpDrive = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Drive, Atk, Coef, Flat, Def,
                Element.Fire, Element.Wood, false, 1f, 1f);
            var jpFever = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Fever, Atk,
                DamageMath.FeverChannelAtkCoef, DamageMath.FeverChannelFlat, Def,
                Element.Fire, Element.Wood, false, 1f, 1f, DamageMath.FeverMul);
            var krTap = DamageMath.Resolve(
                FormulaProfile.KR_LEGACY_REPORTED, SkillType.Tap, Atk, Coef, Flat, Def,
                Element.Fire, Element.Wood, false, 1f, 1f);

            // G2 R06: JP/KR branches are computable contrast values, never Measured against GL.
            Assert.True(jpTap.Computed && jpSlide.Computed && jpAuto.Computed && jpDrive.Computed && jpFever.Computed);
            Assert.True(krTap.Computed);
            Assert.Equal(FormulaEvidence.HistoricalCandidate, jpTap.Evidence);
            Assert.Equal(FormulaEvidence.DesignPlaceholder, jpDrive.Evidence);
            Assert.NotEqual(FormulaEvidence.Measured, jpDrive.Evidence);
            Assert.True(jpTap.RequireInt() > 0);
            Assert.True(jpSlide.RequireInt() > 0);
            Assert.True(jpAuto.RequireInt() > 0);
            Assert.True(jpDrive.RequireInt() > 0);
            Assert.True(jpFever.RequireInt() > 0);
            Assert.NotEqual(jpTap.RequireInt(), jpSlide.RequireInt());
            Assert.NotEqual(jpTap.RequireInt(), jpAuto.RequireInt());
            Assert.True(krTap.RequireInt() < jpTap.RequireInt());

            var sim = JpSim();
            var hp = sim.Enemies[0].Hp;
            sim.Allies[0].Charge = 100f;
            Assert.True(sim.TryTap(0));
            Assert.True(sim.Enemies[0].Hp < hp);
            Assert.Contains(sim.Events.Events, e =>
                e.Kind == "hit"
                && e.Channel == SkillType.Tap
                && e.Opcode == EffectOpcodes.DmgTap
                && e.Amount > 0
                && e.Profile == FormulaProfile.JP_LEGACY_EMPIRICAL
                && e.FormulaStatus == FormulaStatus.Ok);
        }

        static int DistinctCount(params string[] ops)
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < ops.Length; i++)
                seen.Add(ops[i]);
            return seen.Count;
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

        // G2 R02: Fever hits no longer come from the timer in Manual mode; the fixture taps the
        // first alive ally through the same command path a player would use.
        static void RunFeverHits(BattleSim sim)
        {
            sim.Clocks.FeverMinHitIntervalSec = 0f;
            var slot = 0;
            for (int i = 0; i < sim.Allies.Length; i++)
                if (sim.Allies[i] != null && sim.Allies[i].Alive) { slot = i; break; }
            for (int i = 0; i < BattleSim.TickHz && sim.FeverHitsLeft > 60 && sim.FeverActive; i++)
            {
                sim.TickFeverOnly();
                sim.Submit(BattleCommand.FeverTap(slot));
            }
        }
    }
}
