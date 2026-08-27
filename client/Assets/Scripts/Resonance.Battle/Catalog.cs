using System.Collections.Generic;

namespace Resonance.Battle
{
    public static class Catalog
    {
        public static IReadOnlyDictionary<string, CharacterDef> Characters { get; private set; }
        public static IReadOnlyDictionary<string, SkillDef> Skills { get; private set; }
        public static IReadOnlyDictionary<string, EffectDef> Effects { get; private set; }
        public static StageDef VerticalSliceStage { get; private set; }
        public static readonly string[] DefaultParty = { "C001", "C007", "C010", "C003", "C005" };

        static Catalog()
        {
            BuildBuiltin();
            CatalogJson.TryLoadDefault();
        }

        internal static void Install(
            Dictionary<string, CharacterDef> chars,
            Dictionary<string, SkillDef> skills,
            Dictionary<string, EffectDef> effects,
            StageDef stage)
        {
            Characters = chars;
            Skills = skills;
            Effects = effects;
            VerticalSliceStage = stage;
        }

        internal static void BuildBuiltin()
        {
            var skills = new Dictionary<string, SkillDef>();
            AddSkills(skills);

            var effects = new Dictionary<string, EffectDef>
            {
                ["dot_flame"] = new EffectDef { Id = "dot_flame", Kind = EffectKind.Dot, Magnitude = 0.18f, DurationSec = 8f, MaxStack = 1, SourceTier = 2, Group = "dot" },
                ["def_down"] = new EffectDef { Id = "def_down", Kind = EffectKind.DefDebuff, Magnitude = 0.20f, DurationSec = 10f, MaxStack = 1, SourceTier = 2, Group = "def" },
                ["atk_up"] = new EffectDef { Id = "atk_up", Kind = EffectKind.AtkBuff, Magnitude = 0.18f, DurationSec = 12f, MaxStack = 1, SourceTier = 2, Group = "atk" },
                ["shield"] = new EffectDef { Id = "shield", Kind = EffectKind.Shield, Magnitude = 0.22f, DurationSec = 8f, MaxStack = 1, SourceTier = 1, Group = "shield" },
                ["taunt"] = new EffectDef { Id = "taunt", Kind = EffectKind.Taunt, Magnitude = 1f, DurationSec = 6f, MaxStack = 1, SourceTier = 2, Group = "taunt" },
                ["haste"] = new EffectDef { Id = "haste", Kind = EffectKind.ChargeHaste, Magnitude = 0.25f, DurationSec = 8f, MaxStack = 1, SourceTier = 1, Group = "haste" },
                ["burst_atk"] = new EffectDef { Id = "burst_atk", Kind = EffectKind.AtkBuff, Magnitude = 0.35f, DurationSec = 10f, MaxStack = 1, SourceTier = 3, Group = "atk" }
            };
            var chars = new Dictionary<string, CharacterDef>();
            AddParty(chars);
            AddEnemies(chars);

            var stage = new StageDef
            {
                Id = "VS-1",
                Name = "废都入口",
                TimeLimitSec = 90f,
                Wave0 = new[] { "E001", "E002", "E003", "E004", "E005" },
                Wave1 = new[] { "EBOSS" }
            };
            Install(chars, skills, effects, stage);
        }

        public static CharacterDef MustChar(string id) => Characters[id];
        public static SkillDef MustSkill(string id) => Skills[id];
        public static EffectDef TryEffect(string id) => string.IsNullOrEmpty(id) ? null : (Effects.TryGetValue(id, out var e) ? e : null);

