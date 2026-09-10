using System.Collections.Generic;

namespace Resonance.Battle
{
    public static class Catalog
    {
        public static IReadOnlyDictionary<string, CharacterDef> Characters { get; private set; }
        public static IReadOnlyDictionary<string, SkillDef> Skills { get; private set; }
        public static IReadOnlyDictionary<string, EffectDef> Effects { get; private set; }
        public static StageDef VerticalSliceStage { get; private set; }
        public static StageDef[] Stages { get; private set; } = new StageDef[0];
        public static StageDef[] HardStages { get; private set; } = new StageDef[0];
        public static string[] PlayableIds { get; private set; } = new string[0];
        static readonly string[] DefaultPartyCore = { "C001", "C007", "C010", "C003", "C005" };
        public static string[] DefaultParty => CopyWave(DefaultPartyCore);
        public static GearDef[] Cartas => GearCatalog.Cartas;

        public static CharacterDef PlayableAt(Element element, Role role)
        {
            if (PlayableIds == null) return null;
            for (int i = 0; i < PlayableIds.Length; i++)
            {
                var c = TryChar(PlayableIds[i]);
                if (c != null && c.Element == element && c.Role == role) return c;
            }
            return null;
        }

        public static string[] MatrixIds
        {
            get
            {
                var ids = new string[25];
                var n = 0;
                for (int e = 0; e < 5; e++)
                {
                    for (int r = 0; r < 5; r++)
                    {
                        var c = PlayableAt((Element)e, (Role)r);
                        ids[n++] = c != null ? c.Id : "";
                    }
                }
                return ids;
            }
        }

        static Catalog()
        {
            BuildBuiltin();
            CatalogJson.TryLoadDefault();
        }

        internal static void Install(
            Dictionary<string, CharacterDef> chars,
            Dictionary<string, SkillDef> skills,
            Dictionary<string, EffectDef> effects,
            StageDef stage,
            StageDef[] stages = null)
        {
            if (chars == null) chars = new Dictionary<string, CharacterDef>();
            if (skills == null) skills = new Dictionary<string, SkillDef>();
            if (effects == null) effects = new Dictionary<string, EffectDef>();
            Characters = chars;
            Skills = skills;
            Effects = effects;
            stages = CompactStages(stages);
            if (stages == null || stages.Length == 0)
                stages = MakeChapter(stage);
            Stages = stages;
            HardStages = MakeHardChapter(stages);
            VerticalSliceStage = stages.Length > 0 ? stages[0] : stage;
            var play = new List<string>();
            foreach (var kv in chars)
            {
                if (kv.Value == null || kv.Value.IsEnemy) continue;
                play.Add(kv.Key);
            }
            play.Sort();
            PlayableIds = play.ToArray();
        }

        public static StageDef[] MakeChapter(StageDef first = null)
        {
            var names = new[]
            {
                "废都入口", "锈轨巷", "断桥", "浊潮井", "棘林边缘",
                "残灯回廊", "白昼裂口", "影缚地窟", "炉心外环", "城门广场",
                "守核下层", "城门守核"
            };
            var waves = new[]
            {
                new[] { "E001", "E002", "E003", "E004", "E005" },
                new[] { "E001", "E001", "E002", "E005" },
                new[] { "E002", "E002", "E003" },
                new[] { "E003", "E003", "E004", "E001" },
                new[] { "E005", "E005", "E002" },
                new[] { "E004", "E003", "E001" },
                new[] { "E001", "E005", "E004", "E002" },
                new[] { "E003", "E002", "E005", "E001" },
                new[] { "E002", "E004", "E005" },
                new[] { "E001", "E002", "E003", "E005" },
                new[] { "E002", "E002", "E001", "E003", "E005" },
                new[] { "E002", "E001", "E005", "E003", "E004" }
            };
            var stages = new StageDef[12];
            for (int i = 0; i < 12; i++)
            {
                stages[i] = new StageDef
                {
                    Id = i == 0 ? "VS-1" : ("CH1-" + (i + 1)),
                    Name = "第1章-" + (i + 1) + "  " + names[i],
                    TimeLimitSec = 95f - i * 1.5f,
                    Wave0 = CopyWave(waves[i]),
                    Wave1 = CopyWave(BossWave),
                    EnemyHpMul = 0.76f + i * 1.42f,
                    EnemyAtkMul = 0.85f + i * 0.05f,
                    EnemyDefMul = 0.90f + i * 0.10f
                };
            }
            if (first != null)
            {
                stages[0].Id = first.Id;
                stages[0].Name = first.Name;
                stages[0].TimeLimitSec = first.TimeLimitSec;
                stages[0].Wave0 = CopyWave(first.Wave0);
                stages[0].Wave1 = CopyWave(first.Wave1);
                stages[0].EnemyHpMul = first.EnemyHpMul;
                stages[0].EnemyAtkMul = first.EnemyAtkMul;
                stages[0].EnemyDefMul = first.EnemyDefMul;
            }
            return stages;
        }

