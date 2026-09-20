using System;
using System.IO;
using System.Linq;
using System.Text;
using Resonance.Battle;
using Xunit;
using Xunit.Abstractions;

namespace Resonance.Tests
{
    // Controlled full-build battle fixtures. Frozen factory inputs plus legal Submit/Tick only;
    // these recordings do not claim normal reward acquisition or ordinary UI/video coverage.
    public sealed class OriginalReplayFamilyTests
    {
        readonly ITestOutputHelper _output;
        public OriginalReplayFamilyTests(ITestOutputHelper output) { _output = output; }

        [Theory]
        [InlineData("A")]
        [InlineData("B")]
        [InlineData("C")]
        public void CompleteFamilyActuallyTriggersAndItsBossBattleRoundTrips(string family)
        {
            var input = Input(family); string openingHash = ExpeditionContent.Fingerprint(input);
            var sim = RunBattleFactory.Create(input); int harmonyEvents = 0, forteConsumptions = 0;
            PlayLegalStrategy(sim, family, ref harmonyEvents, ref forteConsumptions);
            Assert.Equal(BattleOutcome.Victory, sim.Outcome);
            Assert.False(sim.ForceNoCrit); Assert.All(sim.CommandLog, c => Assert.True(c.Accepted));
            Assert.True(sim.Log.Any(x => x.Crit), "Default seeded family trace retained no observed critical hit.");
            AssertFamilyTriggered(family, sim);
            if (family == "C") { Assert.True(harmonyEvents > 0); Assert.True(forteConsumptions > 0); }
            Assert.Equal(openingHash, ExpeditionContent.Fingerprint(input));
            Assert.Equal(openingHash, ExpeditionContent.Fingerprint(sim.OpeningExpeditionInput));

            var captured = OriginalBattleRecord.Capture(sim);
            var parsed = OriginalBattleRecord.FromJson(captured.ToJson());
            Assert.Equal(openingHash, parsed.InputHash); Assert.Equal(input.RelicIds, parsed.Input.RelicIds);
            Assert.Equal(input.OpeningHp, parsed.Input.OpeningHp);
            var report = OriginalBattleReplayer.Verify(parsed);
            Assert.True(report.Match, string.Join(" | ", report.Differences)); Assert.NotNull(report.Replayed);
            Assert.Equal(BattleOutcome.Victory, report.Replayed.Outcome); Assert.False(report.Replayed.ForceNoCrit);
            AssertFamilyTriggered(family, report.Replayed);
            Assert.Equal(sim.Log.Select(x => x.Crit), report.Replayed.Log.Select(x => x.Crit));
            Assert.Equal(sim.Events.ComputeHash(), report.Replayed.Events.ComputeHash());
            Assert.Equal(parsed.ResolutionHash, OriginalBattleRecord.Capture(report.Replayed).ResolutionHash);
            Assert.Equal(parsed.FinalState.RelicTriggerSerials, OriginalBattleSnapshot.Capture(report.Replayed).RelicTriggerSerials);

            _output.WriteLine("scope=controlled-battle-fixture; family={0}; outcome={1}; elapsed={2:0.0000}; endTick={3}; seed={4}; relics={5}; A04EffectiveHpDamage={6}; B01EffectiveHpDamage={7}; harmonyEvents={8}; forteConsumptions={9}; critsInRetainedLog={10}; inputHash={11}; eventHash={12}; resolutionHash={13}",
                family, sim.Outcome, sim.OriginalEncounter.ElapsedSec, sim.TickIndex, input.Seed, string.Join(",", input.RelicIds),
                sim.ExpeditionResolutions.Where(r => r.SourceRelicId == "A04").Sum(r => (long)r.EffectiveHpDamage),
                sim.ExpeditionResolutions.Where(r => r.SourceRelicId == "B01").Sum(r => (long)r.EffectiveHpDamage),
                harmonyEvents, forteConsumptions, sim.Log.Count(x => x.Crit), parsed.InputHash, parsed.EventHash, parsed.ResolutionHash);
            ExportFixtureIfRequested(family, parsed);
        }

        [Theory]
        [InlineData("opening-hp")]
        [InlineData("relic-parameters")]
        public void ChangedOpeningHpOrRelicParametersWithOldHashAreRejectedBeforeConstruction(string change)
        {
            var sim = RunBattleFactory.Create(Input("C"));
            for (int i = 0; i < 90; i++) sim.Tick();
            var captured = OriginalBattleRecord.Capture(sim);
            var record = OriginalBattleRecord.FromJson(captured.ToJson()); string originalHash = record.InputHash;
            if (change == "opening-hp") record.Input.OpeningHp[0]--;
            else record.Input.RelicParameters.ForteDamageBonus += 0.1f;
            // Both altered values remain in the legal numerical domain; the frozen identity must reject them.
            RunBattleFactory.Validate(record.Input);
            Assert.Equal(originalHash, record.InputHash);
            Assert.NotEqual(originalHash, ExpeditionContent.Fingerprint(record.Input));
            var report = OriginalBattleReplayer.Verify(record);
            Assert.False(report.Match); Assert.Null(report.Replayed);
            Assert.Contains(report.Differences, d => d.IndexOf("Frozen input hash mismatch", StringComparison.OrdinalIgnoreCase) >= 0);
            _output.WriteLine("tamper={0}; rejectedBeforeConstruction=true; diagnostic={1}", change, string.Join(" | ", report.Differences));
        }

