using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Resonance.Battle;
using Xunit;
using Xunit.Abstractions;

namespace Resonance.Tests
{
    // Real Capture -> JSON -> Verify tests. Policies use Submit and natural Tick only.
    // Expected-output tampering is deliberate; the last test alone changes a pre-battle input fixture.
    public sealed class OriginalReplayTests
    {
        readonly ITestOutputHelper _output;
        public OriginalReplayTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void RecordedAcceptedAndRejectedCommandsRoundTripWithDefaultCriticalRolls()
        {
            var sim = Create("C01", "C02", "C03", "C04");
            Assert.Equal(CommandReject.NotCharged, sim.Submit(BattleCommand.Tap(0)).Reason);
            Assert.Equal(CommandReject.ModeDisabled, sim.Submit(BattleCommand.DriveBegin(0)).Reason);
            Assert.Equal(CommandReject.InvalidValue, sim.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 3 }).Reason);
            RunReadySkills(sim, 600);
            var record = RoundTrip(OriginalBattleRecord.Capture(sim));
            Assert.Equal(sim.CommandLog.Count, record.Commands.Count);
            Assert.Contains(record.Commands, c => !c.Accepted && c.Reason == CommandReject.NotCharged);
            Assert.Contains(record.Commands, c => c.Accepted && c.Kind == BattleCommandKind.Tap);
            var report = OriginalBattleReplayer.Verify(record); AssertMatch(report);
            AssertCommandTrace(sim, report.Replayed);
            Assert.False(sim.ForceNoCrit); Assert.False(report.Replayed.ForceNoCrit);
            Assert.True(sim.Log.Any(x => x.Crit), "Default seeded trace produced no observed critical hit.");
            Assert.Equal(sim.Log.Select(x => x.Crit), report.Replayed.Log.Select(x => x.Crit));
            Assert.Equal(sim.Events.ComputeHash(), report.Replayed.Events.ComputeHash());
            Assert.Equal(record.ResolutionHash, OriginalBattleRecord.Capture(report.Replayed).ResolutionHash);
            _output.WriteLine("accepted={0}; rejected={1}; ticks={2}; critsInRetainedLog={3}; eventHash={4}; resolutionHash={5}",
                record.Commands.Count(x => x.Accepted), record.Commands.Count(x => !x.Accepted), record.EndTick,
                sim.Log.Count(x => x.Crit), record.EventHash, record.ResolutionHash);
        }

        [Fact]
        public void PausedQueueReplacementClearAndResumeRegenerateBoundCommandsExactlyOnce()
        {
            var sim = Create("C01"); WaitForReady(sim, 0, 1, 3);
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause }).Accepted);
            Focus(sim, 1); Queue(sim, 0); Queue(sim, 3); Queue(sim, 1);
            Focus(sim, 2); Queue(sim, 0);
            Assert.Equal(CommandReject.None, sim.ClearExpeditionQueuedCommand(3));
            Focus(sim, 1); Queue(sim, 3); Focus(sim, 0);
            Assert.Equal(new[] { 0, 1, 3 }, sim.ExpeditionQueue.Select(q => q.ActorSlot));
            int beforeResume = sim.CommandLog.Count, tick = sim.TickIndex;
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Resume }).Accepted);
            Assert.Equal(tick, sim.TickIndex); Assert.Empty(sim.ExpeditionQueue);
            var children = sim.CommandLog.Skip(beforeResume).ToArray();
            Assert.Equal(new[] { BattleCommandKind.Resume, BattleCommandKind.Tap, BattleCommandKind.Tap, BattleCommandKind.Tap }, children.Select(c => c.Kind));
            Assert.Equal(2, children[1].RequiredEnemySlot); Assert.Equal(1, children[1].RequiredEnemyGeneration);
            Assert.Equal(1, children[3].RequiredEnemySlot); Assert.Equal(1, children[3].RequiredEnemyGeneration);
            var report = OriginalBattleReplayer.Verify(RoundTrip(OriginalBattleRecord.Capture(sim))); AssertMatch(report);
            AssertCommandTrace(sim, report.Replayed);
            Assert.Equal(sim.ExpeditionQueueResults.Select(QueueResultKey), report.Replayed.ExpeditionQueueResults.Select(QueueResultKey));
            Assert.Empty(report.Replayed.ExpeditionQueue);
            Assert.Equal(sim.ExpeditionResolutions.Count, report.Replayed.ExpeditionResolutions.Count);
        }

        [Fact]
        public void CaptureWhilePausedRetainsUnsubmittedQueueWithoutExecutingItOrAdvancingTime()
        {
            var sim = Create("C01"); WaitForReady(sim, 0, 3);
            Assert.True(sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause }).Accepted);
            Focus(sim, 1); Queue(sim, 0); Queue(sim, 3);
            Assert.Equal(CommandReject.None, sim.ClearExpeditionQueuedCommand(0));
            Focus(sim, 2); Queue(sim, 0);
            var record = RoundTrip(OriginalBattleRecord.Capture(sim));
            var report = OriginalBattleReplayer.Verify(record); AssertMatch(report);
            Assert.True(report.Replayed.Paused); Assert.Equal(sim.TickIndex, report.Replayed.TickIndex);
            Assert.Equal(sim.OriginalEncounter.ElapsedSec, report.Replayed.OriginalEncounter.ElapsedSec);
            Assert.Equal(sim.ExpeditionQueue.Select(QueueKey), report.Replayed.ExpeditionQueue.Select(QueueKey));
            Assert.DoesNotContain(report.Replayed.CommandLog, c => c.Kind == BattleCommandKind.Tap);
            AssertCommandTrace(sim, report.Replayed);
        }

        [Fact]
        public void InvalidBoundGenerationRejectionIsPreservedAsActualSubmittedInput()
        {
            var sim = Create("C01"); WaitForReady(sim, 0);
            var invalid = BattleCommand.Tap(0); invalid.RequiredEnemySlot = 1;
            invalid.RequiredEnemyGeneration = sim.Enemies[1].InstanceGeneration + 1;
            var result = sim.Submit(invalid);
            Assert.False(result.Accepted); Assert.Equal(CommandReject.TargetInvalid, result.Reason);
            var record = RoundTrip(OriginalBattleRecord.Capture(sim));
            var report = OriginalBattleReplayer.Verify(record); AssertMatch(report); AssertCommandTrace(sim, report.Replayed);
            var rejected = report.Replayed.CommandLog.Last();
            Assert.Equal(invalid.RequiredEnemyGeneration, rejected.RequiredEnemyGeneration);
            Assert.Equal(CommandReject.TargetInvalid, rejected.Reason);
        }

        [Fact]
        public void FullBossVictoryReplaysPhaseMaskGenerationsAndFinalTickRejectedCommands()
        {
            var sim = Create("C01"); RunMaskFirst(sim);
            Assert.Equal(BattleOutcome.Victory, sim.Outcome);
            Assert.Equal(2, sim.OriginalEncounter.Phase); Assert.True(sim.OriginalEncounter.MaskRebuildCount > 0);
            Assert.Contains(sim.Enemies, e => e.Slot > 0 && e.InstanceGeneration > 1);
            Assert.Equal(CommandReject.NotInProgress, sim.Submit(BattleCommand.Tap(0)).Reason);
            Assert.Equal(CommandReject.NotInProgress, sim.Submit(new BattleCommand { Kind = BattleCommandKind.Pause }).Reason);
            var record = RoundTrip(OriginalBattleRecord.Capture(sim));
            Assert.Equal(sim.TickIndex, record.EndTick);
            var report = OriginalBattleReplayer.Verify(record); AssertMatch(report); AssertCommandTrace(sim, report.Replayed);
            Assert.Equal(BattleOutcome.Victory, report.Replayed.Outcome);
            Assert.Equal(sim.OriginalEncounter.AreaCasts, report.Replayed.OriginalEncounter.AreaCasts);
            Assert.Equal(sim.OriginalEncounter.MaskRebuildCount, report.Replayed.OriginalEncounter.MaskRebuildCount);
            Assert.Equal(sim.Enemies.Select(e => e.InstanceGeneration), report.Replayed.Enemies.Select(e => e.InstanceGeneration));
            Assert.False(report.Replayed.ForceNoCrit);
            _output.WriteLine("fullBossOutcome={0}; ticks={1}; elapsed={2:0.0000}; phase={3}; rebuiltMasks={4}; eventHash={5}; resolutionHash={6}",
                sim.Outcome, record.EndTick, sim.OriginalEncounter.ElapsedSec, sim.OriginalEncounter.Phase,
                sim.OriginalEncounter.MaskRebuildCount, record.EventHash, record.ResolutionHash);
            var fixturePath = Environment.GetEnvironmentVariable("ORIGINAL_REPLAY_FIXTURE_PATH");
            if (!string.IsNullOrWhiteSpace(fixturePath))
            {
                Assert.True(Path.IsPathRooted(fixturePath), "Fixture output path must be absolute.");
                fixturePath = Path.GetFullPath(fixturePath); Directory.CreateDirectory(Path.GetDirectoryName(fixturePath));
                var json = record.ToJson(); File.WriteAllText(fixturePath, json, new UTF8Encoding(false));
                _output.WriteLine("fixtureTapePath={0}; sha256={1}; bytes={2}; scope=real-simulation-fixture-not-ordinary-UI-video",
                    fixturePath, BattleEventLog.HashUtf8(json), Encoding.UTF8.GetByteCount(json));
            }
        }

        [Theory]
        [InlineData("phase")]
        [InlineData("intent")]
        [InlineData("generation")]
        [InlineData("barrier")]
        [InlineData("threshold")]
        [InlineData("harmony")]
        [InlineData("forte")]
        [InlineData("completed-root")]
        [InlineData("trigger")]
        [InlineData("last-relic")]
        [InlineData("native-root")]
        public void NewStateDifferenceFailsEvenWhenOldDigestAndHpAreIdentical(string difference)
        {
            var sim = Create("C01", "C02", "C03", "C04"); RunReadySkills(sim, 300);
            var record = RoundTrip(OriginalBattleRecord.Capture(sim));
            switch (difference)
            {
                case "phase": record.FinalState.Encounter.Phase = record.FinalState.Encounter.Phase == 1 ? 2 : 1; break;
                case "intent": record.FinalState.Encounter.IntentSerial++; break;
                case "generation": record.FinalState.UnitGenerations[6]++; break;
                case "barrier": record.FinalState.BarrierEnergy++; break;
                case "threshold": record.FinalState.BarrierThreshold++; break;
                case "harmony": record.FinalState.HarmonyActorMask ^= 1; break;
                case "forte": record.FinalState.ForteStored = !record.FinalState.ForteStored; break;
                case "completed-root": record.FinalState.LastCompletedRootActionId++; break;
                case "trigger": record.FinalState.TriggerSerial++; break;
                case "last-relic": record.FinalState.LastTriggeredRelicId = "fixture-tampered-relic"; break;
                case "native-root": record.FinalState.RootActionId++; break;
            }
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotEmpty(report.Differences); Assert.NotNull(report.Replayed);
            Assert.Equal(record.FinalState.CoreState, OriginalBattleSnapshot.Capture(report.Replayed).CoreState);
            Assert.Equal(sim.Allies.Select(u => u.Hp), report.Replayed.Allies.Select(u => u.Hp));
            Assert.Equal(sim.Enemies.Select(u => u.Hp), report.Replayed.Enemies.Select(u => u.Hp));
        }

        [Fact]
        public void ExpectedFinalOutputsCannotChooseOrShortenTheReplayBoundary()
        {
            var sim = Create("C01"); RunReadySkills(sim, 300);
            var record = RoundTrip(OriginalBattleRecord.Capture(sim)); int actualEnd = record.EndTick;
            record.FinalState.CoreState = "TickIndex=0"; record.FinalState.Encounter.ElapsedSec = 0;
            record.EventHash = "wrong-comparison-only-event-hash"; record.ResolutionHash = "wrong-comparison-only-resolution-hash";
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotNull(report.Replayed); Assert.NotEmpty(report.Differences);
            Assert.Equal(actualEnd, report.Replayed.TickIndex);
            Assert.Equal(sim.Events.ComputeHash(), report.Replayed.Events.ComputeHash());
            Assert.Equal(sim.OriginalEncounter.ElapsedSec, report.Replayed.OriginalEncounter.ElapsedSec);
        }

        [Fact]
        public void NegativeIndependentBoundaryIsRejectedBeforeSimulation()
        {
            var record = OriginalBattleRecord.Capture(Create("C01")); record.EndTick = -1;
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotEmpty(report.Differences); Assert.Null(report.Replayed);
        }

        [Theory]
        [InlineData("record-schema")]
        [InlineData("input-schema")]
        [InlineData("mode")]
        [InlineData("content-version")]
        [InlineData("content-hash")]
        [InlineData("ruleset")]
        public void UnknownInputVersionIsExplicitlyRejectedEvenWithRecomputedInputHash(string change)
        {
            var record = OriginalBattleRecord.Capture(Create("C01"));
            switch (change)
            {
                case "record-schema": record.SchemaVersion++; break;
                case "input-schema": record.Input.SchemaVersion++; break;
                case "mode": record.Input.ModeId = "legacy"; break;
                case "content-version": record.Input.ContentVersion += "-unknown"; break;
                case "content-hash": record.Input.ContentHash = "unknown-content-hash"; break;
                case "ruleset": record.Input.RulesetId += "-unknown"; break;
            }
            record.InputHash = ExpeditionContent.Fingerprint(record.Input);
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotEmpty(report.Differences); Assert.Null(report.Replayed);
        }

        [Fact]
        public void TamperedFrozenSkillWithUnchangedInputHashIsRejectedBeforeSimulation()
        {
            var record = OriginalBattleRecord.Capture(Create("C01")); record.Input.Skills[0].FlatPower++;
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotEmpty(report.Differences); Assert.Null(report.Replayed);
        }

        [Fact]
        public void ChangingOnlyExpectedCommandAcceptanceIsDetectedWithoutChangingExecution()
        {
            var sim = Create("C01"); Assert.False(sim.Submit(BattleCommand.Tap(0)).Accepted);
            for (int i = 0; i < 90; i++) sim.Tick();
            var record = RoundTrip(OriginalBattleRecord.Capture(sim));
            record.Commands[0].Accepted = true; record.Commands[0].Reason = CommandReject.None;
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.NotEmpty(report.Differences); Assert.NotNull(report.Replayed);
            Assert.False(report.Replayed.CommandLog[0].Accepted);
            Assert.Equal(CommandReject.NotCharged, report.Replayed.CommandLog[0].Reason);
        }

        [Fact]
        public void CapturedTapeOwnsCopiesOfInputCommandsAndFinalState()
        {
            var sim = Create("C01"); sim.Submit(BattleCommand.Tap(0));
            var record = OriginalBattleRecord.Capture(sim); string saved = record.ToJson();
            sim.Submit(new BattleCommand { Kind = BattleCommandKind.SetSpeed, Value = 2 }); sim.Tick();
            sim.CommandLog[0].Reason = CommandReject.InvalidValue; // Tape copy boundary only; not a replay fixture.
            var opening = sim.OpeningExpeditionInput; opening.Boss.CastDurationSec = 99f;
            Assert.Equal(saved, record.ToJson());
            Assert.Equal(0, record.EndTick); Assert.Single(record.Commands);
            Assert.Equal(record.InputHash, ExpeditionContent.Fingerprint(record.Input));
        }

        [Fact]
        public void ResolutionHashUsesActualPropertiesRatherThanOnlyTheNumberOfResults()
        {
            var firstInput = Input("C01"); var secondInput = firstInput.DeepClone();
            secondInput.Characters.Single(c => c.Id == secondInput.PartyIds[0]).Atk += 100; // Declared frozen-input numerical fixture.
            var first = RunBattleFactory.Create(firstInput); var second = RunBattleFactory.Create(secondInput);
            for (int i = 0; i < 90; i++) { first.Tick(); second.Tick(); }
            Assert.NotEmpty(first.ExpeditionResolutions);
            Assert.Equal(first.ExpeditionResolutions.Count, second.ExpeditionResolutions.Count);
            Assert.NotEqual(first.ExpeditionResolutions.Sum(r => r.EffectiveHpDamage), second.ExpeditionResolutions.Sum(r => r.EffectiveHpDamage));
            var a = OriginalBattleRecord.Capture(first); var b = OriginalBattleRecord.Capture(second);
            Assert.NotEqual(a.ResolutionHash, b.ResolutionHash);
            AssertMatch(OriginalBattleReplayer.Verify(RoundTrip(a)));
            AssertMatch(OriginalBattleReplayer.Verify(RoundTrip(b)));
        }

        static ExpeditionBattleInput Input(params string[] relics)
        {
            var input = RunBattleFactory.CreateInput("N7", "single", 260921, relics, null);
            input.RunId = "original-replay-fixture"; input.EncounterId = "original-replay-fixture/N7/5";
            input.BattleOrdinal = 5; input.AttemptId = "original-replay-attempt-1";
            return input;
        }
        static BattleSim Create(params string[] relics) => RunBattleFactory.Create(Input(relics));
        static OriginalBattleRecord RoundTrip(OriginalBattleRecord record) => OriginalBattleRecord.FromJson(record.ToJson());
        static void AssertMatch(OriginalReplayReport report)
        {
            Assert.True(report.Match, string.Join(" | ", report.Differences)); Assert.NotNull(report.Replayed);
        }
        static void AssertCommandTrace(BattleSim expected, BattleSim actual) =>
            Assert.Equal(expected.CommandLog.Select(CommandKey), actual.CommandLog.Select(CommandKey));
        static string CommandKey(CommandRecord c) => string.Join("|", c.Seq, c.Tick, c.TerminalFeverTick, c.Kind, c.Slot,
            c.Timing, c.Value, c.RequiredEnemySlot, c.RequiredEnemyGeneration, c.Accepted, c.Reason);
        static string QueueKey(ExpeditionQueuedCommand c) => c.ActorSlot + "|" + c.RequiredEnemySlot + "|" + c.RequiredEnemyGeneration;
        static string QueueResultKey(ExpeditionQueueExecution c) => c.ActorSlot + "|" + c.RequiredEnemySlot + "|" + c.RequiredEnemyGeneration
            + "|" + c.Result.Seq + "|" + c.Result.Tick + "|" + c.Result.Accepted + "|" + c.Result.Reason;
        static void Queue(BattleSim sim, int slot) => Assert.Equal(CommandReject.None, sim.QueueExpeditionTap(slot));
        static void Focus(BattleSim sim, int slot) => Assert.True(sim.Submit(BattleCommand.FocusEnemy(slot)).Accepted);
        static void WaitForReady(BattleSim sim, params int[] slots)
        {
            for (int tick = 0; tick < 600 && !slots.All(sim.CanAct) && sim.Outcome == BattleOutcome.InProgress; tick++) sim.Tick();
            Assert.True(slots.All(sim.CanAct));
        }
        static void RunReadySkills(BattleSim sim, int ticks)
        {
            for (int tick = 0; tick < ticks && sim.Outcome == BattleOutcome.InProgress; tick++)
            {
                for (int slot = 0; slot < 5 && sim.Outcome == BattleOutcome.InProgress; slot++)
                    if (sim.CanAct(slot)) Assert.True(sim.Submit(BattleCommand.Tap(slot)).Accepted);
                if (sim.Outcome == BattleOutcome.InProgress) sim.Tick();
            }
        }
        static void RunMaskFirst(BattleSim sim)
        {
            var priority = new[] { 1, 2, 4, 0, 3 };
            for (int tick = 0; tick < 400 * BattleSim.TickHz && sim.Outcome == BattleOutcome.InProgress; tick++)
            {
                var target = sim.Enemies.Where(u => u.Alive && u.Slot != 0).OrderBy(u => u.Hp).ThenBy(u => u.Slot).FirstOrDefault() ?? sim.Enemies[0];
                if (target.Alive && sim.FocusEnemySlot != target.Slot) Focus(sim, target.Slot);
                var intent = sim.OriginalIntentSnapshot;
                foreach (int slot in priority)
                {
                    if (!sim.CanAct(slot)) continue;
                    if (slot == 1 && (intent == null || !intent.IsCasting || intent.RemainingCastSec > 0.2f)) continue;
                    if (slot == 2 && !sim.Allies.Any(u => u.Alive && u.Hp <= u.MaxHp * 0.8f)) continue;
                    Assert.True(sim.Submit(BattleCommand.Tap(slot)).Accepted);
                    if (sim.Outcome != BattleOutcome.InProgress) break;
                }
                if (sim.Outcome == BattleOutcome.InProgress) sim.Tick();
            }
        }
    }
}