        static void AddParty(Dictionary<string, CharacterDef> c)
        {
            c["C001"] = Unit("C001", "焰刃", Element.Fire, Role.Attacker, 2300, 1180, 650, 920, 800, 9.0f, false);
            c["C002"] = Unit("C002", "炉心卫士", Element.Fire, Role.Defender, 3400, 720, 1180, 640, 500, 8.5f, false);
            c["C003"] = Unit("C003", "潮汐祭司", Element.Water, Role.Healer, 2900, 900, 820, 780, 650, 9.0f, false);
            c["C004"] = Unit("C004", "深蓝咒师", Element.Water, Role.Debuffer, 2550, 870, 760, 1080, 680, 8.0f, false);
            c["C005"] = Unit("C005", "森语引路者", Element.Wood, Role.Supporter, 2750, 800, 850, 1020, 600, 7.5f, false);
            c["C006"] = Unit("C006", "荆棘猎手", Element.Wood, Role.Attacker, 2250, 1120, 690, 980, 850, 9.0f, false);
            c["C007"] = Unit("C007", "白昼守望", Element.Light, Role.Defender, 3250, 760, 1120, 700, 530, 8.5f, false);
            c["C008"] = Unit("C008", "晨星歌者", Element.Light, Role.Supporter, 2680, 830, 820, 1060, 620, 7.5f, false);
            c["C009"] = Unit("C009", "夜幕医师", Element.Dark, Role.Healer, 2850, 920, 800, 820, 680, 9.0f, false);
            c["C010"] = Unit("C010", "影缚者", Element.Dark, Role.Debuffer, 2500, 900, 730, 1120, 700, 8.0f, false);
            c["C011"] = Unit("C011", "灼红侍从", Element.Fire, Role.Attacker, 2050, 980, 610, 840, 720, 9.0f, false);
            c["C012"] = Unit("C012", "冰镜使者", Element.Water, Role.Supporter, 2400, 720, 760, 900, 560, 7.5f, false);
        }

        static void AddEnemies(Dictionary<string, CharacterDef> c)
        {
            c["E001"] = Unit("E001", "废铁斥候", Element.Fire, Role.Attacker, 1800, 620, 420, 500, 300, 9.5f, true);
            c["E002"] = Unit("E002", "锈盾步卒", Element.Wood, Role.Defender, 2400, 480, 780, 380, 220, 9.0f, true);
            c["E003"] = Unit("E003", "浊潮咒徒", Element.Water, Role.Debuffer, 1700, 560, 400, 620, 340, 8.5f, true);
            c["E004"] = Unit("E004", "残灯祭司", Element.Light, Role.Healer, 1900, 500, 450, 480, 260, 9.5f, true);
            c["E005"] = Unit("E005", "棘林弓手", Element.Wood, Role.Attacker, 1650, 640, 390, 700, 360, 9.0f, true);
            var boss = Unit("EBOSS", "城门守核", Element.Dark, Role.Defender, 9800, 920, 980, 520, 400, 8.0f, true);
            boss.IsBoss = true;
            c["EBOSS"] = boss;
        }

        static CharacterDef Unit(string id, string name, Element el, Role role, int hp, int atk, int def, int agl, int crt, float charge, bool enemy)
        {
            var prefix = enemy ? id : id;
            return new CharacterDef
            {
                Id = id,
                Name = name,
                Element = el,
                Role = role,
                Hp = hp,
                Atk = atk,
                Def = def,
                Agl = agl,
                Crt = crt,
                ChargeTimeSec = charge,
                AutoSkillId = prefix + "_auto",
                TapSkillId = prefix + "_tap",
                SlideSkillId = prefix + "_slide",
                DriveSkillId = prefix + "_drive",
                LeaderSkillId = prefix + "_leader",
                IsEnemy = enemy
            };
        }

        static void AddSkills(Dictionary<string, SkillDef> s)
        {
            RoleKit(s, "C001", Role.Attacker);
            RoleKit(s, "C002", Role.Defender);
            RoleKit(s, "C003", Role.Healer);
            RoleKit(s, "C004", Role.Debuffer);
            RoleKit(s, "C005", Role.Supporter);
            RoleKit(s, "C006", Role.Attacker);
            RoleKit(s, "C007", Role.Defender);
            RoleKit(s, "C008", Role.Supporter);
            RoleKit(s, "C009", Role.Healer);
            RoleKit(s, "C010", Role.Debuffer);
            RoleKit(s, "C011", Role.Attacker);
            RoleKit(s, "C012", Role.Supporter);
            RoleKit(s, "E001", Role.Attacker);
            RoleKit(s, "E002", Role.Defender);
            RoleKit(s, "E003", Role.Debuffer);
            RoleKit(s, "E004", Role.Healer);
            RoleKit(s, "E005", Role.Attacker);
            RoleKit(s, "EBOSS", Role.Defender);
        }

