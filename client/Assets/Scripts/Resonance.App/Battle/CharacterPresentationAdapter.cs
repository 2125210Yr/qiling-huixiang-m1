using Resonance.Battle;
using UnityEngine;

namespace Resonance.App
{
    public sealed class CharacterPresentationAdapter : ICharacterPresentation
    {
        readonly Transform _root;
        readonly BattleFighter[] _allies;
        readonly BattleFighter[] _enemies;

        public CharacterPresentationAdapter(Transform root, BattleFighter[] allies, BattleFighter[] enemies)
        {
            _root = root;
            _allies = allies;
            _enemies = enemies;
        }

        public void PlayCue(PresentationCue cue)
        {
            var target = Find(cue.Slot, cue.Ally);
            var caster = Find(cue.CasterSlot, cue.CasterAlly);
            switch (cue.Kind)
            {
                case PresentationCueKind.Cast:
                    if (caster != null) caster.PlayCast(cue.Channel, cue.Fever);
                    CombatFeel.PlayCue(cue, _root, caster);
                    VfxRouter.PlayCue(cue, _root, caster, target);
                    break;
                case PresentationCueKind.Hit:
                    if (target != null) target.Hit();
                    CombatFeel.PlayCue(cue, _root, target);
                    VfxRouter.PlayCue(cue, _root, caster, target);
                    break;
                case PresentationCueKind.Death:
                    VfxRouter.PlayCue(cue, _root, caster, target);
                    break;
                case PresentationCueKind.Buff:
                    VfxRouter.PlayCue(cue, _root, caster, target);
                    break;
                case PresentationCueKind.Fever:
                    CombatFeel.PlayCue(cue, _root, caster);
                    VfxRouter.PlayCue(cue, _root, caster, target);
                    break;
                case PresentationCueKind.Judge:
                    VfxRouter.PlayCue(cue, _root, caster, target);
                    break;
            }
        }

        BattleFighter Find(int slot, bool ally)
        {
            var list = ally ? _allies : _enemies;
            if (list == null || slot < 0 || slot >= list.Length) return null;
            return list[slot];
        }
    }
}
