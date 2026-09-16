using System;
using System.Collections.Generic;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// C1-T1 / C1-T2 — VerificationCatalog.CloneEffects must keep declared
    /// HasTarget/Target/Side. StripDotFlame only rewrites SkillDef.EffectId
    /// (C001_slide); it does not mutate EffectDef rows.
    /// </summary>
    [Collection("G2RecheckCatalog")]
    public sealed class G2C1CloneTargetTests
    {
        [Fact]
        public void C1T1_Apply_NpBasic_PreservesBuiltinEffectDefFields()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    Catalog.BuildBuiltin();
                    var snapshot = SnapshotEffects(Catalog.Effects);
                    var originals = CaptureLiveRefs(Catalog.Effects);
                    Assert.True(snapshot.Count > 0, "builtin effect table empty");
                    Assert.False(
                        AnyHasTarget(snapshot),
                        "builtin EffectDef table unexpectedly sets HasTarget=true; extend this case to require a true-HasTarget clone");

                    var plan = VerificationRunPlan.Resolve("np.basic.v1", null);
                    Assert.NotNull(plan);
                    Assert.NotNull(plan.Fights);
                    Assert.Equal(VerificationScenario.Basic.Name, plan.Fights[0].Name);

                    var note = VerificationCatalog.Apply(plan);
                    Assert.DoesNotContain("apply failed", note, StringComparison.Ordinal);
                    Assert.True(VerificationCatalog.Installed);

                    AssertEffectsMatchSnapshot(
                        Catalog.Effects,
                        snapshot,
                        "cloned table after VerificationCatalog.Apply");
                    AssertLiveRefsMatchSnapshot(
                        originals,
                        snapshot,
                        "builtin EffectDef objects must not be mutated by Apply");

                    var burst = Catalog.TryEffect("burst_atk");
                    var taunt = Catalog.TryEffect("taunt");
                    var defDown = Catalog.TryEffect("def_down");
                    Assert.NotNull(burst);
                    Assert.NotNull(taunt);
                    Assert.NotNull(defDown);
                    Assert.Equal(TargetSide.Ally, burst.Side);
                    Assert.Equal(TargetSide.Self, taunt.Side);
                    Assert.Equal(TargetSide.Foe, defDown.Side);
                    Assert.False(burst.HasTarget);
                    Assert.False(taunt.HasTarget);
                    Assert.False(defDown.HasTarget);

                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();

                    AssertLiveRefsMatchSnapshot(
                        originals,
                        snapshot,
                        "builtin EffectDef objects must still match the pre-Apply snapshot after RestoreBuiltin");
                    AssertEffectsMatchSnapshot(
                        Catalog.Effects,
                        snapshot,
                        "Catalog after RestoreBuiltin");
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        [Fact]
        public void C1T2_C001Drive_BurstAtkOnAlliesNotEnemies_TauntOnCasterViaOverlayTap()
        {
            lock (CatalogGate)
            {
                RestoreBuiltinCatalog();
                try
                {
                    var plan = VerificationRunPlan.Resolve("np.basic.v1", null);
                    var note = VerificationCatalog.Apply(plan);
                    Assert.DoesNotContain("apply failed", note, StringComparison.Ordinal);
                    Assert.True(VerificationCatalog.Installed);

                    var drive = Catalog.MustSkill("C001_drive");
                    Assert.Equal("burst_atk", drive.EffectId);

                    Assert.True(Catalog.Stages != null && Catalog.Stages.Length > 0);
                    var sim = new BattleSim(Catalog.DefaultParty, 0, 6101, Catalog.Stages[0], null)
                    {
                        Speed = 1,
                        Auto = AutoMode.Manual,
                        ForceNoCrit = true,
                        Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
                    };
                    Assert.Equal("C001", sim.Allies[0] != null && sim.Allies[0].Def != null ? sim.Allies[0].Def.Id : "");

                    InflateAllies(sim);
                    InflateEnemies(sim);
                    LockFoes(sim);
                    SuppressEnemyAutoAttacks(sim);

                    TickUntil(sim, () => sim.Drive >= 100f, 90f, "Drive>=100");
                    Assert.True(sim.Drive >= 100f, "Drive did not reach 100 before DriveBegin. Drive=" + sim.Drive);

                    var begin = sim.Submit(BattleCommand.DriveBegin(0));
                    Assert.True(begin.Accepted, "DriveBegin rejected: " + begin.Reason);
                    var resolve = sim.Submit(BattleCommand.DriveResolve(DriveTiming.Perfect));
                    Assert.True(resolve.Accepted, "DriveResolve rejected: " + resolve.Reason);

                    var allyBurst = CountStatus(sim.Allies, "burst_atk");
                    var enemyBurst = CountStatusList(sim.Enemies, "burst_atk");
                    Assert.True(
                        allyBurst > 0,
                        "C001_drive burst_atk must land on living allies. ally=" + allyBurst + " enemy=" + enemyBurst);
                    Assert.Equal(0, enemyBurst);

                    // DefaultParty has no Tap/Slide that links taunt (C007_drive is Drive-only).
                    // Overlay a synthetic Tap that keeps the foe damage picker so a lost Side
                    // would send taunt to the enemy pool — the named Self side must win.
                    var tapId = sim.Allies[0].Def.TapSkillId;
                    var tap = Catalog.CloneSkill(Catalog.MustSkill(tapId));
                    tap.EffectId = "taunt";
                    sim.OverlaySkill(tapId, tap);

                    TickUntil(sim, () => sim.Allies[0] != null && sim.Allies[0].Charge >= 100f, 8f, "C001 Charge>=100");
                    var tapResult = sim.Submit(BattleCommand.Tap(0));
                    Assert.True(tapResult.Accepted, "overlay Tap(taunt) rejected: " + tapResult.Reason);

                    var caster = sim.Allies[0];
                    Assert.NotNull(FindStatus(caster, "taunt"));
                    for (int i = 1; i < sim.Allies.Length; i++)
                    {
                        var u = sim.Allies[i];
                        if (u == null) continue;
                        Assert.True(FindStatus(u, "taunt") == null, "taunt leaked onto ally slot " + i);
                    }
                    for (int i = 0; i < sim.Enemies.Count; i++)
                    {
                        var e = sim.Enemies[i];
                        if (e == null) continue;
                        Assert.True(FindStatus(e, "taunt") == null, "taunt leaked onto enemy slot " + i);
                    }
                }
                finally
                {
                    VerificationCatalog.RestoreBuiltin();
                    RestoreBuiltinCatalog();
                }
            }
        }

        static Dictionary<string, EffectSnap> SnapshotEffects(IReadOnlyDictionary<string, EffectDef> src)
        {
            var d = new Dictionary<string, EffectSnap>(StringComparer.Ordinal);
            if (src == null) return d;
            foreach (var kv in src)
            {
                if (kv.Value == null || string.IsNullOrEmpty(kv.Key)) continue;
                d[kv.Key] = EffectSnap.From(kv.Value);
            }
            return d;
        }

        static Dictionary<string, EffectDef> CaptureLiveRefs(IReadOnlyDictionary<string, EffectDef> src)
        {
            var d = new Dictionary<string, EffectDef>(StringComparer.Ordinal);
            if (src == null) return d;
            foreach (var kv in src)
            {
                if (kv.Value == null || string.IsNullOrEmpty(kv.Key)) continue;
                d[kv.Key] = kv.Value;
            }
            return d;
        }

        static bool AnyHasTarget(Dictionary<string, EffectSnap> snapshot)
        {
            foreach (var kv in snapshot)
                if (kv.Value.HasTarget) return true;
            return false;
        }

        static void AssertEffectsMatchSnapshot(
            IReadOnlyDictionary<string, EffectDef> live,
            Dictionary<string, EffectSnap> snapshot,
            string where)
        {
            Assert.NotNull(live);
            foreach (var kv in snapshot)
            {
                Assert.True(live.ContainsKey(kv.Key), where + " missing effect " + kv.Key);
                AssertEffectFields(kv.Value, live[kv.Key], where + " id=" + kv.Key);
            }
        }

        static void AssertLiveRefsMatchSnapshot(
            Dictionary<string, EffectDef> originals,
            Dictionary<string, EffectSnap> snapshot,
            string where)
        {
            foreach (var kv in originals)
            {
                Assert.True(snapshot.ContainsKey(kv.Key), where + " snapshot missing " + kv.Key);
                AssertEffectFields(snapshot[kv.Key], kv.Value, where + " id=" + kv.Key);
            }
        }

        /// <summary>
        /// StripDotFlame is the only named substitute Apply writes, and it only
        /// clears SkillDef.EffectId. No EffectDef field is a declared substitute.
        /// </summary>
        static bool NamedSubstituteDeclaresEffectField(string effectId, string field)
        {
            return false;
        }

        static void AssertEffectFields(EffectSnap expected, EffectDef actual, string where)
        {
            Assert.NotNull(actual);
            AssertField(where, actual.Id, "Id", expected.Id, actual.Id);
            AssertField(where, actual.Id, "Opcode", expected.Opcode, actual.Opcode);
            AssertField(where, actual.Id, "Kind", expected.Kind, actual.Kind);
            AssertField(where, actual.Id, "HasTarget", expected.HasTarget, actual.HasTarget);
            AssertField(where, actual.Id, "Target", expected.Target, actual.Target);
            AssertField(where, actual.Id, "Side", expected.Side, actual.Side);
            AssertField(where, actual.Id, "Trigger", expected.Trigger ?? "", actual.Trigger ?? "");
            AssertField(where, actual.Id, "PeriodSec", expected.PeriodSec, actual.PeriodSec);
            AssertField(where, actual.Id, "MaxStack", expected.MaxStack, actual.MaxStack);
            AssertField(where, actual.Id, "Magnitude", expected.Magnitude, actual.Magnitude);
            AssertField(where, actual.Id, "DurationSec", expected.DurationSec, actual.DurationSec);
            AssertField(where, actual.Id, "SourceTier", expected.SourceTier, actual.SourceTier);
            AssertField(where, actual.Id, "Group", expected.Group ?? "", actual.Group ?? "");
        }

        static void AssertField<T>(string where, string effectId, string field, T expected, T actual)
        {
            if (NamedSubstituteDeclaresEffectField(effectId, field)) return;
            Assert.True(
                EqualityComparer<T>.Default.Equals(expected, actual),
                where + " field " + field + " expected=" + expected + " actual=" + actual);
        }

        static int CountStatus(UnitState[] units, string id)
        {
            var n = 0;
            if (units == null) return 0;
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (u == null || !u.Alive) continue;
                if (FindStatus(u, id) != null) n++;
            }
            return n;
        }

        static int CountStatusList(List<UnitState> units, string id)
        {
            var n = 0;
            if (units == null) return 0;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null || !u.Alive) continue;
                if (FindStatus(u, id) != null) n++;
            }
            return n;
        }

        static void TickUntil(BattleSim sim, Func<bool> pred, float seconds, string label)
        {
            var n = (int)Math.Ceiling(seconds / BattleSim.TickDt) + 2;
            for (int i = 0; i < n && sim.Outcome == BattleOutcome.InProgress && !pred(); i++)
                sim.Tick();
            Assert.Equal(BattleOutcome.InProgress, sim.Outcome);
            Assert.True(pred(), label + " not reached after " + seconds + "s ticks. Drive=" + sim.Drive);
        }

        sealed class EffectSnap
        {
            public string Id;
            public string Opcode;
            public EffectKind Kind;
            public bool HasTarget;
            public TargetRule Target;
            public TargetSide Side;
            public float Magnitude;
            public float DurationSec;
            public int MaxStack;
            public int SourceTier;
            public string Group;
            public string Trigger;
            public float PeriodSec;

            public static EffectSnap From(EffectDef e)
            {
                return new EffectSnap
                {
                    Id = e.Id,
                    Opcode = e.Opcode,
                    Kind = e.Kind,
                    HasTarget = e.HasTarget,
                    Target = e.Target,
                    Side = e.Side,
                    Magnitude = e.Magnitude,
                    DurationSec = e.DurationSec,
                    MaxStack = e.MaxStack,
                    SourceTier = e.SourceTier,
                    Group = e.Group,
                    Trigger = e.Trigger,
                    PeriodSec = e.PeriodSec
                };
            }
        }
    }
}
