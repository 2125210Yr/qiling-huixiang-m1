using System;

namespace Resonance.Battle
{
    public struct SkillTag
    {
        public bool Heal;
        public bool Debuff;
        public bool Crit;
        public bool Weak;
        public bool Endure;
        public bool Charge;
    }

    public static class SkillTags
    {
        // 总看板: 减益 / 回复 / 暴击 / 弱点 / 忍耐 / 充能 — only bare tokens, not compound names.
        static readonly string[] Longer =
        {
            "减益效果", "减益屏障", "减益挑衅", "减益伤害", "减益命中", "减益回避", "减益持续",
            "弱化减益", "持续伤害减益", "伤害减益", "所有减益", "受到减益", "受减益", "移除减益",
            "减益无效", "时间系减益", "减益攻击", "减益爆发", "减益数", "减益数量", "一个减益", "攻击和减益",
            "即时回复", "持续回复", "禁止回复", "致命回复", "Skill回复", "回复技能", "回复量", "回复型",
            "暴击率", "暴击几率", "暴击攻击", "暴击伤害",
            "弱点攻击", "弱点防御", "弱点技能", "弱点伤害", "弱点Skill", "弱点SkiIl", "弱点SkiI",
            "忍耐状态", "忍耐效果", "无视敌人忍耐", "无视目标的忍耐",
            "完全充能", "skill充能", "drive充能", "SKILL充能", "DRIVE充能",
            "充能加速", "充能速度", "充能量", "正在充能", "充能Skill", "充能SKILL", "充能中", "充能忠"
        };

        public static SkillTag Parse(string skillText)
        {
            if (string.IsNullOrEmpty(skillText)) return default;
            return new SkillTag
            {
                Heal = Has(skillText, "回复"),
                Debuff = Has(skillText, "减益"),
                Crit = Has(skillText, "暴击"),
                Weak = Has(skillText, "弱点"),
                Endure = Has(skillText, "忍耐"),
                Charge = Has(skillText, "充能")
            };
        }

        static bool Has(string text, string token)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token)) return false;
            var n = token.Length;
            var start = 0;
            while (start <= text.Length - n)
            {
                var at = text.IndexOf(token, start, StringComparison.Ordinal);
                if (at < 0) return false;
                if (!CoveredByLonger(text, at, n)) return true;
                start = at + 1;
            }
            return false;
        }

        static bool CoveredByLonger(string text, int at, int n)
        {
            if (string.IsNullOrEmpty(text) || at < 0 || n < 1) return false;
            for (int i = 0; i < Longer.Length; i++)
            {
                var p = Longer[i];
                if (string.IsNullOrEmpty(p)) continue;
                var minStart = at + n - p.Length;
                if (minStart < 0) minStart = 0;
                for (int s = minStart; s <= at && s + p.Length <= text.Length; s++)
                {
                    if (string.Compare(text, s, p, 0, p.Length, StringComparison.OrdinalIgnoreCase) == 0)
                        return true;
                }
            }
            return false;
        }
    }
}