        static ExpeditionBattleInput Input(string family)
        {
            // B also keeps the already validated auxiliary C01 (five relics, within the workshop route budget).
            var relics = family == "A" ? new[] { "A01", "A02", "A03", "A04" }
                : family == "B" ? new[] { "B01", "B02", "B03", "B04", "C01" }
                : new[] { "C01", "C02", "C03", "C04" };
            var input = RunBattleFactory.CreateInput("N7", "single", 260921, relics, null);
            input.RunId = "family-replay-fixture-" + family;
            input.EncounterId = input.RunId + "/N7/5"; input.BattleOrdinal = 5;
            input.AttemptId = "family-" + family + "-attempt-1";
            return input;
        }

        static void AssertFamilyTriggered(string family, BattleSim sim)
        {
            if (family == "A")
            {
                Assert.True(sim.ExpeditionTotals.AllyShieldAbsorbed > 0);
                Assert.True(sim.ExpeditionRelics.GetTriggerSerial("A01") > 0);
                Assert.Contains(sim.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Derived && r.SourceRelicId == "A04"
                    && !r.TargetAlly && r.EffectiveHpDamage > 0);
            }
            else if (family == "B")
            {
                Assert.Contains(sim.ExpeditionResolutions, r => r.Origin == ResolutionOrigin.Derived && r.SourceRelicId == "B01"
                    && !r.TargetAlly && r.EffectiveHpDamage > 0);
                Assert.True(sim.ExpeditionRelics.GetTriggerSerial("B02") > 0);
                Assert.True(sim.ExpeditionRelics.GetTriggerSerial("B03") > 0);
            }
            else
            {
                Assert.True(sim.ExpeditionRelics.GetTriggerSerial("C03") > 0);
                Assert.True(sim.ExpeditionRelics.GetTriggerSerial("C04") > 0);
            }
        }

        static void PlayLegalStrategy(BattleSim sim, string family, ref int harmonyEvents, ref int forteConsumptions)
        {
            var priority = new[] { 1, 2, 4, 0, 3 };
            for (int tick = 0; tick < 300 * BattleSim.TickHz && sim.Outcome == BattleOutcome.InProgress; tick++)
            {
                var target = family == "A" ? sim.Enemies[0]
                    : sim.Enemies.Where(u => u.Alive && u.Slot != 0).OrderBy(u => u.Hp).ThenBy(u => u.Slot).FirstOrDefault() ?? sim.Enemies[0];
                if (target.Alive && sim.FocusEnemySlot != target.Slot)
                    Assert.True(sim.Submit(BattleCommand.FocusEnemy(target.Slot, CommandSource.Player)).Accepted);
                var intent = sim.OriginalIntentSnapshot;
                foreach (int slot in priority)
                {
                    if (!sim.CanAct(slot)) continue;
                    if (slot == 1 && (intent == null || !intent.IsCasting || intent.RemainingCastSec > 0.2f)) continue;
                    if (slot == 2 && !sim.Allies.Any(u => u.Alive && u.Hp <= u.MaxHp * 0.8f)) continue;
                    long beforeHarmony = sim.ExpeditionRelics.GetTriggerSerial("C03");
                    long beforeForte = sim.ExpeditionRelics.GetTriggerSerial("C04");
                    int beforeResults = sim.ExpeditionResolutions.Count;
                    Assert.True(sim.Submit(BattleCommand.Tap(slot, CommandSource.Player)).Accepted, sim.FailedReason);
                    if (sim.ExpeditionRelics.GetTriggerSerial("C03") > beforeHarmony) harmonyEvents++;
                    if (sim.ExpeditionRelics.GetTriggerSerial("C04") > beforeForte)
                    {
                        forteConsumptions++;
                        // C04 is consumed in the native damage channel, not a separately fabricated relic hit.
                        Assert.Contains(sim.ExpeditionResolutions.Skip(beforeResults), r => r.Origin == ResolutionOrigin.Native
                            && r.SourceAlly && r.SourceSlot == slot && !r.TargetAlly && r.EffectiveDamage > 0);
                    }
                    if (sim.Outcome != BattleOutcome.InProgress) break;
                }
                if (sim.Outcome == BattleOutcome.InProgress) sim.Tick();
            }
        }

        void ExportFixtureIfRequested(string family, OriginalBattleRecord record)
        {
            var directory = Environment.GetEnvironmentVariable("ORIGINAL_REPLAY_FAMILY_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) return;
            Assert.True(Path.IsPathRooted(directory), "Fixture tape directory must be absolute.");
            directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "fixture-" + family.ToLowerInvariant() + "-boss.original-replay.json");
            string json = record.ToJson(); File.WriteAllText(path, json, new UTF8Encoding(false));
            _output.WriteLine("scope=controlled-battle-fixture-not-ordinary-UI-video; tape={0}; bytes={1}; sha256={2}",
                path, Encoding.UTF8.GetByteCount(json), BattleEventLog.HashUtf8(json));
        }
    }
}
