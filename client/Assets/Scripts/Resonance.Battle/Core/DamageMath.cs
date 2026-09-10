using System;

namespace Resonance.Battle
{
    public static class DamageMath
    {
        public const string NotMeasuredCode = "NOT_MEASURED";
        public const string DesignPlaceholderCode = "DESIGN_PLACEHOLDER";
        public const float ExtraDmgMulFloor = 0.1f;
        public const float FeverMul = 0.6f;
        public const float FeverHitTapFraction = FeverMul;
        public const float FeverChannelAtkCoef = 1f;
        public const int FeverChannelFlat = 0;
        public const float SkillFlatWeight = 1.25f;
        public const float ElemAdvantage = 1.4f;
        public const float ElemNeutral = 1.0f;
        public const float ElemDisadvantage = 0.7f;
        public const float ElemCritAdd = 1.0f;
        public const float KrTapDefDecay = 0.00008f;
        public const float AutoDefDecay = 0.00005f;

        public static float ElementMultiplier(Element attacker, Element defender)
        {
            if (Beats(attacker, defender)) return ElemAdvantage;
            if (Beats(defender, attacker)) return ElemDisadvantage;
            return ElemNeutral;
        }

        public static float ElemCritMultiplier(Element attacker, Element defender, bool crit)
        {
            var e = ElementMultiplier(attacker, defender);
            return crit ? e + ElemCritAdd : e;
        }

        // Cycle is 火→木→水→火, 光↔暗. Enum order is Fire, Water, Wood — not the cycle.
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

        public static float ClampExtraDmgMul(float extraDmgMul)
        {
            if (extraDmgMul < ExtraDmgMulFloor) extraDmgMul = ExtraDmgMulFloor;
            return extraDmgMul;
        }

        public static float ExtraDmgMul(float extraDmg)
        {
            return ClampExtraDmgMul(1f + extraDmg);
        }

        public static float ExtraDmg(
            SkillType type,
            bool advantage,
            float tsAmp,
            float ssAmp,
            float dsAmp,
            float skillDefDown,
            float weakDefDown)
        {
            var extra = skillDefDown;
            if (advantage) extra += weakDefDown;
            if (type == SkillType.Slide) extra += ssAmp;
            else if (type == SkillType.Drive) extra += dsAmp;
            else if (type == SkillType.Tap) extra += tsAmp;
            return extra;
        }

        public static int SkillDmg(int atk, float coef, int flat, float skillFlat = 0f)
        {
            return (int)Math.Round(atk * (double)coef + flat + skillFlat * SkillFlatWeight, MidpointRounding.AwayFromZero);
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
            extraDmgMul = ClampExtraDmgMul(extraDmgMul);
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
            extraDmgMul = ClampExtraDmgMul(extraDmgMul);
            var numer = (extraAtk * 120.0 + skillDmg * 400.0 + agiTerm) * elemCrit;
            var denom = 0.2 * def + 400.0;
            var core = numer / denom + truePierce;
            var bonusMul = crit ? 2 : 1;
            var d = core * extraDmgMul + bonusHit * bonusMul + enchantPlusCarta;
            var n = (int)Math.Round(d, MidpointRounding.AwayFromZero);
            return n < 1 ? 1 : n;
        }

        public static int ComputeTsPercent(
            int extraAtk,
            int baseAtk,
            float percent,
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
            extraDmgMul = ClampExtraDmgMul(extraDmgMul);
            var numer = (extraAtk + baseAtk) * 125.0 * percent * elemCrit;
            var denom = 0.15 * def + 400.0;
            var core = numer / denom + truePierce;
            var bonusMul = crit ? 2 : 1;
            var d = core * extraDmgMul * feverMul + bonusHit * bonusMul + enchantPlusCarta;
            var n = (int)Math.Round(d, MidpointRounding.AwayFromZero);
            return n < 1 ? 1 : n;
        }

        public static int ComputeSsPercent(
            int extraAtk,
            int baseAtk,
            float percent,
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
            extraDmgMul = ClampExtraDmgMul(extraDmgMul);
            var numer = (extraAtk + baseAtk + agiTerm) * 120.0 * percent * elemCrit;
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
            return ComputeSkill(SkillType.Tap, atk, coef, flat, defense, atkEl, defEl, crit, buffMul, variance, 1f);
        }

        public static int ComputeSkill(
            SkillType type,
            int atk,
            float coef,
            int flat,
            int defense,
            Element atkEl,
            Element defEl,
            bool crit,
            float extraDmgMul,
            float variance,
            float feverMul = 1f,
            float percentAtk = 0f,
            float skillFlat = 0f,
            int extraAtk = 0,
            int pierce = 0,
            int bonusHit = 0,
            int enchantPlusCarta = 0,
            int agiTerm = 0)
        {
            var elemCrit = ElemCritMultiplier(atkEl, defEl, crit);
            if (type == SkillType.Slide)
                variance = 1f;
            extraDmgMul = ClampExtraDmgMul(extraDmgMul * variance);
            var truePierce = TruePierce(pierce, defense);
            if (percentAtk > 0f && type != SkillType.Drive && type != SkillType.Auto && type != SkillType.Fever)
            {
                if (type == SkillType.Slide)
                    return ComputeSsPercent(extraAtk, atk, percentAtk, defense, elemCrit, extraDmgMul, truePierce, bonusHit, crit, enchantPlusCarta, agiTerm);
                return ComputeTsPercent(extraAtk, atk, percentAtk, defense, elemCrit, extraDmgMul, feverMul, truePierce, bonusHit, crit, enchantPlusCarta);
            }

            var skillDmg = SkillDmg(atk, coef, flat, skillFlat);
            if (type == SkillType.Auto)
                return ComputeAuto(extraAtk, atk, skillDmg, defense, elemCrit, extraDmgMul, truePierce, bonusHit, crit, enchantPlusCarta);
            if (type == SkillType.Slide)
                return ComputeSs(extraAtk, skillDmg, defense, elemCrit, extraDmgMul, truePierce, bonusHit, crit, enchantPlusCarta, agiTerm);
            if (type == SkillType.Drive)
                return ComputeDs(extraAtk, skillDmg, defense, elemCrit, extraDmgMul, truePierce, bonusHit, crit, enchantPlusCarta);
            if (type == SkillType.Fever)
                return ComputeFever(extraAtk, skillDmg, defense, elemCrit, extraDmgMul, truePierce, bonusHit, crit, enchantPlusCarta, feverMul);
            return ComputeTs(extraAtk, skillDmg, defense, elemCrit, extraDmgMul, feverMul, truePierce, bonusHit, crit, enchantPlusCarta);
        }

