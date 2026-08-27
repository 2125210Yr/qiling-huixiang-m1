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
