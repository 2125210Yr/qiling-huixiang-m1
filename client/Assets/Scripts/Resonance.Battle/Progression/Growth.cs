using System;

namespace Resonance.Battle
{
    public sealed class UnitProgress
    {
        public string Id = "";
        public int Level = 1;
        public int Uncap;
        public int Ignition;
        public int IgnAtk;
        public int IgnCrt;
        public int IgnAgl;
        public int Affection;
        public string SkinId = "";
        public string Gear0 = "";
        public string Gear1 = "";
        public string Gear2 = "";
        public string Gear3 = "";
        public int Plus0, Plus1, Plus2, Plus3; // gear +0..+15 for slots 武器/防具/饰品/残章
        public string Reserve = "EEEEE";
    }

    public sealed class StatBreak
    {
        public int BodyHp, AffHp, GearHp, TotalHp;
        public int BodyAtk, AffAtk, GearAtk, TotalAtk;
        public int BodyDef, AffDef, GearDef, TotalDef;
        public int BodyAgl, AffAgl, GearAgl, TotalAgl;
        public int BodyCrt, AffCrt, GearCrt, TotalCrt;
    }

    public static class Growth
    {
        public const int MaxLevel = 60;
        public static readonly int[] IgnitionStops = { 0, 1, 2, 5, 8, 11, 12 };

        public static CharacterDef Clone(CharacterDef src)
        {
            if (src == null) return new CharacterDef();
            return new CharacterDef
            {
                Id = src.Id,
                Name = src.Name,
                Element = src.Element,
                Role = src.Role,
                Hp = src.Hp,
                Atk = src.Atk,
                Def = src.Def,
                Agl = src.Agl,
                Crt = src.Crt,
                ChargeTimeSec = src.ChargeTimeSec,
                AutoSkillId = src.AutoSkillId,
                TapSkillId = src.TapSkillId,
                SlideSkillId = src.SlideSkillId,
                DriveSkillId = src.DriveSkillId,
                LeaderSkillId = src.LeaderSkillId,
                IsEnemy = src.IsEnemy,
                IsBoss = src.IsBoss,
                NativeStar = src.NativeStar,
                MaxStar = src.MaxStar,
                UncapMax = src.UncapMax,
                IgnitionMax = src.IgnitionMax
            };
        }

        public static CharacterDef Apply(CharacterDef src, UnitProgress p)
        {
            var b = BreakDown(src, p);
            var d = Clone(src);
            if (d == null) d = new CharacterDef();
            d.Hp = b.TotalHp;
            d.Atk = b.TotalAtk;
            d.Def = b.TotalDef;
            d.Agl = b.TotalAgl;
            d.Crt = b.TotalCrt;
            return d;
        }

        public static StatBreak BreakDown(CharacterDef src, UnitProgress p)
        {
            var b = new StatBreak();
            if (src == null) return b;
            var lv = p == null ? 1 : ClampLv(p.Level);
            var uncap = p == null ? 0 : ClampUncap(src, p.Uncap);
            var ign = p == null ? 0 : ClampIgn(src, p.Ignition);
            var mul = BodyMul(lv, uncap, ign);
            b.BodyHp = (int)Math.Round(src.Hp * mul);
            b.BodyAtk = (int)Math.Round(src.Atk * mul);
            b.BodyDef = (int)Math.Round(src.Def * mul);
            b.BodyAgl = (int)Math.Round(src.Agl * mul);
            b.BodyCrt = (int)Math.Round(src.Crt * mul);
            var aff = p == null ? 0 : p.Affection;
            var am = AffMul(aff);
            b.AffHp = (int)Math.Round(b.BodyHp * am) - b.BodyHp;
            b.AffAtk = (int)Math.Round(b.BodyAtk * am) - b.BodyAtk;
            b.AffDef = (int)Math.Round(b.BodyDef * am) - b.BodyDef;
            b.AffAgl = (int)Math.Round(b.BodyAgl * am) - b.BodyAgl;
            b.AffCrt = (int)Math.Round(b.BodyCrt * am) - b.BodyCrt;
            AddGearFlats(p, out b.GearHp, out b.GearAtk, out b.GearDef, out b.GearAgl, out b.GearCrt);
            b.TotalHp = b.BodyHp + b.AffHp + b.GearHp;
            b.TotalAtk = b.BodyAtk + b.AffAtk + b.GearAtk;
            b.TotalDef = b.BodyDef + b.AffDef + b.GearDef;
            b.TotalAgl = b.BodyAgl + b.AffAgl + b.GearAgl;
            b.TotalCrt = b.BodyCrt + b.AffCrt + b.GearCrt;
            return b;
        }

        public static int CombatPower(CharacterDef d)
        {
            if (d == null) return 0;
            return (int)Math.Round(d.Hp * 0.25 + d.Atk * 1.8 + d.Def * 1.3 + d.Agl * 0.8 + d.Crt * 0.7);
        }

        public static CharacterDef ScaleEnemy(CharacterDef src, StageDef stage)
        {
            var d = Clone(src);
            if (d == null) d = new CharacterDef();
            if (src == null || stage == null) return d;
            d.Hp = Math.Max(1, (int)Math.Round(src.Hp * stage.EnemyHpMul));
            d.Atk = Math.Max(1, (int)Math.Round(src.Atk * stage.EnemyAtkMul));
            d.Def = Math.Max(1, (int)Math.Round(src.Def * stage.EnemyDefMul));
            return d;
        }

