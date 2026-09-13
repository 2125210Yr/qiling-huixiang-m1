using System.Text;
using Resonance.Battle;

namespace Resonance.App
{
    /// <summary>
    /// Read-only pause / speed view of <see cref="BattleSim"/> + <see cref="BattleClockPolicy"/>.
    /// Host persist tiers are ×1 / ×2 / ×3 (primary GT Robin shows 3x SPEED).
    /// Reference speed table U011 still UNKNOWN. Not T04. Not M1 acceptance.
    /// </summary>
    public readonly struct PauseClockView
    {
        public readonly bool HasBattle;
        public readonly bool Paused;
        public readonly int Speed;
        public readonly float BattleDt;
        public readonly string SpeedWord;
        public readonly string StatusLine;
        public readonly string PolicyLine;

        public PauseClockView(bool hasBattle, bool paused, int speed, float battleDt,
            string speedWord, string statusLine, string policyLine)
        {
            HasBattle = hasBattle;
            Paused = paused;
            Speed = speed;
            BattleDt = battleDt;
            SpeedWord = speedWord ?? "";
            StatusLine = statusLine ?? "";
            PolicyLine = policyLine ?? "";
        }
    }

    public static class PauseClockReadout
    {
        public const string LayoutStatus = "NEEDS_REFERENCE";
        public const int SpeedLo = 1;
        public const int SpeedMid = 2;
        public const int SpeedHi = 3;

        public const string Title = "PAUSE";
        public const string SpeedSection = "SPEED";
        /// <summary>Inventory engineering copy. Pause card does not print these.</summary>
        public const string HomeWarn = "Leaving returns to Home";
        public const string HomeWarnEn = HomeWarn;
        public const string UiIndependent = "UI does not freeze with pause";
        public const string Unmeasured = "reference unmeasured";

        public static bool IsHostTier(int speed)
        {
            return speed == SpeedLo || speed == SpeedMid || speed == SpeedHi;
        }

        public static string FormatSpeed(int speed)
        {
            if (speed < 1) speed = 1;
            return "×" + speed;
        }

        public static PauseClockView Read(BattleSim battle)
        {
            if (battle == null)
            {
                return new PauseClockView(false, false, SpeedLo, BattleSim.TickDt,
                    FormatSpeed(SpeedLo), "no battle clock", "read-only bind failed · " + Unmeasured);
            }

            var speed = battle.Speed < 1 ? 1 : battle.Speed;
            var clocks = battle.Clocks;
            var dt = clocks != null ? clocks.BattleDt(speed) : BattleSim.TickDt * speed;
            if (dt <= 0f) dt = BattleSim.TickDt;

            var word = FormatSpeed(speed);
            var status = battle.Paused
                ? "battle clock paused  ·  " + word
                : "battle clock running  ·  " + word;
            return new PauseClockView(true, battle.Paused, speed, dt, word, status, PolicyLine(clocks, speed));
        }

        public static string PolicyLine(BattleClockPolicy clocks, int speed)
        {
            var sb = new StringBuilder(96);
            sb.Append("step ").Append(FormatSpeed(speed < 1 ? 1 : speed));
            sb.Append("  ·  ").Append(Coupling(clocks));
            sb.Append("  ·  ").Append(UiIndependent);
            sb.Append("  ·  ").Append(Unmeasured);
            return sb.ToString();
        }

        public static string Coupling(BattleClockPolicy clocks)
        {
            if (clocks == null) return "clock policy default";
            var sb = new StringBuilder(48);
            Append(sb, clocks.StageCountdownScalesWithSpeed, "countdown");
            Append(sb, clocks.ChargeScalesWithSpeed, "charge");
            Append(sb, clocks.SlideCdScalesWithSpeed, "slide CD");
            Append(sb, clocks.StatusDurationScalesWithSpeed, "status");
            Append(sb, clocks.AutoIntervalScalesWithSpeed, "auto");
            Append(sb, clocks.FeverWindowScalesWithSpeed, "fever win");
            Append(sb, clocks.DriveQteScalesWithSpeed, "drive QTE");
            if (sb.Length == 0) return "clocks ignore speed";
            return "scales with speed: " + sb;
        }

        static void Append(StringBuilder sb, bool on, string word)
        {
            if (!on || sb == null || string.IsNullOrEmpty(word)) return;
            if (sb.Length > 0) sb.Append('·');
            sb.Append(word);
        }
    }
}
