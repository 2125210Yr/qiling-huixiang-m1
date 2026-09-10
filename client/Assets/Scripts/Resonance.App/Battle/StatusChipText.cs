using System;
using Resonance.Battle;

namespace Resonance.App
{
    /// <summary>
    /// Persistent status ticket copy: kind name + stacks + remaining seconds.
    /// Chinese only. Shared by portrait chips and field chips.
    /// </summary>
    public static class StatusChipText
    {
        public const int Cap = 4;

        public static string[] Labels(UnitState u)
        {
            if (u == null || u.Status == null || u.Status.Count == 0) return null;
            var n = u.Status.Count;
            if (n > Cap) n = Cap;
            var labels = new string[n];
            var wrote = 0;
            for (int i = 0; i < u.Status.Count && wrote < Cap; i++)
            {
                var st = u.Status[i];
                if (st == null || st.Def == null) continue;
                var word = Word(st.Def.Kind);
                if (string.IsNullOrEmpty(word)) continue;
                if (st.Stacks > 1) word += "×" + st.Stacks;
                var sec = st.Remaining > 0f ? (int)Math.Ceiling(st.Remaining) : 0;
                if (sec > 0) word += " " + sec + "s";
                labels[wrote++] = word;
            }
            if (wrote == 0) return null;
            if (wrote == labels.Length) return labels;
            var trim = new string[wrote];
            Array.Copy(labels, trim, wrote);
            return trim;
        }

        public static string Word(EffectKind k)
        {
            switch (k)
            {
                case EffectKind.Heal: return "回复";
                case EffectKind.Dot: return "持续";
                case EffectKind.Shield: return "屏障";
                case EffectKind.AtkBuff: return "攻击↑";
                case EffectKind.DefDebuff: return "防御↓";
                case EffectKind.ChargeHaste: return "加速";
                case EffectKind.Taunt: return "挑衅";
                case EffectKind.TsAmp: return "点按↑";
                case EffectKind.SsAmp: return "上滑↑";
                case EffectKind.DsAmp: return "驱动↑";
                case EffectKind.SkillDefDown: return "技防↓";
                case EffectKind.WeakDefDown: return "弱防↓";
                case EffectKind.Reflect: return "反射";
                case EffectKind.Immortal: return "不死";
                case EffectKind.Silence: return "沉默";
                case EffectKind.Stun: return "晕眩";
                case EffectKind.Freeze: return "冻结";
                case EffectKind.Bleed: return "流血";
                case EffectKind.Poison: return "中毒";
                case EffectKind.Burn: return "灼烧";
                case EffectKind.AntiHeal: return "禁疗";
                case EffectKind.ChargeAmount: return "充能↑";
                case EffectKind.ChargeSpeed: return "充速↑";
                case EffectKind.CooldownDelta: return "冷却";
                case EffectKind.Barrier: return "屏障";
                case EffectKind.DebuffBarrier: return "减益盾";
                case EffectKind.Enrage: return "激怒";
                case EffectKind.Overload: return "超载";
                case EffectKind.DualWield: return "双刃";
                default: return "";
            }
        }
    }
}
