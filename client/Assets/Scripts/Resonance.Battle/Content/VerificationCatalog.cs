using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    /// <summary>
    /// Installs DESIGN_PLACEHOLDER verification stages over a cloned builtin catalog.
    /// Must run before <c>StartBattleAt</c>. RestoreBuiltin after the session so Editor play
    /// does not keep the overlay. Does not write HP/Drive/Charge/Fever on a live BattleSim.
    /// Verification-only: Apply writes <see cref="PlayableSubstitutes.StripDotFlame"/>
    /// onto the cloned skill map. Production builtin <c>C001_slide</c> stays
    /// <c>EffectId=dot_flame</c> outside Apply.
    /// </summary>
    public static class VerificationCatalog
    {
        public const string Provenance = DesignPlaceholderPolicy.Provenance;
        public const string SchemaVersion = DesignPlaceholderPolicy.SchemaVersion;

        public static bool Installed { get; private set; }
        public static string InstalledMode { get; private set; }

        public static string Apply(VerificationRunPlan plan)
        {
            if (plan == null || plan.Fights == null || plan.Fights.Length == 0)
                return "plan empty";
            try
            {
                Catalog.BuildBuiltin();
                var chars = CloneChars();
                var skills = CloneSkills();
                var effects = CloneEffects();
                var first = plan.Fights[0];
                ApplyChargeAndDrive(chars, skills, first);
                ApplyNamedSubstitute(skills);
                var stages = new StageDef[plan.Fights.Length];
                for (int i = 0; i < plan.Fights.Length; i++)
                    stages[i] = ToStage(plan.Fights[i], i);
                Catalog.Install(chars, skills, effects, stages[0], stages);
                DesignPlaceholderPolicy.Bind(first);
                var mode = plan.Mode ?? first.Name ?? "";
                Installed = true;
                InstalledMode = mode + "+" + PlayableSubstitutes.StripDotFlame;
                var roster = DescribeRoster(Catalog.DefaultParty, stages);
                return Provenance + " " + SchemaVersion
                    + " mode=" + InstalledMode
                    + " sub=" + PlayableSubstitutes.StripDotFlame
                    + " fights=" + stages.Length
                    + " chargeSec=" + first.ChargeTimeSec.ToString("0.##")
                    + " declaredAutoDriveGain=" + first.DeclaredAutoDriveGain
                    + " honorDeclared=" + first.HonorDeclaredAutoDriveGain
                    + " tickUnitFloor=Math.Max(14f,DriveGain) until integrator hunk"
                    + " " + roster;
            }
            catch (Exception e)
            {
                Installed = false;
                InstalledMode = "";
                try { DesignPlaceholderPolicy.Clear(); } catch { }
                return "apply failed: " + (e != null ? e.Message : "unknown");
            }
        }

        /// <summary>
        /// Write <see cref="PlayableSubstitutes.CreateStripDotFlameOverlays"/> clones
        /// back into the verification skill map. Does not mutate production defaults.
        /// Does not implement Dot.
        /// </summary>
        static void ApplyNamedSubstitute(Dictionary<string, SkillDef> skills)
        {
            if (skills == null) return;
            foreach (var kv in PlayableSubstitutes.CreateStripDotFlameOverlays(skills))
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;
                skills[kv.Key] = kv.Value;
            }
        }

        /// <summary>
        /// Non-throwing roster note for DefaultParty + verification waves.
        /// Never calls <see cref="Catalog.EnsurePartyPlayable"/>.
        /// </summary>
        static string DescribeRoster(string[] party, StageDef[] stages)
        {
            try
            {
                var report = new ContentValidationReport();
                if (stages == null || stages.Length == 0)
                {
                    AppendReport(report, Catalog.EvaluatePartyPlayable(party, (StageDef)null, null, null));
                }
                else
                {
                    for (int i = 0; i < stages.Length; i++)
                        AppendReport(report, Catalog.EvaluatePartyPlayable(party, stages[i], null, null));
                }
                var blocking = report.PlayableRosterViolations();
                if (blocking == null || blocking.Count == 0)
                    return "roster=ok";
                return "roster=" + report.Summary();
            }
            catch (Exception e)
            {
                return "roster=eval_failed " + (e != null ? e.Message : "");
            }
        }

        static void AppendReport(ContentValidationReport dest, ContentValidationReport src)
        {
            if (dest == null || src == null || src.Violations == null) return;
            for (int i = 0; i < src.Violations.Count; i++)
                dest.Violations.Add(src.Violations[i]);
        }

        public static void RestoreBuiltin()
        {
            DesignPlaceholderPolicy.Clear();
            Installed = false;
            InstalledMode = "";
            Catalog.BuildBuiltin();
            CatalogJson.TryLoadDefault();
        }

        static void ApplyChargeAndDrive(
            Dictionary<string, CharacterDef> chars,
            Dictionary<string, SkillDef> skills,
            VerificationScenario policy)
        {
            if (policy == null) return;
            if (chars != null)
            {
                foreach (var kv in chars)
                {
                    var c = kv.Value;
                    if (c == null || c.IsEnemy) continue;
                    c.ChargeTimeSec = policy.ChargeTimeSec;
                }
            }
            if (skills == null) return;
            foreach (var kv in skills)
            {
                var s = kv.Value;
                if (s == null) continue;
                var auto = s.Type == SkillType.Auto
                    || (!string.IsNullOrEmpty(s.Id) && s.Id.EndsWith("_auto"));
                if (!auto) continue;
                s.DriveGain = policy.DeclaredAutoDriveGain;
            }
        }

        static StageDef ToStage(VerificationScenario sc, int index)
        {
            var id = sc.StageId();
            if (string.IsNullOrEmpty(id)) id = "NP-" + (index + 1);
            return new StageDef
            {
                Id = id,
                Name = Provenance + " " + sc.DisplayName,
                TimeLimitSec = sc.TimeLimitSec,
                Wave0 = Catalog.CopyWave(sc.Wave0),
                Wave1 = Catalog.CopyWave(sc.Wave1),
                EnemyHpMul = sc.EnemyHpMul,
                EnemyAtkMul = sc.EnemyAtkMul,
                EnemyDefMul = sc.EnemyDefMul
            };
        }

        static Dictionary<string, CharacterDef> CloneChars()
        {
            var d = new Dictionary<string, CharacterDef>();
            if (Catalog.Characters == null) return d;
            foreach (var kv in Catalog.Characters)
            {
                if (kv.Value == null) continue;
                d[kv.Key] = Growth.Clone(kv.Value);
            }
            return d;
        }

        static Dictionary<string, SkillDef> CloneSkills()
        {
            var d = new Dictionary<string, SkillDef>();
            if (Catalog.Skills == null) return d;
            foreach (var kv in Catalog.Skills)
            {
                if (kv.Value == null) continue;
                d[kv.Key] = Catalog.CloneSkill(kv.Value);
            }
            return d;
        }

        static Dictionary<string, EffectDef> CloneEffects()
        {
            var d = new Dictionary<string, EffectDef>();
            if (Catalog.Effects == null) return d;
            foreach (var kv in Catalog.Effects)
            {
                var e = kv.Value;
                if (e == null) continue;
                d[kv.Key] = new EffectDef
                {
                    Id = e.Id,
                    Opcode = e.Opcode,
                    Kind = e.Kind,
                    Magnitude = e.Magnitude,
                    DurationSec = e.DurationSec,
                    MaxStack = e.MaxStack,
                    SourceTier = e.SourceTier,
                    Group = e.Group,
                    Trigger = e.Trigger,
                    PeriodSec = e.PeriodSec
                };
            }
            return d;
        }
    }
}