        public static int CycleIgnition(int current, int max)
        {
            if (max < 1) return 0;
            for (int i = 0; i < IgnitionStops.Length; i++)
            {
                if (IgnitionStops[i] > current && IgnitionStops[i] <= max)
                    return IgnitionStops[i];
            }
            return 0;
        }

        public static int IgnitionPips(int ignition)
        {
            var n = 0;
            for (int i = 1; i < IgnitionStops.Length; i++)
                if (ignition >= IgnitionStops[i]) n++;
            return n;
        }

        public static string AffectionRank(int points)
        {
            if (points >= 100) return "S";
            if (points >= 80) return "A";
            if (points >= 60) return "B";
            if (points >= 40) return "C";
            if (points >= 20) return "D";
            return "E";
        }

        public static int CycleAffection(int current)
        {
            var n = current + 20;
            return n > 100 ? 0 : n;
        }

        public static string NormalizedReserve(string r)
        {
            if (string.IsNullOrEmpty(r) || r.Length < 5) return "EEEEE";
            var c = new char[5];
            for (int i = 0; i < 5; i++)
            {
                var ch = r[i];
                c[i] = ch == 'T' || ch == 'S' ? ch : 'E';
            }
            return new string(c);
        }

        public static bool ReserveAllEmpty(string r)
        {
            r = NormalizedReserve(r);
            return r[0] == 'E' && r[1] == 'E' && r[2] == 'E' && r[3] == 'E' && r[4] == 'E';
        }

        public static string CycleReserveSlot(string r, int slot)
        {
            r = NormalizedReserve(r);
            if (slot < 0 || slot > 4) return r;
            var c = r.ToCharArray();
            c[slot] = c[slot] == 'E' ? 'T' : c[slot] == 'T' ? 'S' : 'E';
            return new string(c);
        }

        static float BodyMul(int lv, int uncap, int ign) =>
            1f + 0.035f * (lv - 1) + 0.02f * uncap + 0.012f * ign;

        static float AffMul(int affection)
        {
            if (affection < 0) affection = 0;
            if (affection > 100) affection = 100;
            return (1f + 0.18f * (affection / 100f)) * Bond.StatMul(Bond.Level(affection));
        }

        static int ClampLv(int lv) => lv < 1 ? 1 : (lv > MaxLevel ? MaxLevel : lv);

        static int ClampUncap(CharacterDef src, int uncap)
        {
            var max = src == null ? 6 : src.UncapMax;
            if (uncap < 0) return 0;
            return uncap > max ? max : uncap;
        }

        static int ClampIgn(CharacterDef src, int ign)
        {
            var max = src == null ? 12 : src.IgnitionMax;
            if (ign < 0) return 0;
            return ign > max ? max : ign;
        }

        static void AddGearFlats(UnitProgress p, out int hp, out int atk, out int def, out int agl, out int crt)
        {
            hp = atk = def = agl = crt = 0;
            if (p == null) return;
            AddOne(p.Gear0, ref hp, ref atk, ref def, ref agl, ref crt);
            AddOne(p.Gear1, ref hp, ref atk, ref def, ref agl, ref crt);
            AddOne(p.Gear2, ref hp, ref atk, ref def, ref agl, ref crt);
            AddOne(p.Gear3, ref hp, ref atk, ref def, ref agl, ref crt);
            AddPlus(p.Gear0, p.Plus0, 0, ref hp, ref atk, ref def, ref agl, ref crt);
            AddPlus(p.Gear1, p.Plus1, 1, ref hp, ref atk, ref def, ref agl, ref crt);
            AddPlus(p.Gear2, p.Plus2, 2, ref hp, ref atk, ref def, ref agl, ref crt);
            AddPlus(p.Gear3, p.Plus3, 3, ref hp, ref atk, ref def, ref agl, ref crt);
        }

        static void AddOne(string gearId, ref int hp, ref int atk, ref int def, ref int agl, ref int crt)
        {
            var g = GearCatalog.Try(gearId);
            if (g == null) return;
            hp += g.Hp;
            atk += g.Atk;
            def += g.Def;
            agl += g.Agl;
            crt += g.Crt;
        }

        static void AddPlus(string gearId, int plus, int slotIndex, ref int hp, ref int atk, ref int def, ref int agl, ref int crt)
        {
            var g = GearCatalog.Try(gearId);
            if (g == null) return;
            plus = Equipment.ClampPlus(plus);
            if (plus == 0) return;
            if (slotIndex == 3)
            {
                hp += (int)Math.Round(g.Hp * 0.02 * plus);
                atk += (int)Math.Round(g.Atk * 0.02 * plus);
                def += (int)Math.Round(g.Def * 0.02 * plus);
                agl += (int)Math.Round(g.Agl * 0.02 * plus);
                crt += (int)Math.Round(g.Crt * 0.02 * plus);
                return;
            }
            var slot = g.Slot;
            if (string.IsNullOrEmpty(slot))
            {
                var names = GearCatalog.SlotNames;
                if (names != null && slotIndex >= 0 && slotIndex < names.Length)
                    slot = names[slotIndex];
            }
            var b = Equipment.Bonus(slot, plus);
            atk += (int)Math.Round(b.Atk);
            def += (int)Math.Round(b.Def);
        }
    }
}
