using System;
using System.Linq;
using System.Reflection;
using Resonance.Battle;
using Xunit;
using Xunit.Abstractions;

namespace Resonance.Tests
{
    // Acceptance boundary fixtures, not ordinary play. The current combat model has no
    // general hit-chance/miss mechanic. O-009 below tests the executor's absent/zero-result
    // contract; O-010 uses controlled frozen stats/charge and real Submit/Tick settlement.
    // O-016 injects only a request-counter precondition during a real native action.
    public sealed class OriginalRelicAcceptanceBoundaryTests
    {
        readonly ITestOutputHelper _output;
        public OriginalRelicAcceptanceBoundaryTests(ITestOutputHelper output) { _output = output; }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void O009_AbsentOrZeroEffectiveNativeHitDoesNotAccumulateBarrier(bool zeroResult)
        {
            // Synthetic immutable resolution records deliberately isolate the relic contract;
            // they are not evidence that BattleSim implements a general miss mechanic.
            var input = RunBattleFactory.CreateInput("N1", "single", 910091, new[] { "A01" }, null);
            var sim = new BattleSim(input);
            var runtime = new ExpeditionRelicRuntime(input, sim.Allies.Select(u => u.MaxHp).ToArray());
            var caster = sim.Enemies[0];
            var skill = input.Skills.Single(s => s.Id == caster.Def.TapSkillId);
            var view = new RelicBattleView { Allies = sim.Allies, Enemies = sim.Enemies,
                GenerationProvider = unit => unit.InstanceGeneration };
            var requests = 0;
            var action = runtime.BeginNativeAction(1, caster, skill);
            if (zeroResult)
            {
                var emptyHit = NativeAbsorption(action, sim.Allies[0], 0);
                Assert.Equal(0, emptyHit.RequestedDamage);
                Assert.Equal(0, emptyHit.EffectiveDamage);
                runtime.ObserveResolution(action, emptyHit);
            }

            runtime.CompleteNativeAction(action, () => view, request => requests++);

            Assert.True(action.Completed);
            Assert.Equal(0, runtime.BarrierEnergy);
            Assert.Equal(0, runtime.GetTriggerSerial("A01"));
            Assert.Equal(0, runtime.LastRequestCount);
            Assert.Equal(0, requests);

            // Positive control: the same native identity does accumulate actual absorption.
            // This prevents a misclassified enemy skill from making the zero case vacuous.
            var positive = runtime.BeginNativeAction(2, caster, skill);
            runtime.ObserveResolution(positive, NativeAbsorption(positive, sim.Allies[0], 100));
            runtime.CompleteNativeAction(positive, () => view, request => requests++);
            Assert.Equal(100, runtime.BarrierEnergy);
            Assert.True(runtime.GetTriggerSerial("A01") > 0);
            Assert.Equal(0, requests);
            _output.WriteLine("O-009 scope=executor-contract-fixture; observation={0}; energy=0; trigger=0; positiveControlEnergy=100; generalMissMechanic=false",
                zeroResult ? "zero-effective-native-result" : "no-hit-result");
        }

