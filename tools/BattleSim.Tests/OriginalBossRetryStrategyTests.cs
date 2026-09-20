using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using Resonance.Battle;
using Xunit;
using Xunit.Abstractions;

namespace Resonance.Tests
{
    // Explicit persisted N6 fixture. Only the following two N7 attempts are real simulated battles;
    // the four earlier settlement identities are fixture history, not ordinary-play evidence.
    public sealed class OriginalBossRetryStrategyTests
    {
        readonly ITestOutputHelper _output;
        public OriginalBossRetryStrategyTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void SamePersistedBossOpeningNaturallyLosesThenWinsWithLegalCommands()
        {
            var store = new OriginalProfileStore(Path.Combine(Path.GetTempPath(), "original-boss-retry-tests",
                Guid.NewGuid().ToString("N"), "profile.v1.json"));
            var flow = PersistedN6Fixture(store);
            var atDoor = flow.Profile;
            Assert.Equal("N6", atDoor.ActiveRun.CurrentNode); Assert.Equal(4, atDoor.ActiveRun.BattleOrdinal);
            Assert.Equal(new[] { "C01" }, atDoor.ActiveRun.OwnedRelicIds);
            Assert.Equal(ExpeditionContent.GetPartyMaxHp("single"), atDoor.ActiveRun.PartyHp);

            var firstInput = flow.BeginBattle();
            string firstRawHash = Hash(firstInput);
            var firstFields = FieldHashes(firstInput);
            var checkpointHash = Hash(flow.Profile.ActiveRun.BossCheckpoint);
            Assert.Equal(5, firstInput.BattleOrdinal); Assert.Equal("N7", firstInput.Stage.Id);
            Assert.Equal(firstRawHash, checkpointHash);
            var first = RunBattleFactory.Create(firstInput);
            var firstMaxHp = first.Allies.Select(u => u.MaxHp).ToArray();
            var firstEnemyMaxHp = first.Enemies.Select(u => u.MaxHp).ToArray();
            Assert.False(first.ForceNoCrit);
            for (int tick = 0; tick < 400 * BattleSim.TickHz && first.Outcome == BattleOutcome.InProgress; tick++) first.Tick();
            PrintAttempt("natural-no-active-skills", firstInput, firstRawHash, first);
            Assert.Equal(BattleOutcome.Defeat, first.Outcome);
            Assert.All(first.Allies, u => Assert.Equal(0, u.Hp));
            Assert.Empty(first.CommandLog); Assert.False(first.ForceNoCrit);
            Assert.Equal(firstRawHash, Hash(firstInput));
            Assert.True(flow.CompleteBattle(firstInput.EncounterId, first.Outcome, first.Allies.Select(u => u.Hp).ToArray(),
                attemptId: firstInput.AttemptId));

            var afterLoss = flow.Profile;
            Assert.Equal(ExpeditionStatus.BossRetry, afterLoss.ActiveRun.Status);
            Assert.Null(afterLoss.LastRunSummary); Assert.Null(afterLoss.ActiveRun.PendingOffer);
            Assert.Equal(atDoor.ActiveRun.SettledBattleIds, afterLoss.ActiveRun.SettledBattleIds);
            Assert.Equal(atDoor.ActiveRun.SelectedChoices, afterLoss.ActiveRun.SelectedChoices);
            Assert.Equal(atDoor.DiscoveredRelics, afterLoss.DiscoveredRelics);
            Assert.Equal(atDoor.ClearedChapters, afterLoss.ClearedChapters);
            Assert.Equal(atDoor.UnlockedPresets, afterLoss.UnlockedPresets);
            Assert.Equal(atDoor.ActiveRun.OwnedRelicIds, afterLoss.ActiveRun.OwnedRelicIds);
            Assert.Equal(atDoor.ActiveRun.PartyHp, afterLoss.ActiveRun.PartyHp);
            Assert.Equal(checkpointHash, Hash(afterLoss.ActiveRun.BossCheckpoint));

            // Read the committed defeat/checkpoint from disk before asking Flow for a new attempt.
            flow = new ExpeditionFlow(new OriginalProfileStore(store.FilePath));
            var retryInput = flow.RetryBattle();
            string retryRawHash = Hash(retryInput);
            var retryFields = FieldHashes(retryInput);
            Assert.NotEqual(firstInput.AttemptId, retryInput.AttemptId);
            Assert.NotEqual(firstRawHash, retryRawHash);
            foreach (var field in firstFields)
            {
                if (field.Key == nameof(ExpeditionBattleInput.AttemptId)) Assert.NotEqual(field.Value, retryFields[field.Key]);
                else Assert.True(field.Value == retryFields[field.Key], "Frozen input field changed: " + field.Key);
                _output.WriteLine("inputField={0}; first={1}; retry={2}; mayDiffer={3}", field.Key, field.Value,
                    retryFields[field.Key], field.Key == nameof(ExpeditionBattleInput.AttemptId));
            }
            var normalizedFirst = firstInput.DeepClone(); var normalizedRetry = retryInput.DeepClone();
            normalizedFirst.AttemptId = null; normalizedRetry.AttemptId = null;
            Assert.Equal(Json(normalizedFirst), Json(normalizedRetry));
            _output.WriteLine("frozenInputHash={0}; rawFirstInputHash={1}; rawRetryInputHash={2}; contentVersion={3}; contentHash={4}; seed={5}",
                Hash(normalizedFirst), firstRawHash, retryRawHash, firstInput.ContentVersion, firstInput.ContentHash, firstInput.Seed);

            var afterRetry = flow.Profile;
            Assert.Equal(5, afterRetry.ActiveRun.BattleOrdinal);
            Assert.Equal(atDoor.ActiveRun.SettledBattleIds, afterRetry.ActiveRun.SettledBattleIds);
            Assert.Equal(atDoor.ActiveRun.SelectedChoices, afterRetry.ActiveRun.SelectedChoices);
            Assert.Equal(atDoor.DiscoveredRelics, afterRetry.DiscoveredRelics);
            Assert.Equal(atDoor.UnlockedPresets, afterRetry.UnlockedPresets);
            Assert.Null(afterRetry.ActiveRun.PendingOffer);
            Assert.Equal(checkpointHash, Hash(afterRetry.ActiveRun.BossCheckpoint));
            string activeProfileHash = Hash(afterRetry);
            Assert.Throws<InvalidOperationException>(() => flow.CompleteBattle(firstInput.EncounterId, first.Outcome,
                first.Allies.Select(u => u.Hp).ToArray(), attemptId: firstInput.AttemptId));
            Assert.Equal(activeProfileHash, Hash(flow.Profile));

            var retry = RunBattleFactory.Create(retryInput);
            Assert.Equal(firstMaxHp, retry.Allies.Select(u => u.MaxHp).ToArray());
            Assert.Equal(firstEnemyMaxHp, retry.Enemies.Select(u => u.MaxHp).ToArray());
            Assert.Equal(firstInput.OpeningHp, retry.Allies.Select(u => u.Hp).ToArray());
            Assert.Equal(firstInput.Seed, retryInput.Seed); Assert.False(retry.ForceNoCrit);
            int shieldCommands = 0, healCommands = 0;
            RunMaskFirstStrategy(retry, ref shieldCommands, ref healCommands);
            PrintAttempt("mask-first-aligned-shield-and-heal", retryInput, retryRawHash, retry);
            _output.WriteLine("acceptedShieldCommands={0}; acceptedHealCommands={1}; acceptedTotalCommands={2}; openingMaxHp={3}; enemyOpeningMaxHp={4}",
                shieldCommands, healCommands, retry.CommandLog.Count, string.Join(",", firstMaxHp), string.Join(",", firstEnemyMaxHp));
            Assert.Equal(BattleOutcome.Victory, retry.Outcome);
            Assert.True(shieldCommands > 0); Assert.True(healCommands > 0);
            Assert.All(retry.CommandLog, c => Assert.True(c.Accepted));
            Assert.Contains(retry.ExpeditionResolutions, r => r.Killed && !r.TargetAlly && r.TargetSlot > 0);
            Assert.False(retry.ForceNoCrit); Assert.Equal(retryRawHash, Hash(retryInput));
            Assert.True(flow.CompleteBattle(retryInput.EncounterId, retry.Outcome, retry.Allies.Select(u => u.Hp).ToArray(),
                facts: new[] { "fixture=N6-persisted; actual=N7-defeat-then-retry-victory", "frozenInputSha256=" + Hash(normalizedRetry) },
                attemptId: retryInput.AttemptId));
            var won = flow.Profile;
            Assert.Null(won.ActiveRun); Assert.True(won.LastRunSummary.Victory); Assert.True(won.LastRunSummary.ResultApplied);
            Assert.Equal(5, won.LastRunSummary.BattlesCompleted); Assert.Equal(new[] { "C01" }, won.LastRunSummary.RelicIds);
            Assert.Equal(new[] { "single", "sweep" }, won.UnlockedPresets); Assert.Single(won.ClearedChapters);
            Assert.Equal(atDoor.DiscoveredRelics, won.DiscoveredRelics);
            string settledProfileHash = Hash(won);
            Assert.False(flow.CompleteBattle(retryInput.EncounterId, retry.Outcome, retry.Allies.Select(u => u.Hp).ToArray(), attemptId: retryInput.AttemptId));
            Assert.Equal(settledProfileHash, Hash(flow.Profile));
        }

