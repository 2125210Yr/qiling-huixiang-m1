using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    public sealed class GearDef
    {
        public string Id;
        public string Name;
        public string Slot;
        public int Star;
        public int Hp;
        public int Atk;
        public int Def;
        public int Agl;
        public int Crt;
        public string Pair;
        public string ElementGate;
        public string RoleGate;
        public string ModeGate;
    }

    public static class GearCatalog
    {
        public const string CartaSlot = "残章";
        public static readonly string[] SlotNames = { "武器", "防具", "饰品", CartaSlot };

        public static readonly GearDef[] Equipment =
        {
            new GearDef { Id = "EQ_WPN", Name = "残响刃", Slot = "武器", Atk = 220 },
            new GearDef { Id = "EQ_ARM", Name = "废铁甲", Slot = "防具", Hp = 480, Def = 90 },
            new GearDef { Id = "EQ_ACC", Name = "契核坠", Slot = "饰品", Atk = 90, Hp = 160 }
        };

        public static readonly GearDef[] Cartas;
        public static readonly GearDef[] All;
        public static readonly string DefaultCartaId;

        static GearCatalog()
        {
            Cartas = BuildCartas() ?? new GearDef[0];
            var eq = Equipment ?? new GearDef[0];
            All = new GearDef[eq.Length + Cartas.Length];
            if (eq.Length > 0) Array.Copy(eq, 0, All, 0, eq.Length);
            if (Cartas.Length > 0) Array.Copy(Cartas, 0, All, eq.Length, Cartas.Length);
            DefaultCartaId = FirstId(Cartas);
            for (int i = 0; i < Cartas.Length; i++)
            {
                var g = Cartas[i];
                if (g == null) continue;
                if (g.Star >= 5 && g.ElementGate == "火" && g.Atk > 0)
                {
                    DefaultCartaId = g.Id;
                    break;
                }
            }
        }

        public static GearDef Try(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id == "EQ_CRD") id = DefaultCartaId;
            if (string.IsNullOrEmpty(id)) return null;
            var all = All;
            if (all == null) return null;
            for (int i = 0; i < all.Length; i++)
            {
                var g = all[i];
                if (g != null && g.Id == id) return g;
            }
            return null;
        }

        public static string ForSlot(int slot)
        {
            if (slot == 3) return DefaultCartaId ?? "";
            var eq = Equipment;
            if (eq == null || slot < 0 || slot >= eq.Length) return "";
            var g = eq[slot];
            return g == null || g.Id == null ? "" : g.Id;
        }

        public static string CycleSlot(int slot, string current)
        {
            if (slot == 3) return CycleCarta(current);
            var id = ForSlot(slot);
            if (string.IsNullOrEmpty(id)) return "";
            return current == id ? "" : id;
        }

        public static string Label(string id)
        {
            var g = Try(id);
            return g == null || string.IsNullOrEmpty(g.Name) ? "空" : g.Name;
        }

        static string CycleCarta(string current)
        {
            var cartas = Cartas;
            if (cartas == null || cartas.Length == 0) return "";
            if (string.IsNullOrEmpty(current))
                return FirstId(cartas);
            if (current == "EQ_CRD") current = DefaultCartaId;
            for (int i = 0; i < cartas.Length; i++)
            {
                if (cartas[i] == null || cartas[i].Id != current) continue;
                return i + 1 < cartas.Length ? FirstIdFrom(cartas, i + 1) : "";
            }
            return FirstId(cartas);
        }

        static string FirstId(GearDef[] list) => FirstIdFrom(list, 0);

        static string FirstIdFrom(GearDef[] list, int start)
        {
            if (list == null) return "";
            for (int i = start; i < list.Length; i++)
            {
                var g = list[i];
                if (g != null && !string.IsNullOrEmpty(g.Id)) return g.Id;
            }
            return "";
        }

        static GearDef[] BuildCartas()
        {
            var lines = CartaTable.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new GearDef[lines.Length];
            var used = new Dictionary<string, int>();
            for (int i = 0; i < lines.Length; i++)
            {
                var p = lines[i].Trim().Split(',');
                var star = int.Parse(p[0]);
                var pair = p.Length > 1 ? p[1] : "";
                var el = p.Length > 2 ? p[2] : "";
                var role = p.Length > 3 ? p[3] : "";
                var mode = p.Length > 4 ? p[4] : "any";
                int hp, atk, def, agl, crt;
                Flats(star, pair, out hp, out atk, out def, out agl, out crt);
                list[i] = new GearDef
                {
                    Id = "SC" + (i + 1).ToString("000"),
                    Name = UniqueName(used, ElStem(el) + PairStem(pair) + ModeStem(mode)),
                    Slot = CartaSlot,
                    Star = star,
                    Hp = hp,
                    Atk = atk,
                    Def = def,
                    Agl = agl,
                    Crt = crt,
                    Pair = pair,
                    ElementGate = el,
                    RoleGate = role,
                    ModeGate = mode
                };
            }
            return list;
        }

        static void Flats(int star, string pair, out int hp, out int atk, out int def, out int agl, out int crt)
        {
            hp = atk = def = agl = crt = 0;
            int hpV, atkV, defV, aglV, crtV;
            if (star >= 5) { hpV = 260; atkV = 150; defV = 80; aglV = 64; crtV = 64; }
            else if (star == 4) { hpV = 170; atkV = 95; defV = 52; aglV = 40; crtV = 40; }
            else { hpV = 100; atkV = 55; defV = 32; aglV = 24; crtV = 24; }
            if (string.IsNullOrEmpty(pair))
            {
                hp = hpV / 2;
                return;
            }
            if (pair.IndexOf('血') >= 0) hp = hpV;
            if (pair.IndexOf('攻') >= 0) atk = atkV;
            if (pair.IndexOf('防') >= 0) def = defV;
            if (pair.IndexOf('敏') >= 0) agl = aglV;
            if (pair.IndexOf('暴') >= 0) crt = crtV;
        }

        static string UniqueName(Dictionary<string, int> used, string stem)
        {
            if (!used.ContainsKey(stem))
            {
                used[stem] = 1;
                return stem;
            }
            var n = used[stem] + 1;
            used[stem] = n;
            return stem + "·" + n;
        }

        static string ElStem(string el)
        {
            if (el == "火") return "焰";
            if (el == "水") return "潮";
            if (el == "木") return "棘";
            if (el == "光") return "昼";
            if (el == "暗") return "夜";
            return "契";
        }

        static string PairStem(string pair)
        {
            if (pair == "血攻") return "生锋";
            if (pair == "血防") return "生壁";
            if (pair == "血敏") return "生风";
            if (pair == "血暴") return "生芒";
            if (pair == "攻防") return "锋壁";
            if (pair == "攻敏") return "锋风";
            if (pair == "攻暴") return "锋芒";
            if (pair == "防敏") return "壁风";
            if (pair == "防暴") return "壁芒";
            return "余响";
        }

        static string ModeStem(string mode)
        {
            if (mode == "pvp") return "对决";
            if (mode == "pve") return "征途";
            if (mode == "rb") return "裂口";
            if (mode == "wb") return "王庭";
            if (mode == "raid5") return "五围";
            if (mode == "raid20") return "廿围";
            if (mode == "space") return "深途";
            if (mode == "metro") return "下廊";
            return "残章";
        }

        // Shape only: rarity / stat pair / element·role·mode gates from 魂之歌牌.csv (156).
        // Names and flats are clean-room. Original titles stay out.
        const string CartaTable = @"5,血攻,水,,any
