namespace Resonance.Battle
{
    public struct Meal
    {
        public string Name;
        public float AtkMul;
        public float Minutes;
    }

    public static class Food
    {
        // 10 min battle ATK ×1.03. Names are clean-room.
        public static readonly Meal[] All =
        {
            new Meal { Name = "余烬羹", AtkMul = 1.03f, Minutes = 10f },
            new Meal { Name = "契核饼", AtkMul = 1.03f, Minutes = 10f },
            new Meal { Name = "潮汐粥", AtkMul = 1.03f, Minutes = 10f }
        };

        public static Meal Try(int index)
        {
            var all = All;
            if (all == null || index < 0 || index >= all.Length) return default(Meal);
            return all[index];
        }

        public static float AtkMulOf(int index)
        {
            var all = All;
            if (all == null || index < 0 || index >= all.Length) return 1f;
            var mul = all[index].AtkMul;
            return mul > 0f ? mul : 1f;
        }

        public static Meal Demo() => Try(0);
    }
}
