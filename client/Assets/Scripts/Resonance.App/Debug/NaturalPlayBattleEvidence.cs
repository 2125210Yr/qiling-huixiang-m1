using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Resonance.Battle;

namespace Resonance.App
{
    /// <summary>
    /// L01/L02 per-fight capture. Header is frozen at battle start. Commands/events/result/digest
    /// are written before <c>GameRoot.Battle</c> is replaced by NEXT/retry. Pointer path is EventSystem only.
    /// </summary>
    public sealed class NaturalPlayBattleEvidence
    {
        public const string PointerPath = "UnityEngine.EventSystem";
        public const string HistoricalRun7Rel =
            "DC_RECON_KIT/m1/G2_REVIEW_20260913/artifacts/natural-play/run7";

        public string BattleId;
        public string ScenarioName;
        public string RegressionId;
        public string FrozenHeader;
        public string StageId;
        public int Seed;
        public int Seq;
        public bool Persisted;
        public string Dir;
        public string Digest;
        public string OutcomeAtPersist;
        public BattleSim Sim;

        public static string NewSessionId()
        {
            return "np-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
        }

        public static string MakeBattleId(string sessionId, int seq)
        {
            return (sessionId ?? "np") + "-" + seq.ToString("000");
        }

        public static NaturalPlayBattleEvidence Begin(BattleSim sim, string sessionId, int seq, VerificationScenario sc)
        {
            var ev = new NaturalPlayBattleEvidence
            {
                Sim = sim,
                Seq = seq,
                BattleId = MakeBattleId(sessionId, seq),
                ScenarioName = sc != null ? sc.Name : "",
                RegressionId = sc != null ? sc.RegressionId : "",
                StageId = sc != null ? sc.StageId() : ""
            };
            if (sim != null)
            {
                ev.Seed = sim.Seed;
                ev.FrozenHeader = sim.RunHeader() ?? "";
            }
            return ev;
        }

        public string Persist(string capturesRoot, BattleSim live)
        {
            var sim = live != null ? live : Sim;
            if (sim == null) return "no-sim";
            if (string.IsNullOrEmpty(capturesRoot)) return "no-dir";
            Dir = Path.Combine(capturesRoot, "natural-play", "battles", BattleId);
            Directory.CreateDirectory(Dir);
            var events = sim.Events != null ? sim.Events.ExportCanonical() ?? "" : "";
            var commands = FormatCommandLog(sim);
            OutcomeAtPersist = sim.Outcome.ToString();
            var result = BuildBattleResult(sim, events, commands);
            Digest = BattleEventLog.HashUtf8(FrozenHeader + "\n" + commands + "\n" + events + "\n" + OutcomeAtPersist);
            WriteUtf8(Path.Combine(Dir, "header.txt"), FrozenHeader ?? "");
            WriteUtf8(Path.Combine(Dir, "commands.txt"), commands);
            WriteUtf8(Path.Combine(Dir, "events.txt"), events);
            WriteUtf8(Path.Combine(Dir, "result.txt"), result);
            WriteUtf8(Path.Combine(Dir, "digest.txt"), Digest);
            WriteUtf8(Path.Combine(Dir, "scenario.txt"),
                "battle_id=" + BattleId + "\n"
                + "scenario=" + ScenarioName + "\n"
                + "regression=" + RegressionId + "\n"
                + "provenance=" + DesignPlaceholderPolicy.Provenance + "\n"
                + "schema=" + DesignPlaceholderPolicy.SchemaVersion + "\n"
                + "stage=" + StageId + "\n"
                + "seed=" + Seed + "\n"
                + "pointer=" + PointerPath + "\n"
                + "os_touch=NOT_CLAIMED\n");
            Persisted = true;
            return Dir;
        }

        public static void WriteSessionIndex(string capturesRoot, IList<NaturalPlayBattleEvidence> battles)
        {
            if (string.IsNullOrEmpty(capturesRoot)) return;
            var root = Path.Combine(capturesRoot, "natural-play");
            Directory.CreateDirectory(root);
            var sb = new StringBuilder(512);
            sb.AppendLine("# Per-battle event files. This index is not a single-fight log.");
            sb.AppendLine("# Do not treat natural-play.events.txt as battle-001 after NEXT.");
            sb.AppendLine("historical_baseline=" + HistoricalRun7Rel);
            sb.AppendLine("historical_verdict=FAIL BattlePlay missing=Tap,Slide");
            sb.AppendLine("historical_note=run7 kept as FAIL baseline; not rewritten to PASS");
            sb.AppendLine("pointer=" + PointerPath);
            sb.AppendLine("os_touch=NOT_CLAIMED");
            if (battles != null)
            {
                for (int i = 0; i < battles.Count; i++)
                {
                    var b = battles[i];
                    if (b == null) continue;
                    sb.Append("battle_id=").Append(b.BattleId)
                        .Append(" scenario=").Append(b.ScenarioName)
                        .Append(" persisted=").Append(b.Persisted ? "1" : "0")
                        .Append(" events=").Append(b.Dir != null ? Path.Combine(b.Dir, "events.txt") : "")
                        .Append('\n');
                }
            }
            WriteUtf8(Path.Combine(root, "BATTLE_INDEX.txt"), sb.ToString());
            WriteUtf8(Path.Combine(capturesRoot, "natural-play.events.txt"), sb.ToString());
        }

