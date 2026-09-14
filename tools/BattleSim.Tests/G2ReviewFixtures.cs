using System;
using System.Collections.Generic;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// Shared fixtures for the G2 review red tests (F/Q/R/E rows of REGRESSION_MATRIX.md).
    /// Fixtures may inject state (charge, gauges, enemy HP) — the "no injection" rule only
    /// governs the Unity natural-play flow (N01). Nothing here weakens a contract assertion.
    /// </summary>
    internal static class G2ReviewFixtures
    {
        public const int BigHp = 500000;

        public static BattleSim NewJp(int seed, AutoMode auto = AutoMode.Manual, bool forceNoCrit = false)
        {
            return new BattleSim(Catalog.DefaultParty, 0, seed)
            {
                ForceNoCrit = forceNoCrit,
                Speed = 1,
                Auto = auto,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
            };
        }

        /// <summary>Keeps the current wave alive for the whole test so wave/result transitions never interfere.</summary>
        public static void InflateEnemies(BattleSim sim)
        {
            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                var e = sim.Enemies[i];
                if (e == null) continue;
                e.MaxHp = BigHp;
                e.Hp = BigHp;
            }
        }

        public static void InflateAllies(BattleSim sim)
        {
            for (int i = 0; i < sim.Allies.Length; i++)
            {
                var u = sim.Allies[i];
                if (u == null) continue;
                u.MaxHp = BigHp;
                u.Hp = BigHp;
            }
        }

        /// <summary>Holds the current wave and suppresses incidental autos so Fever/QTE/status clocks can be observed.</summary>
        public static void HoldTheLine(BattleSim sim)
        {
            InflateAllies(sim);
            InflateEnemies(sim);
            LockFoes(sim);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
        }

        public static void TickFor(BattleSim sim, float seconds)
        {
            var n = (int)Math.Ceiling(seconds / BattleSim.TickDt) + 2;
            for (int i = 0; i < n && sim.Outcome == BattleOutcome.InProgress; i++)
                sim.Tick();
        }

        public static void ChargeAll(BattleSim sim)
        {
            for (int i = 0; i < sim.Allies.Length; i++)
                if (sim.Allies[i] != null) sim.Allies[i].Charge = 100f;
        }

        public static void LockFoes(BattleSim sim)
        {
            var stun = Catalog.TryEffect("stun");
            Assert.NotNull(stun);
            for (int i = 0; i < sim.Enemies.Count; i++)
                sim.ApplyStatus(sim.Enemies[i], stun);
        }

        /// <summary>Pushes ally auto-attack timers far below zero so no ally normal attack fires inside the test window.</summary>
        public static void SuppressAllyAutoAttacks(BattleSim sim)
        {
            for (int i = 0; i < sim.Allies.Length; i++)
                if (sim.Allies[i] != null) sim.Allies[i].AutoTimer = -100000f;
        }

        public static void SuppressEnemyAutoAttacks(BattleSim sim)
        {
            for (int i = 0; i < sim.Enemies.Count; i++)
                if (sim.Enemies[i] != null) sim.Enemies[i].AutoTimer = -100000f;
        }

        /// <summary>
        /// Drives the real Fever path (Drive QTE → AddFever) until the window opens.
        /// Perfect = +40 → three resolves in Manual; Full auto self-resolves Great (+30) → four.
        /// </summary>
        public static void ArmFever(BattleSim sim)
        {
            for (int n = 0; n < 8 && !sim.FeverActive; n++)
            {
                ChargeAll(sim);
                sim.Drive = 100f;
                Assert.True(sim.TryBeginDrive(0), "TryBeginDrive rejected while arming fever (n=" + n + ")");
                if (sim.PendingDriveSlot >= 0)
                    Assert.True(sim.ResolveDrive(DriveTiming.Perfect), "ResolveDrive rejected while arming fever (n=" + n + ")");
            }
            Assert.True(sim.FeverActive, "fixture could not open the Fever window");
            Assert.Equal(BattleOutcome.InProgress, sim.Outcome);
        }

        public static BattleCommand Cmd(BattleCommandKind kind, int slot = -1, DriveTiming timing = DriveTiming.Good, int value = 0, CommandSource source = CommandSource.Player)
        {
            return new BattleCommand { Kind = kind, Slot = slot, Timing = timing, Value = value, Source = source };
        }

        public static int CountFeverHits(BattleSim sim, int since = 0)
        {
            var n = 0;
            var ev = sim.Events.Events;
            for (int i = since; i < ev.Count; i++)
                if (ev[i].Kind == "hit" && ev[i].Channel == SkillType.Fever) n++;
            return n;
        }

        public static int CountFeverFloatTexts(BattleSim sim, int since = 0)
        {
            var n = 0;
            for (int i = since; i < sim.Log.Count; i++)
                if (sim.Log[i].Fever) n++;
            return n;
        }

        public static int CountCommands(BattleSim sim, BattleCommandKind kind, CommandSource? source, bool? accepted, int since = 0)
        {
            var n = 0;
            for (int i = since; i < sim.CommandLog.Count; i++)
            {
                var r = sim.CommandLog[i];
                if (r.Kind != kind) continue;
                if (source.HasValue && r.Source != source.Value) continue;
                if (accepted.HasValue && r.Accepted != accepted.Value) continue;
                n++;
            }
            return n;
        }

        public static int CountEvents(BattleSim sim, Func<BattleEvent, bool> pred, int since = 0)
        {
            var n = 0;
            var ev = sim.Events.Events;
            for (int i = since; i < ev.Count; i++)
                if (pred(ev[i])) n++;
            return n;
        }

        public static int[] SnapshotHp(BattleSim sim)
        {
            var list = new List<int>();
            for (int i = 0; i < sim.Allies.Length; i++) list.Add(sim.Allies[i] != null ? sim.Allies[i].Hp : -1);
            for (int i = 0; i < sim.Enemies.Count; i++) list.Add(sim.Enemies[i] != null ? sim.Enemies[i].Hp : -1);
            return list.ToArray();
        }

        public static int CountCasts(BattleSim sim, SkillType type, bool ally, int since = 0)
        {
            var n = 0;
            for (int i = since; i < sim.Casts.Count; i++)
                if (sim.Casts[i].Type == type && sim.Casts[i].CasterAlly == ally && !sim.Casts[i].Fever) n++;
            return n;
        }

        public static EffectDef Fx(string id, EffectKind kind, string opcode, float magnitude, float durationSec, int maxStack = 1, int tier = 1, string group = null)
        {
            return new EffectDef
            {
                Id = id,
                Opcode = opcode,
                Kind = kind,
                Magnitude = magnitude,
                DurationSec = durationSec,
                MaxStack = maxStack,
                SourceTier = tier,
                Group = group ?? id
            };
        }

        public static StatusInst FindStatus(UnitState u, string id)
        {
            for (int i = 0; i < u.Status.Count; i++)
                if (u.Status[i] != null && u.Status[i].Def != null && u.Status[i].Def.Id == id) return u.Status[i];
            return null;
        }
    }
}
