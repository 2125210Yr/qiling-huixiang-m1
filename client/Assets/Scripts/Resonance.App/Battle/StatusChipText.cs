using System;
using Resonance.Battle;

namespace Resonance.App
{
    /// <summary>
    /// Persistent status ticket copy: kind name + stacks + remaining seconds.
    /// Primary ordinary/Robin chips use EN where GT shows EN (DEF ↑ / Barrier).
    /// Shared by portrait chips and field chips.
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
                var word = Word(st.Def);
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

        public static string Word(EffectDef def)
        {
            if (def == null) return "";
            var id = def.Id ?? "";
            // Primary P0 t510 portrait chip EN "vampirism" (lowercase).
            if (id.IndexOf("lifesteal", StringComparison.OrdinalIgnoreCase) >= 0
                || id.IndexOf("vamp", StringComparison.OrdinalIgnoreCase) >= 0)
                return "vampirism";
            // Primary Robin ~t48 portrait chip EN "BLIND".
            if (id.IndexOf("blind", StringComparison.OrdinalIgnoreCase) >= 0
                || id.IndexOf("失明", StringComparison.Ordinal) >= 0)
                return "BLIND";
            // P0 t505 portrait float EN "CRT Rate ↑". Match Id only — EffectDef has no Name.
            if (id.IndexOf("crit_rate", StringComparison.OrdinalIgnoreCase) >= 0)
                return "CRT Rate ↑";
            if (id.IndexOf("crit_atk", StringComparison.OrdinalIgnoreCase) >= 0)
                return "CRT ATK ↑";
            if (id.Equals("crit_up", StringComparison.OrdinalIgnoreCase)
                || id.IndexOf("crit_up", StringComparison.OrdinalIgnoreCase) >= 0)
                return "CRT ↑";
            // Hard r61 field float EN "EVA".
            if (id.IndexOf("evade", StringComparison.OrdinalIgnoreCase) >= 0
                || id.IndexOf("回避", StringComparison.Ordinal) >= 0)
                return BattleCueCopy.EvaFloat;
            return Word(def.Kind);
        }

        public static string Word(EffectKind k)
        {
            switch (k)
            {
                case EffectKind.Heal: return "Recovery";
                case EffectKind.Dot: return "DoT";
                case EffectKind.Shield: return "Barrier";
                case EffectKind.AtkBuff: return "ATK ↑";
                case EffectKind.DefBuff: return "DEF ↑";
                case EffectKind.DefDebuff: return "DEF ↓";
                case EffectKind.ChargeHaste: return "Haste";
                case EffectKind.Taunt: return "Taunt";
                case EffectKind.TsAmp: return "Tap Skill Boost";
                case EffectKind.SsAmp: return "Slide Skill Boost";
                case EffectKind.DsAmp: return "Drive Skill Boost";
                case EffectKind.SkillDefDown: return "Skill DEF ↓";
                case EffectKind.WeakDefDown: return "Weak Point DEF ↓";
                case EffectKind.Reflect: return "Reflect";
                case EffectKind.Immortal: return "Immortal";
                case EffectKind.Silence: return "Silence";
                case EffectKind.Stun: return "Stun";
                case EffectKind.Freeze: return "Freeze";
                case EffectKind.Bleed: return "Bleed";
                case EffectKind.Poison: return "Poison";
                case EffectKind.Burn: return "Burn";
                case EffectKind.AntiHeal: return "Anti-Heal";
                case EffectKind.ChargeAmount: return "Charge ↑";
                case EffectKind.ChargeSpeed: return "Charge SPD ↑";
                case EffectKind.CooldownDelta: return "CD";
                case EffectKind.Barrier: return "Barrier";
                case EffectKind.DebuffBarrier: return "Debuff Barrier";
                case EffectKind.Enrage: return "Enrage";
                case EffectKind.Overload: return "Overload";
                case EffectKind.DualWield: return "Dual";
                default: return "";
            }
        }
    }
}
