using System;
using System.IO;
using System.Linq;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    // State-machine fixtures supply terminal HP; they are not ordinary play or battle balance evidence.
    public sealed class OriginalExpeditionFlowTests
    {
        static ExpeditionFlow NewFlow() => new ExpeditionFlow(new OriginalProfileStore(Path.Combine(Path.GetTempPath(), "original-expedition-tests", Guid.NewGuid().ToString("N"), "profile.v1.json")));
        static void Win(ExpeditionFlow flow, int[] hp = null)
        {
            var battle = flow.BeginBattle();
            Assert.True(flow.CompleteBattle(battle.EncounterId, BattleOutcome.Victory, hp ?? ExpeditionContent.GetPartyMaxHp(flow.Profile.ActiveRun.PresetId), attemptId: battle.AttemptId));
        }
        static void Pick(ExpeditionFlow flow, string preferred = null)
        {
            var p = flow.Profile; var offer = p.ActiveRun.PendingOffer;
            flow.ChooseReward(offer.Id, p.Revision, preferred != null && offer.CandidateIds.Contains(preferred) ? preferred : offer.CandidateIds[0]);
        }
        static void ToBoss(ExpeditionFlow flow, string core = "A01", bool reinforce = false)
        {
            flow.StartRun(core, "single", 438);
            Win(flow); Pick(flow); flow.ChooseRoute("N2-backstage"); Win(flow); Pick(flow);
            flow.ChooseWorkshop(!reinforce); if (reinforce) Pick(flow);
            Win(flow); Pick(flow); Win(flow);
            Assert.Equal("N6", flow.Profile.ActiveRun.CurrentNode);
        }

        [Theory]
        [InlineData(false, 4)]
        [InlineData(true, 5)]
        public void FiveBattleLoopSettlesOnceAndNextRunClearsTemporaryState(bool reinforce, int relicCount)
        {
            var flow = NewFlow(); ToBoss(flow, reinforce: reinforce);
            var before = flow.Profile;
            Assert.Equal(relicCount, before.ActiveRun.OwnedRelicIds.Length);
            var boss = flow.BeginBattle();
            Assert.Equal(5, boss.BattleOrdinal);
            Assert.True(flow.CompleteBattle(boss.EncounterId, BattleOutcome.Victory, new int[5], facts: new[] { "fixture terminal result" }, attemptId: boss.AttemptId));
            Assert.False(flow.CompleteBattle(boss.EncounterId, BattleOutcome.Victory, new int[5]));
            var won = flow.Profile;
            Assert.Null(won.ActiveRun);
            Assert.Equal(5, won.LastRunSummary.BattlesCompleted);
            Assert.True(won.LastRunSummary.ResultApplied);
            Assert.True(won.LastRunSummary.UnlockedNewPreset);
            Assert.Equal(new[] { "single", "sweep" }, won.UnlockedPresets);
            flow.StartRun("C01", "sweep", 994);
            Assert.Equal(new[] { "C01" }, flow.Profile.ActiveRun.OwnedRelicIds);
            Assert.Equal(ExpeditionContent.GetPartyMaxHp("sweep"), flow.Profile.ActiveRun.PartyHp);
            Assert.Null(flow.Profile.ActiveRun.BossCheckpoint);
            Assert.Empty(flow.Profile.ActiveRun.SettledBattleIds);
            Assert.Equal(won.DiscoveredRelics.Union(new[] { "C01" }).OrderBy(x => x), flow.Profile.DiscoveredRelics.OrderBy(x => x));
            Assert.Equal(won.LastRunSummary.RunId, flow.Profile.LastRunSummary.RunId);
        }

        [Fact]
        public void BranchCannotBeChosenTwiceAndRewardsCannotBeReplayed()
        {
            var flow = NewFlow(); flow.StartRun("B01", "single", 75); var input = flow.BeginBattle();
            flow.CompleteBattle(input.EncounterId, BattleOutcome.Victory, ExpeditionContent.GetPartyMaxHp("single"));
            var before = flow.Profile; var offer = before.ActiveRun.PendingOffer;
            Pick(flow);
            Assert.Throws<InvalidOperationException>(() => flow.ChooseReward(offer.Id, before.Revision, offer.CandidateIds.Last()));
            Assert.False(flow.CompleteBattle(input.EncounterId, BattleOutcome.Victory, ExpeditionContent.GetPartyMaxHp("single")));
            flow.ChooseRoute("N2-audience");
            Assert.Throws<InvalidOperationException>(() => flow.ChooseRoute("N2-backstage"));
            Win(flow); Pick(flow); flow.ChooseWorkshop(true); Win(flow); Pick(flow); Win(flow); Win(flow);
            Assert.Contains("N2-audience", flow.Profile.LastRunSummary.VisitedNodeIds);
            Assert.DoesNotContain("N2-backstage", flow.Profile.LastRunSummary.VisitedNodeIds);
        }

        [Fact]
        public void VictoryPreservesDownStateWorkshopRevivesAndBossDoorFullyRestores()
        {
            var flow = NewFlow(); flow.StartRun("A01", "single", 99);
            var max = ExpeditionContent.GetPartyMaxHp("single");
            int[] hp = { 0, 1, max[2] / 2, max[3], max[4] };
            Win(flow, hp);
            var healed = flow.Profile.ActiveRun.PartyHp;
            Assert.Equal(0, healed[0]);
            Assert.Equal(1 + max[1] * 15 / 100, healed[1]);
            Assert.Equal(Math.Min(max[2], hp[2] + max[2] * 15 / 100), healed[2]);
            Pick(flow); flow.ChooseRoute("N2-backstage");
            var next = flow.BeginBattle(); Assert.Equal(healed, next.OpeningHp);
            flow.CompleteBattle(next.EncounterId, BattleOutcome.Victory, hp); Pick(flow); flow.ChooseWorkshop(true);
            Assert.Equal(max[0] * 35 / 100, flow.Profile.ActiveRun.PartyHp[0]);
            Assert.Equal(max[1] * 50 / 100, flow.Profile.ActiveRun.PartyHp[1]);
            Win(flow, hp); Pick(flow); Win(flow, hp);
            Assert.Equal(max, flow.Profile.ActiveRun.PartyHp);
            Assert.Equal(max, flow.BeginBattle().OpeningHp);
        }

        [Fact]
        public void WorkshopReinforcementDoesNotHealAndDeclineConsumesOffer()
        {
            var flow = NewFlow(); flow.StartRun("C01", "single", 55);
            var max = ExpeditionContent.GetPartyMaxHp("single"); var hp = new[] { 0, 1, 1, 1, 1 };
            Win(flow, hp); Pick(flow); flow.ChooseRoute("N2-backstage"); Win(flow, hp); Pick(flow);
            var before = flow.Profile.ActiveRun.PartyHp; flow.ChooseWorkshop(false);
            Assert.Equal(before, flow.Profile.ActiveRun.PartyHp);
            var offer = flow.Profile.ActiveRun.PendingOffer; long rev = flow.Profile.Revision;
            flow.ChooseReward(offer.Id, rev);
            Assert.Equal("N4", flow.Profile.ActiveRun.CurrentNode);
            Assert.Equal(0, flow.Profile.ActiveRun.PartyHp[0]);
            Assert.Equal(before[1] + max[1] * 5 / 100, flow.Profile.ActiveRun.PartyHp[1]);
            Assert.Throws<InvalidOperationException>(() => flow.ChooseReward(offer.Id, rev));
        }

        [Fact]
        public void EngineeringFailureRetainsCheckpointOrdinaryDefeatEndsRun()
        {
            var flow = NewFlow(); flow.StartRun("B01", "single", 32); var first = flow.BeginBattle();
            flow.CompleteBattle(first.EncounterId, BattleOutcome.Failed, null, "unsupported effect");
            Assert.Equal(ExpeditionStatus.Failed, flow.Profile.ActiveRun.Status);
            Assert.Equal("unsupported effect", flow.Profile.ActiveRun.LastError);
            Assert.Empty(flow.Profile.ActiveRun.SettledBattleIds);
            Assert.Null(flow.Profile.LastRunSummary);
            var retry = flow.RetryBattle();
            Assert.Equal(first.Seed, retry.Seed); Assert.Equal(first.OpeningHp, retry.OpeningHp);
            Assert.NotEqual(first.AttemptId, retry.AttemptId);
            Assert.Throws<InvalidOperationException>(() => flow.CompleteBattle(first.EncounterId, BattleOutcome.Defeat, new int[5], attemptId: first.AttemptId));
            flow.CompleteBattle(retry.EncounterId, BattleOutcome.Defeat, new int[5], attemptId: retry.AttemptId);
            Assert.Null(flow.Profile.ActiveRun); Assert.False(flow.Profile.LastRunSummary.Victory);
            Assert.Equal(new[] { "single" }, flow.Profile.UnlockedPresets);
            Assert.Contains("B01", flow.Profile.DiscoveredRelics);
        }

        [Fact]
        public void BossRetryFreezesInputAndCannotRedraftOrReconfigure()
        {
            var flow = NewFlow(); ToBoss(flow); var first = flow.BeginBattle();
            flow.CompleteBattle(first.EncounterId, BattleOutcome.Defeat, new int[5]);
            Assert.Equal(ExpeditionStatus.BossRetry, flow.Profile.ActiveRun.Status);
            Assert.Throws<InvalidOperationException>(() => flow.SetBossPreset("single"));
            Assert.Throws<InvalidOperationException>(() => flow.ChooseWorkshop(false));
            flow.Reload(); var retry = flow.RetryBattle();
            Assert.NotEqual(first.AttemptId, retry.AttemptId);
            retry.AttemptId = first.AttemptId;
            Assert.Equal(Json(first), Json(retry));
            Assert.Equal(5, flow.Profile.ActiveRun.BattleOrdinal);
            var snapshot = flow.Profile; snapshot.ActiveRun.BossCheckpoint.OpeningHp[0] = 0;
            Assert.True(flow.Profile.ActiveRun.BossCheckpoint.OpeningHp[0] > 0);
        }

        [Fact]
        public void ReopeningMidBattleKeepsExactCheckpointAndOffersKeepOrder()
        {
            var flow = NewFlow(); flow.StartRun("A01", "single", 15); var original = flow.BeginBattle();
            long revision = flow.Profile.Revision; flow.Reload(); var reopened = flow.BeginBattle();
            Assert.Equal(Json(original), Json(reopened)); Assert.Equal(revision, flow.Profile.Revision);
            reopened.OpeningHp[0] = 0; Assert.NotEqual(0, flow.BeginBattle().OpeningHp[0]);
            flow.CompleteBattle(original.EncounterId, BattleOutcome.Victory, original.OpeningHp);
            var offer = flow.Profile.ActiveRun.PendingOffer; flow.Reload();
            Assert.Equal(Json(offer), Json(flow.Profile.ActiveRun.PendingOffer));
            var rng = new Random(44); for (int i = 0; i < 400; i++) rng.Next();
            Assert.Equal(Json(offer), Json(flow.Profile.ActiveRun.PendingOffer));
        }

        [Fact]
        public void RepeatedChapterWinDoesNotStackPermanentUnlock()
        {
            var flow = NewFlow(); ToBoss(flow); Win(flow); ToBoss(flow, "B01"); Win(flow);
            Assert.False(flow.Profile.LastRunSummary.UnlockedNewPreset);
            Assert.Single(flow.Profile.ClearedChapters);
            Assert.Equal(new[] { "single", "sweep" }, flow.Profile.UnlockedPresets);
        }

        [Theory]
        [InlineData("A01", "A02", "A04")]
        [InlineData("B01", "B02", "B04")]
        [InlineData("C01", "C03", "C04")]
        public void RewardDraftHonorsPrerequisitesAndGuaranteedProgress(string core, string support, string amplifier)
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var run = new ExpeditionState { RunId = "fixture", RunSeed = seed, CurrentNode = "N1", InitialCoreId = core, OwnedRelicIds = new[] { core } };
                var first = RewardDraft.Generate(run, "R1", "N2", 1);
                Assert.Equal(3, first.CandidateIds.Distinct().Count());
                Assert.Contains(first.CandidateIds, id => id[0] == core[0] && id != core);
                foreach (var id in first.CandidateIds) Assert.True(ExpeditionContent.FindRelic(id).IsEligible(run.OwnedRelicIds));
                Assert.DoesNotContain(amplifier, first.CandidateIds);
                run.OwnedRelicIds = new[] { core, support }; run.CurrentNode = "N4";
                var fourth = RewardDraft.Generate(run, "R4", "N5", 2); Assert.Contains(amplifier, fourth.CandidateIds);
                Assert.Equal(Json(fourth), Json(RewardDraft.Generate(run, "R4", "N5", 2)));
            }
        }

        [Fact]
        public void MissingC03CannotOfferC04AndExhaustionNeverDuplicatesOptions()
        {
            var run = new ExpeditionState { RunId = "fixture", RunSeed = 8, CurrentNode = "N4", InitialCoreId = "C01", OwnedRelicIds = new[] { "C01", "C02" } };
            var offer = RewardDraft.Generate(run, "R4", "N5", 1);
            Assert.Contains("C03", offer.CandidateIds); Assert.DoesNotContain("C04", offer.CandidateIds);
            run.OwnedRelicIds = ExpeditionContent.Relics.Select(r => r.Id).Where(id => id != "A04").ToArray();
            offer = RewardDraft.Generate(run, "R4", "N5", 2);
            Assert.Equal(new[] { "A04" }, offer.CandidateIds); Assert.False(offer.IsRecoveryOnly);
            run.OwnedRelicIds = ExpeditionContent.Relics.Select(r => r.Id).ToArray();
            offer = RewardDraft.Generate(run, "R4", "N5", 3);
            Assert.Empty(offer.CandidateIds); Assert.True(offer.IsRecoveryOnly);
        }

        [Theory]
        [InlineData("A01", "A02", "A03", "A04")]
        [InlineData("B01", "B02", "B03", "B04")]
        [InlineData("C01", "C02", "C03", "C04")]
        public void EveryFamilyHasAReproducibleFourPiecePathBeforeN5(string core, string first, string second, string amplifier)
        {
            int found = -1;
            for (int seed = 0; seed < 200 && found < 0; seed++)
            {
                var run = new ExpeditionState { RunId = "seed-search", RunSeed = seed, CurrentNode = "N1", InitialCoreId = core, OwnedRelicIds = new[] { core } };
                if (!RewardDraft.Generate(run, "R1", "N2", 1).CandidateIds.Contains(first)) continue;
                run.OwnedRelicIds = new[] { core, first }; run.CurrentNode = "N2-backstage";
                if (RewardDraft.Generate(run, "R2", "N3", 2).CandidateIds.Contains(second)) found = seed;
            }
            Assert.True(found >= 0, "No deterministic legal four-piece path found.");
            var flow = NewFlow(); flow.StartRun(core, "single", found);
            Win(flow); Assert.Contains(first, flow.Profile.ActiveRun.PendingOffer.CandidateIds); Pick(flow, first);
            flow.ChooseRoute("N2-backstage"); Win(flow);
            Assert.Contains(second, flow.Profile.ActiveRun.PendingOffer.CandidateIds); Pick(flow, second);
            flow.ChooseWorkshop(true); Win(flow);
            Assert.Contains(amplifier, flow.Profile.ActiveRun.PendingOffer.CandidateIds); Pick(flow, amplifier);
            Assert.Equal(new[] { core, first, second, amplifier }, flow.Profile.ActiveRun.OwnedRelicIds);
            var assembled = flow.BeginBattle();
            Assert.Equal("N5", assembled.Stage.Id);
            Assert.Equal(new[] { core, first, second, amplifier }, assembled.RelicIds);
            Console.WriteLine("Controlled flow fixture: core=" + core + " runSeed=" + found + " route=N2-backstage workshop=rest R1=" + first + " R2=" + second + " R4=" + amplifier);
        }

        internal static string Json<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