        static void RoleKit(Dictionary<string, SkillDef> s, string id, Role role)
        {
            s[id + "_auto"] = Sk(id + "_auto", "连击", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.35f, 40, 0, null);
            switch (role)
            {
                case Role.Attacker:
                    s[id + "_tap"] = Sk(id + "_tap", "直斩", SkillType.Tap, TargetRule.RandomEnemies, 1, 1, 0.90f, 120, 6, null);
                    s[id + "_slide"] = Sk(id + "_slide", "裂空", SkillType.Slide, TargetRule.RandomEnemies, 2, 1, 1.65f, 260, 14, "dot_flame");
                    s[id + "_drive"] = Sk(id + "_drive", "焚城", SkillType.Drive, TargetRule.HighestAtkEnemies, 3, 1, 2.90f, 500, 0, "burst_atk");
                    s[id + "_leader"] = Sk(id + "_leader", "锋势", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "atk_up");
                    break;
                case Role.Defender:
                    s[id + "_tap"] = Sk(id + "_tap", "护壁", SkillType.Tap, TargetRule.RandomEnemies, 1, 1, 0.65f, 80, 6, "shield");
                    s[id + "_slide"] = Sk(id + "_slide", "嘲讽壁垒", SkillType.Slide, TargetRule.RandomEnemies, 1, 1, 0.80f, 100, 14, "taunt");
                    s[id + "_drive"] = Sk(id + "_drive", "全队结界", SkillType.Drive, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "shield");
                    s[id + "_leader"] = Sk(id + "_leader", "守线", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "shield");
                    break;
                case Role.Debuffer:
                    s[id + "_tap"] = Sk(id + "_tap", "蚀纹", SkillType.Tap, TargetRule.RandomEnemies, 1, 1, 0.70f, 90, 6, "dot_flame");
                    s[id + "_slide"] = Sk(id + "_slide", "崩防", SkillType.Slide, TargetRule.LowestHpEnemies, 2, 1, 1.10f, 160, 14, "def_down");
                    s[id + "_drive"] = Sk(id + "_drive", "三重咒锁", SkillType.Drive, TargetRule.RandomEnemies, 3, 1, 1.80f, 280, 0, "def_down");
                    s[id + "_leader"] = Sk(id + "_leader", "弱点暴露", SkillType.Leader, TargetRule.AllEnemies, 5, 0, 0f, 0, 0, "def_down");
                    break;
                case Role.Healer:
                    s[id + "_tap"] = Heal(id + "_tap", "急救", SkillType.Tap, TargetRule.LowestHpAlly, 1, 0.45f, 180, 0.00f, 6);
                    s[id + "_slide"] = Heal(id + "_slide", "双潮", SkillType.Slide, TargetRule.LowestHpAlly, 2, 0.65f, 350, 0.08f, 14);
                    s[id + "_drive"] = Heal(id + "_drive", "满潮", SkillType.Drive, TargetRule.AllAllies, 5, 0.90f, 420, 0.10f, 0);
                    s[id + "_leader"] = Heal(id + "_leader", "潮息", SkillType.Leader, TargetRule.AllAllies, 5, 0.20f, 80, 0f, 0);
                    break;
                default:
                    s[id + "_tap"] = Sk(id + "_tap", "鼓舞", SkillType.Tap, TargetRule.LowestHpAlly, 1, 1, 0.40f, 60, 6, "haste");
                    s[id + "_slide"] = Sk(id + "_slide", "全队加攻", SkillType.Slide, TargetRule.AllAllies, 5, 0, 0f, 0, 14, "atk_up");
                    s[id + "_drive"] = Sk(id + "_drive", "爆发号令", SkillType.Drive, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "burst_atk");
                    s[id + "_leader"] = Sk(id + "_leader", "协律", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "haste");
                    break;
            }
        }

        static SkillDef Sk(string id, string name, SkillType type, TargetRule rule, int count, int hits, float coef, int flat, int drive, string effect)
        {
            return new SkillDef
            {
                Id = id, Name = name, Type = type, Target = rule, TargetCount = count,
                HitCount = hits < 1 ? 1 : hits, AtkCoef = coef, FlatPower = flat, DriveGain = drive, EffectId = effect
            };
        }

        static SkillDef Heal(string id, string name, SkillType type, TargetRule rule, int count, float coef, int flat, float hpFrac, int drive)
        {
            var sk = Sk(id, name, type, rule, count, 0, 0f, 0, drive, null);
            sk.HealCoef = coef;
            sk.FlatHeal = flat;
            sk.HealMaxHpFrac = hpFrac;
            sk.HitCount = 0;
            return sk;
        }
    }
}