        [Fact]
        public void O010_FullBarrierFamilyWithOneEnemyEmitsOnlyOneOverloadAndNoSplash()
        {
            var input = RunBattleFactory.CreateInput("N1", "single", 910101,
                new[] { "A01", "A02", "A03", "A04" }, null);
            input.Stage.Wave0 = new[] { "OE_PROMPTER" };
            foreach (var character in input.Characters)
            {
                character.Def = 0; character.Crt = 0; character.Element = Element.Fire;
                if (character.IsEnemy)
                {
                    character.Hp = 100000;
                    character.ChargeTimeSec = 100000;
                    character.SlideSkillId = character.TapSkillId;
                }
            }
            var incoming = input.Skills.Single(s => s.Id == "OE_PROMPTER_tap");
            incoming.AtkCoef = 0; incoming.FlatPower = 449; incoming.SkillFlat = 0; incoming.PercentAtk = 0;
            incoming.Target = TargetRule.AllEnemies; incoming.TargetCount = 5; incoming.HitCount = 1; incoming.EffectId = null;
            RunBattleFactory.Validate(input);
            var sim = new BattleSim(input);
            foreach (var unit in sim.Allies.Concat(sim.Enemies)) unit.AutoTimer = -100;
            ReadyTap(sim, 1); // Real A02-enhanced native team shield.
            Assert.True(sim.ExpeditionRelics.GetTriggerSerial("A02") > 0);
            var partyHp = sim.Allies.Select(u => u.Hp).ToArray();
            Assert.Single(sim.Enemies);
            sim.Enemies[0].Charge = 100;

            sim.Tick();

            Assert.Equal(BattleOutcome.InProgress, sim.Outcome);
            var enemyHits = sim.ExpeditionResolutions.Where(r => r.Origin == ResolutionOrigin.Native
                && !r.SourceAlly && r.RequestedDamage > 0).ToArray();
            Assert.Equal(5, enemyHits.Length);
            Assert.All(enemyHits, hit => { Assert.Equal(449, hit.ShieldAbsorbed); Assert.Equal(0, hit.EffectiveHpDamage); });
            Assert.Equal(partyHp, sim.Allies.Select(u => u.Hp).ToArray());
            Assert.Equal(1120, sim.ExpeditionRelics.BarrierThreshold);
            Assert.Equal(2245, sim.ExpeditionRelics.BarrierEnergy);
            var enemyHp = sim.Enemies[0].Hp;

            ReadyTap(sim, 2); // Full-health healing is a completed active and releases the stored A family.

            var derived = sim.ExpeditionResolutions.Where(r => r.Origin == ResolutionOrigin.Derived).ToArray();
            var overload = Assert.Single(derived);
            Assert.Equal("A04", overload.SourceRelicId);
            Assert.Equal(0, overload.TargetSlot);
            Assert.Equal(sim.Enemies[0].InstanceGeneration, overload.TargetGeneration);
            Assert.Equal(1, overload.ProcDepth);
            Assert.Equal(4480, overload.RequestedDamage);
            Assert.Equal(4480, overload.EffectiveHpDamage);
            Assert.Equal(enemyHp - 4480, sim.Enemies[0].Hp);
            Assert.DoesNotContain(derived, result => result.SourceRelicId == "A01" || result.SourceRelicId == "A03");
            Assert.Equal(1, sim.ExpeditionRelics.LastRequestCount);
            Assert.Equal(5, sim.ExpeditionRelics.BarrierEnergy);
            Assert.Equal(0, sim.ExpeditionRelics.GetTriggerSerial("A03"));
            Assert.True(sim.ExpeditionRelics.GetTriggerSerial("A04") > 0);
            Assert.Equal(BattleOutcome.InProgress, sim.Outcome);
            _output.WriteLine("O-010 scope=controlled-BattleSim-fixture; enemies=1; energyBefore=2245; threshold=1120; A04Requests=1; A01Requests=0; A03Requests=0; requested=4480; actualHpDamage=4480; energyAfter=5");
        }

        [Fact]
        public void O016_InjectedRequestBudgetOverflowFailsThroughSubmitAndStopsFurtherSettlement()
        {
            // Current authored combinations do not naturally exhaust 16 requests. The forwarding
            // RNG is only a synchronous observation point after BeginNativeAction: it preserves
            // every random value and injects RequestsEmitted, never damage, Outcome, or exceptions.
            // Production Emit throws; production Cast catches and publishes the Failed reason.
            var control = BudgetFixture();
            var overflow = BudgetFixture();
            var safeAttempt = SubmitWithRequestCount(control, ExpeditionRelicRuntime.MaxRequestsPerAction - 1);
            Assert.True(safeAttempt.Accepted, control.FailedReason);
            Assert.Equal(BattleOutcome.InProgress, control.Outcome);
            Assert.Equal(ExpeditionRelicRuntime.MaxRequestsPerAction, control.ExpeditionRelics.LastRequestCount);
            var safeDerived = Assert.Single(control.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Derived);
            Assert.Equal("B01", safeDerived.SourceRelicId);
            Assert.True(safeDerived.EffectiveDamage > 0);

            var failedAttempt = SubmitWithRequestCount(overflow, ExpeditionRelicRuntime.MaxRequestsPerAction);

            Assert.False(failedAttempt.Accepted);
            Assert.Equal(BattleOutcome.Failed, overflow.Outcome);
            Assert.Equal("ORIGINAL_RULE_ERROR RelicRuleException: RELIC_REQUEST_BUDGET_EXCEEDED", overflow.FailedReason);
            Assert.Equal(ExpeditionRelicRuntime.MaxRequestsPerAction + 1, overflow.ExpeditionRelics.LastRequestCount);
            Assert.Equal(0, overflow.ExpeditionRelics.GetTriggerSerial("B01"));
            Assert.DoesNotContain(overflow.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Derived);
            var safeNative = Assert.Single(control.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Native);
            var failedNative = Assert.Single(overflow.ExpeditionResolutions);
            Assert.Equal(ResolutionOrigin.Native, failedNative.Origin);
            Assert.Equal(safeNative.RequestedDamage, failedNative.RequestedDamage);
            Assert.Equal(safeNative.EffectiveHpDamage, failedNative.EffectiveHpDamage);
            Assert.Equal(control.Enemies[0].Hp, overflow.Enemies[0].Hp);
            Assert.Equal(overflow.Enemies[1].MaxHp, overflow.Enemies[1].Hp);
            Assert.Equal(1, overflow.ExpeditionTotals.ResultsCount);
            Assert.Equal(failedNative.EffectiveDamage, overflow.ExpeditionTotals.EnemyEffectiveDamage);

            var partyHp = overflow.Allies.Select(u => u.Hp).ToArray();
            var enemyHp = overflow.Enemies.Select(u => u.Hp).ToArray();
            var count = overflow.ExpeditionResolutions.Count;
            var tick = overflow.TickIndex;
            var reason = overflow.FailedReason;
            var rejected = overflow.Submit(BattleCommand.Tap(0, CommandSource.Fixture));
            Assert.False(rejected.Accepted);
            Assert.Equal(CommandReject.NotInProgress, rejected.Reason);
            overflow.Tick();
            Assert.Equal(BattleOutcome.Failed, overflow.Outcome);
            Assert.Equal(reason, overflow.FailedReason);
            Assert.Equal(tick, overflow.TickIndex);
            Assert.Equal(count, overflow.ExpeditionResolutions.Count);
            Assert.Equal(partyHp, overflow.Allies.Select(u => u.Hp).ToArray());
            Assert.Equal(enemyHp, overflow.Enemies.Select(u => u.Hp).ToArray());
            _output.WriteLine("O-016 scope=reflection-injected-budget-precondition; naturalPlay=false; budget=16; precondition=16; attempted=17; derivedApplied=0; outcome={0}; reason={1}; postFailureSettlement=none",
                overflow.Outcome, overflow.FailedReason);
        }

