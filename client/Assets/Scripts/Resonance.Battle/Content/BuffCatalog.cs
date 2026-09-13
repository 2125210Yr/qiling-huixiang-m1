namespace Resonance.Battle
{
    public sealed class BuffDef
    {
        public string Id;
        public string Name;
        public bool IsDebuff;
        public string Group;
        public string Effect;
        public EffectKind Kind;
        public bool Simulated;
    }

    public static class BuffCatalog
    {
        public static readonly BuffDef[] All = Build();

        public static BuffDef Find(string name)
        {
            if (All == null) return null;
            for (int i = 0; i < All.Length; i++)
            {
                var b = All[i];
                if (b != null && b.Name == name) return b;
            }
            return null;
        }

        public static BuffDef FindId(string id)
        {
            if (All == null) return null;
            for (int i = 0; i < All.Length; i++)
            {
                var b = All[i];
                if (b != null && b.Id == id) return b;
            }
            return null;
        }

        static BuffDef[] Build()
        {
            return new[]
            {
                N("atk_up", "攻击力↑", "输出", "增加攻击力", EffectKind.AtkBuff, true),
                N("def_up", "防御力↑", "生存", "增加防御力", EffectKind.DefBuff, true),
                N("ts_amp", "TS强化", "技能向", "强化Tap Skill", EffectKind.TsAmp),
                N("ts_amp_2", "TS强化Ⅱ", "技能向", "增加Tap Skill的最终伤害", EffectKind.TsAmp),
                N("ts_def_up", "TS防御力↑", "技能向", "增加对Tap Skill的防御力"),
                N("ss_amp", "SS强化", "技能向", "强化Slide Skill", EffectKind.SsAmp),
                N("ss_amp_2", "SS强化Ⅱ", "技能向", "增加Slide Skill的最终伤害", EffectKind.SsAmp),
                N("skill_def_up", "Skill防御力↑", "技能向", "增加所有类型的技能防御力"),
                N("ds_amp", "DS强化", "技能向", "强化Drive Skill的效果", EffectKind.DsAmp),
                N("ds_def_up", "DS防御力↑", "技能向", "增加对Drive Skill的防御力"),
                N("auto_amp", "自动攻击强化", "技能向", "强化自动攻击"),
                N("shield", "屏障", "生存", "产生屏障（在HP受影响前吸收伤害），并为其他目标承受伤害", EffectKind.Shield, true),
                N("debuff_barrier", "减益屏障", "生存", "施放一个吸收减益伤害的独立护盾（会把屏障顶掉）", EffectKind.DebuffBarrier),
                N("debuff_taunt", "减益挑衅", "生存", "代替盟友承受一个减益"),
                N("regen", "回复", "回复", "在一定时间内恢复一定量的HP"),
                N("crit_rate_up", "暴击几率↑", "输出", "增加暴击率"),
                N("crit_up", "暴击↑", "输出", "增加暴击"),
                N("crit_atk_up", "暴击攻击力↑", "输出", "增加暴击的最终伤害"),
                N("combo", "连击", "输出", "击杀敌人后，对剩余敌人造成加成伤害"),
                N("awaken", "觉醒", "输出", "增加暴击伤害，并根据增益数量增加SS伤害"),
                N("immortal", "不死", "生存", "即使受到致命伤害，HP也不会低于1", EffectKind.Immortal),
                N("patience", "耐心", "其他增益", "赋予目前减益数量+1的忍耐"),
                N("endure", "忍耐", "生存", "使伤害无效并减少造成伤害减益的频率"),
                N("adapt_pain", "适应痛苦", "其他增益", "提升一定量的防御力。全体盟友每失去2%HP就提升4%防御力（最多160%，只增加适应痛苦所赋予的防御力）"),
                N("sublime_pain", "升华痛苦", "其他增益", "提升一定量的攻击力。全体盟友每失去2%HP就提升60%攻击力（最多2400%，只增加升华痛苦所赋予的攻击力）"),
                N("heal_up", "即时回复量↑", "回复", "增加回复技能的效果"),
                N("hot_up", "持续回复量↑", "回复", "增加持续回复技能的效果"),
                N("secret_heal", "秘愈", "回复", "每当自身行动或受到伤害时，回复自身HP"),
                N("cleanse", "减益效果无效", "其他增益", "立即移除所有减益效果"),
                N("immune", "免疫", "生存", "赋予所有减益效果免疫；立即移除所有减益"),
                N("revive", "重生", "回复", "死亡时，以一定量HP复活，同时获得充满的Skill量表和增加攻击力"),
                N("weak_atk_up", "弱点攻击力↑", "输出", "增加弱点攻击的伤害"),
                N("support", "支援", "其他增益", "根据增益数增加弱点技能伤害和攻击力"),
                N("charge_amount", "Skill充能↑", "技能向", "增加Skill量表充能量；可与Skill回复加速叠加", EffectKind.ChargeAmount),
                N("haste", "Skill充能加速", "技能向", "加速Skill量表充能速度；可与增加Skill量表充能量的效果叠加", EffectKind.ChargeHaste, true),
                N("dual_wield", "双刃剑", "输出", "增加攻击力的同时减少防御力", EffectKind.DualWield),
                N("overload", "超载", "输出", "除了DS，所有Skill伤害增加，增加Skill冷却时间，减少Skill充能速度", EffectKind.Overload),
                N("shout", "呐喊", "输出", "百分比增加攻击力（最多叠加3次）", EffectKind.AtkBuff),
                N("life_link", "生命连结", "生存", "将一部分受到的伤害（不包含DOT伤害）转为HP，给予HP最低的盟友"),
                N("lifesteal", "吸血", "回复", "攻击所造成伤害的一部分会逐渐回复HP"),
                N("debuff_duration_down", "减益效果持续时间缩短", "其他增益", "减少减益持续时间"),
                N("cooldown_down", "冷却时间减少", "充能节奏", "一定时间内减少目标的SS冷却时间", EffectKind.CooldownDelta),
                N("max_hp_up", "最大HP↑", "生存", "获得额外HP"),
                N("enrage", "激怒", "输出", "储存持续时间内受到的伤害，下一次使用技能时以储存的伤害进行攻击", EffectKind.Enrage),
                N("taunt", "挑衅", "生存", "挑衅敌人并承受所有针对友方的攻击，全体攻击除外", EffectKind.Taunt, true),
                N("reflect", "反射", "生存", "向敌人反射部分伤害；不会反射DS或持续伤害", EffectKind.Reflect),
                N("debuff_resist_up", "减益效果回避率↑", "生存", "增加减益效果回避率"),
                N("dot_resist_up", "增加DOT回避率", "生存", "增加持续伤害减益回避率"),
                D("silence_immune", "沉默无效化", "控制", "沉默无效化"),
                N("true_hit", "直击", "输出", "无视敌人忍耐"),
                N("evade_up", "回避率↑", "生存", "增加技能回避率"),
                N("focus", "专注", "其他增益", "100%命中率和暴击率：免疫失明和影响暴击的负面效果"),
                N("agi_up", "敏捷度↑", "其他增益", "增加敏捷度"),
                N("berserk", "狂暴", "输出", "增加攻击力且无敌（无视攻击和减益），但效果结束时100%概率5s晕眩"),
                D("snow_bomb", "雪球炸弹", "削弱", "当减益持续时间结束时，将造成一定程度的伤害，并重置敌人的Skill量表"),
                D("water_orb", "水球", "削弱", "每次受到攻击都会受到伤害，并在持续时间结束或用尽回数（被攻击2次）时，重置Skill量表（追加伤害同数值的固伤，SS冷却不受影响）"),
                D("heal_down", "即时回复量↓", "削弱", "减少回复技能的效果"),
                D("hot_down", "持续回复量↓", "削弱", "减少持续回复技能的效果"),
                D("fatal_heal", "致命回复", "削弱", "将回复和持续回复的HP回复效果反转为立即伤害或持续伤害；吸血效果和吸收效果都无法造成伤害，也无法回复"),
                D("anti_heal", "禁止回复", "削弱", "无法以任何回复技能回复", EffectKind.AntiHeal),
                N("weak_atk_2", "弱点攻击力Ⅱ", "输出", "减少弱点攻击的伤害"),
                D("weak_def_down", "弱点防御力↓", "削弱", "减少弱点攻击防御力", EffectKind.WeakDefDown),
                D("skill_def_down", "Skill防御力↓", "削弱", "减少对所有技能的防御力", EffectKind.SkillDefDown),
                D("ts_def_down", "TS防御力↓", "削弱", "减少Tap Skill防御力"),
                D("ss_def_down", "SS防御力↓", "削弱", "减少Slide Skill防御力"),
                D("brand", "烙印", "持续伤害", "根据敌人增益数量，造成持续伤害", EffectKind.Dot),
                D("def_down", "防御力↓", "削弱", "减少防御力", EffectKind.DefDebuff, true),
                D("atk_down", "攻击力↓", "削弱", "减少攻击力"),
                D("ds_amp_down", "DS弱化Ⅱ", "削弱", "减少Drive Skill的最终伤害"),
                D("agi_down", "敏捷度↓", "削弱", "减少敏捷度"),
                D("marked", "已标记", "控制", "将伤害技能的目标从敌人改成已标记的目标"),
                D("charm", "诱惑", "控制", "新增益效果无效，无法移除"),
                D("confuse", "混乱", "控制", "混乱时攻击队友(Buff转换给敌方，DeBuff转换给我方)，不影响持续DS效果或复活"),
                D("blind", "失明", "控制", "减少技能命中率"),
                D("silence", "沉默", "控制", "无法使用技能并重置Skill量表（SS冷却时间倒数会停止）", EffectKind.Silence),
                D("debuff_resist_down", "减益效果回避率↓", "削弱", "减少对减益效果的回避率"),
                N("debuff_hit_down", "减少减益命中率", "其他增益", "减少目标的减益命中率"),
                D("charge_amount_down", "Skill充能↓", "削弱", "减少Skill量表充能量；可与Skill量表充能减速叠加"),
                D("charge_speed_down", "Skill充能速度↓", "削弱", "减少Skill量表充能速度；可与减少Skill量表充能量的效果叠加"),
                N("cooldown_up", "冷却时间增加", "充能节奏", "在一定时间内增加目标的SS冷却时间", EffectKind.CooldownDelta),
                N("time_warp", "时间篡改", "其他增益", "移除敌人的一个时间系增益；并减少Skill量表充能速度"),
                N("debuff_duration_up", "延长减益效果", "其他增益", "延长减益持续时间"),
                D("bleed", "流血", "持续伤害", "对一名敌人造成持续伤害（随时间），可叠加", EffectKind.Bleed),
                D("gash", "擦伤", "持续伤害", "增加流血的持续伤害和时间"),
                D("poison", "中毒", "持续伤害", "受到攻击和每次行动都会受到伤害，不可叠加", EffectKind.Poison),
                D("deadly_poison", "猛毒", "持续伤害", "DOT；回复量减少50%；无法缩减或延长减益效果", EffectKind.Poison),
                D("deadly_poison_amp", "猛毒增幅", "持续伤害", "增加猛毒持续伤害300，回复量-30%"),
                D("blade_dance", "乱舞之刃", "持续伤害", "减少目标的防御力，并持续造成伤害", EffectKind.Dot),
                D("curse", "诅咒", "持续伤害", "造成持续伤害，并在减益效果结束或移除后，造成额外伤害", EffectKind.Dot),
                D("decay", "分解", "持续伤害", "造成持续伤害（无法缩短或延长减益效果）", EffectKind.Dot),
                D("burn", "灼烧", "持续伤害", "降低Skill伤害防御力，并在每次受到攻击时造成热伤（受攻击时，每2s造成持续伤害）", EffectKind.Burn),
                D("stun", "晕眩", "控制", "效果时间内，使对方无法行动，且充能量表初始化（效果时间内受到攻击时延长1s，5秒上限）", EffectKind.Stun, true),
                D("petrify", "石化", "控制", "效果时间内，在受到一定次数的攻击前无法行动（充能量表不会被初始化）", EffectKind.Freeze)
            };
        }

        static BuffDef N(string id, string name, string group, string effect, EffectKind kind = EffectKind.Damage, bool sim = false)
        {
            return new BuffDef { Id = id, Name = name, IsDebuff = false, Group = group, Effect = effect, Kind = kind, Simulated = sim };
        }

        static BuffDef D(string id, string name, string group, string effect, EffectKind kind = EffectKind.Damage, bool sim = false)
        {
            return new BuffDef { Id = id, Name = name, IsDebuff = true, Group = group, Effect = effect, Kind = kind, Simulated = sim };
        }
    }
}
