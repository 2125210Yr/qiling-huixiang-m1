using System;
using System.Collections.Generic;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// V01–V03 — playable import gate, atomic Load, candidate-snapshot validation.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2RecheckImportGateTests
    {
        const string ReflectId = "g2r_reflect";
        const string MarkerA = "g2r_a_marker";
        const string BadB = "g2r_b_bad";
        const string MarkerName = "G2R14-A-MARKER";
        const string CandOk = "g2r_cand_ok";
        const string CandSkillOk = "g2r_sk_ok";
        const string CandSkillSteal = "g2r_sk_steal";

        [Fact]
        public void V01_StatusApplyReflect_BlockedFromPlayableImport()
        {
            var fx = new EffectDef
            {
                Id = ReflectId,
                Opcode = EffectOpcodes.StatusApply,
                Kind = EffectKind.Reflect,
                Magnitude = 0.20f,
                DurationSec = 8f,
                MaxStack = 1,
                SourceTier = 1,
                Group = "reflect"
            };
            var verdict = EffectCapability.Check(fx);
            Assert.False(verdict.Ok, "V01: status.apply+Reflect must not be Implemented");
            Assert.Equal(EffectOpcodes.StatusApply, verdict.Opcode);
            Assert.Equal(EffectKind.Reflect, verdict.Kind);
            Assert.Throws<ContentValidationException>(() => EffectCapability.RejectUnplayable(fx));

            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var json =
                        "{\"effects\":[{\"id\":\"" + ReflectId + "\",\"op\":\"" + EffectOpcodes.StatusApply +
                        "\",\"kind\":" + (int)EffectKind.Reflect +
                        ",\"mag\":0.2,\"dur\":8,\"stack\":1,\"tier\":1,\"group\":\"reflect\"}]}";

                    ContentValidationException caught = null;
                    try
                    {
                        CatalogJson.Load(json);
                    }
                    catch (ContentValidationException ex)
                    {
                        caught = ex;
                    }

                    Assert.True(
                        caught != null,
                        "V01: CatalogJson.Load must reject known opcode + unimplemented kind (status.apply+Reflect), not only unknown opcodes");
                    var summary = caught.Report != null ? caught.Report.Summary() : caught.Message;
                    Assert.Contains(ReflectId, summary, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(EffectOpcodes.StatusApply, summary, StringComparison.Ordinal);
                    Assert.Contains("Reflect", summary, StringComparison.Ordinal);

                    Assert.Null(Catalog.TryEffect(ReflectId));
                    var playable = Catalog.ValidateContent();
                    for (int i = 0; i < playable.Violations.Count; i++)
                    {
                        var v = playable.Violations[i];
                        if (v != null && string.Equals(v.Id, ReflectId, StringComparison.Ordinal))
                            Assert.Fail("V01: " + ReflectId + " entered the playable/active catalog");
                    }
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void V02_FailedImport_LeavesActiveCatalogA_NotBuiltinOrPartialB()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var builtinFp = BattleSim.ContentFingerprint();
                    var builtinName = Catalog.MustChar("C001").Name;

                    CatalogJson.Load(
                        "{\"effects\":[{\"id\":\"" + MarkerA + "\",\"op\":\"" + EffectOpcodes.StatusApply +
                        "\",\"kind\":" + (int)EffectKind.AtkBuff +
                        ",\"mag\":0.1,\"dur\":4,\"stack\":1,\"tier\":1,\"group\":\"g2r_a\"}]," +
                        "\"chars\":[{\"id\":\"C001\",\"name\":\"" + MarkerName + "\"}]}");

                    Assert.NotNull(Catalog.TryEffect(MarkerA));
                    Assert.Equal(MarkerName, Catalog.MustChar("C001").Name);
                    var aFp = BattleSim.ContentFingerprint();
                    Assert.NotEqual(builtinFp, aFp);

                    Exception caught = null;
                    try
                    {
                        CatalogJson.Load(
                            "{\"effects\":[{\"id\":\"" + BadB + "\",\"op\":\"" + EffectOpcodes.Revive +
                            "\",\"kind\":0,\"mag\":1,\"dur\":0,\"stack\":1,\"tier\":1,\"group\":\"revive\"}]}");
                    }
                    catch (Exception ex)
                    {
                        caught = ex;
                    }

                    Assert.True(caught != null, "V02: import B with unknown/unplayable opcode must throw");
                    Assert.NotNull(Catalog.TryEffect(MarkerA));
                    Assert.Equal(MarkerName, Catalog.MustChar("C001").Name);
                    Assert.Null(Catalog.TryEffect(BadB));
                    Assert.NotEqual(builtinName, Catalog.MustChar("C001").Name);
                    Assert.Equal(aFp, BattleSim.ContentFingerprint());
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void V03_CandidateSnapshot_DoesNotStealFromLiveCatalog()
        {
            var candOk = new EffectDef
            {
                Id = CandOk,
                Opcode = EffectOpcodes.StatusApply,
                Kind = EffectKind.AtkBuff,
                Magnitude = 0.15f,
                DurationSec = 6f,
                MaxStack = 1,
                SourceTier = 1,
                Group = CandOk
            };
            var candEffects = new Dictionary<string, EffectDef>(StringComparer.Ordinal)
            {
                [CandOk] = candOk
            };
            var candSkills = new Dictionary<string, SkillDef>(StringComparer.Ordinal)
            {
                [CandSkillOk] = new SkillDef
                {
                    Id = CandSkillOk,
                    Name = CandSkillOk,
                    Type = SkillType.Tap,
                    Target = TargetRule.AllAllies,
                    TargetCount = 5,
                    Opcode = EffectOpcodes.DmgTap,
                    EffectId = CandOk
                },
                [CandSkillSteal] = new SkillDef
                {
                    Id = CandSkillSteal,
                    Name = CandSkillSteal,
                    Type = SkillType.Tap,
                    Target = TargetRule.AllAllies,
                    TargetCount = 5,
                    Opcode = EffectOpcodes.DmgTap,
                    EffectId = "atk_up"
                }
            };

            Assert.Null(Catalog.TryEffect(CandOk));
            Assert.NotNull(Catalog.TryEffect("atk_up"));

            var report = EffectCapability.ValidateCatalog(candEffects, candSkills);
            Assert.NotNull(report);

            var okMissing = false;
            var stealResolvedFromLive = true;
            for (int i = 0; i < report.Violations.Count; i++)
            {
                var v = report.Violations[i];
                if (v == null) continue;
                if (string.Equals(v.Id, CandSkillOk, StringComparison.Ordinal)
                    && v.Reason != null
                    && v.Reason.IndexOf("not in catalog", StringComparison.OrdinalIgnoreCase) >= 0)
                    okMissing = true;
                if (string.Equals(v.Id, CandSkillSteal, StringComparison.Ordinal))
                    stealResolvedFromLive = false;
            }

            Assert.False(
                okMissing,
                "V03: skill " + CandSkillOk + " references candidate effect " + CandOk +
                " — ValidateCatalog must use the candidate snapshot, not Catalog.TryEffect");
            Assert.False(
                stealResolvedFromLive,
                "V03: skill " + CandSkillSteal + " references atk_up which is only on the live Catalog; " +
                "candidate effects omitted it — validation must not steal the old table");

            var okFx = EffectCapability.Check(candOk);
            Assert.True(okFx.Ok, "candidate AtkBuff row itself must be playable so the skill can install atomically with it");
        }
    }
}