        static ExpeditionFlow PersistedN6Fixture(OriginalProfileStore store)
        {
            var flow = new ExpeditionFlow(store); flow.StartRun("C01", "single", 260921);
            var profile = flow.Profile; var run = profile.ActiveRun;
            run.RunId = "o3-persisted-n6-retry-fixture";
            run.CurrentNode = "N6"; run.Status = ExpeditionStatus.Ready; run.BattleOrdinal = 4;
            run.VisitedNodeIds = new[] { "N0", "N1", "N2-backstage", "N3", "N4", "N5" };
            run.SelectedChoices = new[] { "N0:C01", "R1:rest", "N2:N2-backstage", "R2:rest", "N3:rest", "R4:rest" };
            run.SettledBattleIds = new[] { run.RunId + "/N1/1", run.RunId + "/N2-backstage/2", run.RunId + "/N4/3", run.RunId + "/N5/4" };
            store.Save(profile.Revision, profile); flow.Reload();
            return flow;
        }

        static void RunMaskFirstStrategy(BattleSim sim, ref int shieldCommands, ref int healCommands)
        {
            var priority = new[] { 1, 2, 4, 0, 3 };
            for (int tick = 0; tick < 400 * BattleSim.TickHz && sim.Outcome == BattleOutcome.InProgress; tick++)
            {
                var target = sim.Enemies.Where(u => u.Alive && u.Slot != 0).OrderBy(u => u.Hp).ThenBy(u => u.Slot).FirstOrDefault() ?? sim.Enemies[0];
                if (target.Alive && sim.FocusEnemySlot != target.Slot)
                    Assert.True(sim.Submit(BattleCommand.FocusEnemy(target.Slot, CommandSource.Player)).Accepted);
                var intent = sim.OriginalIntentSnapshot;
                foreach (int slot in priority)
                {
                    if (!sim.CanAct(slot)) continue;
                    if (slot == 1 && (intent == null || !intent.IsCasting || intent.RemainingCastSec > 0.2f)) continue;
                    if (slot == 2 && !sim.Allies.Any(u => u.Alive && u.Hp <= u.MaxHp * 0.8f)) continue;
                    Assert.True(sim.Submit(BattleCommand.Tap(slot, CommandSource.Player)).Accepted, sim.FailedReason);
                    if (slot == 1) shieldCommands++;
                    if (slot == 2) healCommands++;
                    if (sim.Outcome != BattleOutcome.InProgress) break;
                }
                if (sim.Outcome == BattleOutcome.InProgress) sim.Tick();
            }
        }

