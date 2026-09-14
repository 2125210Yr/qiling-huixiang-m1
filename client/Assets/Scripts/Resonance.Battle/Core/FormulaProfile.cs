using System;

namespace Resonance.Battle
{
    public enum FormulaProfile
    {
        GL_UNKNOWN = 0,
        JP_LEGACY_EMPIRICAL = 1,
        KR_LEGACY_REPORTED = 2
    }

    public enum FormulaStatus
    {
        Ok = 0,
        NotMeasured = 1
    }

    /// <summary>
    /// Provenance class of a formula branch (G2 review R06, API_CONTRACT §6).
    /// Computability and evidence are separate axes: a branch may produce a number
    /// (<see cref="FormulaResult.Computed"/>) while its evidence is only a design placeholder.
    /// </summary>
    public enum FormulaEvidence
    {
        /// <summary>No supporting evidence for this input (GL_UNKNOWN, or input outside a candidate's declared domain).</summary>
        Unknown = 0,
        /// <summary>Engineering preview coefficients chosen by this project (e.g. actual Drive, Fever channel constants).</summary>
        DesignPlaceholder = 1,
        /// <summary>Historical JP/KR candidate from DC_RECON_KIT/02_NUMERICS_AND_CALIBRATION.md; contrast only, not GL truth.</summary>
        HistoricalCandidate = 2,
        /// <summary>Measured against the GL original with recorded inputs. No branch currently qualifies.</summary>
        Measured = 3
    }

    /// <summary>
    /// How <see cref="FormulaResult.RequireInt"/> reads a <see cref="FormulaResult.Bounds"/> interval.
    /// The parameterless overload rejects; it must not collapse Min/Max onto the dummy scalar Value.
    /// </summary>
    public enum FormulaBoundsPolicy
    {
        Reject = 0,
        Min = 1,
        Max = 2,
        Midpoint = 3
    }

    public readonly struct FormulaResult
    {
        public readonly FormulaProfile Profile;
        public readonly FormulaStatus Status;
        public readonly string Code;
        public readonly double Value;
        public readonly double Min;
        public readonly double Max;
        public readonly bool HasBounds;
        /// <summary>Evidence class of the branch that produced this result. Never <see cref="FormulaEvidence.Measured"/> today.</summary>
        public readonly FormulaEvidence Evidence;

        /// <summary>
        /// True when a usable number exists: <see cref="Status"/> is Ok and either a finite <see cref="Value"/>
        /// or finite <see cref="Min"/>/<see cref="Max"/> bounds are present. Says nothing about evidence.
        /// </summary>
        public bool Computed
        {
            get
            {
                if (Status != FormulaStatus.Ok) return false;
                return HasBounds ? IsFinite(Min) && IsFinite(Max) : IsFinite(Value);
            }
        }

        /// <summary>
        /// True only when <see cref="Evidence"/> is <see cref="FormulaEvidence.Measured"/> (GL original).
        /// Callers that mean "has a number" must use <see cref="Computed"/>. BattleSim.TryResolveCombat
        /// already does. No current factory or Resolve branch can mint Measured evidence.
        /// </summary>
        public bool Measured => Evidence == FormulaEvidence.Measured;

        FormulaResult(FormulaProfile profile, FormulaStatus status, string code, double value, double min, double max, bool hasBounds, FormulaEvidence evidence)
        {
            Profile = profile;
            Status = status;
            Code = code;
            Value = value;
            Min = min;
            Max = max;
            HasBounds = hasBounds;
            Evidence = evidence;
        }

        /// <summary>Computed value with unspecified provenance; defaults to <see cref="FormulaEvidence.DesignPlaceholder"/>, never Measured.</summary>
        public static FormulaResult Ok(FormulaProfile profile, double value)
        {
            return Ok(profile, value, FormulaEvidence.DesignPlaceholder);
        }

        public static FormulaResult Ok(FormulaProfile profile, double value, FormulaEvidence evidence)
        {
            return new FormulaResult(profile, FormulaStatus.Ok, "OK", value, value, value, false, GuardEvidence(evidence));
        }

        public static FormulaResult Bounds(FormulaProfile profile, double min, double max)
        {
            return Bounds(profile, min, max, FormulaEvidence.DesignPlaceholder);
        }

        public static FormulaResult Bounds(FormulaProfile profile, double min, double max, FormulaEvidence evidence)
        {
            // Scalar Value is absent on an interval. RequireInt() must not treat this as 0.
            return new FormulaResult(profile, FormulaStatus.Ok, "OK", double.NaN, min, max, true, GuardEvidence(evidence));
        }

        public static FormulaResult NotMeasured(FormulaProfile profile)
        {
            return new FormulaResult(profile, FormulaStatus.NotMeasured, DamageMath.NotMeasuredCode, double.NaN, double.NaN, double.NaN, false, FormulaEvidence.Unknown);
        }

        public static FormulaResult DesignPlaceholder(FormulaProfile profile)
        {
            return new FormulaResult(profile, FormulaStatus.NotMeasured, DamageMath.DesignPlaceholderCode, double.NaN, double.NaN, double.NaN, false, FormulaEvidence.DesignPlaceholder);
        }

        /// <summary>
        /// Input lies outside the historical candidate's declared domain (e.g. pierce with DEF &gt; 20000).
        /// No value is produced and no extrapolation is performed; evidence is <see cref="FormulaEvidence.Unknown"/>
        /// because the candidate says nothing about this input.
        /// </summary>
        public static FormulaResult OutOfCandidateDomain(FormulaProfile profile)
        {
            return new FormulaResult(profile, FormulaStatus.NotMeasured, DamageMath.OutOfCandidateDomainCode, double.NaN, double.NaN, double.NaN, false, FormulaEvidence.Unknown);
        }

        public int RequireInt()
        {
            return RequireInt(FormulaBoundsPolicy.Reject);
        }

        public int RequireInt(FormulaBoundsPolicy bounds)
        {
            if (!Computed)
                throw new InvalidOperationException(Code);
            if (HasBounds)
            {
                if (bounds == FormulaBoundsPolicy.Min)
                    return RoundInt(Min);
                if (bounds == FormulaBoundsPolicy.Max)
                    return RoundInt(Max);
                if (bounds == FormulaBoundsPolicy.Midpoint)
                    return RoundInt((Min + Max) * 0.5);
                throw new InvalidOperationException(DamageMath.BoundsNeedPolicyCode);
            }
            return RoundInt(Value);
        }

        static FormulaEvidence GuardEvidence(FormulaEvidence evidence)
        {
            // FormulaProfiles.md forbids any *_FINAL_VERIFIED path; no GL-measured fixture exists yet.
            if (evidence == FormulaEvidence.Measured)
                throw new InvalidOperationException("No formula branch is measured against the GL original; tag as HistoricalCandidate or DesignPlaceholder.");
            return evidence;
        }

        static int RoundInt(double v)
        {
            return (int)Math.Round(v, MidpointRounding.AwayFromZero);
        }

        static bool IsFinite(double v)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v);
        }
    }
}
