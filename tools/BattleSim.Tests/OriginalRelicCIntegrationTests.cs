using System;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// Controlled real-BattleSim integration fixtures, not normal play or replay evidence.
    /// Content changes happen on the isolated input before construction. ReadyTap explicitly
    /// supplies boundary charge so the tests isolate causal ordering without waiting whole fights.
    /// Every action still goes through the ordinary Submit validation and native settlement.
    /// </summary>
    public sealed class OriginalRelicCIntegrationTests
    {
        static readonly string[] Harmony = { "C01", "C03" };
        static readonly string[] Forte = { "C01", "C03", "C04" };

        [Fact]
        public void C03_RepeatedActorDoesNotCountTwice_ThirdDistinctActorResetsAndChargesLivingParty()
        {
            var sim = Create(Harmony);
            foreach (var ally in sim.Allies) ally.Charge = 0; // Explicit boundary fixture.

            ReadyTap(sim, 1);
            Assert.Equal(1 << 1, sim.ExpeditionRelics.DistinctActorBits);
            Assert.Equal(12f, sim.Allies[2].Charge);
            ReadyTap(sim, 1);
            Assert.Equal(1 << 1, sim.ExpeditionRelics.DistinctActorBits);
            Assert.Equal(24f, sim.Allies[2].Charge);
            ReadyTap(sim, 2);
            Assert.Equal((1 << 1) | (1 << 2), sim.ExpeditionRelics.DistinctActorBits);
            Assert.Equal(12f, sim.Allies[3].Charge);
            ReadyTap(sim, 4);

            Assert.Equal(0, sim.ExpeditionRelics.DistinctActorBits);
            // The third action first relays 12 to slot0, then harmony adds10 to every living member.
            Assert.Equal(new[] { 22f, 10f, 10f, 22f, 10f }, sim.Allies.Select(a => a.Charge).ToArray());
            Assert.False(sim.ExpeditionRelics.ForteStored);
        }

        [Fact]
        public void C04_DamageThatFormsHarmony_IsNotBoostedByTheForteItCreates()
        {
            var control = Create(Harmony);
            var strong = Create(Forte);
            foreach (var sim in new[] { control, strong })
            {
                ReadyTap(sim, 1);
                ReadyTap(sim, 2);
                Assert.False(sim.ExpeditionRelics.ForteStored);
            }

            var controlHit = DamageFromNextTap(control, 0).Single();
            var strongHit = DamageFromNextTap(strong, 0).Single();

            Assert.True(controlHit.RequestedDamage > 0);
            Assert.Equal(controlHit.RequestedDamage, strongHit.RequestedDamage);
            Assert.Equal(controlHit.EffectiveDamage, strongHit.EffectiveDamage);
            Assert.Equal(0, strong.ExpeditionRelics.DistinctActorBits);
            Assert.True(strong.ExpeditionRelics.ForteStored);
            Assert.False(control.ExpeditionRelics.ForteStored);
        }

        [Theory]
        [InlineData(1)] // shield
        [InlineData(2)] // heal
        public void C04_SupportSkillDoesNotConsumeForte_TheNextDamageActionDoes(int supportSlot)
        {
            var sim = Create(Forte);
            ReadyTap(sim, 1);
            ReadyTap(sim, 2);
            ReadyTap(sim, 4);
            Assert.True(sim.ExpeditionRelics.ForteStored);

            var before = sim.ExpeditionResolutions.Count;
            ReadyTap(sim, supportSlot);
            var support = sim.ExpeditionResolutions.Skip(before).Where(r => r.Origin == ResolutionOrigin.Native
                && r.SourceAlly && r.SourceSlot == supportSlot).ToArray();
            Assert.NotEmpty(support);
            Assert.All(support, r => Assert.Equal(0, r.RequestedDamage));
            if (supportSlot == 1) Assert.Contains(support, r => r.ShieldProduced > 0);
            else Assert.Contains(support, r => r.RequestedHeal > 0);
            Assert.True(sim.ExpeditionRelics.ForteStored);

            Assert.NotEmpty(DamageFromNextTap(sim, 3));
            // Only two distinct actors acted since the last harmony, so no new forte can be stored here.
            Assert.False(sim.ExpeditionRelics.ForteStored);
            Assert.Equal((1 << supportSlot) | (1 << 3), sim.ExpeditionRelics.DistinctActorBits);
        }

        [Fact]
        public void C04_AddsToExistingChannelBonus_AndAppliesToEveryHitOfOneNativeAction()
        {
            var input = Input(Forte);
            var guideEffect = input.Effects.Single(e => e.Id == "OE_TEAM_ATK");
            guideEffect.Kind = EffectKind.TsAmp;
            guideEffect.Magnitude = 0.5f;
            var bladeSkill = input.Skills.Single(s => s.Id == "OE_BLADE_tap");
            bladeSkill.HitCount = 3;
            // Frozen arithmetic fixture only. Other integration cases retain the real content's crit stats.
            input.Characters.Single(c => c.Id == "OE_BLADE").Crt = 0;
            var sim = RunBattleFactory.Create(input);
            Assert.True(sim.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture)).Accepted);

            ReadyTap(sim, 4); // Real native linked effect installs TsAmp +0.5.
            ReadyTap(sim, 1);
            ReadyTap(sim, 0); // Third distinct actor forms harmony after its damage.
            Assert.True(sim.ExpeditionRelics.ForteStored);
            Assert.Equal(0.5f, sim.Allies[3].Magnitude(EffectKind.TsAmp));

            var hits = DamageFromNextTap(sim, 3);
            var caster = sim.Allies[3];
            var target = sim.Enemies[0];
            var expected = DamageMath.Resolve(input.Profile, SkillType.Tap, caster.Atk,
                bladeSkill.AtkCoef, bladeSkill.FlatPower, target.DefenseAgainst(caster.Def.Element),
                caster.Def.Element, target.Def.Element, false, 1f + 0.5f + 1.2f, 1f).RequireInt();
            var incorrectMultiplicative = DamageMath.Resolve(input.Profile, SkillType.Tap, caster.Atk,
                bladeSkill.AtkCoef, bladeSkill.FlatPower, target.DefenseAgainst(caster.Def.Element),
                caster.Def.Element, target.Def.Element, false, (1f + 0.5f) * (1f + 1.2f), 1f).RequireInt();

            Assert.Equal(3, hits.Length);
            Assert.NotEqual(expected, incorrectMultiplicative);
            Assert.All(hits, result =>
            {
                Assert.Equal(expected, result.RequestedDamage);
                Assert.Equal(expected, result.EffectiveHpDamage);
                Assert.Equal(0, result.ProcDepth);
                Assert.Equal("", result.SourceRelicId);
                Assert.Equal(hits[0].RootActionId, result.RootActionId);
            });
            Assert.False(sim.ExpeditionRelics.ForteStored);
        }

        [Fact]
        public void RejectedCommandsAndRealNativeAutoAttacks_DoNotAdvanceHarmonyOrRelay()
        {
            var sim = Create(Forte);
            var rejected = sim.Submit(BattleCommand.Tap(0, CommandSource.Fixture));
            Assert.False(rejected.Accepted);
            Assert.Equal(CommandReject.NotCharged, rejected.Reason);
            Assert.Equal(0, sim.ExpeditionRelics.DistinctActorBits);
            Assert.False(sim.ExpeditionRelics.ForteStored);

            const int ticks = 80;
            for (var i = 0; i < ticks; i++) sim.Tick();

            Assert.Contains(sim.Casts, cast => cast.CasterAlly && cast.Type == SkillType.Auto);
            Assert.Contains(sim.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Native && r.SourceAlly
                && r.SkillId.EndsWith("_auto", StringComparison.Ordinal) && r.RequestedDamage > 0);
            Assert.Equal(0, sim.ExpeditionRelics.DistinctActorBits);
            Assert.False(sim.ExpeditionRelics.ForteStored);
            foreach (var ally in sim.Allies)
            {
                var expectedCharge = Math.Min(100f, 35f + ticks * BattleSim.TickDt * 100f / ally.Def.ChargeTimeSec);
                Assert.InRange(Math.Abs(ally.Charge - expectedCharge), 0f, 0.01f);
            }
        }

        static ExpeditionBattleInput Input(string[] relics)
        {
            var input = RunBattleFactory.CreateInput("N4", "single", 260921, relics, null);
            input.RunId = "c-integration-fixture";
            input.EncounterId = "c-integration-fixture/N4/1";
            input.AttemptId = "controlled-input";
            input.Stage.Wave0 = new[] { "OE_PROMPTER" };
            input.Characters.Single(c => c.Id == "OE_PROMPTER").Hp = 1000000;
            return input;
        }

        static BattleSim Create(string[] relics)
        {
            var sim = RunBattleFactory.Create(Input(relics));
            Assert.True(sim.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture)).Accepted);
            return sim;
        }

        static void ReadyTap(BattleSim sim, int slot)
        {
            sim.Allies[slot].Charge = 100; // Declared boundary fixture; never used by normal-play evidence.
            var result = sim.Submit(BattleCommand.Tap(slot, CommandSource.Fixture));
            Assert.True(result.Accepted, "Native Tap rejected for slot " + slot + ": " + result.Reason);
        }

        static ResolutionResult[] DamageFromNextTap(BattleSim sim, int slot)
        {
            var start = sim.ExpeditionResolutions.Count;
            ReadyTap(sim, slot);
            return sim.ExpeditionResolutions.Skip(start).Where(r => r.Origin == ResolutionOrigin.Native
                && r.SourceAlly && r.SourceSlot == slot && !r.TargetAlly && r.RequestedDamage > 0).ToArray();
        }
    }
}
