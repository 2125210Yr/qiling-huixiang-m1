using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Resonance.Battle;

namespace Resonance.Tools
{
    public static class OriginalReplayCli
    {
        public static int Run(string[] args, TextWriter output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            var result = new VerificationOutput();
            int exitCode = 2;
            string phase = "Arguments";
            try
            {
                if (args == null || args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
                    throw new ArgumentException("Usage: OriginalReplay.Verify <path-to-original-replay.json>");
                phase = "File";
                result.File = Path.GetFullPath(args[0]);
                // Hash and parse the same read. Never write, normalize or replace the source tape.
                var bytes = System.IO.File.ReadAllBytes(result.File);
                result.FileSha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                phase = "JSON";
                string json;
                using (var reader = new StreamReader(new MemoryStream(bytes, false), new UTF8Encoding(false, true), true))
                    json = reader.ReadToEnd();
                var record = OriginalBattleRecord.FromJson(json);
                result.SchemaVersion = record?.SchemaVersion;
                result.Format = record?.Format;
                result.InputHash = record?.InputHash;
                result.EndTick = record?.EndTick;
                var input = record?.Input;
                result.ModeId = input?.ModeId;
                result.RulesetId = input?.RulesetId;
                result.ContentVersion = input?.ContentVersion;
                result.ContentHash = input?.ContentHash;
                result.RunId = input?.RunId;
                result.EncounterId = input?.EncounterId;
                result.AttemptId = input?.AttemptId;
                result.BattleOrdinal = input?.BattleOrdinal;
                phase = "Replay verification";
                var verification = OriginalBattleReplayer.Verify(record);
                result.ReplayedOutcome = verification.Replayed?.Outcome.ToString();
                result.ReplayedEndTick = verification.Replayed?.TickIndex;
                result.ReplayedEventHash = verification.Replayed?.Events.ComputeHash();
                result.Differences = verification.Differences.ToArray();
                result.Match = verification.Match;
                result.Status = verification.Match ? "MATCH" : verification.Replayed == null ? "REJECTED" : "MISMATCH";
                exitCode = verification.Match ? 0 : 1;
            }
            catch (Exception error)
            {
                result.Status = "ERROR";
                result.Match = false;
                result.Differences = new[] { phase + " error: " + error.GetType().Name + ": " + error.Message };
            }
            output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions
            { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
            return exitCode;
        }

        sealed class VerificationOutput
        {
            public string Tool { get; set; } = "original-expedition-replay-verifier-v2";
            public string Status { get; set; } = "ERROR";
            public bool Match { get; set; }
            public string File { get; set; }
            public string FileSha256 { get; set; }
            public string VerifierContentHash { get; set; } = ExpeditionContent.ContentHash;
            public int? SchemaVersion { get; set; }
            public string Format { get; set; }
            public string ModeId { get; set; }
            public string RulesetId { get; set; }
            public string ContentVersion { get; set; }
            public string ContentHash { get; set; }
            public string RunId { get; set; }
            public string EncounterId { get; set; }
            public string AttemptId { get; set; }
            public int? BattleOrdinal { get; set; }
            public string InputHash { get; set; }
            public int? EndTick { get; set; }
            public string ReplayedOutcome { get; set; }
            public int? ReplayedEndTick { get; set; }
            public string ReplayedEventHash { get; set; }
            public string[] Differences { get; set; } = Array.Empty<string>();
        }
    }
}
