using System;
using System.Reflection;
using Resonance.Battle;

static class GrowthMultiplierProbe
{
    static void Main()
    {
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        foreach (var affection in new[] { 4, 20, 60, 100 })
        {
            var am = (float)typeof(Growth).GetMethod("AffMul", flags).Invoke(null, new object[] { affection });
            var bond = Bond.StatMul(Bond.Level(affection));
            var widened = Affinity(affection);
            Console.WriteLine("aff={0} actual={1:R} bits={2:X8} widened={3:R} wideBits={4:X8} bond={5:R}",
                affection, (double)am, BitConverter.ToInt32(BitConverter.GetBytes(am), 0),
                (double)widened, BitConverter.ToInt32(BitConverter.GetBytes(widened), 0), (double)bond);
        }
        foreach (var level in new[] { 2, 20, 59, 60 })
        {
            var body = (float)typeof(Growth).GetMethod("BodyMul", flags).Invoke(null, new object[] { level, 1, 1 });
            var widened = (float)(1d + (double)0.035f * (level - 1) + (double)0.02f + (double)0.012f);
            Console.WriteLine("lv={0} actual={1:R} bits={2:X8} widened={3:R} wideBits={4:X8}",
                level, (double)body, BitConverter.ToInt32(BitConverter.GetBytes(body), 0),
                (double)widened, BitConverter.ToInt32(BitConverter.GetBytes(widened), 0));
        }
        var bodyDifferences = 0;
        for (var level = 1; level <= 60; level++)
        for (var uncap = 0; uncap <= 6; uncap++)
        for (var ign = 0; ign <= 12; ign++)
        {
            var actual = (float)typeof(Growth).GetMethod("BodyMul", flags).Invoke(null, new object[] { level, uncap, ign });
            var candidate = (float)(1d + (double)0.035f * (level - 1) + (double)0.02f * uncap + (double)0.012f * ign);
            if (actual != candidate) bodyDifferences++;
        }
        var affinityDifferences = 0;
        for (var affection = 0; affection <= 100; affection++)
        {
            var actual = (float)typeof(Growth).GetMethod("AffMul", flags).Invoke(null, new object[] { affection });
            if (actual != Affinity(affection)) affinityDifferences++;
        }
        Console.WriteLine("coefficientComparison body=5460 differences={0}; affection=101 differences={1}", bodyDifferences, affinityDifferences);
    }

    static float Affinity(int affection) => (float)((1d + (double)0.18f * (affection / 100d)) * (1d + (double)0.01f * Bond.Level(affection)));
}
