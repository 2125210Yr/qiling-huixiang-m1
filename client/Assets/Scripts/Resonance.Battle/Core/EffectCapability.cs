using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Resonance.Battle
{
    public enum CapabilityState
    {
        Implemented = 0,
        Registered_NotImplemented = 1,
        Unknown = 2
    }

    public sealed class CapabilityRow
    {
        public string Opcode;
        public EffectKind Kind;
        public CapabilityState State;
        public string CoverageNote;
        public string Params;
        public string TestCoverage;
        public float MagnitudeMin = -100f;
        public float MagnitudeMax = 100f;
        public bool MagnitudeUnused;
    }

    public sealed class CapabilityVerdict
    {
        public CapabilityState State;
        public string Opcode;
        public EffectKind Kind;
        public string Reason;
        public readonly List<string> ParamErrors = new List<string>();

        public bool Ok => State == CapabilityState.Implemented && ParamErrors.Count == 0;

        public bool IsParamError => ParamErrors.Count > 0;
    }

    public sealed class ContentViolation
    {
        public string Id;
        public string Opcode;
        public EffectKind Kind;
        public string Reason;
        public CapabilityState State;
        public bool IsParamError;
        public bool OpcodeNotImplemented;

        /// <summary>
        /// Import-blocking: unknown opcode/combo is not enough to reject a
        /// round-trip of builtin JSON; only unknown opcodes, param errors,
        /// and opcodes with no Implemented kind are blocking.
        /// </summary>
        public bool BlocksExternalImport;
    }

    public sealed class ContentValidationReport
    {
        public readonly List<ContentViolation> Violations = new List<ContentViolation>();
        public bool Ok => Violations.Count == 0;

        public List<ContentViolation> BlockingViolations()
        {
            var list = new List<ContentViolation>();
            for (int i = 0; i < Violations.Count; i++)
            {
                if (Violations[i] != null && Violations[i].BlocksExternalImport)
                    list.Add(Violations[i]);
            }
            return list;
        }

        public string Summary()
        {
            if (Ok) return "CONTENT_OK";
            var sb = new StringBuilder();
            sb.Append("CONTENT_VALIDATION ").Append(Violations.Count).Append(" violation(s)");
            for (int i = 0; i < Violations.Count; i++)
            {
                var v = Violations[i];
                if (v == null) continue;
                sb.Append(" | id=").Append(v.Id ?? "")
                    .Append(" op=").Append(v.Opcode ?? "")
                    .Append(" kind=").Append(v.Kind)
                    .Append(" ").Append(v.Reason ?? "");
            }
            return sb.ToString();
        }
    }

    public sealed class ContentValidationException : InvalidOperationException
    {
        public readonly ContentValidationReport Report;

        public ContentValidationException(ContentValidationReport report)
            : base(report != null ? report.Summary() : "CONTENT_VALIDATION")
        {
            Report = report ?? new ContentValidationReport();
        }

        public ContentValidationException(string message)
            : base(message)
        {
            Report = new ContentValidationReport();
        }
    }

    /// <summary>
    /// Opcode + kind + parameter capability table (G2 R05).
    /// Evidence: ENGINEERING. Not GL fidelity.
    /// </summary>
    public static class EffectCapability
    {
        public const string TokenOnAction = "on_action";
        public const string TokenOnHitTaken = "on_hit_taken";
        public const string TokenPeriodic = "periodic";

        static readonly string[] AllowedTriggerTokens = { TokenOnAction, TokenOnHitTaken, TokenPeriodic };

        static readonly List<CapabilityRow> Rows = new List<CapabilityRow>();
        static readonly Dictionary<string, CapabilityRow> ByKey = new Dictionary<string, CapabilityRow>(StringComparer.Ordinal);

        // TODO(G2-R05): EffectDef.Trigger / PeriodSec live on Definitions.cs (integrator lease).
        // Resolve via reflection so this file compiles before or after those fields land.
        static readonly FieldInfo TriggerField = typeof(EffectDef).GetField("Trigger");
        static readonly FieldInfo PeriodField = typeof(EffectDef).GetField("PeriodSec");
        static readonly PropertyInfo TriggerProp = typeof(EffectDef).GetProperty("Trigger");
        static readonly PropertyInfo PeriodProp = typeof(EffectDef).GetProperty("PeriodSec");

        public static bool HasLifecycleFields =>
            TriggerField != null || TriggerProp != null || PeriodField != null || PeriodProp != null;

        public static bool HasTriggerField => TriggerField != null || TriggerProp != null;
        public static bool HasPeriodField => PeriodField != null || PeriodProp != null;

        static EffectCapability()
        {
            // Damage channels — BattleSim.SettleSkill / SettleFeverHit.
            Dmg(EffectOpcodes.DmgTap, "SettleSkill Tap channel. ENGINEERING, not GL.");
            Dmg(EffectOpcodes.DmgSlide, "SettleSkill Slide channel. ENGINEERING, not GL.");
            Dmg(EffectOpcodes.DmgAuto, "SettleSkill Auto channel. ENGINEERING, not GL.");
            Dmg(EffectOpcodes.DmgDriveActual, "SettleSkill Drive channel. ENGINEERING, not GL.");
            Dmg(EffectOpcodes.DmgFeverParts, "SettleFeverHit / TryFeverTap. ENGINEERING, not GL.");
            Add(EffectOpcodes.DmgPierce, EffectKind.Damage, CapabilityState.Registered_NotImplemented,
                "No pierce path in BattleSim.SettleSkill. ENGINEERING gap.");
            Add(EffectOpcodes.DmgExtraFlat, EffectKind.Damage, CapabilityState.Registered_NotImplemented,
                "No extra-flat channel in BattleSim.SettleSkill. ENGINEERING gap.");

            // status.apply — only kinds with a Magnitude/Has/SettleEffect consumer.
            StatusImpl(EffectKind.AtkBuff, "UnitState.Atk via Magnitude(AtkBuff). ENGINEERING, not GL.");
            StatusImpl(EffectKind.DefBuff, "UnitState.DefenseAgainst via Magnitude(DefBuff). ENGINEERING, not GL.");
            StatusImpl(EffectKind.DefDebuff, "UnitState.DefenseAgainst via Magnitude(DefDebuff). ENGINEERING, not GL.");
            StatusImpl(EffectKind.TsAmp, "ExtraDmg TsAmp. ENGINEERING, not GL.");
            StatusImpl(EffectKind.SsAmp, "ExtraDmg SsAmp. ENGINEERING, not GL.");
            StatusImpl(EffectKind.DsAmp, "ExtraDmg DsAmp. ENGINEERING, not GL.");
            StatusImpl(EffectKind.SkillDefDown, "ExtraDmg SkillDefDown. ENGINEERING, not GL.");
            StatusImpl(EffectKind.WeakDefDown, "ExtraDmg WeakDefDown. ENGINEERING, not GL.");

            StatusGap(EffectKind.Damage, "No Magnitude/Has consumer for Damage as a status.");
            StatusGap(EffectKind.Heal, "Heal is skill HealCoef/FlatHeal, not EffectKind.Heal.");
            StatusGap(EffectKind.Dot, "SettleEffect stores the status; ApplyPoisonTriggers only reads Poison/Bleed. No Dot tick.");
            StatusGap(EffectKind.Reflect, "No Reflect consumer in BattleSim.");
            StatusGap(EffectKind.Immortal, "No Immortal floor in ApplyDamage.");
            StatusGap(EffectKind.Burn, "Burn is not IsDotTriggerKind; no hit-taken consumer.");
            StatusGap(EffectKind.AntiHeal, "Heal() does not check AntiHeal.");
            StatusGap(EffectKind.DebuffBarrier, "Shield path only reads Shield/Barrier.");
            StatusGap(EffectKind.Enrage, "No Enrage store/release in BattleSim.");
            StatusGap(EffectKind.Overload, "No Overload consumer in BattleSim.");
            StatusGap(EffectKind.DualWield, "No DualWield Atk/Def split in BattleSim.");
            StatusGap(EffectKind.CooldownDelta, "Cooldown lives on slide_cd, not status.apply.");

            // Wrong-opcode rows so Check() rejects Silence-via-status.apply (E01) with a reason.
            Wrong(EffectOpcodes.StatusApply, EffectKind.Silence, EffectOpcodes.ControlApply);
            Wrong(EffectOpcodes.StatusApply, EffectKind.Stun, EffectOpcodes.ControlApply);
            Wrong(EffectOpcodes.StatusApply, EffectKind.Freeze, EffectOpcodes.ControlApply);
            Wrong(EffectOpcodes.StatusApply, EffectKind.Taunt, EffectOpcodes.ControlApply);
            Wrong(EffectOpcodes.StatusApply, EffectKind.Shield, EffectOpcodes.ShieldApply);
            Wrong(EffectOpcodes.StatusApply, EffectKind.Barrier, EffectOpcodes.ShieldApply);
            Wrong(EffectOpcodes.StatusApply, EffectKind.Poison, EffectOpcodes.PoisonApply);
            Wrong(EffectOpcodes.StatusApply, EffectKind.Bleed, EffectOpcodes.PoisonApply);
            Wrong(EffectOpcodes.StatusApply, EffectKind.ChargeHaste, EffectOpcodes.ChargeRate);
            Wrong(EffectOpcodes.StatusApply, EffectKind.ChargeSpeed, EffectOpcodes.ChargeRate);
            Wrong(EffectOpcodes.StatusApply, EffectKind.ChargeAmount, EffectOpcodes.ChargeAdd);

            Add(EffectOpcodes.StatusDispel, EffectKind.Damage, CapabilityState.Implemented,
                "SettleEffect → Dispel by Group (empty Group = every non-permanent). Releases shield remainder.");

            Add(EffectOpcodes.PoisonApply, EffectKind.Poison, CapabilityState.Implemented,
                "ApplyPoisonTriggers / IsDotTriggerKind. Default trigger on_action|on_hit_taken (design).");
            Add(EffectOpcodes.PoisonApply, EffectKind.Bleed, CapabilityState.Implemented,
                "ApplyPoisonTriggers / IsDotTriggerKind. Default trigger on_hit_taken (design).");

            Add(EffectOpcodes.ShieldApply, EffectKind.Shield, CapabilityState.Implemented,
                "ApplyControlAndShield. DurationSec<=0 = permanent until consumed/dispelled.");
            Add(EffectOpcodes.ShieldApply, EffectKind.Barrier, CapabilityState.Implemented,
                "ApplyControlAndShield Barrier. DurationSec<=0 = permanent until consumed/dispelled.");

            Add(EffectOpcodes.ControlApply, EffectKind.Stun, CapabilityState.Implemented,
                "ActionLocked + charge reset. ENGINEERING, not GL.");
            Add(EffectOpcodes.ControlApply, EffectKind.Freeze, CapabilityState.Implemented,
                "ActionLocked + charge reset. ENGINEERING, not GL.");
            Add(EffectOpcodes.ControlApply, EffectKind.Silence, CapabilityState.Implemented,
                "UnitState.SkillLocked => Has(Silence). Tap/Slide/Drive/FeverTap reject Silenced.");
            Add(EffectOpcodes.ControlApply, EffectKind.Taunt, CapabilityState.Implemented,
                "PickFrom preferTaunt / Has(Taunt). ENGINEERING, not GL.");

            Add(EffectOpcodes.ChargeAdd, EffectKind.ChargeAmount, CapabilityState.Implemented,
                "SettleEffect adds Magnitude to UnitState.Charge.");
            Add(EffectOpcodes.ChargeRate, EffectKind.ChargeHaste, CapabilityState.Implemented,
                "ChargeSpeedMul reads Magnitude(ChargeHaste).");
            Add(EffectOpcodes.ChargeRate, EffectKind.ChargeSpeed, CapabilityState.Implemented,
                "ChargeSpeedMul reads Magnitude(ChargeSpeed).");

            Add(EffectOpcodes.SlideCd, EffectKind.CooldownDelta, CapabilityState.Implemented,
                "SettleEffect writes UnitState.SlideCd from DurationSec/Magnitude.");

            Add(EffectOpcodes.Retarget, EffectKind.Damage, CapabilityState.Registered_NotImplemented,
                "No retarget consumer in BattleSim.");
            Add(EffectOpcodes.Revive, EffectKind.Damage, CapabilityState.Registered_NotImplemented,
                "No revive consumer in BattleSim.");
            Add(EffectOpcodes.Revive, EffectKind.Heal, CapabilityState.Registered_NotImplemented,
                "No revive consumer in BattleSim.");
        }

        static void Dmg(string opcode, string note)
        {
            var row = Add(opcode, EffectKind.Damage, CapabilityState.Implemented, note);
            row.MagnitudeUnused = true;
        }

        static void StatusImpl(EffectKind kind, string note)
        {
            Add(EffectOpcodes.StatusApply, kind, CapabilityState.Implemented, note);
        }

        static void StatusGap(EffectKind kind, string note)
        {
            Add(EffectOpcodes.StatusApply, kind, CapabilityState.Registered_NotImplemented, note);
        }

        static void Wrong(string opcode, EffectKind kind, string useOpcode)
        {
            Add(opcode, kind, CapabilityState.Registered_NotImplemented,
                "Unsupported combo; use " + useOpcode + " for " + kind + ".");
        }

        static CapabilityRow Add(string opcode, EffectKind kind, CapabilityState state, string note)
        {
            var row = new CapabilityRow
            {
                Opcode = opcode,
                Kind = kind,
                State = state,
                CoverageNote = note,
                Params = DefaultParamRules(opcode, kind),
                TestCoverage = CoverageFor(opcode, kind, state)
            };
            Rows.Add(row);
            ByKey[Key(opcode, kind)] = row;
            return row;
        }

        static string DefaultParamRules(string opcode, EffectKind kind)
        {
            var shield = string.Equals(opcode, EffectOpcodes.ShieldApply, StringComparison.Ordinal)
                || kind == EffectKind.Shield || kind == EffectKind.Barrier;
            var mag = string.Equals(opcode, EffectOpcodes.ChargeAdd, StringComparison.Ordinal)
                ? "Magnitude finite charge points [-100,100]"
                : "Magnitude finite [-100,100]";
            var dur = shield
                ? "DurationSec<=0 permanent-until-consumed (shield)"
                : "DurationSec<=0 permanent-until-consumed/dispelled";
            return mag + "; " + dur + "; MaxStack>=1; Trigger tokens on_action|on_hit_taken|periodic joined by '|'; PeriodSec>0 if Trigger contains periodic";
        }

        static string CoverageFor(string opcode, EffectKind kind, CapabilityState state)
        {
            if (opcode == EffectOpcodes.StatusApply && kind == EffectKind.Silence)
                return "G2ReviewCapabilityTests.UnsupportedEffectKindRejectedBeforePlay (E01)";
            if (opcode == EffectOpcodes.Revive)
                return "G2ReviewCapabilityTests.UnsupportedEffectKindRejectedBeforePlay (E01); M1RepairQaTests.UnimplementedOpcodeStillFails";
            if (opcode == EffectOpcodes.DmgPierce || opcode == EffectOpcodes.DmgExtraFlat)
                return "M1EffectOrderTests.UnknownPierceStillFails; M1RepairQaTests.UnimplementedOpcodeStillFails";
            if (opcode == EffectOpcodes.Retarget)
                return "M1RepairQaTests.UnimplementedOpcodeStillFails";
            if (opcode == EffectOpcodes.PoisonApply)
                return "M1EffectOrderTests.PoisonOnActionChangesHpOnJpProfile; G2ReviewCapabilityTests (periodic PeriodSec)";
            if (opcode == EffectOpcodes.ShieldApply)
                return "M1EffectOrderTests.ShieldAbsorbsIncomingDamage; M1RepairQaTests.KnownOpcodeSettlesShieldStunAndTaunt";
            if (opcode == EffectOpcodes.ControlApply)
                return "M1RepairQaTests.KnownOpcodeSettlesShieldStunAndTaunt; M1CoreSliceTests.ControlPausesChargeButSlideCdContinues";
            if (state == CapabilityState.Implemented)
                return "G2ReviewCapabilityTests.DefaultContentPassesValidation; MatrixCoversEveryKnownOpcode";
            return "G2ReviewCapabilityTests.MatrixCoversEveryKnownOpcode; EveryEffectKindAppearsInMatrix";
        }

        static string Key(string opcode, EffectKind kind)
        {
            return (opcode ?? "") + "\t" + ((int)kind).ToString(CultureInfo.InvariantCulture);
        }

        public static IEnumerable<CapabilityRow> Matrix()
        {
            return Rows;
        }

        public static bool OpcodeHasImplementation(string opcode)
        {
            if (string.IsNullOrEmpty(opcode)) return false;
            for (int i = 0; i < Rows.Count; i++)
            {
                var r = Rows[i];
                if (r != null && r.State == CapabilityState.Implemented
                    && string.Equals(r.Opcode, opcode, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public static CapabilityVerdict Check(EffectDef fx)
        {
            var v = new CapabilityVerdict();
            if (fx == null)
            {
                v.State = CapabilityState.Unknown;
                v.Reason = "effect is null";
                return v;
            }

            v.Opcode = fx.Opcode;
            v.Kind = fx.Kind;

            if (string.IsNullOrEmpty(fx.Opcode))
            {
                v.State = CapabilityState.Unknown;
                v.Reason = "empty opcode";
                return v;
            }

            if (!EffectOpcodes.IsKnown(fx.Opcode))
            {
                v.State = CapabilityState.Unknown;
                v.Reason = "unknown opcode";
                return v;
            }

            if (!IsDefinedKind(fx.Kind))
            {
                v.State = CapabilityState.Unknown;
                v.Reason = "unknown EffectKind " + ((int)fx.Kind).ToString(CultureInfo.InvariantCulture);
                return v;
            }

            CapabilityRow row;
            if (!ByKey.TryGetValue(Key(fx.Opcode, fx.Kind), out row) || row == null)
            {
                v.State = CapabilityState.Unknown;
                v.Reason = "unregistered (opcode, kind) combo";
                return v;
            }

            v.State = row.State;
            v.Reason = row.State == CapabilityState.Implemented
                ? row.CoverageNote
                : (string.IsNullOrEmpty(row.CoverageNote) ? "registered but not implemented" : row.CoverageNote);

            AppendParamErrors(fx, row, v.ParamErrors);
            if (v.ParamErrors.Count > 0 && string.IsNullOrEmpty(v.Reason))
                v.Reason = v.ParamErrors[0];
            return v;
        }

        public static CapabilityVerdict CheckSkill(SkillDef sk)
        {
            var v = new CapabilityVerdict();
            if (sk == null)
            {
                v.State = CapabilityState.Unknown;
                v.Reason = "skill is null";
                return v;
            }

            v.Opcode = sk.Opcode;
            if (string.IsNullOrEmpty(sk.Opcode))
            {
                v.State = CapabilityState.Unknown;
                v.Reason = "empty opcode";
                return v;
            }

            if (!EffectOpcodes.IsKnown(sk.Opcode))
            {
                v.State = CapabilityState.Unknown;
                v.Reason = "unknown opcode";
                return v;
            }

            // Skill opcodes are channel-level; EffectKind lives on the linked effect.
            if (OpcodeHasImplementation(sk.Opcode))
            {
                v.State = CapabilityState.Implemented;
                v.Reason = "skill channel opcode has an Implemented registry row";
            }
            else
            {
                v.State = CapabilityState.Registered_NotImplemented;
                v.Reason = "skill opcode has no Implemented (opcode, kind) row";
            }

            if (!string.IsNullOrEmpty(sk.EffectId))
            {
                var fx = Catalog.TryEffect(sk.EffectId);
                if (fx == null)
                {
                    v.ParamErrors.Add("param: missing effect '" + sk.EffectId + "'");
                    if (v.State == CapabilityState.Implemented)
                        v.Reason = "linked effect not in catalog";
                }
                else
                {
                    var ev = Check(fx);
                    if (!ev.Ok)
                    {
                        if (ev.State != CapabilityState.Implemented)
                            v.State = ev.State;
                        for (int i = 0; i < ev.ParamErrors.Count; i++)
                            v.ParamErrors.Add(ev.ParamErrors[i]);
                        if (v.State != CapabilityState.Implemented)
                            v.Reason = "linked effect " + sk.EffectId + ": " + ev.Reason;
                    }
                }
            }

            return v;
        }

        public static void RejectUnplayable(EffectDef fx)
        {
            var verdict = Check(fx);
            if (verdict.Ok) return;
            var report = new ContentValidationReport();
            report.Violations.Add(ToViolation(fx != null ? fx.Id : "", verdict));
            throw new ContentValidationException(report);
        }

        public static void RejectUnplayable(SkillDef sk)
        {
            var verdict = CheckSkill(sk);
            if (verdict.Ok) return;
            var report = new ContentValidationReport();
            report.Violations.Add(ToViolation(sk != null ? sk.Id : "", verdict));
            throw new ContentValidationException(report);
        }

        public static ContentValidationReport ValidateCatalog(
            IReadOnlyDictionary<string, EffectDef> effects,
            IReadOnlyDictionary<string, SkillDef> skills)
        {
            var report = new ContentValidationReport();
            if (effects != null)
            {
                foreach (var kv in effects)
                {
                    var fx = kv.Value;
                    if (fx == null) continue;
                    var verdict = Check(fx);
                    if (verdict.Ok) continue;
                    var id = string.IsNullOrEmpty(fx.Id) ? kv.Key : fx.Id;
                    report.Violations.Add(ToViolation(id, verdict));
                }
            }

            if (skills != null)
            {
                foreach (var kv in skills)
                {
                    var sk = kv.Value;
                    if (sk == null) continue;
                    var verdict = CheckSkill(sk);
                    if (verdict.Ok) continue;
                    var id = string.IsNullOrEmpty(sk.Id) ? kv.Key : sk.Id;
                    report.Violations.Add(ToViolation(id, verdict));
                }
            }

            return report;
        }

        public static ContentValidationReport ValidateBuffs(BuffDef[] buffs)
        {
            var report = new ContentValidationReport();
            if (buffs == null) return report;
            for (int i = 0; i < buffs.Length; i++)
            {
                var b = buffs[i];
                if (b == null) continue;
                var fx = new EffectDef
                {
                    Id = b.Id,
                    Opcode = EffectOpcodes.ForKind(b.Kind),
                    Kind = b.Kind,
                    Magnitude = 1f,
                    DurationSec = 1f,
                    MaxStack = 1
                };
                var verdict = Check(fx);
                if (verdict.Ok) continue;
                report.Violations.Add(ToViolation(b.Id, verdict));
            }
            return report;
        }

        public static void ThrowIfExternalImportInvalid(ContentValidationReport report)
        {
            if (report == null) return;
            var blocking = report.BlockingViolations();
            if (blocking.Count == 0) return;
            var filtered = new ContentValidationReport();
            for (int i = 0; i < blocking.Count; i++)
                filtered.Violations.Add(blocking[i]);
            throw new ContentValidationException(filtered);
        }

        static ContentViolation ToViolation(string id, CapabilityVerdict verdict)
        {
            var opcode = verdict != null ? verdict.Opcode : "";
            var v = new ContentViolation
            {
                Id = id ?? "",
                Opcode = opcode ?? "",
                Kind = verdict != null ? verdict.Kind : 0,
                Reason = FormatReason(verdict),
                State = verdict != null ? verdict.State : CapabilityState.Unknown,
                IsParamError = verdict != null && verdict.IsParamError,
                OpcodeNotImplemented = !string.IsNullOrEmpty(opcode)
                    && EffectOpcodes.IsKnown(opcode)
                    && !OpcodeHasImplementation(opcode)
            };
            v.BlocksExternalImport = v.IsParamError
                || v.State == CapabilityState.Unknown && !EffectOpcodes.IsKnown(v.Opcode)
                || v.OpcodeNotImplemented
                || string.IsNullOrEmpty(v.Opcode);
            return v;
        }

        static string FormatReason(CapabilityVerdict verdict)
        {
            if (verdict == null) return "unknown";
            if (verdict.ParamErrors.Count == 0) return verdict.Reason ?? "";
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(verdict.Reason))
                sb.Append(verdict.Reason).Append("; ");
            for (int i = 0; i < verdict.ParamErrors.Count; i++)
            {
                if (i > 0) sb.Append("; ");
                sb.Append(verdict.ParamErrors[i]);
            }
            return sb.ToString();
        }

        static void AppendParamErrors(EffectDef fx, CapabilityRow row, List<string> errors)
        {
            if (fx == null || errors == null) return;

            if (float.IsNaN(fx.Magnitude) || float.IsInfinity(fx.Magnitude))
                errors.Add("param: Magnitude must be finite");
            else if (row != null && !row.MagnitudeUnused
                && (fx.Magnitude < row.MagnitudeMin || fx.Magnitude > row.MagnitudeMax))
            {
                errors.Add("param: Magnitude " + fx.Magnitude.ToString(CultureInfo.InvariantCulture)
                    + " outside [" + row.MagnitudeMin.ToString(CultureInfo.InvariantCulture)
                    + "," + row.MagnitudeMax.ToString(CultureInfo.InvariantCulture) + "]");
            }

            if (float.IsNaN(fx.DurationSec) || float.IsInfinity(fx.DurationSec))
                errors.Add("param: DurationSec must be finite");
            // DurationSec <= 0 = permanent-until-consumed/dispelled (shields and other statuses).

            if (fx.MaxStack < 1)
                errors.Add("param: MaxStack must be >= 1");

            // TODO(G2-R05): skip Trigger/PeriodSec when Definitions.cs does not yet declare them.
            if (!HasLifecycleFields) return;

            var trigger = ReadTrigger(fx);
            if (!string.IsNullOrEmpty(trigger))
            {
                var tokens = trigger.Split('|');
                var hasPeriodic = false;
                for (int i = 0; i < tokens.Length; i++)
                {
                    var tok = tokens[i] != null ? tokens[i].Trim() : "";
                    if (tok.Length == 0) continue;
                    if (!IsAllowedTriggerToken(tok))
                        errors.Add("param: Trigger token '" + tok + "' not in on_action|on_hit_taken|periodic");
                    if (string.Equals(tok, TokenPeriodic, StringComparison.Ordinal))
                        hasPeriodic = true;
                }

                if (hasPeriodic)
                {
                    if (!HasPeriodField)
                    {
                        errors.Add("param: Trigger contains periodic but EffectDef.PeriodSec is not present");
                    }
                    else
                    {
                        var period = ReadPeriodSec(fx);
                        if (float.IsNaN(period) || float.IsInfinity(period) || period <= 0f)
                            errors.Add("param: PeriodSec must be > 0 when Trigger contains periodic");
                    }
                }
            }
        }

        static bool IsAllowedTriggerToken(string token)
        {
            for (int i = 0; i < AllowedTriggerTokens.Length; i++)
            {
                if (string.Equals(AllowedTriggerTokens[i], token, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        static bool IsDefinedKind(EffectKind kind)
        {
            return Enum.IsDefined(typeof(EffectKind), kind);
        }

        public static string ReadTrigger(EffectDef fx)
        {
            if (fx == null) return "";
            if (TriggerField != null)
                return TriggerField.GetValue(fx) as string ?? "";
            if (TriggerProp != null)
                return TriggerProp.GetValue(fx) as string ?? "";
            return "";
        }

        public static float ReadPeriodSec(EffectDef fx)
        {
            if (fx == null) return 0f;
            if (PeriodField != null)
                return ConvertToFloat(PeriodField.GetValue(fx));
            if (PeriodProp != null)
                return ConvertToFloat(PeriodProp.GetValue(fx));
            return 0f;
        }

        public static void WriteTrigger(EffectDef fx, string value)
        {
            if (fx == null) return;
            if (TriggerField != null) { TriggerField.SetValue(fx, value ?? ""); return; }
            if (TriggerProp != null && TriggerProp.CanWrite) TriggerProp.SetValue(fx, value ?? "");
        }

        public static void WritePeriodSec(EffectDef fx, float value)
        {
            if (fx == null) return;
            if (PeriodField != null) { PeriodField.SetValue(fx, value); return; }
            if (PeriodProp != null && PeriodProp.CanWrite) PeriodProp.SetValue(fx, value);
        }

        public static void CopyLifecycle(EffectDef src, EffectDef dst)
        {
            if (src == null || dst == null || !HasLifecycleFields) return;
            if (HasTriggerField) WriteTrigger(dst, ReadTrigger(src));
            if (HasPeriodField) WritePeriodSec(dst, ReadPeriodSec(src));
        }

        static float ConvertToFloat(object v)
        {
            if (v == null) return 0f;
            try { return Convert.ToSingle(v, CultureInfo.InvariantCulture); }
            catch { return 0f; }
        }

        public static string FormatMatrixMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Effect capability matrix");
            sb.AppendLine();
            sb.AppendLine("**ENGINEERING only — not GL fidelity.**");
            sb.AppendLine();
            sb.AppendLine("- `M1_FIDELITY=DEFERRED_NOT_REMOVED`");
            sb.AppendLine("- Evidence: G2 Path A / AUDIT R05 / API_CONTRACT §5. Rows are registry claims plus BattleSim consumer inspection, not original-game measurements.");
            sb.AppendLine("- Source: `EffectCapability.Matrix()` keyed by `(opcode, EffectKind)`.");
            sb.AppendLine("- `DurationSec <= 0` means permanent-until-consumed/dispelled (shields included).");
            sb.AppendLine("- `Trigger` tokens: `on_action` | `on_hit_taken` | `periodic`. `periodic` requires `PeriodSec > 0` when those fields exist on `EffectDef`.");
            sb.AppendLine();
            sb.AppendLine("| Opcode | Kind | State | Params | Test coverage |");
            sb.AppendLine("|---|---|---|---|---|");
            for (int i = 0; i < Rows.Count; i++)
            {
                var r = Rows[i];
                if (r == null) continue;
                sb.Append("| `").Append(r.Opcode).Append("` | ")
                    .Append(r.Kind).Append(" | ")
                    .Append(r.State).Append(" | ")
                    .Append(EscapeMd(r.Params)).Append(" | ")
                    .Append(EscapeMd(r.TestCoverage)).Append(" |")
                    .AppendLine();
            }
            return sb.ToString();
        }

        static string EscapeMd(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
