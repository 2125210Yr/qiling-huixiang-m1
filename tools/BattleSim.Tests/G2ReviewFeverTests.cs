using System;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// G2 review F01–F04. Asserts API_CONTRACT Fever / Submit behaviour. Red is acceptable.
    /// </summary>
    public sealed class G2ReviewFeverTests
    {
        [Fact]
        public void F01_ManualFever_RequiresInput()
        {
            var sim = ArmedManual();
            EnterFever(sim);
            Assert.True(sim.FeverActive);
            Assert.Equal(AutoMode.Manual, sim.Auto);

            var budget = sim.FeverHitsLeft;
            Assert.True(budget > 0);
            var windowTicks = FeverWindowTicks(sim) + BattleSim.TickHz;
            for (int i = 0; i < windowTicks && sim.FeverActive; i++)
            {
                Assert.Equal(budget, sim.FeverHitsLeft);
                Assert.DoesNotContain(sim.Events.Events, e =>
                    e.Kind == "hit" && e.Channel == SkillType.Fever);
                sim.Tick();
            }

            Assert.False(sim.FeverActive);
            Assert.Equal(FeverEndReason.TimeUp, sim.LastFeverEnd);
            Assert.DoesNotContain(sim.Events.Events, e =>
                e.Kind == "hit" && e.Channel == SkillType.Fever);
        }

        [Fact]
        public void F02_ManualFever_UsesSelectedCaster()
        {
            var sim = ArmedManual();
            Assert.True(AliveAllies(sim) >= 2);
            Assert.True(sim.Allies[1] != null && sim.Allies[1].Alive);
            EnterFever(sim);

            var eventsBefore = sim.Events.Events.Count;
            var logBefore = sim.Log.Count;
            Assert.True(sim.TryFeverTap(1));

            var hit = FindSince(sim.Events.Events, eventsBefore, e =>
                e.Kind == "hit" && e.Channel == SkillType.Fever);
            Assert.NotNull(hit);
            Assert.Equal(1, hit.CasterSlot);
            Assert.True(hit.CasterAlly);

            var fx = FindSince(sim.Log, logBefore, t => t.Fever && t.Kind == SkillType.Fever);
            Assert.NotNull(fx);
            Assert.Equal(1, fx.CasterSlot);
        }

        [Fact]
        public void F03_Fever_WindowAndBudgetAreConsistent()
        {
            var policy = new BattleClockPolicy();
            Assert.True(
                policy.FeverAutoTapsPerSec * policy.FeverWindowSec >= policy.FeverHitBudget - 1,
                "default auto rate cannot exhaust the declared budget inside the window");

            BudgetPathEndsWhenTapsSpendBudget();
            IdlePathEndsOnTimeUp();
            ThrottledSecondTapIsRejected();
        }

        [Fact]
        public void F04_AutoFever_UsesSameCommandPath()
        {
            var sim = ArmedManual();
            sim.Clocks.FeverHitBudget = 40;
            sim.Clocks.FeverWindowSec = 8f;
            sim.Clocks.FeverMinHitIntervalSec = 0.2f;
            sim.Clocks.FeverAutoTapsPerSec = 5f;
            EnterFever(sim);
            sim.Auto = AutoMode.Full;

            var hitsBefore = FeverHitCount(sim);
            var logBefore = sim.CommandLog.Count;
            for (int i = 0; i < BattleSim.TickHz; i++)
                sim.Tick();

            Assert.Contains(sim.CommandLog, r =>
                r.Kind == BattleCommandKind.FeverTap && r.Source == CommandSource.Auto && r.Accepted);
            Assert.True(sim.CommandLog.Count > logBefore);
            Assert.True(FeverHitCount(sim) > hitsBefore);

            var owned = sim.Submit(new BattleCommand
            {
                Kind = BattleCommandKind.FeverTap,
                Slot = 0,
                Source = CommandSource.Player
            });
            Assert.False(owned.Accepted);
            Assert.Equal(CommandReject.AutoOwnsInput, owned.Reason);

            var hitsAtPause = FeverHitCount(sim);
            var leftAtPause = sim.FeverHitsLeft;
            sim.Paused = true;
            for (int i = 0; i < BattleSim.TickHz * 2; i++)
                sim.Tick();
            Assert.Equal(hitsAtPause, FeverHitCount(sim));
            Assert.Equal(leftAtPause, sim.FeverHitsLeft);

            sim.Paused = false;
            sim.Outcome = BattleOutcome.Victory;
            for (int i = 0; i < BattleSim.TickHz; i++)
                sim.Tick();
            Assert.Equal(hitsAtPause, FeverHitCount(sim));
            Assert.Equal(leftAtPause, sim.FeverHitsLeft);
        }

        static void BudgetPathEndsWhenTapsSpendBudget()
        {
            var sim = ArmedManual();
            sim.Clocks.FeverHitBudget = 5;
            sim.Clocks.FeverWindowSec = 8f;
            sim.Clocks.FeverMinHitIntervalSec = 0.2f;
            EnterFever(sim);
            Assert.Equal(5, sim.FeverHitsLeft);

            var intervalTicks = (int)Math.Ceiling(sim.Clocks.FeverMinHitIntervalSec / BattleSim.TickDt) + 1;
            var guard = 0;
            while (sim.FeverActive && sim.Outcome == BattleOutcome.InProgress && guard++ < 4000)
            {
                sim.TryFeverTap(0);
                for (int i = 0; i < intervalTicks && sim.FeverActive; i++)
                    sim.Tick();
            }

            Assert.Equal(FeverEndReason.BudgetExhausted, sim.LastFeverEnd);
            Assert.Equal(0, sim.FeverHitsLeft);
            Assert.False(sim.FeverActive);
        }

        static void IdlePathEndsOnTimeUp()
        {
            var sim = ArmedManual();
            sim.Clocks.FeverHitBudget = 20;
            sim.Clocks.FeverWindowSec = 1f;
            sim.Clocks.FeverMinHitIntervalSec = 0.2f;
            EnterFever(sim);
            var budget = sim.FeverHitsLeft;
            var cap = FeverWindowTicks(sim) + BattleSim.TickHz;
            for (int i = 0; i < cap && sim.FeverActive; i++)
            {
                Assert.Equal(budget, sim.FeverHitsLeft);
                sim.Tick();
            }
            Assert.Equal(FeverEndReason.TimeUp, sim.LastFeverEnd);
            Assert.DoesNotContain(sim.Events.Events, e =>
                e.Kind == "hit" && e.Channel == SkillType.Fever);
        }

        static void ThrottledSecondTapIsRejected()
        {
            var sim = ArmedManual();
            sim.Clocks.FeverMinHitIntervalSec = 0.2f;
            EnterFever(sim);
            var first = sim.Submit(new BattleCommand
            {
                Kind = BattleCommandKind.FeverTap,
                Slot = 0,
                Source = CommandSource.Player
            });
            Assert.True(first.Accepted);
            Assert.Equal(CommandReject.None, first.Reason);

            var second = sim.Submit(new BattleCommand
            {
                Kind = BattleCommandKind.FeverTap,
                Slot = 0,
                Source = CommandSource.Player
            });
            Assert.False(second.Accepted);
            Assert.Equal(CommandReject.FeverThrottled, second.Reason);
        }

        static BattleSim ArmedManual()
        {
            var sim = new BattleSim(Catalog.DefaultParty, 0, 3)
            {
                ForceNoCrit = true,
                Speed = 1,
                Auto = AutoMode.Manual,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
            };
            sim.TimeLeft = 999f;
            InflateFoes(sim);
            LockFoes(sim);
            ChargeAll(sim);
            return sim;
        }

        static void EnterFever(BattleSim sim)
        {
            for (int n = 0; n < 6 && !sim.FeverActive; n++)
            {
                ChargeAll(sim);
                sim.Drive = 100f;
                Assert.True(sim.TryBeginDrive(0));
                Assert.True(sim.ResolveDrive(DriveTiming.Perfect));
            }
            Assert.True(sim.FeverActive);
        }

        static int FeverWindowTicks(BattleSim sim)
        {
            var window = sim.Clocks != null ? sim.Clocks.FeverWindowSec : BattleSim.UnknownFeverWindowSec;
            return (int)Math.Ceiling(window / BattleSim.TickDt) + 2;
        }

        static int FeverHitCount(BattleSim sim)
        {
            var n = 0;
            for (int i = 0; i < sim.Events.Events.Count; i++)
            {
                var e = sim.Events.Events[i];
                if (e.Kind == "hit" && e.Channel == SkillType.Fever) n++;
            }
            return n;
        }

        static int AliveAllies(BattleSim sim)
        {
            var n = 0;
            for (int i = 0; i < sim.Allies.Length; i++)
                if (sim.Allies[i] != null && sim.Allies[i].Alive) n++;
            return n;
        }

        static void ChargeAll(BattleSim sim)
        {
            for (int i = 0; i < sim.Allies.Length; i++)
            {
                if (sim.Allies[i] != null)
                    sim.Allies[i].Charge = 100f;
            }
        }

        static void InflateFoes(BattleSim sim)
        {
            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                var u = sim.Enemies[i];
                if (u == null) continue;
                u.MaxHp = 500000;
                u.Hp = 500000;
            }
        }

        static void LockFoes(BattleSim sim)
        {
            var stun = Catalog.TryEffect("stun");
            for (int i = 0; i < sim.Enemies.Count; i++)
                sim.ApplyStatus(sim.Enemies[i], stun);
        }

        static BattleEvent FindSince(System.Collections.Generic.List<BattleEvent> events, int start, Func<BattleEvent, bool> pred)
        {
            for (int i = start; i < events.Count; i++)
                if (pred(events[i])) return events[i];
            return null;
        }

        static FloatText FindSince(System.Collections.Generic.List<FloatText> log, int start, Func<FloatText, bool> pred)
        {
            for (int i = start; i < log.Count; i++)
                if (pred(log[i])) return log[i];
            return null;
        }
    }
}
