using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.Battle
{
    public sealed class SaveBlob
    {
        public string[] PartyIds = (string[])Catalog.DefaultParty.Clone();
        public int LeaderSlot;
        public int ClearedCount;
        public int LastSeed;
        public int Speed = 1;
        public AutoMode Auto;
        public bool AutoTap
        {
            get => Auto != AutoMode.Manual;
            set => Auto = value ? AutoMode.Full : AutoMode.Manual;
        }
        public bool UseHard;
        public int ClearedHard;
        public int Gold = 12000;
        public int Stone = 180;
        public int Meal = -1;
        public bool PvpDoor;
        public int DailyClaimed;
        public int MailRead;
        public readonly List<UnitProgress> Units = new List<UnitProgress>();
        public readonly List<string> Puppets = new List<string>();
        public readonly List<string> Roster = new List<string>();
        public readonly List<string> Skins = new List<string>();

        public bool Owns(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (Roster != null && Roster.Count > 0)
            {
                for (int i = 0; i < Roster.Count; i++)
                    if (Roster[i] == id) return true;
                return false;
            }
            if (PartyIds == null) return false;
            for (int i = 0; i < PartyIds.Length; i++)
                if (PartyIds[i] == id) return true;
            return false;
        }

        public bool Grant(string id)
        {
            if (string.IsNullOrEmpty(id) || Roster == null) return false;
            for (int i = 0; i < Roster.Count; i++)
                if (Roster[i] == id) return false;
            Roster.Add(id);
            GetUnit(id);
            return true;
        }

        public bool SpendGold(int n)
        {
            if (n <= 0) return true;
            if (Gold < n) return false;
            Gold -= n;
            return true;
        }

        public bool SpendStone(int n)
        {
            if (n <= 0) return true;
            if (Stone < n) return false;
            Stone -= n;
            return true;
        }

        public void AddGold(int n)
        {
            if (n <= 0) return;
            Gold += n;
            if (Gold > 9999999) Gold = 9999999;
        }

        public void AddStone(int n)
        {
            if (n <= 0) return;
            Stone += n;
            if (Stone > 999999) Stone = 999999;
        }

        public bool Vs1Cleared
        {
            get => ClearedCount > 0;
            set { if (value && ClearedCount < 1) ClearedCount = 1; }
        }

        public UnitProgress GetUnit(string id)
        {
            if (id == null) id = "";
            if (Units != null)
            {
                for (int i = 0; i < Units.Count; i++)
                {
                    var u = Units[i];
                    if (u != null && u.Id == id) return u;
                }
                var p = new UnitProgress { Id = id, Level = 1 };
                Units.Add(p);
                return p;
            }
            return new UnitProgress { Id = id, Level = 1 };
        }

        public int PartyLength => PartyIds != null ? PartyIds.Length : 0;
        public int LivePartyCap => FightStats.CapForLength(PartyLength);

        public int ClampLeaderSlot(int slot)
        {
            var last = PartyLength - 1;
            if (last < 0) return 0;
            if (slot < 0) return 0;
            if (slot > last) return last;
            return slot;
        }

        public string LeaderId()
        {
            if (PartyIds == null || PartyIds.Length == 0) return null;
            return PartyIds[ClampLeaderSlot(LeaderSlot)];
        }

        public UnitProgress[] ProgressForParty()
        {
            var n = PartyLength > 0 ? PartyLength : (Catalog.DefaultParty != null ? Catalog.DefaultParty.Length : FightStats.DefaultPartyCap);
            if (n < 1) n = FightStats.DefaultPartyCap;
            var a = new UnitProgress[n];
            for (int i = 0; i < n; i++)
            {
                var id = PartyIds != null && i < PartyIds.Length ? PartyIds[i] : null;
                if (string.IsNullOrEmpty(id) && Catalog.DefaultParty != null && i < Catalog.DefaultParty.Length)
                    id = Catalog.DefaultParty[i];
                a[i] = GetUnit(id ?? "");
            }
            return a;
        }

        public bool IsStageLocked(int index)
        {
            var n = Catalog.Stages != null ? Catalog.Stages.Length : 12;
            if (UseHard)
            {
                if (ClearedCount < n) return true;
                return index > ClearedHard;
            }
            return index > ClearedCount;
        }

        public void SetPartySlot(int slot, string id)
        {
            if (PartyIds == null || slot < 0 || slot >= PartyIds.Length) return;
            if (!string.IsNullOrEmpty(id) && !Owns(id)) return;
            for (int i = 0; i < PartyIds.Length; i++)
            {
                if (i != slot && PartyIds[i] == id)
                {
                    PartyIds[i] = PartyIds[slot];
                    break;
                }
            }
            PartyIds[slot] = id;
        }
    }

    public static class SaveStore
    {
        const string Company = "Resonance";
        const string Product = "契灵回响";
        const string FileName = "save.json";
        static readonly Encoding Utf8 = new UTF8Encoding(false);
        static string _pathOverride;

        public static string UserDefaultPath =>
            Path.GetFullPath(Path.Combine(LocalLowRoot(), Company, Product, FileName));

        public static bool HasPathOverride => !string.IsNullOrEmpty(_pathOverride);

        public static void SetPathOverride(string path)
        {
            _pathOverride = string.IsNullOrEmpty(path) ? null : Path.GetFullPath(path);
        }

        public static string DefaultPath => HasPathOverride ? _pathOverride : UserDefaultPath;

        public static bool IsUserDefaultPath(string path) => SamePath(path, UserDefaultPath);

        static string LocalLowRoot()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Path.GetDirectoryName(local);
            if (string.IsNullOrEmpty(appData))
                appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData");
            return Path.Combine(appData, "LocalLow");
        }

        static bool SamePath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }

        public static SaveBlob LoadOrNew(string path = null)
        {
            path = string.IsNullOrEmpty(path) ? DefaultPath : path;
            var loaded = TryLoadViable(path);
            if (loaded == null && SamePath(path, UserDefaultPath) && !HasPathOverride)
                loaded = TryAdoptLegacy(path);
            if (loaded != null)
            {
                Sanitize(loaded);
                Costume.ReplaceFrom(loaded.Skins);
                EnsureStarterKit(loaded);
                return loaded;
            }
            return SeedNew();
        }

        public static void EnsureStarterKit(SaveBlob b)
        {
            if (b == null || b.PartyIds == null || b.PartyIds.Length == 0) return;
            var reserves = new[] { "TSTEE", "TTEEE", "SSTEE", "TEEEE", "ESTEE" };
            for (int i = 0; i < b.PartyIds.Length; i++)
            {
                var pid = b.PartyIds[i];
                if (string.IsNullOrEmpty(pid)) continue;
                var u = b.GetUnit(pid);
                if (string.IsNullOrEmpty(u.Gear0) && string.IsNullOrEmpty(u.Gear1) &&
                    string.IsNullOrEmpty(u.Gear2) && string.IsNullOrEmpty(u.Gear3))
                {
                    SetGear(u, i % 4, GearCatalog.ForSlot(i % 4));
                    if (u.Id == "C001" && string.IsNullOrEmpty(u.Gear3))
                        u.Gear3 = GearCatalog.DefaultCartaId;
                }
                if (Growth.ReserveAllEmpty(u.Reserve))
                    u.Reserve = reserves[i % reserves.Length];
            }
            PuppetCatalog.FillStarter(b.Puppets);
            EnsureRoster(b);
            if (Costume.Unlocked("C001", "echo") == false)
                Costume.Unlock("C001", "echo");
        }

        static void EnsureRoster(SaveBlob b)
        {
            if (b == null) return;
            if (b.Roster == null) return;
            if (b.Roster.Count == 0)
            {
                var starter = Catalog.DefaultParty;
                for (int i = 0; i < starter.Length; i++)
                    b.Grant(starter[i]);
            }
            if (b.PartyIds == null) return;
            for (int i = 0; i < b.PartyIds.Length; i++)
                b.Grant(b.PartyIds[i]);
        }

        public static string ApplyVictory(SaveBlob b, int stageIndex, bool hard)
        {
            if (b == null) return "";
            var need = stageIndex + 1;
            if (need < 1) return "";
            if ((hard ? b.ClearedHard : b.ClearedCount) >= need) return "";

            var prevCleared = b.ClearedCount;
            var prevHard = b.ClearedHard;
            var unitCount = b.Units != null ? b.Units.Count : 0;
            var snaps = CaptureUnits(b);
            try
            {
                if (hard) b.ClearedHard = need;
                else b.ClearedCount = need;
                GrantVictoryAffection(b);
                return ApplyClearReward(b, stageIndex);
            }
            catch
            {
                b.ClearedCount = prevCleared;
                b.ClearedHard = prevHard;
                RestoreUnits(b, snaps, unitCount);
                throw;
            }
        }

        static void GrantVictoryAffection(SaveBlob b)
        {
            if (b == null || b.PartyIds == null) return;
            for (int i = 0; i < b.PartyIds.Length; i++)
            {
                var pid = b.PartyIds[i];
                if (string.IsNullOrEmpty(pid)) continue;
                var u = b.GetUnit(pid);
                if (u != null && u.Affection < 100) u.Affection = Math.Min(100, u.Affection + 4);
            }
        }

        struct UnitSnap
        {
            public UnitProgress Unit;
            public int Level;
            public int Affection;
            public string Gear0;
            public string Gear1;
            public string Gear2;
            public string Gear3;
        }

        static UnitSnap[] CaptureUnits(SaveBlob b)
        {
            if (b == null || b.Units == null || b.Units.Count == 0)
                return new UnitSnap[0];
            var a = new UnitSnap[b.Units.Count];
            for (int i = 0; i < b.Units.Count; i++)
            {
                var u = b.Units[i];
                a[i].Unit = u;
                if (u == null) continue;
                a[i].Level = u.Level;
                a[i].Affection = u.Affection;
                a[i].Gear0 = u.Gear0;
                a[i].Gear1 = u.Gear1;
                a[i].Gear2 = u.Gear2;
                a[i].Gear3 = u.Gear3;
            }
            return a;
        }

        static void RestoreUnits(SaveBlob b, UnitSnap[] snaps, int unitCount)
        {
            if (b == null || b.Units == null) return;
            while (b.Units.Count > unitCount)
                b.Units.RemoveAt(b.Units.Count - 1);
            if (snaps == null) return;
            var n = snaps.Length;
            if (n > b.Units.Count) n = b.Units.Count;
            for (int i = 0; i < n; i++)
            {
                var u = snaps[i].Unit;
                if (u == null) continue;
                u.Level = snaps[i].Level;
                u.Affection = snaps[i].Affection;
                u.Gear0 = snaps[i].Gear0;
                u.Gear1 = snaps[i].Gear1;
                u.Gear2 = snaps[i].Gear2;
                u.Gear3 = snaps[i].Gear3;
            }
        }

        public static string ApplyClearReward(SaveBlob b, int stageIndex)
        {
            if (b == null || b.PartyIds == null || b.PartyIds.Length == 0) return "";
            var loot = "";
            var slot = ((stageIndex % 4) + 4) % 4;
            var gearId = GearCatalog.ForSlot(slot);
            var gear = GearCatalog.Try(gearId);
            var who = b.GetUnit(PartyIdAt(b, stageIndex));
            if (GetGear(who, slot).Length == 0)
            {
                SetGear(who, slot, gearId);
                loot = "掉落  " + (gear != null ? gear.Name : gearId) + "  →  " + CharName(who.Id);
            }
            for (int i = 0; i < b.PartyIds.Length; i++)
            {
                var pid = b.PartyIds[i];
                if (string.IsNullOrEmpty(pid)) continue;
                var u = b.GetUnit(pid);
                if (u != null && u.Level < Growth.MaxLevel) u.Level++;
            }
            if (loot.Length > 0) return loot;
            return CharName(who.Id) + "  LV " + who.Level;
        }

        static string PartyIdAt(SaveBlob b, int slot)
        {
            var n = b != null && b.PartyIds != null && b.PartyIds.Length > 0
                ? b.PartyIds.Length
                : (Catalog.DefaultParty != null ? Catalog.DefaultParty.Length : FightStats.DefaultPartyCap);
            if (n < 1) n = FightStats.DefaultPartyCap;
            slot = ((slot % n) + n) % n;
            if (b != null && b.PartyIds != null && slot < b.PartyIds.Length &&
                !string.IsNullOrEmpty(b.PartyIds[slot]))
                return b.PartyIds[slot];
            if (Catalog.DefaultParty != null && slot < Catalog.DefaultParty.Length)
                return Catalog.DefaultParty[slot];
            return Catalog.DefaultParty != null && Catalog.DefaultParty.Length > 0 ? Catalog.DefaultParty[0] : "";
        }

        static string CharName(string id)
        {
            var ch = Catalog.TryChar(id);
            return ch != null ? ch.Name : (id ?? "");
        }

        static string GetGear(UnitProgress u, int slot)
        {
            if (u == null) return "";
            if (slot == 1) return u.Gear1 ?? "";
            if (slot == 2) return u.Gear2 ?? "";
            if (slot == 3) return u.Gear3 ?? "";
            return u.Gear0 ?? "";
        }

        static void SetGear(UnitProgress u, int slot, string id)
        {
            if (u == null) return;
            if (slot == 1) u.Gear1 = id;
            else if (slot == 2) u.Gear2 = id;
            else if (slot == 3) u.Gear3 = id;
            else u.Gear0 = id;
        }

        static SaveBlob SeedNew()
        {
            var b = new SaveBlob();
            EnsureStarterKit(b);
            return b;
        }

        public static SaveBlob Reset(string path = null)
        {
            path = string.IsNullOrEmpty(path) ? DefaultPath : path;
            TryDelete(path);
            TryDelete(Sidecar(path, ".bak"));
            TryDelete(Sidecar(path, ".tmp"));
            var b = SeedNew();
            Write(b, path);
            return b;
        }

        public static void Write(SaveBlob blob, string path = null)
        {
            path = string.IsNullOrEmpty(path) ? DefaultPath : path;
            if (HasPathOverride && SamePath(path, UserDefaultPath))
                path = _pathOverride;
            if (blob == null) blob = SeedNew();
            Sanitize(blob);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = Serialize(blob);
            var tmp = Sidecar(path, ".tmp");
            Exception last = null;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    WriteAll(tmp, json);
                    Commit(tmp, path);
                    return;
                }
                catch (Exception e)
                {
                    last = e;
                }
            }
            if (last != null) throw last;
        }

        static string Sidecar(string path, string suffix) => path + suffix;

        static void WriteAll(string path, string json)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var sw = new StreamWriter(fs, Utf8))
            {
                sw.Write(json);
                sw.Flush();
                try { fs.Flush(true); }
                catch { fs.Flush(); }
            }
        }

        static void Commit(string tmp, string path)
        {
            var bak = Sidecar(path, ".bak");
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tmp, path, bak, true);
                    TryDelete(tmp);
                    return;
                }
                catch
                {
                    try { File.Copy(path, bak, true); }
                    catch { }
                }
            }
            try
            {
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch
            {
                File.Copy(tmp, path, true);
                TryDelete(tmp);
            }
        }

        static void TryDelete(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        static SaveBlob TryLoadViable(string path)
        {
            if (TryReadViable(path, out var b)) return b;
            if (TryReadViable(Sidecar(path, ".bak"), out b)) return b;
            if (TryReadViable(Sidecar(path, ".tmp"), out b)) return b;
            return null;
        }

        static SaveBlob TryAdoptLegacy(string dest)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var candidates = new[]
            {
                Path.Combine(LocalLowRoot(), Company, Company, FileName),
                Path.Combine(local, Company, FileName)
            };
            for (int i = 0; i < candidates.Length; i++)
            {
                var src = candidates[i];
                if (SamePath(src, dest)) continue;
                var b = TryLoadViable(src);
                if (b == null) continue;
                try { Write(b, dest); }
                catch { }
                return b;
            }
            return null;
        }

        static bool TryReadViable(string path, out SaveBlob blob)
        {
            blob = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            string json = null;
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    json = File.ReadAllText(path, Utf8);
                    break;
                }
                catch
                {
                    if (i == 1) return false;
                }
            }
            return TryParse(json, out blob);
        }

        static bool TryParse(string json, out SaveBlob blob)
        {
            blob = null;
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var root = MiniJson.Parse(json) as Dictionary<string, object>;
                if (root != null && LooksLikeSave(root))
                {
                    blob = FromRoot(root);
                    Sanitize(blob);
                    return blob != null && blob.PartyIds != null && blob.PartyIds.Length > 0;
                }
            }
            catch
            {
            }
            if (json.IndexOf("\"party\"", StringComparison.Ordinal) < 0 &&
                json.IndexOf("\"clearedN\"", StringComparison.Ordinal) < 0)
                return false;
            try
            {
                blob = LegacyParse(json);
                Sanitize(blob);
                return blob != null && blob.PartyIds != null && blob.PartyIds.Length > 0;
            }
            catch
            {
                blob = null;
                return false;
            }
        }

        static bool LooksLikeSave(Dictionary<string, object> root)
        {
            if (root == null || root.Count == 0) return false;
            return root.ContainsKey("party") || root.ContainsKey("clearedN") || root.ContainsKey("units")
                || root.ContainsKey("cleared") || root.ContainsKey("leader") || root.ContainsKey("puppets")
                || root.ContainsKey("g0");
        }

        public static string Serialize(SaveBlob b)
        {
            if (b == null) b = new SaveBlob();
            Sanitize(b);
            var sb = new StringBuilder();
            sb.Append("{\"party\":[");
            var partyN = b.PartyLength > 0
                ? b.PartyLength
                : (Catalog.DefaultParty != null ? Catalog.DefaultParty.Length : FightStats.DefaultPartyCap);
            if (partyN < 1) partyN = FightStats.DefaultPartyCap;
            for (int i = 0; i < partyN; i++)
            {
                if (i > 0) sb.Append(',');
                var id = b.PartyIds != null && i < b.PartyIds.Length ? b.PartyIds[i] : null;
                if (string.IsNullOrEmpty(id) && Catalog.DefaultParty != null && i < Catalog.DefaultParty.Length)
                    id = Catalog.DefaultParty[i];
                if (id == null) id = "";
                sb.Append('"').Append(Esc(id)).Append('"');
            }
            sb.Append("],\"leader\":").Append(b.LeaderSlot);
            sb.Append(",\"cleared\":").Append(b.Vs1Cleared ? "true" : "false");
            sb.Append(",\"clearedN\":").Append(b.ClearedCount);
            sb.Append(",\"seed\":").Append(b.LastSeed);
            sb.Append(",\"speed\":").Append(b.Speed);
            sb.Append(",\"auto\":").Append((int)b.Auto);
            sb.Append(",\"hard\":").Append(b.UseHard ? "true" : "false");
            sb.Append(",\"clearedH\":").Append(b.ClearedHard);
            sb.Append(",\"gold\":").Append(b.Gold);
            sb.Append(",\"stone\":").Append(b.Stone);
            sb.Append(",\"meal\":").Append(b.Meal);
            sb.Append(",\"pvp\":").Append(b.PvpDoor ? "true" : "false");
            sb.Append(",\"daily\":").Append(b.DailyClaimed);
            sb.Append(",\"mail\":").Append(b.MailRead);
            Costume.WriteTo(b.Skins);
            sb.Append(",\"roster\":[");
            WriteStringList(sb, b.Roster);
            sb.Append("],\"skins\":[");
            WriteStringList(sb, b.Skins);
            sb.Append("],\"puppets\":[");
            var puppets = b.Puppets;
            if (puppets != null)
            {
                for (int i = 0; i < puppets.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(Esc(puppets[i])).Append('"');
                }
            }
            sb.Append("],\"units\":[");
            var units = b.Units;
            var unitWritten = false;
            if (units != null)
            {
                for (int i = 0; i < units.Count; i++)
                {
                    var u = units[i];
                    if (u == null) continue;
                    if (unitWritten) sb.Append(',');
                    unitWritten = true;
                    sb.Append("{\"id\":\"").Append(Esc(u.Id)).Append("\",\"lv\":").Append(u.Level);
                    sb.Append(",\"uncap\":").Append(u.Uncap).Append(",\"ign\":").Append(u.Ignition);
                    sb.Append(",\"ia\":").Append(u.IgnAtk).Append(",\"ic\":").Append(u.IgnCrt).Append(",\"ig\":").Append(u.IgnAgl);
                    sb.Append(",\"aff\":").Append(u.Affection);
                    sb.Append(",\"rsv\":\"").Append(Esc(Growth.NormalizedReserve(u.Reserve))).Append("\"");
                    sb.Append(",\"skin\":\"").Append(Esc(u.SkinId)).Append("\"");
                    sb.Append(",\"g0\":\"").Append(Esc(u.Gear0)).Append("\",\"g1\":\"").Append(Esc(u.Gear1)).Append("\"");
                    sb.Append(",\"g2\":\"").Append(Esc(u.Gear2)).Append("\",\"g3\":\"").Append(Esc(u.Gear3)).Append("\"");
                    sb.Append(",\"p0\":").Append(u.Plus0).Append(",\"p1\":").Append(u.Plus1);
                    sb.Append(",\"p2\":").Append(u.Plus2).Append(",\"p3\":").Append(u.Plus3).Append("}");
                }
            }
            sb.Append("]}");
            return sb.ToString();
        }

        static void WriteStringList(StringBuilder sb, List<string> list)
        {
            if (list == null) return;
            var first = true;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.IsNullOrEmpty(list[i])) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(Esc(list[i])).Append('"');
            }
        }

        public static SaveBlob Parse(string json)
        {
            if (TryParse(json, out var b) && b != null) return b;
            var fresh = new SaveBlob();
            Sanitize(fresh);
            return fresh;
        }

        static SaveBlob FromRoot(Dictionary<string, object> root)
        {
            var b = new SaveBlob();
            if (root.TryGetValue("party", out var p) && p is List<object> list && list.Count > 0)
            {
                b.PartyIds = new string[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    var fallback = Catalog.DefaultParty != null && i < Catalog.DefaultParty.Length
                        ? Catalog.DefaultParty[i]
                        : (Catalog.DefaultParty != null && Catalog.DefaultParty.Length > 0 ? Catalog.DefaultParty[0] : "");
                    var id = Convert.ToString(list[i]);
                    if (string.IsNullOrEmpty(id)) id = fallback;
                    if (Catalog.Characters != null && Catalog.Characters.Count > 0 && !Catalog.Characters.ContainsKey(id))
                        id = fallback;
                    b.PartyIds[i] = id;
                }
            }
            b.LeaderSlot = ReadNum(root, "leader", 0);
            b.ClearedCount = ReadNum(root, "clearedN", 0);
            if (b.ClearedCount < 1 && root.TryGetValue("cleared", out var c) && c is bool ok && ok)
                b.ClearedCount = 1;
            b.LastSeed = ReadNum(root, "seed", 0);
            b.Speed = Math.Max(1, ReadNum(root, "speed", 1));
            b.Auto = ReadAuto(root);
            if (root.TryGetValue("hard", out var h) && h is bool hard) b.UseHard = hard;
            b.ClearedHard = ReadNum(root, "clearedH", 0);
            b.Gold = ReadNum(root, "gold", 12000);
            b.Stone = ReadNum(root, "stone", 180);
            b.Meal = ReadNum(root, "meal", -1);
            if (root.TryGetValue("pvp", out var pvp) && pvp is bool door) b.PvpDoor = door;
            b.DailyClaimed = ReadNum(root, "daily", 0);
            b.MailRead = ReadNum(root, "mail", 0);
            ReadStringList(root, "roster", b.Roster);
            ReadStringList(root, "skins", b.Skins);
            if (root.TryGetValue("puppets", out var pup) && pup is List<object> plist)
            {
                b.Puppets.Clear();
                for (int i = 0; i < plist.Count; i++)
                {
                    var id = Convert.ToString(plist[i]);
                    if (!string.IsNullOrEmpty(id)) b.Puppets.Add(id);
                }
            }
            if (root.TryGetValue("units", out var u) && u is List<object> units)
            {
                for (int i = 0; i < units.Count; i++)
                {
                    var o = units[i] as Dictionary<string, object>;
                    if (o == null) continue;
                    var id = Str(o, "id");
                    if (string.IsNullOrEmpty(id)) continue;
                    var prog = b.GetUnit(id);
                    prog.Level = Math.Max(1, ReadNum(o, "lv", 1));
                    prog.Uncap = ReadNum(o, "uncap", 0);
                    prog.Ignition = ReadNum(o, "ign", 0);
                    prog.IgnAtk = ReadNum(o, "ia", 0);
                    prog.IgnCrt = ReadNum(o, "ic", 0);
                    prog.IgnAgl = ReadNum(o, "ig", 0);
                    prog.Affection = ReadNum(o, "aff", 0);
                    prog.Reserve = Growth.NormalizedReserve(Str(o, "rsv"));
                    prog.SkinId = Str(o, "skin");
                    prog.Gear0 = Str(o, "g0");
                    prog.Gear1 = Str(o, "g1");
                    prog.Gear2 = Str(o, "g2");
                    prog.Gear3 = Str(o, "g3");
                    prog.Plus0 = ReadNum(o, "p0", 0);
                    prog.Plus1 = ReadNum(o, "p1", 0);
                    prog.Plus2 = ReadNum(o, "p2", 0);
                    prog.Plus3 = ReadNum(o, "p3", 0);
                }
            }
            return b;
        }

        static void Sanitize(SaveBlob b)
        {
            if (b == null) return;
            var n = b.PartyIds != null && b.PartyIds.Length > 0
                ? b.PartyIds.Length
                : (Catalog.DefaultParty != null ? Catalog.DefaultParty.Length : FightStats.DefaultPartyCap);
            if (n < 1) n = FightStats.DefaultPartyCap;
            var next = new string[n];
            for (int i = 0; i < n; i++)
            {
                var id = b.PartyIds != null && i < b.PartyIds.Length ? b.PartyIds[i] : null;
                if (string.IsNullOrEmpty(id) ||
                    (Catalog.Characters != null && Catalog.Characters.Count > 0 && !Catalog.Characters.ContainsKey(id)))
                {
                    if (Catalog.DefaultParty != null && i < Catalog.DefaultParty.Length)
                        id = Catalog.DefaultParty[i];
                    else if (Catalog.DefaultParty != null && Catalog.DefaultParty.Length > 0)
                        id = Catalog.DefaultParty[0];
                }
                next[i] = id;
            }
            b.PartyIds = next;
            b.LeaderSlot = b.ClampLeaderSlot(b.LeaderSlot);
            if (b.ClearedCount < 0) b.ClearedCount = 0;
            if (b.ClearedHard < 0) b.ClearedHard = 0;
            if (b.Speed < 1) b.Speed = 1;
            if (b.Speed > 2) b.Speed = 2;
            if (b.Gold < 0) b.Gold = 0;
            if (b.Stone < 0) b.Stone = 0;
            if (b.Meal < -1) b.Meal = -1;
            if (b.Meal > 2) b.Meal = 2;
            if (b.DailyClaimed < 0) b.DailyClaimed = 0;
            if (b.MailRead < 0) b.MailRead = 0;
            PuppetCatalog.Sanitize(b.Puppets);
            EnsureRoster(b);
            if (b.Units != null)
            {
                for (int i = 0; i < b.Units.Count; i++)
                    SanitizeUnit(b.Units[i]);
            }
        }

        static void ReadStringList(Dictionary<string, object> root, string key, List<string> dest)
        {
            if (dest == null || root == null || string.IsNullOrEmpty(key)) return;
            dest.Clear();
            if (!root.TryGetValue(key, out var raw) || !(raw is List<object> list)) return;
            for (int i = 0; i < list.Count; i++)
            {
                var id = Convert.ToString(list[i]);
                if (!string.IsNullOrEmpty(id)) dest.Add(id);
            }
        }

        static void SanitizeUnit(UnitProgress u)
        {
            if (u == null) return;
            if (u.Id == null) u.Id = "";
            if (u.Level < 1) u.Level = 1;
            if (u.Level > Growth.MaxLevel) u.Level = Growth.MaxLevel;
            if (u.Uncap < 0) u.Uncap = 0;
            if (u.Ignition < 0) u.Ignition = 0;
            u.IgnAtk = Ignition.Clamp(u.IgnAtk);
            u.IgnCrt = Ignition.Clamp(u.IgnCrt);
            u.IgnAgl = Ignition.Clamp(u.IgnAgl);
            if (u.Affection < 0) u.Affection = 0;
            if (u.Affection > 100) u.Affection = 100;
            u.Gear0 = KnownGear(u.Gear0);
            u.Gear1 = KnownGear(u.Gear1);
            u.Gear2 = KnownGear(u.Gear2);
            u.Gear3 = KnownGear(u.Gear3);
            u.Plus0 = Equipment.ClampPlus(u.Plus0);
            u.Plus1 = Equipment.ClampPlus(u.Plus1);
            u.Plus2 = Equipment.ClampPlus(u.Plus2);
            u.Plus3 = Equipment.ClampPlus(u.Plus3);
            u.Reserve = Growth.NormalizedReserve(u.Reserve);
            if (u.SkinId == null) u.SkinId = "";
        }

        static string KnownGear(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            return GearCatalog.Try(id) != null ? id : "";
        }

        static SaveBlob LegacyParse(string json)
        {
            var b = new SaveBlob();
            b.Vs1Cleared = json.IndexOf("\"cleared\":true", StringComparison.Ordinal) >= 0;
            b.AutoTap = json.IndexOf("\"auto\":true", StringComparison.Ordinal) >= 0;
            if (!b.AutoTap)
            {
                var n = ScanInt(json, "\"auto\":", -1);
                if (n >= 0 && n <= 2) b.Auto = (AutoMode)n;
            }
            b.LeaderSlot = ScanInt(json, "\"leader\":", 0);
            b.LastSeed = ScanInt(json, "\"seed\":", 0);
            b.Speed = Math.Max(1, ScanInt(json, "\"speed\":", 1));
            b.ClearedCount = ScanInt(json, "\"clearedN\":", b.ClearedCount);
            b.ClearedHard = ScanInt(json, "\"clearedH\":", 0);
            return b;
        }

        static AutoMode ReadAuto(Dictionary<string, object> root)
        {
            if (root == null || !root.TryGetValue("auto", out var a) || a == null) return AutoMode.Manual;
            if (a is bool on) return on ? AutoMode.Full : AutoMode.Manual;
            try
            {
                var n = Convert.ToInt32(a);
                if (n < 0) n = 0;
                if (n > 2) n = 2;
                return (AutoMode)n;
            }
            catch
            {
                return AutoMode.Manual;
            }
        }

        static int ReadNum(Dictionary<string, object> o, string k, int fallback)
        {
            if (o == null || !o.TryGetValue(k, out var v) || v == null) return fallback;
            try { return Convert.ToInt32(v); }
            catch { return fallback; }
        }

        static string Str(Dictionary<string, object> o, string k)
        {
            if (o == null || !o.TryGetValue(k, out var v) || v == null) return "";
            return Convert.ToString(v) ?? "";
        }

        static int ScanInt(string json, string key, int fallback)
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
            return any ? (neg ? -n : n) : fallback;
        }

        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
