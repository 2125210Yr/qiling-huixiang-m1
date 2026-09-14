using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// R01–R04 — real BattleReplayer / Capture / ContentFingerprint. No homemade replay loop.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2RecheckReplayContractTests
    {
        [Fact]
        public void R01_AutoFever_ReplayedOnce_ViaBattleReplayer()
        {
            const int seed = 1601;
            var original = FeverFactory(seed, AutoMode.Full);
            ArmFever(original);
            original.Auto = AutoMode.Full;

            var hits0 = CountFeverHits(original);
            for (int i = 0; i < BattleSim.TickHz && original.FeverActive; i++)
                original.Tick();

            var autoTaps = CountCommands(original, BattleCommandKind.FeverTap, CommandSource.Auto, true);
            var origHits = CountFeverHits(original, hits0);
            Assert.True(autoTaps > 0, "R01: Auto Fever never submitted FeverTap");
            Assert.True(origHits > 0, "R01: Auto Fever produced no fever hits");

            var rec = BattleRunRecord.Capture(original, Catalog.DefaultParty, StageId());
            var report = BattleReplayer.Verify(rec, s =>
            {
                var n = FeverFactory(s, AutoMode.Full);
                ArmFever(n);
                n.Auto = AutoMode.Full;
                return n;
            });
            Assert.NotNull(report.Replayed);

            var replayHits = CountFeverHits(report.Replayed);
            Assert.Equal(origHits + hits0, replayHits);
            Assert.True(
                BattleReplayer.Divergences.Count == 0,
                "R01: BattleReplayer ignored reject(s): " + string.Join(" ; ", BattleReplayer.Divergences));
            Assert.True(
                report.Match,
                "R01: BattleReplayer.Verify Match=false (double-fire or mixed auto+tape). Diff=" + DiffBlob(report));
        }

        [Fact]
        public void R02_Speed3AutoFull_StartThenChange_HeaderNotInferredAs1Manual()
        {
            var sim = NewJp(1602, AutoMode.Full, forceNoCrit: true);
            sim.Speed = 3;
            InflateEnemies(sim);
            LockFoes(sim);
            for (int i = 0; i < 5; i++)
                sim.Tick();

            var midSpeed = Submit(sim, BattleCommandKind.SetSpeed, value: 2);
            var midAuto = Submit(sim, BattleCommandKind.SetAuto, value: (int)AutoMode.Manual);
            Assert.True(midSpeed.Accepted, "SetSpeed rejected: " + midSpeed.Reason);
            Assert.True(midAuto.Accepted, "SetAuto rejected: " + midAuto.Reason);
            Assert.True(sim.TickIndex > 0, "change must happen after tick 0 so inference has no tick-0 Set*");

            var rec = BattleRunRecord.Capture(sim, Catalog.DefaultParty, StageId());
            Assert.Equal(3, rec.Speed);
            Assert.Equal(AutoMode.Full, rec.Auto);
            if (rec.Initial != null)
            {
                Assert.Equal(3, rec.Initial.Speed);
                Assert.Equal(AutoMode.Full, rec.Initial.Auto);
            }

            var replayed = BattleReplayer.Replay(rec, s =>
            {
                var n = FeverFactory(s, AutoMode.Manual);
                n.Speed = 1;
                return n;
            });
            Assert.Equal(2, replayed.Speed);
            Assert.Equal(AutoMode.Manual, replayed.Auto);
        }

        [Fact]
        public void R03_ReplayReject_ForcesMatchFalse_EvenIfHpMatches()
        {
            var sim = NewJp(1603, AutoMode.Manual, forceNoCrit: true);
            InflateEnemies(sim);
            LockFoes(sim);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
            for (int i = 0; i < 3; i++)
                sim.Tick();

            var rec = BattleRunRecord.Capture(sim, Catalog.DefaultParty, StageId());
            rec.Commands.Add(new CommandRecord
            {
                Seq = 99,
                Tick = 0,
                Kind = BattleCommandKind.FeverTap,
                Slot = 0,
                Source = CommandSource.Player,
                Accepted = true,
                Reason = CommandReject.None
            });

            var report = BattleReplayer.Verify(rec, FreshJp);
            Assert.True(
                BattleReplayer.Divergences.Count > 0,
                "R03 fixture: inserted accepted FeverTap must be rejected on replay");
            Assert.False(
                report.Match,
                "R03: Verify/Match must fail when an originally-accepted command is rejected, even if HP/digest still match. Diff=" +
                DiffBlob(report));
            var blob = DiffBlob(report);
            Assert.False(string.IsNullOrEmpty(blob), "R03: Diff must name the rejected command");
            Assert.Contains("99", blob);
            Assert.Contains("0", blob);
            Assert.True(
                blob.IndexOf("reject", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("Fever", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf(CommandReject.FeverNotActive.ToString(), System.StringComparison.Ordinal) >= 0,
                "R03: Diff must cite Seq/Tick/reject reason, not only digest/HP. Diff=" + blob);
        }

        [Fact]
        public void R04_HashChanges_WhenHpFlatPowerTriggerOrStageChange()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var c = Catalog.MustChar("C001");
                    var sk = Catalog.MustSkill("C001_tap");
                    var fx = Catalog.TryEffect("dot_flame");
                    Assert.NotNull(fx);
                    var stage = Catalog.VerticalSliceStage;
                    Assert.NotNull(stage);

                    var hp0 = c.Hp;
                    var flat0 = sk.FlatPower;
                    var trig0 = fx.Trigger;
                    var period0 = fx.PeriodSec;
                    var time0 = stage.TimeLimitSec;

                    var fp0 = BattleSim.ContentFingerprint();
                    var id0 = BattleContentIdentity.Fingerprint();

                    c.Hp = hp0 + 111;
                    Assert.NotEqual(fp0, BattleSim.ContentFingerprint());
                    Assert.NotEqual(id0, BattleContentIdentity.Fingerprint());
                    c.Hp = hp0;

                    sk.FlatPower = flat0 + 77;
                    Assert.NotEqual(fp0, BattleSim.ContentFingerprint());
                    Assert.NotEqual(id0, BattleContentIdentity.Fingerprint());
                    sk.FlatPower = flat0;

                    fx.Trigger = EffectDef.TriggerPeriodic;
                    fx.PeriodSec = 0.5f;
                    Assert.NotEqual(fp0, BattleSim.ContentFingerprint());
                    Assert.NotEqual(id0, BattleContentIdentity.Fingerprint());
                    fx.Trigger = trig0;
                    fx.PeriodSec = period0;

                    stage.TimeLimitSec = time0 + 17f;
                    Assert.NotEqual(fp0, BattleSim.ContentFingerprint());
                    Assert.NotEqual(id0, BattleContentIdentity.Fingerprint());
                    stage.TimeLimitSec = time0;
                }
                finally
                {
                    RestoreBuiltinCatalog();
                }
            }
        }

        static string StageId()
        {
            return Catalog.VerticalSliceStage != null ? Catalog.VerticalSliceStage.Id : "VS-1";
        }

        static BattleSim FeverFactory(int seed, AutoMode auto)
        {
            var sim = NewJp(seed, auto, forceNoCrit: true);
            InflateEnemies(sim);
            LockFoes(sim);
            sim.TimeLeft = 999f;
            sim.Clocks.FeverHitBudget = 8;
            sim.Clocks.FeverWindowSec = 4f;
            sim.Clocks.FeverMinHitIntervalSec = 0.2f;
            sim.Clocks.FeverAutoTapsPerSec = 5f;
            return sim;
        }
    }
}
