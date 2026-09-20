using System;
using System.IO;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class OriginalProfileStoreTests
    {
        static string TestPath()
        {
            string folder = Path.Combine(Path.GetTempPath(), "original-profile-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder); return Path.Combine(folder, "profile.v1.json");
        }

        [Theory]
        [InlineData(ProfileWritePoint.BeforeWrite, false)]
        [InlineData(ProfileWritePoint.AfterTempFlushed, false)]
        [InlineData(ProfileWritePoint.BeforeReplace, false)]
        [InlineData(ProfileWritePoint.AfterReplace, true)]
        public void AtomicWriteFaultProducesWholeOldOrWholeNewState(ProfileWritePoint point, bool committed)
        {
            string path = TestPath(); var store = new OriginalProfileStore(path);
            var old = store.Save(0, store.Load());
            var failing = new OriginalProfileStore(path, p => { if (p == point) throw new IOException("injected " + p); });
            var next = OriginalProfileStore.Clone(old);
            next.UnlockedPresets = new[] { "single", "sweep" }; next.ClearedChapters = new[] { "silent-theatre" };
            next.LastRunSummary = new ExpeditionRunSummary { RunId = "completed", ResultApplied = true, Victory = true };
            Assert.Throws<IOException>(() => failing.Save(old.Revision, next));
            var loaded = store.Load();
            Assert.Equal(committed ? 2 : 1, loaded.Revision);
            Assert.Equal(committed, Array.IndexOf(loaded.UnlockedPresets, "sweep") >= 0);
            Assert.Equal(committed, Array.IndexOf(loaded.ClearedChapters, "silent-theatre") >= 0);
            Assert.Equal(committed, loaded.LastRunSummary != null && loaded.LastRunSummary.ResultApplied);
            Assert.Equal(1, old.Revision);
        }

        [Fact]
        public void SaveFailureDoesNotPublishRewardAndSuccessfulRestartCannotDoubleCollect()
        {
            string path = TestPath(); bool fail = false;
            var flow = new ExpeditionFlow(new OriginalProfileStore(path, p => { if (fail && p == ProfileWritePoint.BeforeReplace) throw new IOException("injected"); }));
            flow.StartRun("A01", "single", 8); var battle = flow.BeginBattle();
            flow.CompleteBattle(battle.EncounterId, BattleOutcome.Victory, battle.OpeningHp);
            var profile = flow.Profile; var offer = profile.ActiveRun.PendingOffer;
            fail = true;
            Assert.Throws<IOException>(() => flow.ChooseReward(offer.Id, profile.Revision, offer.CandidateIds[0]));
            Assert.Equal(profile.Revision, flow.Profile.Revision);
            Assert.Equal(new[] { "A01" }, flow.Profile.ActiveRun.OwnedRelicIds);
            Assert.Equal(offer.Id, flow.Profile.ActiveRun.PendingOffer.Id);
            var restarted = new ExpeditionFlow(new OriginalProfileStore(path));
            restarted.ChooseReward(offer.Id, profile.Revision, offer.CandidateIds[0]);
            restarted.Reload(); Assert.Equal(2, restarted.Profile.ActiveRun.OwnedRelicIds.Length);
            Assert.Null(restarted.Profile.ActiveRun.PendingOffer);
            Assert.Throws<InvalidOperationException>(() => restarted.ChooseReward(offer.Id, profile.Revision, offer.CandidateIds[1]));
        }

        [Fact]
        public void FailureAfterRewardCommitReopensExactlyOneAwardAndRejectsStaleRetry()
        {
            string path = TestPath(); bool fail = false;
            var flow = new ExpeditionFlow(new OriginalProfileStore(path, p => { if (fail && p == ProfileWritePoint.AfterReplace) throw new IOException("crash after commit"); }));
            flow.StartRun("B01", "single", 12); var battle = flow.BeginBattle();
            flow.CompleteBattle(battle.EncounterId, BattleOutcome.Victory, battle.OpeningHp);
            var before = flow.Profile; var offer = before.ActiveRun.PendingOffer;
            fail = true;
            Assert.Throws<IOException>(() => flow.ChooseReward(offer.Id, before.Revision, offer.CandidateIds[0]));
            Assert.Equal(before.Revision, flow.Profile.Revision);
            Assert.Throws<InvalidOperationException>(() => flow.ChooseReward(offer.Id, before.Revision, offer.CandidateIds[1]));
            flow.Reload();
            Assert.Equal(2, flow.Profile.ActiveRun.OwnedRelicIds.Length);
            Assert.Contains(offer.CandidateIds[0], flow.Profile.ActiveRun.OwnedRelicIds);
            Assert.DoesNotContain(offer.CandidateIds[1], flow.Profile.ActiveRun.OwnedRelicIds);
            Assert.Equal("N2", flow.Profile.ActiveRun.CurrentNode);
        }

        [Fact]
        public void InterruptedFirstWriteKeepsTheValidEmptyProfileAndNoLegacyFile()
        {
            string path = TestPath();
            var flow = new ExpeditionFlow(new OriginalProfileStore(path, p => { if (p == ProfileWritePoint.AfterTempFlushed) throw new IOException("first write interrupted"); }));
            Assert.Throws<IOException>(() => flow.StartRun("A01", "single", 1));
            var loaded = new OriginalProfileStore(path).Load();
            Assert.Equal(0, loaded.Revision); Assert.Null(loaded.ActiveRun);
            Assert.False(File.Exists(path)); Assert.True(File.Exists(path + ".tmp"));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(path), "save.json")));
        }

        [Fact]
        public void IncompatibleContentCanBeExplicitlyEndedButNeverResumed()
        {
            string path = TestPath(); var flow = new ExpeditionFlow(new OriginalProfileStore(path));
            flow.StartRun("C01", "single", 1); var previous = flow.Profile;
            previous.ActiveRun.FrozenContentHash = "a-different-content-version";
            new OriginalProfileStore(path).Save(previous.Revision, previous);
            flow.Reload();
            Assert.Throws<InvalidOperationException>(() => flow.BeginBattle());
            Assert.Equal("C01", flow.Profile.ActiveRun.InitialCoreId);
            flow.EndRun();
            Assert.Null(flow.Profile.ActiveRun); Assert.False(flow.Profile.LastRunSummary.Victory);
            Assert.Contains("C01", flow.Profile.DiscoveredRelics);
        }

        [Fact]
        public void ValidBackupRecoversAndFollowingWritePreservesIt()
        {
            string path = TestPath(); var store = new OriginalProfileStore(path);
            var first = store.Save(0, store.Load()); var second = OriginalProfileStore.Clone(first);
            second.DiscoveredRelics = new[] { "A01" }; store.Save(1, second);
            File.WriteAllText(path, "{broken");
            var recovered = store.Load(); Assert.True(store.RecoveredFromBackup); Assert.Equal(1, recovered.Revision);
            var backup = File.ReadAllBytes(path + ".bak");
            recovered.DiscoveredRelics = new[] { "B01" }; store.Save(1, recovered);
            Assert.Equal(backup, File.ReadAllBytes(path + ".bak"));
            Assert.Equal(new[] { "B01" }, store.Load().DiscoveredRelics);
        }

        [Fact]
        public void TwoCorruptCopiesFailWithoutResetOrOverwrite()
        {
            string path = TestPath(); File.WriteAllText(path, "corrupt-body"); File.WriteAllText(path + ".bak", "corrupt-backup");
            var store = new OriginalProfileStore(path);
            Assert.Throws<InvalidDataException>(() => store.Load());
            Assert.Throws<InvalidDataException>(() => store.Save(0, new OriginalProfile()));
            Assert.Equal("corrupt-body", File.ReadAllText(path)); Assert.Equal("corrupt-backup", File.ReadAllText(path + ".bak"));
        }

        [Fact]
        public void SemanticallyValidJsonTamperFailsIntegrityInsteadOfGrantingUnlock()
        {
            string path = TestPath(); var store = new OriginalProfileStore(path); store.Save(0, store.Load());
            string text = File.ReadAllText(path).Replace("\"single\"", "\"single\",\"sweep\"");
            File.WriteAllText(path, text);
            Assert.Throws<InvalidDataException>(() => store.Load());
            Assert.Equal(text, File.ReadAllText(path));
        }

        [Fact]
        public void RevisionCompareAndSwapRejectsSecondWriterAndLegacyFileIsUntouched()
        {
            string path = TestPath(); string legacy = Path.Combine(Path.GetDirectoryName(path), "save.json");
            byte[] original = { 0, 1, 22, 200, 255 }; File.WriteAllBytes(legacy, original);
            var one = new ExpeditionFlow(new OriginalProfileStore(path)); var two = new ExpeditionFlow(new OriginalProfileStore(path));
            one.StartRun("A01", "single", 1);
            Assert.Throws<InvalidOperationException>(() => two.StartRun("B01", "single", 1));
            Assert.Null(two.Profile.ActiveRun);
            Assert.Equal(original, File.ReadAllBytes(legacy));
            Assert.Throws<ArgumentException>(() => new OriginalProfileStore(legacy));
            var reloaded = new OriginalProfileStore(path).Load(); Assert.Equal("A01", reloaded.ActiveRun.InitialCoreId);
        }
    }
}
