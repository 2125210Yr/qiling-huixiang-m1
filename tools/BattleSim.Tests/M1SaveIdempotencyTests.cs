using System;
using System.IO;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// M1-G2-SAVE-IDEM: ApplyVictory 通关奖励内部幂等夹具。
    /// 不是 T23 还原（无门票、无中断恢复战斗切片、无支付/账号）。
    /// </summary>
    public sealed class M1SaveIdempotencyTests
    {
        [Fact]
        public void RepeatApplyVictoryDoesNotCopyRewards()
        {
            Catalog.BuildBuiltin();
            var b = new SaveBlob();
            var gold = b.Gold;
            var stone = b.Stone;

            var first = SaveStore.ApplyVictory(b, 0, false);
            Assert.False(string.IsNullOrEmpty(first));
            Assert.Equal(1, b.ClearedCount);
            Assert.Equal(0, b.ClearedHard);
            Assert.Equal(2, b.GetUnit(b.PartyIds[0]).Level);
            Assert.Equal(4, b.GetUnit(b.PartyIds[0]).Affection);
            Assert.Equal("EQ_WPN", b.GetUnit(b.PartyIds[0]).Gear0);
            Assert.Equal(gold, b.Gold);
            Assert.Equal(stone, b.Stone);

            var afterFirst = Snap(b);
            var second = SaveStore.ApplyVictory(b, 0, false);
            Assert.Equal("", second);
            AssertSame(afterFirst, b);
            Assert.Equal(gold, b.Gold);
            Assert.Equal(stone, b.Stone);
        }

        [Fact]
        public void NextStageGrantsOnceReplayDoesNot()
        {
            Catalog.BuildBuiltin();
            var b = new SaveBlob();
            SaveStore.ApplyVictory(b, 0, false);
            var after0 = Snap(b);

            var next = SaveStore.ApplyVictory(b, 1, false);
            Assert.False(string.IsNullOrEmpty(next));
            Assert.Equal(2, b.ClearedCount);
            Assert.Equal(after0.LeadLevel + 1, b.GetUnit(b.PartyIds[0]).Level);
            Assert.Equal(after0.LeadAff + 4, b.GetUnit(b.PartyIds[0]).Affection);

            var after1 = Snap(b);
            Assert.Equal("", SaveStore.ApplyVictory(b, 0, false));
            Assert.Equal("", SaveStore.ApplyVictory(b, 1, false));
            AssertSame(after1, b);
        }

        [Fact]
        public void HardAndNormalFirstClearsAreSeparate()
        {
            Catalog.BuildBuiltin();
            var b = new SaveBlob();
            SaveStore.ApplyVictory(b, 0, false);
            var afterNormal = Snap(b);

            var hard = SaveStore.ApplyVictory(b, 0, true);
            Assert.False(string.IsNullOrEmpty(hard));
            Assert.Equal(1, b.ClearedCount);
            Assert.Equal(1, b.ClearedHard);
            Assert.Equal(afterNormal.LeadLevel + 1, b.GetUnit(b.PartyIds[0]).Level);
            Assert.Equal(afterNormal.LeadAff + 4, b.GetUnit(b.PartyIds[0]).Affection);

            var afterHard = Snap(b);
            Assert.Equal("", SaveStore.ApplyVictory(b, 0, true));
            Assert.Equal("", SaveStore.ApplyVictory(b, 0, false));
            AssertSame(afterHard, b);
        }

        [Fact]
        public void ReloadFromTempDoesNotCopyVictoryOrTouchLocalLow()
        {
            Catalog.BuildBuiltin();
            var userSave = SaveStore.DefaultPath;
            var userExisted = File.Exists(userSave);
            var userStamp = userExisted ? File.GetLastWriteTimeUtc(userSave) : (DateTime?)null;
            var userDir = Path.GetDirectoryName(userSave);
            var userDirExisted = !string.IsNullOrEmpty(userDir) && Directory.Exists(userDir);

            var path = Path.Combine(Path.GetTempPath(), "resonance-save-idem-" + Guid.NewGuid().ToString("N") + ".json");
            Assert.StartsWith(
                Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(path),
                StringComparison.OrdinalIgnoreCase);
            Assert.False(string.Equals(Path.GetFullPath(path), Path.GetFullPath(userSave), StringComparison.OrdinalIgnoreCase));

            try
            {
                var b = new SaveBlob();
                SaveStore.EnsureStarterKit(b);
                SaveStore.ApplyVictory(b, 0, false);
                var gold = b.Gold;
                var stone = b.Stone;
                SaveStore.Write(b, path);
                Assert.True(File.Exists(path));

                var loaded = SaveStore.LoadOrNew(path);
                var before = Snap(loaded);
                Assert.Equal("", SaveStore.ApplyVictory(loaded, 0, false));
                AssertSame(before, loaded);
                Assert.Equal(gold, loaded.Gold);
                Assert.Equal(stone, loaded.Stone);

                SaveStore.Write(loaded, path);
                var again = SaveStore.LoadOrNew(path);
                Assert.Equal(before.Cleared, again.ClearedCount);
                Assert.Equal(before.LeadLevel, again.GetUnit(again.PartyIds[0]).Level);
                Assert.Equal(before.LeadAff, again.GetUnit(again.PartyIds[0]).Affection);
            }
            finally
            {
                WipeTemp(path);
            }

            if (userExisted)
                Assert.Equal(userStamp.Value, File.GetLastWriteTimeUtc(userSave));
            else
                Assert.False(File.Exists(userSave));
            if (!userDirExisted && !string.IsNullOrEmpty(userDir))
                Assert.False(Directory.Exists(userDir));
        }

        static ProgressSnap Snap(SaveBlob b)
        {
            var lead = b.GetUnit(b.PartyIds[0]);
            return new ProgressSnap
            {
                Cleared = b.ClearedCount,
                ClearedHard = b.ClearedHard,
                Gold = b.Gold,
                Stone = b.Stone,
                LeadLevel = lead.Level,
                LeadAff = lead.Affection,
                Gear0 = lead.Gear0 ?? "",
                Gear1 = lead.Gear1 ?? "",
                Gear2 = lead.Gear2 ?? "",
                Gear3 = lead.Gear3 ?? "",
                PartyLevels = PartyLevels(b),
                PartyAffs = PartyAffs(b)
            };
        }

        static void AssertSame(ProgressSnap s, SaveBlob b)
        {
            Assert.Equal(s.Cleared, b.ClearedCount);
            Assert.Equal(s.ClearedHard, b.ClearedHard);
            Assert.Equal(s.Gold, b.Gold);
            Assert.Equal(s.Stone, b.Stone);
            Assert.Equal(s.PartyLevels, PartyLevels(b));
            Assert.Equal(s.PartyAffs, PartyAffs(b));
            var lead = b.GetUnit(b.PartyIds[0]);
            Assert.Equal(s.Gear0, lead.Gear0 ?? "");
            Assert.Equal(s.Gear1, lead.Gear1 ?? "");
            Assert.Equal(s.Gear2, lead.Gear2 ?? "");
            Assert.Equal(s.Gear3, lead.Gear3 ?? "");
        }

        static int[] PartyLevels(SaveBlob b)
        {
            var a = new int[b.PartyIds.Length];
            for (int i = 0; i < a.Length; i++)
                a[i] = string.IsNullOrEmpty(b.PartyIds[i]) ? 0 : b.GetUnit(b.PartyIds[i]).Level;
            return a;
        }

        static int[] PartyAffs(SaveBlob b)
        {
            var a = new int[b.PartyIds.Length];
            for (int i = 0; i < a.Length; i++)
                a[i] = string.IsNullOrEmpty(b.PartyIds[i]) ? 0 : b.GetUnit(b.PartyIds[i]).Affection;
            return a;
        }

        static void WipeTemp(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
            try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            try { if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp"); } catch { }
        }

        sealed class ProgressSnap
        {
            public int Cleared;
            public int ClearedHard;
            public int Gold;
            public int Stone;
            public int LeadLevel;
            public int LeadAff;
            public string Gear0;
            public string Gear1;
            public string Gear2;
            public string Gear3;
            public int[] PartyLevels;
            public int[] PartyAffs;
        }
    }
}
