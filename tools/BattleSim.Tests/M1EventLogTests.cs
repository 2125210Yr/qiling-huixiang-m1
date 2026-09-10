using System;
using System.IO;
using System.Text;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class M1EventLogTests
    {
        [Fact]
        public void SameSeedSameTicksExportHashMatches()
        {
            var a = RunFixed(20260910, 90);
            var b = RunFixed(20260910, 90);
            Assert.True(a.Events.Events.Count > 0);
            Assert.Equal(a.Events.ExportCanonical(), b.Events.ExportCanonical());
            Assert.Equal(a.Events.ComputeHash(), b.Events.ComputeHash());
            Assert.Equal(a.Events.ComputeHash(), BattleEventLog.HashUtf8(a.Events.ExportCanonical()));
            Assert.All(a.Events.Events, e => Assert.Equal(FormulaProfile.GL_UNKNOWN, e.Profile));
        }

        [Fact]
        public void SameSeedSameCommandSequenceExportHashMatches()
        {
            var a = PlayManualSequence(20260910);
            var b = PlayManualSequence(20260910);
            Assert.True(a.Events.Events.Count > 0);
            Assert.Equal(a.Events.ExportCanonical(), b.Events.ExportCanonical());
            Assert.Equal(a.Events.ComputeHash(), b.Events.ComputeHash());
        }

        [Fact]
        public void ExportToMemoryAndTempHashesMatchAndSkipUserSave()
        {
            var sim = RunFixed(20260910, 90);
            var canonical = sim.Events.ExportCanonical();
            var hash = sim.Events.ComputeHash();
            Assert.False(string.IsNullOrEmpty(canonical));
            Assert.Equal(64, hash.Length);

            using (var ms = new MemoryStream())
            {
                sim.Events.ExportTo(ms);
                Assert.Equal(canonical, Encoding.UTF8.GetString(ms.ToArray()));
                Assert.Equal(hash, BattleEventLog.HashUtf8(Encoding.UTF8.GetString(ms.ToArray())));
            }

            var savePath = SaveStore.DefaultPath;
            var saveExisted = File.Exists(savePath);
            DateTime? saveStamp = saveExisted ? File.GetLastWriteTimeUtc(savePath) : (DateTime?)null;
            var saveDir = Path.GetDirectoryName(savePath);
            var saveDirExisted = !string.IsNullOrEmpty(saveDir) && Directory.Exists(saveDir);

            Assert.Throws<InvalidOperationException>(() => sim.Events.ExportToFile(savePath));
            if (!string.IsNullOrEmpty(saveDir))
            {
                var beside = Path.Combine(saveDir, "resonance-eventlog-must-not-land.log");
                Assert.Throws<InvalidOperationException>(() => sim.Events.ExportToFile(beside));
                Assert.False(File.Exists(beside));
            }
            if (saveExisted)
                Assert.Equal(saveStamp.Value, File.GetLastWriteTimeUtc(savePath));
            else
                Assert.False(File.Exists(savePath));
            if (!saveDirExisted && !string.IsNullOrEmpty(saveDir))
                Assert.False(Directory.Exists(saveDir));

            var tmp = Path.Combine(Path.GetTempPath(), "resonance-eventlog-" + Guid.NewGuid().ToString("N") + ".log");
            try
            {
                sim.Events.ExportToFile(tmp);
                Assert.True(File.Exists(tmp));
                Assert.StartsWith(
                    Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    Path.GetFullPath(tmp),
                    StringComparison.OrdinalIgnoreCase);
                Assert.False(string.Equals(Path.GetFullPath(tmp), Path.GetFullPath(savePath), StringComparison.OrdinalIgnoreCase));
                var fileText = File.ReadAllText(tmp, Encoding.UTF8);
                Assert.Equal(canonical, fileText);
                Assert.Equal(hash, BattleEventLog.HashUtf8(fileText));
                Assert.Equal(hash, BattleEventLog.HashUtf8(Encoding.UTF8.GetString(File.ReadAllBytes(tmp))));
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }

            if (saveExisted)
                Assert.Equal(saveStamp.Value, File.GetLastWriteTimeUtc(savePath));
            else
                Assert.False(File.Exists(savePath));
        }

        [Fact]
        public void EmptyLogExportIsHashableAndFileExportRequiresPath()
        {
            var log = new BattleEventLog();
            Assert.Equal("", log.ExportCanonical());
            Assert.Equal(BattleEventLog.HashUtf8(""), log.ComputeHash());
            Assert.Throws<ArgumentException>(() => log.ExportToFile(null));
            Assert.Throws<ArgumentException>(() => log.ExportToFile(""));
            Assert.Throws<ArgumentNullException>(() => log.ExportTo(null));
        }

        static BattleSim RunFixed(int seed, int ticks)
        {
            var sim = new BattleSim(Catalog.DefaultParty, 0, seed) { Deterministic = true, Speed = 1 };
            sim.AutoTap = true;
            for (int i = 0; i < ticks; i++)
                sim.Tick();
            return sim;
        }

        static BattleSim PlayManualSequence(int seed)
        {
            var sim = new BattleSim(Catalog.DefaultParty, 0, seed)
            {
                Deterministic = true,
                Speed = 1,
                Auto = AutoMode.Manual
            };
            for (int i = 0; i < BattleSim.TickHz; i++)
                sim.Tick();
            if (sim.Allies[0] != null) sim.Allies[0].Charge = 100f;
            sim.TryTap(0);
            for (int i = 0; i < BattleSim.TickHz; i++)
                sim.Tick();
            if (sim.Allies[0] != null)
            {
                sim.Allies[0].Charge = 100f;
                sim.Allies[0].SlideCd = 0f;
            }
            sim.TrySlide(0);
            for (int i = 0; i < BattleSim.TickHz; i++)
                sim.Tick();
            return sim;
        }
    }
}
