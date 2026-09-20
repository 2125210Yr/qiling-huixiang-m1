using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Resonance.Battle
{
    public enum RelicRequestKind { Damage, Charge }

    // Runtime reads these unit references; only the BattleSim-owned apply delegate may mutate units.
    public sealed class RelicBattleView
    {
        public IReadOnlyList<UnitState> Allies;
        public IReadOnlyList<UnitState> Enemies;
        public int FocusEnemySlot = -1;
        public bool StopDerivedDamage;
        public Func<UnitState, int> GenerationProvider;
    }

    public sealed class RelicRequest
    {
        public RelicRequestKind Kind { get; internal set; }
        public string SourceRelicId { get; internal set; }
        public long RootActionId { get; internal set; }
        public int ProcDepth => 1;
        public string SkillId { get; internal set; }
        public int SourceSlot { get; internal set; }
        public bool SourceAlly { get; internal set; }
        public UnitState Target { get; internal set; }
        public int TargetSlot { get; internal set; }
        public int TargetGeneration { get; internal set; }
        public bool TargetAlly { get; internal set; }
        public int Damage { get; internal set; }
        public float Charge { get; internal set; }
    }

    public sealed class RelicNativeAction
    {
        public long RootActionId { get; internal set; }
        public string SkillId { get; internal set; }
        public int SourceSlot { get; internal set; }
        public bool SourceAlly { get; internal set; }
        public bool IsActive { get; internal set; }
        public bool IsSingleTargetDirect { get; internal set; }
        public bool Completed { get; internal set; }
        public int RequestsEmitted { get; internal set; }
        internal ExpeditionRelicRuntime Owner;
        internal bool ForteAtStart;
        internal bool ForteConsumed;
        internal bool IsDirectDamage;
        internal bool EnhancedShieldComputed;
        internal readonly HashSet<string> Triggered = new HashSet<string>(StringComparer.Ordinal);
        internal int FirstDamage;
        internal int FirstTargetSlot = -1;
        internal int FirstTargetGeneration;
        internal bool NativeKill;
        internal long NativeShieldAbsorbed;
    }

    public sealed class RelicRuleException : InvalidOperationException
    {
        public RelicRuleException(string message) : base(message) { }
    }

    public sealed class ExpeditionRelicRuntime
    {
        readonly HashSet<string> _owned;
        readonly Dictionary<string, long> _triggerSerials = new Dictionary<string, long>(StringComparer.Ordinal);
        readonly ExpeditionRelicParameters _parameters;
        readonly int _energyCap;
        RelicNativeAction _current;
        bool _completing;
        public const int MaxRequestsPerAction = 16;
        public int BarrierThreshold { get; private set; }
        public int Threshold => BarrierThreshold;
        public int BarrierEnergy { get; private set; }
        public int DistinctActorBits { get; private set; }
        public int HarmonyActorMask => DistinctActorBits;
        public bool ForteStored { get; private set; }
        public long TriggerSerial { get; private set; }
        public string LastTriggeredRelicId { get; private set; }
        public long LastCompletedRootActionId { get; private set; }
        public int LastRequestCount { get; private set; }

        /// <summary>Global serial at this relic's last real trigger; zero means it has not triggered.</summary>
        public long GetTriggerSerial(string relicId)
        {
            long serial;
            return relicId != null && _triggerSerials.TryGetValue(relicId, out serial) ? serial : 0;
        }
        /// <summary>A stable, read-only copy including owned relics which have not triggered yet.</summary>
        public IReadOnlyDictionary<string, long> RelicTriggerSerials =>
            new ReadOnlyDictionary<string, long>(new Dictionary<string, long>(_triggerSerials, StringComparer.Ordinal));

        public ExpeditionRelicRuntime(ExpeditionBattleInput input, int[] openingMaxHp)
        {
            RunBattleFactory.Validate(input);
            if (openingMaxHp == null || openingMaxHp.Length != 5) throw new ArgumentException("Five frozen opening MaxHP values are required.");
            long total = 0;
            foreach (int hp in openingMaxHp)
            {
                if (hp < 1 || hp > 1000000) throw new ArgumentException("Opening MaxHP is outside the supported domain.");
                total = checked(total + hp);
            }
            _owned = new HashSet<string>(input.RelicIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            foreach (var relicId in _owned) _triggerSerials.Add(relicId, 0);
            _parameters = input.RelicParameters.DeepClone();
            BarrierThreshold = Math.Max(1, (int)decimal.Floor(total * (decimal)_parameters.BarrierThresholdFraction / openingMaxHp.Length));
            _energyCap = checked(BarrierThreshold * _parameters.BarrierStoredThresholds);
        }
        public RelicNativeAction BeginNativeAction(long rootActionId, UnitState caster, SkillDef skill)
        {
            if (_current != null || _completing) throw new RelicRuleException("RELIC_NESTED_NATIVE_ACTION");
            if (rootActionId <= LastCompletedRootActionId || rootActionId <= 0) throw new RelicRuleException("RELIC_ROOT_ORDER");
            if (caster == null || skill == null || caster.Slot < 0 || caster.Slot >= 5) throw new ArgumentException("Invalid native action identity.");
            var damage = EffectOpcodes.IsDamageChannel(skill.Opcode) && (skill.AtkCoef > 0f || skill.FlatPower > 0 || skill.PercentAtk > 0f)
                && skill.HealCoef <= 0f && skill.FlatHeal <= 0 && skill.HealMaxHpFrac <= 0f;
            var nativeTier = skill.Type == SkillType.Auto || skill.Type == SkillType.Tap || skill.Type == SkillType.Slide;
            var foeRule = skill.Target == TargetRule.RandomEnemies || skill.Target == TargetRule.LowestHpEnemies
                || skill.Target == TargetRule.HighestAtkEnemies || skill.Target == TargetRule.LowestHpRatioEnemies;
            _current = new RelicNativeAction { RootActionId = rootActionId, SourceSlot = caster.Slot,
                SourceAlly = caster.Ally, SkillId = skill.Id ?? "", Owner = this,
                IsActive = skill.Type == SkillType.Tap || skill.Type == SkillType.Slide,
                IsDirectDamage = damage && nativeTier,
                IsSingleTargetDirect = damage && nativeTier && foeRule && skill.TargetCount == 1,
                ForteAtStart = ForteStored };
            return _current;
        }
        public float DamageChannelBonus(RelicNativeAction action)
        {
            RequireCurrent(action);
            return action.ForteAtStart && action.SourceAlly && action.IsActive && action.IsDirectDamage ? _parameters.ForteDamageBonus : 0f;
        }
        public int AdjustNativeShield(int basePoints, bool sourceAlly, bool targetAlly)
        {
            if (basePoints < 0) throw new ArgumentOutOfRangeException(nameof(basePoints));
            if (!sourceAlly || !targetAlly || !_owned.Contains("A02")) return basePoints;
            int adjusted = Scale(basePoints, 1f + _parameters.ShieldBonus);
            if (_current != null && adjusted > basePoints) _current.EnhancedShieldComputed = true;
            return adjusted;
        }
        public void ObserveResolution(RelicNativeAction action, ResolutionResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            // Dot and derived observations never enter native budgets, even if they share the root ID.
            if (result.Origin != ResolutionOrigin.Native || result.ProcDepth != 0) return;
            RequireCurrent(action);
            if (_completing || result.RootActionId != action.RootActionId || result.SourceSlot != action.SourceSlot
                || result.SourceAlly != action.SourceAlly || result.SkillId != action.SkillId)
                throw new RelicRuleException("RELIC_RESOLUTION_IDENTITY");
            if (!action.SourceAlly && action.IsDirectDamage && result.TargetAlly && _owned.Contains("A01"))
                action.NativeShieldAbsorbed = checked(action.NativeShieldAbsorbed + result.ShieldAbsorbed);
            if (action.SourceAlly && result.TargetAlly && result.ShieldProduced > 0 && action.EnhancedShieldComputed && _owned.Contains("A02"))
                Mark(action, "A02");
            if (!action.SourceAlly || result.TargetAlly || result.RequestedDamage <= 0 || result.EffectiveDamage <= 0) return;
            if (action.IsSingleTargetDirect)
            {
                if (action.FirstDamage == 0)
                {
                    action.FirstDamage = result.RequestedDamage;
                    action.FirstTargetSlot = result.TargetSlot;
                    action.FirstTargetGeneration = result.TargetGeneration;
                }
                action.NativeKill |= result.NativeKill;
            }
            if (action.ForteAtStart && !action.ForteConsumed && action.IsActive && action.IsDirectDamage)
            {
                ForteStored = false; action.ForteConsumed = true; Mark(action, "C04");
            }
        }
        public void CompleteNativeAction(RelicNativeAction action, Func<RelicBattleView> readCurrent, Action<RelicRequest> applyRequest)
        {
            RequireCurrent(action);
            if (_completing) throw new RelicRuleException("RELIC_REENTRANT_COMPLETE");
            if (readCurrent == null || applyRequest == null) throw new ArgumentNullException("Relic view and application delegates are required.");
            _completing = true;
            try
            {
                if (Read(readCurrent).StopDerivedDamage) return;
                if (!action.SourceAlly)
                {
                    if (_owned.Contains("A01") && action.NativeShieldAbsorbed > 0)
                    {
                        int next = (int)Math.Min(_energyCap, checked(BarrierEnergy + action.NativeShieldAbsorbed));
                        if (next > BarrierEnergy) { BarrierEnergy = next; Mark(action, "A01"); }
                    }
                    return;
                }
                ResolveBarrier(action, readCurrent, applyRequest);
                ResolveScatter(action, readCurrent, applyRequest);
                ResolveRelay(action, readCurrent, applyRequest);
            }
            finally
            {
                action.Completed = true; LastCompletedRootActionId = action.RootActionId;
                LastRequestCount = action.RequestsEmitted; _current = null; _completing = false;
            }
        }

        void ResolveBarrier(RelicNativeAction action, Func<RelicBattleView> read, Action<RelicRequest> apply)
        {
            if (!action.IsActive || !_owned.Contains("A01") || BarrierEnergy < BarrierThreshold) return;
            var view = Read(read); if (view.StopDerivedDamage) return;
            var main = PreferredEnemy(view, true); if (main == null) return;
            bool overload = _owned.Contains("A04");
            int chunks = Math.Min(BarrierEnergy / BarrierThreshold, overload ? _parameters.OverloadThresholds : 1);
            int spent = checked(chunks * BarrierThreshold);
            int damage = Scale(spent, overload ? _parameters.OverloadRatio : _parameters.ShockRatio);
            BarrierEnergy -= spent;
            Emit(action, view, main, overload ? "A04" : "A01", damage, 0f, apply);
            if (!_owned.Contains("A03")) return;
            var excluded = new HashSet<int> { main.Slot };
            for (int i = 0; i < _parameters.ShockSplashTargets; i++)
            {
                view = Read(read); if (view.StopDerivedDamage) return;
                var next = StableEnemy(view, excluded); if (next == null) return;
                excluded.Add(next.Slot);
                Emit(action, view, next, "A03", Scale(damage, _parameters.ShockSplashRatio), 0f, apply);
            }
        }

        void ResolveScatter(RelicNativeAction action, Func<RelicBattleView> read, Action<RelicRequest> apply)
        {
            if (!action.IsSingleTargetDirect || action.FirstDamage <= 0 || !_owned.Contains("B01")) return;
            bool improved = _owned.Contains("B02"), split = _owned.Contains("B03");
            int count = split ? _parameters.ImprovedScatterTargets : _parameters.ScatterTargets;
            int damage = Scale(action.FirstDamage, improved ? _parameters.ImprovedScatterRatio : _parameters.ScatterRatio);
            var excluded = new HashSet<int> { action.FirstTargetSlot };
            for (int i = 0; i < count; i++)
            {
                var view = Read(read); if (view.StopDerivedDamage) return;
                var target = StableEnemy(view, excluded); if (target == null) break;
                excluded.Add(target.Slot);
                Emit(action, view, target, "B01", damage, 0f, apply);
                if (damage > 0 && improved) Mark(action, "B02");
                if (damage > 0 && split && i >= _parameters.ScatterTargets) Mark(action, "B03");
            }
            if (!action.NativeKill || !_owned.Contains("B04")) return;
            var current = Read(read); if (current.StopDerivedDamage) return;
            var echo = PreferredEnemy(current, false); if (echo == null) return;
            Emit(action, current, echo, "B04", Scale(action.FirstDamage, _parameters.KillEchoRatio), 0f, apply);
        }

        void ResolveRelay(RelicNativeAction action, Func<RelicBattleView> read, Action<RelicRequest> apply)
        {
            if (!action.IsActive || !_owned.Contains("C01")) return;
            var view = Read(read); if (view.StopDerivedDamage) return;
            bool improved = _owned.Contains("C02");
            for (int offset = 1; offset < 5; offset++)
            {
                var target = AllyAt(view, (action.SourceSlot + offset) % 5);
                if (target == null || !target.Alive || target.Charge >= 100f) continue;
                float charge = improved ? _parameters.ImprovedRelayCharge : _parameters.RelayCharge;
                Emit(action, view, target, "C01", 0, charge, apply);
                if (charge > 0 && improved) Mark(action, "C02");
                break;
            }
            if (!_owned.Contains("C03")) return;
            DistinctActorBits |= 1 << action.SourceSlot;
            int count = 0;
            for (int bits = DistinctActorBits; bits != 0; bits >>= 1) count += bits & 1;
            if (count < _parameters.HarmonyDistinctActors) return;
            DistinctActorBits = 0;
            Mark(action, "C03");
            for (int slot = 0; slot < 5; slot++)
            {
                view = Read(read); if (view.StopDerivedDamage) return;
                var target = AllyAt(view, slot);
                if (target != null && target.Alive && target.Charge < 100f)
                    Emit(action, view, target, "C03", 0, _parameters.HarmonyCharge, apply);
            }
            // Armed after all native hits and relay processing. Never retroactively boosts this root.
            if (_owned.Contains("C04")) ForteStored = true;
        }

        void Emit(RelicNativeAction action, RelicBattleView view, UnitState target, string relicId, int damage, float charge, Action<RelicRequest> apply)
        {
            if (damage < 0 || float.IsNaN(charge) || float.IsInfinity(charge) || charge < 0) throw new RelicRuleException("RELIC_INVALID_REQUEST");
            if (damage == 0 && charge == 0) return;
            if (++action.RequestsEmitted > MaxRequestsPerAction) throw new RelicRuleException("RELIC_REQUEST_BUDGET_EXCEEDED");
            int generation = view.GenerationProvider == null ? 0 : view.GenerationProvider(target);
            if (generation < 0) throw new RelicRuleException("RELIC_INVALID_GENERATION");
            var request = new RelicRequest { Kind = damage > 0 ? RelicRequestKind.Damage : RelicRequestKind.Charge,
                SourceRelicId = relicId, RootActionId = action.RootActionId, SkillId = action.SkillId,
                SourceSlot = action.SourceSlot, SourceAlly = action.SourceAlly, Target = target,
                TargetSlot = target.Slot, TargetAlly = target.Ally, TargetGeneration = generation, Damage = damage, Charge = charge };
            apply(request);
            Mark(action, relicId);
        }

        void Mark(RelicNativeAction action, string relicId)
        {
            if (!action.Triggered.Add(relicId)) return;
            TriggerSerial = checked(TriggerSerial + 1); LastTriggeredRelicId = relicId;
            _triggerSerials[relicId] = TriggerSerial;
        }

        void RequireCurrent(RelicNativeAction action)
        {
            if (action == null || action.Owner != this || action != _current || action.Completed)
                throw new RelicRuleException("RELIC_ACTION_NOT_ACTIVE");
        }

        static RelicBattleView Read(Func<RelicBattleView> getter)
        {
            var view = getter();
            if (view == null || view.Allies == null || view.Enemies == null || view.Allies.Count > 5 || view.Enemies.Count > 5)
                throw new RelicRuleException("RELIC_INVALID_VIEW");
            ValidateUnits(view.Allies, true); ValidateUnits(view.Enemies, false);
            return view;
        }

        static void ValidateUnits(IReadOnlyList<UnitState> units, bool ally)
        {
            int slots = 0;
            foreach (var unit in units)
            {
                if (unit == null) continue;
                if (unit.Ally != ally || unit.Slot < 0 || unit.Slot >= 5 || (slots & (1 << unit.Slot)) != 0
                    || float.IsNaN(unit.Charge) || float.IsInfinity(unit.Charge) || unit.Charge < 0f || unit.Charge > 100f
                    || unit.Hp < 0 || unit.MaxHp <= 0 || unit.Hp > unit.MaxHp)
                    throw new RelicRuleException("RELIC_INVALID_UNIT_VIEW");
                slots |= 1 << unit.Slot;
            }
        }

        static UnitState AllyAt(RelicBattleView view, int slot)
        {
            foreach (var unit in view.Allies) if (unit != null && unit.Slot == slot) return unit;
            return null;
        }

        static UnitState PreferredEnemy(RelicBattleView view, bool allowFocus)
        {
            UnitState boss = null;
            foreach (var unit in view.Enemies)
            {
                if (unit == null || !unit.Alive) continue;
                if (allowFocus && unit.Slot == view.FocusEnemySlot) return unit;
                if (unit.Def != null && unit.Def.IsBoss && (boss == null || unit.Slot < boss.Slot)) boss = unit;
            }
            return boss ?? StableEnemy(view, null);
        }

        static UnitState StableEnemy(RelicBattleView view, HashSet<int> excluded)
        {
            UnitState result = null;
            foreach (var unit in view.Enemies)
                if (unit != null && unit.Alive && (excluded == null || !excluded.Contains(unit.Slot)) && (result == null || unit.Slot < result.Slot)) result = unit;
            return result;
        }

        static int Scale(int amount, float factor)
        {
            try { return ResolutionMath.Scale(amount, factor); }
            catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
            { throw new RelicRuleException("RELIC_AMOUNT_OUT_OF_RANGE: " + ex.Message); }
        }
    }
}
