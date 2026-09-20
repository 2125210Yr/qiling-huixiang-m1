using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    public sealed class EncounterView
    {
        public IReadOnlyList<UnitState> Enemies;
        public BattleOutcome Outcome;
    }

    public enum EncounterRequestKind { AutoAttack, AreaAttack, RebuildMissingMasks }

    /// <summary>A bounded request. BattleSim remains the sole owner of combat and spawning.</summary>
    public sealed class EncounterRequest
    {
        public EncounterRequestKind Kind;
        public long ActionId;
        public string SkillId;
        public int CasterSlot;
        public float DamageMultiplier = 1f;
        public string MaskCharacterId;
        public int[] MaskSlots = Array.Empty<int>();
        public int[] ExpectedGenerations = Array.Empty<int>();
    }

    /// <summary>A copied presentation/replay view of the authoritative encounter clocks.</summary>
    public sealed class EncounterSnapshot
    {
        public string Stage;
        public bool IsBoss;
        public string AreaName;
        public int Phase;
        public bool PhasePending;
        public bool IsCasting;
        public float RemainingCastSec;
        public float CastDurationSec;
        public float NextIntentSec;
        public float AutoRemainingSec;
        public double ElapsedSec;
        public int AliveMasks;
        public float AreaMultiplier;
        public long ActionSerial;
        public long IntentSerial;
        public int AreaCasts;
        public int MaskRebuildCount;
        public bool Cancelled;
    }

    /// <summary>Fixed N4/N7 encounter scheduler; it does not resolve damage or mutate units.</summary>
    public sealed class BossEncounterController
    {
        readonly BossEncounterDef _boss;
        readonly EliteEncounterDef _elite;
        readonly int _casterSlot;
        readonly float _castDuration;
        readonly float _autoInterval;
        readonly string _autoSkill;
        readonly string _areaSkill;
        double _castDue;
        double _nextIntentAt;
        double _nextAutoAt;
        double _lastAreaAt = -1d;
        bool _dispatching;

        public string Stage { get; private set; }
        public int Phase { get; private set; } = 1;
        public bool PhasePending { get; private set; }
        public bool IsCasting { get; private set; }
        public float RemainingCastSec => Cancelled || !IsCasting ? 0f : Remaining(_castDue);
        public float NextIntentSec => Cancelled || IsCasting ? 0f : Remaining(_nextIntentAt);
        public float AutoRemainingSec => Cancelled || IsCasting ? 0f : Remaining(_nextAutoAt);
        public long ActionSerial { get; private set; }
        public long IntentSerial { get; private set; }
        public int AreaCasts { get; private set; }
        public int MaskRebuildCount { get; private set; }
        public bool Cancelled { get; private set; }
        public double ElapsedSec { get; private set; }

        public BossEncounterController(BossEncounterDef def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            _boss = def.DeepClone();
            if (_boss.BossSlot != 0 || _boss.MaskSlots == null || _boss.MaskSlots.Length != 2 ||
                _boss.MaskSlots[0] != 1 || _boss.MaskSlots[1] != 2 || string.IsNullOrWhiteSpace(_boss.MaskCharacterId))
                throw new ArgumentException("N7 requires boss slot 0 and fixed mask slots 1, 2.", nameof(def));
            Positive(_boss.FirstIntentSec); Positive(_boss.CastDurationSec); Positive(_boss.PhaseOneIntervalSec);
            Positive(_boss.PhaseTwoIntervalSec); Positive(_boss.AutoIntervalSec); Positive(_boss.PhaseTwoHpFraction);
            if (_boss.PhaseTwoHpFraction > 1f || !Finite(_boss.DamagePerLivingMask) || _boss.DamagePerLivingMask < 0f)
                throw new ArgumentException("Invalid boss phase or mask multiplier.", nameof(def));
            RequireSkillIds(_boss.AutoSkillId, _boss.AreaSkillId);
            Stage = "N7"; _casterSlot = _boss.BossSlot; _castDuration = _boss.CastDurationSec;
            _autoInterval = _boss.AutoIntervalSec; _autoSkill = _boss.AutoSkillId; _areaSkill = _boss.AreaSkillId;
            _nextIntentAt = _boss.FirstIntentSec; _nextAutoAt = _autoInterval;
        }

        public BossEncounterController(EliteEncounterDef def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            _elite = def.DeepClone();
            if (_elite.CasterSlot != 0) throw new ArgumentException("N4 requires caster slot 0.", nameof(def));
            Positive(_elite.FirstIntentSec); Positive(_elite.CastDurationSec); Positive(_elite.IntervalSec); Positive(_elite.AutoIntervalSec);
            RequireSkillIds(_elite.AutoSkillId, _elite.AreaSkillId);
            Stage = "N4"; _casterSlot = _elite.CasterSlot; _castDuration = _elite.CastDurationSec;
            _autoInterval = _elite.AutoIntervalSec; _autoSkill = _elite.AutoSkillId; _areaSkill = _elite.AreaSkillId;
            _nextIntentAt = _elite.FirstIntentSec; _nextAutoAt = _autoInterval;
        }

        public bool OwnsEnemySlot(int slot) => slot == _casterSlot || (_boss != null && (slot == 1 || slot == 2));

        /// <summary>
        /// Called once per accepted world tick, after status/outcome resolution. Pause and policy holds do not call it.
        /// Locks defer due attacks while time continues; an overdue cast remains visible and blocks all autos.
        /// </summary>
        public void Advance(float worldDt, Func<EncounterView> readCurrent, Action<EncounterRequest> apply)
        {
            if (!Finite(worldDt) || worldDt < 0f) throw new ArgumentOutOfRangeException(nameof(worldDt));
            RequireCallbacks(readCurrent, apply);
            if (_dispatching) throw new InvalidOperationException("An encounter callback cannot advance its clock recursively.");
            if (Cancelled) return;
            ObserveStableBoundary(readCurrent, apply);
            if (Cancelled || worldDt == 0f) return;
            ElapsedSec += worldDt;
            var view = readCurrent();
            if (StopIfTerminal(view)) return;
            var caster = UnitAt(view, _casterSlot);

            if (IsCasting)
            {
                if (ElapsedSec < _castDue || caster.ActionLocked || caster.SkillLocked) return;
                // Read masks immediately before the request, after the complete player action chain.
                float multiplier = AreaMultiplier(view);
                IsCasting = false;
                Dispatch(new EncounterRequest { Kind = EncounterRequestKind.AreaAttack, SkillId = _areaSkill,
                    CasterSlot = _casterSlot, DamageMultiplier = multiplier }, apply);
                var completedView = readCurrent();
                bool ended = StopIfTerminal(completedView);
                // Core may catch its own exception and report Failed without throwing through the callback.
                if (completedView.Outcome == BattleOutcome.Failed) return;
                AreaCasts = checked(AreaCasts + 1);
                _lastAreaAt = ElapsedSec;
                if (Cancelled || ended) return;
                ObserveStableBoundary(readCurrent, apply);
                if (Cancelled) return;
                _nextAutoAt = ElapsedSec + _autoInterval;
                _nextIntentAt = ElapsedSec + CurrentInterval;
                return;
            }

            // A simultaneous intent and auto belongs to the intent. Never spend this tick twice on its new cast.
            if (ElapsedSec >= _nextIntentAt)
            {
                IsCasting = true; _castDue = ElapsedSec + _castDuration;
                IntentSerial = checked(IntentSerial + 1);
                return;
            }
            if (ElapsedSec < _nextAutoAt || caster.ActionLocked) return;
            Dispatch(new EncounterRequest { Kind = EncounterRequestKind.AutoAttack, SkillId = _autoSkill,
                CasterSlot = _casterSlot }, apply);
            if (Cancelled || StopIfTerminal(readCurrent())) return;
            ObserveStableBoundary(readCurrent, apply);
            if (!Cancelled) _nextAutoAt = ElapsedSec + _autoInterval;
        }

        /// <summary>Observe only after a native action and its bounded relic chain have both settled.</summary>
        public void ObserveStableBoundary(Func<EncounterView> readCurrent, Action<EncounterRequest> apply)
        {
            RequireCallbacks(readCurrent, apply);
            if (Cancelled) return;
            var view = readCurrent();
            if (StopIfTerminal(view) || _boss == null || Phase == 2) return;
            var boss = UnitAt(view, _casterSlot);
            if (boss.MaxHp <= 0) throw new InvalidOperationException("The encounter caster must have positive maximum HP.");
            if (boss.Hp <= boss.MaxHp * (double)_boss.PhaseTwoHpFraction) PhasePending = true;
            if (!PhasePending || IsCasting || _dispatching) return;

            // Publish phase first so synchronous core boundary callbacks cannot recursively perform it again.
            Phase = 2; PhasePending = false;
            var slots = new List<int>(2); var generations = new List<int>(2);
            foreach (int slot in _boss.MaskSlots)
            {
                var mask = UnitAt(view, slot);
                if (mask != null && mask.Alive) continue;
                slots.Add(slot); generations.Add(mask == null ? 0 : mask.InstanceGeneration);
            }
            var request = new EncounterRequest { Kind = EncounterRequestKind.RebuildMissingMasks, CasterSlot = _casterSlot,
                MaskCharacterId = _boss.MaskCharacterId, MaskSlots = slots.ToArray(), ExpectedGenerations = generations.ToArray() };
            Dispatch(request, apply);
            // Count confirmed new living instances, not attempted rebuilds or surviving masks.
            var after = readCurrent();
            for (int i = 0; i < slots.Count; i++)
            {
                var mask = UnitAt(after, slots[i]);
                if (mask != null && mask.Alive && mask.InstanceGeneration > generations[i])
                    MaskRebuildCount = checked(MaskRebuildCount + 1);
            }
            if (Cancelled || StopIfTerminal(after)) return;
            if (_lastAreaAt >= 0d) _nextIntentAt = Math.Max(ElapsedSec, _lastAreaAt + CurrentInterval);
        }

        public void Cancel()
        {
            Cancelled = true; IsCasting = false; PhasePending = false;
        }

        public EncounterSnapshot Snapshot(EncounterView view) => new EncounterSnapshot
        {
            Stage = Stage, IsBoss = _boss != null, AreaName = _boss != null ? "终幕回响" : "错拍重音",
            Phase = Phase, PhasePending = PhasePending, IsCasting = IsCasting, RemainingCastSec = RemainingCastSec,
            CastDurationSec = _castDuration, NextIntentSec = NextIntentSec, AutoRemainingSec = AutoRemainingSec,
            ElapsedSec = ElapsedSec, AliveMasks = CountAliveMasks(view), AreaMultiplier = AreaMultiplier(view),
            ActionSerial = ActionSerial, IntentSerial = IntentSerial, AreaCasts = AreaCasts,
            MaskRebuildCount = MaskRebuildCount, Cancelled = Cancelled
        };

        double CurrentInterval => _boss == null ? _elite.IntervalSec : Phase == 1 ? _boss.PhaseOneIntervalSec : _boss.PhaseTwoIntervalSec;
        float Remaining(double due) => (float)Math.Max(0d, due - ElapsedSec);
        float AreaMultiplier(EncounterView view) => _boss == null ? 1f : 1f + _boss.DamagePerLivingMask * CountAliveMasks(view);
        int CountAliveMasks(EncounterView view)
        {
            if (_boss == null) return 0;
            int count = 0;
            foreach (int slot in _boss.MaskSlots) if (UnitAt(view, slot)?.Alive == true) count++;
            return count;
        }
        bool StopIfTerminal(EncounterView view)
        {
            if (view == null || view.Enemies == null) throw new ArgumentException("A current encounter view is required.", nameof(view));
            if (view.Outcome != BattleOutcome.InProgress || UnitAt(view, _casterSlot)?.Alive != true)
            { Cancel(); return true; }
            return false;
        }
        static UnitState UnitAt(EncounterView view, int slot)
        {
            if (view?.Enemies == null) return null;
            for (int i = 0; i < view.Enemies.Count; i++)
            {
                var unit = view.Enemies[i];
                if (unit != null && !unit.Ally && unit.Slot == slot) return unit;
            }
            return null;
        }
        void Dispatch(EncounterRequest request, Action<EncounterRequest> apply)
        {
            request.ActionId = ActionSerial = checked(ActionSerial + 1);
            _dispatching = true;
            try { apply(request); }
            catch { Cancel(); throw; }
            finally { _dispatching = false; }
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static void Positive(float value)
        {
            if (!Finite(value) || value <= 0f) throw new ArgumentException("Encounter timings and phase fraction must be finite and positive.");
        }
        static void RequireSkillIds(string autoSkill, string areaSkill)
        {
            if (string.IsNullOrWhiteSpace(autoSkill) || string.IsNullOrWhiteSpace(areaSkill))
                throw new ArgumentException("An encounter requires authored auto and area skills.");
        }
        static void RequireCallbacks(Func<EncounterView> readCurrent, Action<EncounterRequest> apply)
        {
            if (readCurrent == null) throw new ArgumentNullException(nameof(readCurrent));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
        }
    }
}
