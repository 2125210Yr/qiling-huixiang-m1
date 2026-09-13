using System;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class M1ClockModeTests
    {
        [Fact]
        public void GlClockSecondsStayUnknownAndConfigurable()
        {
            var names = Enum.GetNames(typeof(FormulaProfile));
            Assert.Contains("GL_UNKNOWN", names);
            Assert.DoesNotContain("GL_FINAL_VERIFIED", names);
            var sim = NewSim();
            Assert.Equal(FormulaProfile.GL_UNKNOWN, sim.Profile);
            Assert.Equal(FormulaStatus.NotMeasured, sim.Clocks.NumericStatus);
            Assert.Equal(DamageMath.DesignPlaceholderCode, sim.Clocks.NumericCode);
            Assert.Equal(BattleSim.UnknownFeverWindowSec, sim.Clocks.FeverWindowSec);
            sim.Clocks.FeverWindowSec = 3.5f;
            sim.Clocks.FeverHitBudget = 11;
            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));
            Assert.True(sim.ResolveDrive(DriveTiming.Perfect));
            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));
            Assert.True(sim.ResolveDrive(DriveTiming.Perfect));
            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));
            Assert.True(sim.ResolveDrive(DriveTiming.Perfect));
            Assert.True(sim.FeverActive);
            Assert.Equal(3.5f, sim.FeverLeft);
            Assert.Equal(11, sim.FeverHitsLeft);
            Assert.NotEqual(BattleSim.UnknownFeverWindowSec, sim.FeverLeft);
        }

        [Fact]
        public void PauseFreezesNamedBattleClocks()
        {
            var sim = ArmedClocks();
            var snap = Snap(sim);
            sim.Paused = true;
            for (int i = 0; i < BattleSim.TickHz; i++)
                sim.Tick();
            Assert.Equal(snap.TickIndex, sim.TickIndex);
            Assert.Equal(snap.TimeLeft, sim.TimeLeft);
            Assert.Equal(snap.Charge, sim.Allies[0].Charge);
            Assert.Equal(snap.SlideCd, sim.Allies[0].SlideCd);
            Assert.Equal(snap.AutoTimer, sim.Allies[0].AutoTimer);
            Assert.Equal(snap.StatusLeft, FirstStatus(sim.Allies[0]));
            Assert.Equal(snap.FeverLeft, sim.FeverLeft);
            Assert.Equal(snap.FeverHits, sim.FeverHitsLeft);
            Assert.Equal(0, sim.PendingDriveSlot);
            sim.TickFeverOnly();
            Assert.Equal(snap.FeverLeft, sim.FeverLeft);
        }

        [Fact]
        public void SpeedScalesNamedBattleClocks()
        {
            var slow = ArmedClocks(beginDrive: false);
            var fast = ArmedClocks(beginDrive: false);
            LockOtherAllies(slow);
            LockOtherAllies(fast);
            var time0 = slow.TimeLeft;
            var charge0 = slow.Allies[0].Charge;
            var cd0 = slow.Allies[0].SlideCd;
            var auto0 = slow.Allies[0].AutoTimer;
            var fever0 = slow.FeverLeft;
            fast.Speed = 2;
            const int n = 12;
            for (int i = 0; i < n; i++)
            {
                slow.Tick();
                fast.Tick();
            }
            Assert.InRange((time0 - fast.TimeLeft) / (time0 - slow.TimeLeft), 1.8f, 2.2f);
            Assert.InRange((fast.Allies[0].Charge - charge0) / (slow.Allies[0].Charge - charge0), 1.8f, 2.2f);
            Assert.InRange((cd0 - fast.Allies[0].SlideCd) / (cd0 - slow.Allies[0].SlideCd), 1.8f, 2.2f);
            Assert.InRange((fast.Allies[0].AutoTimer - auto0) / (slow.Allies[0].AutoTimer - auto0), 1.8f, 2.2f);
            Assert.InRange((fever0 - fast.FeverLeft) / (fever0 - slow.FeverLeft), 1.8f, 2.2f);
        }

        [Fact]
        public void SpeedThreeScalesNamedBattleClocks()
        {
            // Primary GT Robin battle chrome shows >> 3x SPEED.
            var slow = ArmedClocks(beginDrive: false);
            var fast = ArmedClocks(beginDrive: false);
            LockOtherAllies(slow);
            LockOtherAllies(fast);
            var time0 = slow.TimeLeft;
            var charge0 = slow.Allies[0].Charge;
            fast.Speed = 3;
            const int n = 12;
            for (int i = 0; i < n; i++)
            {
                slow.Tick();
                fast.Tick();
            }
            Assert.InRange((time0 - fast.TimeLeft) / (time0 - slow.TimeLeft), 2.7f, 3.3f);
            Assert.InRange((fast.Allies[0].Charge - charge0) / (slow.Allies[0].Charge - charge0), 2.7f, 3.3f);
        }

        [Fact]
        public void ClockPolicyCanExemptFeverFromSpeed()
        {
            var scaled = ArmedClocks(beginDrive: false);
            var frozen = ArmedClocks(beginDrive: false);
            scaled.Speed = 2;
            frozen.Speed = 2;
            frozen.Clocks.FeverWindowScalesWithSpeed = false;
            for (int i = 0; i < 9; i++)
            {
                scaled.Tick();
                frozen.Tick();
            }
            Assert.True(scaled.FeverLeft < frozen.FeverLeft);
            Assert.Equal(3f - BattleSim.TickDt * 9, frozen.FeverLeft, 3);
        }

        [Fact]
        public void QteWaitAdvancesOnlyQteAndStageClocks()
        {
            var sim = NewSim();
            LockFoes(sim);
            ChargeAll(sim);
            sim.Drive = 100f;
            sim.Allies[0].Charge = 40f;
            sim.Allies[0].SlideCd = 4f;
            Assert.True(sim.TryBeginDrive(0));
            var charge = sim.Allies[0].Charge;
            var cd = sim.Allies[0].SlideCd;
            var time = sim.TimeLeft;
            for (int i = 0; i < 10; i++)
                sim.Tick();
            Assert.Equal(0, sim.PendingDriveSlot);
            Assert.True(sim.TimeLeft < time);
            Assert.Equal(charge, sim.Allies[0].Charge);
            Assert.Equal(cd, sim.Allies[0].SlideCd);
        }

        [Fact]
        public void StayHeldFreezesManualAndFull()
        {
            StayHeldFreezes(AutoMode.Manual);
            StayHeldFreezes(AutoMode.Semi);
            StayHeldFreezes(AutoMode.Full);
        }

        [Fact]
        public void StayHeldSurvivesCatchUpTicksOnFull()
        {
            var sim = NewSim();
            sim.Auto = AutoMode.Full;
            ChargeAll(sim);
            sim.Drive = 100f;
            sim.StayHeld();
            Assert.True(sim.HoldSticky);
            var time = sim.TimeLeft;
            var drive = sim.Drive;
            for (int i = 0; i < BattleSim.TickHz * 3; i++)
                sim.Tick();
            Assert.True(sim.HoldSim);
            Assert.True(sim.HoldSticky);
            Assert.Equal(time, sim.TimeLeft);
            Assert.Equal(drive, sim.Drive);
            Assert.Equal(BattleOutcome.InProgress, sim.Outcome);
            Assert.False(SliceDriveSequence.HasDriveCast(sim));
            sim.HoldSim = false;
            sim.Tick();
            Assert.False(sim.HoldSim);
            Assert.True(sim.TimeLeft < time);
        }

        [Fact]
        public void HoldWatchdogStillReleasesNonStickyHoldOnFull()
        {
            var sim = NewSim();
            sim.Auto = AutoMode.Full;
            sim.HoldSim = true;
            Assert.False(sim.HoldSticky);
            var t = sim.TimeLeft;
            var cap = (int)(BattleSim.HoldTimeoutSec * BattleSim.TickHz) + 2;
            for (int i = 0; i < cap; i++)
                sim.Tick();
            Assert.False(sim.HoldSim);
            Assert.True(sim.TimeLeft < t);
        }

        [Fact]
        public void ManualDoesNotAutoCastSkillsOrDrive()
        {
            var sim = NewSim();
            sim.Auto = AutoMode.Manual;
            ChargeAll(sim);
            sim.Drive = 100f;
            sim.Tick();
            Assert.Equal(100f, sim.Allies[0].Charge);
            Assert.Equal(100f, sim.Drive);
            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.False(SliceDriveSequence.HasDriveCast(sim));
        }

        [Fact]
        public void SemiAutoCastsSkillsButNotDrive()
        {
            var sim = NewSim();
            sim.Auto = AutoMode.Semi;
            ChargeAll(sim);
            sim.Drive = 100f;
            sim.Tick();
            Assert.True(sim.Allies[0].Charge < 100f);
            Assert.Equal(100f, sim.Drive);
            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.False(SliceDriveSequence.HasDriveCast(sim));
        }

        [Fact]
        public void FullAutoCastsDriveAfterHoldClears()
        {
            var sim = NewSim();
            sim.Auto = AutoMode.Full;
            ChargeAll(sim);
            sim.Drive = 100f;
            sim.StayHeld();
            sim.Tick();
            Assert.False(SliceDriveSequence.HasDriveCast(sim));
            sim.HoldSim = false;
            sim.Tick();
            Assert.True(SliceDriveSequence.HasDriveCast(sim) || sim.Drive < 100f);
        }

        [Fact]
        public void DeathWaveAndResultEventsAreLogged()
        {
            var stage = new StageDef
            {
                Id = "CLK-WV",
                Name = "clock-wave",
                TimeLimitSec = 45f,
                Wave0 = new[] { "E001" },
                Wave1 = new[] { "E001" }
            };
            var sim = new BattleSim(new[] { "C001" }, 0, 3, stage, null)
            {
                Deterministic = true,
                Auto = AutoMode.Manual,
                Speed = 1
            };
            Assert.Equal(0, sim.WaveIndex);
            Assert.True(MvpLoop.HasEventKind(sim, "wave"));
            var afterEnter = sim.Events.Events.Count;
            sim.Enemies[0].Hp = 0;
            Assert.True(MvpLoop.TryPlayUntilKindSince(sim, "wave", afterEnter, 8, out _));
            Assert.Equal(1, sim.WaveIndex);
            Assert.True(MvpLoop.HasEventKind(sim, "death"));
            Assert.Contains(sim.Events.Events, e => e.Kind == "wave" && e.Opcode == "advance" && e.Amount == 1);

            var afterWave = sim.Events.Events.Count;
            sim.Enemies[0].Hp = 0;
            Assert.True(MvpLoop.TryPlayUntilKindSince(sim, "result", afterWave, 8, out _));
            Assert.Equal(BattleOutcome.Victory, sim.Outcome);
            Assert.Contains(sim.Events.Events, e => e.Kind == "result" && e.Opcode == "clear");

            var wipe = new BattleSim(new[] { "C001" }, 0, 4, stage, null)
            {
                Deterministic = true,
                Auto = AutoMode.Manual
            };
            wipe.Allies[0].Hp = 0;
            wipe.Tick();
            Assert.Equal(BattleOutcome.Defeat, wipe.Outcome);
            Assert.Contains(wipe.Events.Events, e => e.Kind == "result" && e.Opcode == "wipe");
            Assert.Contains(wipe.Events.Events, e => e.Kind == "death" && e.TargetAlly);

            var timeout = NewSim();
            timeout.TimeLeft = BattleSim.TickDt * 0.25f;
            timeout.Tick();
            Assert.Equal(BattleOutcome.Defeat, timeout.Outcome);
            Assert.Contains(timeout.Events.Events, e => e.Kind == "result" && e.Opcode == "timeout");
        }

        static void StayHeldFreezes(AutoMode mode)
        {
            var sim = NewSim();
            sim.Auto = mode;
            ChargeAll(sim);
            sim.Drive = 100f;
            var time = sim.TimeLeft;
            for (int i = 0; i < BattleSim.TickHz * 2; i++)
            {
                sim.StayHeld();
                sim.Tick();
            }
            Assert.Equal(time, sim.TimeLeft);
            Assert.True(sim.HoldSim);
            Assert.True(sim.HoldSticky);
            Assert.Equal(BattleOutcome.InProgress, sim.Outcome);
            Assert.False(SliceDriveSequence.HasDriveCast(sim));
        }

        static BattleSim NewSim()
        {
            return new BattleSim(Catalog.DefaultParty, 0, 3)
            {
                Deterministic = true,
                Speed = 1,
                Auto = AutoMode.Manual
            };
        }

        static BattleSim ArmedClocks(bool beginDrive = true)
        {
            var sim = NewSim();
            LockFoes(sim);
            sim.Allies[0].Charge = 40f;
            sim.Allies[0].SlideCd = 5f;
            sim.Allies[0].AutoTimer = 0.2f;
            sim.FeverActive = true;
            sim.FeverEver = true;
            sim.FeverLeft = 3f;
            sim.FeverHitsLeft = 40;
            if (beginDrive)
            {
                sim.Drive = 100f;
                sim.Allies[0].Charge = 100f;
                Assert.True(sim.TryBeginDrive(0));
            }
            return sim;
        }

        static void LockFoes(BattleSim sim)
        {
            var stun = Catalog.TryEffect("stun");
            for (int i = 0; i < sim.Enemies.Count; i++)
                sim.ApplyStatus(sim.Enemies[i], stun);
        }

        static void LockOtherAllies(BattleSim sim)
        {
            var stun = Catalog.TryEffect("stun");
            for (int i = 1; i < sim.Allies.Length; i++)
                sim.ApplyStatus(sim.Allies[i], stun);
        }

        static void ChargeAll(BattleSim sim)
        {
            for (int i = 0; i < sim.Allies.Length; i++)
            {
                if (sim.Allies[i] != null)
                    sim.Allies[i].Charge = 100f;
            }
        }

        static float FirstStatus(UnitState u)
        {
            if (u == null || u.Status.Count == 0) return 0f;
            return u.Status[0].Remaining;
        }

        static ClockSnap Snap(BattleSim sim)
        {
            return new ClockSnap
            {
                TickIndex = sim.TickIndex,
                TimeLeft = sim.TimeLeft,
                Charge = sim.Allies[0].Charge,
                SlideCd = sim.Allies[0].SlideCd,
                AutoTimer = sim.Allies[0].AutoTimer,
                StatusLeft = FirstStatus(sim.Allies[0]),
                FeverLeft = sim.FeverLeft,
                FeverHits = sim.FeverHitsLeft
            };
        }

        static float Ratio(float slowNow, float fastNow, float start)
        {
            var slowDelta = start - slowNow;
            var fastDelta = start - fastNow;
            Assert.True(slowDelta > 0f);
            return fastDelta / slowDelta;
        }

        struct ClockSnap
        {
            public int TickIndex;
            public float TimeLeft;
            public float Charge;
            public float SlideCd;
            public float AutoTimer;
            public float StatusLeft;
            public float FeverLeft;
            public int FeverHits;
        }
    }
}
