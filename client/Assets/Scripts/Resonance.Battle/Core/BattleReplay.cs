using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Resonance.Battle
{
    /// <summary>
    /// Deterministic snapshot of a <see cref="BattleSim"/>. Charge, SlideCd, Drive,
    /// FeverGauge, TimeLeft, and status Remaining are stored rounded to 1e-3.
    /// </summary>
    public sealed class BattleStateDigest
    {
        public const string CanonicalVersion = "g2d1";

        public int TickIndex;
        public BattleOutcome Outcome;
        public int WaveIndex;
        public int Speed;
        public AutoMode Auto;
        public float Drive;
        public float FeverGauge;
        public bool FeverActive;
        public int FeverHitsLeft;
        public float TimeLeft;
        public int EventCount;
        public readonly List<UnitDigest> Units = new List<UnitDigest>(16);
        public string Hash;

        public sealed class UnitDigest
        {
            public bool Ally;
            public int Slot;
            public int Hp;
            public int MaxHp;
            public int Shield;
            public float Charge;
            public float SlideCd;
            public int StatusCount;
            public readonly List<StatusDigest> Status = new List<StatusDigest>(4);
        }

        public sealed class StatusDigest
        {
            public string Id;
            public EffectKind Kind;
            public int Stacks;
            public float Remaining;
        }

        public static BattleStateDigest Of(BattleSim sim)
        {
            var d = new BattleStateDigest();
            if (sim == null)
            {
                d.Hash = d.Sha256();
                return d;
            }

            d.TickIndex = sim.TickIndex;
            d.Outcome = sim.Outcome;
            d.WaveIndex = sim.WaveIndex;
            d.Speed = sim.Speed;
            d.Auto = sim.Auto;
            d.Drive = Round3(sim.Drive);
            d.FeverGauge = Round3(sim.FeverGauge);
            d.FeverActive = sim.FeverActive;
            d.FeverHitsLeft = sim.FeverHitsLeft;
            d.TimeLeft = Round3(sim.TimeLeft);
            d.EventCount = sim.Events != null && sim.Events.Events != null ? sim.Events.Events.Count : 0;

            if (sim.Allies != null)
            {
                for (int i = 0; i < sim.Allies.Length; i++)
                    AddUnit(d.Units, sim.Allies[i], true, i);
            }
            if (sim.Enemies != null)
            {
                for (int i = 0; i < sim.Enemies.Count; i++)
                    AddUnit(d.Units, sim.Enemies[i], false, i);
            }

            d.Hash = d.Sha256();
            return d;
        }

        /// <summary>Alias kept for <c>G2ReviewReplayComponentTests</c>.</summary>
        public static BattleStateDigest Capture(BattleSim sim) => Of(sim);

        static void AddUnit(List<UnitDigest> dest, UnitState u, bool ally, int fallbackSlot)
        {
            var row = new UnitDigest
            {
                Ally = ally,
                Slot = u != null ? u.Slot : fallbackSlot
            };
            if (u != null)
            {
                row.Hp = u.Hp;
                row.MaxHp = u.MaxHp;
                row.Shield = u.Shield;
                row.Charge = Round3(u.Charge);
                row.SlideCd = Round3(u.SlideCd);
                if (u.Status != null)
                {
                    for (int i = 0; i < u.Status.Count; i++)
                    {
                        var st = u.Status[i];
                        if (st == null || st.Def == null) continue;
                        row.Status.Add(new StatusDigest
                        {
                            Id = st.Def.Id ?? "",
                            Kind = st.Def.Kind,
                            Stacks = st.Stacks,
                            Remaining = Round3(st.Remaining)
                        });
                    }
                }
            }
            row.StatusCount = row.Status.Count;
            dest.Add(row);
        }

        public string ToCanonicalString()
        {
            var sb = new StringBuilder(768);
            sb.Append(CanonicalVersion);
            sb.Append('\n');
            Line(sb, "TickIndex", I(TickIndex));
            Line(sb, "Outcome", Outcome.ToString());
            Line(sb, "WaveIndex", I(WaveIndex));
            Line(sb, "Speed", I(Speed));
            Line(sb, "Auto", Auto.ToString());
            Line(sb, "Drive", F3(Drive));
            Line(sb, "FeverGauge", F3(FeverGauge));
            Line(sb, "FeverActive", FeverActive ? "1" : "0");
            Line(sb, "FeverHitsLeft", I(FeverHitsLeft));
            Line(sb, "TimeLeft", F3(TimeLeft));
            for (int i = 0; i < Units.Count; i++)
            {
                var u = Units[i];
                var p = UnitPrefix(u);
                Line(sb, p + ".Hp", I(u.Hp));
                Line(sb, p + ".MaxHp", I(u.MaxHp));
                Line(sb, p + ".Shield", I(u.Shield));
                Line(sb, p + ".Charge", F3(u.Charge));
                Line(sb, p + ".SlideCd", F3(u.SlideCd));
                Line(sb, p + ".StatusCount", I(u.Status != null ? u.Status.Count : u.StatusCount));
                if (u.Status == null) continue;
                for (int s = 0; s < u.Status.Count; s++)
                {
                    var st = u.Status[s];
                    if (st == null) continue;
                    var sp = p + ".S[" + I(s) + "]";
                    Line(sb, sp + ".Id", st.Id ?? "");
                    Line(sb, sp + ".Kind", st.Kind.ToString());
                    Line(sb, sp + ".Stacks", I(st.Stacks));
                    Line(sb, sp + ".Remaining", F3(st.Remaining));
                }
            }
            Line(sb, "Events.Count", I(EventCount));
            if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
                sb.Length--;
            return sb.ToString();
        }

        /// <summary>Alias kept for <c>G2ReviewReplayComponentTests</c>.</summary>
        public string ToText() => ToCanonicalString();

        public string Sha256()
        {
            return BattleEventLog.HashUtf8(ToCanonicalString());
        }

        /// <summary>
        /// Human-readable field differences. Empty when both snapshots are equal
        /// at the contract rounding (1e-3 for named floats).
        /// </summary>
        public static IReadOnlyList<string> Diff(BattleStateDigest a, BattleStateDigest b)
        {
            var diffs = new List<string>();
            if (ReferenceEquals(a, b)) return diffs;
            if (a == null || b == null)
            {
                diffs.Add((a == null ? "<null>" : a.ToCanonicalString()) + " != " + (b == null ? "<null>" : b.ToCanonicalString()));
                return diffs;
            }

            AddIf(diffs, "TickIndex", a.TickIndex != b.TickIndex, I(a.TickIndex), I(b.TickIndex));
            AddIf(diffs, "Outcome", a.Outcome != b.Outcome, a.Outcome.ToString(), b.Outcome.ToString());
            AddIf(diffs, "WaveIndex", a.WaveIndex != b.WaveIndex, I(a.WaveIndex), I(b.WaveIndex));
            AddIf(diffs, "Speed", a.Speed != b.Speed, I(a.Speed), I(b.Speed));
            AddIf(diffs, "Auto", a.Auto != b.Auto, a.Auto.ToString(), b.Auto.ToString());
            AddIf(diffs, "Drive", a.Drive != b.Drive, F3(a.Drive), F3(b.Drive));
            AddIf(diffs, "FeverGauge", a.FeverGauge != b.FeverGauge, F3(a.FeverGauge), F3(b.FeverGauge));
            AddIf(diffs, "FeverActive", a.FeverActive != b.FeverActive, a.FeverActive ? "1" : "0", b.FeverActive ? "1" : "0");
            AddIf(diffs, "FeverHitsLeft", a.FeverHitsLeft != b.FeverHitsLeft, I(a.FeverHitsLeft), I(b.FeverHitsLeft));
            AddIf(diffs, "TimeLeft", a.TimeLeft != b.TimeLeft, F3(a.TimeLeft), F3(b.TimeLeft));
            AddIf(diffs, "Events.Count", a.EventCount != b.EventCount, I(a.EventCount), I(b.EventCount));

            var na = a.Units != null ? a.Units.Count : 0;
            var nb = b.Units != null ? b.Units.Count : 0;
            if (na != nb)
                diffs.Add("Units.Count: " + I(na) + " != " + I(nb));
            var n = na < nb ? na : nb;
            for (int i = 0; i < n; i++)
            {
                var ua = a.Units[i];
                var ub = b.Units[i];
                var p = UnitPrefix(ua != null ? ua : ub);
                if (ua == null || ub == null)
                {
                    diffs.Add(p + ": " + (ua == null ? "<null>" : "unit") + " != " + (ub == null ? "<null>" : "unit"));
                    continue;
                }
                if (ua.Ally != ub.Ally || ua.Slot != ub.Slot)
                    diffs.Add(p + ".Key: " + UnitPrefix(ua) + " != " + UnitPrefix(ub));
                AddIf(diffs, p + ".Hp", ua.Hp != ub.Hp, I(ua.Hp), I(ub.Hp));
                AddIf(diffs, p + ".MaxHp", ua.MaxHp != ub.MaxHp, I(ua.MaxHp), I(ub.MaxHp));
                AddIf(diffs, p + ".Shield", ua.Shield != ub.Shield, I(ua.Shield), I(ub.Shield));
                AddIf(diffs, p + ".Charge", ua.Charge != ub.Charge, F3(ua.Charge), F3(ub.Charge));
                AddIf(diffs, p + ".SlideCd", ua.SlideCd != ub.SlideCd, F3(ua.SlideCd), F3(ub.SlideCd));
                var sa = ua.Status != null ? ua.Status.Count : ua.StatusCount;
                var sb = ub.Status != null ? ub.Status.Count : ub.StatusCount;
                AddIf(diffs, p + ".StatusCount", sa != sb, I(sa), I(sb));
                var sn = (ua.Status != null && ub.Status != null)
                    ? (ua.Status.Count < ub.Status.Count ? ua.Status.Count : ub.Status.Count)
                    : 0;
                for (int s = 0; s < sn; s++)
                {
                    var xa = ua.Status[s];
                    var xb = ub.Status[s];
                    var sp = p + ".S[" + I(s) + "]";
                    if (xa == null || xb == null)
                    {
                        diffs.Add(sp + ": " + (xa == null ? "<null>" : "status") + " != " + (xb == null ? "<null>" : "status"));
                        continue;
                    }
                    AddIf(diffs, sp + ".Id", !string.Equals(xa.Id ?? "", xb.Id ?? "", StringComparison.Ordinal), xa.Id ?? "", xb.Id ?? "");
                    AddIf(diffs, sp + ".Kind", xa.Kind != xb.Kind, xa.Kind.ToString(), xb.Kind.ToString());
                    AddIf(diffs, sp + ".Stacks", xa.Stacks != xb.Stacks, I(xa.Stacks), I(xb.Stacks));
                    AddIf(diffs, sp + ".Remaining", xa.Remaining != xb.Remaining, F3(xa.Remaining), F3(xb.Remaining));
                }
            }
            return diffs;
        }

        static void AddIf(List<string> diffs, string field, bool changed, string left, string right)
        {
            if (changed) diffs.Add(field + ": " + left + " != " + right);
        }

        static string UnitPrefix(UnitDigest u)
        {
            if (u == null) return "Unit";
            return (u.Ally ? "Ally[" : "Enemy[") + I(u.Slot) + "]";
        }

        static void Line(StringBuilder sb, string key, string value)
        {
            sb.Append(key);
            sb.Append('=');
            sb.Append(value);
            sb.Append('\n');
        }

        internal static float Round3(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return v;
            return (float)Math.Round(v, 3, MidpointRounding.AwayFromZero);
        }

        internal static string F3(float v)
        {
            if (float.IsPositiveInfinity(v)) return "inf";
            if (float.IsNegativeInfinity(v)) return "-inf";
            if (float.IsNaN(v)) return "nan";
            return v.ToString("0.000", CultureInfo.InvariantCulture);
        }

        internal static string I(int v) => v.ToString(CultureInfo.InvariantCulture);

        internal static float ParseF3(string raw, float fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            if (raw == "inf") return float.PositiveInfinity;
            if (raw == "-inf") return float.NegativeInfinity;
            if (raw == "nan") return float.NaN;
            float v;
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? Round3(v) : fallback;
        }
    }

    /// <summary>Compact event row mirrored from <see cref="BattleEvent"/> for replay verify.</summary>
    public sealed class EventSummary
    {
        public int Tick;
        public string Phase;
        public string Kind;
        public string Opcode;
        public int Amount;
        public int CasterSlot;
        public int TargetSlot;

        public static EventSummary From(BattleEvent e)
        {
            if (e == null) return null;
            return new EventSummary
            {
                Tick = e.Tick,
                Phase = e.Phase ?? "",
                Kind = e.Kind ?? "",
                Opcode = e.Opcode ?? "",
                Amount = e.Amount,
                CasterSlot = e.CasterSlot,
                TargetSlot = e.TargetSlot
            };
        }

        public string Key()
        {
            return BattleStateDigest.I(Tick)
                + "|" + (Phase ?? "")
                + "|" + (Kind ?? "")
                + "|" + (Opcode ?? "")
                + "|" + BattleStateDigest.I(Amount)
                + "|" + BattleStateDigest.I(CasterSlot)
                + "|" + BattleStateDigest.I(TargetSlot);
        }
    }

    /// <summary>
    /// Captured run: header, copied <see cref="CommandRecord"/> list, final digest, event summaries.
    /// <see cref="ToJsonLines"/> is a hand-written JSON-lines writer (no extra packages).
    /// </summary>
    public sealed class BattleRunRecord
    {
        public string RunHeader;
        public int Seed;
        public string RulesVersion = BattleSim.RulesVersion;
        public FormulaProfile Profile;
        public AutoMode Auto;
        public int Speed;
        public string[] PartyIds;
        public string StageId;
        public readonly List<CommandRecord> Commands = new List<CommandRecord>(64);
        public BattleStateDigest FinalDigest;
        public readonly List<EventSummary> EventSummaries = new List<EventSummary>(256);

        public static BattleRunRecord Capture(BattleSim sim, string[] partyIds, string stageId)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            var rec = new BattleRunRecord();
            rec.RunHeader = sim.RunHeader();
            rec.Seed = sim.Seed;
            rec.RulesVersion = BattleSim.RulesVersion;
            rec.Profile = sim.Profile;
            // Header Speed/Auto are the *start* of the run. Mid-fight SetSpeed/SetAuto
            // stay on the tape; Replay applies this header first, then the tape.
            rec.Auto = InferInitialAuto(sim);
            rec.Speed = InferInitialSpeed(sim);
            rec.PartyIds = CopyIds(partyIds) ?? IdsFromAllies(sim);
            rec.StageId = stageId ?? "";
            if (sim.CommandLog != null)
            {
                for (int i = 0; i < sim.CommandLog.Count; i++)
                {
                    var copy = CopyRecord(sim.CommandLog[i]);
                    if (copy != null) rec.Commands.Add(copy);
                }
            }
            rec.FinalDigest = BattleStateDigest.Of(sim);
            if (sim.Events != null && sim.Events.Events != null)
            {
                for (int i = 0; i < sim.Events.Events.Count; i++)
                {
                    var row = EventSummary.From(sim.Events.Events[i]);
                    if (row != null) rec.EventSummaries.Add(row);
                }
            }
            return rec;
        }

        public string ToJsonLines()
        {
            var sb = new StringBuilder(512 + Commands.Count * 96 + EventSummaries.Count * 80);
            sb.Append("{\"rec\":\"header\"");
            J(sb, "runHeader", RunHeader);
            J(sb, "seed", Seed);
            J(sb, "rulesVersion", RulesVersion ?? BattleSim.RulesVersion);
            J(sb, "profile", Profile.ToString());
            J(sb, "auto", Auto.ToString());
            J(sb, "speed", Speed);
            sb.Append(",\"partyIds\":[");
            var ids = PartyIds ?? new string[0];
            for (int i = 0; i < ids.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"');
                Escape(sb, ids[i] ?? "");
                sb.Append('"');
            }
            sb.Append(']');
            J(sb, "stageId", StageId);
            sb.Append("}\n");

            for (int i = 0; i < Commands.Count; i++)
            {
                var c = Commands[i];
                if (c == null) continue;
                sb.Append("{\"rec\":\"cmd\"");
                J(sb, "seq", c.Seq);
                J(sb, "tick", c.Tick);
                J(sb, "kind", c.Kind.ToString());
                J(sb, "slot", c.Slot);
                J(sb, "timing", c.Timing.ToString());
                J(sb, "value", c.Value);
                J(sb, "source", c.Source.ToString());
                J(sb, "accepted", c.Accepted);
                J(sb, "reason", c.Reason.ToString());
                sb.Append("}\n");
            }

            var d = FinalDigest;
            if (d != null)
            {
                sb.Append("{\"rec\":\"digest\"");
                J(sb, "tickIndex", d.TickIndex);
                J(sb, "outcome", d.Outcome.ToString());
                J(sb, "waveIndex", d.WaveIndex);
                J(sb, "speed", d.Speed);
                J(sb, "auto", d.Auto.ToString());
                J(sb, "drive", BattleStateDigest.F3(d.Drive));
                J(sb, "feverGauge", BattleStateDigest.F3(d.FeverGauge));
                J(sb, "feverActive", d.FeverActive);
                J(sb, "feverHitsLeft", d.FeverHitsLeft);
                J(sb, "timeLeft", BattleStateDigest.F3(d.TimeLeft));
                J(sb, "eventCount", d.EventCount);
                J(sb, "sha256", d.Hash ?? d.Sha256());
                sb.Append("}\n");
                if (d.Units != null)
                {
                    for (int i = 0; i < d.Units.Count; i++)
                    {
                        var u = d.Units[i];
                        if (u == null) continue;
                        sb.Append("{\"rec\":\"unit\"");
                        J(sb, "ally", u.Ally);
                        J(sb, "slot", u.Slot);
                        J(sb, "hp", u.Hp);
                        J(sb, "maxHp", u.MaxHp);
                        J(sb, "shield", u.Shield);
                        J(sb, "charge", BattleStateDigest.F3(u.Charge));
                        J(sb, "slideCd", BattleStateDigest.F3(u.SlideCd));
                        sb.Append("}\n");
                        if (u.Status == null) continue;
                        for (int s = 0; s < u.Status.Count; s++)
                        {
                            var st = u.Status[s];
                            if (st == null) continue;
                            sb.Append("{\"rec\":\"status\"");
                            J(sb, "ally", u.Ally);
                            J(sb, "slot", u.Slot);
                            J(sb, "id", st.Id);
                            J(sb, "kind", st.Kind.ToString());
                            J(sb, "stacks", st.Stacks);
                            J(sb, "remaining", BattleStateDigest.F3(st.Remaining));
                            sb.Append("}\n");
                        }
                    }
                }
            }

            for (int i = 0; i < EventSummaries.Count; i++)
            {
                var e = EventSummaries[i];
                if (e == null) continue;
                sb.Append("{\"rec\":\"event\"");
                J(sb, "tick", e.Tick);
                J(sb, "phase", e.Phase);
                J(sb, "kind", e.Kind);
                J(sb, "opcode", e.Opcode);
                J(sb, "amount", e.Amount);
                J(sb, "casterSlot", e.CasterSlot);
                J(sb, "targetSlot", e.TargetSlot);
                sb.Append("}\n");
            }
            return sb.ToString();
        }

        public static BattleRunRecord ParseJsonLines(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var rec = new BattleRunRecord();
            rec.FinalDigest = new BattleStateDigest();
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed[0] == '#') continue;
                if (trimmed[0] != '{') continue;
                var map = JsonMap.Parse(trimmed);
                var kind = map.Get("rec", "");
                if (kind == "header" || kind == "h")
                    ReadHeader(rec, map);
                else if (kind == "cmd" || kind == "command")
                    rec.Commands.Add(ReadCmd(map));
                else if (kind == "digest")
                    ReadDigestScalars(rec.FinalDigest, map);
                else if (kind == "unit")
                    rec.FinalDigest.Units.Add(ReadUnit(map));
                else if (kind == "status")
                    AttachStatus(rec.FinalDigest, map);
                else if (kind == "event")
                    rec.EventSummaries.Add(ReadEvent(map));
            }
            if (rec.FinalDigest != null)
                rec.FinalDigest.Hash = rec.FinalDigest.Sha256();
            return rec;
        }

        static void ReadHeader(BattleRunRecord rec, JsonMap map)
        {
            rec.RunHeader = map.Get("runHeader", rec.RunHeader ?? "");
            rec.Seed = map.GetInt("seed", rec.Seed);
            rec.RulesVersion = map.Get("rulesVersion", rec.RulesVersion ?? BattleSim.RulesVersion);
            rec.Profile = ParseEnum(map.Get("profile", ""), rec.Profile);
            rec.Auto = ParseEnum(map.Get("auto", ""), rec.Auto);
            rec.Speed = map.GetInt("speed", rec.Speed);
            rec.PartyIds = map.GetStringArray("partyIds");
            rec.StageId = map.Get("stageId", rec.StageId ?? "");
        }

        static void ReadDigestScalars(BattleStateDigest d, JsonMap map)
        {
            d.TickIndex = map.GetInt("tickIndex", d.TickIndex);
            d.Outcome = ParseEnum(map.Get("outcome", ""), d.Outcome);
            d.WaveIndex = map.GetInt("waveIndex", d.WaveIndex);
            d.Speed = map.GetInt("speed", d.Speed);
            d.Auto = ParseEnum(map.Get("auto", ""), d.Auto);
            d.Drive = BattleStateDigest.ParseF3(map.Get("drive", ""), d.Drive);
            d.FeverGauge = BattleStateDigest.ParseF3(map.Get("feverGauge", ""), d.FeverGauge);
            d.FeverActive = map.GetBool("feverActive");
            d.FeverHitsLeft = map.GetInt("feverHitsLeft", d.FeverHitsLeft);
            d.TimeLeft = BattleStateDigest.ParseF3(map.Get("timeLeft", ""), d.TimeLeft);
            d.EventCount = map.GetInt("eventCount", d.EventCount);
            d.Hash = map.Get("sha256", d.Hash);
        }

        static BattleStateDigest.UnitDigest ReadUnit(JsonMap map)
        {
            var u = new BattleStateDigest.UnitDigest
            {
                Ally = map.GetBool("ally"),
                Slot = map.GetInt("slot", 0),
                Hp = map.GetInt("hp", 0),
                MaxHp = map.GetInt("maxHp", 0),
                Shield = map.GetInt("shield", 0),
                Charge = BattleStateDigest.ParseF3(map.Get("charge", ""), 0f),
                SlideCd = BattleStateDigest.ParseF3(map.Get("slideCd", ""), 0f)
            };
            u.StatusCount = u.Status.Count;
            return u;
        }

        static void AttachStatus(BattleStateDigest d, JsonMap map)
        {
            var ally = map.GetBool("ally");
            var slot = map.GetInt("slot", 0);
            BattleStateDigest.UnitDigest unit = null;
            if (d.Units != null)
            {
                for (int i = 0; i < d.Units.Count; i++)
                {
                    var u = d.Units[i];
                    if (u != null && u.Ally == ally && u.Slot == slot) { unit = u; break; }
                }
            }
            if (unit == null) return;
            unit.Status.Add(new BattleStateDigest.StatusDigest
            {
                Id = map.Get("id", ""),
                Kind = ParseEnum(map.Get("kind", ""), default(EffectKind)),
                Stacks = map.GetInt("stacks", 1),
                Remaining = BattleStateDigest.ParseF3(map.Get("remaining", ""), 0f)
            });
            unit.StatusCount = unit.Status.Count;
        }

        static EventSummary ReadEvent(JsonMap map)
        {
            return new EventSummary
            {
                Tick = map.GetInt("tick", 0),
                Phase = map.Get("phase", ""),
                Kind = map.Get("kind", ""),
                Opcode = map.Get("opcode", ""),
                Amount = map.GetInt("amount", 0),
                CasterSlot = map.GetInt("casterSlot", -1),
                TargetSlot = map.GetInt("targetSlot", -1)
            };
        }

        static CommandRecord ReadCmd(JsonMap map)
        {
            return new CommandRecord
            {
                Seq = map.GetInt("seq", 0),
                Tick = map.GetInt("tick", 0),
                Kind = ParseEnum(map.Get("kind", ""), default(BattleCommandKind)),
                Slot = map.GetInt("slot", 0),
                Timing = ParseEnum(map.Get("timing", ""), default(DriveTiming)),
                Value = map.GetInt("value", 0),
                Source = ParseEnum(map.Get("source", ""), default(CommandSource)),
                Accepted = map.GetBool("accepted"),
                Reason = ParseEnum(map.Get("reason", ""), CommandReject.None)
            };
        }

        static void J(StringBuilder sb, string key, string value)
        {
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            Escape(sb, value ?? "");
            sb.Append('"');
        }

        static void J(StringBuilder sb, string key, int value)
        {
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":");
            sb.Append(BattleStateDigest.I(value));
        }

        static void J(StringBuilder sb, string key, bool value)
        {
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":");
            sb.Append(value ? "true" : "false");
        }

        static void Escape(StringBuilder sb, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '\\') sb.Append("\\\\");
                else if (c == '"') sb.Append("\\\"");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else sb.Append(c);
            }
        }

        static int InferInitialSpeed(BattleSim sim)
        {
            var current = sim != null ? sim.Speed : 1;
            if (sim == null || sim.CommandLog == null) return current;
            var saw = false;
            var atTick0 = int.MinValue;
            for (int i = 0; i < sim.CommandLog.Count; i++)
            {
                var c = sim.CommandLog[i];
                if (c == null || !c.Accepted || c.Kind != BattleCommandKind.SetSpeed) continue;
                saw = true;
                if (c.Tick == 0 && atTick0 == int.MinValue) atTick0 = c.Value;
            }
            if (!saw) return current;
            return atTick0 != int.MinValue ? atTick0 : 1;
        }

        static AutoMode InferInitialAuto(BattleSim sim)
        {
            var current = sim != null ? sim.Auto : AutoMode.Manual;
            if (sim == null || sim.CommandLog == null) return current;
            var saw = false;
            var atTick0 = (AutoMode?)null;
            for (int i = 0; i < sim.CommandLog.Count; i++)
            {
                var c = sim.CommandLog[i];
                if (c == null || !c.Accepted || c.Kind != BattleCommandKind.SetAuto) continue;
                saw = true;
                if (c.Tick == 0 && atTick0 == null) atTick0 = (AutoMode)c.Value;
            }
            if (!saw) return current;
            return atTick0 ?? AutoMode.Manual;
        }

        internal static CommandRecord CopyRecord(CommandRecord src)
        {
            if (src == null) return null;
            return new CommandRecord
            {
                Seq = src.Seq,
                Tick = src.Tick,
                Kind = src.Kind,
                Slot = src.Slot,
                Timing = src.Timing,
                Value = src.Value,
                Source = src.Source,
                Accepted = src.Accepted,
                Reason = src.Reason
            };
        }

        internal static string[] CopyIds(string[] src)
        {
            if (src == null) return null;
            var d = new string[src.Length];
            Array.Copy(src, d, src.Length);
            return d;
        }

        internal static string[] IdsFromAllies(BattleSim sim)
        {
            if (sim == null || sim.Allies == null) return new string[0];
            var d = new string[sim.Allies.Length];
            for (int i = 0; i < sim.Allies.Length; i++)
                d[i] = sim.Allies[i] != null && sim.Allies[i].Def != null ? sim.Allies[i].Def.Id : "";
            return d;
        }

        internal static T ParseEnum<T>(string raw, T fallback) where T : struct
        {
            T parsed;
            if (!string.IsNullOrEmpty(raw) && Enum.TryParse(raw, true, out parsed))
                return parsed;
            int n;
            if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)
                && Enum.IsDefined(typeof(T), n))
                return (T)(object)n;
            return fallback;
        }
    }

    /// <summary>
    /// Header bag used by the kv-line <see cref="ReplayScript"/> (compat with
    /// <c>G2ReviewReplayComponentTests</c>). New code should use <see cref="BattleRunRecord"/>.
    /// </summary>
    public sealed class ReplayHeader
    {
        public int Seed;
        public string[] PartyIds;
        public int LeaderSlot;
        public string StageId;
        public FormulaProfile Profile;
        public AutoMode Auto;
        public int Speed;
        public string RulesVersion = BattleSim.RulesVersion;
        public string DataVersion = "unknown";
        public string RunHeader;
    }

    /// <summary>
    /// Legacy kv-line script used by <c>G2ReviewReplayComponentTests</c>.
    /// New capture/replay goes through <see cref="BattleRunRecord"/> JSON-lines.
    /// </summary>
    public sealed class ReplayScript
    {
        public ReplayHeader Header = new ReplayHeader();
        public readonly List<CommandRecord> Commands = new List<CommandRecord>(64);

        public static ReplayScript FromSim(BattleSim sim, string[] partyIds, int leaderSlot, StageDef stage)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            var script = new ReplayScript();
            script.Header.Seed = sim.Seed;
            script.Header.PartyIds = BattleRunRecord.CopyIds(partyIds) ?? BattleRunRecord.IdsFromAllies(sim);
            script.Header.LeaderSlot = leaderSlot;
            script.Header.StageId = stage != null && !string.IsNullOrEmpty(stage.Id) ? stage.Id : "";
            script.Header.Profile = sim.Profile;
            script.Header.Auto = sim.Auto;
            script.Header.Speed = sim.Speed;
            script.Header.RulesVersion = BattleSim.RulesVersion;
            script.Header.RunHeader = sim.RunHeader();
            script.Header.DataVersion = DataVersionFromRunHeader(script.Header.RunHeader)
                ?? ResolveDataVersion();
            if (sim.CommandLog != null)
            {
                for (int i = 0; i < sim.CommandLog.Count; i++)
                {
                    var copy = BattleRunRecord.CopyRecord(sim.CommandLog[i]);
                    if (copy != null) script.Commands.Add(copy);
                }
            }
            return script;
        }

        public string ToJsonLines()
        {
            var sb = new StringBuilder(256 + Commands.Count * 80);
            sb.Append("# replay-format=kv1");
            sb.Append('\n');
            sb.Append("header ");
            WriteHeader(sb, Header ?? new ReplayHeader());
            sb.Append('\n');
            for (int i = 0; i < Commands.Count; i++)
            {
                var c = Commands[i];
                if (c == null) continue;
                sb.Append("cmd ");
                WriteCmd(sb, c);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public static ReplayScript ParseJsonLines(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var script = new ReplayScript();
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                if (raw == null) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                if (StartsWithToken(line, "header"))
                    script.Header = ReadHeader(KvAfterToken(line, "header"));
                else if (StartsWithToken(line, "cmd") || StartsWithToken(line, "command"))
                    script.Commands.Add(ReadCmd(KvAfterToken(line, StartsWithToken(line, "command") ? "command" : "cmd")));
            }
            return script;
        }

        static void WriteHeader(StringBuilder sb, ReplayHeader h)
        {
            Kv(sb, "seed", BattleStateDigest.I(h.Seed), false);
            Kv(sb, "party", h.PartyIds == null ? "" : string.Join(",", h.PartyIds), true);
            Kv(sb, "leader", BattleStateDigest.I(h.LeaderSlot), true);
            Kv(sb, "stage", h.StageId ?? "", true);
            Kv(sb, "profile", h.Profile.ToString(), true);
            Kv(sb, "auto", h.Auto.ToString(), true);
            Kv(sb, "speed", BattleStateDigest.I(h.Speed), true);
            Kv(sb, "rules", h.RulesVersion ?? BattleSim.RulesVersion, true);
            Kv(sb, "data", string.IsNullOrEmpty(h.DataVersion) ? "unknown" : h.DataVersion, true);
            Kv(sb, "run", h.RunHeader ?? "", true);
        }

        static ReplayHeader ReadHeader(string kv)
        {
            var map = ParseKv(kv);
            var h = new ReplayHeader();
            h.Seed = GetInt(map, "seed", 0);
            var party = Get(map, "party", "");
            h.PartyIds = string.IsNullOrEmpty(party) ? new string[0] : party.Split(',');
            h.LeaderSlot = GetInt(map, "leader", 0);
            h.StageId = Get(map, "stage", "");
            h.Profile = BattleRunRecord.ParseEnum(Get(map, "profile", ""), FormulaProfile.GL_UNKNOWN);
            h.Auto = BattleRunRecord.ParseEnum(Get(map, "auto", ""), AutoMode.Manual);
            h.Speed = GetInt(map, "speed", 1);
            h.RulesVersion = Get(map, "rules", BattleSim.RulesVersion);
            h.DataVersion = Get(map, "data", "unknown");
            h.RunHeader = Get(map, "run", "");
            return h;
        }

        static void WriteCmd(StringBuilder sb, CommandRecord c)
        {
            Kv(sb, "seq", BattleStateDigest.I(c.Seq), false);
            Kv(sb, "tick", BattleStateDigest.I(c.Tick), true);
            Kv(sb, "kind", c.Kind.ToString(), true);
            Kv(sb, "slot", BattleStateDigest.I(c.Slot), true);
            Kv(sb, "timing", c.Timing.ToString(), true);
            Kv(sb, "value", BattleStateDigest.I(c.Value), true);
            Kv(sb, "source", c.Source.ToString(), true);
            Kv(sb, "accepted", c.Accepted ? "1" : "0", true);
            Kv(sb, "reason", c.Reason.ToString(), true);
        }

        static CommandRecord ReadCmd(string kv)
        {
            var map = ParseKv(kv);
            return new CommandRecord
            {
                Seq = GetInt(map, "seq", 0),
                Tick = GetInt(map, "tick", 0),
                Kind = BattleRunRecord.ParseEnum(Get(map, "kind", ""), default(BattleCommandKind)),
                Slot = GetInt(map, "slot", 0),
                Timing = BattleRunRecord.ParseEnum(Get(map, "timing", ""), default(DriveTiming)),
                Value = GetInt(map, "value", 0),
                Source = BattleRunRecord.ParseEnum(Get(map, "source", ""), default(CommandSource)),
                Accepted = GetBool(map, "accepted"),
                Reason = BattleRunRecord.ParseEnum(Get(map, "reason", ""), CommandReject.None)
            };
        }

        static string DataVersionFromRunHeader(string runHeader)
        {
            if (string.IsNullOrEmpty(runHeader)) return null;
            var map = ParseKv(runHeader);
            string v;
            if (map.TryGetValue("data", out v) && !string.IsNullOrEmpty(v)) return v;
            return null;
        }

        static string ResolveDataVersion()
        {
            try
            {
                var fp = BattleSim.ContentFingerprint();
                if (!string.IsNullOrEmpty(fp)) return fp;
            }
            catch (Exception)
            {
            }

            var t = typeof(Catalog);
            foreach (var name in new[] { "DataVersion", "ContentVersion", "Version" })
            {
                var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (p != null && p.PropertyType == typeof(string))
                {
                    var v = p.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(v)) return v;
                }
                var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (f != null && f.FieldType == typeof(string))
                {
                    var v = f.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
            return "unknown";
        }

        static bool StartsWithToken(string line, string token)
        {
            if (line.Length < token.Length) return false;
            if (!line.StartsWith(token, StringComparison.OrdinalIgnoreCase)) return false;
            return line.Length == token.Length || char.IsWhiteSpace(line[token.Length]);
        }

        static string KvAfterToken(string line, string token)
        {
            var i = token.Length;
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            return i < line.Length ? line.Substring(i) : "";
        }

        static void Kv(StringBuilder sb, string key, string value, bool semi)
        {
            if (semi) sb.Append(';');
            sb.Append(key);
            sb.Append('=');
            EscapeTo(sb, value ?? "");
        }

        static void EscapeTo(StringBuilder sb, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '\\' || c == ';' || c == '\n' || c == '\r')
                {
                    sb.Append('\\');
                    if (c == '\n') { sb.Append('n'); continue; }
                    if (c == '\r') { sb.Append('r'); continue; }
                    sb.Append(c);
                    continue;
                }
                sb.Append(c);
            }
        }

        static Dictionary<string, string> ParseKv(string line)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(line)) return map;
            var key = new StringBuilder();
            var val = new StringBuilder();
            var inVal = false;
            for (int i = 0; i <= line.Length; i++)
            {
                var atEnd = i == line.Length;
                var c = atEnd ? ';' : line[i];
                if (!inVal)
                {
                    if (c == '=') { inVal = true; continue; }
                    if (!atEnd) key.Append(c);
                    continue;
                }
                if (!atEnd && c == '\\' && i + 1 < line.Length)
                {
                    var n = line[++i];
                    if (n == 'n') val.Append('\n');
                    else if (n == 'r') val.Append('\r');
                    else val.Append(n);
                    continue;
                }
                if (c == ';')
                {
                    var k = key.ToString().Trim();
                    if (k.Length > 0) map[k] = val.ToString();
                    key.Length = 0;
                    val.Length = 0;
                    inVal = false;
                    continue;
                }
                val.Append(c);
            }
            return map;
        }

        static string Get(Dictionary<string, string> map, string key, string fallback)
        {
            string v;
            return map.TryGetValue(key, out v) ? v : fallback;
        }

        static int GetInt(Dictionary<string, string> map, string key, int fallback)
        {
            string v;
            int n;
            if (map.TryGetValue(key, out v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                return n;
            return fallback;
        }

        static bool GetBool(Dictionary<string, string> map, string key)
        {
            string v;
            if (!map.TryGetValue(key, out v) || v == null) return false;
            return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class ReplayReport
    {
        public BattleStateDigest OriginalDigest;
        public BattleStateDigest ReplayedDigest;
        public readonly List<string> DigestDiff = new List<string>();
        public readonly List<string> EventDiff = new List<string>();
        /// <summary>-1 when event sequences match on the summary fields.</summary>
        public int FirstEventDivergence = -1;
        public int ExpectedEventCount;
        public int ActualEventCount;
        public BattleSim Replayed;

        public bool Ok => DigestDiff.Count == 0 && EventDiff.Count == 0 && FirstEventDivergence < 0;
        public bool Match => Ok;

        public IReadOnlyList<string> Diff
        {
            get
            {
                var all = new List<string>(DigestDiff.Count + EventDiff.Count);
                for (int i = 0; i < DigestDiff.Count; i++) all.Add(DigestDiff[i]);
                for (int i = 0; i < EventDiff.Count; i++) all.Add(EventDiff[i]);
                return all;
            }
        }
    }

    /// <summary>
    /// Replays an accepted command tape onto a fresh sim.
    /// <para>
    /// Ordering: at live <see cref="BattleSim.TickIndex"/> T, every recorded accepted
    /// command whose <see cref="CommandRecord.Tick"/> == T is submitted with
    /// <see cref="CommandSource.Replay"/>; only then is <see cref="BattleSim.Tick"/>
    /// called, which advances the sim to T+1 (unless paused or already finished).
    /// Commands therefore land after T completed ticks and before the (T+1)th advance.
    /// Header Speed/Auto are applied first via Submit(SetSpeed/SetAuto, Replay).
    /// </para>
    /// </summary>
    public static class BattleReplayer
    {
        /// <summary>
        /// Filled by the latest <see cref="Run"/> / <see cref="Replay"/>: a replayed
        /// originally-accepted command that <see cref="BattleSim.Submit"/> now rejected.
        /// </summary>
        public static readonly List<string> Divergences = new List<string>();

        public static BattleSim Replay(BattleRunRecord rec, Func<int, BattleSim> factory)
        {
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            Divergences.Clear();
            var sim = factory(rec.Seed);
            if (sim == null) throw new InvalidOperationException("Replay factory returned null.");

            sim.Submit(new BattleCommand
            {
                Kind = BattleCommandKind.SetSpeed,
                Value = rec.Speed,
                Source = CommandSource.Replay
            });
            sim.Submit(new BattleCommand
            {
                Kind = BattleCommandKind.SetAuto,
                Value = (int)rec.Auto,
                Source = CommandSource.Replay
            });

            var accepted = new List<CommandRecord>(rec.Commands.Count);
            for (int i = 0; i < rec.Commands.Count; i++)
            {
                var c = rec.Commands[i];
                if (c != null && c.Accepted) accepted.Add(c);
            }

            var goal = rec.FinalDigest != null ? rec.FinalDigest.TickIndex : LastTick(accepted);
            var cursor = 0;
            var spins = 0;
            var spinLimit = Math.Max(goal, 0) + accepted.Count + 16;
            while (spins++ <= spinLimit)
            {
                DrainAt(sim, accepted, ref cursor, sim.TickIndex);
                if (sim.TickIndex >= goal)
                {
                    DrainAt(sim, accepted, ref cursor, sim.TickIndex);
                    break;
                }
                var before = sim.TickIndex;
                sim.Tick();
                if (sim.TickIndex != before) continue;
                DrainAt(sim, accepted, ref cursor, sim.TickIndex);
                if (sim.TickIndex >= goal) break;
                if (sim.Outcome != BattleOutcome.InProgress || sim.Paused)
                    break;
            }
            DrainAt(sim, accepted, ref cursor, sim.TickIndex);
            return sim;
        }

        public static ReplayReport Verify(BattleRunRecord rec, Func<int, BattleSim> factory)
        {
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            var replayed = Replay(rec, factory);
            var report = Compare(rec, replayed);
            report.Replayed = replayed;
            return report;
        }

        public static BattleSim Run(ReplayScript script, Func<BattleSim> factory, int maxTicks)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (script == null) throw new ArgumentNullException(nameof(script));
            Divergences.Clear();
            var sim = factory();
            if (sim == null) throw new InvalidOperationException("Replay factory returned null.");

            var cmds = script.Commands;
            var cursor = 0;
            if (maxTicks < 0) maxTicks = 0;
            for (int step = 0; step < maxTicks; step++)
            {
                DrainAt(sim, cmds, ref cursor, sim.TickIndex);
                if (sim.Outcome != BattleOutcome.InProgress)
                    break;
                sim.Tick();
            }
            DrainAt(sim, cmds, ref cursor, sim.TickIndex);
            return sim;
        }

        public static ReplayReport Verify(BattleSim original, BattleSim replayed)
        {
            var rec = original != null
                ? BattleRunRecord.Capture(original, BattleRunRecord.IdsFromAllies(original), "")
                : null;
            return Compare(rec, replayed);
        }

        static ReplayReport Compare(BattleRunRecord rec, BattleSim replayed)
        {
            var report = new ReplayReport();
            report.OriginalDigest = rec != null ? rec.FinalDigest : null;
            report.ReplayedDigest = BattleStateDigest.Of(replayed);
            var digestDiff = BattleStateDigest.Diff(report.OriginalDigest, report.ReplayedDigest);
            for (int i = 0; i < digestDiff.Count; i++)
                report.DigestDiff.Add(digestDiff[i]);

            var expected = rec != null ? rec.EventSummaries : null;
            var actualEvents = replayed != null && replayed.Events != null ? replayed.Events.Events : null;
            var na = expected != null ? expected.Count : 0;
            var nb = actualEvents != null ? actualEvents.Count : 0;
            report.ExpectedEventCount = na;
            report.ActualEventCount = nb;
            var n = na < nb ? na : nb;
            for (int i = 0; i < n; i++)
            {
                var a = expected[i];
                var b = EventSummary.From(actualEvents[i]);
                if (a != null && b != null && string.Equals(a.Key(), b.Key(), StringComparison.Ordinal))
                    continue;
                report.FirstEventDivergence = i;
                report.EventDiff.Add("[" + BattleStateDigest.I(i) + "] " + (a != null ? a.Key() : "<null>") + " != " + (b != null ? b.Key() : "<null>"));
                break;
            }
            if (report.FirstEventDivergence < 0 && na != nb)
            {
                report.FirstEventDivergence = n;
                report.EventDiff.Add("Events.Count " + BattleStateDigest.I(na) + " != " + BattleStateDigest.I(nb));
            }
            return report;
        }

        static void DrainAt(BattleSim sim, List<CommandRecord> cmds, ref int cursor, int tick)
        {
            if (cmds == null || sim == null) return;
            while (cursor < cmds.Count)
            {
                var rec = cmds[cursor];
                if (rec == null) { cursor++; continue; }
                if (rec.Tick > tick) break;
                cursor++;
                if (rec.Tick < tick || !rec.Accepted) continue;
                SubmitReplay(sim, rec);
            }
        }

        static void SubmitReplay(BattleSim sim, CommandRecord rec)
        {
            var result = sim.Submit(new BattleCommand
            {
                Kind = rec.Kind,
                Slot = rec.Slot,
                Timing = rec.Timing,
                Value = rec.Value,
                Source = CommandSource.Replay
            });
            if (!result.Accepted)
            {
                Divergences.Add(
                    "tick=" + BattleStateDigest.I(sim.TickIndex)
                    + ";kind=" + rec.Kind
                    + ";slot=" + BattleStateDigest.I(rec.Slot)
                    + ";reject=" + result.Reason);
            }
        }

        static int LastTick(List<CommandRecord> cmds)
        {
            var t = 0;
            if (cmds == null) return t;
            for (int i = 0; i < cmds.Count; i++)
                if (cmds[i] != null && cmds[i].Tick > t) t = cmds[i].Tick;
            return t;
        }
    }

    /// <summary>Minimal JSON object reader for the hand-written JSON-lines in <see cref="BattleRunRecord"/>.</summary>
    sealed class JsonMap
    {
        readonly Dictionary<string, string> _str = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string[]> _arr = new Dictionary<string, string[]>(StringComparer.Ordinal);

        public static JsonMap Parse(string json)
        {
            var map = new JsonMap();
            if (string.IsNullOrEmpty(json)) return map;
            var i = 0;
            SkipWs(json, ref i);
            if (i >= json.Length || json[i] != '{') return map;
            i++;
            while (i < json.Length)
            {
                SkipWs(json, ref i);
                if (i >= json.Length) break;
                if (json[i] == '}') break;
                if (json[i] == ',') { i++; continue; }
                var key = ReadString(json, ref i);
                SkipWs(json, ref i);
                if (i < json.Length && json[i] == ':') i++;
                SkipWs(json, ref i);
                if (i >= json.Length) break;
                if (json[i] == '"')
                    map._str[key] = ReadString(json, ref i);
                else if (json[i] == '[')
                    map._arr[key] = ReadStringArray(json, ref i);
                else if (json[i] == '{')
                    SkipObject(json, ref i);
                else
                {
                    var start = i;
                    while (i < json.Length && json[i] != ',' && json[i] != '}' && !char.IsWhiteSpace(json[i]))
                        i++;
                    map._str[key] = json.Substring(start, i - start);
                }
            }
            return map;
        }

        public string Get(string key, string fallback)
        {
            string v;
            return _str.TryGetValue(key, out v) ? v : fallback;
        }

        public int GetInt(string key, int fallback)
        {
            string v;
            int n;
            if (_str.TryGetValue(key, out v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                return n;
            return fallback;
        }

        public bool GetBool(string key)
        {
            string v;
            if (!_str.TryGetValue(key, out v) || v == null) return false;
            return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public string[] GetStringArray(string key)
        {
            string[] a;
            return _arr.TryGetValue(key, out a) ? a : new string[0];
        }

        static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static string ReadString(string s, ref int i)
        {
            if (i >= s.Length || s[i] != '"') return "";
            i++;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                var c = s[i++];
                if (c == '"') break;
                if (c == '\\' && i < s.Length)
                {
                    var n = s[i++];
                    if (n == 'n') sb.Append('\n');
                    else if (n == 'r') sb.Append('\r');
                    else if (n == 't') sb.Append('\t');
                    else sb.Append(n);
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        static string[] ReadStringArray(string s, ref int i)
        {
            var list = new List<string>(8);
            if (i >= s.Length || s[i] != '[') return new string[0];
            i++;
            while (i < s.Length)
            {
                SkipWs(s, ref i);
                if (i >= s.Length) break;
                if (s[i] == ']') { i++; break; }
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '"') list.Add(ReadString(s, ref i));
                else
                {
                    var start = i;
                    while (i < s.Length && s[i] != ',' && s[i] != ']') i++;
                    list.Add(s.Substring(start, i - start));
                }
            }
            return list.ToArray();
        }

        static void SkipObject(string s, ref int i)
        {
            var depth = 0;
            while (i < s.Length)
            {
                var c = s[i++];
                if (c == '"')
                {
                    i--;
                    ReadString(s, ref i);
                    continue;
                }
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth <= 0) return;
                }
            }
        }
    }
}
