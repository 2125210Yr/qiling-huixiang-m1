using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Battle
{
    public enum CommandSource { Player, Auto, Replay, Fixture }

    public enum BattleCommandKind
    {
        Tap, Slide, DriveBegin, DriveResolve, FeverTap, FocusEnemy, Pause, Resume, SetSpeed, SetAuto
    }

    public enum CommandReject
    {
        None, NotInProgress, Paused, QtePending, NoQtePending, SlotInvalid, UnitDead,
        ActionLocked, Silenced, NotCharged, SlideOnCooldown, DriveNotReady, FeverNotActive, FeverThrottled,
        FeverBudgetExhausted, AutoOwnsInput, InvalidValue
    }

    public struct BattleCommand
    {
        public BattleCommandKind Kind;
        public int Slot;
        public DriveTiming Timing;
        public int Value;
        public CommandSource Source;

        public static BattleCommand Tap(int slot, CommandSource src = CommandSource.Player)
            => new BattleCommand { Kind = BattleCommandKind.Tap, Slot = slot, Source = src };
        public static BattleCommand Slide(int slot, CommandSource src = CommandSource.Player)
            => new BattleCommand { Kind = BattleCommandKind.Slide, Slot = slot, Source = src };
        public static BattleCommand DriveBegin(int slot, CommandSource src = CommandSource.Player)
            => new BattleCommand { Kind = BattleCommandKind.DriveBegin, Slot = slot, Source = src };
        public static BattleCommand DriveResolve(DriveTiming timing, CommandSource src = CommandSource.Player)
            => new BattleCommand { Kind = BattleCommandKind.DriveResolve, Timing = timing, Source = src };
        public static BattleCommand FeverTap(int slot, CommandSource src = CommandSource.Player)
            => new BattleCommand { Kind = BattleCommandKind.FeverTap, Slot = slot, Source = src };
    }

    public struct CommandResult
    {
        public bool Accepted;
        public CommandReject Reason;
        public int Seq;
        public int Tick;
    }

    public sealed class CommandRecord
    {
        public int Seq;
        public int Tick;
        public BattleCommandKind Kind;
        public int Slot;
        public DriveTiming Timing;
        public int Value;
        public CommandSource Source;
        public bool Accepted;
        public CommandReject Reason;

        public override string ToString()
        {
            return "#" + Seq + " t" + Tick + " " + Kind + " slot=" + Slot + " timing=" + Timing + " v=" + Value
                + " src=" + Source + (Accepted ? " OK" : " REJECT:" + Reason);
        }
    }

    /// <summary>
    /// R07 / §4.D: the single input entry. Player gestures, auto policies, replay and fixtures all submit
    /// commands here; every submission (accepted or not) is appended to <see cref="CommandLog"/>.
    /// </summary>
    public sealed partial class BattleSim
    {
        public readonly List<CommandRecord> CommandLog = new List<CommandRecord>(256);
        int _cmdSeq;

        public string RunHeader()
        {
            var sb = new StringBuilder(160);
            sb.Append("seed=").Append(Seed)
              .Append(";rules=").Append(RulesVersion)
              .Append(";profile=").Append(Profile)
              .Append(";auto=").Append(Auto)
              .Append(";speed=").Append(Speed)
              .Append(";forceNoCrit=").Append(ForceNoCrit ? 1 : 0)
              .Append(";stage=").Append(_stage != null ? _stage.Id : "")
              .Append(";data=").Append(ContentFingerprint())
              .Append(";party=");
            for (int i = 0; i < Allies.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Allies[i] != null && Allies[i].Def != null ? Allies[i].Def.Id : "-");
            }
            return sb.ToString();
        }

        /// <summary>Cheap content identity for logs: counts + stable hash of skill/effect ids and numeric fields.</summary>
        public static string ContentFingerprint()
        {
            var chars = Catalog.Characters;
            var skills = Catalog.Skills;
            var effects = Catalog.Effects;
            unchecked
            {
                int h = 17;
                if (skills != null)
                    foreach (var kv in skills)
                    {
                        var s = kv.Value;
                        h = h * 31 + Fnv(kv.Key);
                        if (s == null) continue;
                        h = h * 31 + Fnv(s.Opcode);
                        h = h * 31 + s.AtkCoef.GetHashCode();
                        h = h * 31 + s.HitCount;
                        h = h * 31 + s.DriveGain;
                    }
                if (effects != null)
                    foreach (var kv in effects)
                    {
                        var e = kv.Value;
                        h = h * 31 + Fnv(kv.Key);
                        if (e == null) continue;
                        h = h * 31 + (int)e.Kind;
                        h = h * 31 + e.Magnitude.GetHashCode();
                        h = h * 31 + e.DurationSec.GetHashCode();
                    }
                return "builtin-c" + (chars != null ? chars.Count : 0) + "-s" + (skills != null ? skills.Count : 0)
                    + "-e" + (effects != null ? effects.Count : 0) + "-" + (h & 0x7fffffff).ToString("x8");
            }
        }

        /// <summary>Process-stable string hash (string.GetHashCode is randomized per process on .NET Core).</summary>
        static int Fnv(string s)
        {
            unchecked
            {
                int h = (int)2166136261;
                if (s == null) return h;
                for (int i = 0; i < s.Length; i++) h = (h ^ s[i]) * 16777619;
                return h;
            }
        }

        public CommandResult Submit(BattleCommand cmd)
        {
            var reason = Execute(cmd);
            var rec = new CommandRecord
            {
                Seq = ++_cmdSeq,
                Tick = TickIndex,
                Kind = cmd.Kind,
                Slot = cmd.Slot,
                Timing = cmd.Timing,
                Value = cmd.Value,
                Source = cmd.Source,
                Accepted = reason == CommandReject.None,
                Reason = reason
            };
            CommandLog.Add(rec);
            return new CommandResult { Accepted = rec.Accepted, Reason = reason, Seq = rec.Seq, Tick = rec.Tick };
        }

        CommandReject Execute(BattleCommand cmd)
        {
            var kind = cmd.Kind;
            var isCombatInput = kind == BattleCommandKind.Tap || kind == BattleCommandKind.Slide
                || kind == BattleCommandKind.DriveBegin || kind == BattleCommandKind.DriveResolve
                || kind == BattleCommandKind.FeverTap;
            if (isCombatInput && cmd.Source == CommandSource.Player && Auto == AutoMode.Full)
                return CommandReject.AutoOwnsInput;

            switch (kind)
            {
                case BattleCommandKind.Tap:
                {
                    if (!CanAcceptSkillInput(cmd.Slot, out var r)) return r;
                    if (Allies[cmd.Slot].Charge < 100f) return CommandReject.NotCharged;
                    return TryTap(cmd.Slot) ? CommandReject.None : CommandReject.InvalidValue;
                }
                case BattleCommandKind.Slide:
                {
                    if (!CanAcceptSkillInput(cmd.Slot, out var r)) return r;
                    if (Allies[cmd.Slot].Charge < 100f) return CommandReject.NotCharged;
                    if (Allies[cmd.Slot].SlideCd > 0f) return CommandReject.SlideOnCooldown;
                    return TrySlide(cmd.Slot) ? CommandReject.None : CommandReject.InvalidValue;
                }
                case BattleCommandKind.DriveBegin:
                {
                    if (!CanAcceptSkillInput(cmd.Slot, out var r)) return r;
                    if (Drive < 100f) return CommandReject.DriveNotReady;
                    return TryBeginDrive(cmd.Slot) ? CommandReject.None : CommandReject.InvalidValue;
                }
                case BattleCommandKind.DriveResolve:
                {
                    switch (ResolveDriveChecked(cmd.Timing))
                    {
                        case DriveResolveResult.Accepted: return CommandReject.None;
                        case DriveResolveResult.NoPending: return CommandReject.NoQtePending;
                        case DriveResolveResult.Paused: return CommandReject.Paused;
                        default: return CommandReject.NotInProgress;
                    }
                }
                case BattleCommandKind.FeverTap:
                {
                    TryFeverTap(cmd.Slot, out var r);
                    return r;
                }
                case BattleCommandKind.FocusEnemy:
                {
                    if (Outcome != BattleOutcome.InProgress) return CommandReject.NotInProgress;
                    return TryFocusEnemy(cmd.Slot) ? CommandReject.None : CommandReject.SlotInvalid;
                }
                case BattleCommandKind.Pause:
                {
                    if (Outcome != BattleOutcome.InProgress) return CommandReject.NotInProgress;
                    if (Paused) return CommandReject.Paused;
                    Paused = true;
                    return CommandReject.None;
                }
                case BattleCommandKind.Resume:
                {
                    if (Outcome != BattleOutcome.InProgress) return CommandReject.NotInProgress;
                    if (!Paused) return CommandReject.InvalidValue;
                    Paused = false;
                    return CommandReject.None;
                }
                case BattleCommandKind.SetSpeed:
                {
                    if (cmd.Value < 1 || cmd.Value > 3) return CommandReject.InvalidValue;
                    Speed = cmd.Value;
                    return CommandReject.None;
                }
                case BattleCommandKind.SetAuto:
                {
                    if (cmd.Value < 0 || cmd.Value > 2) return CommandReject.InvalidValue;
                    Auto = (AutoMode)cmd.Value;
                    return CommandReject.None;
                }
                default:
                    return CommandReject.InvalidValue;
            }
        }
    }
}
