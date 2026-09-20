using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Resonance.Battle;
using Xunit;
using Xunit.Abstractions;

namespace Resonance.Tests
{
    /// <summary>
    /// Reproducible policy comparison on unmodified N5 content. This is automated simulation evidence,
    /// not player/UI footage. Only RelicIds change; no readiness, HP, damage, crit or clock fixtures.
    /// </summary>
    public sealed class OriginalBuildComparisonTests
    {
        const string OutputDirectoryVariable = "RESONANCE_ORIGINAL_COMPARISON_DIR";
        const string Policy = "Before each tick, focus the living enemy with lowest absolute HP, then lowest slot; "
            + "try ready Tap once per actor in order 1,2,4,0,3; then advance one tick. No skill holding, pause, or manual stat writes.";
        static readonly int[] Priority = { 1, 2, 4, 0, 3 };
        static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        static readonly JsonSerializerOptions CompactJson = new JsonSerializerOptions
        {
            IncludeFields = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        readonly ITestOutputHelper _output;
        public OriginalBuildComparisonTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void FixedN5_SixLegalBuildsShareEveryOtherInput_AndActualResultsReconcileWithTheLedger()
        {
            var common = RunBattleFactory.CreateInput("N5", "single", 260921, Array.Empty<string>(), null);
            // Stable identical identity, deliberately independent from the selected build.
            common.RunId = "fixed-n5-build-comparison";
            common.EncounterId = "fixed-n5-build-comparison/N5/1";
            common.AttemptId = "same-frozen-opening";
            common.BattleOrdinal = 1;
            var commonFingerprint = ExpeditionContent.Fingerprint(common);
            var builds = new[]
            {
                new Build("none", Array.Empty<string>()),
                new Build("mixed", new[] { "A01", "B01", "C01", "C02" }),
                new Build("family-a", new[] { "A01", "A02", "A03", "A04" }),
                new Build("family-b", new[] { "B01", "B02", "B03", "B04" }),
                new Build("family-c", new[] { "C01", "C02", "C03", "C04" }),
                new Build("family-b-plus-c01", new[] { "B01", "B02", "B03", "B04", "C01" })
            };
            var runs = new List<RunEvidence>();
            var simulations = new List<BattleSim>();
            foreach (var build in builds)
            {
                var input = common.DeepClone();
                input.RelicIds = (string[])build.RelicIds.Clone();
                Assert.Equal(commonFingerprint, WithoutRelicsFingerprint(input));
                RunBattleFactory.Validate(input); // Enforce ordinary content bounds and legal family prerequisites.
                var sim = RunBattleFactory.Create(input);
                Assert.Equal(ExpeditionContent.Fingerprint(input), ExpeditionContent.Fingerprint(sim.OpeningExpeditionInput));
                Assert.Equal(commonFingerprint, WithoutRelicsFingerprint(sim.OpeningExpeditionInput));
                Assert.Equal(1, sim.Speed);
                Assert.Equal(AutoMode.Manual, sim.Auto);
                Assert.False(sim.ForceNoCrit);
                Assert.Equal(common.OpeningHp, sim.Allies.Select(a => a.Hp).ToArray());
                Assert.All(sim.Allies, a => Assert.Equal(a.MaxHp, a.Hp));
                sim.FreezeInitialHeader();

                RunIdenticalPolicy(sim, input.Stage.TimeLimitSec);
                simulations.Add(sim);
                runs.Add(Capture(build, input, sim));
            }
            // The shared source itself also remains unchanged after every clone and simulation.
            Assert.Equal(commonFingerprint, ExpeditionContent.Fingerprint(common));
            var control = runs[0].Metrics;
            foreach (var run in runs)
                if (control.Outcome == nameof(BattleOutcome.Victory) && run.Metrics.Outcome == nameof(BattleOutcome.Victory)
                    && run.Metrics.GameSeconds > 0)
                    run.Metrics.ClearSpeedRelativeToControl = control.GameSeconds / run.Metrics.GameSeconds;

            var report = new
            {
                Schema = "original-build-comparison-v1",
                Scenario = "N5/single/seed260921/fullHP/defaultCrit/1x/Manual",
                Policy,
                PolicyFingerprint = ExpeditionContent.Fingerprint(new object[] { Policy, Priority }),
                SharedInputFingerprintIgnoringRelics = commonFingerprint,
                SharedInput = common,
                AllowedInputDifference = "RelicIds only",
                DerivedDamageMeaning = "Only actual ResolutionOrigin.Derived damage. C-family charge and damage-channel bonuses remain native resolutions.",
                HpLossMeaning = "AllyHpDamageTaken is cumulative real HP damage; AllyNetHpLoss is opening HP minus final HP after effective healing.",
                ClaimBoundary = "Fixed automated policy comparison, not ordinary UI play. No predeclared 2x success threshold.",
                Runs = runs
            };
            _output.WriteLine("ORIGINAL_BUILD_COMPARISON sharedInputFingerprint=" + commonFingerprint);
            foreach (var run in runs)
                _output.WriteLine("ORIGINAL_BUILD_COMPARISON_METRICS " + JsonSerializer.Serialize(run.Metrics, CompactJson));
            var outputDirectory = Environment.GetEnvironmentVariable(OutputDirectoryVariable);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                var directory = Path.GetFullPath(outputDirectory);
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, "comparison-n5-single-seed260921.json");
                File.WriteAllText(path, JsonSerializer.Serialize(report, Json), new UTF8Encoding(false));
                _output.WriteLine("ORIGINAL_BUILD_COMPARISON_ARTIFACT " + path);
            }
            else _output.WriteLine("ORIGINAL_BUILD_COMPARISON_ARTIFACT not requested; set " + OutputDirectoryVariable + " for the complete input, resolutions and command tape.");