        public static StageDef[] MakeHardChapter(StageDef[] normal)
        {
            if (normal == null || normal.Length == 0) return new StageDef[0];
            var hard = new StageDef[normal.Length];
            for (int i = 0; i < normal.Length; i++)
            {
                var s = normal[i];
                if (s == null)
                {
                    hard[i] = new StageDef { Id = "H", Wave0 = CopyWave(null), Wave1 = CopyWave(null) };
                    continue;
                }
                hard[i] = new StageDef
                {
                    Id = (s.Id ?? "") + "H",
                    Name = (s.Name ?? "") + "  困难",
                    TimeLimitSec = s.TimeLimitSec,
                    Wave0 = CopyWave(s.Wave0),
                    Wave1 = CopyWave(s.Wave1),
                    EnemyHpMul = s.EnemyHpMul * StageMode.EnemyHpMul(StageModeKind.Hard),
                    EnemyAtkMul = s.EnemyAtkMul * StageMode.EnemyAtkMul(StageModeKind.Hard),
                    EnemyDefMul = s.EnemyDefMul * StageMode.EnemyDefMul(StageModeKind.Hard),
                    Difficulty = 1,
                    NeedStageId = s.Id ?? ""
                };
            }
            return hard;
        }

        public static StageDef[] Chapter(bool hard) => hard ? HardStages : Stages;

        static readonly string[] BossWave = { "EBOSS" };

        internal static string[] CopyWave(string[] src)
        {
            if (src == null || src.Length == 0) return new string[0];
            var n = 0;
            for (int i = 0; i < src.Length; i++)
                if (!string.IsNullOrEmpty(src[i])) n++;
            var d = new string[n];
            var j = 0;
            for (int i = 0; i < src.Length; i++)
                if (!string.IsNullOrEmpty(src[i])) d[j++] = src[i];
            return d;
        }

        static StageDef[] CompactStages(StageDef[] stages)
        {
            if (stages == null) return null;
            var n = 0;
            for (int i = 0; i < stages.Length; i++)
                if (stages[i] != null) n++;
            if (n == stages.Length) return stages;
            if (n == 0) return null;
            var d = new StageDef[n];
            var j = 0;
            for (int i = 0; i < stages.Length; i++)
                if (stages[i] != null) d[j++] = stages[i];
            return d;
        }

        internal static void BuildBuiltin()
        {
            var skills = new Dictionary<string, SkillDef>();
            AddSkills(skills);

            var effects = new Dictionary<string, EffectDef>
            {
                ["dot_flame"] = Fx("dot_flame", EffectKind.Dot, 0.18f, 8f, 1, 2, "dot"),
                ["def_down"] = Fx("def_down", EffectKind.DefDebuff, 0.20f, 10f, 1, 2, "def"),
                ["atk_up"] = Fx("atk_up", EffectKind.AtkBuff, 0.18f, 12f, 1, 2, "atk"),
                ["shield"] = Fx("shield", EffectKind.Shield, 0.22f, 8f, 1, 1, "shield"),
                ["taunt"] = Fx("taunt", EffectKind.Taunt, 1f, 6f, 1, 2, "taunt"),
                ["haste"] = Fx("haste", EffectKind.ChargeHaste, 0.25f, 8f, 1, 1, "haste"),
                ["burst_atk"] = Fx("burst_atk", EffectKind.AtkBuff, 0.35f, 10f, 1, 3, "atk"),
                ["stun"] = Fx("stun", EffectKind.Stun, 1f, 3f, 1, 2, "stun")
            };
            var chars = new Dictionary<string, CharacterDef>();
            AddParty(chars);
            AddEnemies(chars);

            var stage = new StageDef
            {
                Id = "VS-1",
                Name = "废都入口",
                TimeLimitSec = 90f,
                Wave0 = new[] { "E001", "E002", "E003" },
                Wave1 = new[] { "EBOSS" }
            };
            Install(chars, skills, effects, stage, MakeChapter(stage));
        }

