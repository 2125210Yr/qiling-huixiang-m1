using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    public sealed partial class BattleSim
    {
        readonly ExpeditionBattleInput _expeditionInput;
        public bool IsOriginalExpedition => _expeditionInput != null;
        public ExpeditionBattleInput OpeningExpeditionInput => _expeditionInput?.DeepClone();
        readonly List<ResolutionResult> _expeditionResolutions = new List<ResolutionResult>();
        public IReadOnlyList<ResolutionResult> ExpeditionResolutions => _expeditionResolutions.AsReadOnly();
        public ExpeditionRelicRuntime ExpeditionRelics { get; private set; }
        public ResolutionLedger ExpeditionTotals { get; private set; }
        public long OriginalRootActionId => _originalRootActionId;
        long _originalRootActionId;
        RelicNativeAction _originalAction;
        RelicRequest _originalRequest;
        StatusInst _originalDotSource;

        void InitializeOriginalRuntime()
        {
            if (!IsOriginalExpedition) return;
            var max = new int[Allies.Length];
            for (int i = 0; i < Allies.Length; i++) { max[i] = Allies[i].MaxHp; Allies[i].InstanceGeneration = 1; }
            foreach (var enemy in Enemies) enemy.InstanceGeneration = 1;
            ExpeditionRelics = new ExpeditionRelicRuntime(_expeditionInput, max);
            ExpeditionTotals = new ResolutionLedger();
            InitializeOriginalEncounter();
        }

        // The legacy and original modes share CastCore and the same damage/heal/shield mutation paths.
        void Cast(UnitState caster, bool casterAlly, SkillDef skill, float dmgMul)
        {
            if (!IsOriginalExpedition) { CastCore(caster, casterAlly, skill, dmgMul); return; }
            if (Outcome != BattleOutcome.InProgress) return;
            try
            {
                SettleOriginalBoundary();
                if (Outcome != BattleOutcome.InProgress) return;
                if (_originalAction != null) throw new RelicRuleException("Nested native cast is not supported.");
                if (skill != null && FightSkillReady(skill))
                    _originalAction = ExpeditionRelics.BeginNativeAction(checked(++_originalRootActionId), caster, skill);
                CastCore(caster, casterAlly, skill, dmgMul);
                if (Outcome != BattleOutcome.Failed && _originalAction != null)
                    ExpeditionRelics.CompleteNativeAction(_originalAction, OriginalRelicView, ApplyOriginalRelicRequest);
                SettleOriginalBoundary();
            }
            catch (Exception error) { FailOriginalRule(error); }
            finally { _originalAction = null; _originalRequest = null; }
        }

        RelicBattleView OriginalRelicView() => new RelicBattleView
        {
            Allies = Allies, Enemies = Enemies, FocusEnemySlot = FocusEnemySlot,
            StopDerivedDamage = Outcome == BattleOutcome.Failed || OriginalBossDead(),
            GenerationProvider = unit => unit.InstanceGeneration
        };

        bool OriginalBossDead() => _expeditionInput.IsBoss && Enemies.Count > _expeditionInput.Boss.BossSlot &&
            !Enemies[_expeditionInput.Boss.BossSlot].Alive;

        void ApplyOriginalRelicRequest(RelicRequest request)
        {
            if (request == null || Outcome == BattleOutcome.Failed) return;
            var target = UnitAt(request.TargetAlly, request.TargetSlot);
            if (target == null || !target.Alive || !ReferenceEquals(target, request.Target) ||
                target.InstanceGeneration != request.TargetGeneration) return;
            if (request.Kind == RelicRequestKind.Charge)
            {
                if (float.IsNaN(request.Charge) || float.IsInfinity(request.Charge) || request.Charge < 0)
                    throw new RelicRuleException("Invalid derived charge.");
                target.Charge = Math.Min(100f, target.Charge + request.Charge);
                return;
            }
            if (OriginalBossDead()) return;
            if (request.Damage < 0) throw new RelicRuleException("Negative derived damage.");
            _originalRequest = request;
            try { ApplyDamage(UnitAt(request.SourceAlly, request.SourceSlot), target, request.Damage, false); }
            finally { _originalRequest = null; }
        }

        void RecordOriginalResult(UnitState source, UnitState target, int requestedDamage = 0, int absorbed = 0,
            int hpDamage = 0, int overkill = 0, int requestedHeal = 0, int effectiveHeal = 0, int shieldProduced = 0, bool killed = false)
        {
            if (!IsOriginalExpedition) return;
            var derived = _originalRequest;
            var dot = _originalDotSource;
            var origin = derived != null ? ResolutionOrigin.Derived : dot != null || _poisonResolving ? ResolutionOrigin.Dot : ResolutionOrigin.Native;
            var sourceSlot = derived != null ? derived.SourceSlot : dot != null ? dot.SourceSlot : source != null ? source.Slot : _originalAction?.SourceSlot ?? -1;
            var sourceAlly = derived != null ? derived.SourceAlly : dot != null ? dot.SourceAlly : source != null ? source.Ally : _originalAction?.SourceAlly ?? false;
            var result = new ResolutionResult(origin, derived?.RootActionId ?? _originalAction?.RootActionId ?? checked(++_originalRootActionId),
                derived != null ? 1 : 0, derived?.SourceRelicId, derived?.SkillId ?? dot?.SourceSkillId ?? _originalAction?.SkillId,
                sourceSlot, sourceAlly, target.Slot, target.Ally, target.InstanceGeneration,
                requestedDamage: requestedDamage, shieldAbsorbed: absorbed, effectiveHpDamage: hpDamage, overkill: overkill,
                requestedHeal: requestedHeal, effectiveHeal: effectiveHeal, overheal: requestedHeal - effectiveHeal,
                shieldProduced: shieldProduced, killed: killed);
            ExpeditionTotals.Add(result);
            _expeditionResolutions.Add(result);
            if (_originalAction != null) ExpeditionRelics.ObserveResolution(_originalAction, result);
        }

        void FailOriginalRule(Exception error)
        {
            Outcome = BattleOutcome.Failed;
            FailedReason = "ORIGINAL_RULE_ERROR " + error.GetType().Name + ": " + error.Message;
            LastEvent = FailedReason;
            OriginalEncounter?.Cancel();
            NoteEvent("fail", "original.rule", null, null, 0, _activeKind);
        }

        void SettleOriginalBoundary()
        {
            if (!IsOriginalExpedition) return;
            if (Outcome != BattleOutcome.InProgress) { OriginalEncounter?.Cancel(); return; }
            bool anyEnemy = false;
            foreach (var enemy in Enemies) if (enemy.Alive) { anyEnemy = true; break; }
            if (OriginalBossDead() || !anyEnemy)
            {
                SweepDeaths(); Outcome = BattleOutcome.Victory; LastEvent = "胜利"; NoteResult("clear");
                ObserveOriginalEncounterBoundary();
                return;
            }
            AnyAllyAlive();
            ObserveOriginalEncounterBoundary();
        }

        float OriginalChannelBonus() => IsOriginalExpedition && _originalAction != null && _originalRequest == null && !_poisonResolving
            ? ExpeditionRelics.DamageChannelBonus(_originalAction) : 0f;

        int OriginalAttack(UnitState unit) => unit == null ? 0 : ResolutionMath.ToBattleInt(
            unit.Def.Atk * (1d + unit.Magnitude(EffectKind.AtkBuff)));

        int OriginalDefense(UnitState unit) => unit == null ? 0 : ResolutionMath.ToBattleInt(
            unit.Def.Def * (1d + unit.Magnitude(EffectKind.DefBuff)) * (1d - unit.Magnitude(EffectKind.DefDebuff)));

        // A conservative pre-mitigation envelope prevents the legacy int formula helpers from wrapping.
        // Values outside this supported domain fail explicitly; the actual formula still runs unchanged.
        void GuardOriginalFormula(int atk, int extraAtk, float coef, int flat, float skillFlat, float percentAtk,
            float elemCrit, float extraMul, float feverMul)
        {
            if (!IsOriginalExpedition) return;
            if (atk < 0 || extraAtk < 0 || coef < 0 || flat < 0 || skillFlat < 0 || percentAtk < 0)
                throw new RelicRuleException("Negative original formula input.");
            var rawSkill = ResolutionMath.ToBattleInt(atk * (double)coef + flat + skillFlat * DamageMath.SkillFlatWeight);
            var totalAtk = ResolutionMath.ToBattleInt((double)atk + extraAtk);
            ResolutionMath.ToBattleInt((rawSkill + totalAtk * Math.Max(1d, percentAtk)) * Math.Max(1d, elemCrit) *
                Math.Max(1d, extraMul) * Math.Max(1d, feverMul));
        }

        CharacterDef ResolveCharacter(string id)
        {
            if (!IsOriginalExpedition) return Catalog.TryChar(id);
            foreach (var character in _expeditionInput.Characters)
                if (character.Id == id) return character;
            throw new InvalidOperationException("Unknown original character: " + id);
        }
    }
}
