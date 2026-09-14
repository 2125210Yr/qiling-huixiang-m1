using System;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// G2 review R06 / REGRESSION_MATRIX P01–P03: computability and evidence are separate axes.
    /// Nothing here claims GL fidelity; every number is HistoricalCandidate or DesignPlaceholder.
    /// </summary>
    public sealed class G2ReviewProvenanceTests
    {
        const int Atk = 1000;
        const float Coef = 1f;
        const int Flat = 707;
        const int Def = 2500;

        static FormulaResult R(FormulaProfile p, SkillType t, int def = Def, int pierce = 0, float percentAtk = 0f, float feverMul = 1f)
        {
            return DamageMath.Resolve(p, t, Atk, Coef, Flat, def, Element.Fire, Element.Wood, false, 1f, 1f, feverMul, percentAtk, pierce: pierce);
        }

        // P01 -----------------------------------------------------------------------------------

        [Fact]
        public void P01_ComputedValueDoesNotImplyEmpiricalEvidence()
        {
            // Actual Drive: the plan (02_NUMERICS §C.6) has no actual-Drive formula; ComputeDs coefficients are ours.
            var jpDrive = R(FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Drive);
            Assert.True(jpDrive.Computed);
            Assert.False(jpDrive.Measured);
            Assert.True(jpDrive.RequireInt() > 0);
            Assert.Equal(FormulaEvidence.DesignPlaceholder, jpDrive.Evidence);
            Assert.NotEqual(FormulaEvidence.Measured, jpDrive.Evidence);
            Assert.Equal(FormulaEvidence.DesignPlaceholder, DamageMath.EvidenceOf(FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Drive));
            Assert.Contains("design placeholder", DamageMath.BranchSource(FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Drive), StringComparison.OrdinalIgnoreCase);

            // Historical Tap candidate: computable AND carries a historical (not measured) evidence class.
            var jpTap = R(FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Tap);
            Assert.True(jpTap.Computed);
            Assert.False(jpTap.Measured);
            Assert.Equal(FormulaEvidence.HistoricalCandidate, jpTap.Evidence);
            Assert.Equal(FormulaEvidence.HistoricalCandidate, R(FormulaProfile.KR_LEGACY_REPORTED, SkillType.Tap).Evidence);
            Assert.Equal(FormulaEvidence.HistoricalCandidate, R(FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Slide).Evidence);
            Assert.Equal(FormulaEvidence.HistoricalCandidate, R(FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Auto).Evidence);

            // Fever channel coefficients are design constants.
            var jpFever = DamageMath.Resolve(
                FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Fever, Atk,
                DamageMath.FeverChannelAtkCoef, DamageMath.FeverChannelFlat, Def,
                Element.Fire, Element.Wood, false, 1f, 1f, DamageMath.FeverMul);
            Assert.True(jpFever.Computed);
            Assert.False(jpFever.Measured);
            Assert.Equal(FormulaEvidence.DesignPlaceholder, jpFever.Evidence);

            // Percent-ATK Tap/Slide variants are not in the candidate set: computable, design placeholder.
            var pctTap = R(FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Tap, percentAtk: 1f);
            Assert.True(pctTap.Computed);
            Assert.False(pctTap.Measured);
            Assert.Equal(FormulaEvidence.DesignPlaceholder, pctTap.Evidence);

            // GL_UNKNOWN strict path: no value, no evidence.
            var gl = R(FormulaProfile.GL_UNKNOWN, SkillType.Tap);
            Assert.False(gl.Computed);
            Assert.Equal(FormulaEvidence.Unknown, gl.Evidence);
            Assert.Equal(FormulaStatus.NotMeasured, gl.Status);
            Assert.Equal(DamageMath.NotMeasuredCode, gl.Code);
            Assert.False(gl.Measured);
            Assert.Throws<InvalidOperationException>(() => gl.RequireInt());
            Assert.Equal(FormulaEvidence.Unknown, DamageMath.EvidenceOf(FormulaProfile.GL_UNKNOWN, SkillType.Drive));

            // No (profile, type) branch anywhere reports Measured.
            foreach (FormulaProfile p in Enum.GetValues(typeof(FormulaProfile)))
                foreach (SkillType t in Enum.GetValues(typeof(SkillType)))
                {
                    Assert.NotEqual(FormulaEvidence.Measured, DamageMath.EvidenceOf(p, t));
                    var r = R(p, t);
                    Assert.NotEqual(FormulaEvidence.Measured, r.Evidence);
                    Assert.False(string.IsNullOrEmpty(DamageMath.BranchSource(p, t)));
                    // Frozen: Measured := Evidence==Measured. Design/historical numbers stay Computed.
                    Assert.False(r.Measured);
                    if (p == FormulaProfile.GL_UNKNOWN)
                        Assert.False(r.Computed);
                    else
                        Assert.True(r.Computed);
                }

            // The factories refuse to mint Measured evidence.
            Assert.Throws<InvalidOperationException>(() => FormulaResult.Ok(FormulaProfile.JP_LEGACY_EMPIRICAL, 1, FormulaEvidence.Measured));
            Assert.Throws<InvalidOperationException>(() => FormulaResult.Bounds(FormulaProfile.JP_LEGACY_EMPIRICAL, 1, 2, FormulaEvidence.Measured));

            // Untagged factories default to DesignPlaceholder, never Measured; bounds count as Computed.
            var untagged = FormulaResult.Ok(FormulaProfile.JP_LEGACY_EMPIRICAL, 5);
            Assert.Equal(FormulaEvidence.DesignPlaceholder, untagged.Evidence);
            Assert.True(untagged.Computed);
            Assert.False(untagged.Measured);
            var bounds = FormulaResult.Bounds(FormulaProfile.JP_LEGACY_EMPIRICAL, 10, 20, FormulaEvidence.HistoricalCandidate);
            Assert.True(bounds.Computed && bounds.HasBounds);
            Assert.False(bounds.Measured);
            Assert.Equal(10d, bounds.Min);
            Assert.Equal(20d, bounds.Max);
            Assert.Equal(DamageMath.BoundsNeedPolicyCode, Assert.Throws<InvalidOperationException>(() => bounds.RequireInt()).Message);
            Assert.Equal(10, bounds.RequireInt(FormulaBoundsPolicy.Min));
            Assert.Equal(20, bounds.RequireInt(FormulaBoundsPolicy.Max));
            Assert.Equal(15, bounds.RequireInt(FormulaBoundsPolicy.Midpoint));
            Assert.False(FormulaResult.DesignPlaceholder(FormulaProfile.JP_LEGACY_EMPIRICAL).Computed);
        }

        /// <summary>
        /// REGRESSIONS P02: interval results stay readable; RequireInt cannot silently return Value=0.
        /// </summary>
        [Fact]
        public void P02_BoundsRequireInt_RejectsSilentZero()
        {
            var bounds = FormulaResult.Bounds(FormulaProfile.JP_LEGACY_EMPIRICAL, 10, 20);
            Assert.True(bounds.Computed);
            Assert.True(bounds.HasBounds);
            Assert.Equal(10d, bounds.Min);
            Assert.Equal(20d, bounds.Max);
            Assert.False(bounds.Measured);
            Assert.Equal(FormulaEvidence.DesignPlaceholder, bounds.Evidence);
            Assert.Equal(DamageMath.BoundsNeedPolicyCode, Assert.Throws<InvalidOperationException>(() => bounds.RequireInt()).Message);
            Assert.Equal(10, bounds.RequireInt(FormulaBoundsPolicy.Min));
            Assert.Equal(20, bounds.RequireInt(FormulaBoundsPolicy.Max));
            Assert.Equal(15, bounds.RequireInt(FormulaBoundsPolicy.Midpoint));
        }

        // P02 -----------------------------------------------------------------------------------

        /// <summary>
        /// Decision: the original plan declares a KR Slide second decay max(0.7, 1-0.00005*D)
        /// (DC_RECON_KIT/02_NUMERICS_AND_CALIBRATION.md §C.2; tools/candidate_formulas.py slide_bounds),
        /// distinct from the KR Tap factor max(0.6, 1-0.00008*D) (§C.1; tap_base). The engine now applies
        /// both from their own constants. Rounding convention: channel value rounded, then decay, then rounded
        /// again (matches the pre-existing KR Tap branch); the Python reference is unrounded, so the
        /// cross-check against it allows ±1.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(2500)]
        [InlineData(6000)]
        [InlineData(20000)]
        public void P02_LegacyKrSlide_MatchesDeclaredCandidate(int def)
        {
            var jpTap = R(FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Tap, def).RequireInt();
            var krTap = R(FormulaProfile.KR_LEGACY_REPORTED, SkillType.Tap, def).RequireInt();
            var jpSlide = R(FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Slide, def).RequireInt();
            var krSlide = R(FormulaProfile.KR_LEGACY_REPORTED, SkillType.Slide, def).RequireInt();

            // Declared factors (02_NUMERICS §C.1/§C.2). Engine constants are float (pre-existing KR Tap convention),
            // so compare to 6 places rather than pretend double precision.
            var tapDecay = Math.Max(0.6, 1.0 - 0.00008 * def);
            var slideDecay = Math.Max(0.7, 1.0 - 0.00005 * def);
            Assert.Equal(tapDecay, DamageMath.KrLegacyDecay(SkillType.Tap, def), 6);
            Assert.Equal(slideDecay, DamageMath.KrLegacyDecay(SkillType.Slide, def), 6);

            // Same-rounding comparison: JP channel value -> KR factor -> AwayFromZero, floor 1.
            Assert.Equal(Math.Max(1, (int)Math.Round(jpTap * DamageMath.KrLegacyDecay(SkillType.Tap, def), MidpointRounding.AwayFromZero)), krTap);
            Assert.Equal(Math.Max(1, (int)Math.Round(jpSlide * DamageMath.KrLegacyDecay(SkillType.Slide, def), MidpointRounding.AwayFromZero)), krSlide);

            // Cross-check the KR Slide point (r = 0, Ae = 0) against the unrounded reference structure
            // (400*S + 120*Ae + 40*r)/(400 + 0.2*D) * (E+c) * max(0.7, 1-0.00005*D), S = engine SkillDmg.
            var skillDmg = DamageMath.SkillDmg(Atk, Coef, Flat);
            var reference = 400.0 * skillDmg / (400.0 + 0.2 * def) * 1.4 * slideDecay;
            Assert.InRange(krSlide, reference - 1.0, reference + 1.0);

            if (def == 0)
            {
                Assert.Equal(jpTap, krTap);
                Assert.Equal(jpSlide, krSlide);
            }
            else
            {
                Assert.True(krSlide < jpSlide);
                Assert.True(krTap < jpTap);
                // Slide is NOT the Tap factor: shallower slope and higher floor.
                Assert.True(slideDecay > tapDecay);
            }
            if (def >= 20000)
            {
                Assert.Equal(DamageMath.KrTapDecayFloor, DamageMath.KrLegacyDecay(SkillType.Tap, def));
                Assert.Equal(DamageMath.KrSlideDecayFloor, DamageMath.KrLegacyDecay(SkillType.Slide, def));
            }

            // No KR-specific decay is declared for other channels.
            Assert.Equal(1.0, DamageMath.KrLegacyDecay(SkillType.Auto, def));
            Assert.Equal(1.0, DamageMath.KrLegacyDecay(SkillType.Drive, def));
            Assert.Equal(1.0, DamageMath.KrLegacyDecay(SkillType.Fever, def));
            Assert.Equal(
                R(FormulaProfile.JP_LEGACY_EMPIRICAL, SkillType.Auto, def).RequireInt(),
                R(FormulaProfile.KR_LEGACY_REPORTED, SkillType.Auto, def).RequireInt());

            // Still only a historical candidate.
            Assert.Equal(FormulaEvidence.HistoricalCandidate, R(FormulaProfile.KR_LEGACY_REPORTED, SkillType.Slide, def).Evidence);
            Assert.Contains("0.00005", DamageMath.BranchSource(FormulaProfile.KR_LEGACY_REPORTED, SkillType.Slide));
            Assert.Contains("0.00008", DamageMath.BranchSource(FormulaProfile.KR_LEGACY_REPORTED, SkillType.Tap));
        }

        // P03 -----------------------------------------------------------------------------------

        [Fact]
        public void P03_PierceCandidate_RejectsUnsupportedDomain()
        {
            Assert.Equal(20000, DamageMath.PierceCandidateDefenseMax);
            Assert.Equal("OUT_OF_CANDIDATE_DOMAIN", DamageMath.OutOfCandidateDomainCode);
            // Raw fit unchanged inside the domain (WikiTsTruePierceAddsInsideBrackets fixture).
            Assert.Equal(650, DamageMath.TruePierce(1000, 2500));

            foreach (var profile in new[] { FormulaProfile.JP_LEGACY_EMPIRICAL, FormulaProfile.KR_LEGACY_REPORTED })
            {
                // Boundary is inclusive.
                var atMax = R(profile, SkillType.Tap, DamageMath.PierceCandidateDefenseMax, pierce: 1000);
                Assert.True(atMax.Computed);
                Assert.Equal(FormulaEvidence.HistoricalCandidate, atMax.Evidence);

                // Beyond the declared domain: refuse to extrapolate, no value, no evidence.
                var beyond = R(profile, SkillType.Tap, DamageMath.PierceCandidateDefenseMax + 1, pierce: 1000);
                Assert.False(beyond.Computed);
                Assert.False(beyond.Measured);
                Assert.Equal(FormulaStatus.NotMeasured, beyond.Status);
                Assert.Equal(DamageMath.OutOfCandidateDomainCode, beyond.Code);
                Assert.Equal(FormulaEvidence.Unknown, beyond.Evidence);
                Assert.Equal(profile, beyond.Profile);
                Assert.Throws<InvalidOperationException>(() => beyond.RequireInt());

                // Same DEF without pierce is unaffected: the domain restriction belongs to the pierce candidate only.
                var noPierce = R(profile, SkillType.Tap, DamageMath.PierceCandidateDefenseMax + 1);
                Assert.True(noPierce.Computed);

                // Applies to every channel that receives pierce.
                foreach (var t in new[] { SkillType.Slide, SkillType.Auto, SkillType.Drive, SkillType.Fever })
                    Assert.Equal(DamageMath.OutOfCandidateDomainCode, R(profile, t, 50000, pierce: 1).Code);
            }

            Assert.True(DamageMath.PierceInCandidateDomain(0, int.MaxValue));
            Assert.True(DamageMath.PierceInCandidateDomain(1, -5));
            Assert.True(DamageMath.PierceInCandidateDomain(1, 20000));
            Assert.False(DamageMath.PierceInCandidateDomain(1, 20001));

            // GL_UNKNOWN stays NOT_MEASURED even when out of domain; it never reaches the candidate.
            Assert.Equal(DamageMath.NotMeasuredCode, R(FormulaProfile.GL_UNKNOWN, SkillType.Tap, 50000, pierce: 1000).Code);
        }
    }
}