        public static CharacterDef MustChar(string id) => Characters[id];
        public static SkillDef MustSkill(string id) => Skills[id];
        public static CharacterDef TryChar(string id)
        {
            if (string.IsNullOrEmpty(id) || Characters == null) return null;
            CharacterDef c;
            return Characters.TryGetValue(id, out c) ? c : null;
        }
        public static SkillDef TrySkill(string id)
        {
            if (string.IsNullOrEmpty(id) || Skills == null) return null;
            SkillDef s;
            return Skills.TryGetValue(id, out s) ? s : null;
        }

        public static SkillDef CloneSkill(SkillDef s)
        {
            if (s == null) return null;
            return new SkillDef
            {
                Id = s.Id,
                Name = s.Name,
                Type = s.Type,
                Target = s.Target,
                TargetCount = s.TargetCount,
                HitCount = s.HitCount,
                AtkCoef = s.AtkCoef,
                FlatPower = s.FlatPower,
                DriveGain = s.DriveGain,
                EffectId = s.EffectId,
                HealCoef = s.HealCoef,
                FlatHeal = s.FlatHeal,
                HealMaxHpFrac = s.HealMaxHpFrac,
                SkillFlat = s.SkillFlat,
                PercentAtk = s.PercentAtk,
                IsIgnitedVariant = s.IsIgnitedVariant,
                BaseSkillId = s.BaseSkillId,
                RequireTags = s.RequireTags == null ? null : CopyWave(s.RequireTags),
                Opcode = s.Opcode
            };
        }
        public static EffectDef TryEffect(string id)
        {
            if (string.IsNullOrEmpty(id) || Effects == null) return null;
            EffectDef e;
            return Effects.TryGetValue(id, out e) ? e : null;
        }

        static void AddParty(Dictionary<string, CharacterDef> c)
        {
            c["C001"] = Unit("C001", "冰刃", Element.Fire, Role.Attacker, 2200, 1180, 640, 900, 820, 9.0f, false);
            c["C002"] = Unit("C002", "炉心卫士", Element.Fire, Role.Defender, 3300, 740, 1160, 660, 520, 8.5f, false);
            c["C003"] = Unit("C003", "潮汐祭司", Element.Water, Role.Healer, 2900, 900, 800, 800, 660, 9.0f, false);
            c["C004"] = Unit("C004", "深蓝咒师", Element.Water, Role.Debuffer, 2500, 880, 740, 1100, 700, 8.0f, false);
            c["C005"] = Unit("C005", "森语引路者", Element.Wood, Role.Supporter, 2700, 820, 840, 1040, 600, 7.5f, false);
            c["C006"] = Unit("C006", "荆棘猎手", Element.Wood, Role.Attacker, 2240, 1160, 650, 920, 840, 9.0f, false);
            c["C007"] = Unit("C007", "白昼守望", Element.Light, Role.Defender, 3280, 760, 1140, 680, 530, 8.5f, false);
            c["C008"] = Unit("C008", "晨星歌者", Element.Light, Role.Supporter, 2720, 830, 830, 1060, 610, 7.5f, false);
            c["C009"] = Unit("C009", "夜幕医师", Element.Dark, Role.Healer, 2880, 920, 790, 820, 670, 9.0f, false);
            c["C010"] = Unit("C010", "影缚者", Element.Dark, Role.Debuffer, 2480, 900, 730, 1120, 710, 8.0f, false);
            c["C011"] = Unit("C011", "灼红侍从", Element.Fire, Role.Supporter, 2680, 810, 850, 1020, 590, 7.5f, false);
            c["C012"] = Unit("C012", "冰镜使者", Element.Water, Role.Supporter, 2740, 800, 860, 1000, 580, 7.5f, false);
            c["C013"] = Unit("C013", "熔渣咒印", Element.Fire, Role.Debuffer, 2520, 860, 750, 1080, 690, 8.0f, false);
            c["C014"] = Unit("C014", "炉灰医师", Element.Fire, Role.Healer, 2920, 880, 810, 790, 650, 9.0f, false);
            c["C015"] = Unit("C015", "裂潮刃", Element.Water, Role.Attacker, 2180, 1200, 630, 880, 830, 9.0f, false);
            c["C016"] = Unit("C016", "堰门卫", Element.Water, Role.Defender, 3340, 720, 1180, 650, 510, 8.5f, false);
            c["C017"] = Unit("C017", "根墙守", Element.Wood, Role.Defender, 3320, 750, 1170, 670, 500, 8.5f, false);
            c["C018"] = Unit("C018", "毒棘使", Element.Wood, Role.Debuffer, 2460, 890, 720, 1110, 720, 8.0f, false);
            c["C019"] = Unit("C019", "青苔愈", Element.Wood, Role.Healer, 2860, 910, 820, 810, 640, 9.0f, false);
            c["C020"] = Unit("C020", "昼锋", Element.Light, Role.Attacker, 2220, 1170, 660, 910, 800, 9.0f, false);
            c["C021"] = Unit("C021", "眩光缚", Element.Light, Role.Debuffer, 2540, 870, 760, 1070, 680, 8.0f, false);
            c["C022"] = Unit("C022", "晨露愈", Element.Light, Role.Healer, 2940, 890, 780, 780, 680, 9.0f, false);
            c["C023"] = Unit("C023", "夜刃", Element.Dark, Role.Attacker, 2160, 1190, 620, 940, 850, 9.0f, false);
            c["C024"] = Unit("C024", "影壁", Element.Dark, Role.Defender, 3260, 730, 1150, 640, 540, 8.5f, false);
            c["C025"] = Unit("C025", "低语引", Element.Dark, Role.Supporter, 2660, 840, 820, 1080, 620, 7.5f, false);
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
            PartySkills(s);
            RoleKit(s, "E001", Role.Attacker);
            RoleKit(s, "E002", Role.Defender);
            RoleKit(s, "E003", Role.Debuffer);
            RoleKit(s, "E004", Role.Healer);
            RoleKit(s, "E005", Role.Attacker);
            RoleKit(s, "EBOSS", Role.Defender);
        }

