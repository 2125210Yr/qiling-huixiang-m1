using System;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// P01/P02 — Measured is not Computed; Bounds.RequireInt must not silently return 0.
    /// Do not re-introduce Measured==Computed.
    /// </summary>
    public sealed class G2RecheckProvenanceTests
    {
        [Fact]
        public void P01_DesignPlaceholderDrive_ComputedTrue_MeasuredFalse()
        {
            var drive = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL,
                SkillType.Drive,
                1000, 1f, 707, 2500,
                Element.Fire, Element.Wood,
                false, 1f, 1f, 1f);

            Assert.True(drive.Computed);
            Assert.Equal(FormulaEvidence.DesignPlaceholder, drive.Evidence);
            Assert.NotEqual(FormulaEvidence.Measured, drive.Evidence);
            Assert.False(
                drive.Measured,
                "P01: DesignPlaceholder Drive must not report Measured=true (legacy alias of Computed)");
            Assert.True(drive.RequireInt() > 0);

            var tap = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL,
                SkillType.Tap,
                1000, 1f, 707, 2500,
                Element.Fire, Element.Wood,
                false, 1f, 1f, 1f);
            Assert.True(tap.Computed);
            Assert.Equal(FormulaEvidence.HistoricalCandidate, tap.Evidence);
            Assert.False(tap.Measured);
        }

        [Fact]
        public void P02_Bounds_RequireInt_MustNotSilentlyReturnZero()
        {
            var bounds = FormulaResult.Bounds(FormulaProfile.JP_LEGACY_EMPIRICAL, 10, 20);
            Assert.True(bounds.Computed);
            Assert.True(bounds.HasBounds);
            Assert.Equal(10, bounds.Min);
            Assert.Equal(20, bounds.Max);
            Assert.Equal(FormulaEvidence.DesignPlaceholder, bounds.Evidence);

            var thrown = Assert.Throws<InvalidOperationException>(() => bounds.RequireInt());
            Assert.False(string.IsNullOrEmpty(thrown.Message));
        }
    }
}
