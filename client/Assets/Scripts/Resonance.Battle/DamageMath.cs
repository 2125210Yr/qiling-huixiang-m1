using System;

namespace Resonance.Battle
{
    public static class DamageMath
    {
        public const float FeverHitTapFraction = 0.60f;

        public static float ElementMultiplier(Element attacker, Element defender)
        {
            if (Beats(attacker, defender)) return 1.4f;
            if (Beats(defender, attacker)) return 0.7f;
            return 1.00f;
        }

        public static float ElemCritMultiplier(Element attacker, Element defender, bool crit)
        {
            var e = ElementMultiplier(attacker, defender);
            return crit ? e + 1f : e;
        }

        public static bool Beats(Element a, Element b)
        {
            return (a == Element.Fire && b == Element.Wood)
                || (a == Element.Wood && b == Element.Water)
                || (a == Element.Water && b == Element.Fire)
                || (a == Element.Light && b == Element.Dark)
                || (a == Element.Dark && b == Element.Light);
        }

        public static float CritChance(int crt)
        {
            if (crt <= 0) return 0f;
            var p = crt / (crt + 4000f);
            if (p > 0.55f) p = 0.55f;
            return p;
        }

        public static int ComputeTs(
            int extraAtk,
            int skillDmg,
            int def,
            float elemCrit,
            float extraDmgMul,
            float feverMul,
            int truePierce,
            int bonusHit,
            bool crit,
            int enchantPlusCarta)
        {
            if (def < 0) def = 0;
            if (extraDmgMul < 0.1f) extraDmgMul = 0.1f;
            var numer = (extraAtk * 125.0 + skillDmg * 400.0) * elemCrit;
            var denom = 0.15 * def + 400.0;
            var core = numer / denom + truePierce;
            var bonusMul = crit ? 2 : 1;
            var d = core * extraDmgMul * feverMul + bonusHit * bonusMul + enchantPlusCarta;
            var n = (int)Math.Round(d, MidpointRounding.AwayFromZero);
            return n < 1 ? 1 : n;
        }

        public static int ComputeSs(
            int extraAtk,
            int skillDmg,
            int def,
            float elemCrit,
            float extraDmgMul,
            int truePierce,
            int bonusHit,
            bool crit,
            int enchantPlusCarta,
            int agiTerm = 0)
        {
            if (def < 0) def = 0;
            if (extraDmgMul < 0.1f) extraDmgMul = 0.1f;
            var numer = (extraAtk * 120.0 + skillDmg * 400.0 + agiTerm) * elemCrit;
            var denom = 0.2 * def + 400.0;
            var core = numer / denom + truePierce;
            var bonusMul = crit ? 2 : 1;
            var d = core * extraDmgMul + bonusHit * bonusMul + enchantPlusCarta;
            var n = (int)Math.Round(d, MidpointRounding.AwayFromZero);
            return n < 1 ? 1 : n;
        }

        public static int TruePierce(int pierce, int def)
        {
            if (pierce <= 0) return 0;
            if (def < 0) def = 0;
            var v = pierce * 0.6 + pierce * 0.4 * def / 20000.0;
            return (int)Math.Round(v, MidpointRounding.AwayFromZero);
        }

        public static int Compute(
            int atk,
            float coef,
            int flat,
            int defense,
            Element atkEl,
            Element defEl,
            bool crit,
            float buffMul,
            float variance)
        {
            var skillDmg = (int)Math.Round(atk * coef + flat, MidpointRounding.AwayFromZero);
            var elemCrit = ElemCritMultiplier(atkEl, defEl, crit);
            var n = ComputeTs(0, skillDmg, defense, elemCrit, buffMul * variance, 1f, 0, 0, crit, 0);
            return n < 1 ? 1 : n;
        }
    }
}
