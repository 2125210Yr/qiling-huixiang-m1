using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Resonance.Battle;

namespace Resonance.App
{
    /// <summary>
    /// L01/L02 per-fight capture on the existing folder store (not a second archive).
    /// <see cref="Begin"/> freezes <see cref="BattleInitialHeader"/> after Speed/Auto/Profile
    /// are already set, plus the opening <see cref="FrozenHeader"/> text.
    /// <see cref="Persist"/> writes the usual txt files and a standard
    /// <see cref="BattleRunRecord"/> JSON-lines tape the replayer already parses.
    /// Pointer path is EventSystem only.
    /// </summary>
    public sealed class NaturalPlayBattleEvidence
    {
        public const string PointerPath = "UnityEngine.EventSystem";
        public const string HistoricalRun7Rel =
            "DC_RECON_KIT/m1/G2_REVIEW_20260913/artifacts/natural-play/run7";
        public const string ReplayFileName = "replay.jsonl";
        public const string RecordFileName = "record.jsonl";
        public const string RefuseNoSim = "no-sim";
        public const string RefuseNoDir = "no-dir";
        public const string RefuseSimMismatch = "sim-mismatch";
        public const string RefuseBattleIdMismatch = "battle-id-mismatch";

        public string BattleId;
        public string ScenarioName;
        public string RegressionId;
        public string FrozenHeader;
        public BattleInitialHeader FrozenInitial;
        public string StageId;
        public int Seed;
        public int Seq;
        public bool Persisted;
        public string Dir;
        public string Digest;
        public string OutcomeAtPersist;
        public string PolicyIdentity;
        public string LastPersistStatus;
        public BattleRunRecord Record;
        public BattleSim Sim;

        BattleSim _boundSim;
        string _boundBattleId;

        public static string NewSessionId()
        {
            return "np-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
        }

        public static string MakeBattleId(string sessionId, int seq)
        {
            return (sessionId ?? "np") + "-" + seq.ToString("000");
        }

        /// <summary>
        /// Bind one fight. Caller must have set Speed/Auto/Profile (GameRoot does this,
        /// then <see cref="BattleInitialHeader.Freeze"/>). First Freeze wins.
        /// </summary>
        public static NaturalPlayBattleEvidence Begin(BattleSim sim, string sessionId, int seq, VerificationScenario sc)
        {
            var ev = new NaturalPlayBattleEvidence
            {
                Sim = sim,
                _boundSim = sim,
                Seq = seq,
                BattleId = MakeBattleId(sessionId, seq),
                ScenarioName = sc != null ? sc.Name : "",
                RegressionId = sc != null ? sc.RegressionId : "",
                StageId = sc != null ? sc.StageId() : "",
                PolicyIdentity = CurrentPolicyIdentity()
            };
            ev._boundBattleId = ev.BattleId;
            if (sim != null)
            {
                ev.Seed = sim.Seed;
                var party = PartyIdsFrom(sim);
                var stage = FirstNonEmpty(
                    ev.StageId,
                    sim.InitialHeader != null ? sim.InitialHeader.StageId : null);
                ev.FrozenInitial = BattleInitialHeader.Copy(sim.FreezeInitialHeader(party, stage));
                ev.FrozenHeader = sim.RunHeader() ?? "";
                if (ev.FrozenInitial != null && string.IsNullOrEmpty(ev.StageId))
                    ev.StageId = ev.FrozenInitial.StageId ?? "";
            }
            return ev;
        }

