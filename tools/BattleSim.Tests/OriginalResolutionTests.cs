using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class OriginalResolutionTests
    {
        static ResolutionResult Damage(int request = 100, int shield = 30, int hp = 50, int overkill = 20,
            ResolutionOrigin origin = ResolutionOrigin.Native, bool killed = true, bool sourceAlly = true, bool targetAlly = false)
            => new ResolutionResult(origin, 7, origin == ResolutionOrigin.Derived ? 1 : 0,
                origin == ResolutionOrigin.Derived ? "B01" : "", "OE_POINT_tap", 0, sourceAlly, 1, targetAlly, 2,
                requestedDamage: request, shieldAbsorbed: shield, effectiveHpDamage: hp, overkill: overkill, killed: killed);

        [Fact]
        public void ContributionsAreActualShieldAndHpLossWithoutDoubleCountingOverkill()
        {
            var ledger = new ResolutionLedger();
            ledger.Add(Damage());
            Assert.Equal(100, ledger.RequestedDamage);
            Assert.Equal(30, ledger.ShieldAbsorbed);
            Assert.Equal(50, ledger.EffectiveHpDamage);
            Assert.Equal(80, ledger.EffectiveDamage);
            Assert.Equal(80, ledger.EnemyEffectiveDamage);
            Assert.Equal(20, ledger.Overkill);
            Assert.Equal(1, ledger.ResultsCount);
        }

        [Fact]
        public void FullyAbsorbedDamageStillContributesWithoutHpDamage()
        {
            var ledger = new ResolutionLedger();
            ledger.Add(Damage(100, 100, 0, 0, killed: false, sourceAlly: false, targetAlly: true));
            Assert.Equal(100, ledger.AllyShieldAbsorbed);
            Assert.Equal(100, ledger.AllyEffectiveDamageTaken);
            Assert.Equal(0, ledger.EffectiveHpDamage);
            Assert.Equal(0, ledger.EnemyEffectiveDamage);
        }

        [Fact]
        public void FullHpHealingRecordsOverhealAndShieldProductionIsNotAbsorption()
        {
            var ledger = new ResolutionLedger();
            ledger.Add(new ResolutionResult(ResolutionOrigin.Native, 1, 0, "", "OE_HEALER_tap", 2, true, 0, true, 0,
                requestedHeal: 700, effectiveHeal: 0, overheal: 700));
            ledger.Add(new ResolutionResult(ResolutionOrigin.Native, 2, 0, "", "OE_GUARD_tap", 1, true, 0, true, 0,
                shieldProduced: 1200));
            Assert.Equal(700, ledger.RequestedHeal);
            Assert.Equal(700, ledger.Overheal);
            Assert.Equal(0, ledger.EffectiveHeal);
            Assert.Equal(1200, ledger.ShieldProduced);
            Assert.Equal(0, ledger.ShieldAbsorbed);
            Assert.Equal(0, ledger.EffectiveDamage);
        }

        [Fact]
        public void UnknownSourcesDoNotAcquireAllyCreditButActualPartyDamageIsCounted()
        {
            var ledger = new ResolutionLedger();
            ledger.Add(new ResolutionResult(ResolutionOrigin.Dot, 1, 0, "", "", -1, true, 0, false, 0,
                requestedDamage: 10, effectiveHpDamage: 10));
            ledger.Add(new ResolutionResult(ResolutionOrigin.Dot, 2, 0, "", "", -1, false, 0, true, 0,
                requestedDamage: 20, shieldAbsorbed: 5, effectiveHpDamage: 15));
            ledger.Add(new ResolutionResult(ResolutionOrigin.Native, 3, 0, "", "self", 0, true, 0, true, 0,
                requestedDamage: 10, effectiveHpDamage: 10));
            ledger.Add(new ResolutionResult(ResolutionOrigin.Native, 4, 0, "", "heal", 2, true, 0, true, 0,
                requestedHeal: 30, effectiveHeal: 20, overheal: 10));
            Assert.Equal(0, ledger.EnemyEffectiveDamage);
            Assert.Equal(30, ledger.AllyEffectiveDamageTaken);
            Assert.Equal(20, ledger.AllyEffectiveHealing);
            Assert.Equal(5, ledger.AllyShieldAbsorbed);
            Assert.Equal(40, ledger.EffectiveDamage);
        }

        [Fact]
        public void ModelKeepsNativeDerivedAndDotAttributionDistinct()
        {
            var native = Damage(); var derived = Damage(origin: ResolutionOrigin.Derived); var dot = Damage(origin: ResolutionOrigin.Dot);
            Assert.True(native.NativeKill);
            Assert.False(derived.NativeKill);
            Assert.False(dot.NativeKill);
            Assert.True(derived.Killed);
            Assert.Equal("B01", derived.SourceRelicId);
            Assert.Equal(1, derived.ProcDepth);
            Assert.Equal(7, derived.RootActionId);
            Assert.Equal(2, derived.TargetGeneration);
            Assert.True(derived.SourceAlly);
            Assert.False(derived.TargetAlly);
        }

        [Fact]
        public void InvalidOrInconsistentAmountsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => Damage(-1, 0, 0, 0));
            Assert.Throws<ArgumentException>(() => Damage(100, 50, 80, 0));
            Assert.Throws<ArgumentException>(() => Damage(100, 0, 0, 100, killed: true));
            Assert.Throws<ArgumentException>(() => new ResolutionResult(ResolutionOrigin.Native, 1, 0, "", "x", 0, true, 0, true, 0,
                requestedHeal: 10, effectiveHeal: 8, overheal: 5));
            Assert.Throws<ArgumentException>(() => new ResolutionResult(ResolutionOrigin.Native, 1, 0, "", "x", 0, true, 0, true, 0,
                requestedDamage: 10, effectiveHpDamage: 10, requestedHeal: 10, effectiveHeal: 10));
        }

        [Fact]
        public void InvalidCausalityCannotPretendToBeNative()
        {
            Assert.Throws<ArgumentException>(() => new ResolutionResult(ResolutionOrigin.Native, 0, 0, "", "x", 0, true, 0, false, 0));
            Assert.Throws<ArgumentException>(() => new ResolutionResult(ResolutionOrigin.Native, 1, 1, "B01", "x", 0, true, 0, false, 0));
            Assert.Throws<ArgumentException>(() => new ResolutionResult(ResolutionOrigin.Derived, 1, 0, "B01", "x", 0, true, 0, false, 0));
            Assert.Throws<ArgumentException>(() => new ResolutionResult(ResolutionOrigin.Derived, 1, 1, "", "x", 0, true, 0, false, 0));
            Assert.Throws<ArgumentException>(() => new ResolutionResult(ResolutionOrigin.Dot, 1, 0, "", "x", -1, false, 0, true, -1));
        }

        [Fact]
        public void AggregateTotalsUseLongRatherThanOverflowingInt()
        {
            var ledger = new ResolutionLedger();
            var hit = Damage(int.MaxValue, 0, int.MaxValue, 0);
            ledger.Add(hit); ledger.Add(hit); ledger.Add(hit);
            Assert.Equal((long)int.MaxValue * 3, ledger.RequestedDamage);
            Assert.Equal((long)int.MaxValue * 3, ledger.EffectiveDamage);
            Assert.Equal((long)int.MaxValue * 3, ledger.EnemyEffectiveDamage);
        }

        [Fact]
        public void NumericalBoundariesFailExplicitlyInsteadOfWrappingOrSaturating()
        {
            Assert.Throws<OverflowException>(() => ResolutionMath.AddNonNegative(long.MaxValue, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ResolutionMath.AddNonNegative(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ResolutionMath.ToBattleInt(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => ResolutionMath.ToBattleInt(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => ResolutionMath.ToBattleInt(-0.1));
            Assert.Throws<OverflowException>(() => ResolutionMath.ToBattleInt((double)int.MaxValue + 1));
            Assert.Throws<OverflowException>(() => ResolutionMath.Scale(int.MaxValue, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => ResolutionMath.Scale(-2, -2d));
            Assert.Throws<ArgumentOutOfRangeException>(() => ResolutionMath.Scale(2, -2d));
            Assert.Equal(13, ResolutionMath.ToBattleInt(12.5));
            Assert.Equal(0, ResolutionMath.ToBattleInt(0));
        }

        [Fact]
        public void AuthoredFloatRatiosUseDecimalMidpointRounding()
        {
            Assert.Equal(4, ResolutionMath.Scale(5, 0.7f));
            Assert.Equal(8, ResolutionMath.Scale(3, 2.5f));
            Assert.Equal(0, ResolutionMath.Scale(0, 0.7f));
        }

        [Fact]
        public void OverflowLeavesEveryAggregateUnchanged()
        {
            var ledger = new ResolutionLedger();
            // Seed an otherwise unreachable boundary instead of looping billions of times.
            typeof(ResolutionLedger).GetProperty(nameof(ResolutionLedger.EnemyEffectiveDamage))
                .GetSetMethod(true).Invoke(ledger, new object[] { long.MaxValue });
            Assert.Throws<OverflowException>(() => ledger.Add(Damage()));
            Assert.Equal(0, ledger.ResultsCount);
            Assert.Equal(0, ledger.RequestedDamage);
            Assert.Equal(0, ledger.ShieldAbsorbed);
            Assert.Equal(0, ledger.EffectiveHpDamage);
            Assert.Equal(long.MaxValue, ledger.EnemyEffectiveDamage);
        }

        [Fact]
        public void ResultHasNoPublicMutableStateAndSerializationPreservesIt()
        {
            Assert.Empty(typeof(ResolutionResult).GetFields());
            Assert.All(typeof(ResolutionResult).GetProperties(), p => Assert.False(p.SetMethod != null && p.SetMethod.IsPublic, p.Name));
            var original = Damage(origin: ResolutionOrigin.Derived);
            var serializer = new DataContractJsonSerializer(typeof(ResolutionResult));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, original); stream.Position = 0;
                var copy = (ResolutionResult)serializer.ReadObject(stream);
                Assert.Equal(original.RootActionId, copy.RootActionId);
                Assert.Equal(original.TargetGeneration, copy.TargetGeneration);
                Assert.Equal(original.EffectiveDamage, copy.EffectiveDamage);
                Assert.Equal(original.SourceRelicId, copy.SourceRelicId);
                Assert.False(copy.NativeKill);
            }
        }
    }
}
