using System;

namespace Resonance.Battle
{
    public static class DamageMath
    {
        public const string NotMeasuredCode = "NOT_MEASURED";
        public const string DesignPlaceholderCode = "DESIGN_PLACEHOLDER";
        public const string OutOfCandidateDomainCode = "OUT_OF_CANDIDATE_DOMAIN";
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
        // KR_LEGACY_REPORTED second defense decay (HistoricalCandidate, REPORTED class):
        // DC_RECON_KIT/02_NUMERICS_AND_CALIBRATION.md §C.1/§C.2, tools/candidate_formulas.py tap_base/slide_bounds [S07].
        //   Tap:   * max(0.6, 1 - 0.00008*D)
        //   Slide: * max(0.7, 1 - 0.00005*D)   (distinct floor and slope; not the Tap factor)
        public const float KrTapDefDecay = 0.00008f;
        public const double KrTapDecayFloor = 0.6;
        public const float KrSlideDefDecay = 0.00005f;
        public const double KrSlideDecayFloor = 0.7;
        public const float AutoDefDecay = 0.00005f;
        /// <summary>
        /// Declared DEF domain of the historical pierce candidate P_eff = P * (0.6 + 0.4*D/20000)
        /// (02_NUMERICS §C.3, FormulaProfiles.md, candidate_formulas.py piercing_unmodified: "域 0–20000").
        /// HistoricalCandidate bound, not a design choice; outside it Resolve refuses to extrapolate.
        /// </summary>
        public const int PierceCandidateDefenseMax = 20000;

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

        /// <summary>
        /// Raw S08 pierce helper. Does not refuse out-of-domain DEF; <see cref="Resolve"/> is the
        /// evidence-aware gate and returns <see cref="OutOfCandidateDomainCode"/> when DEF exceeds
        /// <see cref="PierceCandidateDefenseMax"/> with a nonzero pierce term.
        /// </summary>
        public static int TruePierce(int pierce, int def)
        {
            if (pierce <= 0) return 0;
            if (def < 0) def = 0;
            var v = pierce * 0.6 + pierce * 0.4 * def / (double)PierceCandidateDefenseMax;
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

            // P03: the pierce candidate is only declared on DEF 0..20000. Negative DEF is clamped to 0 by every
            // channel (engine convention); above the max we refuse rather than extrapolate the 0.6+0.4*D/20000 fit.
            if (!PierceInCandidateDomain(pierce, defense))
                return FormulaResult.OutOfCandidateDomain(profile);

            var value = ComputeSkill(
                type, atk, coef, flat, defense, atkEl, defEl, crit, extraDmgMul, variance,
                feverMul, percentAtk, skillFlat, extraAtk, pierce, bonusHit, enchantPlusCarta, agiTerm);
            if (profile == FormulaProfile.KR_LEGACY_REPORTED && (type == SkillType.Tap || type == SkillType.Slide))
            {
                // Engine convention (unchanged from the original KR Tap branch): round the channel value first,
                // then apply the reported KR decay and round again. The Python reference is unrounded.
                var decay = KrLegacyDecay(type, defense);
                value = (int)Math.Round(value * decay, MidpointRounding.AwayFromZero);
                if (value < 1) value = 1;
            }
            var evidence = EvidenceOf(profile, type);
            // The percent-ATK Tap/Slide variants (ComputeTsPercent/ComputeSsPercent) are not among the
            // historical candidates in 02_NUMERICS §C; a number is still produced, but only as a design placeholder.
            if (percentAtk > 0f && (type == SkillType.Tap || type == SkillType.Slide) && evidence == FormulaEvidence.HistoricalCandidate)
                evidence = FormulaEvidence.DesignPlaceholder;
            return FormulaResult.Ok(profile, value, evidence);
        }

        /// <summary>
        /// KR_LEGACY_REPORTED second defense decay per channel. Decision (G2 P02): the original plan
        /// (02_NUMERICS §C.2, candidate_formulas.py slide_bounds) declares a KR Slide factor
        /// max(0.7, 1-0.00005*D) that the engine previously omitted while applying the Tap factor
        /// max(0.6, 1-0.00008*D). Both are now applied from their own constants; the Slide factor is
        /// NOT the Tap factor. Other channels declare no KR-specific decay and return 1.
        /// </summary>
        public static double KrLegacyDecay(SkillType type, int defense)
        {
            if (defense < 0) defense = 0;
            if (type == SkillType.Tap)
                return Math.Max(KrTapDecayFloor, 1.0 - KrTapDefDecay * defense);
            if (type == SkillType.Slide)
                return Math.Max(KrSlideDecayFloor, 1.0 - KrSlideDefDecay * defense);
            return 1.0;
        }

