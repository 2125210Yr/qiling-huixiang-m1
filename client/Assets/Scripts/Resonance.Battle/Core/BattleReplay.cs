using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    /// REGRESSIONS.md R04 / A53-02 — combat-data identity for fields the sim consumes.
    /// <see cref="Fingerprint"/> / <see cref="BattleSim.ContentFingerprint"/> stay catalog-global
    /// (<c>g2id1-</c>). <see cref="Compute"/> / DataIdentity are fight-complete: catalog + party/stage
    /// + grown stats + sim skill/effect overlays + bound <see cref="DesignPlaceholderPolicy"/>.
    /// Overlay maps are private on BattleSim; <see cref="TryReadSimOverlays"/> reads them until
    /// the integrator lands <c>patches/A53-IDENTITY.md</c>.
    /// </summary>
    public static class BattleContentIdentity
    {
        public const string CanonicalVersion = "g2id1";

        static PropertyInfo _skillOverlayProp;
        static PropertyInfo _effectOverlayProp;
        static FieldInfo _skillOverlayField;
        static FieldInfo _effectOverlayField;
        static bool _overlayReflectReady;

        public static string Fingerprint()
        {
            return BattleEventLog.HashUtf8(CanonicalCatalog());
        }

        /// <summary>
        /// Catalog + selected stage + grown ally Def/ExtraAtk + overlays + bound policy.
        /// Does not include live HP or Speed/Auto.
        /// </summary>
        public static string Compute(BattleSim sim, string[] partyIds, string stageId)
        {
            IReadOnlyDictionary<string, SkillDef> skills;
            IReadOnlyDictionary<string, EffectDef> effects;
            TryReadSimOverlays(sim, out skills, out effects);
            return Compute(sim, partyIds, stageId, skills, effects);
        }

        /// <summary>QA hook: fight identity with explicit overlays (no BattleSim field access required).</summary>
        public static string Compute(
            BattleSim sim,
            string[] partyIds,
            string stageId,
            IReadOnlyDictionary<string, SkillDef> skillOverlay,
            IReadOnlyDictionary<string, EffectDef> effectOverlay)
        {
            var sb = new StringBuilder(2048);
            sb.Append(CanonicalCatalog());
            sb.Append("party=");
            var ids = partyIds ?? BattleRunRecord.IdsFromAllies(sim);
            if (ids != null)
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(ids[i] ?? "");
                }
            }
            sb.Append('\n');
            sb.Append("stageId=").Append(stageId ?? "").Append('\n');
            sb.Append("leader=").Append(BattleStateDigest.I(sim != null ? sim.LeaderSlot : 0)).Append('\n');
            sb.Append("profile=").Append(sim != null ? sim.Profile.ToString() : "").Append('\n');
            sb.Append("clocks=").Append(ClockKey(sim != null ? sim.Clocks : null)).Append('\n');
            if (sim != null && sim.Allies != null)
            {
                for (int i = 0; i < sim.Allies.Length; i++)
                    AppendGrownAlly(sb, sim.Allies[i], i);
            }
            var stage = FindStage(stageId);
            if (stage != null) AppendStage(sb, "sel", stage);
            AppendBoundPolicy(sb);
            AppendOverlaySkills(sb, skillOverlay);
            AppendOverlayEffects(sb, effectOverlay);
            return BattleEventLog.HashUtf8(sb.ToString());
        }

        /// <summary>QA hook: bound DESIGN_PLACEHOLDER values included in Compute / DataIdentity.</summary>
        public static string PolicyKey()
        {
            var sb = new StringBuilder(96);
            AppendBoundPolicy(sb);
            return sb.ToString();
        }

        /// <summary>
        /// QA hook: skill/effect overlays currently on <paramref name="sim"/>.
        /// Prefers public <c>SkillOverlays</c>/<c>EffectOverlays</c> (patches/A53-IDENTITY.md);
        /// otherwise reads private <c>_skillOverlay</c>/<c>_effectOverlay</c>.
        /// </summary>
        public static bool TryReadSimOverlays(
            BattleSim sim,
            out IReadOnlyDictionary<string, SkillDef> skillOverlay,
            out IReadOnlyDictionary<string, EffectDef> effectOverlay)
        {
            skillOverlay = null;
            effectOverlay = null;
            if (sim == null) return false;
            EnsureOverlayReflect();
            skillOverlay = ReadOverlayMap<SkillDef>(sim, _skillOverlayProp, _skillOverlayField);
            effectOverlay = ReadOverlayMap<EffectDef>(sim, _effectOverlayProp, _effectOverlayField);
            return skillOverlay != null || effectOverlay != null;
        }

        public static string CanonicalCatalog()
        {
            var sb = new StringBuilder(4096);
            sb.Append(CanonicalVersion);
            sb.Append('\n');
            AppendChars(sb, Catalog.Characters);
            AppendSkills(sb, Catalog.Skills);
            AppendEffects(sb, Catalog.Effects);
            AppendStageArray(sb, "stages", Catalog.Stages);
            AppendStageArray(sb, "hard", Catalog.HardStages);
            if (Catalog.VerticalSliceStage != null)
                AppendStage(sb, "vs", Catalog.VerticalSliceStage);
            return sb.ToString();
        }

        public static string ClockKey(BattleClockPolicy c)
        {
            if (c == null) return "null";
            return BattleStateDigest.F3(c.SlideCdSec)
                + "|" + BattleStateDigest.F3(c.FeverWindowSec)
                + "|" + BattleStateDigest.I(c.FeverHitBudget)
                + "|" + BattleStateDigest.F3(c.DriveQteTimeoutSec)
                + "|" + BattleStateDigest.F3(c.HoldTimeoutSec)
                + "|" + (c.StageCountdownScalesWithSpeed ? "1" : "0")
                + "|" + (c.ChargeScalesWithSpeed ? "1" : "0")
                + "|" + (c.SlideCdScalesWithSpeed ? "1" : "0")
                + "|" + (c.StatusDurationScalesWithSpeed ? "1" : "0")
                + "|" + (c.AutoIntervalScalesWithSpeed ? "1" : "0")
                + "|" + (c.FeverWindowScalesWithSpeed ? "1" : "0")
                + "|" + (c.DriveQteScalesWithSpeed ? "1" : "0")
                + "|" + (c.HoldWatchdogScalesWithSpeed ? "1" : "0")
                + "|" + BattleStateDigest.F3(c.FeverMinHitIntervalSec)
                + "|" + BattleStateDigest.F3(c.FeverAutoTapsPerSec);
        }

        public static StageDef FindStage(string id)
        {
            if (Catalog.VerticalSliceStage != null
                && (string.IsNullOrEmpty(id) || string.Equals(Catalog.VerticalSliceStage.Id, id, StringComparison.Ordinal)))
                return Catalog.VerticalSliceStage;
            var found = FindIn(Catalog.Stages, id);
            if (found != null) return found;
            return FindIn(Catalog.HardStages, id);
        }

        static StageDef FindIn(StageDef[] table, string id)
        {
            if (table == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < table.Length; i++)
            {
                var s = table[i];
                if (s != null && string.Equals(s.Id, id, StringComparison.Ordinal)) return s;
            }
            return null;
        }

        static void AppendGrownAlly(StringBuilder sb, UnitState u, int slot)
        {
            sb.Append("ally[").Append(BattleStateDigest.I(slot)).Append("]");
            if (u == null)
            {
                sb.Append("=null\n");
                return;
            }
            var d = u.Def;
            sb.Append(".id=").Append(d != null ? d.Id ?? "" : "");
            sb.Append(";hp=").Append(BattleStateDigest.I(d != null ? d.Hp : 0));
            sb.Append(";atk=").Append(BattleStateDigest.I(d != null ? d.Atk : 0));
            sb.Append(";def=").Append(BattleStateDigest.I(d != null ? d.Def : 0));
            sb.Append(";agl=").Append(BattleStateDigest.I(d != null ? d.Agl : 0));
            sb.Append(";crt=").Append(BattleStateDigest.I(d != null ? d.Crt : 0));
            sb.Append(";extraAtk=").Append(BattleStateDigest.I(u.ExtraAtk));
            sb.Append(";ignCrt=").Append(BattleStateDigest.F3(u.IgnCrtAdd));
            sb.Append(";ignAgl=").Append(BattleStateDigest.F3(u.IgnAglAdd));
            sb.Append('\n');
        }

        static void AppendChars(StringBuilder sb, IReadOnlyDictionary<string, CharacterDef> map)
        {
            var keys = SortedKeys(map);
            sb.Append("chars=").Append(BattleStateDigest.I(keys.Count)).Append('\n');
            for (int i = 0; i < keys.Count; i++)
            {
                CharacterDef c;
                map.TryGetValue(keys[i], out c);
                sb.Append("C[").Append(keys[i]).Append("]");
                if (c == null) { sb.Append("=null\n"); continue; }
                sb.Append(".hp=").Append(BattleStateDigest.I(c.Hp));
                sb.Append(";atk=").Append(BattleStateDigest.I(c.Atk));
                sb.Append(";def=").Append(BattleStateDigest.I(c.Def));
                sb.Append(";agl=").Append(BattleStateDigest.I(c.Agl));
                sb.Append(";crt=").Append(BattleStateDigest.I(c.Crt));
                sb.Append(";charge=").Append(BattleStateDigest.F3(c.ChargeTimeSec));
                sb.Append(";tap=").Append(c.TapSkillId ?? "");
                sb.Append(";slide=").Append(c.SlideSkillId ?? "");
                sb.Append(";drive=").Append(c.DriveSkillId ?? "");
                sb.Append(";lead=").Append(c.LeaderSkillId ?? "");
                sb.Append(";el=").Append(c.Element.ToString());
                sb.Append(";auto=").Append(c.AutoSkillId ?? "");
                sb.Append('\n');
            }
        }

        static void AppendSkills(StringBuilder sb, IReadOnlyDictionary<string, SkillDef> map)
        {
            var keys = SortedKeys(map);
            sb.Append("skills=").Append(BattleStateDigest.I(keys.Count)).Append('\n');
            for (int i = 0; i < keys.Count; i++)
            {
                SkillDef s = null;
                if (map != null) map.TryGetValue(keys[i], out s);
                sb.Append("K[").Append(keys[i]).Append("]");
                if (s == null) { sb.Append("=null\n"); continue; }
                AppendSkillCombatFields(sb, s);
                sb.Append('\n');
            }
        }

        static void AppendEffects(StringBuilder sb, IReadOnlyDictionary<string, EffectDef> map)
        {
            var keys = SortedKeys(map);
            sb.Append("effects=").Append(BattleStateDigest.I(keys.Count)).Append('\n');
            for (int i = 0; i < keys.Count; i++)
            {
                EffectDef e = null;
                if (map != null) map.TryGetValue(keys[i], out e);
                sb.Append("E[").Append(keys[i]).Append("]");
                if (e == null) { sb.Append("=null\n"); continue; }
                AppendEffectCombatFields(sb, e);
                sb.Append('\n');
            }
        }

        static void AppendSkillCombatFields(StringBuilder sb, SkillDef s)
        {
            sb.Append(".op=").Append(s.Opcode ?? "");
            sb.Append(";tgt=").Append(s.Target.ToString());
            sb.Append(";tn=").Append(BattleStateDigest.I(s.TargetCount));
            sb.Append(";hits=").Append(BattleStateDigest.I(s.HitCount));
            sb.Append(";coef=").Append(FR(s.AtkCoef));
            sb.Append(";flat=").Append(BattleStateDigest.I(s.FlatPower));
            sb.Append(";drv=").Append(BattleStateDigest.I(s.DriveGain));
            sb.Append(";fx=").Append(s.EffectId ?? "");
            sb.Append(";heal=").Append(FR(s.HealCoef));
            sb.Append(";pct=").Append(FR(s.PercentAtk));
            sb.Append(";type=").Append(s.Type.ToString());
            sb.Append(";hflat=").Append(BattleStateDigest.I(s.FlatHeal));
            sb.Append(";hfrac=").Append(FR(s.HealMaxHpFrac));
            sb.Append(";sflat=").Append(FR(s.SkillFlat));
        }

        static void AppendEffectCombatFields(StringBuilder sb, EffectDef e)
        {
            sb.Append(".kind=").Append(e.Kind.ToString());
            sb.Append(";mag=").Append(FR(e.Magnitude));
            sb.Append(";dur=").Append(FR(e.DurationSec));
            sb.Append(";stack=").Append(BattleStateDigest.I(e.MaxStack));
            sb.Append(";trig=").Append(e.Trigger ?? "");
            sb.Append(";period=").Append(FR(e.PeriodSec));
            sb.Append(";op=").Append(e.Opcode ?? "");
            sb.Append(";grp=").Append(e.Group ?? "");
            sb.Append(";tier=").Append(BattleStateDigest.I(e.SourceTier));
            sb.Append(";hasT=").Append(e.HasTarget ? "1" : "0");
            sb.Append(";tgt=").Append(e.Target.ToString());
            sb.Append(";side=").Append(e.Side.ToString());
        }

        static void AppendBoundPolicy(StringBuilder sb)
        {
            sb.Append("policy.active=").Append(DesignPlaceholderPolicy.Active ? "1" : "0");
            sb.Append(";schema=").Append(DesignPlaceholderPolicy.Schema ?? "");
            sb.Append(";scenario=").Append(DesignPlaceholderPolicy.ScenarioName ?? "");
            sb.Append(";honor=").Append(DesignPlaceholderPolicy.HonorDeclaredAutoDriveGain ? "1" : "0");
            sb.Append(";autoDrive=").Append(BattleStateDigest.I(DesignPlaceholderPolicy.DeclaredAutoDriveGain));
            sb.Append(";charge=").Append(FR(DesignPlaceholderPolicy.ChargeTimeSec));
            sb.Append('\n');
        }

        static void AppendOverlaySkills(StringBuilder sb, IReadOnlyDictionary<string, SkillDef> map)
        {
            var keys = SortedKeys(map);
            sb.Append("ovSkills=").Append(BattleStateDigest.I(keys.Count)).Append('\n');
            for (int i = 0; i < keys.Count; i++)
            {
                SkillDef s = null;
                if (map != null) map.TryGetValue(keys[i], out s);
                sb.Append("Kov[").Append(keys[i]).Append("]");
                if (s == null) { sb.Append("=null\n"); continue; }
                AppendSkillCombatFields(sb, s);
                sb.Append('\n');
            }
        }

        static void AppendOverlayEffects(StringBuilder sb, IReadOnlyDictionary<string, EffectDef> map)
        {
            var keys = SortedKeys(map);
            sb.Append("ovEffects=").Append(BattleStateDigest.I(keys.Count)).Append('\n');
            for (int i = 0; i < keys.Count; i++)
            {
                EffectDef e = null;
                if (map != null) map.TryGetValue(keys[i], out e);
                sb.Append("Eov[").Append(keys[i]).Append("]");
                if (e == null) { sb.Append("=null\n"); continue; }
                AppendEffectCombatFields(sb, e);
                sb.Append('\n');
            }
        }

        static void EnsureOverlayReflect()
        {
            if (_overlayReflectReady) return;
            var t = typeof(BattleSim);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _skillOverlayProp = t.GetProperty("SkillOverlays", flags);
            _effectOverlayProp = t.GetProperty("EffectOverlays", flags);
            _skillOverlayField = t.GetField("_skillOverlay", flags);
            _effectOverlayField = t.GetField("_effectOverlay", flags);
            _overlayReflectReady = true;
        }

        static IReadOnlyDictionary<string, T> ReadOverlayMap<T>(BattleSim sim, PropertyInfo prop, FieldInfo field)
        {
            if (prop != null && prop.CanRead)
            {
                var viaProp = prop.GetValue(sim, null) as IReadOnlyDictionary<string, T>;
                if (viaProp != null) return viaProp;
            }
            if (field != null)
                return field.GetValue(sim) as IReadOnlyDictionary<string, T>;
            return null;
        }

        static void AppendStageArray(StringBuilder sb, string prefix, StageDef[] table)
        {
            var n = table != null ? table.Length : 0;
            sb.Append(prefix).Append("=").Append(BattleStateDigest.I(n)).Append('\n');
            if (table == null) return;
            for (int i = 0; i < table.Length; i++)
                if (table[i] != null) AppendStage(sb, prefix + "[" + BattleStateDigest.I(i) + "]", table[i]);
        }

        static void AppendStage(StringBuilder sb, string prefix, StageDef s)
        {
            sb.Append(prefix);
            sb.Append(".id=").Append(s.Id ?? "");
            sb.Append(";t=").Append(FR(s.TimeLimitSec));
            sb.Append(";hpMul=").Append(FR(s.EnemyHpMul));
            sb.Append(";atkMul=").Append(FR(s.EnemyAtkMul));
            sb.Append(";defMul=").Append(FR(s.EnemyDefMul));
            sb.Append(";diff=").Append(BattleStateDigest.I(s.Difficulty));
            sb.Append(";w0=").Append(JoinIds(s.Wave0));
            sb.Append(";w1=").Append(JoinIds(s.Wave1));
            sb.Append('\n');
        }

        static string JoinIds(string[] ids)
        {
            if (ids == null || ids.Length == 0) return "";
            return string.Join(",", ids);
        }

        static string FR(float v)
        {
            return v.ToString("G9", CultureInfo.InvariantCulture);
        }

        static List<string> SortedKeys<T>(IReadOnlyDictionary<string, T> map)
        {
            var keys = new List<string>(map != null ? map.Count : 0);
            if (map != null)
            {
                foreach (var kv in map)
                    if (kv.Key != null) keys.Add(kv.Key);
            }
            keys.Sort(StringComparer.Ordinal);
            return keys;
        }
    }

    /// <summary>
    /// REGRESSIONS.md R02 — real start-of-battle header.
    /// Call <see cref="Freeze"/> after Speed/Auto/Profile/Clocks are assigned and before the first
    /// <see cref="BattleSim.Tick"/>. GameRoot hook: <c>patches/G2R14-REPLAY.md</c>.
    /// Do not recover Speed/Auto from later SetSpeed/SetAuto (that guessed 1/Manual).
    /// </summary>
    public sealed class BattleInitialHeader
    {
        static readonly ConditionalWeakTable<BattleSim, BattleInitialHeader> Frozen
            = new ConditionalWeakTable<BattleSim, BattleInitialHeader>();

        public int Seed;
        public string RulesVersion = BattleSim.RulesVersion;
        public FormulaProfile Profile;
        public AutoMode Auto;
        public int Speed = 1;
        public bool ForceNoCrit;
        public int LeaderSlot;
        public string[] PartyIds;
        public string StageId;
        public string ClockIdentity;
        public string GrowthIdentity;
        public string DataIdentity;
        public string ContentFingerprint;
        public bool FrozenAtStart;

        /// <summary>First call wins. Snapshot the configured sim as the opening header.</summary>
        public static BattleInitialHeader Freeze(BattleSim sim, string[] partyIds = null, string stageId = null)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            BattleInitialHeader existing;
            if (Frozen.TryGetValue(sim, out existing) && existing != null)
                return existing;
            var attached = ReadSimField(sim);
            if (attached != null)
            {
                Frozen.Add(sim, attached);
                return attached;
            }
            var h = Snapshot(sim, partyIds, stageId, true);
            Frozen.Add(sim, h);
            WriteSimField(sim, h);
            return h;
        }

        public static BattleInitialHeader TryGet(BattleSim sim)
        {
            if (sim == null) return null;
            BattleInitialHeader h;
            if (Frozen.TryGetValue(sim, out h) && h != null) return h;
            return ReadSimField(sim);
        }

        /// <summary>
        /// Prefer a start freeze. Tests that never froze keep the legacy 1/Manual guess
        /// (safe only when the opening Speed/Auto were the defaults).
        /// </summary>
        public static BattleInitialHeader ResolveForCapture(BattleSim sim, string[] partyIds, string stageId)
        {
            var frozen = TryGet(sim);
            if (frozen != null) return Copy(frozen);
            return Snapshot(sim, partyIds, stageId, false);
        }

        public static BattleInitialHeader Snapshot(BattleSim sim, string[] partyIds, string stageId, bool atStart)
        {
            var ids = BattleRunRecord.CopyIds(partyIds) ?? BattleRunRecord.IdsFromAllies(sim);
            var stage = stageId ?? "";
            var h = new BattleInitialHeader();
            h.Seed = sim != null ? sim.Seed : 0;
            h.RulesVersion = BattleSim.RulesVersion;
            h.Profile = sim != null ? sim.Profile : default(FormulaProfile);
            h.ForceNoCrit = sim != null && sim.ForceNoCrit;
            h.LeaderSlot = sim != null ? sim.LeaderSlot : 0;
            h.PartyIds = ids;
            h.StageId = stage;
            h.ClockIdentity = BattleContentIdentity.ClockKey(sim != null ? sim.Clocks : null);
            h.GrowthIdentity = GrowthKey(sim);
            h.ContentFingerprint = SafeContentFingerprint();
            h.DataIdentity = BattleContentIdentity.Compute(sim, ids, stage);
            h.FrozenAtStart = atStart;
            if (atStart)
            {
                h.Auto = sim != null ? sim.Auto : AutoMode.Manual;
                h.Speed = sim != null && sim.Speed >= 1 ? sim.Speed : 1;
            }
            else
            {
                // Legacy path for G2ReviewReplay* which never freeze. Wrong when the
                // opening values were not Speed=1 / Manual (R02).
                h.Auto = InferInitialAuto(sim);
                h.Speed = InferInitialSpeed(sim);
            }
            return h;
        }

        public static BattleInitialHeader Copy(BattleInitialHeader src)
        {
            if (src == null) return null;
            return new BattleInitialHeader
            {
                Seed = src.Seed,
                RulesVersion = src.RulesVersion,
                Profile = src.Profile,
                Auto = src.Auto,
                Speed = src.Speed,
                ForceNoCrit = src.ForceNoCrit,
                LeaderSlot = src.LeaderSlot,
                PartyIds = BattleRunRecord.CopyIds(src.PartyIds),
                StageId = src.StageId,
                ClockIdentity = src.ClockIdentity,
                GrowthIdentity = src.GrowthIdentity,
                DataIdentity = src.DataIdentity,
                ContentFingerprint = src.ContentFingerprint,
                FrozenAtStart = src.FrozenAtStart
            };
        }

        static string GrowthKey(BattleSim sim)
        {
            if (sim == null || sim.Allies == null) return "";
            var sb = new StringBuilder(sim.Allies.Length * 32);
            for (int i = 0; i < sim.Allies.Length; i++)
            {
                var u = sim.Allies[i];
                if (i > 0) sb.Append('|');
                if (u == null) { sb.Append('-'); continue; }
                sb.Append(u.Def != null ? u.Def.Id ?? "" : "");
                sb.Append(':').Append(BattleStateDigest.I(u.Def != null ? u.Def.Hp : 0));
                sb.Append('/').Append(BattleStateDigest.I(u.Def != null ? u.Def.Atk : 0));
                sb.Append('+').Append(BattleStateDigest.I(u.ExtraAtk));
            }
            return sb.ToString();
        }

        static string SafeContentFingerprint()
        {
            try { return BattleSim.ContentFingerprint(); }
            catch (Exception) { return ""; }
        }

        static BattleInitialHeader ReadSimField(BattleSim sim)
        {
            if (sim == null) return null;
            var t = sim.GetType();
            foreach (var name in new[] { "InitialHeader", "Initial" })
            {
                var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (p != null && typeof(BattleInitialHeader).IsAssignableFrom(p.PropertyType))
                    return p.GetValue(sim) as BattleInitialHeader;
                var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (f != null && typeof(BattleInitialHeader).IsAssignableFrom(f.FieldType))
                    return f.GetValue(sim) as BattleInitialHeader;
            }
            return null;
        }

        static void WriteSimField(BattleSim sim, BattleInitialHeader h)
        {
            if (sim == null || h == null) return;
            var t = sim.GetType();
            foreach (var name in new[] { "InitialHeader", "Initial" })
            {
                var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (p != null && p.CanWrite && typeof(BattleInitialHeader).IsAssignableFrom(p.PropertyType))
                {
                    p.SetValue(sim, h);
                    return;
                }
                var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (f != null && typeof(BattleInitialHeader).IsAssignableFrom(f.FieldType))
                {
                    f.SetValue(sim, h);
                    return;
                }
            }
        }

        // Guessed opening Speed: first tick-0 SetSpeed, else 1 if any SetSpeed exists, else current.
        // R02: Speed=3 then SetSpeed=2 with no tick-0 command becomes 1 — freeze instead.
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
    }

    /// <summary>
    /// Captured run: header, copied <see cref="CommandRecord"/> list, final digest, event summaries.
    /// <see cref="ToJsonLines"/> is a hand-written JSON-lines writer (no extra packages).
    /// Replay policy is external-input (REGRESSIONS.md R01): Auto rows stay on the tape for
    /// observation but are not re-injected.
    /// </summary>
    public sealed class BattleRunRecord
    {
        public string RunHeader;
        public int Seed;
        public string RulesVersion = BattleSim.RulesVersion;
        public FormulaProfile Profile;
        public AutoMode Auto;
        public int Speed;
        public int LeaderSlot;
        public string[] PartyIds;
        public string StageId;
        public string DataIdentity;
        public string ContentFingerprint;
        public bool HeaderFrozen;
        public BattleInitialHeader Initial;
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
            var initial = BattleInitialHeader.ResolveForCapture(sim, partyIds, stageId);
            rec.Initial = initial;
            rec.Auto = initial.Auto;
            rec.Speed = initial.Speed;
            rec.LeaderSlot = initial.LeaderSlot;
            rec.PartyIds = BattleRunRecord.CopyIds(initial.PartyIds) ?? BattleRunRecord.IdsFromAllies(sim);
            rec.StageId = !string.IsNullOrEmpty(initial.StageId) ? initial.StageId : (stageId ?? "");
            rec.DataIdentity = initial.DataIdentity;
            rec.ContentFingerprint = initial.ContentFingerprint;
            rec.HeaderFrozen = initial.FrozenAtStart;
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
            J(sb, "leaderSlot", LeaderSlot);
            J(sb, "dataIdentity", DataIdentity);
            J(sb, "contentFingerprint", ContentFingerprint);
            J(sb, "headerFrozen", HeaderFrozen);
            J(sb, "forceNoCrit", Initial != null && Initial.ForceNoCrit);
            J(sb, "clockIdentity", Initial != null ? Initial.ClockIdentity : "");
            J(sb, "growthIdentity", Initial != null ? Initial.GrowthIdentity : "");
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
            rec.LeaderSlot = map.GetInt("leaderSlot", rec.LeaderSlot);
            rec.PartyIds = map.GetStringArray("partyIds");
            rec.StageId = map.Get("stageId", rec.StageId ?? "");
            rec.DataIdentity = map.Get("dataIdentity", rec.DataIdentity ?? "");
            rec.ContentFingerprint = map.Get("contentFingerprint", rec.ContentFingerprint ?? "");
            rec.HeaderFrozen = map.GetBool("headerFrozen");
            if (rec.Initial == null) rec.Initial = new BattleInitialHeader();
            rec.Initial.Seed = rec.Seed;
            rec.Initial.RulesVersion = rec.RulesVersion;
            rec.Initial.Profile = rec.Profile;
            rec.Initial.Auto = rec.Auto;
            rec.Initial.Speed = rec.Speed;
            rec.Initial.LeaderSlot = rec.LeaderSlot;
            rec.Initial.PartyIds = rec.PartyIds;
            rec.Initial.StageId = rec.StageId;
            rec.Initial.DataIdentity = rec.DataIdentity;
            rec.Initial.ContentFingerprint = rec.ContentFingerprint;
            rec.Initial.FrozenAtStart = rec.HeaderFrozen;
            rec.Initial.ForceNoCrit = map.GetBool("forceNoCrit");
            rec.Initial.ClockIdentity = map.Get("clockIdentity", rec.Initial.ClockIdentity ?? "");
            rec.Initial.GrowthIdentity = map.Get("growthIdentity", rec.Initial.GrowthIdentity ?? "");
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
            var initial = BattleInitialHeader.ResolveForCapture(sim, partyIds,
                stage != null && !string.IsNullOrEmpty(stage.Id) ? stage.Id : "");
            script.Header.Profile = initial.Profile;
            script.Header.Auto = initial.Auto;
            script.Header.Speed = initial.Speed;
            script.Header.RulesVersion = BattleSim.RulesVersion;
            script.Header.RunHeader = sim.RunHeader();
            script.Header.DataVersion = !string.IsNullOrEmpty(initial.DataIdentity)
                ? initial.DataIdentity
                : (DataVersionFromRunHeader(script.Header.RunHeader) ?? ResolveDataVersion());
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
                var id = BattleContentIdentity.Fingerprint();
                if (!string.IsNullOrEmpty(id)) return id;
            }
            catch (Exception)
            {
            }

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

    /// <summary>
    /// Per-Verify diagnostic lists owned by <see cref="ReplayReport"/>.
    /// A later Verify may replace <see cref="BattleReplayer.Divergences"/>; it cannot clear this snapshot.
    /// </summary>
    public sealed class ReplayDiagnosticSnapshot
    {
        public readonly List<string> Divergences = new List<string>();
        public readonly List<string> Unconsumed = new List<string>();
    }

    public sealed class ReplayReport
    {
        public BattleStateDigest OriginalDigest;
        public BattleStateDigest ReplayedDigest;
        public readonly List<string> DigestDiff = new List<string>();
        public readonly List<string> EventDiff = new List<string>();
        /// <summary>REGRESSIONS.md R03 — originally-accepted tape rows that Submit rejected on replay.</summary>
        public readonly List<string> CommandDiff = new List<string>();
        /// <summary>REGRESSIONS.md R04 — rules / data-identity / ForceNoCrit mismatches.</summary>
        public readonly List<string> VersionDiff = new List<string>();
        /// <summary>REGRESSIONS.md R03 — external accepted commands never submitted.</summary>
        public readonly List<string> Unconsumed = new List<string>();
        /// <summary>Owned copy of this Verify's command rejects / leftover tape rows.</summary>
        public readonly ReplayDiagnosticSnapshot Diagnostics = new ReplayDiagnosticSnapshot();
        /// <summary>-1 when event sequences match on the summary fields.</summary>
        public int FirstEventDivergence = -1;
        public int ExpectedEventCount;
        public int ActualEventCount;
        public BattleSim Replayed;

        // R03: reject / leftover / version mismatch cannot still Match.
        public bool Ok => DigestDiff.Count == 0
            && EventDiff.Count == 0
            && FirstEventDivergence < 0
            && CommandDiff.Count == 0
            && VersionDiff.Count == 0
            && Unconsumed.Count == 0;
        public bool Match => Ok;

        public IReadOnlyList<string> Diff
        {
            get
            {
                var all = new List<string>(
                    DigestDiff.Count + EventDiff.Count + CommandDiff.Count + VersionDiff.Count + Unconsumed.Count);
                for (int i = 0; i < DigestDiff.Count; i++) all.Add(DigestDiff[i]);
                for (int i = 0; i < EventDiff.Count; i++) all.Add(EventDiff[i]);
                for (int i = 0; i < CommandDiff.Count; i++) all.Add(CommandDiff[i]);
                for (int i = 0; i < VersionDiff.Count; i++) all.Add(VersionDiff[i]);
                for (int i = 0; i < Unconsumed.Count; i++) all.Add(Unconsumed[i]);
                return all;
            }
        }
    }

    /// <summary>
    /// External-input replay (REVIEW X05 / REGRESSIONS.md R01).
    /// Auto strategies regenerate inside <see cref="BattleSim.Tick"/> (AutoFire / TickFever).
    /// Auto actions still <see cref="BattleSim.Submit"/> on the live run for observation;
    /// the tape does <b>not</b> re-inject <see cref="CommandSource.Auto"/> rows.
    /// Do not mix half-replay half-regen.
    /// <para>
    /// Ordering: at live <see cref="BattleSim.TickIndex"/> T, every recorded accepted
    /// <i>external</i> command whose <see cref="CommandRecord.Tick"/> == T is submitted with
    /// <see cref="CommandSource.Replay"/>; only then is <see cref="BattleSim.Tick"/>
    /// called, which advances the sim to T+1 (unless paused or already finished).
    /// Header Speed/Auto are applied first via Submit(SetSpeed/SetAuto, Replay).
    /// Replay never sets <see cref="BattleSim.ForceNoCrit"/> (seeded crits stay on).
    /// </para>
    /// </summary>
    public static class BattleReplayer
    {
        /// <summary>
        /// Last completed Replay/Run/Verify on any thread (last-writer mirror).
        /// Concurrent tests that read these lists must serialize this assembly
        /// (<see cref="SharedDiagnosticStaticsRequireSerializedTests"/>). After Verify,
        /// use <see cref="ReplayReport.CommandDiff"/>, <see cref="ReplayReport.Unconsumed"/>,
        /// and <see cref="ReplayReport.Diagnostics"/> — those lists belong to the report.
        /// </summary>
        public static readonly List<string> Divergences = new List<string>();
        /// <summary>External accepted commands left on the tape after Replay/Run stopped.</summary>
        public static readonly List<string> Unconsumed = new List<string>();
        /// <summary>
        /// Shared static mirrors are last-writer. Hosts that read
        /// <see cref="Divergences"/> / <see cref="Unconsumed"/> after Verify must run
        /// this assembly serially. ReplayReport owns its copies after Verify returns.
        /// </summary>
        public const bool SharedDiagnosticStaticsRequireSerializedTests = true;

        [ThreadStatic]
        static ReplayDiagnosticSnapshot t_session;

        /// <summary>R01: Player / Fixture / Replay are re-injected. Auto is regenerated by Tick.</summary>
        public static bool IsExternalInput(CommandRecord rec)
        {
            return rec != null && rec.Accepted && rec.Source != CommandSource.Auto;
        }

        public static BattleSim Replay(BattleRunRecord rec, Func<int, BattleSim> factory)
        {
            ReplayDiagnosticSnapshot session;
            return ReplayCore(rec, factory, out session);
        }

        public static ReplayReport Verify(BattleRunRecord rec, Func<int, BattleSim> factory)
        {
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            ReplayDiagnosticSnapshot session;
            var replayed = ReplayCore(rec, factory, out session);
            var report = Compare(rec, replayed, session);
            report.Replayed = replayed;
            return report;
        }

        public static BattleSim Run(ReplayScript script, Func<BattleSim> factory, int maxTicks)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (script == null) throw new ArgumentNullException(nameof(script));
            var session = BeginSession();
            try
            {
                var sim = factory();
                if (sim == null) throw new InvalidOperationException("Replay factory returned null.");

                var h = script.Header;
                if (h != null)
                    ApplyHeader(sim, h.Speed, h.Auto, session);

                var external = new List<CommandRecord>(script.Commands.Count);
                for (int i = 0; i < script.Commands.Count; i++)
                {
                    var c = script.Commands[i];
                    if (IsExternalInput(c)) external.Add(c);
                }

                var cursor = 0;
                if (maxTicks < 0) maxTicks = 0;
                for (int step = 0; step < maxTicks; step++)
                {
                    DrainAt(sim, external, ref cursor, sim.TickIndex, session);
                    if (sim.Outcome != BattleOutcome.InProgress)
                        break;
                    sim.Tick();
                }
                DrainAt(sim, external, ref cursor, sim.TickIndex, session);
                CollectUnconsumed(external, cursor, session);
                return sim;
            }
            finally
            {
                PublishSession(session);
            }
        }

        public static ReplayReport Verify(BattleSim original, BattleSim replayed)
        {
            var rec = original != null
                ? BattleRunRecord.Capture(original, BattleRunRecord.IdsFromAllies(original), "")
                : null;
            return Compare(rec, replayed, null);
        }

        static BattleSim ReplayCore(BattleRunRecord rec, Func<int, BattleSim> factory, out ReplayDiagnosticSnapshot session)
        {
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            session = BeginSession();
            try
            {
                var sim = factory(rec.Seed);
                if (sim == null) throw new InvalidOperationException("Replay factory returned null.");
                // Seeded crits stay on; factory owns ForceNoCrit. Do not restore ForceNoCrit=true.

                ApplyHeader(sim, rec.Speed, rec.Auto, session);

                var external = new List<CommandRecord>(rec.Commands.Count);
                for (int i = 0; i < rec.Commands.Count; i++)
                {
                    var c = rec.Commands[i];
                    if (IsExternalInput(c)) external.Add(c);
                }

                var goal = rec.FinalDigest != null ? rec.FinalDigest.TickIndex : LastTick(external);
                var cursor = 0;
                var spins = 0;
                var spinLimit = Math.Max(goal, 0) + external.Count + 16;
                while (spins++ <= spinLimit)
                {
                    DrainAt(sim, external, ref cursor, sim.TickIndex, session);
                    if (sim.TickIndex >= goal)
                    {
                        DrainAt(sim, external, ref cursor, sim.TickIndex, session);
                        break;
                    }
                    var before = sim.TickIndex;
                    sim.Tick();
                    if (sim.TickIndex != before) continue;
                    DrainAt(sim, external, ref cursor, sim.TickIndex, session);
                    if (sim.TickIndex >= goal) break;
                    if (sim.Outcome != BattleOutcome.InProgress || sim.Paused)
                        break;
                }
                DrainAt(sim, external, ref cursor, sim.TickIndex, session);
                CollectUnconsumed(external, cursor, session);
                return sim;
            }
            finally
            {
                PublishSession(session);
            }
        }

        static ReplayDiagnosticSnapshot BeginSession()
        {
            var session = new ReplayDiagnosticSnapshot();
            t_session = session;
            return session;
        }

        static void PublishSession(ReplayDiagnosticSnapshot session)
        {
            Divergences.Clear();
            Unconsumed.Clear();
            if (session == null) return;
            for (int i = 0; i < session.Divergences.Count; i++)
                Divergences.Add(session.Divergences[i]);
            for (int i = 0; i < session.Unconsumed.Count; i++)
                Unconsumed.Add(session.Unconsumed[i]);
        }

        static ReplayDiagnosticSnapshot ActiveSession(ReplayDiagnosticSnapshot session)
        {
            return session ?? t_session;
        }

        static void NoteDivergence(ReplayDiagnosticSnapshot session, string line)
        {
            var dest = ActiveSession(session);
            if (dest != null) dest.Divergences.Add(line);
            else Divergences.Add(line);
        }

        static void ApplyHeader(BattleSim sim, int speed, AutoMode auto, ReplayDiagnosticSnapshot session)
        {
            if (sim == null) return;
            if (speed >= 1)
            {
                var r = sim.Submit(new BattleCommand
                {
                    Kind = BattleCommandKind.SetSpeed,
                    Value = speed,
                    Source = CommandSource.Replay
                });
                if (!r.Accepted)
                    NoteDivergence(session, "seq=" + BattleStateDigest.I(r.Seq)
                        + ";tick=" + BattleStateDigest.I(r.Tick)
                        + ";kind=" + BattleCommandKind.SetSpeed
                        + ";reject=" + r.Reason);
            }
            {
                var r = sim.Submit(new BattleCommand
                {
                    Kind = BattleCommandKind.SetAuto,
                    Value = (int)auto,
                    Source = CommandSource.Replay
                });
                if (!r.Accepted)
                    NoteDivergence(session, "seq=" + BattleStateDigest.I(r.Seq)
                        + ";tick=" + BattleStateDigest.I(r.Tick)
                        + ";kind=" + BattleCommandKind.SetAuto
                        + ";reject=" + r.Reason);
            }
        }

        static ReplayReport Compare(BattleRunRecord rec, BattleSim replayed, ReplayDiagnosticSnapshot session)
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

            CopyDiagnostics(report, session);
            AddVersionDiff(report, rec, replayed);
            return report;
        }

        static void CopyDiagnostics(ReplayReport report, ReplayDiagnosticSnapshot session)
        {
            var div = session != null ? session.Divergences : Divergences;
            var unc = session != null ? session.Unconsumed : Unconsumed;
            for (int i = 0; i < div.Count; i++)
            {
                report.CommandDiff.Add(div[i]);
                report.Diagnostics.Divergences.Add(div[i]);
            }
            for (int i = 0; i < unc.Count; i++)
            {
                report.Unconsumed.Add(unc[i]);
                report.Diagnostics.Unconsumed.Add(unc[i]);
            }
        }

        static void AddVersionDiff(ReplayReport report, BattleRunRecord rec, BattleSim replayed)
        {
            if (rec == null) return;
            if (!string.IsNullOrEmpty(rec.RulesVersion)
                && !string.Equals(rec.RulesVersion, BattleSim.RulesVersion, StringComparison.Ordinal))
            {
                report.VersionDiff.Add("RulesVersion: " + rec.RulesVersion + " != " + BattleSim.RulesVersion);
            }

            var expectedId = rec.DataIdentity;
            if (string.IsNullOrEmpty(expectedId) && rec.Initial != null)
                expectedId = rec.Initial.DataIdentity;
            if (!string.IsNullOrEmpty(expectedId) && replayed != null)
            {
                var actualId = BattleContentIdentity.Compute(replayed, rec.PartyIds, rec.StageId ?? "");
                if (!string.Equals(expectedId, actualId, StringComparison.Ordinal))
                    report.VersionDiff.Add("DataIdentity: tape != replayed");
            }

            if (rec.Initial != null && replayed != null && rec.Initial.ForceNoCrit != replayed.ForceNoCrit)
            {
                report.VersionDiff.Add("ForceNoCrit: "
                    + (rec.Initial.ForceNoCrit ? "1" : "0")
                    + " != "
                    + (replayed.ForceNoCrit ? "1" : "0"));
            }
        }

        static void DrainAt(BattleSim sim, List<CommandRecord> cmds, ref int cursor, int tick, ReplayDiagnosticSnapshot session)
        {
            if (cmds == null || sim == null) return;
            while (cursor < cmds.Count)
            {
                var rec = cmds[cursor];
                if (rec == null) { cursor++; continue; }
                if (rec.Tick > tick) break;
                cursor++;
                if (rec.Tick < tick || !IsExternalInput(rec)) continue;
                SubmitReplay(sim, rec, session);
            }
        }

        static void CollectUnconsumed(List<CommandRecord> cmds, int cursor, ReplayDiagnosticSnapshot session)
        {
            if (cmds == null) return;
            for (int i = cursor; i < cmds.Count; i++)
            {
                var rec = cmds[i];
                if (!IsExternalInput(rec)) continue;
                var line = "seq=" + BattleStateDigest.I(rec.Seq)
                    + ";tick=" + BattleStateDigest.I(rec.Tick)
                    + ";kind=" + rec.Kind
                    + ";unconsumed";
                var dest = ActiveSession(session);
                if (dest != null) dest.Unconsumed.Add(line);
                else Unconsumed.Add(line);
            }
        }

        static void SubmitReplay(BattleSim sim, CommandRecord rec, ReplayDiagnosticSnapshot session)
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
                NoteDivergence(session,
                    "seq=" + BattleStateDigest.I(rec.Seq)
                    + ";tick=" + BattleStateDigest.I(sim.TickIndex)
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
