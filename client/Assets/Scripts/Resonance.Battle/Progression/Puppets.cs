using System.Collections.Generic;

namespace Resonance.Battle
{
    public enum PuppetRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    public sealed class PuppetDef
    {
        public string Id;
        public string Name;
        public Element Element;
        public bool Untyped;
        public PuppetRarity Rarity;
        public bool Tap;
        public bool Slide;
        public bool Drive;
        public bool Leader;
    }

    public static class PuppetCatalog
    {
        public const int Count = 220;
        public const int OwnCap = 3;

        static readonly string[] ElMark = { "焰", "潮", "芽", "曜", "暮" };
        static readonly string[] RareMark = { "素胚", "薄纹", "低语", "残响", "契核" };
        static readonly int[][] ElemByRarity =
        {
            new[] { 6, 6, 7, 8, 6, 2 },
            new[] { 12, 12, 8, 8, 12, 0 },
            new[] { 13, 12, 14, 14, 13, 0 },
            new[] { 10, 13, 13, 9, 10, 0 },
            new[] { 2, 2, 2, 3, 3, 0 }
        };

        public static readonly PuppetDef[] All = Build();

        public static PuppetDef Try(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var all = All;
            if (all == null) return null;
            for (int i = 0; i < all.Length; i++)
            {
                var p = all[i];
                if (p != null && p.Id == id) return p;
            }
            return null;
        }

        public static string Label(string id)
        {
            var p = Try(id);
            return p == null || string.IsNullOrEmpty(p.Name) ? "空" : p.Name;
        }

        public static string RarityWord(PuppetRarity r)
        {
            if (r == PuppetRarity.Legendary) return "传奇";
            if (r == PuppetRarity.Epic) return "史诗";
            if (r == PuppetRarity.Rare) return "稀有";
            if (r == PuppetRarity.Uncommon) return "罕见";
            return "常见";
        }

        public static void FillStarter(List<string> owned)
        {
            Sanitize(owned);
            if (owned == null) return;
            var all = All;
            if (all == null) return;
            for (int i = 0; i < OwnCap && i < all.Length && owned.Count < OwnCap; i++)
            {
                var p = all[i];
                if (p == null || string.IsNullOrEmpty(p.Id)) continue;
                if (owned.Contains(p.Id)) continue;
                owned.Add(p.Id);
            }
        }

        public static void Sanitize(List<string> owned)
        {
            if (owned == null) return;
            var keep = new List<string>(OwnCap);
            for (int i = 0; i < owned.Count && keep.Count < OwnCap; i++)
            {
                var id = owned[i];
                if (Try(id) == null) continue;
                if (keep.Contains(id)) continue;
                keep.Add(id);
            }
            owned.Clear();
            for (int i = 0; i < keep.Count && i < OwnCap; i++)
                owned.Add(keep[i]);
        }

        public static bool TryOwn(List<string> owned, string id)
        {
            if (owned == null || Try(id) == null) return false;
            Sanitize(owned);
            if (owned.Contains(id) || owned.Count >= OwnCap) return false;
            owned.Add(id);
            return true;
        }

        static PuppetDef[] Build()
        {
            var list = new PuppetDef[Count];
            var masks = SkillMasks();
            var n = 0;
            for (int r = 0; r < ElemByRarity.Length; r++)
            {
                var row = ElemByRarity[r];
                for (int e = 0; e < row.Length; e++)
                {
                    for (int k = 0; k < row[e]; k++)
                    {
                        var untyped = e == 5;
                        var el = untyped ? Element.Fire : (Element)e;
                        var mask = masks[n];
                        list[n] = new PuppetDef
                        {
                            Id = "P" + (n + 1).ToString("D3"),
                            Name = MakeName((PuppetRarity)r, el, untyped, k + 1),
                            Element = el,
                            Untyped = untyped,
                            Rarity = (PuppetRarity)r,
                            Tap = (mask & 1) != 0,
                            Slide = (mask & 2) != 0,
                            Drive = (mask & 4) != 0,
                            Leader = (mask & 8) != 0
                        };
                        n++;
                    }
                }
            }
            return list;
        }

        static string MakeName(PuppetRarity r, Element el, bool untyped, int k)
        {
            var mark = untyped ? "素" : ElMark[(int)el];
            return "契偶·" + RareMark[(int)r] + mark + k.ToString("D2");
        }

        static int[] SkillMasks()
        {
            var m = new int[Count];
            var i = 0;
            void Add(int mask, int n)
            {
                for (int k = 0; k < n; k++) m[i++] = mask;
            }
            Add(2, 53);
            Add(1, 47);
            Add(4, 33);
            Add(1 | 2, 22);
            Add(8, 22);
            Add(2 | 4, 13);
            Add(2 | 8, 11);
            Add(1 | 8, 6);
            Add(4 | 8, 5);
            Add(1 | 2 | 8, 3);
            Add(1 | 4, 2);
            Add(1 | 4 | 8, 1);
            Add(1 | 2 | 4, 1);
            Add(1 | 2 | 4 | 8, 1);
            return m;
        }
    }
}