        public static int ComputeAuto(
            int extraAtk,
            int baseAtk,
            int skillDmg,
            int def,
            float elemCrit,
            float extraDmgMul,
            int truePierce,
            int bonusHit,
            bool crit,
            int enchantPlusCarta)
        {
            if (def < 0) def = 0;
            extraDmgMul = ClampExtraDmgMul(extraDmgMul);
            var aTotal = extraAtk + baseAtk;
            var core = 20.0 * aTotal * elemCrit / (0.4 * def + 500.0) + skillDmg - 20.0 * baseAtk / 500.0;
            core *= Math.Max(0.6, 1.0 - AutoDefDecay * def);
            core += truePierce;
            var bonusMul = crit ? 2 : 1;
            var d = core * extraDmgMul + bonusHit * bonusMul + enchantPlusCarta;
            var n = (int)Math.Round(d, MidpointRounding.AwayFromZero);
            return n < 1 ? 1 : n;
        }

        public static FormulaResult Resolve(
            FormulaProfile profile,
            SkillType type,
            int atk,
            float coef,
            int flat,
            int defense,
            Element atkEl,
            Element defEl,
            bool crit,
            float extraDmgMul,
            float variance,
            float feverMul = 1f,
            float percentAtk = 0f,
            float skillFlat = 0f,
            int extraAtk = 0,
            int pierce = 0,
            int bonusHit = 0,
            int enchantPlusCarta = 0,
            int agiTerm = 0)
        {
            if (profile == FormulaProfile.GL_UNKNOWN)
                return FormulaResult.NotMeasured(profile);
            if (profile != FormulaProfile.JP_LEGACY_EMPIRICAL && profile != FormulaProfile.KR_LEGACY_REPORTED)
                return FormulaResult.NotMeasured(FormulaProfile.GL_UNKNOWN);

            var value = ComputeSkill(
                type, atk, coef, flat, defense, atkEl, defEl, crit, extraDmgMul, variance,
                feverMul, percentAtk, skillFlat, extraAtk, pierce, bonusHit, enchantPlusCarta, agiTerm);
            if (profile == FormulaProfile.KR_LEGACY_REPORTED && type == SkillType.Tap)
            {
                if (defense < 0) defense = 0;
                var decay = Math.Max(0.6, 1.0 - KrTapDefDecay * defense);
                value = (int)Math.Round(value * decay, MidpointRounding.AwayFromZero);
                if (value < 1) value = 1;
            }
            return FormulaResult.Ok(profile, value);
        }

        public static string ChannelOpcode(SkillType type)
        {
            if (type == SkillType.Auto) return EffectOpcodes.DmgAuto;
            if (type == SkillType.Slide) return EffectOpcodes.DmgSlide;
            if (type == SkillType.Drive) return EffectOpcodes.DmgDriveActual;
            if (type == SkillType.Fever) return EffectOpcodes.DmgFeverParts;
            if (type == SkillType.Leader) return EffectOpcodes.StatusApply;
            return EffectOpcodes.DmgTap;
        }

        public static int ComputeFever(
            int extraAtk,
            int skillDmg,
            int def,
            float elemCrit,
            float extraDmgMul,
            int truePierce,
            int bonusHit,
            bool crit,
            int enchantPlusCarta,
            float feverMul = FeverMul)
        {
            if (feverMul <= 0f) feverMul = FeverMul;
            return ComputeTs(extraAtk, skillDmg, def, elemCrit, extraDmgMul, feverMul, truePierce, bonusHit, crit, enchantPlusCarta);
        }

        public static int ComputeDs(
            int extraAtk,
            int skillDmg,
            int def,
            float elemCrit,
            float extraDmgMul,
            int truePierce,
            int bonusHit,
            bool crit,
            int enchantPlusCarta)
        {
            if (def < 0) def = 0;
            extraDmgMul = ClampExtraDmgMul(extraDmgMul);
            var numer = (extraAtk * 130.0 + skillDmg * 400.0) * elemCrit;
            var denom = 0.12 * def + 400.0;
            var core = numer / denom + truePierce;
            var bonusMul = crit ? 2 : 1;
            var d = core * extraDmgMul + bonusHit * bonusMul + enchantPlusCarta;
            var n = (int)Math.Round(d, MidpointRounding.AwayFromZero);
            return n < 1 ? 1 : n;
        }
    }
}