            foreach (var sim in simulations)
            {
                Assert.True(sim.Outcome == BattleOutcome.Victory || sim.Outcome == BattleOutcome.Defeat,
                    "The real encounter must settle normally, not stop at a harness limit or rule error: " + sim.Outcome + "; " + sim.FailedReason);
                Assert.All(sim.CommandLog, c => Assert.True(c.Accepted, "Ready-policy command rejected: " + c));
                Assert.False(sim.ForceNoCrit);
                Assert.Equal(1, sim.Speed);
                Assert.Equal(AutoMode.Manual, sim.Auto);
                Reconcile(sim);
            }
        }

        static void RunIdenticalPolicy(BattleSim sim, float limitSeconds)
        {
            var boundedTicks = (int)Math.Ceiling(limitSeconds * BattleSim.TickHz) + BattleSim.TickHz;
            for (var step = 0; step < boundedTicks && sim.Outcome == BattleOutcome.InProgress; step++)
            {
                var target = sim.Enemies.Where(e => e.Alive).OrderBy(e => e.Hp).ThenBy(e => e.Slot).FirstOrDefault();
                if (target != null) sim.Submit(BattleCommand.FocusEnemy(target.Slot, CommandSource.Fixture));
                foreach (var slot in Priority)
                {
                    if (sim.Outcome != BattleOutcome.InProgress) break;
                    if (sim.CanAct(slot)) sim.Submit(BattleCommand.Tap(slot, CommandSource.Fixture));
                }
                if (sim.Outcome == BattleOutcome.InProgress) sim.Tick();
            }
        }

        static RunEvidence Capture(Build build, ExpeditionBattleInput input, BattleSim sim)
        {
            var results = sim.ExpeditionResolutions.ToArray();
            var enemyDamage = results.Where(IsEnemyDamage).Sum(r => (long)r.EffectiveDamage);
            var derived = results.Where(r => IsEnemyDamage(r) && r.Origin == ResolutionOrigin.Derived).Sum(r => (long)r.EffectiveDamage);
            var seconds = sim.TickIndex / (double)BattleSim.TickHz;
            return new RunEvidence
            {
                Metrics = new BuildMetrics
                {
                    Build = build.Name,
                    RelicIds = (string[])input.RelicIds.Clone(),
                    Outcome = sim.Outcome.ToString(),
                    FailureReason = sim.FailedReason,
                    Ticks = sim.TickIndex,
                    GameSeconds = seconds,
                    StageElapsedSeconds = input.Stage.TimeLimitSec - sim.TimeLeft,
                    EnemyEffectiveDamage = enemyDamage,
                    EnemyEffectiveDamagePerGameSecond = seconds > 0 ? enemyDamage / seconds : 0,
                    RelicDerivedEffectiveDamage = derived,
                    RelicDerivedShare = enemyDamage > 0 ? derived / (double)enemyDamage : 0,
                    AllyShieldAbsorbed = results.Where(r => r.TargetAlly).Sum(r => (long)r.ShieldAbsorbed),
                    AllyHpDamageTaken = results.Where(r => r.TargetAlly).Sum(r => (long)r.EffectiveHpDamage),
                    AllyNetHpLoss = input.OpeningHp.Sum(hp => (long)hp) - sim.Allies.Sum(a => (long)a.Hp),
                    AllyEffectiveHealing = results.Where(r => r.TargetAlly && r.SourceAlly && r.SourceSlot >= 0).Sum(r => (long)r.EffectiveHeal),
                    AllyShieldProduced = results.Where(r => r.TargetAlly).Sum(r => (long)r.ShieldProduced),
                    SuccessfulActiveSkills = sim.CommandLog.Count(c => c.Kind == BattleCommandKind.Tap && c.Accepted),
                    FinalAllyHp = sim.Allies.Select(a => a.Hp).ToArray(),
                    FinalEnemyHp = sim.Enemies.Select(e => e.Hp).ToArray(),
                    ResolutionCount = results.Length
                },
                InputFingerprint = ExpeditionContent.Fingerprint(input),
                InputFingerprintIgnoringRelics = WithoutRelicsFingerprint(input),
                CommandFingerprint = ExpeditionContent.Fingerprint(sim.CommandLog),
                FinalStateDigest = BattleStateDigest.Of(sim).ToCanonicalString(),
                Ledger = sim.ExpeditionTotals,
                Resolutions = results,
                Commands = sim.CommandLog.ToArray()
            };
        }

