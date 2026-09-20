using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace Resonance.Battle
{
    public enum ProfileWritePoint { BeforeWrite, AfterTempFlushed, BeforeReplace, AfterReplace }

    public sealed class OriginalProfileStore
    {
        readonly string _path;
        readonly Action<ProfileWritePoint> _fault;
        public string FilePath => _path;
        public bool RecoveredFromBackup { get; private set; }

        public OriginalProfileStore(string path, Action<ProfileWritePoint> fault = null)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Original profile path is required.");
            _path = Path.GetFullPath(path);
            if (!string.Equals(Path.GetFileName(_path), "profile.v1.json", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Original mode must use its independent profile.v1.json file.");
            _fault = fault;
        }

        public OriginalProfile Load()
        {
            RecoveredFromBackup = false;
            if (!File.Exists(_path) && !File.Exists(_path + ".bak")) return new OriginalProfile();
            OriginalProfile result;
            if (TryRead(_path, out result)) return result;
            if (TryRead(_path + ".bak", out result)) { RecoveredFromBackup = true; return result; }
            throw new InvalidDataException("原创存档与备份都无法验证。已保留文件，请选择有效备份恢复；不会自动清空存档。");
        }

        // Returns the committed state only after the atomic commit. Failed callers retain their old snapshot.
        public OriginalProfile Save(long expectedRevision, OriginalProfile next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            using (var gate = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var previous = Load();
                var preserveBackup = RecoveredFromBackup;
                if (previous.Revision != expectedRevision) throw new InvalidOperationException("存档版本已改变，请重新载入后继续。");
                var candidate = Clone(next);
                candidate.Revision = checked(expectedRevision + 1);
                candidate.IntegritySha256 = null;
                Validate(candidate);
                candidate.IntegritySha256 = Hash(Serialize(candidate));
                byte[] bytes = Serialize(candidate);
                var temporary = _path + ".tmp";
                _fault?.Invoke(ProfileWritePoint.BeforeWrite);
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                _fault?.Invoke(ProfileWritePoint.AfterTempFlushed);
                _fault?.Invoke(ProfileWritePoint.BeforeReplace);
                if (File.Exists(_path))
                    File.Replace(temporary, _path, preserveBackup ? null : _path + ".bak");
                else File.Move(temporary, _path);
                _fault?.Invoke(ProfileWritePoint.AfterReplace);
                RecoveredFromBackup = false;
                return Clone(candidate);
            }
        }

        public static T Clone<T>(T value)
        {
            if (ReferenceEquals(value, null)) return default(T);
            using (var stream = new MemoryStream(Serialize(value)))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }

        static byte[] Serialize<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                return stream.ToArray();
            }
        }

        static string Hash(byte[] bytes)
        {
            using (var hash = SHA256.Create())
            {
                var data = hash.ComputeHash(bytes); var result = new StringBuilder(data.Length * 2);
                foreach (byte b in data) result.Append(b.ToString("x2"));
                return result.ToString();
            }
        }

        static bool TryRead(string path, out OriginalProfile result)
        {
            result = null;
            if (!File.Exists(path)) return false;
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    result = (OriginalProfile)new DataContractJsonSerializer(typeof(OriginalProfile)).ReadObject(stream);
                Validate(result);
                string saved = result.IntegritySha256;
                result.IntegritySha256 = null;
                string computed = Hash(Serialize(result));
                result.IntegritySha256 = saved;
                if (saved == null || !string.Equals(saved, computed, StringComparison.Ordinal)) { result = null; return false; }
                return true;
            }
            catch (Exception ex) when (ex is System.Runtime.Serialization.SerializationException || ex is InvalidDataException || ex is System.Xml.XmlException || ex is FormatException || ex is ArgumentException)
            { result = null; return false; }
        }

        public static void Validate(OriginalProfile profile)
        {
            if (profile == null || profile.SchemaVersion != 1 || profile.Revision < 0 || string.IsNullOrEmpty(profile.ContentVersion))
                throw new InvalidDataException("Unsupported or invalid original profile.");
            Unique(profile.UnlockedPresets); Unique(profile.DiscoveredRelics); Unique(profile.ClearedChapters);
            if (Array.IndexOf(profile.UnlockedPresets, "single") < 0) throw new InvalidDataException("Base preset is missing.");
            var run = profile.ActiveRun;
            if (run == null) return;
            if (string.IsNullOrEmpty(run.RunId) || string.IsNullOrEmpty(run.CurrentNode) || string.IsNullOrEmpty(run.FrozenContentHash) ||
                string.IsNullOrEmpty(run.RulesetId) || string.IsNullOrEmpty(run.PresetId) || string.IsNullOrEmpty(run.InitialCoreId) ||
                run.PartyHp == null || run.PartyHp.Length != 5 || run.BattleOrdinal < 0 || run.BattleOrdinal > 5 ||
                !Enum.IsDefined(typeof(ExpeditionStatus), run.Status)) throw new InvalidDataException("Invalid active expedition.");
            foreach (int hp in run.PartyHp) if (hp < 0) throw new InvalidDataException("Invalid expedition HP.");
            Unique(run.UnlockSnapshot); Unique(run.OwnedRelicIds); Unique(run.VisitedNodeIds); Unique(run.SelectedChoices); Unique(run.SettledBattleIds);
            if (Array.IndexOf(run.OwnedRelicIds, run.InitialCoreId) < 0) throw new InvalidDataException("Starting core is missing.");
            if (Array.IndexOf(run.VisitedNodeIds, "N2-backstage") >= 0 && Array.IndexOf(run.VisitedNodeIds, "N2-audience") >= 0)
                throw new InvalidDataException("Mutually exclusive branches were both visited.");
            if ((run.Status == ExpeditionStatus.Reward) != (run.PendingOffer != null)) throw new InvalidDataException("Reward state does not match offer.");
            if (run.PendingOffer != null)
            {
                Unique(run.PendingOffer.CandidateIds);
                if (string.IsNullOrEmpty(run.PendingOffer.Id) || run.PendingOffer.Revision != profile.Revision ||
                    run.PendingOffer.CandidateIds.Length > 3 || run.PendingOffer.IsRecoveryOnly != (run.PendingOffer.CandidateIds.Length == 0))
                    throw new InvalidDataException("Invalid reward offer.");
            }
            if ((run.Status == ExpeditionStatus.Battle || run.Status == ExpeditionStatus.Failed || run.Status == ExpeditionStatus.BossRetry) && run.CurrentBattleCheckpoint == null)
                throw new InvalidDataException("Battle checkpoint is missing.");
            if (run.Status == ExpeditionStatus.BossRetry && run.BossCheckpoint == null) throw new InvalidDataException("Boss checkpoint is missing.");
        }

        static void Unique(string[] values)
        {
            if (values == null) throw new InvalidDataException("Profile collection is missing.");
            var found = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
                if (string.IsNullOrEmpty(value) || !found.Add(value)) throw new InvalidDataException("Duplicate or missing profile identity.");
        }
    }
}
