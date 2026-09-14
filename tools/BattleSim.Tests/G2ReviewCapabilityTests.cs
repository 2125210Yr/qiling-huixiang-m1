using System;
using System.Collections.Generic;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// G2 R05 / REGRESSION_MATRIX E01: opcode+kind capability table.
    /// ENGINEERING only — not GL fidelity. M1_FIDELITY=DEFERRED_NOT_REMOVED.
    /// </summary>
    public sealed class G2ReviewCapabilityTests
    {
        [Fact]
        public void UnsupportedEffectKindRejectedBeforePlay()
        {
            var silenceWrong = new EffectDef
            {
                Id = "silence_via_status",
                Opcode = EffectOpcodes.StatusApply,
                Kind = EffectKind.Silence,
                Magnitude = 1f,
                DurationSec = 4f,
                MaxStack = 1,
                Group = "silence"
            };
            var silence = EffectCapability.Check(silenceWrong);
            Assert.False(silence.Ok);
            Assert.Equal(CapabilityState.Registered_NotImplemented, silence.State);
            Assert.Contains("control.apply", silence.Reason, StringComparison.Ordinal);

            var silenceOk = EffectCapability.Check(new EffectDef
            {
                Id = "silence_ok",
                Opcode = EffectOpcodes.ControlApply,
                Kind = EffectKind.Silence,
                Magnitude = 1f,
                DurationSec = 4f,
                MaxStack = 1,
                Group = "silence"
            });
            Assert.True(silenceOk.Ok);

            var revive = EffectCapability.Check(new EffectDef
            {
                Id = "revive_fx",
                Opcode = EffectOpcodes.Revive,
                Kind = EffectKind.Damage,
                Magnitude = 1f,
                DurationSec = 0f,
                MaxStack = 1,
                Group = "revive"
            });
            Assert.False(revive.Ok);
            Assert.Equal(CapabilityState.Registered_NotImplemented, revive.State);

            Assert.Throws<ContentValidationException>(() => EffectCapability.RejectUnplayable(silenceWrong));

            Catalog.BuildBuiltin();
            try
            {
                Assert.Throws<ContentValidationException>(() => CatalogJson.Load(
                    "{\"effects\":[{\"id\":\"revive_fx\",\"op\":\"revive\",\"kind\":0,\"mag\":1,\"dur\":0,\"stack\":1,\"tier\":1,\"group\":\"revive\"}]}"));
            }
            finally
            {
                Catalog.BuildBuiltin();
            }

            if (!EffectCapability.HasLifecycleFields)
                return;

            var periodic = new EffectDef
            {
                Id = "periodic_poison",
                Opcode = EffectOpcodes.PoisonApply,
                Kind = EffectKind.Poison,
                Magnitude = 0.1f,
                DurationSec = 8f,
                MaxStack = 1,
                Group = "poison"
            };
            EffectCapability.WriteTrigger(periodic, EffectCapability.TokenPeriodic);
            EffectCapability.WritePeriodSec(periodic, 0f);
            var period = EffectCapability.Check(periodic);
            Assert.False(period.Ok);
            Assert.True(period.IsParamError);
            Assert.Contains("PeriodSec", period.ParamErrors[0], StringComparison.Ordinal);
        }

        [Fact]
        public void DefaultContentPassesValidation()
        {
            Catalog.BuildBuiltin();
            var report = Catalog.ValidateContent();
            Assert.NotNull(report);

            // Builtin must stay start-safe: no unknown opcode, no param errors,
            // no opcode that has zero Implemented kinds. Kind-level consumer
            // gaps (dot_flame / Dot) are reported, not treated as import-blocking.
            Assert.Empty(report.BlockingViolations());
            for (int i = 0; i < report.Violations.Count; i++)
            {
                var v = report.Violations[i];
                Assert.NotEqual(CapabilityState.Unknown, v.State);
                Assert.False(v.IsParamError);
                Assert.False(v.OpcodeNotImplemented);
            }

            Assert.True(EffectOpcodes.IsImplemented(EffectOpcodes.StatusApply));
            Assert.True(EffectOpcodes.IsImplemented(EffectOpcodes.ControlApply));
            Assert.False(EffectOpcodes.IsImplemented(EffectOpcodes.DmgPierce));
            Assert.False(EffectOpcodes.IsImplemented(EffectOpcodes.Revive));
            Assert.True(EffectOpcodes.IsImplemented(EffectOpcodes.StatusDispel));
        }

        [Fact]
        public void MatrixCoversEveryKnownOpcode()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in EffectCapability.Matrix())
            {
                if (row == null || string.IsNullOrEmpty(row.Opcode)) continue;
                seen.Add(row.Opcode);
            }

            foreach (var opcode in EffectOpcodes.KnownList)
                Assert.True(seen.Contains(opcode), "matrix missing opcode " + opcode);
        }

        [Fact]
        public void EveryEffectKindAppearsInMatrix()
        {
            var seen = new HashSet<EffectKind>();
            foreach (var row in EffectCapability.Matrix())
            {
                if (row == null) continue;
                seen.Add(row.Kind);
            }

            foreach (EffectKind kind in Enum.GetValues(typeof(EffectKind)))
                Assert.True(seen.Contains(kind), "matrix missing kind " + kind);
        }
    }
}
