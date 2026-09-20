using System;
using System.Collections.Generic;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // Pure executor fixtures. Apply performs only requested unit mutations; BattleSim hooks are tested separately.
    public sealed class OriginalExpeditionRelicRuntimeTests
    {
        sealed class Rig
        {
            public readonly ExpeditionRelicRuntime Runtime;
            public readonly UnitState[] Allies = Enumerable.Range(0, 5).Select(i => Unit(i, true)).ToArray();
            public readonly UnitState[] Enemies = Enumerable.Range(0, 4).Select(i => Unit(i, false)).ToArray();
            public readonly List<RelicRequest> Requests = new List<RelicRequest>();
            public readonly RelicBattleView View;
            public long NextRoot = 1;
            public Action<RelicRequest> AfterApply;
            public Rig(params string[] ids)
            {
                var input = RunBattleFactory.CreateInput("N5", "single", 1, ids, null);
                Runtime = new ExpeditionRelicRuntime(input, Enumerable.Repeat(1000, 5).ToArray());
                View = new RelicBattleView { Allies = Allies, Enemies = Enemies, GenerationProvider = u => u.Ally ? 0 : 7 };
            }
            static UnitState Unit(int slot, bool ally) => new UnitState
            { Slot = slot, Ally = ally, Hp = 10000, MaxHp = 10000, Def = new CharacterDef { Id = (ally ? "p" : "e") + slot, IsBoss = !ally && slot == 0 } };
            public RelicNativeAction Begin(int slot = 0, bool ally = true, SkillType type = SkillType.Tap, int targets = 1, bool damage = true)
            {
                var skill = new SkillDef { Id = "fixture-skill", Type = type, Target = targets == 1 ? TargetRule.RandomEnemies : TargetRule.AllEnemies,
                    TargetCount = targets, AtkCoef = damage ? 1f : 0f, HitCount = 1, Opcode = DamageMath.ChannelOpcode(type) };
                return Runtime.BeginNativeAction(NextRoot++, ally ? Allies[slot] : Enemies[slot], skill);
            }
            public void Damage(RelicNativeAction action, int requested = 1000, int shield = 0, int hp = 1000, bool killed = false, int target = 1,
                ResolutionOrigin origin = ResolutionOrigin.Native, int generation = 7)
            {
                var result = new ResolutionResult(origin, action.RootActionId, origin == ResolutionOrigin.Derived ? 1 : 0,
                    origin == ResolutionOrigin.Derived ? "B01" : null, action.SkillId, action.SourceSlot, action.SourceAlly,
                    target, !action.SourceAlly, generation, requestedDamage: requested, shieldAbsorbed: shield,
                    effectiveHpDamage: hp, overkill: requested - shield - hp, killed: killed);
                Runtime.ObserveResolution(action, result);
            }
            public void Complete(RelicNativeAction action) => Runtime.CompleteNativeAction(action, () => View, Apply);
            public void Apply(RelicRequest request)
            {
                Requests.Add(request);
                if (request.Kind == RelicRequestKind.Damage) request.Target.Hp = Math.Max(0, request.Target.Hp - request.Damage);
                else request.Target.Charge = Math.Min(100f, request.Target.Charge + request.Charge);
                AfterApply?.Invoke(request);
            }
            public void Absorb(int amount)
            {
                var enemy = Begin(0, false);
                Damage(enemy, requested: amount, shield: amount, hp: 0, target: 0, generation: 0);
                Complete(enemy);
            }
        }

        [Fact]
        public void A01AggregatesOnlyActualEnemyNativeAbsorptionThenReleasesOneThreshold()
        {
            var rig = new Rig("A01"); var enemy = rig.Begin(0, false);
            rig.Damage(enemy, 50, 50, 0, target: 0); rig.Damage(enemy, 300, 250, 50, target: 1);
            Assert.Equal(0, rig.Runtime.BarrierEnergy);
            rig.Complete(enemy);
            Assert.Equal(200, rig.Runtime.BarrierThreshold); Assert.Equal(300, rig.Runtime.BarrierEnergy);
            rig.View.FocusEnemySlot = 2;
            rig.Complete(rig.Begin(damage: false));
            var hit = Assert.Single(rig.Requests);
            Assert.Equal("A01", hit.SourceRelicId); Assert.Equal(200, hit.Damage); Assert.Equal(2, hit.TargetSlot);
            Assert.Equal(100, rig.Runtime.BarrierEnergy);
        }

        [Fact]
        public void A02EnhancesOnlyAlliedProducedShield()
        {
            var rig = new Rig("A01", "A02");
            Assert.Equal(126, rig.Runtime.AdjustNativeShield(101, true, true));
            Assert.Equal(101, rig.Runtime.AdjustNativeShield(101, false, true));
            Assert.Equal(101, rig.Runtime.AdjustNativeShield(101, true, false));
            Assert.Equal(0, rig.Runtime.BarrierEnergy);
        }

        [Fact]
        public void A04ReplacesNormalShockAndLeavesFractionalThreshold()
        {
            var rig = new Rig("A01", "A03", "A04"); rig.Absorb(599);
            rig.Complete(rig.Begin());
            Assert.Equal(3, rig.Requests.Count);
            Assert.Equal(new[] { "A04", "A03", "A03" }, rig.Requests.Select(r => r.SourceRelicId));
            Assert.Equal(new[] { 800, 400, 400 }, rig.Requests.Select(r => r.Damage));
            Assert.Equal(199, rig.Runtime.BarrierEnergy);
            Assert.Equal(3, rig.Requests.Select(r => r.TargetSlot).Distinct().Count());
        }

        [Theory]
        [InlineData(false, 400)]
        [InlineData(true, 700)]
        public void BUsesFirstEffectivePreShieldHitOnceAndCopiesWithoutRecalculation(bool improved, int expected)
        {
            var rig = improved ? new Rig("B01", "B02", "B03") : new Rig("B01", "B03");
            var action = rig.Begin();
            rig.Damage(action, 1000, 100, 50); rig.Damage(action, 3000, 0, 3000);
            rig.Complete(action);
            Assert.Equal(new[] { expected, expected }, rig.Requests.Select(r => r.Damage));
            Assert.Equal(new[] { 0, 2 }, rig.Requests.Select(r => r.TargetSlot));
            Assert.All(rig.Requests, r => { Assert.Equal(action.RootActionId, r.RootActionId); Assert.Equal(1, r.ProcDepth); Assert.Equal(7, r.TargetGeneration); });
        }

        [Fact]
        public void B04RequiresNativeKillAndRechecksBossAfterScatter()
        {
            var rig = new Rig("B01", "B02", "B04"); var action = rig.Begin();
            rig.Damage(action, 1000, 0, 50, killed: true); rig.Enemies[1].Hp = 0;
            rig.Complete(action);
            Assert.Equal(new[] { "B01", "B04" }, rig.Requests.Select(r => r.SourceRelicId));
            Assert.Equal(new[] { 700, 1000 }, rig.Requests.Select(r => r.Damage));
            Assert.All(rig.Requests, r => Assert.Equal(0, r.TargetSlot));
        }

        [Theory]
        [InlineData(false, 12f)]
        [InlineData(true, 20f)]
        public void CRelaySkipsFullAndDeadAndDoesNotSelectTheCaster(bool improved, float charge)
        {
            var rig = improved ? new Rig("C01", "C02") : new Rig("C01");
            rig.Allies[1].Charge = 100f; rig.Allies[2].Hp = 0;
            rig.Complete(rig.Begin());
            var request = Assert.Single(rig.Requests);
            Assert.Equal(RelicRequestKind.Charge, request.Kind); Assert.Equal(3, request.TargetSlot);
            Assert.Equal(charge, rig.Allies[3].Charge); Assert.Equal(0, rig.Allies[0].Charge);
        }

        [Fact]
        public void C04ArmsAfterThirdActorAndKeepsWholeActionBonusWhileConsumingOnlyOnce()
        {
            var rig = new Rig("C01", "C03", "C04");
            rig.Complete(rig.Begin(0, damage: false)); rig.Complete(rig.Begin(0, damage: false));
            Assert.Equal(1, rig.Runtime.HarmonyActorMask);
            rig.Complete(rig.Begin(1, damage: false));
            var third = rig.Begin(2);
            Assert.Equal(0f, rig.Runtime.DamageChannelBonus(third));
            rig.Damage(third); rig.Complete(third);
            Assert.True(rig.Runtime.ForteStored); Assert.Equal(0, rig.Runtime.HarmonyActorMask);
            var heal = rig.Begin(2, damage: false); rig.Complete(heal);
            Assert.True(rig.Runtime.ForteStored);
            var boosted = rig.Begin(3);
            Assert.Equal(1.2f, rig.Runtime.DamageChannelBonus(boosted));
            rig.Damage(boosted); Assert.False(rig.Runtime.ForteStored);
            Assert.Equal(1.2f, rig.Runtime.DamageChannelBonus(boosted));
            long serial = rig.Runtime.TriggerSerial;
            rig.Damage(boosted); Assert.Equal(serial, rig.Runtime.TriggerSerial);
            rig.Complete(boosted);
        }

        [Theory]
        [InlineData(199, 0, 199)]
        [InlineData(200, 400, 0)]
        [InlineData(600, 1200, 0)]
        [InlineData(9000, 1200, 0)]
        public void A04ThresholdBoundariesAndEnergyCap(int absorbed, int damage, int remaining)
        {
            var rig = new Rig("A01", "A02", "A04"); rig.Absorb(absorbed);
            Assert.Equal(Math.Min(600, absorbed), rig.Runtime.BarrierEnergy);
            rig.Complete(rig.Begin());
            Assert.Equal(remaining, rig.Runtime.BarrierEnergy);
            if (damage == 0) Assert.Empty(rig.Requests);
            else Assert.Equal(damage, Assert.Single(rig.Requests).Damage);
        }

        [Fact]
        public void ARejectsDotDerivedAndNonNativeTierAbsorptionAndDoesNotSpendWithoutEnemies()
        {
            var rig = new Rig("A01");
            var enemy = rig.Begin(0, false);
            rig.Damage(enemy, 600, 600, 0, origin: ResolutionOrigin.Dot);
            rig.Damage(enemy, 600, 600, 0, origin: ResolutionOrigin.Derived);
            rig.Complete(enemy); Assert.Equal(0, rig.Runtime.BarrierEnergy);
            var leader = rig.Begin(0, false, SkillType.Leader);
            rig.Damage(leader, 600, 600, 0); rig.Complete(leader);
            Assert.Equal(0, rig.Runtime.BarrierEnergy);
            rig.Absorb(600);
            rig.Complete(rig.Begin(type: SkillType.Auto));
            Assert.Equal(600, rig.Runtime.BarrierEnergy); Assert.Empty(rig.Requests);
            foreach (var target in rig.Enemies) target.Hp = 0;
            rig.Complete(rig.Begin()); Assert.Equal(600, rig.Runtime.BarrierEnergy);
        }

        [Fact]
        public void A02OnlyMarksActualEnhancedProductionOncePerAction()
        {
            var rig = new Rig("A01", "A02"); var action = rig.Begin(damage: false);
            rig.Runtime.AdjustNativeShield(100, true, true);
            Assert.Equal(0, rig.Runtime.TriggerSerial);
            for (int slot = 0; slot < 5; slot++)
                rig.Runtime.ObserveResolution(action, new ResolutionResult(ResolutionOrigin.Native, action.RootActionId, 0, null,
                    action.SkillId, action.SourceSlot, true, slot, true, 0, shieldProduced: 125));
            Assert.Equal(1, rig.Runtime.TriggerSerial); Assert.Equal("A02", rig.Runtime.LastTriggeredRelicId);
            rig.Complete(action); Assert.Equal(0, rig.Runtime.BarrierEnergy);
        }

        [Fact]
        public void AUsesStableSlotsAfterInvalidFocusAndStopsWhenBossDies()
        {
            var rig = new Rig("A01", "A03"); rig.Absorb(400); rig.View.FocusEnemySlot = 99;
            rig.View.Enemies = new[] { rig.Enemies[3], rig.Enemies[2], rig.Enemies[0], rig.Enemies[1] };
            rig.AfterApply = request => { if (request.TargetSlot == 0) rig.View.StopDerivedDamage = true; };
            rig.Complete(rig.Begin());
            Assert.Equal(0, Assert.Single(rig.Requests).TargetSlot);
            Assert.Equal(200, rig.Runtime.BarrierEnergy);
        }

        [Fact]
        public void BIgnoresGroupDamageSingleSurvivorDerivedKillsAndAbsentOtherTarget()
        {
            var rig = new Rig("B01", "B02", "B04"); var area = rig.Begin(targets: 5);
            rig.Damage(area); rig.Complete(area); Assert.Empty(rig.Requests);
            var single = rig.Begin();
            rig.Damage(single, 1000, 0, 1000);
            rig.Damage(single, 1000, 0, 1, killed: true, origin: ResolutionOrigin.Derived);
            rig.Complete(single); Assert.Equal("B01", Assert.Single(rig.Requests).SourceRelicId);
            rig.Requests.Clear(); foreach (var enemy in rig.Enemies) if (enemy.Slot != 1) enemy.Hp = 0;
            var alone = rig.Begin(); rig.Damage(alone); rig.Complete(alone); Assert.Empty(rig.Requests);
        }

        [Fact]
        public void BSkipsAnInvalidFirstResultAndUsesFirstRealAbsorptionAsReference()
        {
            var rig = new Rig("B01", "B02"); var action = rig.Begin();
            rig.Damage(action, 2000, 0, 0, target: 0);
            rig.Damage(action, 5, 5, 0, target: 2);
            rig.Damage(action, 999, 0, 999, target: 3);
            rig.Complete(action);
            Assert.Equal(4, Assert.Single(rig.Requests).Damage); // Authored 0.7f, halfway away from zero.
            Assert.Equal(0, rig.Requests[0].TargetSlot);
        }

        [Fact]
        public void B04RechecksLiveTargetAfterScatterAndNeverRecursesFromItsKill()
        {
            var rig = new Rig("B01", "B02", "B04"); var action = rig.Begin();
            rig.Damage(action, 1000, 0, 10, killed: true); rig.Enemies[1].Hp = 0;
            rig.Enemies[0].Hp = 1;
            rig.AfterApply = request => rig.Runtime.ObserveResolution(action,
                new ResolutionResult(ResolutionOrigin.Derived, action.RootActionId, 1, request.SourceRelicId, action.SkillId,
                    action.SourceSlot, true, request.TargetSlot, false, request.TargetGeneration,
                    requestedDamage: request.Damage, effectiveHpDamage: 1, overkill: request.Damage - 1, killed: true));
            rig.Complete(action);
            Assert.Equal(new[] { 0, 2 }, rig.Requests.Select(r => r.TargetSlot));
            Assert.Equal(new[] { "B01", "B04" }, rig.Requests.Select(r => r.SourceRelicId));
        }

        [Theory]
        [InlineData(SkillType.Auto)]
        [InlineData(SkillType.Tap)]
        [InlineData(SkillType.Slide)]
        public void BAcceptsOnlyTheDefinedNativeSkillTiers(SkillType type)
        {
            var rig = new Rig("B01"); var action = rig.Begin(type: type);
            rig.Damage(action); rig.Complete(action); Assert.Single(rig.Requests);
        }

        [Fact]
        public void CWrapsClampsAndDoesNothingWhenOnlyCasterIsEligible()
        {
            var rig = new Rig("C01", "C02"); rig.Allies[0].Charge = 95f;
            rig.Complete(rig.Begin(4)); Assert.Equal(100f, rig.Allies[0].Charge);
            rig.Requests.Clear();
            foreach (var ally in rig.Allies) if (ally.Slot != 0) ally.Hp = 0;
            rig.Allies[0].Charge = 0;
            rig.Complete(rig.Begin()); Assert.Empty(rig.Requests); Assert.Equal(0f, rig.Allies[0].Charge);
        }

        [Fact]
        public void CDoesNotProgressFromAutoEnemyOrRejectedAction()
        {
            var rig = new Rig("C01", "C03", "C04");
            rig.Complete(rig.Begin(type: SkillType.Auto)); rig.Complete(rig.Begin(0, false));
            Assert.Empty(rig.Requests); Assert.Equal(0, rig.Runtime.HarmonyActorMask); Assert.False(rig.Runtime.ForteStored);
            // Rejected command creates no Begin/Complete call. Observation of a foreign root is an explicit rule error.
            var active = rig.Begin();
            Assert.Throws<RelicRuleException>(() => rig.Runtime.ObserveResolution(active,
                new ResolutionResult(ResolutionOrigin.Native, active.RootActionId + 1, 0, null, active.SkillId, 0, true, 0, false, 7,
                    requestedDamage: 1, effectiveHpDamage: 1)));
        }

        [Fact]
        public void C04NeverStacksAndCanConsumeOldForteWhileRearmingOnSameAction()
        {
            var rig = new Rig("C01", "C03", "C04");
            for (int cycle = 0; cycle < 2; cycle++)
                for (int slot = 0; slot < 3; slot++) rig.Complete(rig.Begin(slot, damage: false));
            Assert.True(rig.Runtime.ForteStored);
            rig.Complete(rig.Begin(0, damage: false)); rig.Complete(rig.Begin(1, damage: false));
            var third = rig.Begin(2); Assert.Equal(1.2f, rig.Runtime.DamageChannelBonus(third));
            rig.Damage(third); Assert.False(rig.Runtime.ForteStored);
            rig.Complete(third); Assert.True(rig.Runtime.ForteStored); Assert.Equal(0, rig.Runtime.HarmonyActorMask);
            var noTarget = rig.Begin(3); rig.Complete(noTarget); Assert.True(rig.Runtime.ForteStored);
        }

        [Fact]
        public void AllFamiliesUseFixedOrderAndAtMostTwelveMutationsWithoutRecursion()
        {
            var rig = new Rig(ExpeditionContent.Relics.Select(r => r.Id).ToArray());
            rig.Complete(rig.Begin(0, damage: false)); rig.Complete(rig.Begin(1, damage: false));
            rig.Absorb(600); rig.Requests.Clear();
            var action = rig.Begin(2); rig.Damage(action, 1000, 0, 10, killed: true, target: 3); rig.Enemies[3].Hp = 0;
            rig.Complete(action);
            Assert.Equal(new[] { "A04", "A03", "A03", "B01", "B01", "B04", "C01", "C03", "C03", "C03", "C03", "C03" },
                rig.Requests.Select(r => r.SourceRelicId));
            Assert.Equal(12, rig.Runtime.LastRequestCount);
            Assert.All(rig.Requests, r => Assert.Equal(1, r.ProcDepth));
            Assert.Throws<RelicRuleException>(() => rig.Complete(action));
            Assert.True(rig.Runtime.ForteStored);
        }

        [Fact]
        public void FrozenParametersAndThresholdDoNotFollowCallerMutation()
        {
            var input = RunBattleFactory.CreateInput("N1", "single", 1, new[] { "A01", "A02" }, null);
            int[] hp = { 1000, 1000, 1000, 1000, 1004 };
            var runtime = new ExpeditionRelicRuntime(input, hp);
            input.RelicParameters.ShieldBonus = 2f; input.RelicIds[1] = "C01"; hp[0] = 1000000;
            Assert.Equal(200, runtime.Threshold); Assert.Equal(125, runtime.AdjustNativeShield(100, true, true));
            var tiny = new ExpeditionRelicRuntime(RunBattleFactory.CreateInput("N1", "single", 1, new[] { "A01" }, null), new[] { 1, 1, 1, 1, 1 });
            Assert.Equal(1, tiny.Threshold);
        }

        [Fact]
        public void EngineeringOverflowAndInvalidViewsFailInsteadOfWrappingOrTruncating()
        {
            var input = RunBattleFactory.CreateInput("N1", "single", 1, new[] { "B01" }, null);
            input.RelicParameters.ScatterRatio = 10f;
            var runtime = new ExpeditionRelicRuntime(input, Enumerable.Repeat(1000, 5).ToArray());
            var rig = new Rig(); var action = runtime.BeginNativeAction(1, rig.Allies[0],
                new SkillDef { Id = "huge", Opcode = EffectOpcodes.DmgTap, Type = SkillType.Tap, AtkCoef = 1f, Target = TargetRule.RandomEnemies, TargetCount = 1 });
            runtime.ObserveResolution(action, new ResolutionResult(ResolutionOrigin.Native, 1, 0, null, "huge", 0, true, 1, false, 7,
                requestedDamage: int.MaxValue, effectiveHpDamage: 1, overkill: int.MaxValue - 1));
            Assert.Throws<RelicRuleException>(() => runtime.CompleteNativeAction(action, () => rig.View, rig.Apply));
            Assert.Empty(rig.Requests);
            var invalid = rig.Begin(); rig.Allies[1].Charge = float.NaN;
            Assert.Throws<RelicRuleException>(() => rig.Complete(invalid));
        }
    }
}
