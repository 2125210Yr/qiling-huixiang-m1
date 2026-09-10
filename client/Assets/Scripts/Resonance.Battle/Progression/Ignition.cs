using System;

namespace Resonance.Battle
{
    // Red ATK/CRT/AGL multipliers for the damage formula.
    // Atk → extraAtk*(1+Atk)+baseAtk*Atk. Crt/Agl add into extraDmgMul (crit / weakness).
    public struct IgnitionBonus
    {
        public float Atk;
        public float Crt;
        public float Agl;

        public int ExtraAtk(int extraAtk, int baseAtk)
        {
            var v = extraAtk * (1.0 + Atk) + baseAtk * Atk;
            return (int)Math.Round(v, MidpointRounding.AwayFromZero);
        }

        public float ExtraDmgAdd(bool crit, bool advantage)
        {
            var add = 0f;
            if (crit) add += Crt;
            if (advantage) add += Agl;
            return add;
        }
    }

    public static class Ignition
    {
        public const int StoneCap = 12;
        public const int MainRed = 400;
        public const int ExtraRed = 100;
        public const float AtkPerRed = 0.0015f;
        public const float CrtPerRed = 0.003f;
        public const float AglPerRed = 0.002f;

        public static int Clamp(int stones)
        {
            if (stones < 0) return 0;
            return stones > StoneCap ? StoneCap : stones;
        }

        public static IgnitionBonus Of(int atkStones, int crtStones, int aglStones)
        {
            return new IgnitionBonus
            {
                Atk = AtkPerRed * Red(atkStones),
                Crt = CrtPerRed * Red(crtStones),
                Agl = AglPerRed * Red(aglStones)
            };
        }

        // First stone of a color is the 400-red main core; extras add 100. Not SQL trees.
        static int Red(int stones)
        {
            stones = Clamp(stones);
            if (stones == 0) return 0;
            return MainRed + ExtraRed * (stones - 1);
        }
    }
}
