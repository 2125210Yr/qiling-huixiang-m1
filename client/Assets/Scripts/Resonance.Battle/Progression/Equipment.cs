namespace Resonance.Battle
{
    public struct EquipPiece
    {
        public string Slot;
        public int Plus;
        public float Atk;
        public float Def;
    }

    // Slot stubs +0..+15. Generic keys only; no item names, no bag.
    public static class Equipment
    {
        public const int MaxPlus = 15;
        public const string Weapon = "weapon";
        public const string Armor = "armor";
        public const string Acc = "acc";
        public static readonly string[] Slots = { Weapon, Armor, Acc };

        public static EquipPiece Make(string slot, int plus)
        {
            plus = ClampPlus(plus);
            slot = CanonicalSlot(slot);
            float atk = 0f;
            float def = 0f;
            if (slot == Weapon)
                atk = 220f + 18f * plus;
            else if (slot == Armor)
                def = 90f + 12f * plus;
            else if (slot == Acc)
            {
                atk = 90f + 8f * plus;
                def = 36f + 6f * plus;
            }
            return new EquipPiece { Slot = slot, Plus = plus, Atk = atk, Def = def };
        }

        // Make(plus) − Make(0) so +0 adds zero on top of catalog flats.
        public static EquipPiece Bonus(string slot, int plus)
        {
            var grown = Make(slot, plus);
            var zero = Make(slot, 0);
            return new EquipPiece
            {
                Slot = grown.Slot,
                Plus = grown.Plus,
                Atk = grown.Atk - zero.Atk,
                Def = grown.Def - zero.Def
            };
        }

        public static int ClampPlus(int plus)
        {
            if (plus < 0) return 0;
            return plus > MaxPlus ? MaxPlus : plus;
        }

        static string CanonicalSlot(string slot)
        {
            if (string.IsNullOrEmpty(slot)) return "";
            if (Same(slot, Weapon) || slot == "武器") return Weapon;
            if (Same(slot, Armor) || slot == "防具") return Armor;
            if (Same(slot, Acc) || Same(slot, "accessory") || slot == "饰品") return Acc;
            return "";
        }

        static bool Same(string a, string b)
        {
            if (a == b) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                var ca = a[i];
                var cb = b[i];
                if (ca >= 'A' && ca <= 'Z') ca = (char)(ca + 32);
                if (cb >= 'A' && cb <= 'Z') cb = (char)(cb + 32);
                if (ca != cb) return false;
            }
            return true;
        }
    }
}
