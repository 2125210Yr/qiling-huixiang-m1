using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    public static class EffectOpcodes
    {
        public const string DmgTap = "dmg.tap";
        public const string DmgSlide = "dmg.slide";
        public const string DmgAuto = "dmg.auto";
        public const string DmgPierce = "dmg.pierce";
        public const string DmgExtraFlat = "dmg.extra_flat";
        public const string DmgDriveActual = "dmg.drive_actual";
        public const string DmgFeverParts = "dmg.fever_parts";
        public const string StatusApply = "status.apply";
        public const string StatusDispel = "status.dispel";
        public const string PoisonApply = "poison.apply";
        public const string ShieldApply = "shield.apply";
        public const string ControlApply = "control.apply";
        public const string ChargeAdd = "charge.add";
        public const string ChargeRate = "charge.rate";
        public const string SlideCd = "slide_cd";
        public const string Retarget = "retarget";
        public const string Revive = "revive";

        static readonly string[] Known =
        {
            DmgTap, DmgSlide, DmgAuto, DmgPierce, DmgExtraFlat, DmgDriveActual, DmgFeverParts,
            StatusApply, StatusDispel, PoisonApply, ShieldApply, ControlApply,
            ChargeAdd, ChargeRate, SlideCd, Retarget, Revive
        };

        public static IReadOnlyList<string> KnownList => Known;

        public static string ForKind(EffectKind kind)
        {
            if (kind == EffectKind.Poison || kind == EffectKind.Bleed) return PoisonApply;
            if (kind == EffectKind.Shield || kind == EffectKind.Barrier) return ShieldApply;
            if (kind == EffectKind.Stun || kind == EffectKind.Freeze
                || kind == EffectKind.Taunt || kind == EffectKind.Silence)
                return ControlApply;
            if (kind == EffectKind.ChargeAmount)
                return ChargeAdd;
            if (kind == EffectKind.ChargeHaste || kind == EffectKind.ChargeSpeed)
                return ChargeRate;
            return StatusApply;
        }

        public static bool IsKnown(string opcode) => InSet(Known, opcode);

        /// <summary>
        /// True when the opcode has at least one Implemented (opcode, kind) row.
        /// Kind-level gaps still fail <see cref="EffectCapability.Check"/>.
        /// </summary>
        public static bool IsImplemented(string opcode) => EffectCapability.OpcodeHasImplementation(opcode);

        public static bool IsDamageChannel(string opcode)
        {
            return string.Equals(opcode, DmgTap, StringComparison.Ordinal)
                || string.Equals(opcode, DmgSlide, StringComparison.Ordinal)
                || string.Equals(opcode, DmgAuto, StringComparison.Ordinal)
                || string.Equals(opcode, DmgDriveActual, StringComparison.Ordinal)
                || string.Equals(opcode, DmgFeverParts, StringComparison.Ordinal);
        }

        public static bool IsDotTriggerKind(EffectKind kind)
        {
            return kind == EffectKind.Poison || kind == EffectKind.Bleed;
        }

        static bool InSet(string[] set, string opcode)
        {
            if (string.IsNullOrEmpty(opcode) || set == null) return false;
            for (int i = 0; i < set.Length; i++)
            {
                if (string.Equals(set[i], opcode, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public static void RequireKnown(string opcode)
        {
            if (IsKnown(opcode)) return;
            throw new UnknownOpcodeException(opcode);
        }

        public static void RequireImplemented(string opcode)
        {
            if (IsImplemented(opcode)) return;
            throw new UnknownOpcodeException(opcode);
        }
    }

    public sealed class UnknownOpcodeException : InvalidOperationException
    {
        public readonly string Opcode;

        public UnknownOpcodeException(string opcode)
            : base("UNKNOWN_OPCODE " + (string.IsNullOrEmpty(opcode) ? "<empty>" : opcode))
        {
            Opcode = opcode ?? "";
        }
    }
}
