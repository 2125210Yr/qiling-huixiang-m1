using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Resonance.Battle
{
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
                // Keep the last good live snapshot (builtin or custom A).
                // Rebuilding builtin here would discard A (X04 / V02).
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

            var baseline = Catalog.CreateBuiltinTables();
            var candidate = Catalog.CloneTables(baseline);
            var chars = candidate.Characters;
            var skills = candidate.Skills;
            var effects = candidate.Effects;

            foreach (var item in AsList(root, "chars"))
            {
                var o = item as Dictionary<string, object>;
                if (o == null) continue;
                var id = Str(o, "id");
                if (string.IsNullOrEmpty(id)) continue;
                if (chars.TryGetValue(id, out var existing) && existing != null)
                    OverlayChar(existing, o);
                else
                    chars[id] = ReadChar(o);
            }
            foreach (var item in AsList(root, "skills"))
            {
                var o = item as Dictionary<string, object>;
                if (o == null) continue;
                var id = Str(o, "id");
                if (string.IsNullOrEmpty(id)) continue;
                if (skills.TryGetValue(id, out var existing) && existing != null)
                    OverlaySkill(existing, o);
                else
                    skills[id] = ReadSkill(o);
            }
            foreach (var item in AsList(root, "effects"))
            {
                var o = item as Dictionary<string, object>;
                if (o == null) continue;
                var id = Str(o, "id");
                if (string.IsNullOrEmpty(id)) continue;
                if (effects.TryGetValue(id, out var existing) && existing != null)
                    OverlayEffect(existing, o);
                else
                    effects[id] = ReadEffect(o);
            }

            FillPlayableSlice(chars, skills, baseline.Skills);

            StageDef[] stages = null;
            var stageList = AsList(root, "stages");
            if (stageList.Count > 0)
            {
                var kept = new List<StageDef>();
                for (int i = 0; i < stageList.Count; i++)
                {
                    var row = stageList[i] as Dictionary<string, object>;
                    if (row == null) continue;
                    kept.Add(ReadStage(row));
                }
                if (kept.Count > 0) stages = kept.ToArray();
            }
            StageDef stage = null;
            if (root.TryGetValue("stage", out var sv) && sv is Dictionary<string, object> so)
                stage = ReadStage(so);
            if (stage == null && stages != null && stages.Length > 0)
                stage = stages[0];
            if (stage == null)
                stage = Catalog.CloneStage(baseline.Stage);
            if (stage == null)
                throw new InvalidDataException("catalog stage");
            candidate.Stage = stage;
            candidate.Stages = stages;
            Catalog.ActivateCandidate(candidate, baseline);
        }

        static void FillPlayableSlice(
            Dictionary<string, CharacterDef> chars,
            Dictionary<string, SkillDef> skills,
            Dictionary<string, SkillDef> baselineSkills)
        {
            if (chars == null || skills == null) return;
            if (chars.TryGetValue("C001", out var c001) && c001 != null)
            {
                // Do not clobber a JSON overlay name (V02 custom A). Only fill a blank slice.
                if (string.IsNullOrEmpty(c001.Name)) c001.Name = "冰刃";
                if (c001.Element == 0 && c001.Role == 0)
                {
                    c001.Element = Element.Fire;
                    c001.Role = Role.Attacker;
                }
            }
            foreach (var kv in chars)
            {
                var c = kv.Value;
                if (c == null) continue;
                var id = string.IsNullOrEmpty(c.Id) ? kv.Key : c.Id;
                if (string.IsNullOrEmpty(id)) continue;
                if (string.IsNullOrEmpty(c.Id)) c.Id = id;
                if (string.IsNullOrEmpty(c.AutoSkillId)) c.AutoSkillId = id + "_auto";
                if (string.IsNullOrEmpty(c.TapSkillId)) c.TapSkillId = id + "_tap";
                if (string.IsNullOrEmpty(c.SlideSkillId)) c.SlideSkillId = id + "_slide";
                if (string.IsNullOrEmpty(c.DriveSkillId)) c.DriveSkillId = id + "_drive";
                if (string.IsNullOrEmpty(c.LeaderSkillId)) c.LeaderSkillId = id + "_leader";
                EnsureSkill(skills, c.AutoSkillId, baselineSkills);
                EnsureSkill(skills, c.TapSkillId, baselineSkills);
                EnsureSkill(skills, c.SlideSkillId, baselineSkills);
                EnsureSkill(skills, c.DriveSkillId, baselineSkills);
                EnsureSkill(skills, c.LeaderSkillId, baselineSkills);
            }
        }

        static void EnsureSkill(
            Dictionary<string, SkillDef> skills,
            string id,
            Dictionary<string, SkillDef> baselineSkills)
        {
            if (string.IsNullOrEmpty(id) || skills == null || skills.ContainsKey(id)) return;
            if (baselineSkills != null && baselineSkills.TryGetValue(id, out var src) && src != null)
            {
                skills[id] = Catalog.CloneSkill(src);
                return;
            }
            skills[id] = new SkillDef { Id = id, Name = id, HitCount = 1, TargetCount = 1 };
        }

        static CharacterDef ReadChar(Dictionary<string, object> o)
        {
            return new CharacterDef
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
                ChargeTimeSec = Flt(o, "charge", 9f),
                AutoSkillId = Str(o, "auto"),
                TapSkillId = Str(o, "tap"),
                SlideSkillId = Str(o, "slide"),
                DriveSkillId = Str(o, "drive"),
                LeaderSkillId = Str(o, "leader"),
                IsEnemy = Bool(o, "enemy"),
                IsBoss = Bool(o, "boss"),
                BattleLevel = Int(o, "battleLv", 1),
                NativeStar = Int(o, "native", 5),
                MaxStar = Int(o, "maxStar", 6),
                UncapMax = Int(o, "uncap", 6),
                IgnitionMax = Int(o, "ign", 12)
            };
        }

        static void OverlayChar(CharacterDef dst, Dictionary<string, object> o)
        {
            if (dst == null || o == null) return;
            if (HasText(o, "name")) dst.Name = Str(o, "name");
            if (HasNum(o, "el")) dst.Element = (Element)Int(o, "el");
            if (HasNum(o, "role")) dst.Role = (Role)Int(o, "role");
            if (HasNum(o, "hp")) dst.Hp = Int(o, "hp");
            if (HasNum(o, "atk")) dst.Atk = Int(o, "atk");
            if (HasNum(o, "def")) dst.Def = Int(o, "def");
            if (HasNum(o, "agl")) dst.Agl = Int(o, "agl");
            if (HasNum(o, "crt")) dst.Crt = Int(o, "crt");
            if (HasNum(o, "charge")) dst.ChargeTimeSec = Flt(o, "charge");
            if (HasText(o, "auto")) dst.AutoSkillId = Str(o, "auto");
            if (HasText(o, "tap")) dst.TapSkillId = Str(o, "tap");
            if (HasText(o, "slide")) dst.SlideSkillId = Str(o, "slide");
            if (HasText(o, "drive")) dst.DriveSkillId = Str(o, "drive");
            if (HasText(o, "leader")) dst.LeaderSkillId = Str(o, "leader");
            if (HasKey(o, "enemy")) dst.IsEnemy = Bool(o, "enemy");
            if (HasKey(o, "boss")) dst.IsBoss = Bool(o, "boss");
            if (HasNum(o, "battleLv")) dst.BattleLevel = Int(o, "battleLv", 1);
            if (HasNum(o, "native")) dst.NativeStar = Int(o, "native", 5);
            if (HasNum(o, "maxStar")) dst.MaxStar = Int(o, "maxStar", 6);
            if (HasNum(o, "uncap")) dst.UncapMax = Int(o, "uncap", 6);
            if (HasNum(o, "ign")) dst.IgnitionMax = Int(o, "ign", 12);
        }

        static SkillDef ReadSkill(Dictionary<string, object> o)
        {
            return new SkillDef
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
                BaseSkillId = Str(o, "base"),
                Opcode = Str(o, "op"),
                SlideRank = Int(o, "slideRank"),
                SlideSkillLv = Int(o, "slideLv"),
                SlideSkillLvMax = Int(o, "slideLvMax")
            };
        }

        static void OverlaySkill(SkillDef dst, Dictionary<string, object> o)
        {
            if (dst == null || o == null) return;
            if (HasText(o, "name")) dst.Name = Str(o, "name");
            if (HasNum(o, "type")) dst.Type = (SkillType)Int(o, "type");
            if (HasNum(o, "target")) dst.Target = (TargetRule)Int(o, "target");
            if (HasNum(o, "n")) dst.TargetCount = Int(o, "n");
            if (HasNum(o, "hits")) dst.HitCount = Int(o, "hits");
            if (HasNum(o, "coef")) dst.AtkCoef = Flt(o, "coef");
            if (HasNum(o, "flat")) dst.FlatPower = Int(o, "flat");
            if (HasNum(o, "drive")) dst.DriveGain = Int(o, "drive");
            if (HasText(o, "fx")) dst.EffectId = Str(o, "fx");
            if (HasNum(o, "hcoef")) dst.HealCoef = Flt(o, "hcoef");
            if (HasNum(o, "hflat")) dst.FlatHeal = Int(o, "hflat");
            if (HasNum(o, "hfrac")) dst.HealMaxHpFrac = Flt(o, "hfrac");
            if (HasNum(o, "sflat")) dst.SkillFlat = Flt(o, "sflat");
            if (HasNum(o, "pct")) dst.PercentAtk = Flt(o, "pct");
            if (HasKey(o, "ign")) dst.IsIgnitedVariant = Bool(o, "ign");
            if (HasText(o, "base")) dst.BaseSkillId = Str(o, "base");
            if (HasText(o, "op")) dst.Opcode = Str(o, "op");
            if (HasNum(o, "slideRank")) dst.SlideRank = Int(o, "slideRank");
            if (HasNum(o, "slideLv")) dst.SlideSkillLv = Int(o, "slideLv");
            if (HasNum(o, "slideLvMax")) dst.SlideSkillLvMax = Int(o, "slideLvMax");
        }

        static EffectDef ReadEffect(Dictionary<string, object> o)
        {
            var fx = new EffectDef
            {
                Id = Str(o, "id"),
                Opcode = Str(o, "op"),
                Kind = (EffectKind)Int(o, "kind"),
                Magnitude = Flt(o, "mag"),
                DurationSec = Flt(o, "dur"),
                MaxStack = Int(o, "stack", 1),
                SourceTier = Int(o, "tier"),
                Group = Str(o, "group")
            };
            ApplyEffectTargetJson(fx, o);
            if (HasNum(o, "side")) fx.Side = (TargetSide)Int(o, "side");
            ApplyLifecycleJson(fx, o);
            return fx;
        }

        static void OverlayEffect(EffectDef dst, Dictionary<string, object> o)
        {
            if (dst == null || o == null) return;
            if (HasText(o, "op")) dst.Opcode = Str(o, "op");
            if (HasNum(o, "kind")) dst.Kind = (EffectKind)Int(o, "kind");
            if (HasNum(o, "mag")) dst.Magnitude = Flt(o, "mag");
            if (HasNum(o, "dur")) dst.DurationSec = Flt(o, "dur");
            if (HasNum(o, "stack")) dst.MaxStack = Int(o, "stack", 1);
            if (HasNum(o, "tier")) dst.SourceTier = Int(o, "tier");
            if (HasText(o, "group")) dst.Group = Str(o, "group");
            ApplyEffectTargetJson(dst, o);
            if (HasNum(o, "side")) dst.Side = (TargetSide)Int(o, "side");
            ApplyLifecycleJson(dst, o);
        }

        /// <summary>
        /// Effect target keys: missing / false / true are distinct.
        /// hasTarget:false clears HasTarget (overlay can cancel a prior override).
        /// hasTarget:true requires a defined numeric TargetRule — never silent Self.
        /// target without hasTarget is compat: HasTarget=true and parse target.
        /// Both keys absent: leave constructed/cloned HasTarget/Target (inherit skill).
        /// </summary>
        static void ApplyEffectTargetJson(EffectDef fx, Dictionary<string, object> o)
        {
            if (fx == null || o == null) return;
            var declaredHasTarget = HasKey(o, "hasTarget");
            var declaredTarget = HasKey(o, "target");
            if (!declaredHasTarget && !declaredTarget)
                return;

            if (declaredHasTarget)
            {
                if (!HasBool(o, "hasTarget"))
                    throw new InvalidDataException("effect hasTarget must be true or false" + EffectIdSuffix(fx, o));
                if (!Bool(o, "hasTarget"))
                {
                    fx.HasTarget = false;
                    return;
                }
                if (!TryReadTargetRule(o, out var rule))
                    throw new InvalidDataException("effect hasTarget:true requires a valid numeric target" + EffectIdSuffix(fx, o));
                fx.HasTarget = true;
                fx.Target = rule;
                return;
            }

            if (!TryReadTargetRule(o, out var compat))
                throw new InvalidDataException("effect target must be a valid numeric TargetRule" + EffectIdSuffix(fx, o));
            fx.HasTarget = true;
            fx.Target = compat;
        }

        static bool TryReadTargetRule(Dictionary<string, object> o, out TargetRule rule)
        {
            rule = default;
            if (!HasNum(o, "target")) return false;
            var n = Int(o, "target");
            if (!Enum.IsDefined(typeof(TargetRule), n)) return false;
            rule = (TargetRule)n;
            return true;
        }

        static string EffectIdSuffix(EffectDef fx, Dictionary<string, object> o)
        {
            var id = fx != null && !string.IsNullOrEmpty(fx.Id) ? fx.Id : Str(o, "id");
            return string.IsNullOrEmpty(id) ? "" : " (id=" + id + ")";
        }

        static void ApplyLifecycleJson(EffectDef dst, Dictionary<string, object> o)
        {
            if (dst == null || o == null || !EffectCapability.HasLifecycleFields) return;
            if (HasText(o, "trigger")) EffectCapability.WriteTrigger(dst, Str(o, "trigger"));
            if (HasNum(o, "period")) EffectCapability.WritePeriodSec(dst, Flt(o, "period"));
        }

        static StageDef ReadStage(Dictionary<string, object> so)
        {
            if (so == null) return new StageDef { TimeLimitSec = 90f, Wave0 = Catalog.CopyWave(null), Wave1 = Catalog.CopyWave(null), EnemyHpMul = 1f, EnemyAtkMul = 1f, EnemyDefMul = 1f };
            return new StageDef
            {
                Id = Str(so, "id"),
                Name = Str(so, "name"),
                TimeLimitSec = Flt(so, "time", 90f),
                Wave0 = Catalog.CopyWave(StrArr(so, "w0")),
                Wave1 = Catalog.CopyWave(StrArr(so, "w1")),
                EnemyHpMul = Flt(so, "hpMul", 1f),
                EnemyAtkMul = Flt(so, "atkMul", 1f),
                EnemyDefMul = Flt(so, "defMul", 1f)
            };
        }

        public static string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("{\"party\":[");
            var party = Catalog.DefaultParty;
            if (party != null)
            for (int i = 0; i < party.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(Esc(party[i])).Append('"');
            }
            sb.Append("],\"stage\":");
            WriteStage(sb, Catalog.VerticalSliceStage);
            sb.Append(",\"stages\":[");
            if (Catalog.Stages != null)
            for (int i = 0; i < Catalog.Stages.Length; i++)
            {
                if (i > 0) sb.Append(',');
                WriteStage(sb, Catalog.Stages[i]);
            }
            sb.Append("],\"effects\":[");
            var first = true;
            if (Catalog.Effects != null)
            foreach (var e in Catalog.Effects.Values)
            {
                if (e == null) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"id\":\"").Append(Esc(e.Id)).Append("\",\"op\":\"").Append(Esc(e.Opcode)).Append("\",\"kind\":").Append((int)e.Kind);
                sb.Append(",\"mag\":").Append(Num(e.Magnitude)).Append(",\"dur\":").Append(Num(e.DurationSec));
                sb.Append(",\"stack\":").Append(e.MaxStack).Append(",\"tier\":").Append(e.SourceTier);
                sb.Append(",\"group\":\"").Append(Esc(e.Group)).Append("\"");
                if (e.HasTarget)
                    sb.Append(",\"hasTarget\":true,\"target\":").Append((int)e.Target);
                if (e.Side != TargetSide.FromRule)
                    sb.Append(",\"side\":").Append((int)e.Side);
                if (EffectCapability.HasTriggerField)
                    sb.Append(",\"trigger\":\"").Append(Esc(EffectCapability.ReadTrigger(e))).Append("\"");
                if (EffectCapability.HasPeriodField)
                    sb.Append(",\"period\":").Append(Num(EffectCapability.ReadPeriodSec(e)));
                sb.Append('}');
            }
            sb.Append("],\"chars\":[");
            first = true;
            if (Catalog.Characters != null)
            foreach (var c in Catalog.Characters.Values)
            {
                if (c == null) continue;
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
                sb.Append(",\"battleLv\":").Append(c.BattleLevel);
                sb.Append(",\"native\":").Append(c.NativeStar).Append(",\"maxStar\":").Append(c.MaxStar);
                sb.Append(",\"uncap\":").Append(c.UncapMax).Append(",\"ign\":").Append(c.IgnitionMax).Append('}');
            }
            sb.Append("],\"skills\":[");
            first = true;
            if (Catalog.Skills != null)
            foreach (var s in Catalog.Skills.Values)
            {
                if (s == null) continue;
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
                sb.Append(",\"base\":\"").Append(Esc(s.BaseSkillId)).Append("\",\"op\":\"").Append(Esc(s.Opcode)).Append("\"");
                sb.Append(",\"slideRank\":").Append(s.SlideRank);
                sb.Append(",\"slideLv\":").Append(s.SlideSkillLv);
                sb.Append(",\"slideLvMax\":").Append(s.SlideSkillLvMax).Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        static void WriteStage(StringBuilder sb, StageDef st)
        {
            if (st == null)
            {
                sb.Append("{}");
                return;
            }
            sb.Append("{\"id\":\"").Append(Esc(st.Id)).Append("\",\"name\":\"").Append(Esc(st.Name)).Append("\",");
            sb.Append("\"time\":").Append(Num(st.TimeLimitSec)).Append(",\"w0\":");
            Arr(sb, st.Wave0);
            sb.Append(",\"w1\":");
            Arr(sb, st.Wave1);
            sb.Append(",\"hpMul\":").Append(Num(st.EnemyHpMul));
            sb.Append(",\"atkMul\":").Append(Num(st.EnemyAtkMul));
            sb.Append(",\"defMul\":").Append(Num(st.EnemyDefMul)).Append('}');
        }

        static void Arr(StringBuilder sb, string[] a)
        {
            sb.Append('[');
            if (a != null)
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(Esc(a[i])).Append('"');
                }
            }
            sb.Append(']');
        }

        static List<object> AsList(Dictionary<string, object> o, string k)
        {
            if (o == null || !o.TryGetValue(k, out var v) || !(v is List<object> list))
                return new List<object>();
            return list;
        }

        static bool HasKey(Dictionary<string, object> o, string k)
        {
            return o != null && o.TryGetValue(k, out var v) && v != null;
        }

        static bool HasText(Dictionary<string, object> o, string k)
        {
            if (!HasKey(o, k)) return false;
            return !string.IsNullOrWhiteSpace(Str(o, k));
        }

        static bool HasNum(Dictionary<string, object> o, string k)
        {
            if (!HasKey(o, k)) return false;
            var s = Str(o, k).Trim();
            if (s.Length == 0) return false;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        static bool HasBool(Dictionary<string, object> o, string k)
        {
            if (!HasKey(o, k)) return false;
            if (o.TryGetValue(k, out var v) && v is bool) return true;
            var s = Str(o, k).Trim();
            return string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase);
        }

        static string Str(Dictionary<string, object> o, string k)
        {
            if (o == null || !o.TryGetValue(k, out var v) || v == null) return "";
            return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
        }

        static int Int(Dictionary<string, object> o, string k, int fallback = 0)
        {
            if (o == null || !o.TryGetValue(k, out var v) || v == null) return fallback;
            if (v is bool) return fallback;
            var s = Convert.ToString(v, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            int n;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) return n;
            double d;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return (int)d;
            return fallback;
        }

        static float Flt(Dictionary<string, object> o, string k, float fallback = 0f)
        {
            if (o == null || !o.TryGetValue(k, out var v) || v == null) return fallback;
            if (v is bool) return fallback;
            var s = Convert.ToString(v, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            float f;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) return f;
            return fallback;
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
                    if (_i >= _s.Length || _s[_i] == '}') break;
                    var key = ReadStr() ?? "";
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
                if (_i >= _s.Length || _s[_i] != '"') return "";
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
                if (string.IsNullOrEmpty(t) || t == "-" || t == "+" || t == "." || t == "e" || t == "E")
                    return 0L;
                double d;
                if (t.IndexOf('.') >= 0 || t.IndexOf('e') >= 0 || t.IndexOf('E') >= 0)
                {
                    if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
                    return 0L;
                }
                long n;
                if (long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) return n;
                if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
                return 0L;
            }

            bool ReadBool()
            {
                if (_i + 4 <= _s.Length && _s.Substring(_i, 4) == "true") { _i += 4; return true; }
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
