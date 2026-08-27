using System;
using System.IO;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class BattleSimTests
    {
        [Fact]
        public void FireBeatsWood()
        {
            Assert.Equal(1.4f, DamageMath.ElementMultiplier(Element.Fire, Element.Wood));
            Assert.Equal(0.7f, DamageMath.ElementMultiplier(Element.Wood, Element.Fire));
        }

        [Fact]
        public void WikiTsFormula()
        {
            var d = DamageMath.ComputeTs(1000, 2707, 2500, 1.4f, 1f, 1f, 0, 0, false, 0);
            Assert.Equal(2182, d);
        }

        [Fact]
        public void WikiTsFeverIsSixtyPercent()
        {
            var full = DamageMath.ComputeTs(1000, 2707, 2500, 1.4f, 1f, 1f, 0, 0, false, 0);
            var fever = DamageMath.ComputeTs(1000, 2707, 2500, 1.4f, 1f, 0.6f, 0, 0, false, 0);
            Assert.Equal((int)System.Math.Round(full * 0.6, System.MidpointRounding.AwayFromZero), fever);
        }

        [Fact]
        public void WikiTsAdvantageCrit()
        {
            var d = DamageMath.ComputeTs(1000, 2707, 2500, 2.4f, 1f, 1f, 0, 0, true, 0);
            Assert.Equal(3740, d);
        }

        [Fact]
        public void DamageFormulaMatchesRebuild()
        {
            var d = DamageMath.Compute(1180, 0.90f, 120, 420, Element.Fire, Element.Wood, false, 1f, 1f);
            Assert.True(d > 800 && d < 2500, "dmg=" + d);
        }

        [Fact]
        public void SlideDefDownCoversTapDefDown()
        {
            var sim = NewSim(3);
            var enemy = sim.Enemies[0];
            var tap = new EffectDef { Id = "tap_def", Kind = EffectKind.DefDebuff, Magnitude = 0.10f, DurationSec = 10f, MaxStack = 1, SourceTier = 1, Group = "def" };
            var slide = new EffectDef { Id = "slide_def", Kind = EffectKind.DefDebuff, Magnitude = 0.20f, DurationSec = 10f, MaxStack = 1, SourceTier = 2, Group = "def" };
            sim.ApplyStatus(enemy, tap);
            sim.ApplyStatus(enemy, slide);
            Assert.Single(enemy.Status);
            Assert.Equal("slide_def", enemy.Status[0].Def.Id);
            Assert.Equal(0.20f, enemy.Status[0].Def.Magnitude);
        }

        [Fact]
        public void CatalogJsonRoundtrip()
        {
            var json = CatalogJson.Serialize();
            Assert.Contains("\"id\":\"C001\"", json);
            Catalog.BuildBuiltin();
            CatalogJson.Load(json);
            Assert.Equal(2300, Catalog.MustChar("C001").Hp);
            Assert.Equal("焰刃", Catalog.MustChar("C001").Name);
            Assert.True(Catalog.Skills.ContainsKey("C001_tap"));
            var destDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "client", "Assets", "Content"));
            if (Directory.Exists(Path.GetDirectoryName(destDir)))
            {
                Directory.CreateDirectory(destDir);
                File.WriteAllText(Path.Combine(destDir, "catalog.json"), json);
            }
        }

        [Fact]
        public void SameSeedSameLog()
        {
            var a = RunAuto(42, 200);
            var b = RunAuto(42, 200);
            Assert.Equal(a, b);
        }

        [Fact]
        public void TapAddsSixDrive()
        {
            var sim = NewSim(7);
            ChargeAll(sim);
            Assert.True(sim.TryTap(0));
            Assert.Equal(6f, sim.Drive);
        }

        [Fact]
        public void SlideAddsFourteenDrive()
        {
            var sim = NewSim(8);
            ChargeAll(sim);
            Assert.True(sim.TrySlide(0));
            Assert.Equal(14f, sim.Drive);
        }

        [Fact]
        public void PerfectDrivesReachFever()
        {
            var sim = NewSim(9);
            for (int n = 0; n < 4 && !sim.FeverActive; n++)
            {
                ChargeAll(sim);
                sim.Drive = 100f;
                Assert.True(sim.TryBeginDrive(0));
                Assert.True(sim.ResolveDrive(DriveTiming.Perfect));
            }
            Assert.True(sim.FeverActive);
            Assert.Equal(70, sim.FeverHitsLeft);
        }

        [Fact]
        public void AutoBattleDealsDamageOrFinishes()
        {
            var sim = NewSim(11);
            sim.AutoTap = true;
            var start = EnemyHp(sim);
            for (int i = 0; i < BattleSim.TickHz * 40; i++) sim.Tick();
            var end = EnemyHp(sim);
            Assert.True(end < start || sim.Outcome != BattleOutcome.InProgress);
        }

        [Fact]
        public void SaveRoundtrip()
        {
            var blob = new SaveBlob { LeaderSlot = 2, Vs1Cleared = true, Speed = 2, AutoTap = true };
            var parsed = SaveStore.Parse(SaveStore.Serialize(blob));
            Assert.Equal(2, parsed.LeaderSlot);
            Assert.True(parsed.Vs1Cleared);
            Assert.Equal(2, parsed.Speed);
            Assert.True(parsed.AutoTap);
        }

        static BattleSim NewSim(int seed)
        {
            return new BattleSim(Catalog.DefaultParty, 0, seed) { Deterministic = true, Speed = 1 };
        }

        static void ChargeAll(BattleSim sim)
        {
            for (int i = 0; i < sim.Allies.Length; i++)
                sim.Allies[i].Charge = 100f;
        }

        static int EnemyHp(BattleSim sim)
        {
            var n = 0;
            for (int i = 0; i < sim.Enemies.Count; i++) n += sim.Enemies[i].Hp;
            return n;
        }

        static string RunAuto(int seed, int ticks)
        {
            var sim = NewSim(seed);
            sim.AutoTap = true;
            for (int i = 0; i < ticks; i++) sim.Tick();
            return sim.Outcome + "/" + EnemyHp(sim) + "/" + sim.Drive + "/" + sim.TimeLeft;
        }
    }
}
