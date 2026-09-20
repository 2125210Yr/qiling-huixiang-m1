using System;
using System.Runtime.Serialization;

namespace Resonance.Battle
{
    public enum ResolutionOrigin { Native, Derived, Dot }

    // One authoritative, completed application to one target instance. The caller owns HP/shield mutation.
    [DataContract]
    public sealed class ResolutionResult
    {
        [DataMember(Order = 0)] public ResolutionOrigin Origin { get; private set; }
        [DataMember(Order = 1)] public long RootActionId { get; private set; }
        [DataMember(Order = 2)] public int ProcDepth { get; private set; }
        [DataMember(Order = 3)] public string SourceRelicId { get; private set; }
        [DataMember(Order = 4)] public string SkillId { get; private set; }
        [DataMember(Order = 5)] public int SourceSlot { get; private set; }
        [DataMember(Order = 6)] public bool SourceAlly { get; private set; }
        [DataMember(Order = 7)] public int TargetSlot { get; private set; }
        [DataMember(Order = 8)] public bool TargetAlly { get; private set; }
        [DataMember(Order = 9)] public int TargetGeneration { get; private set; }
        [DataMember(Order = 10)] public int RequestedDamage { get; private set; }
        [DataMember(Order = 11)] public int ShieldAbsorbed { get; private set; }
        [DataMember(Order = 12)] public int EffectiveHpDamage { get; private set; }
        [DataMember(Order = 13)] public int Overkill { get; private set; }
        [DataMember(Order = 14)] public int RequestedHeal { get; private set; }
        [DataMember(Order = 15)] public int EffectiveHeal { get; private set; }
        [DataMember(Order = 16)] public int Overheal { get; private set; }
        [DataMember(Order = 17)] public int ShieldProduced { get; private set; }
        [DataMember(Order = 18)] public bool Killed { get; private set; }
        public bool NativeKill => Origin == ResolutionOrigin.Native && Killed;
        public int EffectiveDamage => checked(ShieldAbsorbed + EffectiveHpDamage);

        public ResolutionResult(ResolutionOrigin origin, long rootActionId, int procDepth, string sourceRelicId,
            string skillId, int sourceSlot, bool sourceAlly, int targetSlot, bool targetAlly, int targetGeneration,
            int requestedDamage = 0, int shieldAbsorbed = 0, int effectiveHpDamage = 0, int overkill = 0,
            int requestedHeal = 0, int effectiveHeal = 0, int overheal = 0, int shieldProduced = 0, bool killed = false)
        {
            Origin = origin; RootActionId = rootActionId; ProcDepth = procDepth; SourceRelicId = sourceRelicId ?? "";
            SkillId = skillId ?? ""; SourceSlot = sourceSlot; SourceAlly = sourceAlly;
            TargetSlot = targetSlot; TargetAlly = targetAlly; TargetGeneration = targetGeneration;
            RequestedDamage = requestedDamage; ShieldAbsorbed = shieldAbsorbed; EffectiveHpDamage = effectiveHpDamage;
            Overkill = overkill; RequestedHeal = requestedHeal; EffectiveHeal = effectiveHeal; Overheal = overheal;
            ShieldProduced = shieldProduced; Killed = killed;
            Validate();
        }

        void Validate()
        {
            SourceRelicId = SourceRelicId ?? "";
            SkillId = SkillId ?? "";
            if (!Enum.IsDefined(typeof(ResolutionOrigin), Origin) || RootActionId <= 0 ||
                SourceSlot < -1 || SourceSlot > 4 || TargetSlot < 0 || TargetSlot > 4 || TargetGeneration < 0)
                throw new ArgumentException("Resolution identity is outside the supported battle domain.");
            if (Origin == ResolutionOrigin.Derived
                ? ProcDepth != 1 || string.IsNullOrWhiteSpace(SourceRelicId)
                : ProcDepth != 0 || SourceRelicId.Length != 0)
                throw new ArgumentException("Only a single non-recursive relic derivation may carry a relic ID.");

            if (RequestedDamage < 0 || ShieldAbsorbed < 0 || EffectiveHpDamage < 0 || Overkill < 0 ||
                RequestedHeal < 0 || EffectiveHeal < 0 || Overheal < 0 || ShieldProduced < 0)
                throw new ArgumentException("Resolution amounts must be non-negative.");
            if ((long)RequestedDamage != (long)ShieldAbsorbed + EffectiveHpDamage + Overkill ||
                (long)RequestedHeal != (long)EffectiveHeal + Overheal)
                throw new ArgumentException("Requested amounts must equal their actual and excess contributions.");
            if (RequestedDamage > 0 && RequestedHeal > 0)
                throw new ArgumentException("Damage and healing require separate resolution results.");
            if (Killed && EffectiveHpDamage == 0)
                throw new ArgumentException("A kill requires actual HP loss in this resolution.");
        }
        [OnDeserialized] void OnDeserialized(StreamingContext context) => Validate();
    }

