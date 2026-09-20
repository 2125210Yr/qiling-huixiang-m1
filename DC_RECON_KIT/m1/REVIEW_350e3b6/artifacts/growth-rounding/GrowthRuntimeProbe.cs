using System;
using System.Linq;
using Resonance.Battle;

// Runs the same compiled game assembly under Unity's Mono and .NET.
static class GrowthRuntimeProbe
{
    static void Main()
    {
        VerificationCatalog.RestoreBuiltin();
        foreach (var id in Catalog.Characters.Keys.Where(x => x.StartsWith("C")).OrderBy(x => x))
        foreach (var level in new[] { 1, 2, 20, 59, 60 })
        foreach (var uncap in new[] { 0, 1, 6 })
        foreach (var ignition in new[] { 0, 1, 12 })
        foreach (var affection in new[] { 0, 4, 20, 60, 100 })
        {
            var b = Growth.BreakDown(Catalog.MustChar(id), new UnitProgress {
                Id = id, Level = level, Uncap = uncap, Ignition = ignition,
                Affection = affection, Gear3 = "SC006"
            });
            Console.WriteLine(string.Join("|", new object[] {
                id, level, uncap, ignition, affection,
                b.BodyHp, b.BodyAtk, b.BodyDef, b.BodyAgl, b.BodyCrt,
                b.AffHp, b.AffAtk, b.AffDef, b.AffAgl, b.AffCrt,
                b.TotalHp, b.TotalAtk, b.TotalDef, b.TotalAgl, b.TotalCrt
            }));
        }
    }
}
