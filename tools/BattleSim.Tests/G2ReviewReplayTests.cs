using System;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// REGRESSION_MATRIX R02 — seed + command tape reproduces state and events.
    /// Uses <see cref="BattleRunRecord"/> / <see cref="BattleReplayer"/> (API_CONTRACT §1 / §4).
    /// </summary>
    public sealed class G2ReviewReplayTests
    {
        const int Seed = 7;
        const int TickCap = BattleSim.TickHz * 90;

        [Fact]
        public void R02_SeedAndCommands_ReplayStateAndEvents()
        {
            var party = Catalog.DefaultParty;
            var stageId = Catalog.VerticalSliceStage != null ? Catalog.VerticalSliceStage.Id : "VS-1";
            var original = PlayScripted(Seed);
            AssertScriptedActions(original);

            var rec = BattleRunRecord.Capture(original, party, stageId);
            Assert.Equal(Seed, rec.Seed);
            Assert.Equal(BattleSim.RulesVersion, rec.RulesVersion);
            Assert.Equal(FormulaProfile.JP_LEGACY_EMPIRICAL, rec.Profile);
            Assert.Equal(AutoMode.Manual, rec.Auto);
            Assert.False(string.IsNullOrEmpty(rec.RunHeader));
            Assert.True(rec.Commands.Count > 0);
            Assert.NotNull(rec.FinalDigest);

            var report = BattleReplayer.Verify(rec, FreshJp);
            Assert.True(
                report.Match,
                "Diff: " + string.Join(" | ", report.Diff));
            Assert.Empty(report.Diff);
            Assert.Equal(original.Events.Events.Count, report.ExpectedEventCount);
            Assert.Equal(original.Events.Events.Count, report.ActualEventCount);
            Assert.Equal(rec.EventSummaries.Count, report.ActualEventCount);

            var replayed = report.Replayed ?? BattleReplayer.Replay(rec, FreshJp);
            Assert.False(replayed.ForceNoCrit);
            Assert.False(replayed.Deterministic);
            Assert.Equal(original.Events.Events.Count, replayed.Events.Events.Count);
        }

        [Fact]
        public void R02_ChangedCommandSlot_DigestDiffers()
        {
            var rec = BattleRunRecord.Capture(PlayScripted(Seed), Catalog.DefaultParty, "VS-1");
            var mutated = BattleRunRecord.ParseJsonLines(rec.ToJsonLines());
            var flipped = false;
            for (int i = 0; i < mutated.Commands.Count; i++)
            {
                var c = mutated.Commands[i];
                if (c == null || !c.Accepted) continue;
                if (c.Kind != BattleCommandKind.Tap && c.Kind != BattleCommandKind.Slide
                    && c.Kind != BattleCommandKind.DriveBegin)
                    continue;
                c.Slot = c.Slot == 0 ? 1 : 0;
                flipped = true;
                break;
            }
            Assert.True(flipped, "scripted tape should contain an accepted Tap/Slide/DriveBegin");

            var report = BattleReplayer.Verify(mutated, FreshJp);
            Assert.False(report.Match);
            Assert.NotEmpty(report.Diff);
        }

        [Fact]
        public void R02_JsonLines_RoundTripsHeaderAndCommands()
        {
            var original = PlayScripted(Seed);
            var rec = BattleRunRecord.Capture(original, Catalog.DefaultParty, "VS-1");
            var text = rec.ToJsonLines();
            Assert.Contains("\"rec\":\"header\"", text, StringComparison.Ordinal);
            Assert.Contains("\"rec\":\"cmd\"", text, StringComparison.Ordinal);

            var parsed = BattleRunRecord.ParseJsonLines(text);
            Assert.Equal(rec.Seed, parsed.Seed);
            Assert.Equal(rec.RulesVersion, parsed.RulesVersion);
            Assert.Equal(rec.Profile, parsed.Profile);
            Assert.Equal(rec.Auto, parsed.Auto);
            Assert.Equal(rec.Speed, parsed.Speed);
            Assert.Equal(rec.StageId, parsed.StageId);
            Assert.Equal(rec.RunHeader, parsed.RunHeader);
            Assert.Equal(rec.PartyIds, parsed.PartyIds);
            Assert.Equal(rec.Commands.Count, parsed.Commands.Count);
            for (int i = 0; i < rec.Commands.Count; i++)
            {
                var a = rec.Commands[i];
                var b = parsed.Commands[i];
                Assert.Equal(a.Seq, b.Seq);
                Assert.Equal(a.Tick, b.Tick);
                Assert.Equal(a.Kind, b.Kind);
                Assert.Equal(a.Slot, b.Slot);
                Assert.Equal(a.Timing, b.Timing);
                Assert.Equal(a.Value, b.Value);
                Assert.Equal(a.Source, b.Source);
                Assert.Equal(a.Accepted, b.Accepted);
                Assert.Equal(a.Reason, b.Reason);
            }

            var report = BattleReplayer.Verify(parsed, FreshJp);
            Assert.True(report.Match, "parsed tape Diff: " + string.Join(" | ", report.Diff));
        }

        [Fact]
        public void R02_ReplayedSim_ForceNoCritRemainsFalse()
        {
            var rec = BattleRunRecord.Capture(PlayScripted(Seed), Catalog.DefaultParty, "VS-1");
            var replayed = BattleReplayer.Replay(rec, FreshJp);
            Assert.False(replayed.ForceNoCrit);
            Assert.False(replayed.Deterministic);
        }

        static BattleSim FreshJp(int seed)
        {
            return new BattleSim(Catalog.DefaultParty, 0, seed, Catalog.VerticalSliceStage, null)
            {
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                Auto = AutoMode.Manual,
                Speed = 1
            };
        }

        static BattleSim PlayScripted(int seed)
        {
            var sim = FreshJp(seed);
            var resolveAt = -1;
            var didTap = false;
            var didSlide = false;
            var didDrive = false;
            var didSpeed = false;
            var didPause = false;

            for (int step = 0; step < TickCap && sim.Outcome == BattleOutcome.InProgress; step++)
            {
                if (resolveAt >= 0 && sim.TickIndex >= resolveAt && sim.PendingDriveSlot >= 0)
                {
                    Submit(sim, BattleCommandKind.DriveResolve, 0, DriveTiming.Perfect);
                    resolveAt = -1;
                    didDrive = true;
                }

                if (!sim.Paused && sim.PendingDriveSlot < 0 && sim.Outcome == BattleOutcome.InProgress)
                {
                    var lead = sim.Allies[0];
                    if (lead != null && lead.Alive && lead.Charge >= 100f)
                    {
                        if (!didTap)
                        {
                            var tap = Submit(sim, BattleCommandKind.Tap, 0);
                            if (tap.Accepted) didTap = true;
                        }
                        else if (lead.SlideCd <= 0f)
                        {
                            var slide = Submit(sim, BattleCommandKind.Slide, 0);
                            if (slide.Accepted) didSlide = true;
                        }
                    }
                    for (int slot = 1; slot < sim.Allies.Length; slot++)
                    {
                        var u = sim.Allies[slot];
                        if (u == null || !u.Alive || u.Charge < 100f || u.SlideCd > 0f) continue;
                        var slide = Submit(sim, BattleCommandKind.Slide, slot);
                        if (slide.Accepted) didSlide = true;
                    }
                }

                if (didTap && didSlide && !didDrive && resolveAt < 0
                    && sim.Drive >= 100f
                    && sim.PendingDriveSlot < 0
                    && !sim.Paused)
                {
                    for (int slot = 0; slot < sim.Allies.Length; slot++)
                    {
                        var begin = Submit(sim, BattleCommandKind.DriveBegin, slot);
                        if (begin.Accepted)
                        {
                            resolveAt = sim.TickIndex + 5;
                            break;
                        }
                    }
                }

                if (didDrive && !didSpeed && sim.PendingDriveSlot < 0 && !sim.Paused)
                {
                    Submit(sim, BattleCommandKind.SetSpeed, 0, DriveTiming.Good, 2);
                    didSpeed = true;
                }

                if (didSpeed && !didPause && sim.PendingDriveSlot < 0)
                {
                    Submit(sim, BattleCommandKind.Pause);
                    for (int hold = 0; hold < 10; hold++)
                        sim.Tick();
                    Submit(sim, BattleCommandKind.Resume);
                    didPause = true;
                }

                sim.Tick();
            }

            return sim;
        }

        static CommandResult Submit(
            BattleSim sim,
            BattleCommandKind kind,
            int slot = -1,
            DriveTiming timing = DriveTiming.Good,
            int value = 0)
        {
            return sim.Submit(new BattleCommand
            {
                Kind = kind,
                Slot = slot,
                Timing = timing,
                Value = value,
                Source = CommandSource.Player
            });
        }

        static void AssertScriptedActions(BattleSim sim)
        {
            Assert.Contains(sim.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.Tap && c.Slot == 0);
            Assert.Contains(sim.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.Slide);
            Assert.Contains(sim.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.DriveBegin);
            Assert.Contains(sim.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.DriveResolve && c.Timing == DriveTiming.Perfect);
            Assert.Contains(sim.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.Pause);
            Assert.Contains(sim.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.Resume);
            Assert.Contains(sim.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.SetSpeed && c.Value == 2);
        }
    }
}
