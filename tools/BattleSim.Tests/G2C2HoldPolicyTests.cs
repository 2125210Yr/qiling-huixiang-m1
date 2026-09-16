using System;
using System.IO;
using System.Reflection;
using Resonance.App;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// C2-T1 / C2-T1b — core-owned showtime / drive / wave holds replay without HUD.
    /// Seconds are DESIGN_PLACEHOLDER (VfxShowtime.Duration / 0.70 / PhaseLifeSec), not GL.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2C2HoldPolicyTests
    {
        const int Seed = 2201;

        [Fact]
        public void C2_T1_SlideHold_FreezesClocksThenReleases_AndReplays()
        {
            var sim = SlideFactory(Seed);
            var slot = 0;
            Assert.True(sim.Allies[slot] != null && sim.Allies[slot].Charge >= 100f);
            var slide = sim.Submit(BattleCommand.Slide(slot, CommandSource.Player));
            Assert.True(slide.Accepted, "Slide rejected: " + slide.Reason);
            Assert.True(sim.HoldSim);
            Assert.Equal("slide", sim.PolicyHoldKind);

            var budget = BattleSim.TicksForPolicyHold(sim.Clocks.SlideShowtimeHoldSec);
            Assert.Equal(budget, sim.PolicyHoldTicksLeft);
            Assert.True(budget > 0);

            var t0 = sim.TimeLeft;
            var c0 = sim.Allies[slot].Charge;
            var tick0 = sim.TickIndex;
            for (int i = 0; i < budget; i++)
            {
                Assert.True(sim.HoldSim, "policy hold should still be up before tick " + i);
                Assert.Equal(t0, sim.TimeLeft);
                Assert.Equal(c0, sim.Allies[slot].Charge);
                sim.Tick();
            }

            Assert.False(sim.HoldSim);
            Assert.Equal(0, sim.PolicyHoldTicksLeft);
            Assert.Equal(tick0 + budget, sim.TickIndex);
            Assert.Equal(t0, sim.TimeLeft);

            sim.Tick();
            Assert.True(sim.TimeLeft < t0, "first free tick must spend stage time");

            AssertReplayMatch(sim, SlideFactory);
        }

        [Fact]
        public void C2_T1_HoldSimSetter_IsNotPublic()
        {
            var prop = typeof(BattleSim).GetProperty("HoldSim");
            Assert.NotNull(prop);
            Assert.True(prop.CanRead);
            Assert.Null(prop.GetSetMethod(false));
            var nonPublic = prop.GetSetMethod(true);
            Assert.NotNull(nonPublic);
            Assert.True(nonPublic.IsAssembly || nonPublic.IsPrivate || nonPublic.IsFamilyOrAssembly);
        }

        [Fact]
        public void C2_T1b_DriveCastHold_Replays()
        {
            var sim = DriveFactory(Seed);
            Assert.False(sim.HoldSim);
            sim.Tick();
            Assert.True(sim.HoldSim, "Auto Full Drive cast must start a core hold");
            Assert.Equal("drive", sim.PolicyHoldKind);

            var budget = BattleSim.TicksForPolicyHold(sim.Clocks.DriveCastHoldSec);
            Assert.Equal(budget, sim.PolicyHoldTicksLeft);
            var t0 = sim.TimeLeft;
            var tick0 = sim.TickIndex;
            for (int i = 0; i < budget; i++)
            {
                Assert.True(sim.HoldSim, "drive hold before tick " + i);
                Assert.Equal(t0, sim.TimeLeft);
                sim.Tick();
            }
            Assert.False(sim.HoldSim);
            Assert.Equal(tick0 + budget, sim.TickIndex);
            Assert.Equal(t0, sim.TimeLeft);

            AssertReplayMatch(sim, DriveFactory);
        }

        [Fact]
        public void C2_T1b_WaveAdvanceHold_Replays()
        {
            var sim = WaveFactory(Seed);
            Assert.Equal(0, sim.WaveIndex);
            KillWave0WithTaps(sim);
            Assert.Equal(1, sim.WaveIndex);
            Assert.True(sim.HoldSim);
            Assert.Equal("wave", sim.PolicyHoldKind);

            var budget = BattleSim.TicksForPolicyHold(sim.Clocks.WaveAdvanceHoldSec);
            Assert.Equal(budget, sim.PolicyHoldTicksLeft);
            var t0 = sim.TimeLeft;
            for (int i = 0; i < budget; i++)
            {
                Assert.True(sim.HoldSim, "wave hold before tick " + i);
                Assert.Equal(t0, sim.TimeLeft);
                sim.Tick();
            }
            Assert.False(sim.HoldSim);
            Assert.Equal(t0, sim.TimeLeft);

            AssertReplayMatch(sim, WaveFactory);
        }

        [Fact]
        public void C2_T1_LegacyBasicTape_HoldUnrecorded_VerifyMatchFalse()
        {
            var dir = LegacyBasicTapeDir();
            Assert.True(Directory.Exists(dir), "legacy tape dir missing: " + dir);

            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var note = VerificationCatalog.Apply(VerificationRunPlan.Resolve("np.basic.v1", null));
                    Assert.DoesNotContain("apply failed", note, StringComparison.Ordinal);
                    NaturalPlayBattleEvidence.BindOpeningPolicy(new NaturalPlayBattleEvidence
                    {
                        ScenarioName = VerificationScenario.Basic.Name
                    });

                    BattleRunRecord rec;
                    Assert.True(NaturalPlayBattleEvidence.TryReadRecord(dir, out rec) && rec != null);
                    var report = BattleReplayer.Verify(rec, NaturalPlayBattleEvidence.ReplayFactory(rec));
                    Assert.False(report.Match, "legacy 1b2ca8e basic tape must stay Match=False (HUD hold was never recorded)");
                    Assert.True(
                        report.DigestDiff.Count > 0
                        || report.EventDiff.Count > 0
                        || report.FirstEventDivergence >= 0,
                        "legacy FAIL must still name a digest/event gap, not a silent pass");
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                    DesignPlaceholderPolicy.Bind(null);
                }
            }
        }

        static void AssertReplayMatch(BattleSim original, Func<int, BattleSim> factory)
        {
            var stageId = original.ActiveStage != null ? original.ActiveStage.Id : "VS-1";
            var rec = BattleRunRecord.Capture(original, Catalog.DefaultParty, stageId);
            var report = BattleReplayer.Verify(rec, factory);
            Assert.True(report.Match, "Diff: " + string.Join(" | ", report.Diff));
            Assert.Empty(report.DigestDiff);
            Assert.Empty(report.EventDiff);
            Assert.NotNull(report.Replayed);
            Assert.Equal(original.TickIndex, report.Replayed.TickIndex);
            Assert.Equal(original.TimeLeft, report.Replayed.TimeLeft, 3);
            Assert.Equal(original.Drive, report.Replayed.Drive, 3);
        }

        static BattleSim SlideFactory(int seed)
        {
            var sim = NewJp(seed, AutoMode.Manual, forceNoCrit: true);
            InflateEnemies(sim);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
            ChargeAll(sim);
            for (int i = 1; i < sim.Allies.Length; i++)
                if (sim.Allies[i] != null) sim.Allies[i].Charge = 0f;
            return sim;
        }

        static BattleSim DriveFactory(int seed)
        {
            var sim = NewJp(seed, AutoMode.Full, forceNoCrit: true);
            InflateEnemies(sim);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
            ChargeAll(sim);
            for (int i = 1; i < sim.Allies.Length; i++)
                if (sim.Allies[i] != null) sim.Allies[i].Charge = 0f;
            sim.Drive = 100f;
            return sim;
        }

        static BattleSim WaveFactory(int seed)
        {
            var sim = NewJp(seed, AutoMode.Manual, forceNoCrit: true);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
            ChargeAll(sim);
            WeakenWave0(sim);
            return sim;
        }

        static void WeakenWave0(BattleSim sim)
        {
            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                var e = sim.Enemies[i];
                if (e == null) continue;
                e.MaxHp = Math.Max(1, e.MaxHp);
                e.Hp = 1;
            }
        }

        static void KillWave0WithTaps(BattleSim sim)
        {
            var cap = BattleSim.TickHz * 40;
            for (int n = 0; n < cap && sim.WaveIndex == 0 && sim.Outcome == BattleOutcome.InProgress; n++)
            {
                if (!sim.HoldSim && !sim.Paused
                    && sim.Allies[0] != null && sim.Allies[0].Alive && sim.Allies[0].Charge >= 100f)
                {
                    var tap = sim.Submit(BattleCommand.Tap(0, CommandSource.Player));
                    Assert.True(tap.Accepted, "wave-0 tap: " + tap.Reason);
                }
                sim.Tick();
            }
            Assert.Equal(1, sim.WaveIndex);
        }

        static string LegacyBasicTapeDir()
        {
            var rel = Path.Combine(
                "DC_RECON_KIT", "m1", "REVIEW_1b2ca8e", "artifacts", "natural-play",
                "20260914T152459-np_basic_v1", "natural-play", "battles", "np-20260914T152600-001");
            var root = FindRepoRoot();
            return Path.GetFullPath(Path.Combine(root, rel));
        }

        static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 10 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "DC_RECON_KIT")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 10 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "DC_RECON_KIT")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