        void PrintAttempt(string strategy, ExpeditionBattleInput input, string inputHash, BattleSim sim)
        {
            _output.WriteLine("strategy={0}; outcome={1}; gameSeconds={2:0.0000}; ticks={3}; inputHash={4}; attemptId={5}; seed={6}; areaCasts={7}; phase={8}; rebuilds={9}; endingHp={10}; error={11}",
                strategy, sim.Outcome, sim.OriginalEncounter.ElapsedSec, sim.TickIndex, inputHash, input.AttemptId, input.Seed,
                sim.OriginalEncounter.AreaCasts, sim.OriginalEncounter.Phase, sim.OriginalEncounter.MaskRebuildCount,
                string.Join(",", sim.Allies.Select(u => u.Hp)), sim.FailedReason);
        }

        static SortedDictionary<string, string> FieldHashes(ExpeditionBattleInput input)
        {
            var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var field in typeof(ExpeditionBattleInput).GetFields(BindingFlags.Public | BindingFlags.Instance))
                hashes[field.Name] = Sha256(Serialize(field.GetValue(input), field.FieldType));
            return hashes;
        }
        static string Hash<T>(T value) => Sha256(Serialize(value, typeof(T)));
        static string Json<T>(T value) => Encoding.UTF8.GetString(Serialize(value, typeof(T)));
        static byte[] Serialize(object value, Type type)
        {
            using (var stream = new MemoryStream())
            { new DataContractJsonSerializer(type).WriteObject(stream, value); return stream.ToArray(); }
        }
        static string Sha256(byte[] bytes)
        {
            using (var algorithm = SHA256.Create())
                return string.Concat(algorithm.ComputeHash(bytes).Select(x => x.ToString("x2")));
        }
    }
}
