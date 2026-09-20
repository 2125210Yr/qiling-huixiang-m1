using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Resonance.Battle
{
    public sealed class ExpeditionRelicDef
    {
        public string Id;
        public string Name;
        public string Family;
        public string ShortDescription;
        public string Description;
        public string[] PrerequisiteIds = new string[0];
        public string[] AnyPrerequisiteIds = new string[0];
        public bool IsCore;
        public bool IsAmplifier;

        public bool IsEligible(IEnumerable<string> ownedIds)
        {
            var owned = new HashSet<string>(ownedIds ?? new string[0], StringComparer.Ordinal);
            if (owned.Contains(Id)) return false;
            foreach (var id in PrerequisiteIds) if (!owned.Contains(id)) return false;
            if (AnyPrerequisiteIds.Length == 0) return true;
            foreach (var id in AnyPrerequisiteIds) if (owned.Contains(id)) return true;
            return false;
        }

        public ExpeditionRelicDef DeepClone()
        {
            var copy = (ExpeditionRelicDef)MemberwiseClone();
            copy.PrerequisiteIds = (string[])PrerequisiteIds.Clone();
            copy.AnyPrerequisiteIds = (string[])AnyPrerequisiteIds.Clone();
            return copy;
        }
    }

    /// <summary>
    /// Original synthetic content, DESIGN_CANDIDATE_V1. This table neither reads nor installs Catalog data.
    /// FormulaProfile deliberately reuses the supported JP candidate as this prototype's chosen mathematics.
    /// </summary>
    public static class ExpeditionContent
    {
        public const string Version = "original-expedition-content-v0.1.0";
        public const string RulesetId = "original-expedition-v01";
        public const string ChapterId = "silent-theatre";
        public const string ChapterName = "失声剧院";
        public const string NumericalSource = "DESIGN_CANDIDATE_V1; ORIGINAL-EXPEDITION-MVP v0.1 sections 5-8";
        public const string SinglePreset = "single";
        public const string SweepPreset = "sweep";
        static readonly string[] Party = { "OE_POINT", "OE_GUARD", "OE_HEALER", "OE_BLADE", "OE_GUIDE" };
        static readonly string[] Nodes = { "N1", "N2-backstage", "N2-audience", "N4", "N5", "N7" };
        static readonly ExpeditionRelicDef[] RelicTable = BuildRelics();
        static readonly string Hash = BuildContentHash();

        public static string ContentHash => Hash;
        public static string[] PartyIds => (string[])Party.Clone();
        public static string[] BattleNodeIds => (string[])Nodes.Clone();
        public static ExpeditionRelicDef[] Relics
        {
            get
            {
                var copy = new ExpeditionRelicDef[RelicTable.Length];
                for (int i = 0; i < copy.Length; i++) copy[i] = RelicTable[i].DeepClone();
                return copy;
            }
        }

        public static ExpeditionRelicDef FindRelic(string id)
        {
            foreach (var relic in RelicTable) if (relic.Id == id) return relic.DeepClone();
            return null;
        }

        public static int[] GetPartyMaxHp(string presetId)
        {
            ValidatePreset(presetId);
            var chars = CreateCharacters();
            var hp = new int[Party.Length];
            for (int i = 0; i < hp.Length; i++) hp[i] = chars[i].Hp;
            return hp;
        }

        public static void ValidatePreset(string presetId)
        {
            if (presetId != SinglePreset && presetId != SweepPreset)
                throw new ArgumentException("Unknown expedition preset: " + presetId, nameof(presetId));
        }

        public static CharacterDef[] CreateCharacters()
        {
            return new[] {
                Character("OE_POINT", "烬羽", Role.Attacker, 5200, 700, 280, 420, 7f),
                Character("OE_GUARD", "砚盾", Role.Defender, 6800, 420, 480, 240, 9f),
                Character("OE_HEALER", "清弦", Role.Healer, 5600, 360, 300, 300, 8f),
                Character("OE_BLADE", "逐锋", Role.Attacker, 5000, 720, 260, 460, 6.5f),
                Character("OE_GUIDE", "引灯", Role.Supporter, 5400, 460, 320, 340, 8f),
                Character("OE_USHER", "缄默卫士", Role.Defender, 8200, 450, 500, 200, 11f, true),
                Character("OE_CHORUS", "失音唱偶", Role.Attacker, 4500, 390, 160, 300, 10f, true),
                Character("OE_PROMPTER", "执谱监场", Role.Supporter, 14000, 540, 330, 200, 12f, true),
                Character("OE_BOSS", "白面指挥者", Role.Attacker, 45000, 760, 350, 200, 15f, true, true),
                Character("OE_MASK", "共鸣面具", Role.Supporter, 7000, 0, 200, 0, 999f, true)
            };
        }

        static CharacterDef Character(string id, string name, Role role, int hp, int atk, int def, int agl,
            float charge, bool enemy = false, bool boss = false)
        {
            return new CharacterDef { Id = id, Name = name, Role = role, Element = Element.Fire,
                Hp = hp, Atk = atk, Def = def, Agl = agl, Crt = enemy ? 120 : 250,
                ChargeTimeSec = charge, AutoSkillId = id + "_auto", TapSkillId = id + "_tap",
                SlideSkillId = id + "_slide", DriveSkillId = null, LeaderSkillId = null,
                IsEnemy = enemy, IsBoss = boss, NativeStar = 1, MaxStar = 1, UncapMax = 0, IgnitionMax = 0 };
        }

        public static SkillDef[] CreateSkills(string presetId)
        {
            ValidatePreset(presetId);
            var skills = new List<SkillDef>();
            foreach (var c in CreateCharacters())
            {
                var mask = c.Id == "OE_MASK";
                skills.Add(Damage(c.Id + "_auto", "普攻", SkillType.Auto, TargetRule.RandomEnemies,
                    1, mask ? 0f : (c.IsEnemy ? 0.36f : 0.28f), mask ? 0 : 20));
            }
            AddPrimary(skills, Damage("OE_POINT_tap", presetId == SweepPreset ? "双线追击" : "定点破音",
                SkillType.Tap, TargetRule.RandomEnemies, presetId == SweepPreset ? 2 : 1,
                presetId == SweepPreset ? 0.8f : 1.6f, presetId == SweepPreset ? 60 : 120));
            AddPrimary(skills, new SkillDef { Id = "OE_GUARD_tap", Name = "全队屏障", Type = SkillType.Tap,
                Target = TargetRule.AllAllies, TargetCount = 5, HitCount = 1,
                EffectId = "OE_TEAM_SHIELD", Opcode = EffectOpcodes.DmgTap });
            AddPrimary(skills, new SkillDef { Id = "OE_HEALER_tap", Name = "回声疗愈", Type = SkillType.Tap,
                Target = TargetRule.AllAllies, TargetCount = 5, HitCount = 1, HealMaxHpFrac = 0.15f,
                FlatHeal = 80, Opcode = EffectOpcodes.DmgTap });
            AddPrimary(skills, Damage("OE_BLADE_tap", "破幕突刺", SkillType.Tap, TargetRule.RandomEnemies, 1, 1.5f, 110));
            AddPrimary(skills, new SkillDef { Id = "OE_GUIDE_tap", Name = "领奏信标", Type = SkillType.Tap,
                Target = TargetRule.AllAllies, TargetCount = 5, HitCount = 1,
                EffectId = "OE_TEAM_ATK", Opcode = EffectOpcodes.DmgTap });
            AddPrimary(skills, Damage("OE_USHER_tap", "沉重敲击", SkillType.Tap, TargetRule.RandomEnemies, 1, 0.85f, 60));
            AddPrimary(skills, Damage("OE_CHORUS_tap", "失音尖啸", SkillType.Tap, TargetRule.RandomEnemies, 1, 0.8f, 45));
            AddPrimary(skills, Damage("OE_PROMPTER_tap", "错拍重音", SkillType.Tap, TargetRule.AllEnemies, 5, 0.65f, 80));
            AddPrimary(skills, Damage("OE_BOSS_tap", "指挥杖", SkillType.Tap, TargetRule.RandomEnemies, 1, 0.9f, 90));
            AddPrimary(skills, Damage("OE_MASK_tap", "静默", SkillType.Tap, TargetRule.RandomEnemies, 1, 0f, 0));
            skills.Add(Damage("OE_BOSS_echo", "终幕回响", SkillType.Tap, TargetRule.AllEnemies, 5, 1.3f, 120));
            return skills.ToArray();
        }

        static SkillDef Damage(string id, string name, SkillType type, TargetRule target, int targets, float coef, int flat)
        {
            return new SkillDef { Id = id, Name = name, Type = type, Target = target, TargetCount = targets,
                HitCount = 1, AtkCoef = coef, FlatPower = flat, DriveGain = 0,
                RequireTags = new string[0], Opcode = DamageMath.ChannelOpcode(type) };
        }

        static void AddPrimary(List<SkillDef> skills, SkillDef tap)
        {
            skills.Add(tap);
            // Kept as an internal equivalent channel; the original-mode HUD exposes only one primary action.
            var slide = Catalog.CloneSkill(tap);
            slide.Id = tap.Id.Substring(0, tap.Id.Length - 4) + "_slide";
            slide.Type = SkillType.Slide;
            slide.Opcode = EffectOpcodes.DmgSlide;
            skills.Add(slide);
        }

        public static EffectDef[] CreateEffects()
        {
            return new[] {
                new EffectDef { Id = "OE_TEAM_SHIELD", Kind = EffectKind.Shield, Opcode = EffectOpcodes.ShieldApply,
                    HasTarget = true, Target = TargetRule.AllAllies, Side = TargetSide.Ally,
                    Magnitude = 0.22f, DurationSec = 8f, MaxStack = 1, SourceTier = 1, Group = "oe.team.shield" },
                new EffectDef { Id = "OE_TEAM_ATK", Kind = EffectKind.AtkBuff, Opcode = EffectOpcodes.StatusApply,
                    HasTarget = true, Target = TargetRule.AllAllies, Side = TargetSide.Ally,
                    Magnitude = 0.20f, DurationSec = 7f, MaxStack = 1, SourceTier = 1, Group = "oe.team.atk" }
            };
        }

        public static StageDef CreateStage(string nodeId)
        {
            string name;
            string[] wave;
            switch (nodeId)
            {
                case "N1": name = "前厅"; wave = new[] { "OE_CHORUS", "OE_USHER" }; break;
                case "N2-backstage": name = "后台 · 少量耐打敌人"; wave = new[] { "OE_USHER", "OE_USHER" }; break;
                case "N2-audience": name = "观众席 · 多名脆弱敌人"; wave = new[] { "OE_CHORUS", "OE_CHORUS", "OE_CHORUS", "OE_CHORUS" }; break;
                case "N4": name = "排练厅"; wave = new[] { "OE_PROMPTER", "OE_CHORUS", "OE_CHORUS" }; break;
                case "N5": name = "返场回廊"; wave = new[] { "OE_CHORUS", "OE_CHORUS", "OE_USHER", "OE_CHORUS" }; break;
                case "N7": name = "白面指挥者"; wave = new[] { "OE_BOSS", "OE_MASK", "OE_MASK" }; break;
                default: throw new ArgumentException("Not an expedition battle node: " + nodeId, nameof(nodeId));
            }
            return new StageDef { Id = nodeId, Name = name, TimeLimitSec = nodeId == "N7" ? 300f : 180f,
                Wave0 = wave, Wave1 = new string[0], Difficulty = nodeId == "N7" ? 3 : nodeId == "N4" ? 2 : 1 };
        }

        public static BattleClockPolicy CreateClocks()
        {
            return new BattleClockPolicy { SlideCdSec = 8f, SlideShowtimeHoldSec = 0f,
                DriveCastHoldSec = 0f, WaveAdvanceHoldSec = 0f,
                NumericStatus = FormulaStatus.Ok, NumericCode = "ORIGINAL_DESIGN_CANDIDATE_V1" };
        }

        static ExpeditionRelicDef[] BuildRelics()
        {
            return new[] {
                Relic("A01", "蓄能屏障", "护盾实际吸收敌伤后蓄能；主动技能释放反击震荡。"),
                Relic("A02", "厚壁", "全队产生的基础护盾提高 25%。", "A01"),
                Relic("A03", "扩散震荡", "反击震荡向另外至多两名敌人扩散 50% 力度。", "A01"),
                Relic("A04", "过载共振", "震荡最多消耗三份蓄能，以消耗量的 200% 反击。", "A01", new[] { "A02", "A03" }),
                Relic("B01", "散射刻印", "原生单体伤害额外向另一敌人散射首击力度的 40%。"),
                Relic("B02", "折射刃", "散射力度从 40% 提高到 70%。", "B01"),
                Relic("B03", "分光", "散射可命中另外两名不同敌人。", "B01"),
                Relic("B04", "破面回响", "原生单体技能击杀后，向存活敌人再回响首击力度的 100%。", "B01", new[] { "B02", "B03" }),
                Relic("C01", "接力信号", "成功主动技能为下一名存活且未满充能队员增加 12 点充能。"),
                Relic("C02", "流畅节拍", "接力充能从 12 点提高到 20 点。", "C01"),
                Relic("C03", "三人和声", "三名不同队员完成主动技能后，全队存活成员增加 10 点充能。", "C01"),
                Relic("C04", "强奏", "形成和声后，下一次主动伤害技能增加 120% 同通道伤害。", "C01", null, "C03")
            };
        }

        static ExpeditionRelicDef Relic(string id, string name, string description, string prerequisite = null,
            string[] any = null, string secondPrerequisite = null)
        {
            return new ExpeditionRelicDef { Id = id, Name = name, Family = id.Substring(0, 1),
                ShortDescription = description, Description = Detail(id), IsCore = id.EndsWith("01", StringComparison.Ordinal),
                IsAmplifier = id.EndsWith("04", StringComparison.Ordinal),
                PrerequisiteIds = prerequisite == null ? new string[0] : secondPrerequisite == null
                    ? new[] { prerequisite } : new[] { prerequisite, secondPrerequisite },
                AnyPrerequisiteIds = any ?? new string[0] };
        }

        static string Detail(string id)
        {
            switch (id)
            {
                case "A01": return "敌方原生攻击被己方护盾实际吸收多少，就积累多少队伍蓄能。阈值 T 为开局队员平均最大生命的 20%，取整且至少 1；最多储存 3T。成功完成一个主动技能后，有至少 1T 时消耗 1T，以消耗量的 100% 对集火目标（失效则优先首领）反击一次。每场清零。衍生伤害不暴击、不产生新遗物连锁。无前置。";
                case "A02": return "前置：蓄能屏障 A01。本队产生的基础护盾数量增加 25%；只增加新护盾，不直接增加蓄能。护盾被替换、过期或尚未被打掉的部分不算吸收。";
                case "A03": return "前置：蓄能屏障 A01。每次震荡向另外至多两名存活敌人，各追加主震荡力度的 50%。按稳定敌方槽位选择，主目标不重复命中；扩散不暴击、不再次触发遗物。";
                case "A04": return "前置：A01，且持有 A02 或 A03。每次震荡最多消耗三个完整 T，主震荡力度为实际消耗量的 200%；不足 1T 不触发，零头保留。本规则替代 A01 的一次 1T 消耗，不额外再触发一次 A01。";
                case "B01": return "己方原生单体直接伤害技能（普攻、主动）每次施法至多散射一次，对另一名存活敌人追加首个有效主命中力度的 40%。取原目标防御与暴击结算后、扣盾扣血前的值；衍生目标照常扣盾扣血，不再次套防御、暴击或源增伤。仅一个敌人时不触发。群攻、持续伤害、衍生伤害不能触发。无前置。";
                case "B02": return "前置：散射刻印 B01。散射比例从 40% 替换为 70%，不是在原散射上再乘 1.7。仍然每次原生单体施法至多追加一次散射处理。";
                case "B03": return "前置：散射刻印 B01。散射额外目标上限从一个变为两个；两者不能相同，也不能是原生主目标。没有足够存活敌人时只命中实际可用目标。";
                case "B04": return "前置：B01，且持有 B02 或 B03。原生单体技能本次施法造成至少一次击杀时，向一名仍存活敌人追加首个有效主命中力度的 100%。优先首领，否则稳定槽位；每次原生施法至多一次。衍生击杀不再引发回响或散射。";
                case "C01": return "己方成功完成主动技能后，按固定出场顺序循环查找下一名存活且充能未满的其他队员，增加 12 点充能，上限 100。找不到就不触发。被拒绝指令、普通自动攻击与敌方技能不产生接力；充能不自动施法、不跳过冷却。无前置。";
                case "C02": return "前置：接力信号 C01。接力量从 12 点替换为 20 点，充能上限仍为 100；不降低冷却，也不为同一技能创建额外连锁。";
                case "C03": return "前置：接力信号 C01。三名不同队员各成功完成主动技能，形成一次和声：全队存活队员增加 10 点充能，上限 100，然后清空本轮三人记录。同一人重复施法不会增加不同人数。";
                case "C04": return "前置：C01 和 C03。形成和声后，储存一份强奏，使下一次己方原生主动伤害技能获得 +120% 同通道伤害加成。成功造成合法伤害才消费；治疗和护盾不消费，不叠加储存。形成和声的当前技能不享受这份新强奏。";
                default: throw new ArgumentException("Unknown relic: " + id);
            }
        }

        static string BuildContentHash()
        {
            var stages = new StageDef[Nodes.Length];
            for (int i = 0; i < Nodes.Length; i++) stages[i] = CreateStage(Nodes[i]);
            return Fingerprint(new object[] { Version, RulesetId, ChapterId, Party, CreateCharacters(),
                CreateSkills(SinglePreset), CreateSkills(SweepPreset), CreateEffects(), stages, RelicTable,
                new ExpeditionRelicParameters(), new BossEncounterDef(), CreateClocks(),
                FormulaProfile.JP_LEGACY_EMPIRICAL, false, false });
        }

        /// <summary>Stable invariant SHA-256 over public-field content DTOs, including nested arrays and numbers.</summary>
        public static string Fingerprint(object value)
        {
            var builder = new StringBuilder();
            AppendCanonical(builder, value);
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) result.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        static void AppendCanonical(StringBuilder output, object value)
        {
            if (value == null) { output.Append("null;"); return; }
            if (value is string text) { output.Append('s').Append(text.Length).Append(':').Append(text); return; }
            var type = value.GetType();
            if (type.IsEnum) { output.Append('e').Append(type.Name).Append(':').Append(Convert.ToInt64(value, CultureInfo.InvariantCulture)).Append(';'); return; }
            if (value is float f) { output.Append('f').Append(f.ToString("R", CultureInfo.InvariantCulture)).Append(';'); return; }
            if (value is double d) { output.Append('d').Append(d.ToString("R", CultureInfo.InvariantCulture)).Append(';'); return; }
            if (type.IsPrimitive) { output.Append(type.Name).Append(':').Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append(';'); return; }
            if (value is IEnumerable sequence)
            {
                output.Append('[');
                foreach (var item in sequence) AppendCanonical(output, item);
                output.Append(']'); return;
            }
            output.Append(type.Name).Append('{');
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            Array.Sort(fields, (a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));
            foreach (var field in fields) { AppendCanonical(output, field.Name); AppendCanonical(output, field.GetValue(value)); }
            output.Append('}');
        }
    }
}
