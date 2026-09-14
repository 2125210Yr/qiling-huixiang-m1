namespace Resonance.Battle
{
    /// <summary>
    /// Process-wide bind for G2R14 natural-play verification catalogs.
    /// Every numeric field is <c>DESIGN_PLACEHOLDER</c> engineering policy, not original-game / GL.
    /// Bound only before <c>GameRoot</c> constructs a <c>BattleSim</c>; never written into a live fight.
    /// </summary>
    public static class DesignPlaceholderPolicy
    {
        public const string Provenance = "DESIGN_PLACEHOLDER";
        public const string SchemaVersion = "G2R14-NP-1";

        public static bool Active { get; private set; }
        public static string ScenarioName { get; private set; }
        public static string Schema { get; private set; }
        /// <summary>
        /// When true, <c>BattleSim.TickUnit</c> should add <see cref="DeclaredAutoDriveGain"/>
        /// (or the skill's own <c>DriveGain</c>) instead of <c>Math.Max(14f, DriveGain)</c>.
        /// The production floor stays until the integrator applies patches/G2R14-NATURAL.md.
        /// </summary>
        public static bool HonorDeclaredAutoDriveGain { get; private set; }
        public static int DeclaredAutoDriveGain { get; private set; }
        public static float ChargeTimeSec { get; private set; }

        public static void Bind(VerificationScenario scenario)
        {
            if (scenario == null)
            {
                Clear();
                return;
            }
            Active = true;
            ScenarioName = scenario.Name ?? "";
            Schema = SchemaVersion;
            HonorDeclaredAutoDriveGain = scenario.HonorDeclaredAutoDriveGain;
            DeclaredAutoDriveGain = scenario.DeclaredAutoDriveGain;
            ChargeTimeSec = scenario.ChargeTimeSec;
        }

        internal static void Clear()
        {
            Active = false;
            ScenarioName = "";
            Schema = "";
            HonorDeclaredAutoDriveGain = false;
            DeclaredAutoDriveGain = 0;
            ChargeTimeSec = 0f;
        }
    }
}
