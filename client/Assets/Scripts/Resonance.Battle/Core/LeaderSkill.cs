using System;

namespace Resonance.Battle
{
    public static class LeaderSkill
    {
        public const float AtkMulMin = 1f;
        public const float AtkMulMax = 1.15f;
        public const float ForeverSec = 1e6f;

        public static void Apply(BattleSim sim, string leaderId)
        {
            if (sim == null) return;
            CharacterDef ch;
            SkillDef skill;
            Resolve(leaderId, sim, out ch, out skill);
            var fx = Kit(ch, skill);
            var caster = FindCaster(sim, ch);
            if (fx != null)
            {
                var foes = IsFoeRule(skill);
                if (foes)
                {
                    var list = sim.Enemies;
                    if (list != null)
                        for (int i = 0; i < list.Count; i++)
                            Paint(sim, list[i], fx);
                }
                else if (skill != null && skill.Target == TargetRule.Self)
                    Paint(sim, caster, fx);
                else
                {
                    var allies = sim.Allies;
                    if (allies != null)
                        for (int i = 0; i < allies.Length; i++)
                            Paint(sim, allies[i], fx);
                }
            }
            Heal(sim, caster, skill);
            if (fx != null || HasHeal(skill))
                sim.LastEvent = skill != null && !string.IsNullOrEmpty(skill.Name)
                    ? "队长技  " + skill.Name
                    : "队长技";
        }

        public static float AtkMul(string leaderId)
        {
            CharacterDef ch;
            SkillDef skill;
            Resolve(leaderId, null, out ch, out skill);
            var bonus = AtkBonus(ch, skill);
            var mul = 1f + bonus;
            if (mul < AtkMulMin) return AtkMulMin;
            if (mul > AtkMulMax) return AtkMulMax;
            return mul;
        }

        static float AtkBonus(CharacterDef ch, SkillDef skill)
        {
            var src = skill != null ? Catalog.TryEffect(skill.EffectId) : null;
            if (src != null && src.Kind == EffectKind.AtkBuff)
                return src.Magnitude;
            if (ch != null && ch.Role == Role.Attacker)
                return AtkMulMax - 1f;
            return 0f;
        }

        static EffectDef Kit(CharacterDef ch, SkillDef skill)
        {
            var src = skill != null ? Catalog.TryEffect(skill.EffectId) : null;
            if (src != null && src.Kind != EffectKind.Damage)
                return Forever(src);
            if (HasHeal(skill)) return null;
            return RoleKit(ch);
        }

        static EffectDef RoleKit(CharacterDef ch)
        {
            if (ch == null) return null;
            switch (ch.Role)
            {
                case Role.Attacker:
                    return Stamp(EffectKind.AtkBuff, AtkMulMax - 1f);
                case Role.Defender:
                    return Stamp(EffectKind.Shield, 0.15f);
                case Role.Debuffer:
                    return Stamp(EffectKind.DefDebuff, 0.15f);
                case Role.Healer:
                    return Stamp(EffectKind.ChargeHaste, 0.10f);
                default:
                    return Stamp(EffectKind.ChargeHaste, 0.15f);
            }
        }

        static EffectDef Forever(EffectDef src)
        {
            if (src == null) return null;
            return new EffectDef
            {
                Id = "leader_" + (src.Id ?? ""),
                Opcode = string.IsNullOrEmpty(src.Opcode) ? EffectOpcodes.ForKind(src.Kind) : src.Opcode,
                Kind = src.Kind,
                Magnitude = src.Magnitude,
                DurationSec = ForeverSec,
                MaxStack = src.MaxStack < 1 ? 1 : src.MaxStack,
                SourceTier = src.SourceTier < 8 ? 8 : src.SourceTier,
                Group = "leader"
            };
        }