        static ResolutionResult NativeAbsorption(RelicNativeAction action, UnitState target, int absorbed)
            => new ResolutionResult(ResolutionOrigin.Native, action.RootActionId, 0, null, action.SkillId,
                action.SourceSlot, action.SourceAlly, target.Slot, target.Ally, target.InstanceGeneration,
                requestedDamage: absorbed, shieldAbsorbed: absorbed);

        static void ReadyTap(BattleSim sim, int slot)
        {
            sim.Allies[slot].Charge = 100;
            var result = sim.Submit(BattleCommand.Tap(slot, CommandSource.Fixture));
            Assert.True(result.Accepted, result.Reason + " " + sim.FailedReason);
        }

        static BattleSim BudgetFixture()
        {
            var input = RunBattleFactory.CreateInput("N1", "single", 910161, new[] { "B01" }, null);
            input.Stage.EnemyHpMul = 10;
            RunBattleFactory.Validate(input);
            var sim = new BattleSim(input);
            Assert.True(sim.Submit(BattleCommand.FocusEnemy(0, CommandSource.Fixture)).Accepted);
            sim.Allies[0].Charge = 100;
            return sim;
        }

        static CommandResult SubmitWithRequestCount(BattleSim sim, int initialCount)
        {
            var rngField = typeof(BattleSim).GetField("_rng", BindingFlags.Instance | BindingFlags.NonPublic);
            var actionField = typeof(BattleSim).GetField("_originalAction", BindingFlags.Instance | BindingFlags.NonPublic);
            var countProperty = typeof(RelicNativeAction).GetProperty(nameof(RelicNativeAction.RequestsEmitted));
            Assert.NotNull(rngField); Assert.NotNull(actionField); Assert.NotNull(countProperty);
            var original = (Random)rngField.GetValue(sim);
            RelicNativeAction injectedAction = null;
            var forwarding = new ForwardingRandom(original, () =>
            {
                if (injectedAction != null) return;
                injectedAction = (RelicNativeAction)actionField.GetValue(sim);
                Assert.NotNull(injectedAction);
                Assert.True(injectedAction.SourceAlly);
                Assert.Equal(0, injectedAction.SourceSlot);
                Assert.Equal(0, injectedAction.RequestsEmitted);
                countProperty.SetValue(injectedAction, initialCount);
            });
            rngField.SetValue(sim, forwarding);
            CommandResult attempt;
            try { attempt = sim.Submit(BattleCommand.Tap(0, CommandSource.Fixture)); }
            finally { rngField.SetValue(sim, original); }
            Assert.NotNull(injectedAction);
            Assert.True(injectedAction.Completed);
            Assert.Equal(injectedAction.RootActionId, sim.ExpeditionRelics.LastCompletedRootActionId);
            return attempt;
        }

        sealed class ForwardingRandom : Random
        {
            readonly Random _original;
            readonly Action _beforeRoll;
            public ForwardingRandom(Random original, Action beforeRoll) { _original = original; _beforeRoll = beforeRoll; }
            public override double NextDouble() { _beforeRoll(); return _original.NextDouble(); }
            public override int Next() => _original.Next();
            public override int Next(int maxValue) => _original.Next(maxValue);
            public override int Next(int minValue, int maxValue) => _original.Next(minValue, maxValue);
            public override void NextBytes(byte[] buffer) => _original.NextBytes(buffer);
        }
    }
}
