using System;
using System.IO;
using System.Text;

namespace Resonance.Battle
{
    /// <summary>Optional evidence files, separate from atomic progression and never an opening input.</summary>
    public static class OriginalReplayStore
    {
        public static string SaveVerified(OriginalBattleRecord record, string directory)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Replay directory is required.", nameof(directory));
            var json = record.ToJson();
            var parsed = OriginalBattleRecord.FromJson(json);
            var report = OriginalBattleReplayer.Verify(parsed);
            if (!report.Match) throw new InvalidOperationException("Original replay mismatch: " + string.Join("; ", report.Differences));
            Directory.CreateDirectory(directory);
            // New file per capture: restarting an unfinished encounter can legitimately reuse AttemptId.
            var path = Path.Combine(directory, "battle-" + Guid.NewGuid().ToString("N") + ".original-replay.json");
            var pending = path + ".pending";
            var bytes = new UTF8Encoding(false).GetBytes(json);
            using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            File.Move(pending, path);
            return path;
        }
    }
}
