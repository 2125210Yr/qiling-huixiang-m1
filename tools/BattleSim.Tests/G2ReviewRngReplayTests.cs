using System;
using System.Collections.Generic;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// G2 review rows R01–R03 (REGRESSION_MATRIX.md). Contract: API_CONTRACT.md §1/§4.
    /// The seed alone decides the random stream; ForceNoCrit is a formula fixture, never a preview default.
    /// </summary>
    public sealed class G2ReviewRngReplayTests
    {
        const int CritRunTicks = BattleSim.TickHz * 25;

        [Fact]
        public void R01_SeededRandom_StillPermitsCriticalHits()
        {
            var probe = new BattleSim(Catalog.DefaultParty, 0, 301);
            Assert.False(probe.ForceNoCrit);
            Assert.False(probe.Deterministic);
            Assert.True(DamageMath.CritChance(probe.Allies[0].Def.Crt) > 0.1f, "default attacker crit chance too low for this probe");

            var totalCrits = 0;
            var totalHits = 0;
            var seeds = new[] { 301, 302, 303 };
            for (int s = 0; s < seeds.Length; s++)
            {
                var sim = NewJp(seeds[s], AutoMode.Full);
                Assert.False(sim.ForceNoCrit);
                InflateEnemies(sim);
                sim.TimeLeft = 999f;
                for (int i = 0; i < CritRunTicks && sim.Outcome == BattleOutcome.InProgress; i++) sim.Tick();
                for (int i = 0; i < sim.Log.Count; i++)
                {
                    var t = sim.Log[i];
                    if (t.Heal || t.Text == null || t.Text.StartsWith("FX ", StringComparison.Ordinal)) continue;
                    totalHits++;
                    if (t.Crit) totalCrits++;
                }
            }
            Assert.True(totalHits > 50, "hits=" + totalHits);
            Assert.True(totalCrits > 0, "seeded runs never rolled a critical hit across " + totalHits + " hits");
        }

        [Fact]
        public void R01_SameSeedSameRandomStreamWithCritsEnabled()
        {
            var a = RunFull(311, CritRunTicks, forceNoCrit: false);
            var b = RunFull(311, CritRunTicks, forceNoCrit: false);
            Assert.Equal(a.Events.ComputeHash(), b.Events.ComputeHash());
            Assert.Equal(SnapshotHp(a), SnapshotHp(b));
            Assert.Equal(CountCrits(a), CountCrits(b));
            var c = RunFull(312, CritRunTicks, forceNoCrit: false);
            Assert.NotEqual(a.Events.ComputeHash(), c.Events.ComputeHash());
        }

        [Fact]
        public void R03_ForceNoCrit_IsFixtureOnly()
        {
            var preview = new BattleSim(Catalog.DefaultParty, 0, 321);
            Assert.False(preview.ForceNoCrit);
            Assert.False(preview.Deterministic);
            var stage = new BattleSim(Catalog.DefaultParty, 0, 321, Catalog.Stages[0], null, new BattleMods());
            Assert.False(stage.ForceNoCrit);

            // Legacy alias only forwards to ForceNoCrit.
            preview.Deterministic = true;
            Assert.True(preview.ForceNoCrit);
            preview.ForceNoCrit = false;
            Assert.False(preview.Deterministic);

            var fixture = RunFull(322, CritRunTicks, forceNoCrit: true);
            Assert.True(fixture.ForceNoCrit);
            Assert.Equal(0, CountCrits(fixture));
            Assert.True(CountDamageTexts(fixture) > 50, "fixture run dealt too few hits to be meaningful");

            var live = RunFull(322, CritRunTicks, forceNoCrit: false);
            Assert.True(CountCrits(live) > 0, "same seed with crits enabled rolled none");
        }

        [Fact]
        public void R02_SeedAndCommands_ReplayStateAndEvents()
        {
            const int seed = 331;
            const int iterations = 420;
            var a = NewJp(seed, AutoMode.Manual);
            var script = new ScriptedDriver();
            for (int i = 0; i < iterations; i++)
            {
                script.BeforeTick(a, i);
                a.Tick();
            }
            Assert.True(script.TapAccepted, "scripted Tap was never accepted");
            Assert.True(script.SlideAccepted, "scripted Slide was never accepted");
            Assert.True(script.DriveBegun && script.DriveResolved, "scripted Drive never completed (begun=" + script.DriveBegun + ")");
            Assert.True(script.SpeedAccepted && script.PauseAccepted && script.ResumeAccepted);
            Assert.Equal(2, a.Speed);

            var recorded = new List<CommandRecord>();
            for (int i = 0; i < a.CommandLog.Count; i++)
                if (a.CommandLog[i].Source == CommandSource.Player) recorded.Add(a.CommandLog[i]);
            Assert.True(recorded.Count >= 6, "recorded=" + recorded.Count);

            var b = NewJp(seed, AutoMode.Manual);
            ScriptedDriver.ApplyFixture(b);
            var cursor = 0;
            var replayResults = new List<CommandResult>();
            var guard = 0;
            while (b.TickIndex < a.TickIndex && guard++ < iterations * 4)
            {
                while (cursor < recorded.Count && recorded[cursor].Tick <= b.TickIndex)
                {
                    var r = recorded[cursor++];
                    Assert.Equal(b.TickIndex, r.Tick);
                    replayResults.Add(b.Submit(new BattleCommand
                    {
                        Kind = r.Kind,
                        Slot = r.Slot,
                        Timing = r.Timing,
                        Value = r.Value,
                        Source = CommandSource.Replay
                    }));
                }
                b.Tick();
            }
            while (cursor < recorded.Count && recorded[cursor].Tick <= b.TickIndex)
            {
                var r = recorded[cursor++];
                replayResults.Add(b.Submit(new BattleCommand { Kind = r.Kind, Slot = r.Slot, Timing = r.Timing, Value = r.Value, Source = CommandSource.Replay }));
            }
            Assert.Equal(recorded.Count, cursor);
            for (int i = 0; i < recorded.Count; i++)
                Assert.True(recorded[i].Accepted == replayResults[i].Accepted,
                    "command #" + i + " " + recorded[i].Kind + "@" + recorded[i].Tick + " accepted=" + recorded[i].Accepted + " replay=" + replayResults[i].Accepted + "/" + replayResults[i].Reason);

            Assert.Equal(a.TickIndex, b.TickIndex);
            Assert.Equal(a.Outcome, b.Outcome);
            Assert.Equal(a.Drive, b.Drive);
            Assert.Equal(a.FeverGauge, b.FeverGauge);
            Assert.Equal(a.FeverActive, b.FeverActive);
            Assert.Equal(a.WaveIndex, b.WaveIndex);
            Assert.Equal(a.Speed, b.Speed);
            Assert.Equal(SnapshotHp(a), SnapshotHp(b));
            Assert.Equal(a.Events.Events.Count, b.Events.Events.Count);
            for (int i = 0; i < a.Events.Events.Count; i++)
            {
                var ea = a.Events.Events[i];
                var eb = b.Events.Events[i];
                Assert.Equal(ea.Kind, eb.Kind);
                Assert.Equal(ea.Opcode, eb.Opcode);
                Assert.Equal(ea.Amount, eb.Amount);
                Assert.Equal(ea.Tick, eb.Tick);
                Assert.Equal(ea.CasterSlot, eb.CasterSlot);
                Assert.Equal(ea.TargetSlot, eb.TargetSlot);
            }
            Assert.Equal(a.Events.ComputeHash(), b.Events.ComputeHash());
            Assert.Equal(CountCrits(a), CountCrits(b));
            Assert.Equal(a.Seed, b.Seed);
            Assert.Equal(a.RunHeader(), b.RunHeader());
            Assert.Contains(BattleSim.RulesVersion, a.RunHeader());
            Assert.Contains(seed.ToString(), a.RunHeader());
        }

        sealed class ScriptedDriver
        {
            public bool TapAccepted, SlideAccepted, DriveBegun, DriveResolved, SpeedAccepted, PauseAccepted, ResumeAccepted;
            int _driveBeginIteration = -1;

            public static void ApplyFixture(BattleSim sim)
            {
                ChargeAll(sim);
                sim.TimeLeft = 300f;
            }

            public void BeforeTick(BattleSim sim, int iteration)
            {
                if (iteration == 0) ApplyFixture(sim);
                if (iteration == 20)
                    SpeedAccepted = sim.Submit(Cmd(BattleCommandKind.SetSpeed, value: 2)).Accepted;
                if (iteration == 40)
                    TapAccepted = sim.Submit(Cmd(BattleCommandKind.Tap, 0)).Accepted;
                if (iteration == 90)
                    SlideAccepted = sim.Submit(Cmd(BattleCommandKind.Slide, 1)).Accepted;
                if (iteration == 100)
                    PauseAccepted = sim.Submit(Cmd(BattleCommandKind.Pause)).Accepted;
                if (iteration == 110)
                    ResumeAccepted = sim.Submit(Cmd(BattleCommandKind.Resume)).Accepted;
                if (!DriveBegun && iteration > 110 && sim.Drive >= 100f && sim.PendingDriveSlot < 0 && sim.Outcome == BattleOutcome.InProgress)
                {
                    DriveBegun = sim.Submit(Cmd(BattleCommandKind.DriveBegin, 0)).Accepted;
                    if (DriveBegun) _driveBeginIteration = iteration;
                }
                if (DriveBegun && !DriveResolved && _driveBeginIteration >= 0 && iteration == _driveBeginIteration + 10)
                    DriveResolved = sim.Submit(Cmd(BattleCommandKind.DriveResolve, 0, DriveTiming.Perfect)).Accepted;
            }
        }

        static BattleSim RunFull(int seed, int ticks, bool forceNoCrit)
        {
            var sim = NewJp(seed, AutoMode.Full, forceNoCrit);
            InflateEnemies(sim);
            sim.TimeLeft = 999f;
            for (int i = 0; i < ticks && sim.Outcome == BattleOutcome.InProgress; i++) sim.Tick();
            return sim;
        }

        static int CountCrits(BattleSim sim)
        {
            var n = 0;
            for (int i = 0; i < sim.Log.Count; i++)
                if (sim.Log[i].Crit) n++;
            return n;
        }

        static int CountDamageTexts(BattleSim sim)
        {
            var n = 0;
            for (int i = 0; i < sim.Log.Count; i++)
            {
                var t = sim.Log[i];
                if (t.Heal || t.Text == null || t.Text.StartsWith("FX ", StringComparison.Ordinal)) continue;
                n++;
            }
            return n;
        }
    }
}