        static void PartySkills(Dictionary<string, SkillDef> s)
        {
            s["C001_auto"] = Sk("C001_auto", "刃息", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.35f, 40, 0, null);
            s["C001_tap"] = Sk("C001_tap", "直斩", SkillType.Tap, TargetRule.RandomEnemies, 1, 1, 0.90f, 120, 6, null);
            s["C001_slide"] = Sk("C001_slide", "裂空", SkillType.Slide, TargetRule.RandomEnemies, 2, 1, 1.65f, 260, 14, "dot_flame");
            s["C001_drive"] = Sk("C001_drive", "焚城", SkillType.Drive, TargetRule.HighestAtkEnemies, 3, 1, 2.90f, 500, 0, "burst_atk");
            s["C001_leader"] = Sk("C001_leader", "锋势", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "atk_up");

            s["C002_auto"] = Sk("C002_auto", "炉步", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.28f, 30, 0, null);
            s["C002_tap"] = Sk("C002_tap", "炉门", SkillType.Tap, TargetRule.RandomEnemies, 1, 1, 0.55f, 70, 6, "shield");
            s["C002_slide"] = Sk("C002_slide", "扛线", SkillType.Slide, TargetRule.RandomEnemies, 1, 1, 0.70f, 90, 14, "taunt");
            s["C002_drive"] = Sk("C002_drive", "炉心结界", SkillType.Drive, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "shield");
            s["C002_leader"] = Sk("C002_leader", "守门", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "shield");

            s["C003_auto"] = Sk("C003_auto", "潮拍", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.22f, 24, 0, null);
            s["C003_tap"] = Heal("C003_tap", "急救", SkillType.Tap, TargetRule.LowestHpAlly, 1, 0.45f, 180, 0f, 6);
            s["C003_slide"] = Heal("C003_slide", "双潮", SkillType.Slide, TargetRule.LowestHpAlly, 2, 0.65f, 350, 0.08f, 14);
            s["C003_drive"] = Heal("C003_drive", "满潮", SkillType.Drive, TargetRule.AllAllies, 5, 0.90f, 420, 0.10f, 0);
            s["C003_leader"] = Heal("C003_leader", "潮息", SkillType.Leader, TargetRule.AllAllies, 5, 0.20f, 80, 0f, 0);

            s["C004_auto"] = Sk("C004_auto", "咒沫", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.30f, 36, 0, null);
            s["C004_tap"] = Sk("C004_tap", "深蓝印", SkillType.Tap, TargetRule.RandomEnemies, 1, 1, 0.68f, 85, 6, "dot_flame");
            s["C004_slide"] = Sk("C004_slide", "沉锚", SkillType.Slide, TargetRule.LowestHpEnemies, 1, 1, 1.25f, 200, 14, "def_down");
            s["C004_drive"] = Sk("C004_drive", "海渊锁", SkillType.Drive, TargetRule.AllEnemies, 5, 1, 1.40f, 220, 0, "def_down");
            s["C004_leader"] = Sk("C004_leader", "潮隙", SkillType.Leader, TargetRule.AllEnemies, 5, 0, 0f, 0, 0, "def_down");

            s["C005_auto"] = Sk("C005_auto", "叶响", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.24f, 28, 0, null);
            s["C005_tap"] = Sk("C005_tap", "路引", SkillType.Tap, TargetRule.LowestHpAlly, 1, 1, 0.35f, 50, 6, "haste");
            s["C005_slide"] = Sk("C005_slide", "棘径加攻", SkillType.Slide, TargetRule.AllAllies, 5, 0, 0f, 0, 14, "atk_up");
            s["C005_drive"] = Sk("C005_drive", "森语号令", SkillType.Drive, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "burst_atk");
            s["C005_leader"] = Sk("C005_leader", "引路", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "haste");

            s["C006_auto"] = Sk("C006_auto", "棘矢", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.38f, 44, 0, null);
            s["C006_tap"] = Sk("C006_tap", "连射", SkillType.Tap, TargetRule.RandomEnemies, 1, 2, 0.55f, 70, 6, null);
            s["C006_slide"] = Sk("C006_slide", "棘雨", SkillType.Slide, TargetRule.AllEnemies, 5, 1, 0.95f, 140, 14, null);
            s["C006_drive"] = Sk("C006_drive", "穿心", SkillType.Drive, TargetRule.HighestAtkEnemies, 1, 1, 3.40f, 620, 0, null);
            s["C006_leader"] = Sk("C006_leader", "猎眼", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "atk_up");

            s["C007_auto"] = Sk("C007_auto", "昼盾", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.26f, 32, 0, null);
            s["C007_tap"] = Sk("C007_tap", "守望斩", SkillType.Tap, TargetRule.RandomEnemies, 1, 1, 0.72f, 95, 6, null);
            s["C007_slide"] = Sk("C007_slide", "白昼壁", SkillType.Slide, TargetRule.AllAllies, 5, 0, 0f, 0, 14, "shield");
            s["C007_drive"] = Sk("C007_drive", "不落日", SkillType.Drive, TargetRule.RandomEnemies, 1, 1, 1.10f, 160, 0, "taunt");
            s["C007_leader"] = Sk("C007_leader", "昼线", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "shield");

            s["C008_auto"] = Sk("C008_auto", "星拍", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.25f, 26, 0, null);
            s["C008_tap"] = Heal("C008_tap", "晨歌", SkillType.Tap, TargetRule.LowestHpAlly, 1, 0.30f, 120, 0f, 6);
            s["C008_slide"] = Sk("C008_slide", "星律", SkillType.Slide, TargetRule.AllAllies, 5, 0, 0f, 0, 14, "haste");
            s["C008_drive"] = Sk("C008_drive", "启明", SkillType.Drive, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "atk_up");
            s["C008_leader"] = Sk("C008_leader", "路灯", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "haste");

            s["C009_auto"] = Sk("C009_auto", "药灯", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.20f, 22, 0, null);
            s["C009_tap"] = Heal("C009_tap", "夜诊", SkillType.Tap, TargetRule.LowestHpAlly, 1, 0.52f, 220, 0f, 6);
            s["C009_slide"] = Heal("C009_slide", "群疗", SkillType.Slide, TargetRule.AllAllies, 5, 0.40f, 160, 0.04f, 14);
            s["C009_drive"] = Heal("C009_drive", "长夜灯", SkillType.Drive, TargetRule.AllAllies, 5, 0.70f, 300, 0.12f, 0);
            s["C009_leader"] = Heal("C009_leader", "守夜", SkillType.Leader, TargetRule.AllAllies, 5, 0.16f, 60, 0f, 0);

            s["C010_auto"] = Sk("C010_auto", "影刺", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.32f, 38, 0, null);
            s["C010_tap"] = Sk("C010_tap", "缚影", SkillType.Tap, TargetRule.HighestAtkEnemies, 1, 1, 0.60f, 80, 6, "def_down");
            s["C010_slide"] = Sk("C010_slide", "缝影", SkillType.Slide, TargetRule.RandomEnemies, 3, 1, 0.88f, 130, 14, "stun");
            s["C010_drive"] = Sk("C010_drive", "影狱", SkillType.Drive, TargetRule.AllEnemies, 5, 1, 1.55f, 240, 0, "def_down");
            s["C010_leader"] = Sk("C010_leader", "弱点", SkillType.Leader, TargetRule.AllEnemies, 5, 0, 0f, 0, 0, "def_down");

            s["C011_auto"] = Sk("C011_auto", "火星", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.26f, 30, 0, null);
            s["C011_tap"] = Sk("C011_tap", "随火", SkillType.Tap, TargetRule.AllAllies, 5, 0, 0f, 0, 6, "atk_up");
            s["C011_slide"] = Sk("C011_slide", "燎旗", SkillType.Slide, TargetRule.AllAllies, 5, 0, 0f, 0, 14, "haste");
            s["C011_drive"] = Sk("C011_drive", "从焰号", SkillType.Drive, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "burst_atk");
            s["C011_leader"] = Sk("C011_leader", "跟火", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "atk_up");

            s["C012_auto"] = Sk("C012_auto", "镜片", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.23f, 26, 0, null);
            s["C012_tap"] = Sk("C012_tap", "冰镜盾", SkillType.Tap, TargetRule.LowestHpAlly, 1, 1, 0.30f, 40, 6, "shield");
            s["C012_slide"] = Sk("C012_slide", "碎镜", SkillType.Slide, TargetRule.RandomEnemies, 1, 1, 1.05f, 150, 14, "def_down");
            s["C012_drive"] = Sk("C012_drive", "冷河", SkillType.Drive, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "haste");
            s["C012_leader"] = Sk("C012_leader", "映河", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "haste");

            s["C013_auto"] = Sk("C013_auto", "渣烫", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.31f, 34, 0, null);
            s["C013_tap"] = Sk("C013_tap", "熔印", SkillType.Tap, TargetRule.RandomEnemies, 1, 1, 0.66f, 82, 6, "dot_flame");
            s["C013_slide"] = Sk("C013_slide", "渣锁", SkillType.Slide, TargetRule.HighestAtkEnemies, 1, 1, 1.18f, 190, 14, "def_down");
            s["C013_drive"] = Sk("C013_drive", "炉咒", SkillType.Drive, TargetRule.AllEnemies, 5, 1, 1.48f, 230, 0, "dot_flame");
            s["C013_leader"] = Sk("C013_leader", "热隙", SkillType.Leader, TargetRule.AllEnemies, 5, 0, 0f, 0, 0, "def_down");

            s["C014_auto"] = Sk("C014_auto", "灰拍", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.21f, 22, 0, null);
            s["C014_tap"] = Heal("C014_tap", "炉敷", SkillType.Tap, TargetRule.LowestHpAlly, 1, 0.48f, 190, 0f, 6);
            s["C014_slide"] = Heal("C014_slide", "热愈", SkillType.Slide, TargetRule.AllAllies, 5, 0.38f, 150, 0.05f, 14);
            s["C014_drive"] = Heal("C014_drive", "复燃", SkillType.Drive, TargetRule.AllAllies, 5, 0.85f, 380, 0.11f, 0);
            s["C014_leader"] = Heal("C014_leader", "暖灰", SkillType.Leader, TargetRule.AllAllies, 5, 0.18f, 70, 0f, 0);

            s["C015_auto"] = Sk("C015_auto", "潮刃", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.37f, 42, 0, null);
            s["C015_tap"] = Sk("C015_tap", "裂潮", SkillType.Tap, TargetRule.RandomEnemies, 1, 1, 0.92f, 125, 6, null);
            s["C015_slide"] = Sk("C015_slide", "二浪", SkillType.Slide, TargetRule.LowestHpEnemies, 1, 2, 1.40f, 220, 14, null);
            s["C015_drive"] = Sk("C015_drive", "海裂", SkillType.Drive, TargetRule.HighestAtkEnemies, 1, 1, 3.20f, 580, 0, "burst_atk");
            s["C015_leader"] = Sk("C015_leader", "潮锋", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "atk_up");

            s["C016_auto"] = Sk("C016_auto", "堰步", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.27f, 30, 0, null);
            s["C016_tap"] = Sk("C016_tap", "闸门", SkillType.Tap, TargetRule.RandomEnemies, 1, 1, 0.58f, 75, 6, "shield");
            s["C016_slide"] = Sk("C016_slide", "挡潮", SkillType.Slide, TargetRule.AllAllies, 5, 0, 0f, 0, 14, "shield");
            s["C016_drive"] = Sk("C016_drive", "合闸", SkillType.Drive, TargetRule.RandomEnemies, 1, 1, 1.05f, 150, 0, "taunt");
            s["C016_leader"] = Sk("C016_leader", "堰线", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "shield");

            s["C017_auto"] = Sk("C017_auto", "根拍", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.25f, 28, 0, null);
            s["C017_tap"] = Sk("C017_tap", "盘根", SkillType.Tap, TargetRule.LowestHpAlly, 1, 1, 0.50f, 60, 6, "shield");
            s["C017_slide"] = Sk("C017_slide", "墙嘲", SkillType.Slide, TargetRule.RandomEnemies, 1, 1, 0.75f, 95, 14, "taunt");
            s["C017_drive"] = Sk("C017_drive", "林壁", SkillType.Drive, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "shield");
            s["C017_leader"] = Sk("C017_leader", "守根", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "shield");

            s["C018_auto"] = Sk("C018_auto", "毒芒", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.33f, 36, 0, null);
            s["C018_tap"] = Sk("C018_tap", "棘毒", SkillType.Tap, TargetRule.LowestHpEnemies, 1, 1, 0.64f, 78, 6, "dot_flame");
            s["C018_slide"] = Sk("C018_slide", "散毒", SkillType.Slide, TargetRule.AllEnemies, 5, 1, 0.90f, 140, 14, "dot_flame");
            s["C018_drive"] = Sk("C018_drive", "枯锁", SkillType.Drive, TargetRule.AllEnemies, 5, 1, 1.42f, 210, 0, "def_down");
            s["C018_leader"] = Sk("C018_leader", "毒隙", SkillType.Leader, TargetRule.AllEnemies, 5, 0, 0f, 0, 0, "def_down");

            s["C019_auto"] = Sk("C019_auto", "苔拍", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.22f, 24, 0, null);
            s["C019_tap"] = Heal("C019_tap", "青敷", SkillType.Tap, TargetRule.LowestHpAlly, 1, 0.50f, 200, 0f, 6);
            s["C019_slide"] = Heal("C019_slide", "苔潮", SkillType.Slide, TargetRule.LowestHpAlly, 2, 0.60f, 280, 0.06f, 14);
            s["C019_drive"] = Heal("C019_drive", "林愈", SkillType.Drive, TargetRule.AllAllies, 5, 0.80f, 360, 0.09f, 0);
            s["C019_leader"] = Heal("C019_leader", "青息", SkillType.Leader, TargetRule.AllAllies, 5, 0.17f, 65, 0f, 0);

            s["C020_auto"] = Sk("C020_auto", "昼矢", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.36f, 40, 0, null);
            s["C020_tap"] = Sk("C020_tap", "锋光", SkillType.Tap, TargetRule.HighestAtkEnemies, 1, 1, 0.88f, 115, 6, null);
            s["C020_slide"] = Sk("C020_slide", "裂昼", SkillType.Slide, TargetRule.RandomEnemies, 2, 1, 1.55f, 240, 14, null);
            s["C020_drive"] = Sk("C020_drive", "日贯", SkillType.Drive, TargetRule.HighestAtkEnemies, 1, 2, 2.70f, 480, 0, "burst_atk");
            s["C020_leader"] = Sk("C020_leader", "昼势", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "atk_up");

            s["C021_auto"] = Sk("C021_auto", "眩芒", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.30f, 32, 0, null);
            s["C021_tap"] = Sk("C021_tap", "闪缚", SkillType.Tap, TargetRule.HighestAtkEnemies, 1, 1, 0.62f, 84, 6, "def_down");
            s["C021_slide"] = Sk("C021_slide", "盲环", SkillType.Slide, TargetRule.RandomEnemies, 2, 1, 1.10f, 170, 14, "dot_flame");
            s["C021_drive"] = Sk("C021_drive", "白锁", SkillType.Drive, TargetRule.AllEnemies, 5, 1, 1.50f, 225, 0, "def_down");
            s["C021_leader"] = Sk("C021_leader", "眩隙", SkillType.Leader, TargetRule.AllEnemies, 5, 0, 0f, 0, 0, "def_down");

            s["C022_auto"] = Sk("C022_auto", "露拍", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.20f, 20, 0, null);
            s["C022_tap"] = Heal("C022_tap", "晨敷", SkillType.Tap, TargetRule.LowestHpAlly, 1, 0.46f, 175, 0f, 6);
            s["C022_slide"] = Heal("C022_slide", "露潮", SkillType.Slide, TargetRule.AllAllies, 5, 0.42f, 170, 0.04f, 14);
            s["C022_drive"] = Heal("C022_drive", "启愈", SkillType.Drive, TargetRule.AllAllies, 5, 0.88f, 400, 0.10f, 0);
            s["C022_leader"] = Heal("C022_leader", "露息", SkillType.Leader, TargetRule.AllAllies, 5, 0.19f, 75, 0f, 0);

            s["C023_auto"] = Sk("C023_auto", "夜刺", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.39f, 46, 0, null);
            s["C023_tap"] = Sk("C023_tap", "暗斩", SkillType.Tap, TargetRule.RandomEnemies, 1, 2, 0.58f, 72, 6, null);
            s["C023_slide"] = Sk("C023_slide", "影雨", SkillType.Slide, TargetRule.AllEnemies, 5, 1, 1.00f, 150, 14, "dot_flame");
            s["C023_drive"] = Sk("C023_drive", "夜贯", SkillType.Drive, TargetRule.LowestHpEnemies, 1, 1, 3.30f, 600, 0, null);
            s["C023_leader"] = Sk("C023_leader", "夜势", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "atk_up");

            s["C024_auto"] = Sk("C024_auto", "壁拍", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.24f, 26, 0, null);
            s["C024_tap"] = Sk("C024_tap", "影盾", SkillType.Tap, TargetRule.LowestHpAlly, 1, 1, 0.48f, 55, 6, "shield");
            s["C024_slide"] = Sk("C024_slide", "壁嘲", SkillType.Slide, TargetRule.RandomEnemies, 1, 1, 0.78f, 100, 14, "taunt");
            s["C024_drive"] = Sk("C024_drive", "沉壁", SkillType.Drive, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "shield");
            s["C024_leader"] = Sk("C024_leader", "守影", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "shield");

            s["C025_auto"] = Sk("C025_auto", "语拍", SkillType.Auto, TargetRule.RandomEnemies, 1, 1, 0.23f, 25, 0, null);
            s["C025_tap"] = Sk("C025_tap", "低语", SkillType.Tap, TargetRule.LowestHpAlly, 1, 1, 0.32f, 45, 6, "haste");
            s["C025_slide"] = Sk("C025_slide", "引暗", SkillType.Slide, TargetRule.AllAllies, 5, 0, 0f, 0, 14, "haste");
            s["C025_drive"] = Sk("C025_drive", "夜令", SkillType.Drive, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "burst_atk");
            s["C025_leader"] = Sk("C025_leader", "耳语", SkillType.Leader, TargetRule.AllAllies, 5, 0, 0f, 0, 0, "haste");
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

        static EffectDef Fx(string id, EffectKind kind, float mag, float dur, int stack, int tier, string group)
        {
            return new EffectDef
            {
                Id = id,
                Opcode = EffectOpcodes.ForKind(kind),
                Kind = kind,
                Magnitude = mag,
                DurationSec = dur,
                MaxStack = stack,
                SourceTier = tier,
                Group = group
            };
        }

        static SkillDef Sk(string id, string name, SkillType type, TargetRule rule, int count, int hits, float coef, int flat, int drive, string effect)
        {
            return new SkillDef
            {
                Id = id, Name = name, Type = type, Target = rule, TargetCount = count,
                HitCount = hits < 1 ? 1 : hits, AtkCoef = coef, FlatPower = flat, DriveGain = drive, EffectId = effect,
                Opcode = DamageMath.ChannelOpcode(type)
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
