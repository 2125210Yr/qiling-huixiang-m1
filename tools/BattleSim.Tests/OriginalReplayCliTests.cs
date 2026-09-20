using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Resonance.Battle;
using Resonance.Tools;
using Xunit;

namespace Resonance.Tests
{
    // Each case retains its own temporary directory; no user replay or profile is touched.
    public sealed class OriginalReplayCliTests
    {
        [Fact]
        public void UnicodePathAndUtf8BomReplayReportsIdentityAndHashOfUnmodifiedFileBytes()
        {
            var record = ShortRecord();
            string path = Path.Combine(NewDirectory(), "真实 回放.original-replay.json");
            var encoding = new UTF8Encoding(true);
            byte[] bytes = encoding.GetPreamble().Concat(encoding.GetBytes(record.ToJson())).ToArray();
            File.WriteAllBytes(path, bytes);

            using var output = Invoke(new[] { path }, out int exitCode);
            var report = output.RootElement;
            Assert.Equal(0, exitCode);
            Assert.Equal("MATCH", report.GetProperty("status").GetString());
            Assert.True(report.GetProperty("match").GetBoolean());
            Assert.Equal(Path.GetFullPath(path), report.GetProperty("file").GetString());
            Assert.Equal(Hash(bytes), report.GetProperty("fileSha256").GetString());
            Assert.Equal(record.Format, report.GetProperty("format").GetString());
            Assert.Equal(record.SchemaVersion, report.GetProperty("schemaVersion").GetInt32());
            Assert.Equal(record.Input.ModeId, report.GetProperty("modeId").GetString());
            Assert.Equal(record.Input.RulesetId, report.GetProperty("rulesetId").GetString());
            Assert.Equal(record.Input.ContentVersion, report.GetProperty("contentVersion").GetString());
            Assert.Equal(record.Input.ContentHash, report.GetProperty("contentHash").GetString());
            Assert.Equal(record.Input.RunId, report.GetProperty("runId").GetString());
            Assert.Equal(record.Input.EncounterId, report.GetProperty("encounterId").GetString());
            Assert.Equal(record.Input.AttemptId, report.GetProperty("attemptId").GetString());
            Assert.Equal(record.Input.BattleOrdinal, report.GetProperty("battleOrdinal").GetInt32());
            Assert.Equal(record.InputHash, report.GetProperty("inputHash").GetString());
            Assert.Equal(record.EndTick, report.GetProperty("endTick").GetInt32());
            Assert.Equal(BattleOutcome.InProgress.ToString(), report.GetProperty("replayedOutcome").GetString());
            Assert.Equal(record.EndTick, report.GetProperty("replayedEndTick").GetInt32());
            Assert.Empty(Differences(report));
            Assert.All(report.EnumerateObject(), field => Assert.True(char.IsLower(field.Name[0]), field.Name));
            Assert.Equal(bytes, File.ReadAllBytes(path));
        }

        [Fact]
        public void TamperedExpectedFinalStateReportsMismatchWithReplayResult()
        {
            var record = ShortRecord();
            record.FinalState.BarrierEnergy++;
            string path = WriteRecord(record);
            byte[] before = File.ReadAllBytes(path);

            using var output = Invoke(new[] { path }, out int exitCode);
            var report = output.RootElement;
            Assert.Equal(1, exitCode);
            Assert.Equal("MISMATCH", report.GetProperty("status").GetString());
            Assert.False(report.GetProperty("match").GetBoolean());
            Assert.Contains("FinalState mismatch.", Differences(report));
            Assert.Equal(record.EndTick, report.GetProperty("replayedEndTick").GetInt32());
            Assert.Equal(BattleOutcome.InProgress.ToString(), report.GetProperty("replayedOutcome").GetString());
            Assert.Equal(Hash(before), report.GetProperty("fileSha256").GetString());
            Assert.Equal(before, File.ReadAllBytes(path));
        }

        [Theory]
        [InlineData("schema")]
        [InlineData("content")]
        public void UnsupportedReplayIsRejectedBeforeSimulation(string changedField)
        {
            var record = ShortRecord();
            if (changedField == "schema") record.SchemaVersion++;
            else
            {
                record.Input.ContentVersion += "-unsupported";
                record.InputHash = ExpeditionContent.Fingerprint(record.Input);
            }
            string path = WriteRecord(record);
            byte[] before = File.ReadAllBytes(path);

            using var output = Invoke(new[] { path }, out int exitCode);
            var report = output.RootElement;
            Assert.Equal(1, exitCode);
            Assert.Equal("REJECTED", report.GetProperty("status").GetString());
            Assert.False(report.GetProperty("match").GetBoolean());
            Assert.Contains(Differences(report), difference => difference.Contains(changedField, StringComparison.OrdinalIgnoreCase));
            AssertNoReplay(report);
            Assert.Equal(Hash(before), report.GetProperty("fileSha256").GetString());
            Assert.Equal(before, File.ReadAllBytes(path));
        }

