using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Resonance.Battle
{
    public sealed class BattleEvent
    {
        public int Tick;
        public string Phase;
        public int Seq;
        public string Kind;
        public string Opcode;
        public int CasterSlot;
        public bool CasterAlly;
        public int TargetSlot;
        public bool TargetAlly;
        public int Amount;
        public SkillType Channel;
        public FormulaProfile Profile;
        public FormulaStatus FormulaStatus;
        /// <summary>R06: evidence class of the numeric branch (never Measured for current branches).</summary>
        public FormulaEvidence Evidence;

        public string Canonical()
        {
            return Tick + "|" + (Phase ?? "") + "|" + Seq + "|" + (Kind ?? "") + "|" + (Opcode ?? "")
                + "|" + CasterSlot + "|" + (CasterAlly ? "1" : "0")
                + "|" + TargetSlot + "|" + (TargetAlly ? "1" : "0")
                + "|" + Amount + "|" + (int)Channel
                + "|" + (int)Profile + "|" + (int)FormulaStatus + "|" + (int)Evidence;
        }
    }

    public sealed class BattleEventLog
    {
        public readonly List<BattleEvent> Events = new List<BattleEvent>(256);
        int _seq;

        public BattleEvent Add(
            int tick,
            string phase,
            string kind,
            string opcode,
            UnitState caster,
            UnitState target,
            int amount,
            SkillType channel,
            FormulaProfile profile,
            FormulaStatus status)
        {
            var ev = new BattleEvent
            {
                Tick = tick,
                Phase = phase ?? "",
                Seq = _seq++,
                Kind = kind ?? "",
                Opcode = opcode ?? "",
                CasterSlot = caster != null ? caster.Slot : -1,
                CasterAlly = caster != null && caster.Ally,
                TargetSlot = target != null ? target.Slot : -1,
                TargetAlly = target != null && target.Ally,
                Amount = amount,
                Channel = channel,
                Profile = profile,
                FormulaStatus = status,
                Evidence = DamageMath.EvidenceOf(profile, channel)
            };
            Events.Add(ev);
            return ev;
        }

        public string ExportCanonical()
        {
            var sb = new StringBuilder(Events.Count * 48);
            for (int i = 0; i < Events.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(Events[i].Canonical());
            }
            return sb.ToString();
        }

        public string ComputeHash()
        {
            return HashUtf8(ExportCanonical());
        }

        public static string HashUtf8(string text)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
                var hex = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                    hex.Append(bytes[i].ToString("x2"));
                return hex.ToString();
            }
        }

        public void ExportTo(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var bytes = Encoding.UTF8.GetBytes(ExportCanonical());
            stream.Write(bytes, 0, bytes.Length);
        }

        public void ExportToFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Export path is required.", nameof(path));
            if (IsUserSaveLocation(path))
                throw new InvalidOperationException("Battle event log must not write the user save.");
            File.WriteAllText(Path.GetFullPath(path), ExportCanonical(), new UTF8Encoding(false));
        }

        static bool IsUserSaveLocation(string path)
        {
            string full;
            string save;
            try
            {
                full = Path.GetFullPath(path);
                save = Path.GetFullPath(SaveStore.DefaultPath);
            }
            catch
            {
                return false;
            }
            if (string.Equals(full, save, StringComparison.OrdinalIgnoreCase))
                return true;
            var saveDir = Path.GetDirectoryName(save);
            var destDir = Path.GetDirectoryName(full);
            if (string.IsNullOrEmpty(saveDir) || string.IsNullOrEmpty(destDir))
                return false;
            return string.Equals(Path.GetFullPath(saveDir), Path.GetFullPath(destDir), StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class BattleHudState
    {
        public const string BattleIdle = "BattleIdle";
        public const string ChargingReady = "ChargingReady";
        public const string Tap = "Tap";
        public const string Slide = "Slide";
        public const string DriveSelect = "DriveSelect";
        public const string Qte = "Qte";
        public const string Fever = "Fever";
        public const string Controlled = "Controlled";
        public const string DeathOrWave = "DeathOrWave";
        public const string Result = "Result";

        public static void Collect(BattleSim sim, int slot, List<string> dest)
        {
            if (dest == null) return;
            dest.Clear();
            if (sim == null)
            {
                dest.Add(BattleIdle);
                return;
            }
            if (sim.Outcome != BattleOutcome.InProgress)
            {
                dest.Add(Result);
                return;
            }
            if (sim.PendingDriveSlot >= 0)
            {
                dest.Add(sim.PendingDriveSlot == slot ? Qte : DriveSelect);
            }
            if (sim.FeverActive) dest.Add(Fever);
            var u = sim.Allies != null && slot >= 0 && slot < sim.Allies.Length ? sim.Allies[slot] : null;
            if (u == null || !u.Alive)
            {
                dest.Add(DeathOrWave);
                return;
            }
            if (u.ActionLocked) dest.Add(Controlled);
            if (u.SlideCd > 0f) dest.Add("SlideCd");
            if (u.Charge >= 100f)
            {
                dest.Add(ChargingReady);
                dest.Add(Tap);
                if (u.SlideCd <= 0f) dest.Add(Slide);
            }
            if (dest.Count == 0) dest.Add(BattleIdle);
        }

        public static string Primary(BattleSim sim, int slot)
        {
            var buf = new List<string>(8);
            Collect(sim, slot, buf);
            return buf.Count > 0 ? buf[0] : BattleIdle;
        }
    }
}
