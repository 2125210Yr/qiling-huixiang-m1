using System;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class G2ReviewReplayComponentTests
    {
        const int ScriptTicks = 600;

        [Fact]
        public void FreshSimDigestsAreEqual()
        {
            var a = Fresh(7);
            var b = Fresh(7);
            var da = BattleStateDigest.Capture(a);
            var db = BattleStateDigest.Capture(b);
            Assert.False(string.IsNullOrEmpty(da.Hash));
            Assert.Equal(64, da.Hash.Length);
            Assert.Equal(da.Hash, db.Hash);
            Assert.Equal(da.ToText(), db.ToText());
            Assert.Empty(BattleStateDigest.Diff(da, db));
        }

        [Fact]
        public void DigestDiffersAfterOneTap()
        {
            var a = Fresh(7);
            var b = Fresh(7);
            ChargeUntilReady(a, 0);
            while (b.TickIndex < a.TickIndex)
                b.Tick();

            var beforeA = BattleStateDigest.Capture(a);
            var beforeB = BattleStateDigest.Capture(b);
            Assert.Equal(beforeA.Hash, beforeB.Hash);

            var tap = a.Submit(new BattleCommand
            {
                Kind = BattleCommandKind.Tap,
                Slot = 0,
                Source = CommandSource.Player
            });
            Assert.True(tap.Accepted, "Tap at full charge should be accepted: " + tap.Reason);

            var afterA = BattleStateDigest.Capture(a);
            Assert.NotEqual(beforeA.Hash, afterA.Hash);
            var fieldDiff = BattleStateDigest.Diff(beforeA, afterA);
            Assert.NotEmpty(fieldDiff);
            Assert.NotEqual(BattleStateDigest.Capture(b).Hash, afterA.Hash);
        }

        [Fact]
        public void ReplayScriptRoundTripsThroughJsonLines()
        {
            var script = new ReplayScript();
            script.Header.Seed = 7;
            script.Header.PartyIds = Catalog.DefaultParty;
            script.Header.LeaderSlot = 0;
            script.Header.StageId = Catalog.VerticalSliceStage.Id;
            script.Header.Profile = FormulaProfile.JP_LEGACY_EMPIRICAL;
            script.Header.Auto = AutoMode.Manual;
            script.Header.Speed = 1;
            script.Header.RulesVersion = BattleSim.RulesVersion;
            script.Header.DataVersion = "unknown";
            script.Header.RunHeader = "seed=7;profile=JP_LEGACY_EMPIRICAL";
            script.Commands.Add(new CommandRecord
            {
                Seq = 1,
                Tick = 12,
                Kind = BattleCommandKind.Tap,
                Slot = 0,
                Timing = DriveTiming.Good,
                Value = 0,
                Source = CommandSource.Player,
                Accepted = true,
                Reason = CommandReject.None
            });
            script.Commands.Add(new CommandRecord
            {
                Seq = 2,
                Tick = 40,
                Kind = BattleCommandKind.SetSpeed,
                Slot = 0,
                Value = 2,
                Source = CommandSource.Player,
                Accepted = true,
                Reason = CommandReject.None
            });
            script.Commands.Add(new CommandRecord
            {
                Seq = 3,
                Tick = 80,
                Kind = BattleCommandKind.DriveResolve,
                Slot = 0,
                Timing = DriveTiming.Perfect,
                Source = CommandSource.Replay,
                Accepted = false,
                Reason = CommandReject.NoQtePending
            });

            var text = script.ToJsonLines();
            Assert.Contains("header ", text, StringComparison.Ordinal);
            Assert.Contains("cmd ", text, StringComparison.Ordinal);
            Assert.Contains("kind=Tap", text, StringComparison.Ordinal);
            Assert.DoesNotContain("{", text, StringComparison.Ordinal);

            var parsed = ReplayScript.ParseJsonLines(text);
            Assert.Equal(script.Header.Seed, parsed.Header.Seed);
            Assert.Equal(script.Header.PartyIds, parsed.Header.PartyIds);
            Assert.Equal(script.Header.LeaderSlot, parsed.Header.LeaderSlot);
            Assert.Equal(script.Header.StageId, parsed.Header.StageId);
            Assert.Equal(script.Header.Profile, parsed.Header.Profile);
            Assert.Equal(script.Header.Auto, parsed.Header.Auto);
            Assert.Equal(script.Header.Speed, parsed.Header.Speed);
            Assert.Equal(script.Header.RulesVersion, parsed.Header.RulesVersion);
            Assert.Equal(script.Header.DataVersion, parsed.Header.DataVersion);
            Assert.Equal(script.Header.RunHeader, parsed.Header.RunHeader);
            Assert.Equal(script.Commands.Count, parsed.Commands.Count);
            for (int i = 0; i < script.Commands.Count; i++)
            {
                var x = script.Commands[i];
                var y = parsed.Commands[i];
                Assert.Equal(x.Seq, y.Seq);
                Assert.Equal(x.Tick, y.Tick);
                Assert.Equal(x.Kind, y.Kind);
                Assert.Equal(x.Slot, y.Slot);
                Assert.Equal(x.Timing, y.Timing);
                Assert.Equal(x.Value, y.Value);
                Assert.Equal(x.Source, y.Source);
                Assert.Equal(x.Accepted, y.Accepted);
                Assert.Equal(x.Reason, y.Reason);
            }
        }

        [Fact]
        public void FullReplayVerifyHasZeroDivergences()
        {
            var party = Catalog.DefaultParty;
            var stage = Catalog.VerticalSliceStage;
            var original = PlayScripted(7, ScriptTicks);
            Assert.True(original.CommandLog.Count > 0, "scripted run should record Submit calls");
            Assert.Contains(original.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.Tap);
            Assert.Contains(original.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.Slide);
            Assert.Contains(original.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.DriveBegin);
            Assert.Contains(original.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.DriveResolve);
            Assert.Contains(original.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.SetSpeed);
            Assert.Contains(original.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.Pause);
            Assert.Contains(original.CommandLog, c => c.Accepted && c.Kind == BattleCommandKind.Resume);

            var script = ReplayScript.FromSim(original, party, 0, stage);
            Assert.Equal(7, script.Header.Seed);
            Assert.Equal(BattleSim.RulesVersion, script.Header.RulesVersion);
            Assert.False(string.IsNullOrEmpty(script.Header.RunHeader));

            var replayed = BattleReplayer.Run(script, () => FreshJp(7), ScriptTicks);
            Assert.Empty(BattleReplayer.Divergences);
            var report = BattleReplayer.Verify(original, replayed);
            Assert.True(
                report.Ok,
                "digest: " + string.Join(" | ", report.DigestDiff)
                + " events: " + string.Join(" | ", report.EventDiff));
            Assert.Equal(-1, report.FirstEventDivergence);
            Assert.Equal(report.OriginalDigest.Hash, report.ReplayedDigest.Hash);
        }

        [Fact]
        public void DifferentSeedReplayReportsDivergence()
        {
            var party = Catalog.DefaultParty;
            var stage = Catalog.VerticalSliceStage;
            var original = PlayScripted(7, ScriptTicks);
            var script = ReplayScript.FromSim(original, party, 0, stage);

            var altSeed = PickDivergentSeed(original, script);
            var replayed = BattleReplayer.Run(script, () => FreshJp(altSeed), ScriptTicks);
            var report = BattleReplayer.Verify(original, replayed);
            Assert.False(report.Ok, "seed " + altSeed + " should diverge from seed 7");
            Assert.True(
                report.DigestDiff.Count > 0 || report.FirstEventDivergence >= 0,
                "expected digest or event-sequence divergence for seed " + altSeed);
        }

        static int PickDivergentSeed(BattleSim original, ReplayScript script)
        {
            foreach (var seed in new[] { 1, 8, 99, 20260913, 42 })
            {
                var replayed = BattleReplayer.Run(script, () => FreshJp(seed), ScriptTicks);
                var report = BattleReplayer.Verify(original, replayed);
                if (!report.Ok) return seed;
            }
            throw new Xunit.Sdk.XunitException(
                "crits/enemy-slide RNG did not diverge for tried seeds; add another seed pair");
        }

        static BattleSim PlayScripted(int seed, int ticks)
        {
            var sim = FreshJp(seed);
            var didTap = false;
            var didSlide = false;
            var didDrive = false;
            var didSpeed = false;
            var didPause = false;
            for (int i = 0; i < ticks; i++)
            {
                if (sim.Outcome == BattleOutcome.InProgress)
                {
                    // Contract order: Tap when charged, Slide later, Drive when the gauge is full.
                    if (sim.PendingDriveSlot < 0 && Ready(sim, 0) && !didTap)
                    {
                        Accept(sim, new BattleCommand
                        {
                            Kind = BattleCommandKind.Tap,
                            Slot = 0,
                            Source = CommandSource.Player
                        });
                        didTap = true;
                    }
                    else if (sim.PendingDriveSlot < 0 && Ready(sim, 0) && didTap && !didSlide
                        && sim.Allies[0] != null && sim.Allies[0].SlideCd <= 0f)
                    {
                        Accept(sim, new BattleCommand
                        {
                            Kind = BattleCommandKind.Slide,
                            Slot = 0,
                            Source = CommandSource.Player
                        });
                        didSlide = true;
                    }
                    else if (!didDrive && didSlide && sim.Drive >= 100f && sim.PendingDriveSlot < 0)
                    {
                        Accept(sim, new BattleCommand
                        {
                            Kind = BattleCommandKind.DriveBegin,
                            Slot = 0,
                            Source = CommandSource.Player
                        });
                        Accept(sim, new BattleCommand
                        {
                            Kind = BattleCommandKind.DriveResolve,
                            Slot = 0,
                            Timing = DriveTiming.Perfect,
                            Source = CommandSource.Player
                        });
                        didDrive = true;
                    }

                    if (!didSpeed && sim.TickIndex >= 90)
                    {
                        Accept(sim, new BattleCommand
                        {
                            Kind = BattleCommandKind.SetSpeed,
                            Value = 2,
                            Source = CommandSource.Player
                        });
                        didSpeed = true;
                    }
                    if (!didPause && sim.TickIndex >= 140)
                    {
                        Accept(sim, new BattleCommand
                        {
                            Kind = BattleCommandKind.Pause,
                            Source = CommandSource.Player
                        });
                        Accept(sim, new BattleCommand
                        {
                            Kind = BattleCommandKind.Resume,
                            Source = CommandSource.Player
                        });
                        didPause = true;
                    }
                }
                sim.Tick();
            }
            return sim;
        }

        static void ChargeUntilReady(BattleSim sim, int slot)
        {
            for (int i = 0; i < BattleSim.TickHz * 20; i++)
            {
                if (Ready(sim, slot)) return;
                sim.Tick();
            }
            throw new Xunit.Sdk.XunitException("slot " + slot + " never reached Charge>=100");
        }

        static bool Ready(BattleSim sim, int slot)
        {
            if (sim.Allies == null || slot < 0 || slot >= sim.Allies.Length) return false;
            var u = sim.Allies[slot];
            return u != null && u.Alive && u.Charge >= 100f;
        }

        static void Accept(BattleSim sim, BattleCommand cmd)
        {
            var r = sim.Submit(cmd);
            Assert.True(r.Accepted, cmd.Kind + " rejected: " + r.Reason + " tick=" + sim.TickIndex);
        }

        static BattleSim Fresh(int seed)
        {
            return new BattleSim(Catalog.DefaultParty, 0, seed, Catalog.VerticalSliceStage, null)
            {
                Auto = AutoMode.Manual,
                Speed = 1
            };
        }

        static BattleSim FreshJp(int seed)
        {
            var sim = Fresh(seed);
            sim.Profile = FormulaProfile.JP_LEGACY_EMPIRICAL;
            return sim;
        }
    }
}