        [Fact]
        public void MalformedJsonReportsParseErrorAndPreservesOriginalFile()
        {
            string path = Path.Combine(NewDirectory(), "malformed.original-replay.json");
            byte[] before = Encoding.UTF8.GetBytes("{ this is not JSON }");
            File.WriteAllBytes(path, before);

            using var output = Invoke(new[] { path }, out int exitCode);
            var report = output.RootElement;
            AssertError(report, exitCode);
            Assert.Contains(Differences(report), difference => difference.Contains("JSON", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(Hash(before), report.GetProperty("fileSha256").GetString());
            Assert.Equal(before, File.ReadAllBytes(path));
        }

        [Fact]
        public void MissingFileReportsIoErrorWithoutHashOrReplay()
        {
            string path = Path.Combine(NewDirectory(), "missing.original-replay.json");

            using var output = Invoke(new[] { path }, out int exitCode);
            var report = output.RootElement;
            AssertError(report, exitCode);
            Assert.Contains(Differences(report), difference => difference.Contains("missing.original-replay.json", StringComparison.Ordinal));
            Assert.Equal(JsonValueKind.Null, report.GetProperty("fileSha256").ValueKind);
            Assert.False(File.Exists(path));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void WrongArgumentCountReportsUsageError(int argumentCount)
        {
            string directory = NewDirectory();
            var args = Enumerable.Range(0, argumentCount)
                .Select(index => Path.Combine(directory, "unused-" + index + ".json")).ToArray();

            using var output = Invoke(args, out int exitCode);
            var report = output.RootElement;
            AssertError(report, exitCode);
            Assert.Contains(Differences(report), difference =>
                difference.Contains("argument", StringComparison.OrdinalIgnoreCase)
                || difference.Contains("usage", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(JsonValueKind.Null, report.GetProperty("fileSha256").ValueKind);
            Assert.Empty(Directory.GetFiles(directory));
        }

        static JsonDocument Invoke(string[] args, out int exitCode)
        {
            using var output = new StringWriter();
            exitCode = OriginalReplayCli.Run(args, output);
            // Parsing the entire output also rejects incidental logs or a second JSON document.
            return JsonDocument.Parse(output.ToString());
        }

        static OriginalBattleRecord ShortRecord()
        {
            var input = RunBattleFactory.CreateInput("N7", "single", 260921, new[] { "C01" }, null);
            input.RunId = "cli-short-run";
            input.EncounterId = "cli-short-run/N7/3";
            input.AttemptId = "cli-short-attempt-1";
            input.BattleOrdinal = 3;
            var sim = RunBattleFactory.Create(input);
            sim.Submit(BattleCommand.Tap(0));
            for (int tick = 0; tick < 120 && sim.Outcome == BattleOutcome.InProgress; tick++) sim.Tick();
            Assert.NotEmpty(sim.ExpeditionResolutions);
            Assert.Equal(BattleOutcome.InProgress, sim.Outcome);
            return OriginalBattleRecord.Capture(sim);
        }

        static string WriteRecord(OriginalBattleRecord record)
        {
            string path = Path.Combine(NewDirectory(), "short.original-replay.json");
            File.WriteAllText(path, record.ToJson(), new UTF8Encoding(false));
            return path;
        }

        static string NewDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "original-replay-cli-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        static string[] Differences(JsonElement report) => report.GetProperty("differences").EnumerateArray()
            .Select(value => value.GetString()).ToArray();

        static void AssertError(JsonElement report, int exitCode)
        {
            Assert.Equal(2, exitCode);
            Assert.Equal("ERROR", report.GetProperty("status").GetString());
            Assert.False(report.GetProperty("match").GetBoolean());
            Assert.NotEmpty(Differences(report));
            AssertNoReplay(report);
        }

        static void AssertNoReplay(JsonElement report)
        {
            Assert.Equal(JsonValueKind.Null, report.GetProperty("replayedOutcome").ValueKind);
            Assert.Equal(JsonValueKind.Null, report.GetProperty("replayedEndTick").ValueKind);
        }
    }
}