        static void Reconcile(BattleSim sim)
        {
            var r = sim.ExpeditionResolutions;
            var ledger = sim.ExpeditionTotals;
            Assert.Equal((long)r.Count, ledger.ResultsCount);
            Assert.Equal(r.Sum(x => (long)x.RequestedDamage), ledger.RequestedDamage);
            Assert.Equal(r.Sum(x => (long)x.ShieldAbsorbed), ledger.ShieldAbsorbed);
            Assert.Equal(r.Sum(x => (long)x.EffectiveHpDamage), ledger.EffectiveHpDamage);
            Assert.Equal(r.Sum(x => (long)x.Overkill), ledger.Overkill);
            Assert.Equal(r.Sum(x => (long)x.RequestedHeal), ledger.RequestedHeal);
            Assert.Equal(r.Sum(x => (long)x.EffectiveHeal), ledger.EffectiveHeal);
            Assert.Equal(r.Sum(x => (long)x.Overheal), ledger.Overheal);
            Assert.Equal(r.Sum(x => (long)x.ShieldProduced), ledger.ShieldProduced);
            Assert.Equal(r.Where(IsEnemyDamage).Sum(x => (long)x.EffectiveDamage), ledger.EnemyEffectiveDamage);
            Assert.Equal(r.Where(x => x.TargetAlly).Sum(x => (long)x.EffectiveDamage), ledger.AllyEffectiveDamageTaken);
            Assert.Equal(r.Where(x => x.TargetAlly).Sum(x => (long)x.ShieldAbsorbed), ledger.AllyShieldAbsorbed);
            Assert.Equal(r.Where(x => x.TargetAlly && x.SourceAlly && x.SourceSlot >= 0).Sum(x => (long)x.EffectiveHeal), ledger.AllyEffectiveHealing);
            Assert.Equal(ledger.RequestedDamage, ledger.ShieldAbsorbed + ledger.EffectiveHpDamage + ledger.Overkill);
            Assert.Equal(ledger.RequestedHeal, ledger.EffectiveHeal + ledger.Overheal);
            var opening = sim.OpeningExpeditionInput;
            for (var slot = 0; slot < sim.Allies.Length; slot++)
            {
                var healing = r.Where(x => x.TargetAlly && x.TargetSlot == slot).Sum(x => (long)x.EffectiveHeal);
                var damage = r.Where(x => x.TargetAlly && x.TargetSlot == slot).Sum(x => (long)x.EffectiveHpDamage);
                Assert.Equal((long)opening.OpeningHp[slot] + healing - damage, sim.Allies[slot].Hp);
            }
            foreach (var enemy in sim.Enemies)
            {
                var healing = r.Where(x => !x.TargetAlly && x.TargetSlot == enemy.Slot).Sum(x => (long)x.EffectiveHeal);
                var damage = r.Where(x => !x.TargetAlly && x.TargetSlot == enemy.Slot).Sum(x => (long)x.EffectiveHpDamage);
                Assert.Equal((long)enemy.MaxHp + healing - damage, enemy.Hp);
            }
        }

        static bool IsEnemyDamage(ResolutionResult result) => result.SourceAlly && result.SourceSlot >= 0 && !result.TargetAlly;
        static string WithoutRelicsFingerprint(ExpeditionBattleInput input)
        {
            var normalized = input.DeepClone();
            normalized.RelicIds = Array.Empty<string>();
            return ExpeditionContent.Fingerprint(normalized);
        }

        sealed class Build
        {
            public readonly string Name;
            public readonly string[] RelicIds;
            public Build(string name, string[] relicIds) { Name = name; RelicIds = relicIds; }
        }

        public sealed class RunEvidence
        {
            public BuildMetrics Metrics;
            public string InputFingerprint, InputFingerprintIgnoringRelics, CommandFingerprint, FinalStateDigest;
            public ResolutionLedger Ledger;
            public ResolutionResult[] Resolutions;
            public CommandRecord[] Commands;
        }

        public sealed class BuildMetrics
        {
            public string Build, Outcome, FailureReason;
            public string[] RelicIds;
            public int Ticks, SuccessfulActiveSkills, ResolutionCount;
            public double GameSeconds, StageElapsedSeconds, EnemyEffectiveDamagePerGameSecond, RelicDerivedShare;
            public double? ClearSpeedRelativeToControl;
            public long EnemyEffectiveDamage, RelicDerivedEffectiveDamage, AllyShieldAbsorbed,
                AllyHpDamageTaken, AllyNetHpLoss, AllyEffectiveHealing, AllyShieldProduced;
            public int[] FinalAllyHp, FinalEnemyHp;
        }
    }
}