        static EffectDef Stamp(EffectKind kind, float mag)
        {
            return new EffectDef
            {
                Id = "leader_" + kind,
                Opcode = EffectOpcodes.ForKind(kind),
                Kind = kind,
                Magnitude = mag,
                DurationSec = ForeverSec,
                MaxStack = 1,
                SourceTier = 8,
                Group = "leader"
            };
        }

        static void Paint(BattleSim sim, UnitState t, EffectDef fx)
        {
            if (sim == null || t == null || !t.Alive || fx == null) return;
            if (t.Status == null) return;
            sim.ApplyStatus(t, fx);
        }

        static void Heal(BattleSim sim, UnitState caster, SkillDef skill)
        {
            if (sim == null || caster == null || caster.Def == null || !HasHeal(skill)) return;
            var allies = sim.Allies;
            if (allies == null) return;
            for (int i = 0; i < allies.Length; i++)
            {
                var t = allies[i];
                if (t == null || !t.Alive || t.Hp >= t.MaxHp) continue;
                var amt = (int)Math.Round(caster.Atk * skill.HealCoef + skill.FlatHeal + t.MaxHp * skill.HealMaxHpFrac);
                if (amt < 1) continue;
                var next = t.Hp + amt;
                if (next > t.MaxHp) next = t.MaxHp;
                var gained = next - t.Hp;
                if (gained < 1) continue;
                t.Hp = next;
                if (sim.Log == null) continue;
                sim.Log.Add(new FloatText
                {
                    UnitSlot = t.Slot,
                    Ally = t.Ally,
                    CasterSlot = caster.Slot,
                    CasterAlly = caster.Ally,
                    Text = "+" + gained,
                    Heal = true,
                    Kind = SkillType.Leader
                });
            }
        }

        static bool HasHeal(SkillDef skill)
        {
            return skill != null && (skill.HealCoef > 0f || skill.FlatHeal > 0 || skill.HealMaxHpFrac > 0f);
        }

        static bool IsFoeRule(SkillDef skill)
        {
            return skill != null && TargetSemantics.IsFoeSide(skill.Target);
        }

        static UnitState FindCaster(BattleSim sim, CharacterDef ch)
        {
            if (sim == null) return null;
            var allies = sim.Allies;
            if (allies == null) return null;
            if (ch != null)
            {
                for (int i = 0; i < allies.Length; i++)
                {
                    var u = allies[i];
                    if (u != null && u.Def != null && u.Def.Id == ch.Id) return u;
                }
            }
            var slot = sim.LeaderSlot;
            if (slot >= 0 && slot < allies.Length) return allies[slot];
            return null;
        }

        static void Resolve(string leaderId, BattleSim sim, out CharacterDef ch, out SkillDef skill)
        {
            ch = null;
            skill = null;
            if (string.IsNullOrEmpty(leaderId) && sim != null)
            {
                var u = FindCaster(sim, null);
                if (u != null) ch = u.Def;
            }
            else
            {
                ch = TryChar(leaderId);
                skill = Catalog.TrySkill(leaderId);
                if (ch == null && skill == null && EndsWithLeader(leaderId))
                    ch = TryChar(leaderId.Substring(0, leaderId.Length - 7));
            }
            if (skill == null && ch != null)
                skill = Catalog.TrySkill(ch.LeaderSkillId);
            if (ch == null && skill != null && !string.IsNullOrEmpty(skill.Id))
            {
                var cut = skill.Id.LastIndexOf('_');
                if (cut > 0) ch = TryChar(skill.Id.Substring(0, cut));
            }
        }

        static bool EndsWithLeader(string id)
        {
            const string tail = "_leader";
            if (string.IsNullOrEmpty(id) || id.Length <= tail.Length) return false;
            return string.CompareOrdinal(id, id.Length - tail.Length, tail, 0, tail.Length) == 0;
        }

        static CharacterDef TryChar(string id)
        {
            if (string.IsNullOrEmpty(id) || Catalog.Characters == null) return null;
            CharacterDef c;
            return Catalog.Characters.TryGetValue(id, out c) ? c : null;
        }
    }
}
