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

    public readonly struct FormulaResult
    {
        public readonly FormulaProfile Profile;
        public readonly FormulaStatus Status;
        public readonly string Code;
        public readonly double Value;
        public readonly double Min;
        public readonly double Max;
        public readonly bool HasBounds;

        public bool Measured => Status == FormulaStatus.Ok && Code != DamageMath.NotMeasuredCode;

        FormulaResult(FormulaProfile profile, FormulaStatus status, string code, double value, double min, double max, bool hasBounds)
        {
            Profile = profile;
            Status = status;
            Code = code;
            Value = value;
            Min = min;
            Max = max;
            HasBounds = hasBounds;
        }

        public static FormulaResult Ok(FormulaProfile profile, double value)
        {
            return new FormulaResult(profile, FormulaStatus.Ok, "OK", value, value, value, false);
        }

        public static FormulaResult Bounds(FormulaProfile profile, double min, double max)
        {
            return new FormulaResult(profile, FormulaStatus.Ok, "OK", 0, min, max, true);
        }

        public static FormulaResult NotMeasured(FormulaProfile profile)
        {
            return new FormulaResult(profile, FormulaStatus.NotMeasured, DamageMath.NotMeasuredCode, double.NaN, double.NaN, double.NaN, false);
        }

        public static FormulaResult DesignPlaceholder(FormulaProfile profile)
        {
            return new FormulaResult(profile, FormulaStatus.NotMeasured, DamageMath.DesignPlaceholderCode, double.NaN, double.NaN, double.NaN, false);
        }

        public int RequireInt()
        {
            if (!Measured)
                throw new InvalidOperationException(Code);
            return (int)Math.Round(Value, MidpointRounding.AwayFromZero);
        }
    }
}