        public static string FormatCommandLog(BattleSim sim)
        {
            if (sim == null || sim.CommandLog == null || sim.CommandLog.Count == 0)
                return "(empty)";
            var sb = new StringBuilder(sim.CommandLog.Count * 48);
            for (int i = 0; i < sim.CommandLog.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(FormatCommand(sim.CommandLog[i]));
            }
            return sb.ToString();
        }

        public static string FormatCommand(CommandRecord rec)
        {
            if (rec == null) return "";
            return "seq=" + rec.Seq
                + " tick=" + rec.Tick
                + " kind=" + rec.Kind
                + " slot=" + rec.Slot
                + " source=" + rec.Source
                + " accepted=" + rec.Accepted
                + " reason=" + rec.Reason
                + " timing=" + rec.Timing
                + " value=" + rec.Value;
        }

        public static bool HasAccepted(BattleSim sim, BattleCommandKind kind, int slot)
        {
            return HasAccepted(sim, kind, slot, CommandSource.Player, false);
        }

        public static bool HasAccepted(BattleSim sim, BattleCommandKind kind, int slot, CommandSource source)
        {
            return HasAccepted(sim, kind, slot, source, true);
        }

        static bool HasAccepted(BattleSim sim, BattleCommandKind kind, int slot, CommandSource source, bool matchSource)
        {
            if (sim == null || sim.CommandLog == null) return false;
            for (int i = 0; i < sim.CommandLog.Count; i++)
            {
                var r = sim.CommandLog[i];
                if (r == null || !r.Accepted || r.Kind != kind) continue;
                if (slot >= 0 && r.Slot != slot) continue;
                if (matchSource && r.Source != source) continue;
                return true;
            }
            return false;
        }

        public static int DistinctAcceptedFeverSlots(BattleSim sim, List<int> into)
        {
            if (into != null) into.Clear();
            if (sim == null || sim.CommandLog == null) return 0;
            for (int i = 0; i < sim.CommandLog.Count; i++)
            {
                var r = sim.CommandLog[i];
                if (r == null || !r.Accepted || r.Kind != BattleCommandKind.FeverTap) continue;
                if (into != null && !into.Contains(r.Slot)) into.Add(r.Slot);
            }
            return into != null ? into.Count : 0;
        }

        public static bool HasAutoSkillSubmit(BattleSim sim)
        {
            if (sim == null || sim.CommandLog == null) return false;
            for (int i = 0; i < sim.CommandLog.Count; i++)
            {
                var r = sim.CommandLog[i];
                if (r == null || !r.Accepted || r.Source != CommandSource.Auto) continue;
                if (r.Kind == BattleCommandKind.Tap || r.Kind == BattleCommandKind.Slide
                    || r.Kind == BattleCommandKind.DriveBegin || r.Kind == BattleCommandKind.DriveResolve
                    || r.Kind == BattleCommandKind.FeverTap)
                    return true;
            }
            return false;
        }

        public static int AllyCastsSince(BattleSim sim, int eventFrom)
        {
            if (sim == null || sim.Events == null || sim.Events.Events == null) return 0;
            var list = sim.Events.Events;
            var n = 0;
            for (int i = eventFrom; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.Kind != "cast" || !e.CasterAlly) continue;
                if (e.Channel == SkillType.Auto || e.Channel == SkillType.Tap
                    || e.Channel == SkillType.Slide || e.Channel == SkillType.Drive
                    || e.Channel == SkillType.Fever)
                    n++;
            }
            return n;
        }

        string BuildBattleResult(BattleSim sim, string events, string commands)
        {
            var sb = new StringBuilder(1024);
            sb.AppendLine("battle_id=" + BattleId);
            sb.AppendLine("scenario=" + ScenarioName);
            sb.AppendLine("regression=" + RegressionId);
            sb.AppendLine("provenance=" + DesignPlaceholderPolicy.Provenance);
            sb.AppendLine("schema=" + DesignPlaceholderPolicy.SchemaVersion);
            sb.AppendLine("stage=" + StageId);
            sb.AppendLine("seed=" + Seed);
            sb.AppendLine("outcome=" + OutcomeAtPersist);
            sb.AppendLine("drive=" + (sim != null ? sim.Drive.ToString("0.#") : ""));
            sb.AppendLine("fever=" + (sim != null && sim.FeverActive));
            sb.AppendLine("feverEver=" + (sim != null && sim.FeverEver));
            sb.AppendLine("auto=" + (sim != null ? sim.Auto.ToString() : ""));
            sb.AppendLine("speed=" + (sim != null ? sim.Speed.ToString() : ""));
            sb.AppendLine("wave=" + (sim != null ? sim.WaveIndex.ToString() : ""));
            sb.AppendLine("event_count=" + (sim != null && sim.Events != null && sim.Events.Events != null
                ? sim.Events.Events.Count : 0));
            sb.AppendLine("command_count=" + (sim != null && sim.CommandLog != null ? sim.CommandLog.Count : 0));
            sb.AppendLine("pointer=" + PointerPath);
            sb.AppendLine("os_touch=NOT_CLAIMED");
            sb.AppendLine();
            sb.AppendLine("--- frozen RunHeader ---");
            sb.AppendLine(string.IsNullOrEmpty(FrozenHeader) ? "(empty)" : FrozenHeader);
            sb.AppendLine();
            sb.AppendLine("--- CommandLog ---");
            sb.AppendLine(commands ?? "");
            sb.AppendLine();
            sb.AppendLine("--- Events ---");
            sb.AppendLine(events ?? "");
            return sb.ToString();
        }

        static void WriteUtf8(string path, string text)
        {
            File.WriteAllText(path, text ?? "", new UTF8Encoding(false));
        }
    }
}
