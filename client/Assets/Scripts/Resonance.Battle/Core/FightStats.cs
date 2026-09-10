using System;

namespace Resonance.Battle
{
    /// <summary>
    /// Per-fight damage / heal ledger. Elapsed is sim time (respects Speed).
    /// TotalDealt is HP damage to enemies after shield absorb.
    /// PartyCap follows the spawned party; DefaultPartyCap is the M1 slice default only.
    /// </summary>
    public sealed class FightStats
    {
        public const int DefaultPartyCap = 5;

        public readonly int PartyCap;
        public float Elapsed;
        public int TotalDealt;
        public int TotalTaken;
        public int TotalHeal;
        public int Hits;
        public readonly int[] AllyDealt;
        public readonly int[] AllyHeal;
        public readonly int[] AllyHits;

        public static int CapForLength(int partyLength)
        {
            return partyLength > 0 ? partyLength : DefaultPartyCap;
        }

        public FightStats(int partyCap = DefaultPartyCap)
        {
            PartyCap = partyCap > 0 ? partyCap : DefaultPartyCap;
            AllyDealt = new int[PartyCap];
            AllyHeal = new int[PartyCap];
            AllyHits = new int[PartyCap];
        }

        public int Dps => Rate(TotalDealt);

        public int AllyDps(int slot)
        {
            if (slot < 0 || slot >= PartyCap) return 0;
            return Rate(AllyDealt[slot]);
        }

        public void Tick(float dt)
        {
            if (dt > 0f) Elapsed += dt;
        }

        public void NoteDamage(UnitState caster, UnitState target, int dmg)
        {
            if (dmg <= 0 || target == null) return;
            if (!target.Ally)
            {
                TotalDealt += dmg;
                Hits++;
                if (caster != null && caster.Ally && caster.Slot >= 0 && caster.Slot < PartyCap)
                {
                    AllyDealt[caster.Slot] += dmg;
                    AllyHits[caster.Slot]++;
                }
            }
            else
            {
                TotalTaken += dmg;
            }
        }

        public void NoteHeal(UnitState caster, UnitState target, int amt)
        {
            if (amt <= 0 || target == null || !target.Ally) return;
            TotalHeal += amt;
            if (caster != null && caster.Ally && caster.Slot >= 0 && caster.Slot < PartyCap)
                AllyHeal[caster.Slot] += amt;
        }

        int Rate(int total)
        {
            if (Elapsed < 0.25f) return 0;
            return (int)Math.Round(total / Elapsed);
        }
    }
}
