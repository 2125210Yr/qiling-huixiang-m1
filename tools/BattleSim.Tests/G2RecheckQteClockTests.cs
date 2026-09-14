using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// Q01/Q02 — the timeout-restore tick must deduct TimeLeft once.
    /// Existing G2Review Q01/Q02 do not assert that tick.
    /// </summary>
    public sealed class G2RecheckQteClockTests
    {
        [Fact]
        public void Q01_QteTimeoutRestoreTick_DeductsTimeLeftOnce()
        {
            var sim = NewJp(1701, AutoMode.Manual, forceNoCrit: true);
            InflateEnemies(sim);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
            LockFoes(sim);
            sim.Speed = 1;
            sim.Clocks.DriveQteTimeoutSec = BattleSim.TickDt;
            sim.Clocks.StageCountdownScalesWithSpeed = true;
            sim.Clocks.DriveQteScalesWithSpeed = true;
            sim.TimeLeft = 30f;

            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));
            var time0 = sim.TimeLeft;
            var drives0 = CountCasts(sim, SkillType.Drive, true);

            sim.Tick();

            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.Equal(DriveTiming.Good, sim.LastDriveTiming);
            Assert.Equal(drives0 + 1, CountCasts(sim, SkillType.Drive, true));
            Assert.Equal(time0 - BattleSim.TickDt, sim.TimeLeft, 3);
        }

        [Fact]
        public void Q02_QteTimeoutRestoreTick_Speed3_OppositeScalePolicies()
        {
            AssertRestoreTick(
                seed: 1702,
                speed: 3,
                stageScales: false,
                qteScales: true,
                expectedStageStep: BattleSim.TickDt);

            AssertRestoreTick(
                seed: 1703,
                speed: 3,
                stageScales: true,
                qteScales: false,
                expectedStageStep: BattleSim.TickDt * 3);
        }

        static void AssertRestoreTick(int seed, int speed, bool stageScales, bool qteScales, float expectedStageStep)
        {
            var sim = NewJp(seed, AutoMode.Manual, forceNoCrit: true);
            InflateEnemies(sim);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
            LockFoes(sim);
            sim.Speed = speed;
            sim.Clocks.DriveQteTimeoutSec = BattleSim.TickDt;
            sim.Clocks.StageCountdownScalesWithSpeed = stageScales;
            sim.Clocks.DriveQteScalesWithSpeed = qteScales;
            sim.TimeLeft = 40f;

            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));
            var time0 = sim.TimeLeft;
            var drives0 = CountCasts(sim, SkillType.Drive, true);

            sim.Tick();

            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.Equal(drives0 + 1, CountCasts(sim, SkillType.Drive, true));
            Assert.Equal(time0 - expectedStageStep, sim.TimeLeft, 3);
        }
    }
}