    public static class ResolutionMath
    {
        public static long AddNonNegative(long left, long right)
        {
            if (left < 0) throw new ArgumentOutOfRangeException(nameof(left));
            if (right < 0) throw new ArgumentOutOfRangeException(nameof(right));
            return checked(left + right);
        }

        public static int ToBattleInt(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Battle values must be finite and non-negative.");
            var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
            if (rounded > int.MaxValue) throw new OverflowException("Battle value exceeds Int32 capacity.");
            return checked((int)rounded);
        }

        public static int Scale(int value, double multiplier)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            return ToBattleInt(value * multiplier);
        }

        // Frozen content uses authored float ratios (for example 0.7f). Decimal conversion preserves that
        // decimal precision so a binary approximation just below 3.5 cannot change a declared midpoint.
        public static int Scale(int value, float multiplier)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            var rounded = Math.Round(checked(value * (decimal)multiplier), MidpointRounding.AwayFromZero);
            if (rounded > int.MaxValue) throw new OverflowException("Battle value exceeds Int32 capacity.");
            return checked((int)rounded);
        }
    }

    public sealed class ResolutionLedger
    {
        public long ResultsCount { get; private set; }
        public long RequestedDamage { get; private set; }
        public long ShieldAbsorbed { get; private set; }
        public long EffectiveHpDamage { get; private set; }
        public long Overkill { get; private set; }
        public long RequestedHeal { get; private set; }
        public long EffectiveHeal { get; private set; }
        public long Overheal { get; private set; }
        public long ShieldProduced { get; private set; }
        public long EnemyEffectiveDamage { get; private set; }
        public long AllyEffectiveDamageTaken { get; private set; }
        public long AllyEffectiveHealing { get; private set; }
        public long AllyShieldAbsorbed { get; private set; }
        public long EffectiveDamage => ResolutionMath.AddNonNegative(ShieldAbsorbed, EffectiveHpDamage);

        public void Add(ResolutionResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            // Compute all next values before publishing any of them. An overflow leaves this ledger intact.
            var count = ResolutionMath.AddNonNegative(ResultsCount, 1);
            var requestedDamage = ResolutionMath.AddNonNegative(RequestedDamage, result.RequestedDamage);
            var shieldAbsorbed = ResolutionMath.AddNonNegative(ShieldAbsorbed, result.ShieldAbsorbed);
            var hpDamage = ResolutionMath.AddNonNegative(EffectiveHpDamage, result.EffectiveHpDamage);
            var overkill = ResolutionMath.AddNonNegative(Overkill, result.Overkill);
            var requestedHeal = ResolutionMath.AddNonNegative(RequestedHeal, result.RequestedHeal);
            var effectiveHeal = ResolutionMath.AddNonNegative(EffectiveHeal, result.EffectiveHeal);
            var overheal = ResolutionMath.AddNonNegative(Overheal, result.Overheal);
            var shieldProduced = ResolutionMath.AddNonNegative(ShieldProduced, result.ShieldProduced);
            var knownAllySource = result.SourceSlot >= 0 && result.SourceAlly;
            var enemyDamage = ResolutionMath.AddNonNegative(EnemyEffectiveDamage,
                knownAllySource && !result.TargetAlly ? result.EffectiveDamage : 0);
            var allyDamageTaken = ResolutionMath.AddNonNegative(AllyEffectiveDamageTaken,
                result.TargetAlly ? result.EffectiveDamage : 0);
            var allyHealing = ResolutionMath.AddNonNegative(AllyEffectiveHealing,
                knownAllySource && result.TargetAlly ? result.EffectiveHeal : 0);
            var allyAbsorbed = ResolutionMath.AddNonNegative(AllyShieldAbsorbed,
                result.TargetAlly ? result.ShieldAbsorbed : 0);
            ResolutionMath.AddNonNegative(shieldAbsorbed, hpDamage);

            ResultsCount = count; RequestedDamage = requestedDamage; ShieldAbsorbed = shieldAbsorbed;
            EffectiveHpDamage = hpDamage; Overkill = overkill; RequestedHeal = requestedHeal;
            EffectiveHeal = effectiveHeal; Overheal = overheal; ShieldProduced = shieldProduced;
            EnemyEffectiveDamage = enemyDamage; AllyEffectiveDamageTaken = allyDamageTaken;
            AllyEffectiveHealing = allyHealing; AllyShieldAbsorbed = allyAbsorbed;
        }
    }
}
