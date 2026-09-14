using System;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// G2 review rows Q01–Q04 (REGRESSION_MATRIX.md). Contract: API_CONTRACT.md §3.
    /// The core QTE clock (QteElapsed/QteLimitSec/QteRemaining) is the single authority;
    /// the HUD must read it instead of owning a second timer.
    /// </summary>
    public sealed class G2ReviewQteClockTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Q01_Qte_OneAuthoritativeClock(int speed)
        {
            var sim = NewJp(200 + speed, AutoMode.Manual);
            InflateEnemies(sim);
            sim.Speed = speed;
            Assert.True(sim.Clocks.DriveQteScalesWithSpeed);
            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.Equal(0f, sim.QteElapsed);
            Assert.Equal(0f, sim.QteRemaining);
            Assert.Equal(-1, sim.LastDriveResolveTick);

            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));
            Assert.Equal(0, sim.PendingDriveSlot);
            Assert.Equal(sim.Clocks.DriveQteTimeoutSec, sim.QteLimitSec);
            Assert.Equal(0f, sim.QteElapsed);
            Assert.Equal(sim.QteLimitSec, sim.QteRemaining);
            var driveCasts0 = CountCasts(sim, SkillType.Drive, true);
            var driveCastEvents0 = CountEvents(sim, e => e.Kind == "cast" && e.Channel == SkillType.Drive && e.CasterAlly);

            const int probe = 10;
            var perTick = BattleSim.TickDt * speed;
            for (int i = 1; i <= probe; i++)
            {
                sim.Tick();
                Assert.Equal(0, sim.PendingDriveSlot);
                Assert.Equal(perTick * i, sim.QteElapsed, 3);
                Assert.Equal(Math.Max(0f, sim.QteLimitSec - sim.QteElapsed), sim.QteRemaining, 4);
            }

            // Let the core time the QTE out on its own; nothing else may resolve it.
            var expectedTicks = (int)Math.Ceiling(sim.QteLimitSec / perTick);
            var ticks = probe;
            for (int i = 0; i < expectedTicks + 5 && sim.PendingDriveSlot >= 0; i++)
            {
                sim.Tick();
                ticks++;
            }
            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.InRange(ticks, expectedTicks - 1, expectedTicks + 1);
            Assert.Equal(DriveTiming.Good, sim.LastDriveTiming);
            Assert.Equal(sim.TickIndex, sim.LastDriveResolveTick);
            Assert.Equal(0, sim.LastDriveResolveSlot);
            Assert.Equal(0f, sim.QteElapsed);
            Assert.Equal(0f, sim.QteRemaining);
            Assert.Equal(driveCasts0 + 1, CountCasts(sim, SkillType.Drive, true));
            Assert.Equal(driveCastEvents0 + 1, CountEvents(sim, e => e.Kind == "cast" && e.Channel == SkillType.Drive && e.CasterAlly));

            // A late "second timeout" from any other owner must be a no-op.
            var hp = SnapshotHp(sim);
            var ev = sim.Events.Events.Count;
            Assert.Equal(DriveResolveResult.NoPending, sim.ResolveDriveChecked(DriveTiming.Good));
            Assert.False(sim.ResolveDrive(DriveTiming.Good));
            Assert.Equal(hp, SnapshotHp(sim));
            Assert.Equal(ev, sim.Events.Events.Count);
            Assert.Equal(driveCasts0 + 1, CountCasts(sim, SkillType.Drive, true));
        }

        [Fact]
        public void Q01_QteClockIgnoresSpeedWhenPolicySaysSo()
        {
            var sim = NewJp(210, AutoMode.Manual);
            InflateEnemies(sim);
            sim.Speed = 3;
            sim.Clocks.DriveQteScalesWithSpeed = false;
            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));
            for (int i = 1; i <= 6; i++)
            {
                sim.Tick();
                Assert.Equal(BattleSim.TickDt * i, sim.QteElapsed, 4);
            }
        }

        [Fact]
        public void Q02_Qte_StageClockRespectsItsPolicy()
        {
            var sim = NewJp(220, AutoMode.Manual);
            InflateEnemies(sim);
            sim.Speed = 3;
            sim.Clocks.StageCountdownScalesWithSpeed = false;
            sim.Clocks.DriveQteScalesWithSpeed = true;
            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));

            var time0 = sim.TimeLeft;
            var qte0 = sim.QteElapsed;
            const int n = 10;
            for (int i = 1; i <= n; i++)
            {
                sim.Tick();
                Assert.Equal(0, sim.PendingDriveSlot);
                Assert.Equal(time0 - BattleSim.TickDt * i, sim.TimeLeft, 3);          // stage: unscaled
                Assert.Equal(qte0 + BattleSim.TickDt * 3 * i, sim.QteElapsed, 3);     // qte: ×3
            }

            // Mirror: stage scales, QTE does not — the two policies are independent in both directions.
            var mirror = NewJp(221, AutoMode.Manual);
            InflateEnemies(mirror);
            mirror.Speed = 3;
            mirror.Clocks.StageCountdownScalesWithSpeed = true;
            mirror.Clocks.DriveQteScalesWithSpeed = false;
            ChargeAll(mirror);
            mirror.Drive = 100f;
            Assert.True(mirror.TryBeginDrive(0));
            var mTime0 = mirror.TimeLeft;
            for (int i = 1; i <= n; i++)
            {
                mirror.Tick();
                Assert.Equal(mTime0 - BattleSim.TickDt * 3 * i, mirror.TimeLeft, 3);
                Assert.Equal(BattleSim.TickDt * i, mirror.QteElapsed, 3);
            }
        }

        [Fact]
        public void Q03_ResolveDrive_RejectsTerminalBattle()
        {
            var sim = NewJp(230, AutoMode.Manual);
            InflateEnemies(sim);
            ChargeAll(sim);
            sim.Drive = 100f;
            sim.FeverGauge = 55f;
            Assert.True(sim.TryBeginDrive(1));
            Assert.Equal(1, sim.PendingDriveSlot);
            for (int i = 0; i < 3; i++) sim.Tick();

            sim.Outcome = BattleOutcome.Defeat;
            var hp = SnapshotHp(sim);
            var drive = sim.Drive;
            var fever = sim.FeverGauge;
            var feverActive = sim.FeverActive;
            var events = sim.Events.Events.Count;
            var casts = sim.Casts.Count;
            var log = sim.Log.Count;
            var timing = sim.LastDriveTiming;
            var charge = sim.Allies[1].Charge;
            var dealt = sim.Stats.TotalDealt;

            Assert.Equal(DriveResolveResult.NotInProgress, sim.ResolveDriveChecked(DriveTiming.Perfect));
            Assert.False(sim.ResolveDrive(DriveTiming.Perfect));
            var late = sim.Submit(Cmd(BattleCommandKind.DriveResolve, 1, DriveTiming.Perfect));
            Assert.False(late.Accepted);
            Assert.Equal(CommandReject.NotInProgress, late.Reason);

            Assert.Equal(hp, SnapshotHp(sim));
            Assert.Equal(drive, sim.Drive);
            Assert.Equal(fever, sim.FeverGauge);
            Assert.Equal(feverActive, sim.FeverActive);
            Assert.Equal(events, sim.Events.Events.Count);
            Assert.Equal(casts, sim.Casts.Count);
            Assert.Equal(log, sim.Log.Count);
            Assert.Equal(timing, sim.LastDriveTiming);
            Assert.Equal(-1, sim.LastDriveResolveTick);
            Assert.Equal(charge, sim.Allies[1].Charge);
            Assert.Equal(dealt, sim.Stats.TotalDealt);
            Assert.Equal(0, CountCasts(sim, SkillType.Drive, true));
        }

        [Fact]
        public void Q04_PausedQteHasNoLateSideEffects()
        {
            var sim = NewJp(240, AutoMode.Manual);
            InflateEnemies(sim);
            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));
            for (int i = 0; i < 5; i++) sim.Tick();
            var elapsed = sim.QteElapsed;
            Assert.True(elapsed > 0f);

            sim.Paused = true;
            var hp = SnapshotHp(sim);
            var drive = sim.Drive;
            var fever = sim.FeverGauge;
            var events = sim.Events.Events.Count;
            var casts = sim.Casts.Count;

            Assert.Equal(DriveResolveResult.Paused, sim.ResolveDriveChecked(DriveTiming.Perfect));
            Assert.False(sim.ResolveDrive(DriveTiming.Perfect));
            var paused = sim.Submit(Cmd(BattleCommandKind.DriveResolve, 0, DriveTiming.Perfect));
            Assert.False(paused.Accepted);
            Assert.Equal(CommandReject.Paused, paused.Reason);
            for (int i = 0; i < 10; i++) sim.Tick();

            Assert.Equal(0, sim.PendingDriveSlot);
            Assert.Equal(elapsed, sim.QteElapsed);
            Assert.Equal(hp, SnapshotHp(sim));
            Assert.Equal(drive, sim.Drive);
            Assert.Equal(fever, sim.FeverGauge);
            Assert.Equal(events, sim.Events.Events.Count);
            Assert.Equal(casts, sim.Casts.Count);
            Assert.Equal(-1, sim.LastDriveResolveTick);

            // Unpause: exactly one resolve is honoured, a duplicate callback is a no-op.
            sim.Paused = false;
            Assert.Equal(DriveResolveResult.Accepted, sim.ResolveDriveChecked(DriveTiming.Perfect));
            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.Equal(DriveTiming.Perfect, sim.LastDriveTiming);
            Assert.Equal(sim.TickIndex, sim.LastDriveResolveTick);
            Assert.Equal(1, CountCasts(sim, SkillType.Drive, true));
            var hp2 = SnapshotHp(sim);
            var ev2 = sim.Events.Events.Count;
            var fever2 = sim.FeverGauge;
            Assert.Equal(DriveResolveResult.NoPending, sim.ResolveDriveChecked(DriveTiming.Perfect));
            Assert.False(sim.ResolveDrive(DriveTiming.Perfect));
            var dup = sim.Submit(Cmd(BattleCommandKind.DriveResolve, 0, DriveTiming.Perfect));
            Assert.False(dup.Accepted);
            Assert.Equal(CommandReject.NoQtePending, dup.Reason);
            Assert.Equal(hp2, SnapshotHp(sim));
            Assert.Equal(ev2, sim.Events.Events.Count);
            Assert.Equal(fever2, sim.FeverGauge);
            Assert.Equal(1, CountCasts(sim, SkillType.Drive, true));
        }

        [Fact]
        public void Q04_ResolveWithoutPendingIsNoPending()
        {
            var sim = NewJp(241, AutoMode.Manual);
            Assert.Equal(-1, sim.PendingDriveSlot);
            var hp = SnapshotHp(sim);
            var events = sim.Events.Events.Count;
            Assert.Equal(DriveResolveResult.NoPending, sim.ResolveDriveChecked(DriveTiming.Perfect));
            Assert.False(sim.ResolveDrive(DriveTiming.Great));
            var r = sim.Submit(Cmd(BattleCommandKind.DriveResolve, 0, DriveTiming.Perfect));
            Assert.False(r.Accepted);
            Assert.Equal(CommandReject.NoQtePending, r.Reason);
            Assert.Equal(hp, SnapshotHp(sim));
            Assert.Equal(events, sim.Events.Events.Count);
            Assert.Equal(0f, sim.FeverGauge);
            Assert.Equal(-1, sim.LastDriveResolveTick);
        }

        [Fact]
        public void Q04_TimeoutDuringQteCancelsWithoutLateDamage()
        {
            var sim = NewJp(242, AutoMode.Manual);
            InflateEnemies(sim);
            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));
            sim.TimeLeft = BattleSim.TickDt * 0.5f;
            sim.Tick();
            Assert.Equal(BattleOutcome.Defeat, sim.Outcome);
            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.Equal(0, CountCasts(sim, SkillType.Drive, true));
            var hp = SnapshotHp(sim);
            var ev = sim.Events.Events.Count;
            var late = sim.ResolveDriveChecked(DriveTiming.Perfect);
            Assert.True(late == DriveResolveResult.NoPending || late == DriveResolveResult.NotInProgress, "late=" + late);
            Assert.False(sim.ResolveDrive(DriveTiming.Perfect));
            Assert.Equal(hp, SnapshotHp(sim));
            Assert.Equal(ev, sim.Events.Events.Count);
            Assert.Equal(0, CountCasts(sim, SkillType.Drive, true));
        }
    }
}
