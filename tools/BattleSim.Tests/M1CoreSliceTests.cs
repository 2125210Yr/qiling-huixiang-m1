using System;
using System.Collections.Generic;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class M1CoreSliceTests
    {
        [Fact]
        public void DefaultProfileIsGlUnknownAndForbidsFinalVerified()
        {
            var names = Enum.GetNames(typeof(FormulaProfile));
            Assert.Contains("GL_UNKNOWN", names);
            Assert.DoesNotContain("GL_FINAL_VERIFIED", names);
            Assert.DoesNotContain("JP_FINAL_VERIFIED", names);
            Assert.DoesNotContain("KR_FINAL_VERIFIED", names);
            var sim = NewSim();
            Assert.Equal(FormulaProfile.GL_UNKNOWN, sim.Profile);
            var unresolved = DamageMath.Resolve(
                FormulaProfile.GL_UNKNOWN, SkillType.Tap, 1000, 1f, 707, 2500,
                Element.Fire, Element.Wood, false, 1f, 1f);
            Assert.False(unresolved.Computed);
            Assert.False(unresolved.Measured);
            Assert.Equal(DamageMath.NotMeasuredCode, unresolved.Code);
        }

        [Fact]
        public void AutoChannelIsNotTap()
        {
            var tap = DamageMath.ComputeSkill(SkillType.Tap, 1000, 1f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 1f);
            var auto = DamageMath.ComputeSkill(SkillType.Auto, 1000, 1f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 1f);
            Assert.NotEqual(tap, auto);
            Assert.Equal(EffectOpcodes.DmgTap, DamageMath.ChannelOpcode(SkillType.Tap));
            Assert.Equal(EffectOpcodes.DmgAuto, DamageMath.ChannelOpcode(SkillType.Auto));
            var jpTap = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Tap, 1000, 1f, 707, 2500,
                Element.Fire, Element.Wood, false, 1f, 1f);
            var krTap = DamageMath.Resolve(
                FormulaProfile.KR_LEGACY_REPORTED, SkillType.Tap, 1000, 1f, 707, 2500,
                Element.Fire, Element.Wood, false, 1f, 1f);
            Assert.True(jpTap.Computed);
            Assert.True(krTap.Computed);
            Assert.False(jpTap.Measured);
            Assert.False(krTap.Measured);
            Assert.True(krTap.Value < jpTap.Value);
        }

        [Fact]
        public void SlideCdIsIndependentOfCharge()
        {
            var sim = NewSim();
            var u = sim.Allies[0];
            u.Charge = 100f;
            Assert.True(sim.TrySlide(0));
            Assert.Equal(0f, u.Charge);
            Assert.True(u.SlideCd > 0f);
            u.Charge = 100f;
            Assert.True(sim.CanAct(0));
            Assert.False(sim.CanSlide(0));
            Assert.True(sim.TryTap(0));
            Assert.Equal(0f, u.Charge);
            Assert.True(u.SlideCd > 0f);
        }

        [Fact]
        public void ControlPausesChargeButSlideCdContinues()
        {
            var sim = NewSim();
            var u = sim.Allies[0];
            u.SlideCd = 4f;
            sim.ApplyStatus(u, Catalog.TryEffect("stun"));
            u.Charge = 40f;
            Assert.True(u.ActionLocked);
            var charge = u.Charge;
            var cd = u.SlideCd;
            for (int i = 0; i < BattleSim.TickHz; i++)
                sim.Tick();
            Assert.Equal(charge, u.Charge);
            Assert.True(u.SlideCd < cd);
            Assert.True(u.SlideCd > 0f);
        }

        [Fact]
        public void UnknownOpcodeFailsAndIsLogged()
        {
            var sim = NewSim();
            Assert.Throws<UnknownOpcodeException>(() => sim.ExecuteOpcode("nope.unknown"));
            Assert.Equal(BattleOutcome.Failed, sim.Outcome);
            Assert.Contains("UNKNOWN_OPCODE", sim.FailedReason);
            Assert.Contains(sim.Events.Events, e => e.Kind == "fail");
            var fx = new EffectDef
            {
                Id = "bad",
                Opcode = "made.up.op",
                Kind = EffectKind.Poison,
                Magnitude = 0.05f,
                DurationSec = 8f,
                Group = "poison"
            };
            var sim2 = NewSim();
            Assert.Throws<UnknownOpcodeException>(() => sim2.ApplyStatus(sim2.Allies[0], fx));
            Assert.Equal(BattleOutcome.Failed, sim2.Outcome);
        }

        [Fact]
        public void PoisonTriggersOnActionAndHitTakenNotPerSecond()
        {
            var sim = NewSim();
            var enemy = sim.Enemies[0];
            var poison = new EffectDef
            {
                Id = "poison",
                Opcode = EffectOpcodes.PoisonApply,
                Kind = EffectKind.Poison,
                Magnitude = 0.05f,
                DurationSec = 12f,
                MaxStack = 1,
                SourceTier = 1,
                Group = "poison"
            };
            sim.Profile = FormulaProfile.JP_LEGACY_EMPIRICAL;
            sim.ApplyStatus(enemy, poison);
            var stun = Catalog.TryEffect("stun");
            for (int i = 0; i < sim.Allies.Length; i++)
                if (sim.Allies[i] != null) sim.ApplyStatus(sim.Allies[i], stun);
            for (int i = 0; i < sim.Enemies.Count; i++)
                sim.ApplyStatus(sim.Enemies[i], stun);
            var hpAfterApply = enemy.Hp;
            for (int i = 0; i < BattleSim.TickHz * 2; i++)
                sim.Tick();
            Assert.Equal(hpAfterApply, enemy.Hp);
            for (int i = 0; i < BattleSim.TickHz * 2 && sim.Allies[0].ActionLocked; i++)
                sim.Tick();

            var beforeHit = enemy.Hp;
            ChargeAll(sim);
            Assert.True(sim.TryTap(0));
            Assert.True(enemy.Hp < beforeHit);

            var actor = sim.Allies[0];
            sim.ApplyStatus(actor, poison);
            actor.Charge = 100f;
            var actorHp = actor.Hp;
            Assert.True(sim.TryTap(0));
            Assert.True(actor.Hp < actorHp);
        }

        [Fact]
        public void PartyCapacityIsNotHardcodedFive()
        {
            Catalog.BuildBuiltin();
            var three = new[] { "C001", "C007", "C010" };
            var seven = new[] { "C001", "C007", "C010", "C003", "C005", "C002", "C006" };
            var a = new BattleSim(three, 0, 1) { Deterministic = true };
            var b = new BattleSim(seven, 0, 1) { Deterministic = true };
            Assert.Equal(3, a.Allies.Length);
            Assert.Equal(7, b.Allies.Length);
            Assert.NotNull(a.Allies[0]);
            Assert.NotNull(b.Allies[6]);
            Assert.Equal(3, FightStats.CapForLength(3));
            Assert.Equal(7, FightStats.CapForLength(7));
            Assert.Equal(FightStats.DefaultPartyCap, FightStats.CapForLength(0));
            var states = new List<string>();
            BattleHudState.Collect(a, 0, states);
            Assert.NotEmpty(states);
        }

        [Fact]
        public void FixedSeedFivePersonSequenceFiresDriveAndFever()
        {
            Catalog.BuildBuiltin();
            var sim = new BattleSim(Catalog.DefaultParty, 0, 20260910, Catalog.Stages[0], null)
            {
                Deterministic = true,
                Auto = AutoMode.Manual,
                Speed = 1
            };
            Assert.Equal(5, sim.Allies.Length);
            Assert.False(SliceDriveSequence.HasDriveCast(sim));
            Assert.True(sim.Drive < 100f);
            Assert.True(SliceDriveSequence.PlayUntilFever(sim));
            Assert.True(SliceDriveSequence.HasDriveCast(sim));
            Assert.Contains(sim.Events.Events, e => e.Channel == SkillType.Drive && e.Kind == "cast");
            Assert.True(sim.FeverEver);
            Assert.True(sim.FeverActive);
        }

        [Fact]
        public void StayHeldKeepsManualBattleFrozen()
        {
            Catalog.BuildBuiltin();
            var sim = new BattleSim(Catalog.DefaultParty, 0, 20260910)
            {
                Deterministic = true,
                Auto = AutoMode.Manual,
                Speed = 1
            };
            var time = sim.TimeLeft;
            for (int i = 0; i < BattleSim.TickHz * 5; i++)
            {
                sim.StayHeld();
                sim.Tick();
            }
            Assert.Equal(time, sim.TimeLeft);
            Assert.True(sim.HoldSim);
            Assert.Equal(BattleOutcome.InProgress, sim.Outcome);
            Assert.False(SliceDriveSequence.HasDriveCast(sim));
        }

        [Fact]
        public void EventLogHashIsStableForSameSeed()
        {
            var a = NewSim();
            var b = NewSim();
            a.AutoTap = true;
            b.AutoTap = true;
            for (int i = 0; i < 90; i++)
            {
                a.Tick();
                b.Tick();
            }
            Assert.Equal(a.Events.ComputeHash(), b.Events.ComputeHash());
            Assert.True(a.Events.Events.Count > 0);
            Assert.All(a.Events.Events, e => Assert.Equal(FormulaProfile.GL_UNKNOWN, e.Profile));
        }

        static BattleSim NewSim()
        {
            return new BattleSim(Catalog.DefaultParty, 0, 3) { Deterministic = true, Speed = 1 };
        }

        static void ChargeAll(BattleSim sim)
        {
            for (int i = 0; i < sim.Allies.Length; i++)
            {
                if (sim.Allies[i] != null)
                    sim.Allies[i].Charge = 100f;
            }
        }
    }
}
