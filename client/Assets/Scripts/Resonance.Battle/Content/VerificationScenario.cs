using System;

namespace Resonance.Battle
{
    /// <summary>
    /// Named, versioned DESIGN_PLACEHOLDER verification fights for G2R14 natural play (N01/N02/N03).
    /// Numbers are engineering reachability policy, not original-game values.
    /// </summary>
    public sealed class VerificationScenario
    {
        public const string Provenance = DesignPlaceholderPolicy.Provenance;
        public const string SchemaVersion = DesignPlaceholderPolicy.SchemaVersion;

        public string Name;
        public string DisplayName;
        public string RegressionId;
        public float ChargeTimeSec;
        public int DeclaredAutoDriveGain;
        public bool HonorDeclaredAutoDriveGain;
        public float TimeLimitSec;
        public float EnemyHpMul;
        public float EnemyAtkMul;
        public float EnemyDefMul;
        public string[] Wave0;
        public string[] Wave1;
        public string[] RequiredGoals;
        public string Notes;

        /// <summary>
        /// N01 — Tap+Slide via real HUD before Drive remaps portrait taps.
        /// Allies spawn at Charge=35 (BattleSim.Spawn). ChargeTimeSec=1.25s → ready ~0.8s.
        /// TickUnit still does Drive += Max(14, DriveGain) until the integrator hunk lands;
        /// five autos therefore still add ~70 at ~2s and 100 at ~4s. The charge policy is
        /// what opens the Tap window; declared DriveGain=2 is for N04 once honored.
        /// </summary>
        public static readonly VerificationScenario Basic = new VerificationScenario
        {
            Name = "np.basic.v1",
            DisplayName = "DESIGN_PLACEHOLDER basic v1",
            RegressionId = "N01",
            ChargeTimeSec = 1.25f,
            DeclaredAutoDriveGain = 2,
            HonorDeclaredAutoDriveGain = true,
            TimeLimitSec = 120f,
            EnemyHpMul = 3.0f,
            EnemyAtkMul = 0.20f,
            EnemyDefMul = 1.0f,
            Wave0 = new[] { "E001", "E002", "E003" },
            Wave1 = new string[0],
            RequiredGoals = new[] { "Tap", "Slide" },
            Notes = "Tap+Slide before HUD DriveBegin remap. Pause/Speed optional. Fever not required."
        };

        /// <summary>
        /// N02 — Fever is required. Accumulate via real vfxGood QTE, then FeverTap two live slots.
        /// No mid-fight gauge writes. No FirePerfect. High HP so three QTE cycles survive.
        /// </summary>
        public static readonly VerificationScenario Fever = new VerificationScenario
        {
            Name = "np.fever.v1",
            DisplayName = "DESIGN_PLACEHOLDER fever v1",
            RegressionId = "N02",
            ChargeTimeSec = 1.25f,
            DeclaredAutoDriveGain = 2,
            HonorDeclaredAutoDriveGain = true,
            TimeLimitSec = 180f,
            EnemyHpMul = 20.0f,
            EnemyAtkMul = 0.15f,
            EnemyDefMul = 1.0f,
            Wave0 = new[] { "E001", "E002", "E003" },
            Wave1 = new string[0],
            RequiredGoals = new[] { "QTE", "Fever", "FeverTapA", "FeverTapB" },
            Notes = "Fever required. Two different alive slots must accept FeverTap. Not optional."
        };

        /// <summary>
        /// N03 — toggle Full Auto via HUD, observe auto policy, return to Manual.
        /// Auto Tap/Slide/Drive still bypass Submit (X05 / REPLAY lease). Auto FeverTap does Submit.
        /// Tanky wave so Full Auto can reach Fever and record Source=Auto FeverTap.
        /// </summary>
        public static readonly VerificationScenario Auto = new VerificationScenario
        {
            Name = "np.auto.v1",
            DisplayName = "DESIGN_PLACEHOLDER auto v1",
            RegressionId = "N03",
            ChargeTimeSec = 1.25f,
            DeclaredAutoDriveGain = 2,
            HonorDeclaredAutoDriveGain = true,
            TimeLimitSec = 180f,
            EnemyHpMul = 16.0f,
            EnemyAtkMul = 0.15f,
            EnemyDefMul = 1.0f,
            Wave0 = new[] { "E001", "E002", "E003" },
            Wave1 = new string[0],
            RequiredGoals = new[] { "AutoFull", "AutoSkillObserved", "AutoSubmit", "AutoManual" },
            Notes = "Real UI CycleBattleAuto. Submit Auto skills preferred; AutoFire bypass is recorded if no Source=Auto skill."
        };

        public static VerificationScenario Named(string token)
        {
            if (string.IsNullOrEmpty(token)) return Basic;
            token = token.Trim();
            if (string.Equals(token, Basic.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "basic", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "n01", StringComparison.OrdinalIgnoreCase))
                return Basic;
            if (string.Equals(token, Fever.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "fever", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "n02", StringComparison.OrdinalIgnoreCase))
                return Fever;
            if (string.Equals(token, Auto.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "auto", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "n03", StringComparison.OrdinalIgnoreCase))
                return Auto;
            return null;
        }

        public static bool IsMatrix(string token)
        {
            if (string.IsNullOrEmpty(token)) return true;
            token = token.Trim();
            if (token == "1") return true;
            return string.Equals(token, "matrix", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "np.matrix.v1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "all", StringComparison.OrdinalIgnoreCase);
        }

        public static VerificationScenario[] MatrixFights()
        {
            return new[] { Basic, Fever, Auto };
        }

        public string StageId()
        {
            if (Name == Fever.Name) return "NP-FEVER";
            if (Name == Auto.Name) return "NP-AUTO";
            return "NP-BASIC";
        }
    }

    /// <summary>CLI / request-file plan for one natural-play session.</summary>
    public sealed class VerificationRunPlan
    {
        public string Mode;
        public VerificationScenario[] Fights;
        public bool RematchExitAfterLastWin;

        public static VerificationRunPlan Resolve(string requestText, string[] argv)
        {
            var token = ReadToken(requestText, argv);
            if (VerificationScenario.IsMatrix(token))
            {
                return new VerificationRunPlan
                {
                    Mode = "matrix",
                    Fights = VerificationScenario.MatrixFights(),
                    RematchExitAfterLastWin = false
                };
            }
            var one = VerificationScenario.Named(token) ?? VerificationScenario.Basic;
            var rematch = one.Name != VerificationScenario.Auto.Name;
            return new VerificationRunPlan
            {
                Mode = one.Name,
                Fights = new[] { one },
                RematchExitAfterLastWin = rematch
            };
        }

        public VerificationScenario FightAt(int index)
        {
            if (Fights == null || index < 0 || index >= Fights.Length) return null;
            return Fights[index];
        }

        static string ReadToken(string requestText, string[] argv)
        {
            if (argv != null)
            {
                for (int i = 0; i < argv.Length; i++)
                {
                    if (!string.Equals(argv[i], "-natural-scenario", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (i + 1 < argv.Length && !string.IsNullOrEmpty(argv[i + 1]) && argv[i + 1][0] != '-')
                        return argv[i + 1];
                }
            }
            if (!string.IsNullOrEmpty(requestText))
            {
                var line = requestText.Trim();
                var nl = line.IndexOfAny(new[] { '\r', '\n', ' ', '\t' });
                if (nl > 0) line = line.Substring(0, nl);
                return line;
            }
            return "matrix";
        }
    }
}