        /// <summary>
        /// True when the pierce candidate may be evaluated: either no pierce is requested, or DEF is within the
        /// declared 0..<see cref="PierceCandidateDefenseMax"/> domain (negative DEF is clamped to 0 by the channels).
        /// </summary>
        public static bool PierceInCandidateDomain(int pierce, int defense)
        {
            if (pierce <= 0) return true;
            return defense <= PierceCandidateDefenseMax;
        }

        /// <summary>
        /// Evidence class of the (profile, channel) branch that <see cref="Resolve"/> executes.
        /// Nothing returns <see cref="FormulaEvidence.Measured"/>: no branch has been measured against the GL original.
        /// </summary>
        public static FormulaEvidence EvidenceOf(FormulaProfile profile, SkillType type)
        {
            if (profile != FormulaProfile.JP_LEGACY_EMPIRICAL && profile != FormulaProfile.KR_LEGACY_REPORTED)
                return FormulaEvidence.Unknown;
            switch (type)
            {
                case SkillType.Tap:    // 02_NUMERICS §C.1 [S05][S07]; KR decay [S07]
                case SkillType.Slide:  // 02_NUMERICS §C.2 [S06][S07] (bounds candidate; engine evaluates the r=agiTerm point)
                case SkillType.Auto:   // 02_NUMERICS §C.5 [S05][S07] candidate structure
                    return FormulaEvidence.HistoricalCandidate;
                case SkillType.Drive:  // 02_NUMERICS §C.6: only a UI prediction exists; ComputeDs coefficients are ours
                case SkillType.Fever:  // 0.6 channel structure is §C.4, but FeverChannelAtkCoef/Flat/FeverMul are design constants
                default:               // Leader and anything else: no damage candidate declared
                    return FormulaEvidence.DesignPlaceholder;
            }
        }

        /// <summary>Short human-readable provenance of the branch, for logs and audit reports.</summary>
        public static string BranchSource(FormulaProfile profile, SkillType type)
        {
            if (profile == FormulaProfile.GL_UNKNOWN)
                return "GL original: unknown; strict path produces no value (02_NUMERICS §A, FormulaProfiles.md)";
            if (profile != FormulaProfile.JP_LEGACY_EMPIRICAL && profile != FormulaProfile.KR_LEGACY_REPORTED)
                return "Unregistered profile; treated as GL_UNKNOWN";

            var kr = profile == FormulaProfile.KR_LEGACY_REPORTED;
            var region = kr ? "KR legacy reported" : "JP legacy empirical";
            switch (type)
            {
                case SkillType.Tap:
                    return region + " tap candidate (400S+125Ae)/(400+0.15D)*(E+c) [S05][S07]"
                        + (kr ? "; KR second decay max(0.6,1-0.00008D) [S07]" : "")
                        + "; percent-ATK variant is a design placeholder";
                case SkillType.Slide:
                    return region + " slide bounds candidate (400S+120Ae+40r)/(400+0.2D)*(E+c) [S06][S07], engine evaluates one point (no RNG distribution claimed)"
                        + (kr ? "; KR second decay max(0.7,1-0.00005D) [S07]" : "")
                        + "; percent-ATK variant is a design placeholder";
                case SkillType.Auto:
                    return region + " auto candidate structure [20A_total*E/(0.4D+500)+S_auto-20A_naked/500]*max(0.6,1-0.00005D) [S05][S07]; no KR-specific decay declared";
                case SkillType.Drive:
                    return "Actual Drive: design placeholder coefficients (130/400, 0.12D+400); original plan only has a UI prediction [S16], no actual-Drive formula";
                case SkillType.Fever:
                    return "Fever channel: 0.6 structure from [S09][S10] but FeverChannelAtkCoef/FeverChannelFlat/FeverMul are design placeholders";
                default:
                    return "No damage candidate declared for " + type + "; design placeholder";
            }
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

        // Design sibling (extraAtk*130, 0.12*def+400). Not 02_NUMERICS §C.6 PredictedDriveUI and not GL-measured.
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
