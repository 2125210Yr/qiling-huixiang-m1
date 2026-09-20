using System;

namespace Resonance.Battle
{
    public sealed partial class BattleSim
    {
        public BossEncounterController OriginalEncounter { get; private set; }
        public EncounterSnapshot OriginalIntentSnapshot => OriginalEncounter?.Snapshot(OriginalEncounterView());

        EncounterView OriginalEncounterView() => new EncounterView { Enemies = Enemies, Outcome = Outcome };

        void InitializeOriginalEncounter()
        {
            if (!IsOriginalExpedition) return;
            if (_expeditionInput.IsBoss) OriginalEncounter = new BossEncounterController(_expeditionInput.Boss);
            else if (_expeditionInput.Elite != null) OriginalEncounter = new BossEncounterController(_expeditionInput.Elite);
        }

        void AdvanceOriginalEncounter(float worldDt)
        {
            OriginalEncounter?.Advance(worldDt, OriginalEncounterView, ApplyOriginalEncounterRequest);
        }

        void ObserveOriginalEncounterBoundary()
        {
            if (OriginalEncounter == null) return;
            if (Outcome != BattleOutcome.InProgress) { OriginalEncounter.Cancel(); return; }
            if (_originalAction == null || _originalAction.Completed)
                OriginalEncounter.ObserveStableBoundary(OriginalEncounterView, ApplyOriginalEncounterRequest);
        }

        void ApplyOriginalEncounterRequest(EncounterRequest request)
        {
            if (request == null || Outcome != BattleOutcome.InProgress || OriginalBossDead()) return;
            if (request.Kind == EncounterRequestKind.RebuildMissingMasks)
            {
                if (!_expeditionInput.IsBoss || request.MaskSlots == null || request.ExpectedGenerations == null ||
                    request.MaskSlots.Length != request.ExpectedGenerations.Length || request.MaskSlots.Length > 2)
                    throw new RelicRuleException("Invalid fixed-mask rebuild request.");
                for (int i = 0; i < request.MaskSlots.Length; i++)
                {
                    int slot = request.MaskSlots[i];
                    if (slot != 1 && slot != 2) throw new RelicRuleException("Mask slot outside fixed encounter.");
                    if (slot >= Enemies.Count) throw new RelicRuleException("Missing fixed encounter slot.");
                    var old = UnitAt(false, slot);
                    int generation = old?.InstanceGeneration ?? 0;
                    if (generation != request.ExpectedGenerations[i] || (old != null && old.Alive)) continue;
                    var replacement = Spawn(Growth.ScaleEnemy(ResolveCharacter(request.MaskCharacterId), _stage), slot, false);
                    replacement.InstanceGeneration = checked(generation + 1);
                    Enemies[slot] = replacement;
                    NoteEvent("encounter", "mask.rebuilt", null, replacement, replacement.InstanceGeneration, SkillType.Auto);
                }
                return;
            }
            var caster = UnitAt(false, request.CasterSlot);
            if (caster == null || !caster.Alive) return;
            var skill = ResolveSkill(request.SkillId);
            if (skill == null || !FightSkillReady(skill)) throw new RelicRuleException("Unplayable scripted encounter action.");
            if (float.IsNaN(request.DamageMultiplier) || float.IsInfinity(request.DamageMultiplier) || request.DamageMultiplier < 0f)
                throw new RelicRuleException("Invalid encounter multiplier.");
            NoteEvent("encounter", request.Kind == EncounterRequestKind.AreaAttack ? "area.resolve" : "auto.resolve",
                caster, null, 0, skill.Type);
            Cast(caster, false, skill, request.DamageMultiplier);
        }
    }
}
