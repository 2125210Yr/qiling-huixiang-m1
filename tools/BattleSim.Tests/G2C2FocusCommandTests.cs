using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// C2-T2 — Player FocusEnemy is a recorded command; replay hits the focused slot.
    /// </summary>
    public sealed class G2C2FocusCommandTests
    {
        const int Seed = 2202;

        [Fact]
        public void C2_T2_FocusEnemy_AcceptedThenTap_ReplayHitTarget()
        {
            var sim = FocusFactory(Seed);
            var focus = LivingEnemySlot(sim, preferLast: true);
            Assert.True(focus >= 0, "need a living enemy");

            var cmd = sim.Submit(BattleCommand.FocusEnemy(focus, CommandSource.Player));
            Assert.True(cmd.Accepted, "FocusEnemy rejected: " + cmd.Reason);
            Assert.Equal(focus, sim.FocusEnemySlot);
            Assert.Contains(sim.CommandLog, c =>
                c.Accepted && c.Kind == BattleCommandKind.FocusEnemy
                && c.Source == CommandSource.Player && c.Slot == focus);

            var tap = sim.Submit(BattleCommand.Tap(0, CommandSource.Player));
            Assert.True(tap.Accepted, "Tap rejected: " + tap.Reason);
            var hit = FirstEnemyTapHit(sim);
            Assert.True(hit >= 0, "C001 tap should produce an enemy hit");
            Assert.Equal(focus, hit);

            var rec = BattleRunRecord.Capture(sim, Catalog.DefaultParty, StageId(sim));
            var report = BattleReplayer.Verify(rec, FocusFactory);
            Assert.True(report.Match, "Diff: " + string.Join(" | ", report.Diff));
            Assert.Empty(report.DigestDiff);
            Assert.Empty(report.EventDiff);
            Assert.NotNull(report.Replayed);
            Assert.Equal(focus, report.Replayed.FocusEnemySlot);
            Assert.Equal(focus, FirstEnemyTapHit(report.Replayed));
        }

        [Fact]
        public void C2_T2_FocusEnemy_InvalidAndDead_RecordedRejected()
        {
            var sim = FocusRejectFactory(Seed);
            var bad = sim.Submit(BattleCommand.FocusEnemy(99, CommandSource.Player));
            Assert.False(bad.Accepted);
            Assert.Equal(CommandReject.SlotInvalid, bad.Reason);
            Assert.Contains(sim.CommandLog, c =>
                !c.Accepted && c.Kind == BattleCommandKind.FocusEnemy
                && c.Slot == 99 && c.Reason == CommandReject.SlotInvalid);

            var deadSlot = 0;
            Assert.True(sim.Enemies.Count > deadSlot && sim.Enemies[deadSlot] != null);
            Assert.False(sim.Enemies[deadSlot].Alive);
            var dead = sim.Submit(BattleCommand.FocusEnemy(deadSlot, CommandSource.Player));
            Assert.False(dead.Accepted);
            Assert.Equal(CommandReject.SlotInvalid, dead.Reason);
            Assert.Contains(sim.CommandLog, c =>
                !c.Accepted && c.Kind == BattleCommandKind.FocusEnemy
                && c.Slot == deadSlot && c.Reason == CommandReject.SlotInvalid);

            var rec = BattleRunRecord.Capture(sim, Catalog.DefaultParty, StageId(sim));
            var report = BattleReplayer.Verify(rec, FocusRejectFactory);
            Assert.True(report.Match, "rejected FocusEnemy must still replay. Diff: " + string.Join(" | ", report.Diff));
        }

        static BattleSim FocusFactory(int seed)
        {
            var sim = NewJp(seed, AutoMode.Manual, forceNoCrit: true);
            InflateEnemies(sim);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
            ChargeAll(sim);
            for (int i = 1; i < sim.Allies.Length; i++)
                if (sim.Allies[i] != null) sim.Allies[i].Charge = 0f;
            return sim;
        }

        static BattleSim FocusRejectFactory(int seed)
        {
            var sim = FocusFactory(seed);
            if (sim.Enemies.Count > 0 && sim.Enemies[0] != null)
                sim.Enemies[0].Hp = 0;
            return sim;
        }

        static int LivingEnemySlot(BattleSim sim, bool preferLast)
        {
            var last = -1;
            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                var e = sim.Enemies[i];
                if (e == null || !e.Alive) continue;
                if (!preferLast) return i;
                last = i;
            }
            return last;
        }

        static int FirstEnemyTapHit(BattleSim sim)
        {
            var ev = sim.Events.Events;
            for (int i = 0; i < ev.Count; i++)
            {
                var e = ev[i];
                if (e == null || e.Kind != "hit") continue;
                if (e.Channel != SkillType.Tap) continue;
                if (e.TargetAlly) continue;
                if (e.TargetSlot < 0) continue;
                return e.TargetSlot;
            }
            return -1;
        }

        static string StageId(BattleSim sim)
        {
            return sim.ActiveStage != null ? sim.ActiveStage.Id : "VS-1";
        }
    }
}
