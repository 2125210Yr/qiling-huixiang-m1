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
        FeverBudgetExhausted, AutoOwnsInput, InvalidValue,
        Unplayable, ModeDisabled, TargetInvalid
    }

    public struct BattleCommand
    {
        public BattleCommandKind Kind;
        public int Slot;
        public DriveTiming Timing;
        public int Value;
        public CommandSource Source;
        // A zero generation leaves historical commands unbound. Queued original skills bind an instance.
        public int RequiredEnemySlot;
        public int RequiredEnemyGeneration;

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
        public static BattleCommand FocusEnemy(int slot, CommandSource src = CommandSource.Player)
            => new BattleCommand { Kind = BattleCommandKind.FocusEnemy, Slot = slot, Source = src };
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
        /// <summary>Post-result clock position before this input; null for battle-time input.</summary>
        public int? TerminalFeverTick;
        public BattleCommandKind Kind;
        public int Slot;
        public DriveTiming Timing;
        public int Value;
        public CommandSource Source;
        public int RequiredEnemySlot;
        public int RequiredEnemyGeneration;
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
        /// <summary>REGRESSIONS.md R02. Set by <see cref="FreezeInitialHeader"/> after Speed/Auto/Profile/Clocks.</summary>
        public BattleInitialHeader InitialHeader { get; set; }
        int _cmdSeq;

        public BattleInitialHeader FreezeInitialHeader(string[] partyIds = null, string stageId = null)
        {
            InitialHeader = BattleInitialHeader.Freeze(this, partyIds, stageId);
            return InitialHeader;
        }

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

        /// <summary>REGRESSIONS.md R04 — full catalog identity (HP/ATK, stage, FlatPower/Target, Trigger/PeriodSec).</summary>
        public static string ContentFingerprint()
        {
            return "g2id1-" + BattleContentIdentity.Fingerprint();
        }

        public CommandResult Submit(BattleCommand cmd)
        {
            int? terminalFeverTick = Outcome != BattleOutcome.InProgress ? TerminalFeverTicks : (int?)null;
            var reason = Execute(cmd);
            var rec = new CommandRecord
            {
                Seq = ++_cmdSeq,
                Tick = TickIndex,
                TerminalFeverTick = terminalFeverTick,
                Kind = cmd.Kind,
                Slot = cmd.Slot,
                Timing = cmd.Timing,
                Value = cmd.Value,
                Source = cmd.Source,
                RequiredEnemySlot = cmd.RequiredEnemySlot,
                RequiredEnemyGeneration = cmd.RequiredEnemyGeneration,
                Accepted = reason == CommandReject.None,
                Reason = reason
            };
            CommandLog.Add(rec);
            if (IsOriginalExpedition && rec.Accepted && cmd.Kind == BattleCommandKind.Resume)
                DrainExpeditionQueueAfterResume();
            return new CommandResult { Accepted = rec.Accepted, Reason = reason, Seq = rec.Seq, Tick = rec.Tick };
        }

        CommandReject Execute(BattleCommand cmd)
        {
            var kind = cmd.Kind;
            if (IsOriginalExpedition && (kind == BattleCommandKind.DriveBegin || kind == BattleCommandKind.DriveResolve || kind == BattleCommandKind.FeverTap))
                return CommandReject.ModeDisabled;
            var isCombatInput = kind == BattleCommandKind.Tap || kind == BattleCommandKind.Slide
                || kind == BattleCommandKind.DriveBegin || kind == BattleCommandKind.DriveResolve
                || kind == BattleCommandKind.FeverTap;
            if (isCombatInput && cmd.Source == CommandSource.Player && Auto == AutoMode.Full && !IsOriginalExpedition)
                return CommandReject.AutoOwnsInput;

            switch (kind)
            {
                case BattleCommandKind.Tap:
                {
                    if (!CanAcceptSkillInput(cmd.Slot, out var r)) return r;
                    var targetReason = ValidateExpeditionTarget(cmd);
                    if (targetReason != CommandReject.None) return targetReason;
                    if (Allies[cmd.Slot].Charge < 100f) return CommandReject.NotCharged;
                    var tapId = Allies[cmd.Slot].Def != null ? Allies[cmd.Slot].Def.TapSkillId : null;
                    var tap = ResolveSkill(tapId);
                    if (!FightSkillReady(tap)) return CommandReject.Unplayable;
                    return RunWithExpeditionTarget(cmd, () => TryTap(cmd.Slot) ? CommandReject.None : CommandReject.InvalidValue);
                }
                case BattleCommandKind.Slide:
                {
                    if (!CanAcceptSkillInput(cmd.Slot, out var r)) return r;
                    var targetReason = ValidateExpeditionTarget(cmd);
                    if (targetReason != CommandReject.None) return targetReason;
                    if (Allies[cmd.Slot].Charge < 100f) return CommandReject.NotCharged;
                    if (Allies[cmd.Slot].SlideCd > 0f) return CommandReject.SlideOnCooldown;
                    var slideId = Allies[cmd.Slot].Def != null ? Allies[cmd.Slot].Def.SlideSkillId : null;
                    var slide = ResolveSkill(slideId);
                    if (!FightSkillReady(slide)) return CommandReject.Unplayable;
                    return RunWithExpeditionTarget(cmd, () => TrySlide(cmd.Slot) ? CommandReject.None : CommandReject.InvalidValue);
                }
                case BattleCommandKind.DriveBegin:
                {
                    if (!CanAcceptSkillInput(cmd.Slot, out var r)) return r;
                    if (Drive < 100f) return CommandReject.DriveNotReady;
                    var driveId = Allies[cmd.Slot].Def != null ? Allies[cmd.Slot].Def.DriveSkillId : null;
                    var drive = ResolveSkill(driveId);
                    if (!FightSkillReady(drive)) return CommandReject.Unplayable;
                    return TryBeginDrive(cmd.Slot) ? CommandReject.None : CommandReject.InvalidValue;
                }
                case BattleCommandKind.DriveResolve:
                {
                    _requestDriveHold = cmd.Source == CommandSource.Auto;
                    switch (ResolveDriveChecked(cmd.Timing))
                    {
                        case DriveResolveResult.Accepted: return CommandReject.None;
                        case DriveResolveResult.NoPending: return CommandReject.NoQtePending;
                        case DriveResolveResult.Paused: return CommandReject.Paused;
                        case DriveResolveResult.Unplayable: return CommandReject.Unplayable;
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
                    if (cmd.Value < 1 || cmd.Value > (IsOriginalExpedition ? 2 : 3)) return CommandReject.InvalidValue;
                    Speed = cmd.Value;
                    return CommandReject.None;
                }
                case BattleCommandKind.SetAuto:
                {
                    if (IsOriginalExpedition && cmd.Value != (int)AutoMode.Manual) return CommandReject.ModeDisabled;
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
