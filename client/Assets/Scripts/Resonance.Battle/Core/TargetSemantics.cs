namespace Resonance.Battle
{
    /// <summary>
    /// Declared target side / declared-skill set. ENGINEERING — not GL.
    /// Cast must use this instead of an EffectKind whitelist.
    /// </summary>
    public static class TargetSemantics
    {
        public static TargetRule Rule(SkillDef skill, EffectDef fx)
        {
            if (fx != null && fx.HasTarget) return fx.Target;
            return skill != null ? skill.Target : TargetRule.Self;
        }

        public static TargetSide Side(SkillDef skill, EffectDef fx)
        {
            if (fx != null && fx.Side != TargetSide.FromRule) return fx.Side;
            return SideOf(Rule(skill, fx));
        }

        public static TargetSide SideOf(TargetRule rule)
        {
            switch (rule)
            {
                case TargetRule.Self:
                    return TargetSide.Self;
                case TargetRule.LowestHpAlly:
                case TargetRule.AllAllies:
                case TargetRule.LowestHpRatioAlly:
                    return TargetSide.Ally;
                default:
                    return TargetSide.Foe;
            }
        }

        public static bool IsAllySide(TargetRule rule)
        {
            var s = SideOf(rule);
            return s == TargetSide.Ally || s == TargetSide.Self;
        }

        public static bool IsFoeSide(TargetRule rule) => SideOf(rule) == TargetSide.Foe;

        /// <summary>Silence blocks these. Auto-attack and Leader are not in the set.</summary>
        public static bool IsDeclaredSkill(SkillType t)
        {
            return t == SkillType.Tap || t == SkillType.Slide
                || t == SkillType.Drive || t == SkillType.Fever;
        }
    }
}
