using System;
using System.IO;
using System.Text;

namespace Resonance.Battle
{
    public sealed class SaveBlob
    {
        public string[] PartyIds = (string[])Catalog.DefaultParty.Clone();
        public int LeaderSlot;
        public bool Vs1Cleared;
        public int LastSeed;
        public int Speed = 1;
        public bool AutoTap;
    }

    public static class SaveStore
    {
        public static string DefaultPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Resonance", "save.json");

        public static SaveBlob LoadOrNew(string path = null)
        {
            path = path ?? DefaultPath;
            try
            {
                if (File.Exists(path))
                    return Parse(File.ReadAllText(path));
            }
            catch
            {
                // fall through to new save
            }
            return new SaveBlob();
        }

        public static void Write(SaveBlob blob, string path = null)
        {
            path = path ?? DefaultPath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, Serialize(blob));
        }

        public static string Serialize(SaveBlob b)
        {
            var sb = new StringBuilder();
            sb.Append("{\"party\":[");
            for (int i = 0; i < b.PartyIds.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(b.PartyIds[i]).Append('"');
            }
            sb.Append("],\"leader\":").Append(b.LeaderSlot);
            sb.Append(",\"cleared\":").Append(b.Vs1Cleared ? "true" : "false");
            sb.Append(",\"seed\":").Append(b.LastSeed);
            sb.Append(",\"speed\":").Append(b.Speed);
            sb.Append(",\"auto\":").Append(b.AutoTap ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }

        public static SaveBlob Parse(string json)
        {
            var b = new SaveBlob();
            b.Vs1Cleared = json.IndexOf("\"cleared\":true", StringComparison.Ordinal) >= 0;
            b.AutoTap = json.IndexOf("\"auto\":true", StringComparison.Ordinal) >= 0;
            b.LeaderSlot = ReadInt(json, "\"leader\":", 0);
            b.LastSeed = ReadInt(json, "\"seed\":", 0);
            b.Speed = Math.Max(1, ReadInt(json, "\"speed\":", 1));
            return b;
        }

        static int ReadInt(string json, string key, int fallback)
        {
            var i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return fallback;
            i += key.Length;
            var n = 0;
            var any = false;
            var neg = false;
            if (i < json.Length && json[i] == '-') { neg = true; i++; }
            while (i < json.Length && json[i] >= '0' && json[i] <= '9')
            {
                n = n * 10 + (json[i] - '0');
                any = true;
                i++;
            }
            if (!any) return fallback;
            return neg ? -n : n;
        }
    }
}
