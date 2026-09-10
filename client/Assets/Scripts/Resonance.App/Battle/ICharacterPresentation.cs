using Resonance.Battle;
using UnityEngine;

namespace Resonance.App
{
    public enum PresentationCueKind
    {
        Cast = 0,
        Hit = 1,
        Death = 2,
        Buff = 3,
        Fever = 4,
        Judge = 5
    }

    public readonly struct PresentationCue
    {
        public readonly PresentationCueKind Kind;
        public readonly int Slot;
        public readonly bool Ally;
        public readonly int CasterSlot;
        public readonly bool CasterAlly;
        public readonly SkillType Channel;
        public readonly int Amount;
        public readonly bool Crit;
        public readonly bool Heal;
        public readonly bool Fever;
        public readonly string Label;
        public readonly Element Elem;
        public readonly Vector2 From;
        public readonly Vector2 To;

        public PresentationCue(
            PresentationCueKind kind,
            int slot,
            bool ally,
            int casterSlot,
            bool casterAlly,
            SkillType channel,
            int amount,
            bool crit,
            bool heal,
            bool fever,
            string label,
            Element elem,
            Vector2 from,
            Vector2 to)
        {
            Kind = kind;
            Slot = slot;
            Ally = ally;
            CasterSlot = casterSlot;
            CasterAlly = casterAlly;
            Channel = channel;
            Amount = amount;
            Crit = crit;
            Heal = heal;
            Fever = fever;
            Label = label;
            Elem = elem;
            From = from;
            To = to;
        }
    }

    public interface ICharacterPresentation
    {
        void PlayCue(PresentationCue cue);
    }
}
