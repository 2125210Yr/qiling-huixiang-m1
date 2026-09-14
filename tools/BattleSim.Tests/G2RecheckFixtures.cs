using System;
using System.Collections.Generic;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// Shared helpers for G2_RECHECK_20260914 red tests. Fixtures may inject
    /// charge / timers / catalog rows — the no-injection rule is Unity natural-play only.
    /// Numbers here are DESIGN_PLACEHOLDER, not original-game values.
    /// </summary>
    internal static class G2RecheckFixtures
    {
        public static readonly object CatalogGate = new object();

        public static BattleSim FreshJp(int seed)
        {
            return G2ReviewFixtures.NewJp(seed, AutoMode.Manual, forceNoCrit: false);
        }

        public static void RestoreBuiltinCatalog()
        {
            Catalog.BuildBuiltin();
        }

        public static Dictionary<string, EffectDef> LiveEffectsOrFail()
        {
            var effects = Catalog.Effects as Dictionary<string, EffectDef>;
            Assert.True(
                effects != null,
                "Catalog.Effects is not a live Dictionary; cannot inject a DESIGN_PLACEHOLDER effect for SkillDef+Submit tests.");
            return effects;
        }

        public static void InstallEffect(EffectDef fx)
        {
            Assert.NotNull(fx);
            Assert.False(string.IsNullOrEmpty(fx.Id));
            LiveEffectsOrFail()[fx.Id] = fx;
        }

        public static EffectDef ChargeSpeedFx(string id = "g2r_charge_speed")
        {
            return new EffectDef
            {
                Id = id,
                Opcode = EffectOpcodes.ChargeRate,
                Kind = EffectKind.ChargeSpeed,
                Magnitude = 0.25f,
                DurationSec = 10f,
                MaxStack = 1,
                SourceTier = 1,
                Group = id
            };
        }

        public static EffectDef ChargeAmountFx(string id = "g2r_charge_amount", float mag = 20f)
        {
            return new EffectDef
            {
                Id = id,
                Opcode = EffectOpcodes.ChargeAdd,
                Kind = EffectKind.ChargeAmount,
                Magnitude = mag,
                DurationSec = 0f,
                MaxStack = 1,
                SourceTier = 1,
                Group = id
            };
        }

        public static EffectDef BarrierFx(string id = "g2r_barrier")
        {
            return new EffectDef
            {
                Id = id,
                Opcode = EffectOpcodes.ShieldApply,
                Kind = EffectKind.Barrier,
                Magnitude = 0.20f,
                DurationSec = 8f,
                MaxStack = 1,
                SourceTier = 1,
                Group = id
            };
        }

        public static EffectDef SilenceFx(string id, float durationSec)
        {
            return new EffectDef
            {
                Id = id,
                Opcode = EffectOpcodes.ControlApply,
                Kind = EffectKind.Silence,
                Magnitude = 1f,
                DurationSec = durationSec,
                MaxStack = 1,
                SourceTier = 2,
                Group = id
            };
        }

        public static SkillDef AllAlliesSupport(string id, SkillType type, string effectId, int driveGain = 0)
        {
            return new SkillDef
            {
                Id = id,
                Name = id,
                Type = type,
                Target = TargetRule.AllAllies,
                TargetCount = 5,
                HitCount = 1,
                AtkCoef = 0f,
                FlatPower = 0,
                DriveGain = driveGain,
                EffectId = effectId,
                Opcode = DamageMath.ChannelOpcode(type)
            };
        }

        public static int SlotOf(BattleSim sim, string charId)
        {
            for (int i = 0; i < sim.Allies.Length; i++)
            {
                var u = sim.Allies[i];
                if (u != null && u.Def != null && string.Equals(u.Def.Id, charId, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        public static CommandResult Submit(BattleSim sim, BattleCommandKind kind, int slot = -1, DriveTiming timing = DriveTiming.Good, int value = 0, CommandSource source = CommandSource.Player)
        {
            return sim.Submit(new BattleCommand
            {
                Kind = kind,
                Slot = slot,
                Timing = timing,
                Value = value,
                Source = source
            });
        }

        public static int CountEnemySkillCasts(BattleSim sim, int enemySlot, int since = 0)
        {
            var n = 0;
            for (int i = since; i < sim.Casts.Count; i++)
            {
                var c = sim.Casts[i];
                if (c.CasterAlly) continue;
                if (c.CasterSlot != enemySlot) continue;
                if (c.Type == SkillType.Tap || c.Type == SkillType.Slide) n++;
            }
            return n;
        }

        public static int CountEnemyAutoCasts(BattleSim sim, int enemySlot, int since = 0)
        {
            var n = 0;
            for (int i = since; i < sim.Casts.Count; i++)
            {
                var c = sim.Casts[i];
                if (!c.CasterAlly && c.CasterSlot == enemySlot && c.Type == SkillType.Auto) n++;
            }
            return n;
        }

        public static string DiffBlob(ReplayReport report)
        {
            if (report == null) return "";
            return string.Join(" | ", report.Diff);
        }
    }

    /// <summary>Serializes catalog-mutating G2Recheck tests. Do not share with G2Review*.</summary>
    [CollectionDefinition("G2RecheckCatalog")]
    public sealed class G2RecheckCatalogCollection
    {
    }
}
