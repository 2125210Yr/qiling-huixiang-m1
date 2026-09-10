using System.Collections.Generic;

namespace Resonance.Battle
{
    // In-memory skin unlocks per character. Ids echo/night match CharacterPresenter.SkinTint.
    public static class Costume
    {
        static Dictionary<string, HashSet<string>> Owned =
            new Dictionary<string, HashSet<string>>();

        public static bool Unlocked(string charId, string skinId)
        {
            if (string.IsNullOrEmpty(charId)) return false;
            if (string.IsNullOrEmpty(skinId)) return true;
            if (!Known(skinId)) return false;
            var bag = Bag();
            HashSet<string> set;
            return bag.TryGetValue(charId, out set) && set != null && set.Contains(skinId);
        }

        public static void Unlock(string charId, string skinId)
        {
            if (string.IsNullOrEmpty(charId) || !Known(skinId)) return;
            var bag = Bag();
            HashSet<string> set;
            if (!bag.TryGetValue(charId, out set) || set == null)
            {
                set = new HashSet<string>();
                bag[charId] = set;
            }
            set.Add(skinId);
        }

        static Dictionary<string, HashSet<string>> Bag()
        {
            var bag = Owned;
            if (bag != null) return bag;
            bag = new Dictionary<string, HashSet<string>>();
            Owned = bag;
            return bag;
        }

        static bool Known(string skinId) =>
            skinId == "echo" || skinId == "night";

        public static void ReplaceFrom(IList<string> packed)
        {
            Owned = new Dictionary<string, HashSet<string>>();
            if (packed == null) return;
            for (int i = 0; i < packed.Count; i++)
            {
                var s = packed[i];
                if (string.IsNullOrEmpty(s)) continue;
                var cut = s.IndexOf(':');
                if (cut <= 0 || cut >= s.Length - 1) continue;
                Unlock(s.Substring(0, cut), s.Substring(cut + 1));
            }
        }

        public static void WriteTo(List<string> dest)
        {
            if (dest == null) return;
            dest.Clear();
            var bag = Bag();
            foreach (var kv in bag)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;
                foreach (var skin in kv.Value)
                {
                    if (string.IsNullOrEmpty(skin)) continue;
                    dest.Add(kv.Key + ":" + skin);
                }
            }
        }
    }
}
