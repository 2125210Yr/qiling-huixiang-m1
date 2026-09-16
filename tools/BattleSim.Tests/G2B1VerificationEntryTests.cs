using System;
using System.Collections.Generic;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// B1-A2 — VerificationCatalog.Apply binds the named strip substitute so
    /// StartBattleAt objects (DefaultParty + Catalog.Stages[0]) accept p0 Slide.
    /// Does not call BindStripDotFlame / PlayableSubstitutes.Bind.
    /// Does not implement Dot. Does not weaken A53 T01/T02.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2B1VerificationEntryTests
    {
        [Fact]
        public void Apply_BasicPlan_StartBattleAtObjects_SlideSlot0_Accepted()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var plan = VerificationRunPlan.Resolve("basic", null);
                    Assert.NotNull(plan);
                    Assert.NotNull(plan.Fights);
                    Assert.Equal(VerificationScenario.Basic.Name, plan.Fights[0].Name);

                    var note = VerificationCatalog.Apply(plan);
                    Assert.DoesNotContain("apply failed", note, StringComparison.Ordinal);
                    Assert.True(VerificationCatalog.Installed);
                    Assert.Contains(PlayableSubstitutes.StripDotFlame, VerificationCatalog.InstalledMode, StringComparison.Ordinal);
                    Assert.Contains(PlayableSubstitutes.StripDotFlame, note, StringComparison.Ordinal);

                    var slide = Catalog.MustSkill("C001_slide");
                    Assert.True(string.IsNullOrEmpty(slide.EffectId), "Apply must write StripDotFlame clones into the installed skill map");
                    Assert.True(Catalog.IsPlayable(slide), "C001_slide must be playable after named substitute");

                    Assert.True(Catalog.Stages != null && Catalog.Stages.Length > 0, "Apply must install the verification stage");
                    var stage = Catalog.Stages[0];
                    var sim = new BattleSim(Catalog.DefaultParty, 0, 4202, stage, null)
                    {
                        Speed = 1,
                        Auto = AutoMode.Manual,
                        ForceNoCrit = false,
                        Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
                    };
                    Assert.Equal("C001", sim.Allies[0] != null && sim.Allies[0].Def != null ? sim.Allies[0].Def.Id : "");
                    ChargeAll(sim);
                    var result = Submit(sim, BattleCommandKind.Slide, 0);
                    Assert.True(
                        result.Accepted,
                        "StartBattleAt objects must Accept p0 Slide after Apply. Reason=" + result.Reason);
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void RestoreBuiltin_RestoresC001SlideDotFlame_Unplayable()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var named = VerificationScenario.Named("np.basic.v1");
                    Assert.NotNull(named);
                    var plan = new VerificationRunPlan
                    {
                        Mode = named.Name,
                        Fights = new[] { named }
                    };
                    VerificationCatalog.Apply(plan);
                    Assert.True(VerificationCatalog.Installed);
                    Assert.True(string.IsNullOrEmpty(Catalog.MustSkill("C001_slide").EffectId));

                    VerificationCatalog.RestoreBuiltin();
                    Assert.False(VerificationCatalog.Installed);

                    var slide = Catalog.MustSkill("C001_slide");
                    Assert.Equal("dot_flame", slide.EffectId);
                    Assert.False(Catalog.IsPlayable(slide), "production C001_slide must stay unplayable after RestoreBuiltin");
                    Assert.False(
                        Catalog.TryGetPlayableSkill("C001_slide", out _),
                        "PlayableSkills must not keep verification-stripped C001_slide");
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void Apply_Installed_C001AndWaveDeclaredSkills_NoPlayableRosterViolations()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var note = VerificationCatalog.Apply(VerificationRunPlan.Resolve("basic", null));
                    Assert.Contains("roster=ok", note, StringComparison.Ordinal);

                    var c001 = Catalog.MustChar("C001");
                    AssertDeclaredFightKit(c001);
                    Assert.True(Catalog.IsPlayable(Catalog.MustSkill(c001.TapSkillId)), "C001 tap");
                    Assert.True(Catalog.IsPlayable(Catalog.MustSkill(c001.SlideSkillId)), "C001 slide");
                    Assert.True(Catalog.IsPlayable(Catalog.MustSkill(c001.DriveSkillId)), "C001 drive");
                    Assert.True(Catalog.IsPlayable(Catalog.MustSkill(c001.LeaderSkillId)), "C001 leader");

                    var stage = Catalog.Stages[0];
                    Assert.NotNull(stage);
                    foreach (var id in WaveIds(stage))
                    {
                        var enemy = Catalog.MustChar(id);
                        AssertDeclaredFightKit(enemy);
                    }

                    var gate = Catalog.EvaluatePartyPlayable(Catalog.DefaultParty, stage, null, null);
                    var blocking = gate.PlayableRosterViolations();
                    Assert.True(
                        blocking.Count == 0,
                        "installed DefaultParty + verification wave must have no playable-roster violations: " + gate.Summary());
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void BuildBuiltin_WithoutApply_C001SlideRemainsUnplayable()
        {
            lock (CatalogGate)
            {
                VerificationCatalog.RestoreBuiltin();
                RestoreBuiltinCatalog();
                try
                {
                    Catalog.BuildBuiltin();
                    Assert.False(VerificationCatalog.Installed, "BuildBuiltin must not install the verification substitute");
                    var slide = Catalog.MustSkill("C001_slide");
                    Assert.Equal("dot_flame", slide.EffectId);
                    Assert.False(Catalog.IsPlayable(slide));
                    Assert.False(Catalog.TryGetPlayableSkill("C001_slide", out _));
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        static void AssertDeclaredFightKit(CharacterDef ch)
        {
            Assert.NotNull(ch);
            Assert.False(string.IsNullOrEmpty(ch.TapSkillId), ch.Id + " missing Tap");
            Assert.False(string.IsNullOrEmpty(ch.SlideSkillId), ch.Id + " missing Slide");
            Assert.False(string.IsNullOrEmpty(ch.DriveSkillId), ch.Id + " missing Drive");
            Assert.False(string.IsNullOrEmpty(ch.LeaderSkillId), ch.Id + " missing Leader");
        }

        static IEnumerable<string> WaveIds(StageDef stage)
        {
            if (stage == null) yield break;
            if (stage.Wave0 != null)
            {
                for (int i = 0; i < stage.Wave0.Length; i++)
                    if (!string.IsNullOrEmpty(stage.Wave0[i])) yield return stage.Wave0[i];
            }
            if (stage.Wave1 != null)
            {
                for (int i = 0; i < stage.Wave1.Length; i++)
                    if (!string.IsNullOrEmpty(stage.Wave1[i])) yield return stage.Wave1[i];
            }
        }
    }
}
