using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;

namespace Resonance.Tests
{
    [Collection("G2RecheckCatalog")]
    public sealed class GrowthRuntimeRoundingTests
    {
        [Theory]
        [InlineData(2, 534)]
        [InlineData(20, 849)]
        public void CombinedModifiers_PreserveUnityMultiplierBoundary(int level, int expected)
        {
            var src = new CharacterDef {
                Hp = 500, Atk = 500, Def = 500, Agl = 500, Crt = 500,
                UncapMax = 6, IgnitionMax = 12
            };
            var b = Growth.BreakDown(src, new UnitProgress { Level = level, Uncap = 1, Ignition = 1 });
            Assert.Equal(new[] { expected, expected, expected, expected, expected },
                new[] { b.BodyHp, b.BodyAtk, b.BodyDef, b.BodyAgl, b.BodyCrt });
        }

        [Fact]
        public void NextBattleLevelTwo_PreservesRecordedUnityStats()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var b = Growth.BreakDown(Catalog.MustChar("C003"), new UnitProgress {
                        Id = "C003", Level = 2, Affection = 4, Gear3 = "SC006"
                    });
                    // Golden values from the same game DLL running under Unity Mono.
                    // The old .NET path rounded the HP/ATK product to float first.
                    Assert.Equal(new[] { 3001, 931, 828, 828, 683 },
                        new[] { b.BodyHp, b.BodyAtk, b.BodyDef, b.BodyAgl, b.BodyCrt });
                    Assert.Equal(new[] { 3283, 1088, 834, 834, 688 },
                        new[] { b.TotalHp, b.TotalAtk, b.TotalDef, b.TotalAgl, b.TotalCrt });
                }
                finally { RestoreBuiltinCatalog(); }
            }
        }

        [Theory]
        [InlineData("C007", 2, 0, 12, 100, 1061, 246, 369, 220, 171)]
        [InlineData("C020", 60, 1, 1, 4, 50, 26, 15, 20, 18)]
        [InlineData("C020", 60, 1, 1, 100, 1886, 994, 561, 773, 680)]
        public void AffectionHalfBoundaries_PreserveUnityRounding(
            string id, int level, int uncap, int ignition, int affection,
            int hp, int atk, int def, int agl, int crt)
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var b = Growth.BreakDown(Catalog.MustChar(id), new UnitProgress {
                        Id = id, Level = level, Uncap = uncap, Ignition = ignition,
                        Affection = affection
                    });
                    Assert.Equal(new[] { hp, atk, def, agl, crt },
                        new[] { b.AffHp, b.AffAtk, b.AffDef, b.AffAgl, b.AffCrt });
                }
                finally { RestoreBuiltinCatalog(); }
            }
        }
    }
}