5,血攻,,攻击,pvp
5,攻暴,水,,raid5
5,血攻,暗,,raid5
5,血攻,光,,raid5
5,血攻,火,,raid5
5,血防,暗,,pvp
5,血攻,水,,raid5
5,血防,,,raid20
5,攻敏,火,,pve
5,血防,,防御,pvp
5,血防,,干扰,space
5,血敏,,辅助,pvp
5,血敏,,,raid20
5,血敏,水,辅助,space
5,血攻,木,,raid5
5,血防,木,,pvp
5,攻敏,暗,攻击,raid5
5,血防,,,any
5,攻暴,,攻击,pve
5,血防,,干扰,pvp
5,血攻,木,,raid5
5,血攻,,攻击,space
5,攻敏,,攻击,pvp
5,攻敏,光,攻击,raid20
5,攻暴,火,,raid5
5,血防,,,any
5,血暴,,,pvp
5,攻防,,,pve
5,血防,,干扰,pvp
5,血敏,,治疗,pve
5,攻敏,,,any
5,攻暴,,攻击,pvp
5,血攻,,辅助,pvp
5,血敏,水,,raid20
5,血攻,,治疗,pvp
5,血防,,,any
5,攻防,,防御,pvp
5,血攻,光,,rb
5,血防,,,wb
5,攻敏,,,pvp
5,攻防,,,rb
5,血敏,水,,rb
5,血攻,,,wb
5,攻防,,,rb
5,血敏,,,rb
5,血防,,,wb
5,攻暴,,,rb
5,血敏,,,wb
5,血攻,,,pvp
5,血防,,,any
5,血暴,,,any
5,血暴,,,any
5,血攻,水,,rb
5,血防,,,any
5,攻敏,暗,,wb
5,血防,火,,rb
5,攻防,,,any
5,血防,,,any
5,攻敏,光,,rb
5,血攻,木,,rb
5,攻暴,水,,rb
5,血敏,,辅助,pvp
5,血防,,防御,pvp
5,血攻,,攻击,pvp
5,血暴,,干扰,pvp
5,血敏,暗,,rb
5,攻暴,水,,wb
5,攻暴,暗,,wb
5,攻敏,光,,rb
5,血防,,干扰,wb
5,攻暴,暗,,rb
5,攻暴,木,,wb
5,血攻,水,,rb
5,血暴,,,pvp
5,血敏,,,pvp
5,攻敏,木,,rb
5,血防,,,pvp
5,血攻,火,,wb
5,血防,,,pvp
5,攻暴,光,,wb
5,攻敏,,攻击,pvp
5,血暴,水,,rb
5,攻敏,水,攻击,wb
5,血攻,火,攻击,rb
5,血暴,,,pvp
5,血防,木,攻击,wb
5,血攻,火,攻击,rb
5,攻暴,光,攻击,wb
5,血攻,水,攻击,wb
5,血防,火,干扰,pve
5,血敏,,,pvp
5,攻暴,木,,rb
5,攻敏,木,攻击,wb
5,血防,,,any
5,血攻,光,,rb
5,血防,,,pvp
5,攻暴,暗,,wb
5,攻敏,光,攻击,rb
5,血攻,,攻击,pvp
5,攻暴,木,攻击,raid5
5,攻敏,,攻击,pvp
5,攻暴,水,,rb
5,血防,火,,wb
5,血防,,,pvp
5,攻暴,暗,,rb
5,防敏,,,pvp
5,攻敏,火,,wb
5,攻暴,火,,rb
5,血防,,,any
5,血攻,木,,wb
5,攻暴,光,攻击,rb
5,血防,,,any
5,攻敏,水,,wb
5,血敏,,,pvp
5,防敏,,,pvp
5,血攻,木,,rb
5,血暴,,,any
5,攻敏,,,pvp
5,血防,,,pvp
5,攻敏,光,,wb
5,攻暴,木,,rb
5,血敏,,,pvp
5,攻防,,攻击,any
5,血敏,木,,any
5,防敏,水,,metro
5,血防,,防御,pvp
5,攻暴,光,,any
5,攻敏,暗,,rb
5,血攻,木,攻击,any
5,攻暴,光,攻击,any
5,血攻,,,any
5,血敏,,,pvp
5,,,,any
4,血防,,,any
4,,,,any
4,血敏,,,any
4,血防,,,any
4,血攻,,,pvp
4,攻暴,火,,any
4,血防,,防御,any
4,攻敏,,,pvp
4,攻防,,,any
4,血防,木,,any
4,防敏,,,pvp
4,血暴,光,,any
4,攻暴,,,pvp
3,,,,any
3,血敏,水,,any
3,血攻,火,,any
3,血暴,木,,any
3,血防,光,,any
3,攻敏,,攻击,any
3,攻暴,暗,,any
3,攻敏,,,any
3,防暴,,,any";
    }

    public static class SkinCatalog
    {
        public static readonly string[] Ids = { "", "echo", "night" };

        public static string Cycle(string id)
        {
            var ids = Ids;
            if (ids == null || ids.Length == 0) return "";
            if (id == null) id = "";
            for (int i = 0; i < ids.Length; i++)
                if (ids[i] == id)
                    return ids[(i + 1) % ids.Length] ?? "";
            return ids[0] ?? "";
        }

        public static string Label(string id)
        {
            if (id == "echo") return "残响";
            if (id == "night") return "夜巡";
            return "本貌";
        }
    }
}
