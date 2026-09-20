namespace Resonance.Battle
{
    // Invented rest/bond curve. Not the spa percent table; no original titles; no soak/build loop.
    public static class Bond
    {
        public const int MaxLevel = 8;
        public const int PointCap = 100;
        const float StatPerLevel = 0.01f;

        public static int ClampPoints(int points)
        {
            if (points < 0) return 0;
            return points > PointCap ? PointCap : points;
        }

        public static int ClampLevel(int level)
        {
            if (level < 0) return 0;
            return level > MaxLevel ? MaxLevel : level;
        }

        public static int Level(int points)
        {
            return ClampPoints(points) * MaxLevel / PointCap;
        }

        public static float StatMul(int level)
        {
            return 1f + StatPerLevel * ClampLevel(level);
        }

        // Growth composes this term before its one float rounding boundary.
        internal static double StatMulPrecise(int level) => 1d + (double)StatPerLevel * ClampLevel(level);
    }
}
