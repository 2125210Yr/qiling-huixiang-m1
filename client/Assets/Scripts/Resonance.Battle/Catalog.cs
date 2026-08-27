using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

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

    public static class CatalogJson
    {
        public static void TryLoadDefault()
        {
            var path = FindCatalogPath();
            if (path == null) return;
            try
            {
                Load(File.ReadAllText(path));
            }
            catch
            {
                Catalog.BuildBuiltin();
            }
        }

        public static string FindCatalogPath()
        {
            var env = Environment.GetEnvironmentVariable("RESONANCE_CONTENT");
            if (!string.IsNullOrEmpty(env))
            {
                var p = Path.Combine(env, "catalog.json");
                if (File.Exists(p)) return p;
            }
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                var a = Path.Combine(dir, "catalog.json");
                var b = Path.Combine(dir, "Content", "catalog.json");
                var c = Path.Combine(dir, "client", "Assets", "Content", "catalog.json");
                var d = Path.Combine(dir, "Assets", "Content", "catalog.json");
                if (File.Exists(a)) return a;
                if (File.Exists(b)) return b;
                if (File.Exists(c)) return c;
                if (File.Exists(d)) return d;
                dir = Directory.GetParent(dir)?.FullName;
            }
            return null;
        }

        public static void Load(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object>;
            if (root == null) throw new InvalidDataException("catalog root");
            var chars = new Dictionary<string, CharacterDef>();
            foreach (var item in AsList(root, "chars"))
            {
                var o = (Dictionary<string, object>)item;
                var def = new CharacterDef
                {
                    Id = Str(o, "id"),
                    Name = Str(o, "name"),
                    Element = (Element)Int(o, "el"),
                    Role = (Role)Int(o, "role"),
                    Hp = Int(o, "hp"),
                    Atk = Int(o, "atk"),
                    Def = Int(o, "def"),
                    Agl = Int(o, "agl"),
                    Crt = Int(o, "crt"),
                    ChargeTimeSec = Flt(o, "charge"),
                    AutoSkillId = Str(o, "auto"),
                    TapSkillId = Str(o, "tap"),
                    SlideSkillId = Str(o, "slide"),
                    DriveSkillId = Str(o, "drive"),
                    LeaderSkillId = Str(o, "leader"),
                    IsEnemy = Bool(o, "enemy"),
                    IsBoss = Bool(o, "boss"),
                    NativeStar = Int(o, "native", 5),
                    MaxStar = Int(o, "maxStar", 6),
                    UncapMax = Int(o, "uncap", 6),
                    IgnitionMax = Int(o, "ign", 12)
                };
                chars[def.Id] = def;
            }
            var skills = new Dictionary<string, SkillDef>();
            foreach (var item in AsList(root, "skills"))
            {
                var o = (Dictionary<string, object>)item;
                var def = new SkillDef
                {
                    Id = Str(o, "id"),
                    Name = Str(o, "name"),
                    Type = (SkillType)Int(o, "type"),
                    Target = (TargetRule)Int(o, "target"),
                    TargetCount = Int(o, "n"),
                    HitCount = Int(o, "hits"),
                    AtkCoef = Flt(o, "coef"),
                    FlatPower = Int(o, "flat"),
                    DriveGain = Int(o, "drive"),
                    EffectId = Str(o, "fx"),
                    HealCoef = Flt(o, "hcoef"),
                    FlatHeal = Int(o, "hflat"),
                    HealMaxHpFrac = Flt(o, "hfrac"),
                    SkillFlat = Flt(o, "sflat"),
                    PercentAtk = Flt(o, "pct"),
                    IsIgnitedVariant = Bool(o, "ign"),
                    BaseSkillId = Str(o, "base")
                };
                skills[def.Id] = def;
            }
            var effects = new Dictionary<string, EffectDef>();
            foreach (var item in AsList(root, "effects"))
            {
                var o = (Dictionary<string, object>)item;
                var def = new EffectDef
                {
                    Id = Str(o, "id"),
                    Kind = (EffectKind)Int(o, "kind"),
                    Magnitude = Flt(o, "mag"),
                    DurationSec = Flt(o, "dur"),
                    MaxStack = Int(o, "stack", 1),
                    SourceTier = Int(o, "tier"),
                    Group = Str(o, "group")
                };
                effects[def.Id] = def;
            }
            var so = root["stage"] as Dictionary<string, object>;
            var stage = new StageDef
            {
                Id = Str(so, "id"),
                Name = Str(so, "name"),
                TimeLimitSec = Flt(so, "time"),
                Wave0 = StrArr(so, "w0"),
                Wave1 = StrArr(so, "w1")
            };
            Catalog.Install(chars, skills, effects, stage);
        }

        public static string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("{\"party\":[");
            for (int i = 0; i < Catalog.DefaultParty.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(Esc(Catalog.DefaultParty[i])).Append('"');
            }
            sb.Append("],\"stage\":{");
            var st = Catalog.VerticalSliceStage;
            sb.Append("\"id\":\"").Append(Esc(st.Id)).Append("\",\"name\":\"").Append(Esc(st.Name)).Append("\",");
            sb.Append("\"time\":").Append(Num(st.TimeLimitSec)).Append(",\"w0\":");
            Arr(sb, st.Wave0);
            sb.Append(",\"w1\":");
            Arr(sb, st.Wave1);
            sb.Append("},\"effects\":[");
            var first = true;
            foreach (var e in Catalog.Effects.Values)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"id\":\"").Append(Esc(e.Id)).Append("\",\"kind\":").Append((int)e.Kind);
                sb.Append(",\"mag\":").Append(Num(e.Magnitude)).Append(",\"dur\":").Append(Num(e.DurationSec));
                sb.Append(",\"stack\":").Append(e.MaxStack).Append(",\"tier\":").Append(e.SourceTier);
                sb.Append(",\"group\":\"").Append(Esc(e.Group)).Append("\"}");
            }
            sb.Append("],\"chars\":[");
            first = true;
            foreach (var c in Catalog.Characters.Values)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"id\":\"").Append(Esc(c.Id)).Append("\",\"name\":\"").Append(Esc(c.Name)).Append("\",");
                sb.Append("\"el\":").Append((int)c.Element).Append(",\"role\":").Append((int)c.Role);
                sb.Append(",\"hp\":").Append(c.Hp).Append(",\"atk\":").Append(c.Atk).Append(",\"def\":").Append(c.Def);
                sb.Append(",\"agl\":").Append(c.Agl).Append(",\"crt\":").Append(c.Crt);
                sb.Append(",\"charge\":").Append(Num(c.ChargeTimeSec));
                sb.Append(",\"auto\":\"").Append(Esc(c.AutoSkillId)).Append("\",\"tap\":\"").Append(Esc(c.TapSkillId)).Append("\",");
                sb.Append("\"slide\":\"").Append(Esc(c.SlideSkillId)).Append("\",\"drive\":\"").Append(Esc(c.DriveSkillId)).Append("\",");
                sb.Append("\"leader\":\"").Append(Esc(c.LeaderSkillId)).Append("\",\"enemy\":").Append(c.IsEnemy ? "true" : "false");
                sb.Append(",\"boss\":").Append(c.IsBoss ? "true" : "false");
                sb.Append(",\"native\":").Append(c.NativeStar).Append(",\"maxStar\":").Append(c.MaxStar);
                sb.Append(",\"uncap\":").Append(c.UncapMax).Append(",\"ign\":").Append(c.IgnitionMax).Append('}');
            }
            sb.Append("],\"skills\":[");
            first = true;
            foreach (var s in Catalog.Skills.Values)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"id\":\"").Append(Esc(s.Id)).Append("\",\"name\":\"").Append(Esc(s.Name)).Append("\",");
                sb.Append("\"type\":").Append((int)s.Type).Append(",\"target\":").Append((int)s.Target);
                sb.Append(",\"n\":").Append(s.TargetCount).Append(",\"hits\":").Append(s.HitCount);
                sb.Append(",\"coef\":").Append(Num(s.AtkCoef)).Append(",\"flat\":").Append(s.FlatPower);
                sb.Append(",\"drive\":").Append(s.DriveGain).Append(",\"fx\":\"").Append(Esc(s.EffectId)).Append("\",");
                sb.Append("\"hcoef\":").Append(Num(s.HealCoef)).Append(",\"hflat\":").Append(s.FlatHeal);
                sb.Append(",\"hfrac\":").Append(Num(s.HealMaxHpFrac)).Append(",\"sflat\":").Append(Num(s.SkillFlat));
                sb.Append(",\"pct\":").Append(Num(s.PercentAtk)).Append(",\"ign\":").Append(s.IsIgnitedVariant ? "true" : "false");
                sb.Append(",\"base\":\"").Append(Esc(s.BaseSkillId)).Append("\"}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        static void Arr(StringBuilder sb, string[] a)
        {
            sb.Append('[');
            for (int i = 0; i < a.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(Esc(a[i])).Append('"');
            }
            sb.Append(']');
        }

        static List<object> AsList(Dictionary<string, object> o, string k)
        {
            if (o == null || !o.TryGetValue(k, out var v) || !(v is List<object> list))
                return new List<object>();
            return list;
        }

        static string Str(Dictionary<string, object> o, string k)
        {
            if (o == null || !o.TryGetValue(k, out var v) || v == null) return "";
            return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
        }

        static int Int(Dictionary<string, object> o, string k, int fallback = 0)
        {
            if (o == null || !o.TryGetValue(k, out var v) || v == null) return fallback;
            return Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        static float Flt(Dictionary<string, object> o, string k)
        {
            if (o == null || !o.TryGetValue(k, out var v) || v == null) return 0f;
            return Convert.ToSingle(v, CultureInfo.InvariantCulture);
        }

        static bool Bool(Dictionary<string, object> o, string k)
        {
            if (o == null || !o.TryGetValue(k, out var v) || v == null) return false;
            if (v is bool b) return b;
            return string.Equals(Convert.ToString(v), "true", StringComparison.OrdinalIgnoreCase);
        }

        static string[] StrArr(Dictionary<string, object> o, string k)
        {
            var list = AsList(o, k);
            var a = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
                a[i] = Convert.ToString(list[i], CultureInfo.InvariantCulture) ?? "";
            return a;
        }

        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        static string Num(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);
    }

    internal static class MiniJson
    {
        public static object Parse(string s)
        {
            var p = new Parser(s ?? "");
            return p.ReadValue();
        }

        sealed class Parser
        {
            readonly string _s;
            int _i;
            public Parser(string s) { _s = s; }

            public object ReadValue()
            {
                Skip();
                if (_i >= _s.Length) return null;
                var c = _s[_i];
                if (c == '{') return ReadObj();
                if (c == '[') return ReadArr();
                if (c == '"') return ReadStr();
                if (c == 't' || c == 'f') return ReadBool();
                if (c == 'n') { _i += 4; return null; }
                return ReadNum();
            }

            Dictionary<string, object> ReadObj()
            {
                _i++;
                var d = new Dictionary<string, object>();
                Skip();
                while (_i < _s.Length && _s[_i] != '}')
                {
                    Skip();
                    var key = ReadStr();
                    Skip();
                    if (_i < _s.Length && _s[_i] == ':') _i++;
                    d[key] = ReadValue();
                    Skip();
                    if (_i < _s.Length && _s[_i] == ',') _i++;
                    Skip();
                }
                if (_i < _s.Length && _s[_i] == '}') _i++;
                return d;
            }

            List<object> ReadArr()
            {
                _i++;
                var list = new List<object>();
                Skip();
                while (_i < _s.Length && _s[_i] != ']')
                {
                    list.Add(ReadValue());
                    Skip();
                    if (_i < _s.Length && _s[_i] == ',') _i++;
                    Skip();
                }
                if (_i < _s.Length && _s[_i] == ']') _i++;
                return list;
            }

            string ReadStr()
            {
                if (_s[_i] != '"') return "";
                _i++;
                var sb = new StringBuilder();
                while (_i < _s.Length)
                {
                    var c = _s[_i++];
                    if (c == '"') break;
                    if (c == '\\' && _i < _s.Length)
                    {
                        var n = _s[_i++];
                        sb.Append(n == 'n' ? '\n' : n);
                    }
                    else sb.Append(c);
                }
                return sb.ToString();
            }

            object ReadNum()
            {
                var start = _i;
                if (_i < _s.Length && _s[_i] == '-') _i++;
                while (_i < _s.Length && ((_s[_i] >= '0' && _s[_i] <= '9') || _s[_i] == '.' || _s[_i] == 'e' || _s[_i] == 'E' || _s[_i] == '+'))
                    _i++;
                var t = _s.Substring(start, _i - start);
                if (t.IndexOf('.') >= 0 || t.IndexOf('e') >= 0 || t.IndexOf('E') >= 0)
                    return double.Parse(t, CultureInfo.InvariantCulture);
                return long.Parse(t, CultureInfo.InvariantCulture);
            }

            bool ReadBool()
            {
                if (_s.Substring(_i, 4) == "true") { _i += 4; return true; }
                _i += 5;
                return false;
            }

            void Skip()
            {
                while (_i < _s.Length)
                {
                    var c = _s[_i];
                    if (c == ' ' || c == '\n' || c == '\r' || c == '\t') _i++;
                    else break;
                }
            }
        }
    }
}