        /// <summary>
        /// Write this fight's folder. Refuses when <paramref name="live"/> is a different
        /// <see cref="BattleSim"/> than the bound instance, or when <see cref="BattleId"/>
        /// was mutated away from the Begin id. <paramref name="live"/> null flushes the
        /// bound sim (battle already torn down). Does not write fight-2 into fight-1.
        /// </summary>
        public string Persist(string capturesRoot, BattleSim live)
        {
            LastPersistStatus = CheckPersist(live);
            if (LastPersistStatus != null) return LastPersistStatus;
            var sim = live != null ? live : BoundSim();
            if (sim == null)
            {
                LastPersistStatus = RefuseNoSim;
                return LastPersistStatus;
            }
            if (string.IsNullOrEmpty(capturesRoot))
            {
                LastPersistStatus = RefuseNoDir;
                return LastPersistStatus;
            }

            Dir = Path.Combine(capturesRoot, "natural-play", "battles", BattleId);
            if (FolderBattleIdConflicts(Dir, BattleId))
            {
                LastPersistStatus = RefuseBattleIdMismatch;
                return LastPersistStatus;
            }

            Directory.CreateDirectory(Dir);
            var party = FrozenInitial != null ? FrozenInitial.PartyIds : PartyIdsFrom(sim);
            var stage = FirstNonEmpty(
                FrozenInitial != null ? FrozenInitial.StageId : null,
                StageId);
            Record = BattleRunRecord.Capture(sim, party, stage);
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
            WriteUtf8(Path.Combine(Dir, "scenario.txt"), BuildScenarioText());
            WriteUtf8(Path.Combine(Dir, ReplayFileName), Record.ToJsonLines());
            Persisted = true;
            LastPersistStatus = Dir;
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
                        .Append(" replay=").Append(b.Dir != null ? Path.Combine(b.Dir, ReplayFileName) : "")
                        .Append('\n');
                }
            }
            WriteUtf8(Path.Combine(root, "BATTLE_INDEX.txt"), sb.ToString());
            WriteUtf8(Path.Combine(capturesRoot, "natural-play.events.txt"), sb.ToString());
        }

        /// <summary>
        /// Rebuild metadata + <see cref="BattleRunRecord"/> from one fight folder.
        /// Not a same-object compare: a new sim can <see cref="BattleReplayer.Verify"/>.
        /// </summary>
        public static NaturalPlayBattleEvidence Load(string dir)
        {
            if (string.IsNullOrEmpty(dir)) throw new ArgumentException("dir");
            if (!Directory.Exists(dir)) throw new DirectoryNotFoundException(dir);
            var ev = new NaturalPlayBattleEvidence
            {
                Dir = dir,
                Persisted = true
            };
            ApplyScenarioFile(ev, Path.Combine(dir, "scenario.txt"));
            ev.FrozenHeader = ReadUtf8(Path.Combine(dir, "header.txt"));
            ev.Digest = FirstLine(ReadUtf8(Path.Combine(dir, "digest.txt")));
            BattleRunRecord rec;
            if (TryReadRecord(dir, out rec) && rec != null)
            {
                ev.Record = rec;
                ev.FrozenInitial = rec.Initial != null ? BattleInitialHeader.Copy(rec.Initial) : ev.FrozenInitial;
                if (string.IsNullOrEmpty(ev.FrozenHeader)) ev.FrozenHeader = rec.RunHeader ?? "";
                if (ev.Seed == 0) ev.Seed = rec.Seed;
                if (string.IsNullOrEmpty(ev.StageId)) ev.StageId = rec.StageId ?? "";
            }
            if (string.IsNullOrEmpty(ev.BattleId))
                ev.BattleId = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            ev._boundBattleId = ev.BattleId;
            ev.LastPersistStatus = dir;
            return ev;
        }

        public static bool TryLoad(string dir, out NaturalPlayBattleEvidence ev)
        {
            ev = null;
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
            ev = Load(dir);
            return ev != null;
        }

        /// <summary>
        /// Parse the standard replay tape (<c>replay.jsonl</c>, else <c>record.jsonl</c>)
        /// with <see cref="BattleRunRecord.ParseJsonLines"/>.
        /// </summary>
        public static bool TryReadRecord(string dir, out BattleRunRecord rec)
        {
            rec = null;
            if (string.IsNullOrEmpty(dir)) return false;
            var path = FindReplayPath(dir);
            if (path == null) return false;
            rec = BattleRunRecord.ParseJsonLines(ReadUtf8(path) ?? "");
            return rec != null;
        }

        /// <summary>
        /// New sim from the tape's opening party/stage/profile/leader/seed.
        /// Growth/gear are Catalog defaults unless <paramref name="growth"/> is supplied.
        /// Bind the recorded policy before calling if the fight used a verification catalog.
        /// Mismatched growth/gear/policy/data identity fails
        /// <see cref="BattleReplayer.Verify"/> with a named Diff (not a same-object compare).
        /// </summary>
        public static BattleSim NewSim(BattleRunRecord rec, UnitProgress[] growth = null)
        {
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            var stage = BattleContentIdentity.FindStage(rec.StageId);
            var sim = new BattleSim(rec.PartyIds, rec.LeaderSlot, rec.Seed, stage, growth)
            {
                Profile = rec.Profile
            };
            if (rec.Initial != null)
                sim.ForceNoCrit = rec.Initial.ForceNoCrit;
            sim.FreezeInitialHeader(rec.PartyIds, rec.StageId ?? "");
            return sim;
        }

        public static Func<int, BattleSim> ReplayFactory(BattleRunRecord rec, UnitProgress[] growth = null)
        {
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            return seed =>
            {
                var stage = BattleContentIdentity.FindStage(rec.StageId);
                var sim = new BattleSim(rec.PartyIds, rec.LeaderSlot, seed, stage, growth)
                {
                    Profile = rec.Profile
                };
                if (rec.Initial != null)
                    sim.ForceNoCrit = rec.Initial.ForceNoCrit;
                return sim;
            };
        }

        /// <summary>
        /// Bind <see cref="DesignPlaceholderPolicy"/> from the recorded scenario name
        /// so a rebuilt sim sees the same honor/charge policy, or no-op if unnamed.
        /// </summary>
        public static void BindOpeningPolicy(NaturalPlayBattleEvidence ev)
        {
            var name = ev != null ? ev.ScenarioName : null;
            if (string.IsNullOrEmpty(name))
            {
                DesignPlaceholderPolicy.Bind(null);
                return;
            }
            DesignPlaceholderPolicy.Bind(VerificationScenario.Named(name));
        }

        /// <summary>
        /// Named opening-identity Diff for growth/gear/clocks/data/rules.
        /// Null means the rebuilt sim is enough to Verify; otherwise the token is
        /// the same family <see cref="BattleReplayer.Verify"/> reports.
        /// </summary>
        public static string NamedOpeningDiff(BattleRunRecord tape, BattleSim rebuilt)
        {
            if (tape == null) return "Record: missing";
            if (rebuilt == null) return "Sim: missing";
            if (!string.IsNullOrEmpty(tape.RulesVersion)
                && !string.Equals(tape.RulesVersion, BattleSim.RulesVersion, StringComparison.Ordinal))
                return "RulesVersion: " + tape.RulesVersion + " != " + BattleSim.RulesVersion;

            var expectedId = tape.DataIdentity;
            if (string.IsNullOrEmpty(expectedId) && tape.Initial != null)
                expectedId = tape.Initial.DataIdentity;
            if (!string.IsNullOrEmpty(expectedId))
            {
                var actual = BattleContentIdentity.Compute(rebuilt, tape.PartyIds, tape.StageId ?? "");
                if (!string.Equals(expectedId, actual, StringComparison.Ordinal))
                    return "DataIdentity: tape != replayed";
            }

            if (tape.Initial != null && !string.IsNullOrEmpty(tape.Initial.GrowthIdentity))
            {
                var snap = BattleInitialHeader.Snapshot(rebuilt, tape.PartyIds, tape.StageId, true);
                if (!string.Equals(tape.Initial.GrowthIdentity ?? "", snap.GrowthIdentity ?? "", StringComparison.Ordinal))
                    return "GrowthIdentity: tape != replayed";
            }

            if (tape.Initial != null && !string.IsNullOrEmpty(tape.Initial.ClockIdentity))
            {
                var clock = BattleContentIdentity.ClockKey(rebuilt.Clocks);
                if (!string.Equals(tape.Initial.ClockIdentity, clock, StringComparison.Ordinal))
                    return "ClockIdentity: tape != replayed";
            }

            if (tape.Initial != null && tape.Initial.ForceNoCrit != rebuilt.ForceNoCrit)
            {
                return "ForceNoCrit: "
                    + (tape.Initial.ForceNoCrit ? "1" : "0")
                    + " != "
                    + (rebuilt.ForceNoCrit ? "1" : "0");
            }
            return null;
        }

        public static string CurrentPolicyIdentity()
        {
            return "schema=" + DesignPlaceholderPolicy.SchemaVersion
                + ";name=" + (DesignPlaceholderPolicy.ScenarioName ?? "")
                + ";active=" + (DesignPlaceholderPolicy.Active ? "1" : "0")
                + ";honor=" + (DesignPlaceholderPolicy.HonorDeclaredAutoDriveGain ? "1" : "0")
                + ";gain=" + DesignPlaceholderPolicy.DeclaredAutoDriveGain.ToString(CultureInfo.InvariantCulture)
                + ";charge=" + DesignPlaceholderPolicy.ChargeTimeSec.ToString("0.##", CultureInfo.InvariantCulture);
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

        /// <summary>Fever slots that accepted <see cref="CommandSource.Player"/> FeverTap (not Auto).</summary>
        public static int DistinctAcceptedFeverSlots(BattleSim sim, List<int> into)
        {
            return DistinctAcceptedFeverSlots(sim, into, CommandSource.Player);
        }

        public static int DistinctAcceptedFeverSlots(BattleSim sim, List<int> into, CommandSource source)
        {
            if (into != null) into.Clear();
            if (sim == null || sim.CommandLog == null) return 0;
            for (int i = 0; i < sim.CommandLog.Count; i++)
            {
                var r = sim.CommandLog[i];
                if (r == null || !r.Accepted || r.Kind != BattleCommandKind.FeverTap) continue;
                if (r.Source != source) continue;
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

        string CheckPersist(BattleSim live)
        {
            if (!string.Equals(BattleId, _boundBattleId, StringComparison.Ordinal)
                && !string.IsNullOrEmpty(_boundBattleId))
                return RefuseBattleIdMismatch;
            var bound = BoundSim();
            if (live != null && bound != null && !ReferenceEquals(live, bound))
                return RefuseSimMismatch;
            if (live == null && bound == null) return RefuseNoSim;
            return null;
        }

        BattleSim BoundSim()
        {
            return _boundSim != null ? _boundSim : Sim;
        }

        string BuildScenarioText()
        {
            var h = FrozenInitial;
            var rec = Record;
            var sb = new StringBuilder(512);
            sb.Append("battle_id=").Append(BattleId).Append('\n')
                .Append("scenario=").Append(ScenarioName).Append('\n')
                .Append("regression=").Append(RegressionId).Append('\n')
                .Append("provenance=").Append(DesignPlaceholderPolicy.Provenance).Append('\n')
                .Append("schema=").Append(DesignPlaceholderPolicy.SchemaVersion).Append('\n')
                .Append("stage=").Append(StageId).Append('\n')
                .Append("seed=").Append(Seed).Append('\n')
                .Append("pointer=").Append(PointerPath).Append('\n')
                .Append("os_touch=NOT_CLAIMED\n")
                .Append("policy=").Append(PolicyIdentity ?? CurrentPolicyIdentity()).Append('\n')
                .Append("rules=").Append(h != null ? h.RulesVersion : BattleSim.RulesVersion).Append('\n')
                .Append("profile=").Append(h != null ? h.Profile.ToString() : "").Append('\n')
                .Append("auto=").Append(h != null ? h.Auto.ToString() : "").Append('\n')
                .Append("speed=").Append(h != null ? h.Speed.ToString(CultureInfo.InvariantCulture) : "").Append('\n')
                .Append("leader=").Append(h != null ? h.LeaderSlot.ToString(CultureInfo.InvariantCulture) : "").Append('\n')
                .Append("party=").Append(JoinIds(h != null ? h.PartyIds : null)).Append('\n')
                .Append("growth=").Append(h != null ? h.GrowthIdentity : "").Append('\n')
                .Append("clock=").Append(h != null ? h.ClockIdentity : "").Append('\n')
                .Append("dataIdentity=").Append(h != null ? h.DataIdentity : (rec != null ? rec.DataIdentity : "")).Append('\n')
                .Append("contentFingerprint=").Append(h != null ? h.ContentFingerprint : "").Append('\n')
                .Append("headerFrozen=").Append(h != null && h.FrozenAtStart ? "1" : "0").Append('\n')
                .Append("forceNoCrit=").Append(h != null && h.ForceNoCrit ? "1" : "0").Append('\n')
                .Append("replay=").Append(ReplayFileName).Append('\n');
            return sb.ToString();
        }

        string BuildBattleResult(BattleSim sim, string events, string commands)
        {
            var h = FrozenInitial;
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
            sb.AppendLine("replay=" + ReplayFileName);
            if (h != null)
            {
                sb.AppendLine("dataIdentity=" + (h.DataIdentity ?? ""));
                sb.AppendLine("growth=" + (h.GrowthIdentity ?? ""));
                sb.AppendLine("clock=" + (h.ClockIdentity ?? ""));
                sb.AppendLine("policy=" + (PolicyIdentity ?? ""));
            }
            sb.AppendLine();
            sb.AppendLine("--- frozen RunHeader ---");
            sb.AppendLine(string.IsNullOrEmpty(FrozenHeader) ? "(empty)" : FrozenHeader);
            sb.AppendLine();
            sb.AppendLine("--- frozen BattleInitialHeader ---");
            sb.AppendLine(FormatInitial(h));
            sb.AppendLine();
            sb.AppendLine("--- CommandLog ---");
            sb.AppendLine(commands ?? "");
            sb.AppendLine();
            sb.AppendLine("--- Events ---");
            sb.AppendLine(events ?? "");
            return sb.ToString();
        }

        static string FormatInitial(BattleInitialHeader h)
        {
            if (h == null) return "(empty)";
            return "seed=" + h.Seed
                + ";rules=" + h.RulesVersion
                + ";profile=" + h.Profile
                + ";auto=" + h.Auto
                + ";speed=" + h.Speed
                + ";forceNoCrit=" + (h.ForceNoCrit ? 1 : 0)
                + ";leader=" + h.LeaderSlot
                + ";stage=" + (h.StageId ?? "")
                + ";party=" + JoinIds(h.PartyIds)
                + ";growth=" + (h.GrowthIdentity ?? "")
                + ";clock=" + (h.ClockIdentity ?? "")
                + ";data=" + (h.DataIdentity ?? "")
                + ";content=" + (h.ContentFingerprint ?? "")
                + ";frozen=" + (h.FrozenAtStart ? 1 : 0);
        }

        static void ApplyScenarioFile(NaturalPlayBattleEvidence ev, string path)
        {
            if (ev == null || !File.Exists(path)) return;
            var h = ev.FrozenInitial ?? new BattleInitialHeader();
            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrEmpty(line)) continue;
                var eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq);
                var val = line.Substring(eq + 1);
                if (key == "battle_id") ev.BattleId = val;
                else if (key == "scenario") ev.ScenarioName = val;
                else if (key == "regression") ev.RegressionId = val;
                else if (key == "stage") ev.StageId = val;
                else if (key == "seed") ev.Seed = ParseInt(val, ev.Seed);
                else if (key == "policy") ev.PolicyIdentity = val;
                else if (key == "rules") h.RulesVersion = val;
                else if (key == "profile") h.Profile = ParseEnum(val, h.Profile);
                else if (key == "auto") h.Auto = ParseEnum(val, h.Auto);
                else if (key == "speed") h.Speed = ParseInt(val, h.Speed);
                else if (key == "leader") h.LeaderSlot = ParseInt(val, h.LeaderSlot);
                else if (key == "party") h.PartyIds = SplitIds(val);
                else if (key == "growth") h.GrowthIdentity = val;
                else if (key == "clock") h.ClockIdentity = val;
                else if (key == "dataIdentity") h.DataIdentity = val;
                else if (key == "contentFingerprint") h.ContentFingerprint = val;
                else if (key == "headerFrozen") h.FrozenAtStart = val == "1" || string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
                else if (key == "forceNoCrit") h.ForceNoCrit = val == "1" || string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
            }
            h.Seed = ev.Seed;
            h.StageId = ev.StageId ?? h.StageId;
            ev.FrozenInitial = h;
        }

        static bool FolderBattleIdConflicts(string dir, string battleId)
        {
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(battleId)) return false;
            var path = Path.Combine(dir, "scenario.txt");
            if (!File.Exists(path)) return false;
            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line == null || !line.StartsWith("battle_id=", StringComparison.Ordinal)) continue;
                var existing = line.Substring("battle_id=".Length);
                return !string.Equals(existing, battleId, StringComparison.Ordinal);
            }
            return false;
        }

        static string FindReplayPath(string dir)
        {
            var replay = Path.Combine(dir, ReplayFileName);
            if (File.Exists(replay)) return replay;
            var record = Path.Combine(dir, RecordFileName);
            return File.Exists(record) ? record : null;
        }

        static string[] PartyIdsFrom(BattleSim sim)
        {
            if (sim == null || sim.Allies == null) return new string[0];
            var d = new string[sim.Allies.Length];
            for (int i = 0; i < sim.Allies.Length; i++)
                d[i] = sim.Allies[i] != null && sim.Allies[i].Def != null ? sim.Allies[i].Def.Id : "";
            return d;
        }

        static string JoinIds(string[] ids)
        {
            if (ids == null || ids.Length == 0) return "";
            return string.Join(",", ids);
        }

        static string[] SplitIds(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return new string[0];
            return raw.Split(',');
        }

        static string FirstNonEmpty(string a, string b)
        {
            if (!string.IsNullOrEmpty(a)) return a;
            return b ?? "";
        }

        static int ParseInt(string raw, int fallback)
        {
            int n;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : fallback;
        }

        static T ParseEnum<T>(string raw, T fallback) where T : struct
        {
            T parsed;
            if (!string.IsNullOrEmpty(raw) && Enum.TryParse(raw, true, out parsed))
                return parsed;
            return fallback;
        }

        static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var n = text.IndexOfAny(new[] { '\r', '\n' });
            return n < 0 ? text : text.Substring(0, n);
        }

        static string ReadUtf8(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path, new UTF8Encoding(false)) : "";
        }

        static void WriteUtf8(string path, string text)
        {
            File.WriteAllText(path, text ?? "", new UTF8Encoding(false));
        }
    }
}
